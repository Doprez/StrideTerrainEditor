//by Idomeneas
using HeightMapEditor;
using ImGui;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using StrideTerrain.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Keys = Stride.Input.Keys;
using static TerrainEditor.TerrainScript;

namespace TerrainEditor
{
    public enum CameraType
    {
        FreeMovement,//move anywhere
        FirstPerson,
        Shoulder,
        Isometric,
        TilesInGame,
        ThirdPerson,//leave last always
    }
    /// <summary>
    /// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
    /// </summary>
    /// <remarks>
    /// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </remarks>
    public class MultiTypeCameraController : SyncScript
    {
        [DataMember(0)]
        public CameraComponent MinimapCamera { get; set; }

        [DataMember(1)]
        public CharacterComponent Character { get; set; }
        [DataMember(2)]
        public ModelComponent Mannequin { get; set; }
        [DataMember(3)]
        public SpriteComponent RenderToTexture { get; set; }
        [DataMember(4)]
        public CameraComponent MultiCamera { get; set; }

        public static CameraType CameraType { get; set; } = CameraType.FreeMovement;

        #region FreeMovementType
        private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;
        private Vector3 upVector;
        private Vector3 translation;
        private float yaw, roll;
        private float pitch;

        public bool Gamepad { get; set; } = false;

        public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

        public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

        public float SpeedFactor { get; set; } = 100.0f;

        public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

        public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

        public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

        #endregion FreeMovementType

        #region ThirdPerson
        /// <summary>
        /// Starting camera distance from the target
        /// </summary>
        public float DefaultDistance { get; set; } = 6f;

        /// <summary>
        /// Minimum camera distance from the target
        /// </summary>
        public float MinimumDistance { get; set; } = 0.4f;

        /// <summary>
        /// Cone radius for the collision cone used to hold the camera
        /// </summary>
        public float ConeRadius { get; set; } = 1.25f;

        /// <summary>
        /// Check to invert the horizontal camera movement
        /// </summary>
        public bool InvertX { get; set; } = false;

        /// <summary>
        /// Minimum camera distance from the target
        /// </summary>
        public float MinVerticalAngle { get; set; } = -20f;

        /// <summary>
        /// Maximum camera distance from the target
        /// </summary>
        public float MaxVerticalAngle { get; set; } = 70f;

        /// <summary>
        /// Check to invert the vertical camera movement
        /// </summary>
        public bool InvertY { get; set; } = false;

        /// <summary>
        /// Maximum rotation speed for the camera around the target in degrees per second
        /// </summary>
        public float RotationSpeed { get; set; } = 360f;

        /// <summary>
        /// Maximum rotation speed for the camera around the target in degrees per second
        /// </summary>
        public float VerticalSpeed { get; set; } = 65f;

        private Vector3 cameraRotationXYZ = new Vector3(-20, 45, 0);
        private Vector3 targetRotationXYZ = new Vector3(-20, 45, 0);
        private readonly EventReceiver<Vector2> cameraDirectionEvent = new EventReceiver<Vector2>(PlayerInput.CameraDirectionEventKey);
        private List<HitResult> resultsOutput;
        private ConeColliderShape coneShape;
        public Vector3 VantagePoint = new Vector3(0, 5, 0);
        public static Vector3 VantagePoint3rdPerson = 
            new Vector3(0, 1.65f, 5f);

        #endregion ThirdPerson

        public Prefab ClickEffect { get; set; }
//       public Prefab ClickEffectBallEntity { get; set; }
        public static Entity ClickBallModelEntity { get; set; }
        public ClickResult lastClickResult;
        TerrainComponent tcomp;

        public override void Start()
        {
            base.Start();
            Entity Terrain_Entity =
                       Services.GetService<SceneSystem>().SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            tcomp = Terrain_Entity.Get<TerrainComponent>();
            // Default up-direction
            upVector = Vector3.UnitY;
            ClickBallModelEntity = new Entity();
            SceneSystem.SceneInstance.RootScene.Entities.Add(ClickBallModelEntity);
            // Add a model included in the game files.
            var modelComponent = ClickBallModelEntity.GetOrCreate<ModelComponent>();
  //          modelComponent.Model = Content.Load<Model>("TerrainSelectionSphere");
            if (TerrainEditorView.ShowSelectionBall)
            {
                modelComponent.Model = Content.Load<Model>("TerrainSelectionSphere");
            }
            else
            {
                modelComponent.Model = null;
            }

            // Configure touch input
            if (!Platform.IsWindowsDesktop)
            {
                Input.Gestures.Add(new GestureConfigDrag());
                Input.Gestures.Add(new GestureConfigComposite());
            }
            ResettingCamera = true;

            coneShape = new ConeColliderShape(DefaultDistance, ConeRadius, ShapeOrientation.UpZ);
            resultsOutput = new List<HitResult>();
            //    if (Entity.GetParent() == null) throw new ArgumentException("ThirdPersonCamera should be placed as a child entity of its target entity!");

            Midpointxy = new Vector2(
                tcomp.m_QuadSideWidthX* tcomp.Width / 2,
                tcomp.m_QuadSideWidthZ * tcomp.Height / 2);

            Entity a = Game.Services.GetService<SceneSystem>().SceneInstance.
                FirstOrDefault(a => a.Name == "CubeInstancing");
            ModelComponent cube = a.Get<ModelComponent>();
            float ht = tcomp.GetCPUHeightAt((int)Midpointxy.X, (int)Midpointxy.Y);
            Vector3 pos = new Vector3(Midpointxy.X, ht, Midpointxy.Y);
            cube.Entity.Transform.Position =
                //new Vector3(0,tcomp.Heightmap.GetHeightAt(0,0),0);
                new Vector3(Midpointxy.X, ht, Midpointxy.Y);

            //the crosshair is always the first child of the camera
            crosshair = Entity.GetChild(0).Get<SpriteComponent>();
            crosshair.Enabled = false;  //disable since we start at terrain editor always

            MinimapCamera.Entity.Transform.Position = Character.Entity.Transform.Position
                + new Vector3(0, TerrainInGameGumps.MiniMapHeight, 0);
            MinimapCamera.Entity.Transform.Rotation = new Quaternion(
                -0.5f, 0.5f, 0.5f, 0.5f);

            Mannequin.Entity.Enable<ActivableEntityComponent>(false);
            Character.Entity.Enable<ActivableEntityComponent>(false);
        }

        private SpriteComponent crosshair = null;
        private Vector2 Midpointxy;

        private readonly StringBuilder fpsStatStringBuilder = new StringBuilder();
        private string fpsStatString = string.Empty;

        public void ResetCamera()
        {
           // Thread.Sleep(1500);
            if (TerrainEditorView.CurrentEditorMode == EditorMode.TerrainEditor)
            {
                CameraSpeed = 100.0f;
                MultiCamera.AspectRatio = 1.778f;
                MultiCamera.VerticalFieldOfView = 45.0f;
                MultiCamera.NearClipPlane = 0.1f;
                MultiCamera.FarClipPlane = 1000.0f;
                Mannequin.Entity.Transform.Position = Vector3.Zero;
                Character.Entity.Transform.Position = Vector3.Zero;
                Midpointxy = new Vector2( tcomp.m_QuadSideWidthX * tcomp.Width / 2,tcomp.m_QuadSideWidthZ * tcomp.Height / 2);
                float ht = tcomp.GetCPUHeightAt((int)Midpointxy.X, (int)Midpointxy.Y);
                //  Character.Entity.Transform.Position=
                //    new Vector3(Midpointxy.X, ht+150, Midpointxy.Y);//.Entity.Transform.Position = Vector3.Zero;
                MultiCamera.Entity.Transform.Position = new Vector3(0, 0, 5);
                MultiCamera.Entity.Transform.Rotation = new Quaternion(0, 0, 0, 1);
                MultiCamera.Entity.GetParent().Transform.Position = new Vector3(Midpointxy.X, ht + tcomp.HeightRange.Y-tcomp.HeightRange.X, Midpointxy.Y + 5);
                MultiCamera.Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(-0.64f, -0.045f, 0.0f);//new Vector3(-1.345f, -0.122f, 0.03f);
            }
            else if (TerrainEditorView.CurrentEditorMode == EditorMode.InGameTerrain)
            {
                crosshair.Enabled = false;
                //if (CameraType == CameraType.ThirdPerson)
                {
                    MultiCamera.Entity.GetParent().Transform.Position = new Vector3(0, 1.65f, 0);
                    MultiCamera.Entity.GetParent().Transform.Rotation = 
                        new Quaternion(-0.16715722f,-0.55716187f, -0.115674f, 0.8051389f);
                    MultiCamera.Entity.Transform.Position = new Vector3(0, 0, 5);
                    MultiCamera.Entity.Transform.Rotation =
                        new Quaternion(0, 0, 0, 1);
                    Midpointxy = new Vector2(tcomp.m_QuadSideWidthX * tcomp.Width / 2, tcomp.m_QuadSideWidthZ * tcomp.Height / 2);
                    float ht = tcomp.GetCPUHeightAt((int)Midpointxy.X, (int)Midpointxy.Y);
                    Vector3 pos = new Vector3(Midpointxy.X, ht + 5, Midpointxy.Y);
                    Mannequin.Entity.Transform.Position = Vector3.Zero;
                    Character.Teleport(pos);

             //       Entity WaterPlane = Game.Services.GetService<SceneSystem>().SceneInstance
             //           .FirstOrDefault(a => a.Name == "WaterPlane");
              //      WaterPlane.Transform.Position = new Vector3(posxy.X, ht, posxy.Y);// MultiTypeMultiCameraController.ClickBallModelEntity.Transform.Position;

                }
            }
            else if (TerrainEditorView.CurrentEditorMode == EditorMode.InGameTiles)
            {
                //the camera is like freemovement. we probably want am Isometric
                //camera like civ games. that way you only have to show only a
                //few of the tiles, those within view
                CameraSpeed = 100.0f;
                MultiCamera.AspectRatio = 1.778f;
                MultiCamera.VerticalFieldOfView = 45.0f;
                MultiCamera.NearClipPlane = 0.1f;
                MultiCamera.FarClipPlane = 2000.0f;
                Mannequin.Entity.Transform.Position = Vector3.Zero;
                Character.Entity.Transform.Position = Vector3.Zero;
                MultiCamera.Entity.Transform.Position = new Vector3(0, 0, 5);
                MultiCamera.Entity.Transform.Rotation = new Quaternion(0, 0, 0, 1);
                MultiCamera.Entity.GetParent().Transform.Position = Vector3.Zero;
                MultiCamera.Entity.GetParent().Transform.RotationEulerXYZ = Vector3.Zero;
                TerrainTiles.Index_inWorld = new Int2(
                    TerrainTiles.m_NumTilesWideX / 2 ,
                    TerrainTiles.m_NumTilesHighZ / 2);
                MultiCamera.Entity.Transform.Position =
               TerrainTiles.Tiles[TerrainTiles.Index_inWorld.X +
               TerrainTiles.m_NumTilesWideX *
               TerrainTiles.Index_inWorld.Y].WorldPosition;
                MultiCamera.Entity.Transform.Position.Y =
                    TerrainEditorView.InGameTiles_Camera_Height;
                MultiCamera.Entity.Transform.Position.X +=
                    TerrainTiles.Width / 2;
                MultiCamera.Entity.Transform.Position.Z +=
                    TerrainTiles.Height / 2;
               // MultiCamera.Entity.GetParent().Transform.Rotation =
               //     Quaternion.RotationY(3*MathF.PI/2);
               MultiCamera.Entity.Transform.RotationEulerXYZ =
                    new Vector3(1.943f, -0.001f, -3.14f);//new Vector3(-0.23f, -0.062f, 0.0f);//new Vector3(-1.345f, -0.122f, 0.03f);
            }
        }

        private bool ResettingCamera=false;
        private void UpdateModes()
        {
            //always show switching mode options            
            {
                string str = "Editor Modes:";
                var curOffset = new Int2(10, 10);
                DebugText.Print(str, curOffset);
                curOffset.X += str.Length * 10;
                //first scene: terraineditor
                str = "F1- Terrain Editor";
                if (TerrainEditorView.CurrentEditorMode == EditorMode.TerrainEditor)
                {
                    DebugText.Print(str, curOffset, Color.Green);
                }
                else
                {
                    DebugText.Print(str, curOffset, Color.White);
                }
                curOffset.X += str.Length * 10;
                str = "F2- Terrain Preview";
                if (TerrainEditorView.CurrentEditorMode == EditorMode.InGameTerrain)
                {
                    DebugText.Print(str, curOffset, Color.Green);
                    curOffset.X += str.Length * 10;
                    str = "F8- Cycle Camera: " + CameraType.ToString();
                    DebugText.Print(str, curOffset, Color.White);
                }
                else
                {
                    DebugText.Print(str, curOffset, Color.White);
                }
                curOffset.X += str.Length * 10;
                str = "F3- World Tiles Preview";
                if (TerrainEditorView.CurrentEditorMode == EditorMode.InGameTiles)
                {
                    DebugText.Print(str, curOffset, Color.Green);
                }
                else
                {
                    DebugText.Print(str, curOffset, Color.White);
                }
            }
            
            if (Input.HasKeyboard)
            {
                if (Input.IsKeyPressed(Keys.F1) &&
                    TerrainEditorView.CurrentEditorMode != EditorMode.TerrainEditor)
                {
                    TerrainInGameGumps.Show_MiniMap = false;
                    TerrainTilesGumps.Show_WorldMap = false;

                    CameraType = CameraType.FreeMovement;
                    CameraSpeed = 100;
                    TerrainEditorView.CurrentEditorMode = EditorMode.TerrainEditor;
                    // tcomp.Entity.Enable<ActivableEntityComponent>(true);
                    tcomp.ToggleVisible(true);
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    Entity a = Game.Services.GetService<SceneSystem>().SceneInstance
                       .FirstOrDefault(a => a.Name == "TerrainInGame");
                    if (a != null)
                    {
                        TerrainScriptInGame scr = a.Get<TerrainScriptInGame>();
                        SceneSystem.SceneInstance.RootScene.Entities.Remove(
                            scr.TerrainModelEntity);
                    }
                    Mannequin.Entity.Enable<ActivableEntityComponent>(false);
                    Character.Entity.Enable<ActivableEntityComponent>(false);
                    Entity a1 = Game.Services.GetService<SceneSystem>().SceneInstance.
                        FirstOrDefault(a => a.Name == "CubeInstancing");
                    a1.Enable<ActivableEntityComponent>(true);
                    ClickBallModelEntity.Enable<ActivableEntityComponent>(true);
                    ResettingCamera =true;
                    TerrainTiles.ToggleWorldTiles(false, tcomp);
                }
                if (Input.IsKeyPressed(Keys.F2) &&
                    TerrainEditorView.CurrentEditorMode != EditorMode.InGameTerrain)
                {
                    TerrainInGameGumps.Show_MiniMap = true;
                    TerrainTilesGumps.Show_WorldMap = false;
                    CameraSpeed = 10;
                    DefaultDistance = 6;
                    if (TerrainEditorView.TerrainLOD != 1)
                    {
                        TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot use LOD in the preview.");
                        TerrainEditorView.TerrainLOD = 1;
                        tcomp.TerrainLOD = 1;
                        tcomp.FullUpdateLOD(TerrainEditorView.TerrainWeights1, TerrainEditorView.TerrainWeights2);
                    }
                    CameraType = CameraType.ThirdPerson;
                    Entity a1 = Game.Services.GetService<SceneSystem>().SceneInstance.
                        FirstOrDefault(a => a.Name == "CubeInstancing");
                    a1.Enable<ActivableEntityComponent>(false);
                    ClickBallModelEntity.Enable<ActivableEntityComponent>(false);
                    TerrainEditorView.CurrentEditorMode = EditorMode.InGameTerrain;
                    tcomp.ToggleVisible(false);
                    Entity a = Game.Services.GetService<SceneSystem>().SceneInstance
                        .FirstOrDefault(a => a.Name == "TerrainInGame");
                    if (a != null)
                    {
                        ImGuiSystem.UpdateTexture( TerrainEditorView.TerrainHeightMapTextureIntPtr,     tcomp.GetHeightmapTex());
                        //need to update since we might have changed the heightmap or blended texture
                        TerrainScriptInGame scr = a.Get<TerrainScriptInGame>();
                        scr.BuildTerrainComponent(tcomp, TerrainEditorView.TerrainWeights1, TerrainEditorView. TerrainWeights2);
                        AreaHandleModels.ToggleAreaModels(true);
                    }
                    Mannequin.Entity.Enable<ActivableEntityComponent>(true);
                    Character.Entity.Enable<ActivableEntityComponent>(true);
                    ResettingCamera = true;
                    TerrainTiles.ToggleWorldTiles(false, tcomp);
                }
                if (Input.IsKeyPressed(Keys.F3) &&
                    TerrainEditorView.CurrentEditorMode != EditorMode.InGameTiles)
                {
                    if (TerrainTiles.UniqueWorldTiles == null || TerrainTiles.UniqueWorldTiles.Count == 0)
                    {
                        TerrainEditorView.MSGlog.Add2Log("There are no unique world tiles! Make sure you load them first!");
                        return;
                    }
                    if (TerrainTiles.Tiles == null || TerrainTiles.Tiles.Count == 0)
                    {
                        TerrainEditorView.MSGlog.Add2Log("There are no world tiles created! Make sure you Generate the World based on rules first!");
                        return;
                    }
                    TerrainInGameGumps.Show_MiniMap = false;
                    TerrainTilesGumps.Show_WorldMap = true;
                    TerrainEditorView.CurrentEditorMode = EditorMode.InGameTiles;
                    CameraType = CameraType.TilesInGame;
                    CameraSpeed = 100;
                    Entity a = Game.Services.GetService<SceneSystem>().SceneInstance
                       .FirstOrDefault(a => a.Name == "TerrainInGame");
                    if (a != null)
                    {
                        TerrainScriptInGame scr = a.Get<TerrainScriptInGame>();
                        SceneSystem.SceneInstance.RootScene.Entities.Remove(
                            scr.TerrainModelEntity);
                    }
                    Mannequin.Entity.Enable<ActivableEntityComponent>(false);
                    Character.Entity.Enable<ActivableEntityComponent>(false);
                    Entity a1 = Game.Services.GetService<SceneSystem>().SceneInstance.
                        FirstOrDefault(a => a.Name == "CubeInstancing");
                    a1.Enable<ActivableEntityComponent>(false);
                    ResettingCamera = true;
                    tcomp.ToggleVisible(false);
                    AreaHandleModels.ToggleAreaModels(false);
                    //add world tiles here
                    TerrainTiles.InitializeWorldTiles(tcomp);
                    //cannot wait, got to fix the camera before ResettingCamera happens
                    MultiCamera.Entity.GetParent().Transform.Position = Vector3.Zero;
                    MultiCamera.Entity.GetParent().Transform.RotationEulerXYZ = Vector3.Zero;
                    TerrainTiles.Index_inWorld = new Int2(
                   TerrainTiles.m_NumTilesWideX / 2 ,
                   TerrainTiles.m_NumTilesHighZ / 2);
                    MultiCamera.Entity.Transform.Position =
                  TerrainTiles.Tiles[TerrainTiles.Index_inWorld.X +
                  TerrainTiles.m_NumTilesWideX *
                  TerrainTiles.Index_inWorld.Y].WorldPosition;
                    MultiCamera.Entity.Transform.Position.Y =
                        TerrainEditorView.InGameTiles_Camera_Height;
                    MultiCamera.Entity.Transform.Position.X += TerrainTiles.Width / 2;
                    MultiCamera.Entity.Transform.Position.Z += TerrainTiles.Height / 2;
                    MultiCamera.Entity.Transform.RotationEulerXYZ =
                         new Vector3(2.578f, 0.002f, 3.142f);
                   // new Vector3(1.943f, -0.001f, -3.142f);
                    //  TerrainTiles.ToggleWorldTiles(true, tcomp);
                }
                if (Input.IsKeyPressed(Keys.F8) &&
                    TerrainEditorView.CurrentEditorMode == EditorMode.InGameTerrain)
                {
                    CameraType = CameraType+1;
                    if(CameraType> CameraType.ThirdPerson)
                    {
                        CameraType = CameraType.FreeMovement;
                    }
                    if (CameraType == CameraType.FirstPerson)
                    {
                        Game.IsMouseVisible = false;
                        crosshair.Enabled = true;
                        Entity.SetParent(Character.Entity);
                        Entity.Transform.Position = new Vector3(0, 1.5f, -2);
                        Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                        return;
                    }
                    Game.IsMouseVisible = true;
                    crosshair.Enabled = false;
                    if (CameraType == CameraType.FreeMovement)
                    {
                        Game.IsMouseVisible = true;
                        crosshair.Enabled = false;
                        Entity TerrainInGame = Game.Services.GetService<SceneSystem>().SceneInstance
                            .FirstOrDefault(a => a.Name == "TerrainInGame");
                        Entity.SetParent(TerrainInGame);
                        Entity.Transform.Position = Character.Entity.Transform.Position
                            +VantagePoint3rdPerson;
                        Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                        return;
                    }
                    Entity CameraTarget = Game.Services.GetService<SceneSystem>().SceneInstance
                        .FirstOrDefault(a => a.Name == "CameraTarget");
                    Entity.SetParent(CameraTarget);
                    Entity.Transform.Position = new Vector3(0, 0, 5);
                    Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                    if (CameraType== CameraType.ThirdPerson)
                    {
                        DefaultDistance = 6;
                        Entity.GetParent().Transform.Position = new Vector3(0, 1.65f, 0);
                        Entity.GetParent().Transform.Rotation = new Quaternion(-0.16715722f,
                            -0.55716187f, -0.115674f, 0.8051389f);
                    }
                    if (CameraType == CameraType.Shoulder)
                    {
                        DefaultDistance = 1;
                    }
                }

            }
        }

        public override void Update()    
        {
            if (ResettingCamera)
            {
                ResetCamera();
                ResettingCamera = false;
            }
            UpdateModes();//always show switches between modes
            if (TerrainEditorView.CurrentEditorMode ==
                             EditorMode.InGameTerrain)
            {
                if (Input.HasKeyboard || Input.HasMouse)
                    AreaHandleModels.ToggleAreaModels(
                        Character.Entity.Transform.Position);
                //fix the camera depending on type               
                MinimapCamera.Entity.Transform.Position = Character.Entity.Transform.Position
                    + new Vector3(0, TerrainInGameGumps.MiniMapHeight, 0);   
                
                TerrainInGameGumps.MinimapTexture =
                    RenderToTexture.CurrentSprite.Texture;
                ImGuiSystem.UpdateTexture(TerrainInGameGumps.MinimapImageIntPtr, TerrainInGameGumps.MinimapTexture);

                if (CameraType == CameraType.FreeMovement)
                {
                    ProcessInputFree();
                    UpdateTransformFree();
                }
                else if (CameraType == CameraType.FirstPerson)
                {
                    UpdateCameraOrientationThirdPerson();
                    //                    UpdateCameraRaycastFirstPerson();
                }
                else if (CameraType == CameraType.ThirdPerson)
                {
                    UpdateCameraRaycastThirdPerson();
                    UpdateCameraOrientationThirdPerson();
                }
                else if (CameraType == CameraType.Isometric)
                {
                    Entity.GetParent().Transform.Position = new Vector3(-2, 3.65f, -2);
                    Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(2.29f, -0.73f, 3.14f);
                }
                else if (CameraType == CameraType.Shoulder)
                {
                    //    Entity.GetParent().Transform.Position = new Vector3(1, 0.65f, 1);
                    //   Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(2.29f, -0.73f, 3.14f);
                    //Entity.GetParent().Transform.Position = 
                    //    new Vector3(-2, 1.2f, -1);
                    UpdateCameraRaycastThirdPerson();
                    UpdateCameraOrientationThirdPerson();
                }
                if (Input.IsKeyPressed(Keys.R) && Input.IsKeyDown(Keys.LeftCtrl))
                {
                    ResettingCamera=true;
                }
                if (Input.IsKeyPressed(Keys.R))
                    TerrainInGameGumps.MiniMapHeight = 20;

                //this will capture the cursor inside the imgui gump
                if (TerrainInGameGumps.ImguiIO.WantCaptureMouse 
                    ||  TerrainInGameGumps.ImguiIO.WantCaptureKeyboard )
                {
                    return;
                }
                float targetDefaultDistance = DefaultDistance;
                if (Input.MouseWheelDelta != 0)
                {
                    //change modes, not the distance, jittery
                    if (Input.MouseWheelDelta > 0)//zoom in
                    {
                        if (CameraType == CameraType.Shoulder)
                        {
                            Game.IsMouseVisible = false;
                            CameraType = CameraType.FirstPerson;
                            crosshair.Enabled = true;
                            Entity.SetParent(Character.Entity);
                            Entity.Transform.Position = new Vector3(0, 1.5f, -2);
                            Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                            return;
                        }
                        if (CameraType == CameraType.ThirdPerson)
                        {
                            DefaultDistance = 1;
                            Game.IsMouseVisible = true;
                            CameraType = CameraType.Shoulder;
                            crosshair.Enabled = false;
                            Entity CameraTarget = Game.Services.GetService<SceneSystem>().SceneInstance
                                .FirstOrDefault(a => a.Name == "CameraTarget");
                            Entity.SetParent(CameraTarget);
                            Entity.Transform.Position = new Vector3(0, 0, 5);
                            Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                        }
                    }
                    else//zoom out
                    {
                        if (CameraType == CameraType.FirstPerson)
                        {
                            DefaultDistance = 1;
                            Game.IsMouseVisible = true;
                            CameraType = CameraType.Shoulder;
                            crosshair.Enabled = false;
                            Entity CameraTarget = Game.Services.GetService<SceneSystem>().SceneInstance
                                .FirstOrDefault(a => a.Name == "CameraTarget");
                            Entity.SetParent(CameraTarget);
                            Entity.Transform.Position = new Vector3(0, 0, 5);
                            Entity.Transform.RotationEulerXYZ = Vector3.Zero;
                            return;
                        }
                        if (CameraType == CameraType.Shoulder)
                        {
                            CameraType = CameraType.ThirdPerson; Entity.GetParent().Transform.Position = new Vector3(0, 1.65f, 0);
                            Entity.GetParent().Transform.Rotation = new Quaternion(-0.16715722f,
                                -0.55716187f, -0.115674f, 0.8051389f);
                            DefaultDistance = 6;
                        }
                    }
                }

            }

            if (TerrainEditorView.CurrentEditorMode ==
                            EditorMode.TerrainEditor)
            {
              //  if (TerrainEditorView.TerrainEditModeSelected != 0)
                {
                    if (Input.HasKeyboard || Input.HasMouse)
                        AreaHandleModels.ToggleAreaModels(
                            Entity.GetParent().Transform.Position
                            //Character.Entity.Transform.Position 
                            //Game.Services.GetService<SceneSystem>(). GraphicsCompositor.Cameras[0].Camera
                            );
                }
                if(Game.DrawTime.FrameCount%100==1)
                this.SpawnPrefabInstance(ClickEffect, null, 1.2f,Matrix.Scaling(10)* Matrix.RotationQuaternion(Quaternion.BetweenDirections(Vector3.UnitY, lastClickResult.HitResult.Normal)) * Matrix.Translation(lastClickResult.WorldPosition));
                if (TerrainEditorView.ImguiIO.WantCaptureMouse)
                {
                    return;
                }
                if (TerrainEditorView.ImguiIO.WantCaptureMouse 
                //    || TerrainEditorView.ImguiIO.WantCaptureKeyboard
                    )
                {
                    return;
                }
                ProcessInputTerrainEditor();
                UpdateCameraTerrainEditor();
            }

            if (TerrainEditorView.CurrentEditorMode ==
                 EditorMode.InGameTiles)
            {
                if (Input.HasKeyboard)
                {
                    //Cursor.Current = Cursors.WaitCursor;
                    TerrainTiles.UpdateWorldTiles(tcomp);
                   // Cursor.Current = Cursors.Default;
                    // Task t = Task.Run(() =>                    {                        TerrainTiles.UpdateWorldTiles(tcomp);                    });                    t.Wait();
                }
                ProcessInputInGameTiles();
                UpdateCameraInGameTiles(); 
            }

            if (TerrainEditorView.show_HelpGUI)
            {
                if (TerrainEditorView.CurrentEditorMode == EditorMode.TerrainEditor)
                {
                    int ypos = 30;// (int)(Game.GraphicsContext.CommandList.Viewport.Height/2.0f) ;// 30;
                    fpsStatStringBuilder.Clear();
                    fpsStatStringBuilder.AppendFormat("Frame: {0}, Update: {1:0.00}ms, Draw: {2:0.00}ms, FPS: {3:0.00}", Game.DrawTime.FrameCount, Game.UpdateTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.FramePerSecond);
                    DebugText.Print(fpsStatStringBuilder.ToString(), new Int2(10, ypos));
                    ypos += 15;
                    if (lastClickResult.HitResult.Succeeded)
                    {
                        DebugText.Print("Point Clicked: " + lastClickResult.WorldPosition.ToString("F1"),
                          new Int2(10, ypos));//, timeOnScreen: TimeSpan.FromSeconds(3));
                    }
                    else
                    {
                        DebugText.Print("Point Clicked: No Hit", new Int2(10, ypos));
                        // DebugText.Print("FPS: " + GameProfilingResults.Fps.ToString(),new Int2(10, 50));//, timeOnScreen: TimeSpan.FromSeconds(3));
                    }
                    ypos += 15;
                    DebugText.Print("W/A/S/D/Q/E to Move Forward/Backward/Left/Right/Up/Down", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Shift + F : Flatten all locations in the selection ball to the height of the ball center", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Middle MB or Right MB (and no left shift pressed) to Rotate Camera", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Shift + LMB/RMB/MMB to Increase/Decrease/Smooth Heights/Selected Texture Weights", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Shift + W/A/S/D/Q/E to Move Faster", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Right Ctrl + M: Hide/Show Main Menu", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Ctrl + R: Reset Camera Position", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Ctrl + C: Center Camera to Selection Ball Center", new Int2(10, ypos));
                }
                else if (TerrainEditorView.CurrentEditorMode == EditorMode.InGameTiles)
                {
                    int ypos = 30;// (int)(Game.GraphicsContext.CommandList.Viewport.Height/2.0f) ;// 30;
                    fpsStatStringBuilder.Clear();
                    fpsStatStringBuilder.AppendFormat("Frame: {0}, Update: {1:0.00}ms, Draw: {2:0.00}ms, FPS: {3:0.00}", Game.DrawTime.FrameCount, Game.UpdateTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.FramePerSecond);
                    DebugText.Print(fpsStatStringBuilder.ToString(), new Int2(10, ypos));
                    ypos += 15;
                    if (lastClickResult.HitResult.Succeeded)
                    {
                        DebugText.Print("Point Clicked: " + lastClickResult.WorldPosition.ToString("F1"),
                          new Int2(10, ypos));
                    }
                    else
                    {
                        DebugText.Print("Point Clicked: No Hit", new Int2(10, ypos));
                    }
                    ypos += 15;
                    DebugText.Print("W/A/S/D/Q/E to Move Forward/Backward/Left/Right/Up/Down", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Left Ctrl + R: Reset Camera Position", new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Camera Position=" + Entity.Transform.Position.ToString(), new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("World Tiles= (" + TerrainTiles.m_NumTilesWideX
                        + "x" + TerrainTiles.m_NumTilesHighZ + "), Total= "+
                        TerrainTiles.Tiles.Count, new Int2(10, ypos));
                    ypos += 15;
                    DebugText.Print("Tile Dims= (" + TerrainTiles.Width
                        + "x" + TerrainTiles.Height + ")", new Int2(10, ypos));
                    Int2 pos=new Int2(-1,1);
   //                 if(Input.HasKeyboard) pos = TerrainTiles.GetWorldTilePos(tcomp);
                    ypos += 15;
                    DebugText.Print("Current World Tile= (" +
                        TerrainTiles.Index_inWorld.X + "," +
                        TerrainTiles.Index_inWorld.Y + ")", new Int2(10, ypos));
                }
                else if(TerrainEditorView.CurrentEditorMode == EditorMode.InGameTerrain)
                {
                    //if (CameraType == CameraType.ThirdPerson)
                    {
                        int ypos = 30;// (int)(Game.GraphicsContext.CommandList.Viewport.Height/2.0f) ;// 30;
                        fpsStatStringBuilder.Clear();
                        fpsStatStringBuilder.AppendFormat("Frame: {0}, Update: {1:0.00}ms, Draw: {2:0.00}ms, FPS: {3:0.00}", Game.DrawTime.FrameCount, Game.UpdateTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.TimePerFrame.TotalMilliseconds, Game.DrawTime.FramePerSecond);
                        DebugText.Print(fpsStatStringBuilder.ToString(), new Int2(10, ypos));
                        ypos += 15;
                        if (lastClickResult.HitResult.Succeeded)
                        {
                            DebugText.Print("Point Clicked: " + lastClickResult.WorldPosition.ToString("F1"),
                              new Int2(10, ypos));//, timeOnScreen: TimeSpan.FromSeconds(3));
                        }
                        else
                        {
                            DebugText.Print("Point Clicked: No Hit", new Int2(10, ypos));
                            // DebugText.Print("FPS: " + GameProfilingResults.Fps.ToString(),new Int2(10, 50));//, timeOnScreen: TimeSpan.FromSeconds(3));
                        }
                        ypos += 15;
                        DebugText.Print("W/A/S/D/Space to Move Forward/Backward/Left/Right/Jump", new Int2(10, ypos));
                        ypos += 15;
                        DebugText.Print("X: Toggle Continuous Movement", new Int2(10, ypos));
                       /* ypos += 15;
                        DebugText.Print("Parent is: "+ Entity.GetParent().ToString(), new Int2(10, ypos));
                        ypos += 15;
                        DebugText.Print("CameraTarget Position=" + Entity.GetParent().Transform.Position.ToString(), new Int2(10, ypos));
                        ypos += 15;
                        DebugText.Print("CameraTarget RotationEulerXYZ =" + Entity.GetParent().Transform.RotationEulerXYZ.ToString(), new Int2(10, ypos));
                        ypos += 15;
                        DebugText.Print("PlayerCharacter Position=" + Character.Entity.Transform.Position.ToString(), new Int2(10, ypos));
                        ypos += 15;
                        DebugText.Print("Mannequin Position=" + Mannequin.Entity.Transform.Position.ToString(), new Int2(10, ypos));
                        ypos += 15;
                       */
                    }
                }
            }
        }
     
        static bool LMBDown = false;
        static bool RMBDown = false;
        static bool MMBDown = false;
        public static float CameraSpeed = 100;

        private void UpdateCameraRaycastFirstPerson()
        {
            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);
            if (InvertY) cameraMovement.Y *= -1;
            if (InvertX) cameraMovement.X *= -1;
            yaw -= cameraMovement.X * RotationSpeed;
            pitch = MathUtil.Clamp(pitch + cameraMovement.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
            var rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0);
            Entity.Transform.Rotation = rotation;
        }

        private void ProcessInputTerrainEditor()
        { 
            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            translation = Vector3.Zero;
            yaw = 0f;
            pitch = 0f;
            roll = 0;

            if (TerrainEditorView.CurrentEditorMode == 
                EditorMode.TerrainEditor)
            {
                // Keyboard and Gamepad based movement
                {
                    float speed = CameraSpeed * deltaTime;

                    Vector3 dir = Vector3.Zero;

                    if (Input.HasKeyboard)
                    {
                        if (Input.IsKeyDown(Keys.LeftShift) && 
                            Input.IsKeyPressed(Keys.F))
                        {
                            TerrainScript.FlattenLocations(tcomp,
                                lastClickResult.WorldPosition);
                            return;
                        }
                        if (Input.IsKeyPressed(Keys.M) && Input.IsKeyDown(Keys.RightCtrl))
                        {
                            TerrainEditorView.show_MainMenu = !TerrainEditorView.show_MainMenu;
                            return;
                        }
                        if (Input.IsKeyPressed(Keys.R) && Input.IsKeyDown(Keys.LeftCtrl))
                        {
                            ResettingCamera = true;
                            return;
                        }
                        if (Input.IsKeyPressed(Keys.C) && Input.IsKeyDown(Keys.LeftCtrl))
                        {                            
                            Entity.GetParent().Transform.Position =
                                new Vector3(ClickBallModelEntity.Transform.Position.X,
                                ClickBallModelEntity.Transform.Position.Y + 100,
                                ClickBallModelEntity.Transform.Position.Z+10);
                            Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(-1.345f, -0.122f, 0.03f);
                            return;
                        }
                        int incr = 1;
                        // Increase speed when pressing shift
                        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                        {
                            speed *= SpeedFactor;
                            incr = 10;
                        }
                        // Move with keyboard
                        // Forward/Backward
                        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
                        {
                            dir.Z += incr;
                        }
                        if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
                        {
                            dir.Z -= incr;
                        }

                        // Left/Right
                        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                        {
                            dir.X -= incr;
                        }
                        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                        {
                            dir.X += incr;
                        }

                        // Down/Up
                        if (Input.IsKeyDown(Keys.Q))
                        {
                            dir.Y -= incr;
                        }
                        if (Input.IsKeyDown(Keys.E))
                        {
                            dir.Y += incr;
                        }


                        // If the player pushes down two or more buttons, the direction and ultimately the base speed
                        // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
                        // normalizing the vector ensures that whichever direction the player chooses, that direction
                        // will always be at most one unit in length.
                        // We're keeping dir as is if isn't longer than one to retain sub unit movement:
                        // a stick not entirely pushed forward should make the entity move slower.
                        if (dir.Length() > 1f && incr == 1)
                        {
                            dir = Vector3.Normalize(dir);
                        }
                    }

                    // Finally, push all of that to the translation variable which will be used within UpdateTransform()
                    translation += dir * KeyboardMovementSpeed * speed;
                }

                // Keyboard and Gamepad based Rotation
                {
                    // See Keyboard & Gamepad translation's deltaTime usage
                    float speed = 1f * deltaTime;
                    Vector2 rotation = Vector2.Zero;
                    if (Gamepad && Input.HasGamePad)
                    {
                        GamePadState padState = Input.DefaultGamePad.State;
                        rotation.X += padState.RightThumb.Y;
                        rotation.Y += -padState.RightThumb.X;
                    }

                    if (Input.HasKeyboard)
                    {
                        if (Input.IsKeyDown(Keys.NumPad2))
                        {
                            rotation.X -= 1;
                        }
                        if (Input.IsKeyDown(Keys.NumPad8))
                        {
                            rotation.X += 1;
                        }

                        if (Input.IsKeyDown(Keys.NumPad4))
                        {
                            rotation.Y += 1;
                        }
                        if (Input.IsKeyDown(Keys.NumPad6))
                        {
                            rotation.Y -= 1;
                        }

                        // See Keyboard & Gamepad translation's Normalize() usage
                        if (rotation.Length() > 1f)
                        {
                            rotation = Vector2.Normalize(rotation);
                        }

                        if (Input.IsKeyDown(Keys.NumPad7))
                        {
                            roll += 1;
                        }
                        if (Input.IsKeyDown(Keys.NumPad9))
                        {
                            roll -= 1;
                        }
                    }

                    // Modulate by speed
                    rotation *= KeyboardRotationSpeed * speed;

                    // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
                    pitch += rotation.X;
                    yaw += rotation.Y;

                }

                // Mouse movement and gestures
                {
                    // This type of input should not use delta time at all, they already are frame-rate independent.
                    //    Lets say that you are going to move your finger/mouse for one second over 40 units, it doesn't matter
                    //    the amount of frames occuring within that time frame, each frame will receive the right amount of delta:
                    //    a quarter of a second -> 10 units, half a second -> 20 units, one second -> your 40 units.
                    if (Input.HasMouse)
                    {
                        if (Input.MouseWheelDelta != 0)
                        {
                            Vector3 dir = Vector3.Zero;
                            if (Input.MouseWheelDelta > 0) dir.Z += 1;
                            else dir.Z -= 1;
                            translation += dir * KeyboardMovementSpeed;
                        }
                        if (Input.IsMouseButtonDown(MouseButton.Left)) LMBDown = true;
                        if (Input.IsMouseButtonReleased(MouseButton.Left)) LMBDown = false;
                        if (Input.IsMouseButtonDown(MouseButton.Right)) RMBDown = true;
                        if (Input.IsMouseButtonReleased(MouseButton.Right)) RMBDown = false;
                        if (Input.IsMouseButtonDown(MouseButton.Middle)) MMBDown = true;
                        if (Input.IsMouseButtonReleased(MouseButton.Middle)) MMBDown = false;

                        MouseButton clickedtype = MouseButton.Left;
                        if (LMBDown) clickedtype = MouseButton.Left;
                        if (RMBDown) clickedtype = MouseButton.Right;
                        if (MMBDown) clickedtype = MouseButton.Middle;

                        if (!LMBDown && !RMBDown && !MMBDown)
                        {
                            if (!DoneEditing)
                            {
                                DoneEditing = true;
                                //update height map texture
                              //  Texture tex = tcomp.GetHeightmapTex();
                              //  tcomp.FullUpdate(tex);
                                ImGuiSystem.UpdateTexture(
                                    TerrainEditorView.TerrainHeightMapTextureIntPtr,
                                    tcomp.GetHeightmapTex());
                            }
                        }

                        if (LMBDown && !Input.IsKeyDown(Keys.LeftShift))
                        {
                            if (TerrainEditorView.TerrainLOD != 1)
                            {
                                TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                                TerrainEditorView.TerrainLOD = 1;
                                tcomp.TerrainLOD = 1;
                                tcomp.FullUpdateLOD(TerrainEditorView.TerrainWeights1, TerrainEditorView.TerrainWeights2);
                                return;
                            }

                            foreach (var pointerEvent in Input.PointerEvents)
                            {
                                lastClickResult.HitResult.Succeeded = false;
                                Ray ray = GetPickRay(pointerEvent.Position, new Vector2(
                                    Game.GraphicsContext.CommandList.Viewport.Width,
                                    Game.GraphicsContext.CommandList.Viewport.Height), MultiCamera);
                                if (tcomp.IntersectsRay(ray,out Vector3 point))
                                {
                                    lastClickResult.WorldPosition = point;
                                    lastClickResult.HitResult.Succeeded = true;
                                    ClickEffect.Entities.FirstOrDefault(e => e.Name == "CubeInstancing");
                                    this.SpawnPrefabInstance(ClickEffect, null, 1.2f, Matrix.RotationQuaternion(Quaternion.BetweenDirections(Vector3.UnitY, lastClickResult.HitResult.Normal)) * Matrix.Translation(lastClickResult.WorldPosition));
                                    ClickBallModelEntity.Transform.Position = lastClickResult.WorldPosition;
                                    ClickBallModelEntity.Transform.Scale = new Vector3(TerrainEditorView.Radius);
                                    Entity a = Game.Services.GetService<SceneSystem>().SceneInstance.
                                        FirstOrDefault(a => a.Name == "CubeInstancing");
                                    ModelComponent cube = a.Get<ModelComponent>();
                                    cube.Entity.Transform.Position = lastClickResult.WorldPosition;
                                }
                            }
                        }

                        if ((LMBDown || RMBDown || MMBDown) &&
                            Input.IsKeyDown(Keys.LeftShift))
                        {
                            if (TerrainEditorView.TerrainLOD != 1)
                            {
                                TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                                TerrainEditorView.TerrainLOD = 1;
                                tcomp.TerrainLOD = 1;
                                tcomp.FullUpdateLOD(TerrainEditorView.TerrainWeights1, TerrainEditorView.TerrainWeights2);
                                return;
                            }
                            DoneEditing = false;
                            foreach (var pointerEvent in Input.PointerEvents)
                            {
                                lastClickResult.HitResult.Succeeded = false;
                                Ray ray = GetPickRay(pointerEvent.Position, new Vector2(
                                    Game.GraphicsContext.CommandList.Viewport.Width,
                                    Game.GraphicsContext.CommandList.Viewport.Height), MultiCamera);
                                if (tcomp.IntersectsRay(ray, out Vector3 point))
                                {
                                    lastClickResult.WorldPosition = point;
                                    lastClickResult.HitResult.Succeeded = true;
                                    this.SpawnPrefabInstance(ClickEffect, null, 1.2f, Matrix.RotationQuaternion(Quaternion.BetweenDirections(Vector3.UnitY, lastClickResult.HitResult.Normal)) * Matrix.Translation(lastClickResult.WorldPosition));
                                    ClickBallModelEntity.Transform.Position = lastClickResult.WorldPosition;
                                    ClickBallModelEntity.Transform.Scale = new Vector3(TerrainEditorView.Radius);
                                    Entity a = Game.Services.GetService<SceneSystem>().SceneInstance.
                                        FirstOrDefault(a => a.Name == "CubeInstancing");
                                    ModelComponent cube = a.Get<ModelComponent>();
                                    cube.Entity.Transform.Position = lastClickResult.WorldPosition;
                                }
                            }
                            if (TerrainEditorView.TerrainEditModeSelected == 0)//edit locations
                                TerrainScript.ProcessLocationChange(tcomp, Game.GraphicsContext, GraphicsDevice, clickedtype, lastClickResult, deltaTime);
                            else if (TerrainEditorView.TerrainEditModeSelected == 1)//paint texture
                            {
                                if (TerrainEditorView.TerrainDisplayModeSelected == 0)
                                {
                                    TerrainScript.ProcessTexturesChange(tcomp, Game.GraphicsContext, GraphicsDevice, clickedtype, lastClickResult);
                                    return;
                                }
                                if (TerrainEditorView.TerrainDisplayModeSelected == 2)
                                {
                                    ProcessVertexColors(tcomp, clickedtype, lastClickResult, deltaTime);
                                    return;
                                }
                            }
                            else if (TerrainEditorView.TerrainEditModeSelected == 2)//add grass
                                AreaHandleModels.ProcessGrass(tcomp, clickedtype, lastClickResult);
                            else if (TerrainEditorView.TerrainEditModeSelected == 3)//add trees
                                AreaHandleModels.ProcessTrees(tcomp, clickedtype, lastClickResult);
                            else if (TerrainEditorView.TerrainEditModeSelected == 4)//add water
                                AreaHandleModels.ProcessWater(tcomp, clickedtype, lastClickResult);
                        }
                        // Rotate with mouse
                        if ((MMBDown||RMBDown) && !Input.IsKeyDown(Keys.LeftShift))
                        {
                            yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
                            pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
                        }
                    }

                }

             //   if(Input.HasKeyboard || Input.HasMouse)   
             //      AreaHandleModels.ToggleAreaModels(Game.Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera);

            }

        }
        bool DoneEditing=true;

        private void ProcessInputInGameTiles()
        {
            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            translation = Vector3.Zero;
            yaw = 0f;
            pitch = 0f;

            if (TerrainEditorView.CurrentEditorMode ==
                EditorMode.InGameTiles)
            {
                // Keyboard and Gamepad based movement
                {
                    float speed = CameraSpeed * deltaTime;

                    Vector3 dir = Vector3.Zero;

                    if (Input.HasKeyboard)
                    {
                        if (Input.IsKeyPressed(Keys.R) && Input.IsKeyDown(Keys.LeftCtrl))
                        {
                            ResettingCamera = true;
                            return;
                        } 
                        int incr = 1;
                        // Increase speed when pressing shift
                        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                        {
                            speed *= SpeedFactor;
                            incr = 10;
                        }
                        // Move with keyboard
                        // Forward/Backward
                        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
                        {
                            dir.Z += incr;
                        }
                        if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
                        {
                            dir.Z -= incr;
                        }

                        // Left/Right
                        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                        {
                            dir.X -= incr;
                        }
                        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                        {
                            dir.X += incr;
                        }

                        // Down/Up
                        if (Input.IsKeyDown(Keys.Q))
                        {
                            dir.Y -= incr;
                        }
                        if (Input.IsKeyDown(Keys.E))
                        {
                            dir.Y += incr;
                        }


                        // If the player pushes down two or more buttons, the direction and ultimately the base speed
                        // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
                        // normalizing the vector ensures that whichever direction the player chooses, that direction
                        // will always be at most one unit in length.
                        // We're keeping dir as is if isn't longer than one to retain sub unit movement:
                        // a stick not entirely pushed forward should make the entity move slower.
                        if (dir.Length() > 1f && incr == 1)
                        {
                            dir = Vector3.Normalize(dir);
                        }
                    }

                    // Finally, push all of that to the translation variable which will be used within UpdateTransform()
                    translation += dir * KeyboardMovementSpeed * speed;
                }

                // Keyboard and Gamepad based Rotation
                {
                    // See Keyboard & Gamepad translation's deltaTime usage
                    float speed = 1f * deltaTime;
                    Vector2 rotation = Vector2.Zero;
                    if (Gamepad && Input.HasGamePad)
                    {
                        GamePadState padState = Input.DefaultGamePad.State;
                        rotation.X += padState.RightThumb.Y;
                        rotation.Y += -padState.RightThumb.X;
                    }

                    if (Input.HasKeyboard)
                    {
                        if (Input.IsKeyDown(Keys.NumPad2))
                        {
                            rotation.X += 1;
                        }
                        if (Input.IsKeyDown(Keys.NumPad8))
                        {
                            rotation.X -= 1;
                        }

                        if (Input.IsKeyDown(Keys.NumPad4))
                        {
                            rotation.Y += 1;
                        }
                        if (Input.IsKeyDown(Keys.NumPad6))
                        {
                            rotation.Y -= 1;
                        }

                        // See Keyboard & Gamepad translation's Normalize() usage
                        if (rotation.Length() > 1f)
                        {
                            rotation = Vector2.Normalize(rotation);
                        }
                    }

                    // Modulate by speed
                    rotation *= KeyboardRotationSpeed * speed;

                    // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
                    pitch += rotation.X;
                    yaw += rotation.Y;

                }
                // Mouse movement and gestures
                {
                    if (Input.HasMouse)
                    {
                        // Rotate with mouse
                        if (Input.IsMouseButtonDown(MouseButton.Right))
                        {
                            yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
                            pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
                        }
                    }

                }
            }
        }

        private void UpdateCameraTerrainEditor()
        {
            // Get the local coordinate system
            var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

            // Enforce the global up-vector by adjusting the local x-axis
            var right = Vector3.Cross(rotation.Forward, upVector);
            var up = Vector3.Cross(right, rotation.Forward);

            // Stabilize
            right.Normalize();
            up.Normalize();

            // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
            var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, upVector));
            pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

            Vector3 finalTranslation = translation;
            finalTranslation.Z = -finalTranslation.Z;
            finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

            // Move in local coordinates
            Entity.Transform.Position += finalTranslation;

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) 
                * Quaternion.RotationAxis(upVector, yaw)
                * Quaternion.RotationYawPitchRoll(0,0,
                    MathUtil.DegreesToRadians(roll));
            //Entity.Transform.Rotation *=                Quaternion.RotationYawPitchRoll(                    MathUtil.DegreesToRadians(10 * yaw),                    MathUtil.DegreesToRadians(10 * pitch),                    MathUtil.DegreesToRadians(roll));
                //Quaternion.RotationY(MathUtil.DegreesToRadians(100*yaw));
        }

        private void ProcessInputFree()
        {
            translation = Vector3.Zero;
            yaw = 0;
            pitch = 0;
            roll = 0;

            // Move with keyboard
            if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
            {
                translation.Z = -KeyboardMovementSpeed.Z;
            }
            else if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
            {
                translation.Z = KeyboardMovementSpeed.Z;
            }

            if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
            {
                translation.X = -KeyboardMovementSpeed.X;
            }
            else if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
            {
                translation.X = KeyboardMovementSpeed.X;
            }

            if (Input.IsKeyDown(Keys.Q))
            {
                translation.Y = -KeyboardMovementSpeed.Y;
            }
            else if (Input.IsKeyDown(Keys.E))
            {
                translation.Y = KeyboardMovementSpeed.Y;
            }

            // Alternative translation speed
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            {
                translation *= SpeedFactor;
            }

            // Rotate with keyboard
            if (Input.IsKeyDown(Keys.NumPad2))
            {
                pitch = -KeyboardRotationSpeed.X;
            }
            else if (Input.IsKeyDown(Keys.NumPad8))
            {
                pitch = KeyboardRotationSpeed.X;
            }

            if (Input.IsKeyDown(Keys.NumPad4))
            {
                yaw = KeyboardRotationSpeed.Y;
            }
            else if (Input.IsKeyDown(Keys.NumPad6))
            {
                yaw = -KeyboardRotationSpeed.Y;
            }

            // Rotate with mouse
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                yaw = -Input.MouseDelta.X * MouseRotationSpeed.X * SpeedFactor;
                pitch = -Input.MouseDelta.Y * MouseRotationSpeed.Y * SpeedFactor;
            }

        }

        private void UpdateCameraInGameTiles()
        {
            // Get the local coordinate system
            var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

            // Enforce the global up-vector by adjusting the local x-axis
            var right = Vector3.Cross(rotation.Forward, upVector);
            var up = Vector3.Cross(right, rotation.Forward);

            // Stabilize
            right.Normalize();
            up.Normalize();

            // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
            var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, upVector));
            pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

            Vector3 finalTranslation = translation;
            finalTranslation.Z = -finalTranslation.Z;
            finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

            // Move in local coordinates
            Entity.Transform.Position += finalTranslation;

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
        }

        private void UpdateTransformFree()
        {
            var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            translation *= elapsedTime;
            yaw *= elapsedTime;
            pitch *= elapsedTime;

            // Get the local coordinate system
            var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

            // Enforce the global up-vector by adjusting the local x-axis
            var right = Vector3.Cross(rotation.Forward, upVector);
            var up = Vector3.Cross(right, rotation.Forward);

            // Stabilize
            right.Normalize();
            up.Normalize();

            // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
            var currentPitch = MathUtil.PiOverTwo - (float)Math.Acos(Vector3.Dot(rotation.Forward, upVector));
            pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

            // Move in local coordinates
            Entity.Transform.Position += Vector3.TransformCoordinate(translation, rotation);

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
        }

        #region 3rd person

        /// <summary>
        /// Raycast between the camera and its target. The script assumes the camera is a child entity of its target.
        /// </summary>
        private void UpdateCameraRaycastThirdPerson()
        {
            var maxLength = DefaultDistance;
            var cameraVector = new Vector3(0, 0, DefaultDistance);
            Entity.GetParent().Transform.Rotation.Rotate(ref cameraVector);

            if (ConeRadius <= 0)
            {
                // If the cone radius
                var raycastStart = Entity.GetParent().Transform.WorldMatrix.TranslationVector;
                var hitResult = this.GetSimulation().Raycast(raycastStart, raycastStart + cameraVector);
                if (hitResult.Succeeded)
                {
                    maxLength = Math.Min(DefaultDistance, (raycastStart - hitResult.Point).Length());
                }
            }
            else
            {
                // If the cone radius is > 0 we will sweep an actual cone and see where it collides
                var fromMatrix = Matrix.Translation(0, 0, -DefaultDistance * 0.5f) *
                                 Entity.GetParent().Transform.WorldMatrix;
                var toMatrix = Matrix.Translation(0, 0, DefaultDistance * 0.5f) *
                                 Entity.GetParent().Transform.WorldMatrix;

                resultsOutput.Clear();
                var cfg = CollisionFilterGroups.DefaultFilter;
                var cfgf = CollisionFilterGroupFlags.DefaultFilter; // Intentionally ignoring the CollisionFilterGroupFlags.StaticFilter; to avoid collision with poles

                this.GetSimulation().ShapeSweepPenetrating(coneShape, fromMatrix, toMatrix, resultsOutput, cfg, cfgf);

                foreach (var result in resultsOutput)
                {
                    if (result.Succeeded)
                    {
                        var signedVector = result.Point - Entity.GetParent().Transform.WorldMatrix.TranslationVector;
                        var signedDistance = Vector3.Dot(cameraVector, signedVector);

                        var currentLength = DefaultDistance * result.HitFraction;
                        if (signedDistance > 0 && currentLength < maxLength)
                            maxLength = currentLength;
                    }
                }
            }

            if (maxLength < MinimumDistance)
                maxLength = MinimumDistance;

            Entity.Transform.Position.Z = maxLength;
        }

        
        private void UpdateCameraOrientationShoulder()
        {
            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);

            if (InvertY) cameraMovement.Y *= -1;
            targetRotationXYZ.X += cameraMovement.Y * VerticalSpeed;
            targetRotationXYZ.X = Math.Max(targetRotationXYZ.X, -MaxVerticalAngle);
            targetRotationXYZ.X = Math.Min(targetRotationXYZ.X, -MinVerticalAngle);

            if (InvertX) cameraMovement.X *= -1;
            targetRotationXYZ.Y -= cameraMovement.X * RotationSpeed;

            // Very simple lerp to allow smoother transition of the camera towards its desired destination. You can change this behavior with a different one, better suited for your game.
            cameraRotationXYZ = Vector3.Lerp(cameraRotationXYZ, targetRotationXYZ, 0.25f);
            Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(MathUtil.DegreesToRadians(cameraRotationXYZ.X), MathUtil.DegreesToRadians(cameraRotationXYZ.Y), 0);
        }

        /// <summary>
        /// Raycast between the camera and its target. The script assumes the camera is a child entity of its target.
        /// </summary>
        private void UpdateCameraOrientationThirdPerson()
        {
            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);

            if (InvertY) cameraMovement.Y *= -1;
            targetRotationXYZ.X += cameraMovement.Y * VerticalSpeed;
            targetRotationXYZ.X = Math.Max(targetRotationXYZ.X, -MaxVerticalAngle);
            targetRotationXYZ.X = Math.Min(targetRotationXYZ.X, -MinVerticalAngle);

            if (InvertX) cameraMovement.X *= -1;
            targetRotationXYZ.Y -= cameraMovement.X * RotationSpeed;

            // Very simple lerp to allow smoother transition of the camera towards its desired destination. You can change this behavior with a different one, better suited for your game.
            cameraRotationXYZ = Vector3.Lerp(cameraRotationXYZ, targetRotationXYZ, 0.25f);
            Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(MathUtil.DegreesToRadians(cameraRotationXYZ.X), MathUtil.DegreesToRadians(cameraRotationXYZ.Y), 0);
        }
        #endregion 3rd person

    }

    [ComponentCategory("Utils")]
    [DataContract("SmoothFollowAndRotate")]
    public class SmoothFollowAndRotate : SyncScript
    {
        [DataMember(0)]
        public bool Enabled = true;
        public Entity EntityToFollow { get; set; }
        public float Speed { get; set; } = 1;

        public override void Update()
        {
            if (TerrainEditorView.CurrentEditorMode !=
                EditorMode.InGameTerrain) return;
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            var currentPosition = Entity.Transform.Position;
            var currentRotation = Entity.Transform.Rotation;

            var lerpSpeed = 1f - MathF.Exp(-Speed * deltaTime);

            EntityToFollow.Transform.GetWorldTransformation(out var otherPosition, out var otherRotation, out var _);

            var newPosition = Vector3.Lerp(currentPosition, otherPosition
                +MultiTypeCameraController.VantagePoint3rdPerson, lerpSpeed);
            Entity.Transform.Position = newPosition;

            Quaternion.Slerp(ref currentRotation, ref otherRotation, lerpSpeed, out var newRotation);
            Entity.Transform.Rotation = newRotation;
        }
    }
}


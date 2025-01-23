//by Idomeneas
using HeightMapEditor;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using System.Linq;


namespace TerrainEditor
{
    //all basic water planes share the same type script here
    //any new plane with new variables automatically makes existing water planes
    //in the scene have the same variable values
    public class WaterScript : ScriptComponent
    {
        /// <summary>
        /// Enables or disables water
        /// </summary>
        [DataMember(10)]
        public bool Enabled = true;

        public int SizeX, SizeY;
        public int TesselationX;
        public int TesselationY;
        public float WaterTransparency = 0.85f;
        public float DisplacementSpeed = 0.25f;
        public float DisplacementAmplitude = 0.15f;
        public float TextureScale=10;
        public bool UseCaustics=true;
        
        // Water fields
        public Vector2 SkyTextureOffset;
        public TextureOffsets NormalTextureFlow;
        public TextureOffsets DiffuseTextureFlow;
        
        // Rendering properties
        public Matrix World;
        public Color3 SunColor = Color.White.ToColor3();
        public CameraComponent Camera;
        public GeometricPrimitive SurfaceGeometry;
        
        private const float SkyScrollSpeed = 0.01f;
        public void Initialize() { }

        public void Setup(SceneSystem SceneSystem) 
        {
            //         Camera = SceneSystem.TryGetMainCamera();
            Entity CameraMultiType = SceneSystem.SceneInstance
                .FirstOrDefault(a => a.Name == "CameraMultiType");
            Camera = CameraMultiType.Get<CameraComponent>();

            SkyTextureOffset = new Vector2(0.0f, 0.0f);
            NormalTextureFlow = new TextureOffsets(0.4f, 0.5f, 10.5f, 0.5f);
            DiffuseTextureFlow = new TextureOffsets(0.4f, 0.5f, 10.0f, 0.5f);
        }

        public void Update()
        {
            var ellapsedSeconds = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            SkyTextureOffset.X = (SkyTextureOffset.X + SkyScrollSpeed * ellapsedSeconds) % 1.0f;
            NormalTextureFlow.Update(ellapsedSeconds);
            DiffuseTextureFlow.Update(ellapsedSeconds);
        }

    }
}

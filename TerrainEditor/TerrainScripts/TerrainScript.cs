//by Idomeneas
using HeightMapEditor;
using ImGui;
using SinglePassWireframe;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using CommandList = Stride.Graphics.CommandList;

namespace TerrainEditor
{
    /// <summary>
    /// The terraincomponent is very slow to constantly update
    /// e.g., recreate the mesh every time there is a vertex change
    /// we end up doing it on the GPU instead (shader)
    /// passing the texture heightmap and other textures for color blending. 
    /// The terrain mesh vertices passed to the shader are just a plane... 
    /// The heights are changed in the shader based on the heightmap texture 
    /// we pass dynamically
    /// </summary>
    public class TerrainScript : StartupScript
    {
        [DataMember(0)]
        public bool Enabled = true;

     //   public static Entity Terrain_Entity;
        public static bool[] SelectedPoints;
        
        public static TerrainComponent GetTerrainEntity(Game Game)
        {
            Entity Terrain_Entity = 
                Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            TerrainComponent tcomp = 
                Terrain_Entity.GetOrCreate<TerrainComponent>();
            return tcomp;
        }

        public override void Start()
        {
            if(!Enabled)return;
            Entity Terrain_Entity = Entity;
         //   SpriteFont arial = Content.Load<SpriteFont>("OpenSans-font");
            TerrainComponent tcomp = Terrain_Entity.Get<TerrainComponent>();
            if (tcomp == null) throw new Exception("Terrain component not created yet...");
            Texture Heightmaptexture = GetHeightMapTexture();// ImGuiSystem._loadedTextures[TerrainEditorView.TerrainHeightMapTextureIntPtr];
            TerrainEditorView.RenderMesh = true;
            SelectedPoints = new bool[Heightmaptexture.Width * Heightmaptexture.Height];
            
            //tcomp.Hidden = true;
          //  tcomp.NeedsUpdating = false;

            BuildTerrainComponent(tcomp);

            if (tcomp.VegetationModel1 == null && tcomp.VegetationModel2 == null &&
                tcomp.VegetationModel3 == null)
            {
                TerrainEditorView.MaxTreeTypes = 0;
                TerrainEditorView.MSGlog.Add2Log("There are no vegetation models! Make sure you add some in the stride studio terrain component!");
                return;
            }

            Entity minimap = Game.Services.GetService<SceneSystem>().SceneInstance
                .FirstOrDefault(a => a.Name == "MinimapCamera");
            CameraComponent cameraminimap = minimap.Get<CameraComponent>();
            cameraminimap.Enabled = true;

            AreaXML.TerrainHeightMap = "InGameTerrain/HeightMap";
            AreaXML.TerrainBlendedTexture = "InGameTerrain/BlendedTexture";
            AreaXML.LoadArea(tcomp);
            TerrainEditorView.MSGlog.Add2Log("Loaded Area " + AreaXML.AreaName + " from directory " +
                 Utility.Resources_TerrainEditorAreas_Directory + @"\" + AreaXML.AreaName);

            //   Thread.Sleep(1000);
            UpdateTerrainComp(tcomp);
           // tcomp.FullUpdate(TerrainScript.GetHeightMapTexture());

            if (TerrainEditorView.TerrainEditModeSelected == 0)
            {
                TerrainEditorView.ShowTrees = false;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, TerrainEditorView.ShowTrees);
                TerrainEditorView.ShowGrass = false;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, TerrainEditorView.ShowGrass);
                TerrainEditorView.ShowWater = false;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Water, TerrainEditorView.ShowWater);
                TerrainEditorView.MSGlog.Add2Log("All area objects are hidden. Also, don't forget to increase the visibility variable if you don't see them...");
            }
        }

        /// <summary>
        /// in case the heightmap texture is not the same as the one 
        /// from the XML area we just loaded, update it
        /// </summary>
        /// <param name="tcomp"></param>
        public async void UpdateTerrainComp(TerrainComponent tcomp)
        {
            // whatever you need to do before delay goes here         
            await Task.Delay(500);
            // whatever you need to do after delay.
            tcomp.FullUpdate(TerrainScript.GetHeightMapTexture(),
               TerrainEditorView.TerrainWeights1, TerrainEditorView.TerrainWeights2);
            TerrainEditorView.TerrainBlendedTexture = TerrainScript.GetBlendedTexture(tcomp, tcomp.Game.GraphicsContext, GraphicsDevice);
            TerrainEditorView.TerrainBlendedTextureIntPtr = ImGuiSystem.BindTexture(TerrainEditorView.TerrainBlendedTexture);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
        }

        public void BuildTerrainComponent(TerrainComponent tcomp)
        {
            if (TerrainEditorView.TerrainDisplayModeSelected == 2)
            {
                tcomp.TEXTURE_REPEAT = 0;
                TerrainEditorView.texrepeat = 0;
            }                
            tcomp.UpdateHeightMapColors(GetHeightMapTexture());
            Entity TerrainEnt = new Entity("TerrainComponentModelComponent");
            TerrainEnt.Add(new WireframeScript());            
            tcomp.TerrainEntity = TerrainEnt;
            SceneSystem.SceneInstance.RootScene.Entities.Add(TerrainEnt);
            //need to pass the weight textures
            tcomp.VertexCPUBuffer = tcomp.GenerateVertices(
               TerrainEditorView.TerrainWeights1, TerrainEditorView.TerrainWeights2);
            var indices = tcomp.GenerateIndices();
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                GraphicsDevice, tcomp.VertexCPUBuffer.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            tcomp.VertexGPUBuffer = vertexBuffer;
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(tcomp.VertexGPUBuffer, VertexTypePosTexNormColor.Layout, tcomp.VertexGPUBuffer.ElementCount) },
                },
                MaterialIndex = 0,
            };
            var model = new Model();
            model.Meshes.Add(mesh);
            if(model.Materials!=null)
                model.Materials.Clear();
            var comp = TerrainEnt.GetOrCreate<ModelComponent>();
            comp.Model = model;
            Material material = Content.Load<Material>("Terrain/TerrainObjectMaterialShader");
            tcomp.MaterialBlendSingle = material;
            //DO NOT RECREATE THE MATERIAL WITH THE SHADER REFERENCE, MESSES THINGS UP
            //LOAD ALL SHADER TEXTURES
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TexturingKeys.Sampler, GraphicsDevice.SamplerStates.LinearWrap);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_1, GetTerrainTexture(1));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_2, GetTerrainTexture(2));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_3, GetTerrainTexture(3));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_4, GetTerrainTexture(4));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_5, GetTerrainTexture(5));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_6, GetTerrainTexture(6));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_7, GetTerrainTexture(7));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_8, GetTerrainTexture(8));
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.TextureRepeat, TerrainEditorView.texrepeat);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.DetailMappingDistance, TerrainEditorView.DetailMappingDistance);
            tcomp.ToggleVisible(true);
            //          TerrainEnt.Enable<ActivableEntityComponent>(true);
            // ImGuiSystem.UpdateTexture(                TerrainEditorView.TerrainHeightMapTextureIntPtr,                tcomp.GetHeightmapTex());
            material = Content.Load<Material>(
                "Terrain/TerrainHeightMaterialShader"
              //  "Terrain/BlendHeightTerrainMaterial"
                ); 
            tcomp.MaterialBlendHeight = material;
            Texture bump1 = Content.Load<Texture>("Textures/BumpMaps/bumpmap1");
            Texture bump2 = Content.Load<Texture>("Textures/BumpMaps/bumpmap2");
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(
              TerrainHeightShaderKeys.BumpMap_1, bump1);
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.BumpMap_2, bump2);
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_1, GetHeightTerrainTexture(1));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_2, GetHeightTerrainTexture(2));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_3, GetHeightTerrainTexture(3));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_4, GetHeightTerrainTexture(4));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_5, GetHeightTerrainTexture(5));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_6, GetHeightTerrainTexture(6));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_7, GetHeightTerrainTexture(7));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_8, GetHeightTerrainTexture(8));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_9, GetHeightTerrainTexture(9));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_10, GetHeightTerrainTexture(10));
            Texture detail = ImGuiSystem._loadedTextures[TerrainEditorView.TerrainDetaiMapTextureIntPtr];
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DetailTex, detail);
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.HeightMap, tcomp.HeightMapColors.ToTexture(tcomp.Width, tcomp.Height,
            GraphicsDevice, Game.GraphicsContext.CommandList));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.HeightRange, new Vector2(tcomp.HeightRange.X, tcomp.HeightRange.Y));
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.SlopeCutoff, TerrainEditorView.SlopeCutoff);
            tcomp.SlopeCutoff = TerrainEditorView.SlopeCutoff;
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DetailMappingDistance, TerrainEditorView.DetailMappingDistance);
            tcomp.DetailMappingDistance = TerrainEditorView.DetailMappingDistance;
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DistanceMultiplier, TerrainEditorView.DistanceMultiplier);
            tcomp.DistanceMultiplier = TerrainEditorView.DistanceMultiplier;
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.TextureRepeat, TerrainEditorView.texrepeat);
            float[] HeightLevels = new float[]{0.1f,0.2f,0.3f,0.4f,0.5f,
             0.6f,0.7f,0.8f,0.9f,1.0f};
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.HeightLevels, HeightLevels);
            Texture Slope1 = Content.Load<Texture>("Materials/TerrainHeightBased/rock_wall_02_diff_1k");
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(
                TerrainHeightShaderKeys.SlopeTex_1, Slope1);
            Texture Slope2 = Content.Load<Texture>("Textures/Walls/stonewall2_d");
            tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(
            TerrainHeightShaderKeys.SlopeTex_2, Slope2);

            material = Content.Load<Material>("Terrain/TerrainMultiBlendMaterialShader");
            tcomp.MaterialBlendMulti = material;
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_1, GetTerrainTexture(1));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_2, GetTerrainTexture(2));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_3, GetTerrainTexture(3));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_4, GetTerrainTexture(4));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_5, GetTerrainTexture(5));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_6, GetTerrainTexture(6));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_7, GetTerrainTexture(7));
            tcomp.MaterialBlendMulti.Passes[0].Parameters.Set(TerrainMultiBlendShaderKeys.Texture_8, GetTerrainTexture(8));
        
            if (TerrainEditorView.TerrainDisplayModeSelected == 0)
            {
                tcomp.CurrentMaterialName = "MaterialBlendSingle";
                model.Materials.Add(tcomp.MaterialBlendSingle);
            }
            else
               if (TerrainEditorView.TerrainDisplayModeSelected == 1)
            {
                tcomp.CurrentMaterialName = "MaterialBlendHeight";
                model.Materials.Add(tcomp.MaterialBlendHeight);
            }
            else
               if (TerrainEditorView.TerrainDisplayModeSelected == 2)
            {
                tcomp.CurrentMaterialName = "MaterialBlendMulti";
                model.Materials.Add(tcomp.MaterialBlendMulti);
            }
        }

        public static Ray GetPickRay(Vector2 screenPos, Vector2 screenDims, CameraComponent camera)
        {
            Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            // Reconstruct the projection-space position in the (-1, +1) range.
            //    Don't forget that Y is down in screen coordinates, but up in projection space
            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            // Compute the near (start) point for the raycast
            // It's assumed to have the same projection space (x,y) coordinates and z = 0 (lying on the near plane)
            // We need to unproject it to world space
            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            // Compute the far (end) point for the raycast
            // It's assumed to have the same projection space (x,y) coordinates and z = 1 (lying on the far plane)
            // We need to unproject it to world space
            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;
            Vector3 dir = vectorFar.XYZ() - vectorNear.XYZ();
            dir.Normalize();
            Ray ray = new Ray(vectorNear.XYZ(), dir);
            return ray;
        }

        public static void FlattenLocations(TerrainComponent tcomp, 
            Vector3 WorldPosition)
        {
            int Width = tcomp.Width,
                Height = tcomp.Height;
            SelectedPoints = new bool[Width * Height];
            int i, j, index;
            float radius = TerrainEditorView.Radius,// PerlinMenuCode.GetRadius(),
                  pow = TerrainEditorView.BallSelectionPower;// PerlinMenuCode.GetPower();
            int minx = (int)Math.Max(0, WorldPosition.X - radius),
                minz = (int)Math.Max(0, WorldPosition.Z - radius),
                maxx = (int)Math.Min(Width, WorldPosition.X + radius),
                maxz = (int)Math.Min(Height, WorldPosition.Z + radius);
            for (j = minz; j < maxz; j++)
            {
                for (i = minx; i < maxx; i++)
                {
                    index = (Width * j) + i;
                    Int2 pos = new Int2(i, j);
                    float ht = tcomp.//GetHeightAt(i,j);//
                             GetCPUHeightAt(pos);
                 //   float ht1 = tcomp.GetHeightAt(i, j);
                    SelectedPoints[index] = false;
                    if (Vector3.Distance(WorldPosition,//.XZ(),
                        new Vector3(tcomp.m_QuadSideWidthX * i, ht,
                        tcomp.m_QuadSideWidthZ * j)) <= radius)
                    {
                        SelectedPoints[index] = true;
                        tcomp.SetVertexHeight(pos, WorldPosition.Y);
                    }
                }
            }
            CubeInstancingRenderScript.UpdatingMode = 2;
        }

        public static void ProcessLocationChange(
            TerrainComponent tcomp, GraphicsContext GraphicsContext,
            GraphicsDevice GraphicsDevice, MouseButton button,
            ClickResult clickResult, float delta)
        {
            int Width = tcomp.Width,
                Height = tcomp.Height;
            SelectedPoints = new bool[Width * Height];
            int i, j, index;
            float radius = TerrainEditorView.Radius,// PerlinMenuCode.GetRadius(),
                  pow = TerrainEditorView.BallSelectionPower;// PerlinMenuCode.GetPower();
            int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                maxx = (int)Math.Min(Width, clickResult.WorldPosition.X + radius),
                maxz = (int)Math.Min(Height, clickResult.WorldPosition.Z + radius);
            for (j = minz; j < maxz; j++)
            {
                for (i = minx; i < maxx; i++)
                {
                    index = (Width * j) + i;
                    Int2 pos = new Int2(i, j);
                    float ht = tcomp.GetCPUHeightAt(pos);
                    SelectedPoints[index] = false;
                    if (Vector3.Distance(clickResult.WorldPosition,
                        new Vector3(tcomp.m_QuadSideWidthX * i, ht,
                        tcomp.m_QuadSideWidthZ * j)) <= radius
                        && Utility.Runif() < TerrainEditorView.BallSelectionStrength)
                    {
                        SelectedPoints[index] = true;
                        float offset = 0;
                        if (button == MouseButton.Left)
                        {
                            offset = Utility.Runif(0.1f, 0.5f) * pow * delta;
                            if (ht + offset > tcomp.HeightRange.Y)
                                offset = 0;
                        }
                        else if (button == MouseButton.Right)
                        {
                            offset = -Utility.Runif(0.1f, 0.5f) * pow * delta;
                            if (ht + offset < tcomp.HeightRange.X)
                                offset = 0;
                        }
                        else if (button == MouseButton.Middle)
                        {//smooth
                            ht = 0;
                            int num = 0;
                            if (tcomp.IsValidCoordinate(i - 1, j - 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j - 1)) + i - 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i - 1, j))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * j) + i - 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i - 1, j + 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j + 1)) + i - 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i + 1, j - 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j - 1)) + i + 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i + 1, j))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * j) + i + 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i + 1, j + 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j + 1)) + i + 1].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i, j + 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j + 1)) + i].Position.Y;
                            }
                            if (tcomp.IsValidCoordinate(i, j - 1))
                            {
                                num++;
                                ht += 10.0f * tcomp.VertexCPUBuffer[(Width * (j - 1)) + i].Position.Y;
                            }
                            ht /= 10.0f*num;
                            if (ht < tcomp.HeightRange.X)
                                ht = tcomp.HeightRange.X;
                            if (ht > tcomp.HeightRange.Y)
                                ht = tcomp.HeightRange.Y;
                        }
                        tcomp.SetVertexHeight(pos, ht + offset);
                    }
                }
            }
            CubeInstancingRenderScript.UpdatingMode = 2;
        }

        /// <summary>
        /// This one is for single display mode that creates the weight textures.
        /// not allowed to paint terrain on height based display mode
        /// </summary>
        public static void ProcessTexturesChange(TerrainComponent tcomp,
            GraphicsContext GraphicsContext,GraphicsDevice GraphicsDevice, 
            MouseButton button, ClickResult clickResult)
        {
            if (TerrainEditorView.TerrainDisplayModeSelected == 1)
            {
                TerrainEditorView.MSGlog.Add2Log("Cannot paint texture when using the height based shader...");
                return;
            }
            SelectedPoints = new bool[tcomp.Width * tcomp.Height];
            if (button == MouseButton.Left)//increase weights for selected texture
            {
                int i, j, //index,
                    index_in_tex;
                float radius = TerrainEditorView.Radius,// PerlinMenuCode.GetRadius(),
                      pow = TerrainEditorView.BallSelectionPower;// PerlinMenuCode.GetPower();
                Color[] ColorValues = new Color[TerrainEditorView.TerrainWeights1.Width * TerrainEditorView.TerrainWeights1.Height];
                // Get the height information and put it in the array
                if (TerrainEditorView.Selectedtexture < 4)
                    ColorValues = TerrainEditorView.TerrainWeights1.GetColorData(
                        GraphicsContext);
                else
                    ColorValues = TerrainEditorView.TerrainWeights2.GetColorData(
                        GraphicsContext);
                int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                    minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                    maxx = (int)Math.Min(tcomp.Width, clickResult.WorldPosition.X + radius),
                    maxz = (int)Math.Min(tcomp.Height, clickResult.WorldPosition.Z + radius);
                for (i = minx; i < maxx; i++)
                {
                    for (j = minz; j < maxz; j++)
                    {
                        // index = (tcomp.Width * j) + i;
                        index_in_tex = (tcomp.Width * //(tcomp.Height-j)
                            j) + i;
                        float height = tcomp.GetCPUHeightAt(i, j);
                        SelectedPoints[index_in_tex] = false;
                        if (Vector3.Distance(clickResult.WorldPosition,
                            new Vector3(tcomp.m_QuadSideWidthX * i, height,
                            tcomp.m_QuadSideWidthZ * j)) <= radius
                            && Utility.RandomFloat() < TerrainEditorView.BallSelectionStrength)
                        {
                            SelectedPoints[index_in_tex] = true;
                            if (TerrainEditorView.Selectedtexture == 0 ||
                                TerrainEditorView.Selectedtexture == 4)
                                ColorValues[index_in_tex].R = (byte)Utility.BoundValue0255(
                            MathF.Round(Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].R));
                            else if (TerrainEditorView.Selectedtexture == 1 ||
                                TerrainEditorView.Selectedtexture == 5)
                                ColorValues[index_in_tex].G = (byte)Utility.BoundValue0255(
                            MathF.Round(Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].G));
                            else if (TerrainEditorView.Selectedtexture == 2 ||
                                TerrainEditorView.Selectedtexture == 6)
                                ColorValues[index_in_tex].B = (byte)Utility.BoundValue0255(
                            MathF.Round(Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].B));
                            else if (TerrainEditorView.Selectedtexture == 3 ||
                                TerrainEditorView.Selectedtexture == 7)
                                ColorValues[index_in_tex].A = (byte)Utility.BoundValue0255(
                            MathF.Round(Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].A));
                        }
                    }
                }
                if (TerrainEditorView.Selectedtexture < 4)
                {
                    TerrainEditorView.TerrainWeights1 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights1.Width,
                        TerrainEditorView.TerrainWeights1.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //     .SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr,
                        TerrainEditorView.TerrainWeights1);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
                }
                else
                {
                    TerrainEditorView.TerrainWeights2 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights2.Width,
                        TerrainEditorView.TerrainWeights2.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //.SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr,
                        TerrainEditorView.TerrainWeights2);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
                }
                Array.Clear(ColorValues, 0, ColorValues.Length);
                // tcomp.Material.Passes[0].Parameters.Set(MaterialKeys.BlendMap, TerrainEditorView.TerrainWeights1);
                //        tcomp.NeedsUpdating = true;//dont need it
            }
            if (button == MouseButton.Right)//decrease weights
            {
                int i, j, //index,
                    index_in_tex;
                float radius = TerrainEditorView.Radius,// PerlinMenuCode.GetRadius(),
                      pow = TerrainEditorView.BallSelectionPower;// PerlinMenuCode.GetPower();
                Color[] ColorValues = new Color[TerrainEditorView.TerrainWeights1.Width * TerrainEditorView.TerrainWeights1.Height];
                // Get the height information and put it in the array
                if (TerrainEditorView.Selectedtexture < 4)
                    ColorValues = TerrainEditorView.TerrainWeights1.GetColorData(
                        GraphicsContext);
                else
                    ColorValues = TerrainEditorView.TerrainWeights2.GetColorData(
                        GraphicsContext);
                int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                    minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                    maxx = (int)Math.Min(tcomp.Width, clickResult.WorldPosition.X + radius),
                    maxz = (int)Math.Min(tcomp.Height, clickResult.WorldPosition.Z + radius);
                for (i = minx; i < maxx; i++)
                {
                    for (j = minz; j < maxz; j++)
                    {
                        //index = (tcomp.Width * j) + i;
                        index_in_tex = (tcomp.Width * j//(tcomp.Height - j)
                            ) + i;
                        float height = tcomp.GetCPUHeightAt(i, j);
                        SelectedPoints[index_in_tex] = false;
                        if (Vector3.Distance(clickResult.WorldPosition,
                            new Vector3(tcomp.m_QuadSideWidthX * i, height,
                            tcomp.m_QuadSideWidthZ * j)) <= radius
                            && Utility.RandomFloat() < TerrainEditorView.BallSelectionStrength)
                        {
                            SelectedPoints[index_in_tex] = true;
                            if (TerrainEditorView.Selectedtexture == 0 ||
                                TerrainEditorView.Selectedtexture == 4)
                                ColorValues[index_in_tex].R = (byte)Utility.BoundValue0255(
                            MathF.Round(-Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].R));
                            else if (TerrainEditorView.Selectedtexture == 1 ||
                                TerrainEditorView.Selectedtexture == 5)
                                ColorValues[index_in_tex].G = (byte)Utility.BoundValue0255(
                            MathF.Round(-Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].G));
                            else if (TerrainEditorView.Selectedtexture == 2 ||
                                TerrainEditorView.Selectedtexture == 6)
                                ColorValues[index_in_tex].B = (byte)Utility.BoundValue0255(
                            MathF.Round(-Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].B));
                            else if (TerrainEditorView.Selectedtexture == 3 ||
                                TerrainEditorView.Selectedtexture == 7)
                                ColorValues[index_in_tex].A = (byte)Utility.BoundValue0255(
                            MathF.Round(-Utility.Runif(10.0f, 50.0f) * pow
                        + ColorValues[index_in_tex].A));
                        }
                    }
                }
                if (TerrainEditorView.Selectedtexture < 4)
                {
                    TerrainEditorView.TerrainWeights1 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights1.Width,
                        TerrainEditorView.TerrainWeights1.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //.SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr,
                        TerrainEditorView.TerrainWeights1);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
                }
                else
                {
                    TerrainEditorView.TerrainWeights2 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights2.Width,
                        TerrainEditorView.TerrainWeights2.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //.SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr,
                        TerrainEditorView.TerrainWeights2);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
                }
                Array.Clear(ColorValues, 0, ColorValues.Length);
            }
            if (button == MouseButton.Middle)//smooth weights for selected texture only
            {
                int i, j,// index,
                         index_in_tex;
                float radius = TerrainEditorView.Radius;
                Color[] ColorValues = new Color[TerrainEditorView.TerrainWeights1.Width * TerrainEditorView.TerrainWeights1.Height];
                // Get the height information and put it in the array
                if (TerrainEditorView.Selectedtexture < 4)
                    ColorValues = TerrainEditorView.TerrainWeights1.GetColorData(
                        GraphicsContext);
                else
                    ColorValues = TerrainEditorView.TerrainWeights2.GetColorData(
                        GraphicsContext);
                int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                    minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                    maxx = (int)Math.Min(tcomp.Width, clickResult.WorldPosition.X + radius),
                    maxz = (int)Math.Min(tcomp.Height, clickResult.WorldPosition.Z + radius);
                for (i = minx; i < maxx; i++)
                {
                    for (j = minz; j < maxz; j++)
                    {
                        //index = (tcomp.Width * j) + i;
                        index_in_tex = (tcomp.Width * j//(tcomp.Height - j)
                            ) + i;
                        float height = tcomp.GetCPUHeightAt(i, j);
                        SelectedPoints[index_in_tex] = false;
                        if (Vector3.Distance(clickResult.WorldPosition,
                            new Vector3(tcomp.m_QuadSideWidthX * i, height,
                            tcomp.m_QuadSideWidthZ * j)) <= radius
                            && Utility.RandomFloat() < TerrainEditorView.BallSelectionStrength)
                        {
                            SelectedPoints[index_in_tex] = true;
                            Int2 size = new Int2(tcomp.Width, tcomp.Height); 
                            Vector4 weight = (
                                ColorValues.GetColorAt(size, i - 1, j - 1).ToVector4() +
                                ColorValues.GetColorAt(size, i - 1, j).ToVector4() +
                                ColorValues.GetColorAt(size, i - 1, j + 1).ToVector4() +
                                ColorValues.GetColorAt(size, i + 1, j - 1).ToVector4() +
                                ColorValues.GetColorAt(size, i + 1, j).ToVector4() +
                                ColorValues.GetColorAt(size, i + 1, j + 1).ToVector4() +
                                ColorValues.GetColorAt(size, i, j - 1).ToVector4() +
                                ColorValues.GetColorAt(size, i, j + 1).ToVector4()) /
                                HeightMapEditor.GeneralExtensions.CountNeighbors(size, i, j);// sumwt;
                            if (TerrainEditorView.Selectedtexture == 0 ||
                                TerrainEditorView.Selectedtexture == 4)
                                ColorValues[index_in_tex].R = (byte)Utility.BoundValue0255(
                            MathF.Round(weight.X));
                            else if (TerrainEditorView.Selectedtexture == 1 ||
                                TerrainEditorView.Selectedtexture == 5)
                                ColorValues[index_in_tex].G = (byte)Utility.BoundValue0255(
                            MathF.Round(weight.Y));
                            else if (TerrainEditorView.Selectedtexture == 2 ||
                                TerrainEditorView.Selectedtexture == 6)
                                ColorValues[index_in_tex].B = (byte)Utility.BoundValue0255(
                            MathF.Round(weight.Z));
                            else if (TerrainEditorView.Selectedtexture == 3 ||
                                TerrainEditorView.Selectedtexture == 7)
                                ColorValues[index_in_tex].A = (byte)Utility.BoundValue0255(
                            MathF.Round(weight.W));
                        }
                    }
                }
                if (TerrainEditorView.Selectedtexture < 4)
                {
                    TerrainEditorView.TerrainWeights1 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights1.Width,
                        TerrainEditorView.TerrainWeights1.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //.SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr,
                        TerrainEditorView.TerrainWeights1);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
                }
                else
                {
                    TerrainEditorView.TerrainWeights2 = ColorValues.ToTexture(
                        TerrainEditorView.TerrainWeights2.Width,
                        TerrainEditorView.TerrainWeights2.Height, GraphicsDevice, GraphicsContext.CommandList);
                    //.SetData(commandList, ColorValues);
                    ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr,
                        TerrainEditorView.TerrainWeights2);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
                }
                Array.Clear(ColorValues, 0, ColorValues.Length);
            }
            CubeInstancingRenderScript.UpdatingMode = 1;
        }

        /// <summary>
        /// this mode updates weights directly in the vertex of the mesh
        /// this allows for multi blend of the material colors, of the premade
        /// material we created in the stride studio, URL: Terrain/BlendTerrainMaterial
        /// </summary>
        /// <param name="Game"></param>
        /// <param name="button"></param>
        /// <param name="clickResult"></param>
        public static void ProcessVertexColors(TerrainComponent tcomp,
            MouseButton button, ClickResult clickResult,float deltaTime)
        {
            if (TerrainEditorView.TerrainDisplayModeSelected == 1)
            {
                TerrainEditorView.MSGlog.Add2Log("Cannot paint texture when using the height based shader...");
                return;
            }
            Color[] ColorValues = new Color[TerrainEditorView.TerrainWeights1.Width * TerrainEditorView.TerrainWeights1.Height];
            // Get the height information and put it in the array
            if (TerrainEditorView.Selectedtexture < 4)
                ColorValues = TerrainEditorView.TerrainWeights1.GetColorData(
                    tcomp.Game.GraphicsContext);
            else
                ColorValues = TerrainEditorView.TerrainWeights2.GetColorData(
                    tcomp.Game.GraphicsContext); 
            int Width = tcomp.Width,
                Height = tcomp.Height;
            SelectedPoints = new bool[Width * Height];
            int i, j, index;
            float radius = TerrainEditorView.Radius,// PerlinMenuCode.GetRadius(),
                  pow = TerrainEditorView.BallSelectionPower;// PerlinMenuCode.GetPower();
            int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                maxx = (int)Math.Min(Width, clickResult.WorldPosition.X + radius),
                maxz = (int)Math.Min(Height, clickResult.WorldPosition.Z + radius);
            for (j = minz; j < maxz; j++)
            {
                for (i = minx; i < maxx; i++)
                {
                    index = (Width * j) + i;
                    Int2 pos = new Int2(i, j);
                    float ht = tcomp.GetCPUHeightAt(pos);
                    SelectedPoints[index] = false;
                    if (Vector3.Distance(clickResult.WorldPosition,
                        new Vector3(tcomp.m_QuadSideWidthX * i, ht,
                        tcomp.m_QuadSideWidthZ * j)) <= radius
                        && Utility.Runif() < TerrainEditorView.BallSelectionStrength)
                    {
                        SelectedPoints[index] = true;
                        Vector4 col = tcomp.GetCPUColorAt(i, j).ToVector4();
                        Vector4 wt1 = tcomp.GetCPUWeight1At(i, j).ToVector4();
                        Vector4 wt2 = tcomp.GetCPUWeight2At(i, j).ToVector4();
                        float offset = 0;
                        if (button == MouseButton.Left)
                        {
                            offset = Utility.Runif(0.01f, 0.05f) * pow * deltaTime;
                        }
                        else if (button == MouseButton.Right)
                        {
                            offset = -Utility.Runif(0.01f, 0.05f) * pow * deltaTime;
                        }
                        else if (button == MouseButton.Middle)
                        {
                            float num = GeneralExtensions.CountNeighbors(new Int2(tcomp.Width, tcomp.Height), i, j);
                            col = (tcomp.GetCPUColorAt(i - 1, j - 1).ToVector4() +
                                tcomp.GetCPUColorAt(i - 1, j).ToVector4() +
                                tcomp.GetCPUColorAt(i - 1, j + 1).ToVector4() +
                                tcomp.GetCPUColorAt(i + 1, j - 1).ToVector4() +
                                tcomp.GetCPUColorAt(i + 1, j).ToVector4() +
                                tcomp.GetCPUColorAt(i + 1, j + 1).ToVector4() +
                                tcomp.GetCPUColorAt(i, j - 1).ToVector4() +
                                tcomp.GetCPUColorAt(i, j + 1).ToVector4())
                                / num;
                            wt1 = (tcomp.GetCPUWeight1At(i - 1, j - 1).ToVector4() +
                           tcomp.GetCPUWeight1At(i - 1, j).ToVector4() +
                           tcomp.GetCPUWeight1At(i - 1, j + 1).ToVector4() +
                           tcomp.GetCPUWeight1At(i + 1, j - 1).ToVector4() +
                           tcomp.GetCPUWeight1At(i + 1, j).ToVector4() +
                           tcomp.GetCPUWeight1At(i + 1, j + 1).ToVector4() +
                           tcomp.GetCPUWeight1At(i, j - 1).ToVector4() +
                           tcomp.GetCPUWeight1At(i, j + 1).ToVector4())
                           / num; 
                            wt2 = (tcomp.GetCPUWeight2At(i - 1, j - 1).ToVector4() +
                           tcomp.GetCPUWeight2At(i - 1, j).ToVector4() +
                           tcomp.GetCPUWeight2At(i - 1, j + 1).ToVector4() +
                           tcomp.GetCPUWeight2At(i + 1, j - 1).ToVector4() +
                           tcomp.GetCPUWeight2At(i + 1, j).ToVector4() +
                           tcomp.GetCPUWeight2At(i + 1, j + 1).ToVector4() +
                           tcomp.GetCPUWeight2At(i, j - 1).ToVector4() +
                           tcomp.GetCPUWeight2At(i, j + 1).ToVector4())
                           / num;
                        }
                        var color = TerrainEditorView.Selectedtexture switch
                        {
                            0 => wt1.X += offset,
                            1 => wt1.Y += offset,
                            2 => wt1.Z += offset,
                            3 => wt1.W += offset,
                            4 => wt2.X += offset,
                            5 => wt2.Y += offset,
                            6 => wt2.Z += offset,
                            7 => wt2.W += offset,
                            _ => 0,
                        };
                        col = col.Fix01();
                        wt1 = wt1.Fix01();
                        wt2 = wt2.Fix01();
                        tcomp.SetVertexColor(pos,col, wt1, wt2);
                        if (TerrainEditorView.Selectedtexture == 0)
                            ColorValues[index].R = (byte)Utility.BoundValue0255(
                                MathF.Round(wt1.X*255.0f));
                        else if (TerrainEditorView.Selectedtexture == 1)
                            ColorValues[index].G = (byte)Utility.BoundValue0255(
                        MathF.Round(wt1.Y * 255.0f));
                        else if (TerrainEditorView.Selectedtexture == 2)
                            ColorValues[index].B = (byte)Utility.BoundValue0255(
                        MathF.Round(wt1.Z * 255.0f));
                        else if (TerrainEditorView.Selectedtexture == 3)
                            ColorValues[index].A = (byte)Utility.BoundValue0255(
                        MathF.Round(wt1.W * 255.0f));
                        if (TerrainEditorView.Selectedtexture == 4)
                            ColorValues[index].R = (byte)Utility.BoundValue0255(
                                MathF.Round(wt2.X * 255.0f));
                        else if (TerrainEditorView.Selectedtexture == 5)
                            ColorValues[index].G = (byte)Utility.BoundValue0255(
                        MathF.Round(wt2.Y * 255.0f));
                        else if (TerrainEditorView.Selectedtexture == 6)
                            ColorValues[index].B = (byte)Utility.BoundValue0255(
                        MathF.Round(wt2.Z * 255.0f));
                        else if (TerrainEditorView.Selectedtexture == 7)
                            ColorValues[index].A = (byte)Utility.BoundValue0255(
                        MathF.Round(wt2.W * 255.0f));
                    }
                }
            }
            CubeInstancingRenderScript.UpdatingMode = 1;
            if (TerrainEditorView.Selectedtexture < 4)
            {
                TerrainEditorView.TerrainWeights1 = ColorValues.ToTexture(
                    TerrainEditorView.TerrainWeights1.Width,
                    TerrainEditorView.TerrainWeights1.Height, tcomp.GraphicsDevice, tcomp.Game.GraphicsContext.CommandList);
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr,
                    TerrainEditorView.TerrainWeights1);
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);
            }
            else
            {
                TerrainEditorView.TerrainWeights2 = ColorValues.ToTexture(
                    TerrainEditorView.TerrainWeights2.Width,
                    TerrainEditorView.TerrainWeights2.Height, tcomp.GraphicsDevice, tcomp.Game.GraphicsContext.CommandList);
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr,
                    TerrainEditorView.TerrainWeights2);
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);
            }
        }

        public static Texture GetHeightMapTexture()
        {
            return ImGuiSystem._loadedTextures[TerrainEditorView.TerrainHeightMapTextureIntPtr];
        }

        public static Texture GetHeightTerrainTexture(int id)
        {
            return ImGuiSystem._loadedTextures[TerrainEditorView.TerrainHeightBasedTexturesIntPtr[id - 1]];
        }
        public static Texture GetTerrainTexture(int id)
        {
            return ImGuiSystem._loadedTextures[TerrainEditorView.TerrainTexturesIntPtr[id - 1]];
        }

        public static void SetTerrainTexture(TerrainComponent tcomp,int id)
        {
            switch(id)
            {
                case 1:
                    // tcomp.Material.Passes[0].Parameters.Set(                        MaterialKeys.DiffuseMap, GetTerrainTexture(1));
                    // Parameters.Set(MaterialKeys.BlendMap, //MaterialKeys.DiffuseMap,
                    //  TerrainEditorView.TerrainWeights2);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_1, GetTerrainTexture(1));
//                    tcomp.Material.Passes[0].Parameters.Set(TerrainShaderOneKeys.Texture_1, GetTerrainTexture(1));
                    break;
                case 2:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_2, GetTerrainTexture(2));
 //                   tcomp.Material.Passes[0].Parameters.Set(TerrainShaderOneKeys.Texture_2, GetTerrainTexture(2));
                    //                    tcomp.Material.Passes[0].Parameters.Set(MaterialKeys.DiffuseMap, GetTerrainTexture(2));
                    //     MaterialBlendSingleLayers layers = tcomp.Material.Passes[0].Material.Descriptor.Layers;
                    //     layers[0].Material.Passes[0].Parameters.Set(MaterialKeys.DiffuseMap, GetTerrainTexture(2));

                    //     .Material?.Descriptor?.Layers[0].Material.Passes?[0].
                    //  Parameters.Set(MaterialKeys.DiffuseMap, GetTerrainTexture(2));
                    //tcomp.Material.Passes[0].Parameters.Get(MaterialBlendSingleLayers.
                    //   var renderMesh = (RenderMesh)renderObject;
                    //   bool hasDiffuseMap = renderMesh.MaterialPass.Parameters.ContainsKey(MaterialKeys.DiffuseMap);
                    //   MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White
                    // gridMaterial.Passes[0].Parameters.Set(TexturingKeys.Texture0, gridTexture);

                    //                    tcomp.Material.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_2, GetTerrainTexture(2));
                    break;
                case 3:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_3, GetTerrainTexture(3));
                    break;
                case 4:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_4, GetTerrainTexture(4));
                    break;
                case 5:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_5, GetTerrainTexture(5));
                    break;
                case 6:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_6, GetTerrainTexture(6));
                    break;
                case 7:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_7, GetTerrainTexture(7));
                    break;
                case 8:
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_8, GetTerrainTexture(8));
                    break;
            }
//            tcomp.Material = SetupTerrainMaterial(GraphicsDevice);
        }
        public static void SetHeightBasedTerrainTexture(TerrainComponent tcomp, int id)
        {
            switch (id)
            {
                case 1:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_1, GetHeightTerrainTexture(1));
                    break;
                case 2:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_2, GetHeightTerrainTexture(2));
                    break;
                case 3:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_3, GetHeightTerrainTexture(3));
                    break;
                case 4:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_4, GetHeightTerrainTexture(4));
                    break;
                case 5:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_5, GetHeightTerrainTexture(5));
                    break;
                case 6:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_6, GetHeightTerrainTexture(6));
                    break;
                case 7:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_7, GetHeightTerrainTexture(7));
                    break;
                case 8:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_8, GetHeightTerrainTexture(8));
                    break;
                case 9:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_9, GetHeightTerrainTexture(9));
                    break;
                case 10:
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.Texture_10, GetHeightTerrainTexture(10));
                    break;
            }
        }

        public static Texture GetBlendedTexture(TerrainComponent tcomp,
            GraphicsContext GraphicsContext, GraphicsDevice GraphicsDevice)
        {
            if (tcomp == null) return null;
            CommandList CommandList = GraphicsContext.CommandList;
            int i, j, index, m_Width = tcomp.Width, m_Height = tcomp.Height;
            Texture tex = Texture.New2D(GraphicsDevice, m_Width, m_Height, 
                PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
            Color[] OutColors = new Color[m_Width * m_Height],
                Weight1Colors = TerrainEditorView.TerrainWeights1.GetColorData(GraphicsContext),
                Weight2Colors = TerrainEditorView.TerrainWeights2.GetColorData(GraphicsContext);

            Texture tex1 = TerrainScript.GetTerrainTexture(1).Resize(m_Width, 
                m_Height, GraphicsContext),
                tex2 = TerrainScript.GetTerrainTexture(2).Resize(
                m_Width, m_Height, GraphicsContext),
                tex3 = TerrainScript.GetTerrainTexture(3).Resize(m_Width, m_Height,
                 GraphicsContext),
                tex4 = TerrainScript.GetTerrainTexture(4).Resize(m_Width, m_Height, 
                 GraphicsContext),
                tex5 = TerrainScript.GetTerrainTexture(5).Resize(m_Width, m_Height, 
                GraphicsContext),
                tex6 = TerrainScript.GetTerrainTexture(6).Resize(m_Width, m_Height, 
                 GraphicsContext),
                tex7 = TerrainScript.GetTerrainTexture(7).Resize(m_Width, m_Height, 
                 GraphicsContext),
                tex8 = TerrainScript.GetTerrainTexture(8).Resize(m_Width, m_Height,
                 GraphicsContext);
                      Color[] Tex1Colors = new Color[tex1.Width * tex1.Height];
                      tex1.GetData(CommandList, Tex1Colors);
                      Color[] Tex2Colors = new Color[tex2.Width * tex2.Height ];
                      tex2.GetData(CommandList, Tex2Colors);
                      Color[] Tex3Colors = new Color[tex3.Width * tex3.Height];
                      tex3.GetData(CommandList, Tex3Colors);
                      Color[] Tex4Colors = new Color[tex4.Width * tex4.Height];
                      tex4.GetData(CommandList, Tex4Colors);
                      Color[] Tex5Colors = new Color[tex5.Width * tex5.Height ];
                      tex5.GetData(CommandList, Tex5Colors);
                      Color[] Tex6Colors = new Color[tex6.Width * tex6.Height ];
                      tex6.GetData(CommandList, Tex6Colors);
                      Color[] Tex7Colors = new Color[tex7.Width * tex7.Height ];
                      tex7.GetData(CommandList, Tex7Colors);
                      Color[] Tex8Colors = new Color[tex8.Width * tex8.Height ];
                      tex8.GetData(CommandList, Tex8Colors);            
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float sumwt = Weight1Colors[index].R / 255.0f +
                        Weight1Colors[index].G / 255.0f + Weight1Colors[index].B / 255.0f +
                        Weight1Colors[index].A / 255.0f + Weight2Colors[index].R / 255.0f +
                        Weight2Colors[index].G / 255.0f + Weight2Colors[index].B / 255.0f +
                        Weight2Colors[index].A / 255.0f;
                    OutColors[index] = (Weight1Colors[index].R / 255.0f / sumwt *
                        Tex1Colors[index] +
                        Weight1Colors[index].G / 255.0f / sumwt *
                        Tex2Colors[index] +
                        Weight1Colors[index].B / 255.0f / sumwt *
                        Tex3Colors[index] +
                        Weight1Colors[index].A / 255.0f / sumwt *
                        Tex4Colors[index] +
                        Weight2Colors[index].R / 255.0f / sumwt *
                        Tex5Colors[index] +
                        Weight2Colors[index].G / 255.0f / sumwt *
                        Tex6Colors[index] +
                        Weight2Colors[index].B / 255.0f / sumwt *
                        Tex7Colors[index] +
                        Weight2Colors[index].A / 255.0f / sumwt *
                        Tex8Colors[index]);
                }
            }
            tex = OutColors.ToTexture(tex.Width, tex.Height, GraphicsDevice, CommandList);
          //  tex.SetData(CommandList, OutColors);
            return tex;
        }

        public static Texture BlendTextures(TerrainComponent tcomp,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
        {
            CommandList CommandList = GraphicsContext.CommandList;
            int i, j, index, m_Width = TerrainEditorView.TextureBlendingWeights.Width, 
                m_Height = TerrainEditorView.TextureBlendingWeights.Height;
            Texture tex = Texture.New2D(GraphicsDevice, m_Width, m_Height,
                PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
            Color[] OutColors = new Color[m_Width * m_Height],
                WeightColors = TerrainEditorView.TextureBlendingWeights.GetColorData(GraphicsContext),
                Source1 = TerrainEditorView.TextureBlendingTex1.Resize(
                    m_Width,m_Height, GraphicsContext).GetColorData(GraphicsContext),
                Source2 = TerrainEditorView.TextureBlendingTex2.Resize(
                    m_Width, m_Height, GraphicsContext).GetColorData(GraphicsContext);

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;

                    float wt = WeightColors[index].R / 255.0f;
                    OutColors[index] = wt * Source1[index] + (1 - wt) * Source2[index];
                }
            }
            tex = OutColors.ToTexture(tex.Width, tex.Height, GraphicsDevice, CommandList);
            //  tex.SetData(CommandList, OutColors);
            return tex;
        }

    }
}

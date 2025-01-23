//by Idomeneas
using ImGui;
using ImGuiNET;
using HeightMapEditor;
using Stride.Engine;
using Stride.Graphics;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static ImGuiNET.ImGui;
using Color = Stride.Core.Mathematics.Color;
using PixelFormat = Stride.Graphics.PixelFormat;

namespace TerrainEditor.Windows
{
    class TerrainEditorApp
    {
        [STAThread]
        static void Main(string[] args)
        {
            
            using (var game = new EditorGame())
            {
                game.Run();
            }
        }
    }

    internal class EditorGame : Game
    {
        //  ImGuiSystem ImGuiSystem;
        TerrainEditorView terrainmenu;
        TerrainInGameGumps TerrainInGameGumps;
        HierarchyView HierarchyView;
        PerfMonitor PerfMonitor;
        ImGuiSystem ImGuiSystem;
        AreaObjectsView AreaObjectsView;
        StrideAssetsView StrideAssetsView;
        ProgressbarView ProgressbarView;
        TerrainTilesGumps TerrainTilesGumps;
        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void BeginRun()
        {
            base.BeginRun();
            ImGuiSystem = new ImGuiSystem(Services, GraphicsDeviceManager);
            StyleColorsDark();
            HierarchyView = new HierarchyView(Services);
            PerfMonitor = new PerfMonitor(Services);
            terrainmenu = new TerrainEditorView(Services);
            terrainmenu._uniqueName = "Terrain Editor";
            TerrainInGameGumps = new TerrainInGameGumps(Services);
            TerrainInGameGumps._uniqueName = "Game Gumps";

            ProgressbarView=new ProgressbarView(Services);
            ProgressbarView._uniqueName = "Progress...";

            AreaObjectsView = new AreaObjectsView(Services);
            AreaObjectsView._uniqueName = "Area Objects View";

            StrideAssetsView=new StrideAssetsView(Services);
            StrideAssetsView._uniqueName = "Stride Assets View";

            TerrainTilesGumps=new TerrainTilesGumps(Services);
            TerrainTilesGumps._uniqueName = "Terrain Tiles Mode Gumps";
            //         AreaObjectsView.Initialize();//load all assets and make an object database
            //load all texture content here
            // string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // string filename = AppContext.BaseDirectory + "Resources\\HeightMap.jpg";

            /*  string[] files = System.IO.Directory.GetFiles(
                  startupPath + "\\Resources\\", "*.jpg");
              for(int i=0;i<MathF.Min(TerrainEditorView.TerrainTexturesIntPtr.Length, 
                  files.Length);i++)
              {
                  using (var inStream = System.IO.File.OpenRead(files[i]))
                      texture = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                  TerrainEditorView.TerrainTexturesIntPtr[i] = ImGuiSystem.BindTexture(
                      texture);
              }*/
            LoadTextures(this);
            //     TerrainEditorView.TerrainWeights1 = Content.Load<Texture>("Textures/Blend/FirstWeights");
            //ImGuiSystem.BindTexture(TerrainEditorView.TerrainWeights1);
            //         TerrainEditorView.TerrainTexturesIntPtr[0]=ImGuiSystem.BindTexture(Content.Load<Texture>("Terrain/TerrainHeightmap"));
            ImGuiStylePtr style = GetStyle();
            style.Colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0, 0, 255, 255);
            style.Colors[(int)ImGuiCol.TabHovered] = Utility.Color2Vec4(Color.Teal);// new Vector4(0, 0, 255, 255);
            style.WindowRounding= 10.0f;
            //            ImGuiIOPtr io =ImGuiNET.ImGui.GetIO();
            //           TerrainEditorCameraController.ResetCamera(Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera);
        }

        void LoadTextures(Game Game)
        {

            string startupPath = Directory.GetParent(Assembly.
       GetExecutingAssembly().Location).Parent.Parent.Parent.FullName;
            if (!Directory.Exists(startupPath + "\\Resources\\"))
            {
                Directory.CreateDirectory(startupPath + "\\Resources\\");
            }
            Utility.Resources_Directory = startupPath + "\\Resources\\";
            StrideAssetsView.LoadAssets();
            if (!Directory.Exists(startupPath + "\\Resources\\TerrainEditor\\"))
            {
                Directory.CreateDirectory(startupPath + "\\Resources\\TerrainEditor\\");
            }
            Utility.Resources_TerrainEditor_Directory = startupPath + "\\Resources\\TerrainEditor\\";
            if (!Directory.Exists(startupPath + "\\Resources\\WorldTile\\"))
            {
                Directory.CreateDirectory(startupPath + "\\Resources\\WorldTile\\");
            }
            Utility.Resources_WorldTile_Directory = startupPath + "\\Resources\\WorldTile\\";
            if (!Directory.Exists(startupPath + "\\Resources\\TerrainEditor\\Areas\\"))
            {
                Directory.CreateDirectory(startupPath + "\\Resources\\TerrainEditor\\Areas\\");
            }
            Utility.Resources_TerrainEditorAreas_Directory = startupPath +
                "\\Resources\\TerrainEditor\\Areas\\";
            

                 /*  string startupPath = Directory.GetParent(Assembly.
          GetExecutingAssembly().Location).Parent.Parent.Parent.FullName;
                 string filename = startupPath + "\\Resources\\HeightMap.jpg";
                 Texture texture;
                 using (var inStream = System.IO.File.OpenRead(filename))
                     texture = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                 if (!texture.CheckFormat())
                     texture = texture.Resize(texture.Width, texture.Height, GraphicsContext);
                 */
                 Texture texture =Utility.LoadTex("TerrainEditor/HeightMap.bmp",
                GraphicsDevice, GraphicsContext//, PixelFormat.R8G8B8A8_UNorm, true, true
                );
            if(texture==null)
            {
                texture = Utility.FlatTex(1024, 1024,new Color(0,0,0,255),
                GraphicsDevice, GraphicsContext);
                PerlinNoise.IsGrayScaleHeightMap = true;
            }
            else//check if 
            {
                if(texture.CheckGrayScale(GraphicsContext))
                    PerlinNoise.IsGrayScaleHeightMap = true;
                else
                    PerlinNoise.IsGrayScaleHeightMap = false;
            }
            int width=texture.Width,height=texture.Height;
            TerrainEditorView.TerrainHeightMapTextureIntPtr = ImGuiSystem.BindTexture(
                texture);

            ////////////////MAJOR NOTE
            ///LOADING USING THE CONTENT LOADER MESSES UP THE TEXTURE PIXEL FORMAT
            ///SO I DO NOT ADVISE IT FOR DEVELOPMENT PURPOSES, PIXEL MANIPULATIONS
            ///ETC. USE THE REFORMAT ENTENSION TO RENDER TO TEXTURE AND CHANGE THE FORMAT
            ///IF ALL FAILS, SAVE USING TerrainEditorView.SaveTex AND RELOAD USING
            ///Utility.LoadTex TO GET IT TO WORK PROPERLY.

            ///NOTE ALSO THAT THE RESIZE AND REFORMAT DO NOT WORK. FOR SOME REASON
            ///THEY WORK ONY ONCE AND THEN THROW EXEMPTION. SO IT IS BEST
            ///TO LOAD THE STARTUP TEXTURES FROM DISC NOT WITH CONTENT LOADED,
            ///UNLESS THEY CAN BE DECOMPRESSED...

            Texture tex1 = Content.Load<Texture>("Terrain/TerrainTexture1");            
            if (tex1.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource= Utility.Resources_Directory+Utility.FindAssetSourceDir("TerrainTexture1.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex1 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex1 = tex1.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if(tex1== null)throw new Exception("Bad texture file detected, tex1");
            }
            TerrainEditorView.TerrainTexturesIntPtr[0] =
                ImGuiSystem.BindTexture(tex1);
            Texture tex2 = Content.Load<Texture>("Terrain/TerrainTexture2");
            if (tex2.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture2.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex2 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex2 = tex2.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex2 == null) throw new Exception("Bad texture file detected, tex2");
            }
            TerrainEditorView.TerrainTexturesIntPtr[1] =
                ImGuiSystem.BindTexture(tex2);
            Texture tex3 = Content.Load<Texture>("Terrain/TerrainTexture3");
            if (tex3.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture3.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex3 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex3 = tex3.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex3 == null) throw new Exception("Bad texture file detected, tex3");
            }
            TerrainEditorView.TerrainTexturesIntPtr[2] =
                ImGuiSystem.BindTexture(tex3);
            Texture tex4 = Content.Load<Texture>("Terrain/TerrainTexture4");
            if (tex4.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture4.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex4 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex4 = tex4.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex4 == null) throw new Exception("Bad texture file detected, tex4");
            }
            TerrainEditorView.TerrainTexturesIntPtr[3] =
                ImGuiSystem.BindTexture(tex4);
            Texture tex5 = Content.Load<Texture>("Terrain/TerrainTexture5");
            if (tex5.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture5.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex5 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex5 = tex5.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex5 == null) throw new Exception("Bad texture file detected, tex5");
            }
            TerrainEditorView.TerrainTexturesIntPtr[4] =
                ImGuiSystem.BindTexture(tex5);
            Texture tex6 = Content.Load<Texture>("Terrain/TerrainTexture6");
            if (tex6.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture6.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex6 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex6 = tex6.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex6 == null) throw new Exception("Bad texture file detected, tex6");
            }
            TerrainEditorView.TerrainTexturesIntPtr[5] =
                ImGuiSystem.BindTexture(tex6);
            Texture tex7 = Content.Load<Texture>("Terrain/TerrainTexture7");
            if (tex7.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture7.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex7 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex7 = tex7.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex7 == null) throw new Exception("Bad texture file detected, tex7");
            }
            TerrainEditorView.TerrainTexturesIntPtr[6] =
                ImGuiSystem.BindTexture(tex7);
            Texture tex8 = Content.Load<Texture>("Terrain/TerrainTexture8");
            if (tex8.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir("TerrainTexture8.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    tex8 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                tex8 = tex8.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (tex8 == null) throw new Exception("Bad texture file detected, tex8");
            }
            TerrainEditorView.TerrainTexturesIntPtr[7] =
                ImGuiSystem.BindTexture(tex8);
            //minimap texture, from render to target camera
            Entity RenderToTexture = Services.GetService<SceneSystem>().
                SceneInstance.FirstOrDefault(a => a.Name == "RenderToTexture");
           // CameraComponent camera = MinimapCamera.Get<CameraComponent>();
            TerrainInGameGumps.MinimapTexture =
                RenderToTexture.Get<SpriteComponent>().CurrentSprite.
                Texture;
            TerrainInGameGumps.MinimapImageIntPtr = ImGuiSystem.BindTexture(
                TerrainInGameGumps.MinimapTexture);

            TerrainEditorView.TerrainPropertiesTexture = Utility.LoadTex(
                "TerrainEditor/TerrainPropertiesTexture.bmp", GraphicsDevice, GraphicsContext);
            TerrainEditorView.TerrainPropertiesTextureIntPtr = ImGuiSystem.BindTexture(
                          TerrainEditorView.TerrainPropertiesTexture);

            int m_Width = texture.Width, m_Height = texture.Height;
            TerrainEditorView.TerrainPropertiesTreeLocs = Utility.FlatTex(
    m_Width, m_Height, Color.Zero, GraphicsDevice, GraphicsContext);
            TerrainEditorView.TerrainPropertiesTreeLocsIntPtr=ImGuiSystem.BindTexture(TerrainEditorView.TerrainPropertiesTreeLocs);
            
            TerrainEditorView.TerrainPropertiesRoad = Utility.FlatTex(
    m_Width, m_Height, Color.Zero, GraphicsDevice, GraphicsContext);
            TerrainEditorView.TerrainPropertiesRoadIntPtr=ImGuiSystem.BindTexture(TerrainEditorView.TerrainPropertiesRoad);
            
            TerrainEditorView.TerrainPropertiesCollision =Utility.FlatTex(
    m_Width, m_Height, Color.Zero, GraphicsDevice, GraphicsContext);
            TerrainEditorView.TerrainPropertiesCollisionIntPtr = ImGuiSystem.BindTexture(TerrainEditorView.TerrainPropertiesCollision);


            TerrainEditorView.TerrainWeights1 = Utility.LoadTex("TerrainEditor/Weights1.bmp",
                GraphicsDevice, GraphicsContext);
            if (TerrainEditorView.TerrainWeights1 == null)
            {
                TerrainEditorView.TerrainWeights1 = 
                    Utility.FlatTex(width, height, new Color(255, 0, 0, 0),
                GraphicsDevice, GraphicsContext);
            }
            TerrainEditorView.TerrainWeights1IntPtr = ImGuiSystem.BindTexture(
                   TerrainEditorView.TerrainWeights1);

            TerrainEditorView.TerrainWeights2 = Utility.LoadTex("TerrainEditor/Weights2.bmp",
          GraphicsDevice, GraphicsContext);
            if (TerrainEditorView.TerrainWeights2 == null)
            {
                TerrainEditorView.TerrainWeights2 =
                    Utility.FlatTex(width, height, new Color(0, 0, 0, 0),
                GraphicsDevice, GraphicsContext);
            }
            TerrainEditorView.TerrainWeights2IntPtr = ImGuiSystem.BindTexture(
                     TerrainEditorView.TerrainWeights2);

            TerrainEditorView.WorldTilesTexture = Utility.LoadTex("TerrainEditor/WorldTilesTexture.bmp",
    GraphicsDevice, GraphicsContext);
            if (TerrainEditorView.WorldTilesTexture == null)
            {
                TerrainEditorView.WorldTilesTexture =
                    Utility.FlatTex(width, height, new Color(0, 0, 0, 255),
                GraphicsDevice, GraphicsContext);
            }
            TerrainEditorView.WorldTilesTextureIntPtr = ImGuiSystem.BindTexture(
                   TerrainEditorView.WorldTilesTexture);

            TerrainTilesGumps.WorldMapTexture = TerrainEditorView.WorldTilesTexture;
            TerrainTilesGumps.WorldMapIntPtr = ImGuiSystem.BindTexture(
                TerrainTilesGumps.WorldMapTexture);

            TerrainEditorView.CreatedImageSourceIntPtr = ImGuiSystem.BindTexture(
                   null);
            TerrainEditorView.CreatedImageResultIntPtr = ImGuiSystem.BindTexture(
                   null);

            TerrainEditorView.TextureBlendingTex1IntPtr = ImGuiSystem.BindTexture(null);
            TerrainEditorView.TextureBlendingTex2IntPtr = ImGuiSystem.BindTexture(null);
            TerrainEditorView.TextureBlendingWeightsIntPtr = ImGuiSystem.BindTexture(null);
            TerrainEditorView.TextureBlendingResultIntPtr = ImGuiSystem.BindTexture(null);

            Entity Terrain_Entity = Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            TerrainComponent tcomp = Terrain_Entity.Get<TerrainComponent>();
            TerrainEditorView.TerrainBlendedTexture = TerrainScript.GetBlendedTexture(tcomp, GraphicsContext, GraphicsDevice);
            TerrainEditorView.TerrainBlendedTextureIntPtr = ImGuiSystem.BindTexture(TerrainEditorView.TerrainBlendedTexture);

            //heightbased textures
            Texture httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_seabed");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory + 
                    Utility.FindAssetSourceDir("level_seabed.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex1");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[0] = ImGuiSystem.BindTexture(httex);
            
            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_sand");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb||
                httex.Description.Format == PixelFormat.BC3_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_sand.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex2");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[1] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_dirt");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_dirt.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex3");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[2] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_grass");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_grass.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex4");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[3] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_swamp");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_swamp.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex5");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[4] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_desert");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_desert.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex6");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[5] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_rock");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_rock.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex7");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[6] = ImGuiSystem.BindTexture(httex);
            
            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_mountain");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_mountain.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex8");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[7] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_snow");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_snow.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex9");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[8] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/HeightTerrainLevels/level_snow");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_TerrainEditor_Directory +
                    Utility.FindAssetSourceDir("level_snow.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, httex10");
            }
            TerrainEditorView.TerrainHeightBasedTexturesIntPtr[9] = ImGuiSystem.BindTexture(httex);

            httex = Content.Load<Texture>("Textures/DetailTexture");
            if (httex.Description.Format == PixelFormat.BC1_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory +
                    Utility.FindAssetSourceDir("DetailTexture.sdtex");
                using (var inStream = System.IO.File.OpenRead(texSource))
                    httex = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                httex = httex.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm_SRgb);
                if (httex == null) throw new Exception("Bad texture file detected, TerrainDetaiMapTextureIntPtr");
            }
            TerrainEditorView.TerrainDetaiMapTextureIntPtr = ImGuiSystem.BindTexture(httex);
        }
    }
}

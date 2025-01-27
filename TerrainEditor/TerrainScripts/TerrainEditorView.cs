//by Idomeneas
using HeightMapEditor;
using ImGui;
using ImGuiNET;
using SinglePassWireframe;
using Stride.Engine;
using Stride.Extensions;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Importer.Assimp;
using Stride.Rendering;
using Stride.Rendering.Materials;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ImGuiNET.ImGui;
using BoundingBox = Stride.Core.Mathematics.BoundingBox;

namespace TerrainEditor
{
    public class AppLog
    {
        public static int MaxNumStrings = 100;
        public string[] AllStrings = new string[MaxNumStrings];
        public bool[] ShowString = new bool[MaxNumStrings];
        static int MSGlines=0,selected=-1;
        static string _searchTerm = "";

        public AppLog()
        {
            Clear();
        }

        public void Clear()
        {
            if(AllStrings!=null)
                Array.Clear(AllStrings, 0, AllStrings.Length);
            selected = -1;
            MSGlines = 0;
            _searchTerm = "";
        }

        public void ShowPopupMSGWindow()
        {
            DelayedToolTip.ShowPopupMSGWindow();
        }

        private DelayedToolTip DelayedToolTip = new DelayedToolTip("",TimeSpan.FromSeconds(1));

        public void Add2Log(string msg)
        {
           // DelayedToolTip.Stop();
       //     if (DelayedToolTip == null)
            //    DelayedToolTip = new DelayedToolTip("Message Logged: " + msg, TimeSpan.FromSeconds(10));
        //    else
            DelayedToolTip.Add("Message Logged: " + msg, TimeSpan.FromSeconds(10));
            TerrainEditorView.show_PopupMSGWindow = true;

       //     MultiTypeCameraController.RenderText("Message Logged: "+msg,duration:1);
            if (MSGlines + 1 < MaxNumStrings)
            {
                AllStrings[MSGlines] = msg;
                ShowString[MSGlines] = true;
                MSGlines++;
                return;
            }
            for(int i=0;i< MaxNumStrings-1; i++)
            {
                AllStrings[i] = AllStrings[i + 1];
            }
            AllStrings[MaxNumStrings - 1] = msg;
            ShowString[MaxNumStrings - 1] = true;
        }

        public void Draw()
        {
           /* if (Button("Add")) {
                for (int i = 0; i < 10; i++)
                {
                    Add2Log(DateTime.UtcNow.ToString());
                }
            }
            SameLine();*/
            if (Button("Clear")) Clear();
            SameLine();
            if (InputText("Filter", ref _searchTerm, 64))
            {
                if (System.String.IsNullOrWhiteSpace(_searchTerm) == false)
                {
                    for (int i = 0; i < MSGlines; i++)
                    {
                        ShowString[i] = true;
                        if (AllStrings[i].IndexOf(_searchTerm) == -1)
                        {
                            ShowString[i] = false;
                        }
                    }
                }
                else {
                    for (int i = 0; i < MSGlines; i++)
                    {
                        ShowString[i] = true;
                    }
                }
            }
            if (selected > -1)
            {
                SameLine();
                if (Button("Copy " + (selected + 1).ToString()))
                    Clipboard.SetText(AllStrings[selected]);
            }
            Separator();
            BeginChild("scrolling", new Vector2(0, 0), false,
                ImGuiWindowFlags.HorizontalScrollbar);
            int count = 1;
            for (int i = 0; i < MSGlines; i++)
            {
                if (ShowString[i])
                {
                    if (selected == i)
                    {
                        TextColored(new Vector4(0, 1, 1, 1), count.ToString() + ". " + AllStrings[i]);
                        if (IsItemHovered() && IsMouseDown(ImGuiMouseButton.Right))
                        {
                            selected = -1;
                        }
                    }
                    else
                    {
                        Text(count.ToString() + ". " + AllStrings[i]);
                        if (IsItemHovered() && IsMouseDown(ImGuiMouseButton.Left))
                        {
                            selected = i;
                        }
                    }
                    count++;
                }
            }
            EndChild();
        }
    };

    public class DelayedToolTip
    {
        private Stopwatch stopWatch = new Stopwatch();
        public TimeSpan delay { get; set; }
        public List<string> text2Show { get; set; }=new List<string>();
        public bool ShowAsWindow;
        public DelayedToolTip(string text2Show1, TimeSpan delay1, 
            bool ShowAsWindow1 = true)
        {
            delay = delay1;
            text2Show = new List<string>();
            text2Show.Add(text2Show1);
            ShowAsWindow = ShowAsWindow1;
            stopWatch.Start();
        }

        public void Add(string text, TimeSpan delay1)
        { 
            text2Show.Add(text);
            delay = delay1;
            stopWatch.Restart();
        }
        public void Stop()
        { 
            stopWatch.Stop(); 
        }

        public void ShowPopupMSGWindow()
        {
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;
            if (ts.TotalSeconds > delay.TotalSeconds)
            {
                stopWatch.Stop();
                text2Show.Clear();
                TerrainEditorView.show_PopupMSGWindow = false;
                return;
            }

            if (ShowAsWindow)
            {
           //     ImGuiIOPtr io = GetIO();
                SetNextWindowPos(new Vector2(10.0f,
                    8.0f * GetWindowViewport().Size.Y / 10.0f), ImGuiCond.Always);
                SetNextWindowSize(new Vector2(
                    GetWindowViewport().Size.X / 2.0f,
                    1.0f * GetWindowViewport().Size.Y / 10.0f), ImGuiCond.Always);
                Begin("ShowPopupMSGWindow", ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar| 
                    ImGuiWindowFlags.AlwaysVerticalScrollbar);
                //    PushTextWrapPos(GetFontSize() * 35.0f);
                foreach (string st in text2Show)
                TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().AsNumericVec4(),
                    st);
                //  PopTextWrapPos();


                End();
            }
            else
            {
                BeginTooltip();
                //   PushTextWrapPos(GetFontSize() * 35.0f);
                foreach (string st in text2Show)
                    TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().AsNumericVec4(),
                        st);
                //  PopTextWrapPos();
                EndTooltip();
            }
        }
    }

    public enum EditorMode
    {
        MainScene, TerrainEditor, InGameTerrain, InGameTiles
    }
    public class TerrainEditorView : BaseWindow
    {
        public static Texture NoiseMapTexture;
        public static Texture SkyTexture;
        public static Texture WaterFloorTexture;
        public static Texture FlowMapTexture;
        public static Texture NormalTexture1;
        public static Texture NormalTexture2;
        public static Texture DiffuseTexture1;
        public static Texture DiffuseTexture2;
        //     public static Scene EditorMainScene;
        public static bool SaveGeneratedBMP = false, show_MainMenu = true, show_HelpGUI = true,
             Show_StackToolWindowGUI, show_DebugLogWindow, show_DemoGUI,
            show_PopupMSGWindow = false;
        // ,show_OpenFileGUI, show_TileTextures;
        public static EditorMode CurrentEditorMode= EditorMode.TerrainEditor;
        public static bool show_GeneralInfoGUI = true,//top right window
             show_SpecificInfoGUI;//bot right window
        protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoMove
           | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar;

        public static int SwitchtoSpecificEditorTab = 0, SwitchtoGeneralTab = 0;
        float xloc, yloc, screenwidth, screenheight;
        public static ImGuiIOPtr ImguiIO;

        private List<ConvolutionFilterBase> filterList = new List<ConvolutionFilterBase>();

        public TerrainEditorView(Stride.Core.IServiceRegistry services) : base(services)
        {
            ImguiIO = GetIO();
            screenwidth = Game.GraphicsContext.CommandList.Viewport.Width;
            screenheight = Game.GraphicsContext.CommandList.Viewport.Height;
            xloc = screenwidth-Game.GraphicsContext.CommandList.Viewport.Width / 4.0f;
            yloc = 55.0f;

            filterList.Add(new Blur3x3Filter());
            filterList.Add(new Blur5x5Filter());
            filterList.Add(new Gaussian3x3BlurFilter());
            filterList.Add(new Gaussian5x5BlurFilter());
            filterList.Add(new SoftenFilter());
            filterList.Add(new MotionBlurFilter());
            filterList.Add(new MotionBlurLeftToRightFilter());
            filterList.Add(new MotionBlurRightToLeftFilter());
            filterList.Add(new HighPass3x3Filter());
            filterList.Add(new EdgeDetectionFilter());
            filterList.Add(new HorizontalEdgeDetectionFilter());
            filterList.Add(new VerticalEdgeDetectionFilter());
            filterList.Add(new EdgeDetection45DegreeFilter());
            filterList.Add(new EdgeDetectionTopLeftBottomRightFilter());
            filterList.Add(new SharpenFilter());
            filterList.Add(new IntenseSharpenFilter());
            filterList.Add(new EmbossFilter());
            filterList.Add(new Emboss45DegreeFilter());
            filterList.Add(new EmbossTopLeftBottomRightFilter());
            filterList.Add(new IntenseEmbossFilter());
            Entity Terrain_Entity =
                Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            tcomp = Terrain_Entity.Get<TerrainComponent>();
            if (tcomp == null) throw new Exception("Terrain component not created yet...");
            NoiseMapTexture = Content.Load<Texture>("Water/noise");
            SkyTexture = Content.Load<Texture>("Water/sky");
            WaterFloorTexture = Content.Load<Texture>("Water/sand");
            FlowMapTexture = Content.Load<Texture>("Water/flowmap");
            NormalTexture1 = Content.Load<Texture>("Water/wave0");
            NormalTexture2 = Content.Load<Texture>("Water/wave1");
            DiffuseTexture1 = Content.Load<Texture>("Water/foam0");
            DiffuseTexture2 = Content.Load<Texture>("Water/foam1");

        }
        TerrainComponent tcomp;

        protected override void OnDestroy() { }

        //     private static uint s_tab_bar_flags = (uint)ImGuiTabBarFlags.Reorderable;
        //      static bool[] s_opened = { true, true, true, true }; // Persistent user state

        public override void Update(GameTime gameTime)
        {
            if (CurrentEditorMode != EditorMode.TerrainEditor) return;
            if (!show_MainMenu) return;
            base.Update(gameTime);
        }
 
        protected override void OnDraw(bool collapsed)
        {
            if (collapsed)
                return;
            //           SetWindowCollapsed(false);            
            SetWindowPos(new Vector2(
                Game.GraphicsContext.CommandList.Viewport.Width
                / 2.0f, 20), ImGuiCond.Always);
            SetWindowSize(new Vector2(80, 25), ImGuiCond.Always);
            bool menu_open = BeginMenu("Main Menu");
            if (menu_open)
            {
                if (BeginMenu("Show/Hide Windows"))
                {
                    // if (MenuItem("IMGUI Demo", null, show_DemoGUI)) { show_DemoGUI = !show_DemoGUI; }
                    //  if (MenuItem("IMGUI Debug Log", null, show_DebugLogWindow)) { show_DebugLogWindow = !show_DebugLogWindow; }
                    //  if (MenuItem("IMGUI Stack Tool", null, Show_StackToolWindowGUI)) { Show_StackToolWindowGUI = !Show_StackToolWindowGUI; }
                    if (MenuItem("Help", null, show_HelpGUI)) { show_HelpGUI = !show_HelpGUI; }
                    if (MenuItem("Editor Options", null, show_GeneralInfoGUI)) { show_GeneralInfoGUI = !show_GeneralInfoGUI; }
                    //  if (MenuItem("Specific Editor Settings", null, show_SpecificInfoGUI)) { show_SpecificInfoGUI = !show_SpecificInfoGUI; }
                    if (MenuItem("Hide All "))
                    {
                        //show_OpenFileGUI = false; 
                        show_DemoGUI = false; show_HelpGUI = false;
                        //show_TileTextures = false; 
                        PerfMonitor.show_PerfMonitorGUI = false;
                        show_GeneralInfoGUI = false;
                        show_SpecificInfoGUI = false; show_DebugLogWindow = false;
                        HierarchyView.show_HeirarchyGUI = false;
                        Show_StackToolWindowGUI = false;
                        MSGlog.Add2Log("Closed all open windows...");
                    }
                    EndMenu();
                }
                if (BeginMenu("Scene Editors"))
                {
                    if (MenuItem("Hierarchy", null, HierarchyView.show_HeirarchyGUI)) { HierarchyView.show_HeirarchyGUI = !HierarchyView.show_HeirarchyGUI; }
                    if (MenuItem("Monitor", null, PerfMonitor.show_PerfMonitorGUI)) { PerfMonitor.show_PerfMonitorGUI = !PerfMonitor.show_PerfMonitorGUI; }
                    EndMenu();
                }
                //             Text("context");
                EndMenu();
                //         if (IsItemClicked())
                //              Text("#1B clicked (after EndMenu)\n");
            }

            if (show_GeneralInfoGUI) ShowGeneralInfoGUI();
            if (show_PopupMSGWindow) MSGlog.ShowPopupMSGWindow();
            //  if (show_DemoGUI) ShowDemoWindow();
            //  if (show_DebugLogWindow) ShowDebugLogWindow();
            //   if (show_SpecificInfoGUI) ShowSpecificInfoGUI();
            //   if (Show_StackToolWindowGUI) ShowStackToolWindow();
        }

        //tabed items in top right window
        void ShowGeneralInfoGUI()
        {
            SetNextWindowPos(new Vector2(xloc, yloc), ImGuiCond.Appearing);
            SetNextWindowSize(new Vector2(
                Game.GraphicsContext.CommandList.Viewport.Width
                / 1.8f, Game.GraphicsContext.CommandList.Viewport.Height
                / 1.1f), ImGuiCond.Appearing);
            if (!Begin("Editor Options", ImGuiWindowFlags.HorizontalScrollbar))
            {
                // Early out if the window is collapsed, as an optimization.
                End();
                return;
            }

            if (BeginTabBar("##footabtitle", 0))
            {
                bool p_open = true;
                ImGuiTabItemFlags flags0 = ImGuiTabItemFlags.None,
                    flags1 = ImGuiTabItemFlags.None, flags2 = ImGuiTabItemFlags.None,
                    flags3 = ImGuiTabItemFlags.None, flags4 = ImGuiTabItemFlags.None
                    , flags5 = ImGuiTabItemFlags.None, flags6 = ImGuiTabItemFlags.None;
                switch (SwitchtoGeneralTab)
                {
                    case 0: flags0 = ImGuiTabItemFlags.SetSelected; break;
                    case 1: flags1 = ImGuiTabItemFlags.SetSelected; break;
                    case 2: flags2 = ImGuiTabItemFlags.SetSelected; break;
                    case 3: flags3 = ImGuiTabItemFlags.SetSelected; break;
                    case 4: flags4 = ImGuiTabItemFlags.SetSelected; break;
                    case 5: flags5 = ImGuiTabItemFlags.SetSelected; break;
                    case 6: flags6 = ImGuiTabItemFlags.SetSelected; break;
                }

                if (BeginTabItem("Render Info", ref p_open, flags0))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 0;
                    }
                    Separator();
                    Separator();
                    ShowHUD();
                    EndTabItem();
                }
                if (BeginTabItem("Randomization", ref p_open, flags1))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 1;
                    }
                    Separator();
                    Separator();
                    ShowRandomizationGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Messages", ref p_open, flags2))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 2;
                    }
                    Separator();
                    Separator();
                    ShowMessagesGUI();
                    EndTabItem();
                }
                if (BeginTabItem("World", ref p_open, flags3))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 3;
                    }
                    Separator();
                    Separator();
                    ShowWorldEditorGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Terrain", ref p_open, flags4))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 4;
                    }
                    Separator();
                    Separator();
                    ShowTerrainEditorGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Area", ref p_open, flags5))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 5;
                    }
                    Separator();
                    Separator();
                    ShowAreaEditorGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Image Manipulation", ref p_open, flags6))
                {
                    if (IsItemHovered())//IsMouseClicked(0))
                    {
                        SwitchtoGeneralTab = 6;
                    }
                    Separator();
                    Separator();
                    ShowTextureManipulationGUI();
                    EndTabItem();
                }
                EndTabBar();
            }

            End();
        }

        public static bool use_earth_rules = true;
        public static float map_uniform_or_random = 0.5f,
    map_land_or_sea = 0.5f, map_wet_or_dry = 0.5f, map_goods_more_or_less = 0.5f,
    map_continent_or_island = 0.5f, map_warm_or_cold = 0.5f, map_plains_or_highlands = 0.5f,
    map_mountain_or_hill = 0.5f, map_rivers = 0.5f, map_forestry = 0.5f,
            map_human_friendly = 0.5f,
            //fixed height for the camera in Tile
            InGameTiles_Camera_Height = 100.0f;
        public static int m_NumTilesWideX = 256,
                          m_NumTilesHighZ = 256, 
            TileSizeX = 1024, TileSizeZ = 1024, UniqueTileNum2Generate=10,
            RandomWalkXDir=1, RandomWalkZDir=1;
        public static IntPtr WorldTilesTextureIntPtr;
        public static Texture WorldTilesTexture = new Texture();

        void ShowWorldEditorGUI()
        {
            Text("World Properties");
            PushItemWidth(100);
            if (InputInt("World Tiles X-direction", ref m_NumTilesWideX, 1, 50))
            {
                if (m_NumTilesWideX < 16) m_NumTilesWideX = 16;
                if (m_NumTilesWideX > 1024) m_NumTilesWideX = 1024;
                TerrainTiles.m_NumTilesWideX = m_NumTilesWideX;
            }
            SameLine(300);
            if (InputInt("World Tiles Z-direction", ref m_NumTilesHighZ, 1, 50))
            {
                if (m_NumTilesHighZ < 16) m_NumTilesHighZ = 16;
                if (m_NumTilesHighZ > 1024) m_NumTilesHighZ = 1024;
                TerrainTiles.m_NumTilesHighZ = m_NumTilesHighZ;
            }
            if (InputInt("Tile Size X-direction", ref TileSizeX, 1, 50))
            {
                if (TileSizeX < 32) TileSizeX = 32;
                if (TileSizeX > 1024) TileSizeX = 1024;
            }
            SameLine(300);
            if (InputInt("Tile Size Z-direction", ref TileSizeZ, 1, 50))
            {
                if (TileSizeZ < 32) TileSizeZ = 32;
                if (TileSizeZ > 1024) TileSizeZ = 1024;
            }
            PopItemWidth();
            Separator();
            if (Button("Generate Unique Tiles"))
            {
                if (MessageBox.Show("Potentially slow operation! Are you sure you want to proceed?",
                    "Attention!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                { return; }
                Cursor.Current = Cursors.WaitCursor;
                TerrainTiles.GenerateUniqueTiles(tcomp);
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            HelpMarker("Potentially very slow operation. Generates "+ UniqueTileNum2Generate+
                " unique tile heightmaps for each type of world tiles, e.g., mountains, " +
                "hills, river masks etc. These can then be edited in the terrain editor and refined to your liking. " +
                "These tiles form a database from which to pick a tile when generating world tiles. " +
                "It is worth making these at least 1024x1024, even if it takes a while to generate them..." +
                " All unique tiles are given specific names depending on the tile type and are " +
                "saved in "+ Utility.Resources_WorldTile_Directory+".");
            SameLine();
            if (Button("Load Unique World Tiles"))
            {
                if (MessageBox.Show("Potentially slow operation! Are you sure you want to proceed?",
                    "Attention!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                { return; }
                // Set cursor as hourglass
                Cursor.Current = Cursors.WaitCursor;
                Task.Run(() =>
                {
                    TerrainTiles.LoadUniqueTiles(tcomp);
                }).Wait();
                // Set cursor as default arrow
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            HelpMarker("Loads unique tile heightmaps from "+ Utility.Resources_WorldTile_Directory+
                " and creates a database of these tiles that can be used to generate world tiles.");
            SameLine();
  /*          if (Button("Create World Tiles"))
            {
                if (MessageBox.Show("Potentially slow operation! Are you sure you want to proceed?",
                    "Attention!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                { return; }
                Cursor.Current = Cursors.WaitCursor;
                TerrainTiles.GenerateWorldTiles(tcomp);
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            HelpMarker("Builds the whole world based on the current World Tiles " +
                "Texture (which has to be generated or loaded first). For each type of tile, it chooses randomly from the " +
                "current unique tile database (generated or read from the disk). This " +
                "operation can be very slow, since the new world tiles need to be " +
                "smoothed, rotated, checked and fixed to have the same height edges. " +
                "Instead we opt to build each tile asynchronously as requested based " +
                "on the location we are in the world. Once a tile model is build we have it " +
                "and all we need to do is enable/disable the tile entities as we move" +
                " about the world..");*/
            SameLine();
            if (Button("Open World Tiles Dir##OpenWorldTilesDir"))
            {
                string argument = Utility.Resources_WorldTile_Directory;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
            if (SliderInt("Unique Tile Number to Generate", 
                ref UniqueTileNum2Generate, 1, 100))
            {
            }            
            Separator();
            Text("Generated World based on rules");
            if (Button("Generate with Rules##GeneratewithRules"))
            {
                if (TerrainTiles.UniqueWorldTiles == null || TerrainTiles.UniqueWorldTiles.Count == 0)
                {
                    TerrainEditorView.MSGlog.Add2Log(
                        "There are no unique world tiles! Loading them...");
                    Cursor.Current = Cursors.WaitCursor;
                    Task.Run(() =>
                    {
                        TerrainTiles.LoadUniqueTiles(tcomp);
                    }).Wait();
                    // Set cursor as default arrow
                    Cursor.Current = Cursors.Default;
                    //return;
                }
                Cursor.Current = Cursors.WaitCursor;
                TerrainTiles.m_NumTilesWideX = m_NumTilesWideX;
                TerrainTiles.m_NumTilesHighZ = m_NumTilesHighZ;
                TerrainTiles.Width = TileSizeX;
                TerrainTiles.Height = TileSizeZ;
                TerrainTiles.Randomize_Terrain_BasedOnRules();
                WorldTilesTexture=TerrainTiles.Generate_WorldMap_Texture(
                    GraphicsDevice,Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(WorldTilesTextureIntPtr,WorldTilesTexture);
                TerrainTiles.CreateWorldTiles(tcomp);
                TerrainTilesGumps.WorldMapTexture = WorldTilesTexture;
                ImGuiSystem.UpdateTexture(TerrainTilesGumps.WorldMapIntPtr, TerrainTilesGumps.WorldMapTexture);
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            HelpMarker("Builds the whole world based on the generated World Tiles " +
                "Texture. For each type of tile, it chooses randomly from the " +
                "current unique tile database (generated or read from the disk). This " +
                "operation can be very slow, since the new world tiles need to be " +
                "smoothed, rotated, checked and fixed to have the same height edges. " +
                "Instead we opt to build each tile asynchronously as requested based " +
                "on the location we are in the world. Once a tile model is build we have it " +
                "and all we need to do is enable/disable the tile entities as we move" +
                " about the world..");
            SameLine();
            if (Button("Reset Randomization Values##ResetWorldRandomizationValues"))
            {
                map_uniform_or_random = 0.5f; map_land_or_sea = 0.5f;
                map_wet_or_dry = 0.5f; map_goods_more_or_less = 0.5f;
                map_continent_or_island = 0.5f; map_warm_or_cold = 0.5f;
                map_plains_or_highlands = 0.5f; map_mountain_or_hill = 0.5f;
                map_rivers = 0.5f; map_forestry = 0.5f; map_human_friendly = 0.5f;
                UniqueTileNum2Generate = 10;
            }
            SameLine();
            if (Checkbox("Use Earth Rules", ref use_earth_rules))
            {
            }
            SameLine(); 
            HelpMarker("Earth rules implies connected maps left-right, north-south poles with snow and desert about the equator.");

            DrawImage(WorldTilesTextureIntPtr, new Vector2(128.0f, 128.0f));
            if (IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(WorldTilesTextureIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            } 
            SameLine();
            BeginChild("worldtilesprops", new Vector2(GetWindowWidth() * .85f,
                100), false, ImGuiWindowFlags.HorizontalScrollbar);

            TextWrapped("Number of world tiles: "+TerrainTiles.total_tile_number.ToString()+
                ", Land: "+ TerrainTiles.number_of_land_tiles.ToString()+
                ", Sea: " + (TerrainTiles.total_tile_number-
                TerrainTiles.number_of_land_tiles).ToString());
            if (Button("Load World##WorldTilesTextureIntPtrLoadWorld"))
            {
                if (WorldTilesTexture != null)
                {
                    if (TerrainTiles.UniqueWorldTiles == null || TerrainTiles.UniqueWorldTiles.Count == 0)
                    {
                        TerrainEditorView.MSGlog.Add2Log("There are no unique world tiles! Make sure you load them first!");
                        return;
                    }
                    OpenFileDialog theDialog = new OpenFileDialog();
                    theDialog.Title = "Load World Tile Map";
                    theDialog.Filter = "bmp files|*.*";
                    theDialog.InitialDirectory = Utility.Resources_WorldTile_Directory;
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        WorldTilesTexture = Utility.LoadTex(theDialog.FileName.ToString(),
                            GraphicsDevice, Game.GraphicsContext, false);
                        m_NumTilesWideX = WorldTilesTexture.Width;
                        m_NumTilesHighZ = WorldTilesTexture.Height;
                        ImGuiSystem.UpdateTexture(WorldTilesTextureIntPtr,
                            WorldTilesTexture);
                        TerrainTiles.LoadTiles_WorldMap_Texture(WorldTilesTexture,
                            GraphicsDevice, Game.GraphicsContext);
                        TerrainTiles.m_NumTilesWideX = m_NumTilesWideX;
                        TerrainTiles.m_NumTilesHighZ = m_NumTilesHighZ;
                        TerrainTiles.Width = TileSizeX;
                        TerrainTiles.Height = TileSizeZ;
                        TerrainTiles.CreateWorldTiles(tcomp);
                        Cursor.Current = Cursors.Default;
                    }
                }
            }
            SameLine();
            HelpMarker("When you load the world map from a file, the world tile types are known and you do not need to generate based on rules (it is done for you). Just hit 'Generate World Tiles'...");
            //could same all info from the world tiles in a save/load game fashion
            //but we're only saving the map types sea-land and generate the rest...
            if (Button("SaveAs##WorldTilesTextureIntPtr"))
            {
                if (WorldTilesTexture != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save World Tile Map";
                    theDialog.Filter = "bmp files|*.bmp";
                    theDialog.InitialDirectory = Utility.Resources_WorldTile_Directory;
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved World Tile Map image.");
                        SaveTex(WorldTilesTexture, theDialog.FileName.ToString(), Game.GraphicsContext, false);
                    }
                }
            }
            PushItemWidth(100);
            if (SliderInt("Random Walk X-dir", ref RandomWalkXDir, 1, 20))
            {
            }
            SameLine();
            if (SliderInt("Random Walk Z-dir", ref RandomWalkZDir, 1, 20))
            {
            }
            PopItemWidth();
            EndChild();

            Text("World Map Properties");
            if (SliderFloat("Probability Sea/Land", ref map_land_or_sea, 0.1f, 0.9f))
            {
            }
            if (SliderFloat("Probability Continent/Island", ref map_continent_or_island, 0.1f, 0.9f))
            {
            }
            if (SliderFloat("Probability Uniform/Random", ref map_uniform_or_random, 0.1f, 0.9f))
            {
            }
            if (SliderFloat("Probability Human Friendly", ref map_human_friendly, 0.1f, 0.9f))
            {
            }
            if (SliderFloat("Probability Wet/Dry", ref map_wet_or_dry, 0.1f, 0.9f))
            {
            }
            SameLine();HelpMarker("Wet leads to Wetlands (Forests/Jungle, Grassland, Swamp, Marsh), Dry leads to Desert near the equator.");

            if (SliderFloat("Probability Warm/Cold", ref map_warm_or_cold, 0.1f, 0.9f))
            {
            }
            if (SliderFloat("Probability Plains/Highlands", ref map_plains_or_highlands, 0, 1))
            {
            }
            if (SliderFloat("Probability Mountain/Hill", ref map_mountain_or_hill, 0, 1))
            {
            }
            if (SliderFloat("Probability Rivers", ref map_rivers, 0, 1))
            {
            }
            if (SliderFloat("Probability Forestry", ref map_forestry, 0, 1))
            {
            }

            if (SliderFloat("Probability Goods More/Less", ref map_goods_more_or_less, 0, 1))
            {
            }
            Separator();
            TextWrapped("Set the camera height while in Game Tile mode. Camera height is fixed" +
                " at this value, and DAWS is used to move left/right in/out.");
            if (SliderFloat("Camera Height##InGameTiles", ref InGameTiles_Camera_Height,
                50, 1000))
            {
            }
            
        }

        public static IntPtr TextureBlendingTex1IntPtr,
          TextureBlendingTex2IntPtr, TextureBlendingWeightsIntPtr,
          TextureBlendingResultIntPtr;
        public static Texture TextureBlendingTex1 = new Texture(),
          TextureBlendingTex2 = new Texture(), TextureBlendingWeights = new Texture(),
          TextureBlendingResult = new Texture();
        public int FilteringModeIndex = 0;
        public float RotationAngle = 90;
        static string[] FilteringMode = {
        "Blur3x3Filter",//0
		"Blur5x5Filter",//1 
		"Gaussian3x3BlurFilter",//2 
		"Gaussian5x5BlurFilter",//3 
		"SoftenFilter",//4 
		"MotionBlurFilter",//5 
		"MotionBlurLeftToRightFilter",//6
		"MotionBlurRightToLeftFilter",//7
		"HighPass3x3Filter",//8
		"EdgeDetectionFilter",//9
		"HorizontalEdgeDetectionFilter",//10
		"VerticalEdgeDetectionFilter",//11
		"EdgeDetection45DegreeFilter",//12
		"EdgeDetectionTopLeftBottomRightFilter",//13
		"SharpenFilter",//14
		"IntenseSharpenFilter",//15
		"EmbossFilter",//16
		"Emboss45DegreeFilter",//17
		"EmbossTopLeftBottomRightFilter",//18
		"IntenseEmbossFilter",//19
	 };

        void ShowTextureManipulationGUI()
        {
            if (tcomp == null) return;

            #region pics
            BeginChild("CreatedImageEditorGUI1", new Vector2(GetWindowWidth() * .95f,
                //GetWindowHeight() * .5f
                350), false, ImGuiWindowFlags.HorizontalScrollbar);
            Columns(4, null, false);
            DrawImage(TextureBlendingTex1IntPtr, new Vector2(256.0f, 256.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TextureBlendingTex1IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Load Source1##LoadSource1"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Source1 Texture";
                theDialog.Filter = "bmp files|*.*";
                //        string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    TextureBlendingTex1 = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    ImGuiSystem.UpdateTexture(TextureBlendingTex1IntPtr,
                        TextureBlendingTex1);
                }
            }
            NextColumn();
            DrawImage(TextureBlendingTex2IntPtr, new Vector2(256.0f, 256.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TextureBlendingTex2IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Load Source2##LoadSource2"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Source2 Texture";
                theDialog.Filter = "bmp files|*.*";
                //        string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    TextureBlendingTex2 = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    ImGuiSystem.UpdateTexture(TextureBlendingTex2IntPtr,
                        TextureBlendingTex2);
                }
            }
            NextColumn();
            DrawImage(TextureBlendingWeightsIntPtr, new Vector2(256.0f, 256.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TextureBlendingWeightsIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Load Blend Weights##LoadBlendWeights"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Blend Weights Texture";
                theDialog.Filter = "bmp files|*.*";
                //        string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    TextureBlendingWeights = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    ImGuiSystem.UpdateTexture(TextureBlendingWeightsIntPtr,
                        TextureBlendingWeights);
                }
            }
            NextColumn();
            DrawImage(TextureBlendingResultIntPtr, new Vector2(256.0f, 256.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TextureBlendingResultIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Blend Textures##BlendTextures"))//OpenPopup("New Terrain");
            {
                //check widths and heights
                if (TextureBlendingTex1.Width == 0 || TextureBlendingTex2.Width == 0 ||
                    TextureBlendingWeights.Width == 0)
                {
                    SwitchtoGeneralTab = 2;
                    MSGlog.Add2Log("You need to load in the blending textures and the weights before you perform blend operations!");
                    return;
                }
                TextureBlendingResult = TerrainScript.BlendTextures(
                    tcomp,GraphicsDevice, Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(TextureBlendingResultIntPtr, TextureBlendingResult);
            }

            if (Button("Save##TextureBlendingResult"))//OpenPopup("New Terrain");
            {
                if (TextureBlendingResult != null)
                {
                    FilenameType = "TextureBlendingResult";
                    MSGlog.Add2Log("Saving current Texture Blending Result.");
                    SaveTex(TextureBlendingResult, FilenameType + ".bmp", Game.GraphicsContext);
                }
            }
            if (Button("SaveAs##TextureBlendingResult"))
            {
                if (TextureBlendingResult != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save Texture Blending Result";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current Texture Blending Result image.");
                        SaveTex(TextureBlendingResult, theDialog.FileName.ToString(), Game.GraphicsContext, false);
                    }
                }
            }
            EndChild();
            Columns(1);
            Separator();
            Separator();
            #endregion pics
            if (Checkbox("Zoom in Textures", ref ZoomInTexture))
            {
            }
            SameLine();
            if (Checkbox("Show Texture Format", ref ShowTextureFormat))
            {
            }
            SameLine();
            bool IsGrayScaleHeightMap = PerlinNoise.IsGrayScaleHeightMap;
            if (Checkbox("Single Channel Image", ref IsGrayScaleHeightMap))
            {
                PerlinNoise.IsGrayScaleHeightMap = IsGrayScaleHeightMap;
            }
            SameLine();
            HelpMarker("A Single Channel HeightMap is a Gray Scale HeightMap, where all channels have the same value (and only the Red Channel is used for the height value), but this leads to 256 levels of height values. Uncheck this to get heights from all channels in the generated heightmap (leads to a short height -32,768 to 32,767, which yields 65,535 levels for much smoother height maps)");
            SameLine();
            if (Checkbox("Save after Creation", ref SaveGeneratedBMP))
            {
            }
            Separator();
            Separator();
            BeginChild("CreatedImageEditorGUI2", new Vector2(GetWindowWidth() * .95f,
                //GetWindowHeight() * .5f
                350), false, ImGuiWindowFlags.HorizontalScrollbar);
            Columns(2, null, false);
            TextWrapped("Original Image");
            DrawImage(CreatedImageSourceIntPtr, new Vector2(256.0f, 256.0f));
            if (IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(CreatedImageSourceIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Create##CreatedImage"))
            {
                FilenameType = "Image";
                Generate();
            }
            SameLine();
            HelpMarker("Creates a BMP image using the current randomization method, with the given filename and saves it if Save after creation is true.");
            SameLine();
            if (Button("Load##LoadCreatedImage"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load an Image";
                theDialog.Filter = "bmp files|*.*";
                //        string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    Texture texture = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    ImGuiSystem.UpdateTexture(CreatedImageSourceIntPtr, texture);
                }
            }
            SameLine();
            if (Button("SaveAs##CreatedImageLoaded"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save created image";
                    theDialog.Filter = "bmp files|*.bmp";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current image as BMP.");
                        SaveTex(ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr], theDialog.FileName.ToString(),
                            Game.GraphicsContext, false);
                    }
                }
            }
            SameLine();
            if (Button("BumpMap##BumpMapCreatedImage"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    Texture texture = ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr];
                    texture = Utility.CalculateBumpMap(texture,
                        PowValue, GraphicsDevice, Game.GraphicsContext);
                    ImGuiSystem.UpdateTexture(CreatedImageResultIntPtr, texture);
                }
            }
            SameLine();
            if (Button("NormalMap##NormalMapCreatedImage"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    Texture texture = ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr];
                    texture = Utility.CalculateNormalMap(texture,
                        PowValue, GraphicsDevice, Game.GraphicsContext);
                    ImGuiSystem.UpdateTexture(CreatedImageResultIntPtr, texture);
                }
            }
            SameLine();
            if (Button("Blur##BlurMapCreatedImage"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    MotionBlurFilter filter = new MotionBlurFilter();
                    Texture texture = ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr];
                    texture = texture.ConvolutionFilter<MotionBlurFilter>(
                        filter, GraphicsDevice, Game.GraphicsContext);
                    ImGuiSystem.UpdateTexture(CreatedImageResultIntPtr, texture);
                }
            }
            if (Button("Rotate##RotateCreatedImage"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    Texture texture = ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr];
                    texture = texture.Rotate(RotationAngle, GraphicsDevice,
                        Game.GraphicsContext);
                    ImGuiSystem.UpdateTexture(CreatedImageResultIntPtr, texture);
                }
            }
            SameLine();
            PushItemWidth(100);
            if (InputFloat("Angle", ref RotationAngle, 1, 50))
            {
                if (RotationAngle < 0) RotationAngle = 0;
                if (RotationAngle >360) RotationAngle = 360;
            }
            PopItemWidth();

            if (Button("Apply Filter##ApplyFilterCreatedImage"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr] != null)
                {
                    ConvolutionFilterBase filter =
                        (ConvolutionFilterBase)filterList[FilteringModeIndex];
                    Texture texture = ImGuiSystem._loadedTextures[CreatedImageSourceIntPtr];
                    texture = texture.ConvolutionFilter<ConvolutionFilterBase>(
                        filter, GraphicsDevice, Game.GraphicsContext);
                    ImGuiSystem.UpdateTexture(CreatedImageResultIntPtr, texture);
                }
            }
            SameLine();
            PushItemWidth(150);
            if (Combo("Filter Options", ref FilteringModeIndex, FilteringMode,
                FilteringMode.Length))
            {
            }
            PopItemWidth();

            NextColumn();
            TextWrapped("Resulting Image");
            DrawImage(CreatedImageResultIntPtr, new Vector2(256.0f, 256.0f));
            if (IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(CreatedImageResultIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("SaveAs##CreatedImageResulting"))
            {
                if (ImGuiSystem._loadedTextures[CreatedImageResultIntPtr] != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save created image";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current created image as BMP.");
                        SaveTex(ImGuiSystem._loadedTextures[CreatedImageResultIntPtr], theDialog.FileName.ToString(),
                            Game.GraphicsContext, false);
                    }
                }
            }
            EndChild();
            Columns(1);
        }

        static string[] TerrainTreeEditMode = {
        "Vegetation Type1","Vegetation Type2","Vegetation Type3"};
        static string[] TerrainGrassEditMode = {
        "Grass Type1","Grass Type2","Grass Type3"};
        public static int MaxTreeInstances = 500, CurrentTreeInstances=0,
            CurrentGrassInstances = 0, MaxGrassInstances=500,
            MaxTreeTypes= TerrainTreeEditMode.Length,
            MaxGrassTypes = TerrainGrassEditMode.Length;
        public static float Repulsion_Distance=10.0f, ObjectVisibility=50.0f;
        public static bool TreeEditorPlaceRandom = true, 
            ShowTrees = true,  ShowGrass = true, ShowWater=true,
            UseRepulsion = true, GrassEditorPlaceRandom=true;
        public static int  TerrainTreeModeSelected = 0, TerrainGrassModeSelected = 0;
        public static IntPtr TerrainPropertiesRoadIntPtr,
            TerrainPropertiesTreeLocsIntPtr, TerrainPropertiesTextureIntPtr
            , TerrainPropertiesCollisionIntPtr;
        public static Texture TerrainPropertiesRoad = new Texture(),
            TerrainPropertiesTreeLocs = new Texture(),
            TerrainPropertiesTexture = new Texture(),
            TerrainPropertiesCollision = new Texture();

        void ShowAreaEditorGUI()
        {
            if (tcomp == null) return;

            #region area info
            TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().
                AsNumericVec4(), "Area");
            PushItemWidth(150);//System.GetGraphicsClass()->GetWindowWidth()-80.0f);
            string Areaname= AreaXML.AreaName;
            if (InputText("Area Name, ", ref Areaname, 150))
            { AreaXML.AreaName = Areaname; }

            PopItemWidth();
            SameLine();
            if (Button("Load Area XML##LoadTerrainXML"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Area XML";
                theDialog.Filter = "xml files|*.xml";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditorAreas_Directory;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    var onlyFileName = System.IO.Path.GetFileName(theDialog.FileName);
                    AreaXML.AreaName = onlyFileName.Substring(0,
                    onlyFileName.IndexOf("."));
                    if(!string.IsNullOrEmpty(AreaXML.AreaName) )
                    AreaXML.LoadArea(tcomp);
                    tcomp.FullUpdate(TerrainScript.GetHeightMapTexture(), TerrainWeights1, TerrainWeights2);
                    MSGlog.Add2Log("Loaded Area " + AreaXML.AreaName + " from directory " +
                        Utility.Resources_TerrainEditorAreas_Directory + @"\" + AreaXML.AreaName);
                    Cursor.Current = Cursors.Default;
                }
            }
            SameLine();
            HelpMarker("Loads a new area from an xml file");
            SameLine();
            if (Button("Save Area as XML##SaveTerrainXML"))
            {
                //for testing

                /* AreaXML.TerrainHeightMap = "TerrainHeightMap";
                AreaXML.TerrainBlendedTexture = "TerrainHeightMap";
                AreaXML.AddModel("Models/Tree2/tree", Game);
                AreaXML.AddModel("Models/Tree2/tree", Game);
                new AreaObject("objectname", new Entity(),
                    new Vector3(2.3f, 2.1f, 6.3f), Vector3.UnitX, Vector3.One,
                    Vector4.One,AreaObjectType.Door);
                new AreaObject("objectname", new Entity(),
                    new Vector3(32.3f, 32.1f, 36.3f), Vector3.UnitX, Vector3.One,
                    Vector4.One, AreaObjectType.Door);
                new AreaObject("objectname2", new Entity(),
             new Vector3(12.3f, 342.1f, 46.3f), Vector3.UnitX, Vector3.One,
             Vector4.One, AreaObjectType.Door);*/
                Cursor.Current = Cursors.WaitCursor;
                AreaXML.SaveArea(tcomp);
                MSGlog.Add2Log("Saved Area "+ AreaXML.AreaName+" in directory "+
                    Utility.Resources_TerrainEditorAreas_Directory+ @"\" + AreaXML.AreaName);
                SaveTex(ImGuiSystem._loadedTextures[TerrainHeightMapTextureIntPtr], FilenameType + ".bmp", Game.GraphicsContext);
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            HelpMarker("Saves all objects in the current area to an xml file. Objects include the terrain itself (filenames of the heightmap texture, blended textures, etc), and any added models or objects you have added to the scene (saves their names, which must exist in the stride database at runtime).");

            TextWrapped("Area Information");
            TextWrapped("Terrain HeightMap: " + AreaXML.TerrainHeightMap);
            TextWrapped("Terrain BlendedTexture: " + AreaXML.TerrainBlendedTexture);
            bool showobjects= AreaObjectsView.show_AreaObjectsViewGUI;
            if (Checkbox("Show All Area Objects", ref showobjects))
            {
                AreaObjectsView.show_AreaObjectsViewGUI = showobjects;
                ShowTrees = true;
                ShowGrass = true;
                ShowWater = true; 
                AreaHandleModels.ToggleAreaModels(true);
            }
            SameLine();
            showobjects = StrideAssetsView.show_AllStrideAssetsViewGUI;
            if (Checkbox("Show All Stride Assets Loaded", ref showobjects))
            {
                StrideAssetsView.show_AllStrideAssetsViewGUI = showobjects;
            }
            #endregion area info
            Separator();
            Separator();
            TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().
                AsNumericVec4(), "Vegetation");
           // Columns(2, null, true);
            PushItemWidth(150);
            if (InputInt("Max # of Trees", ref MaxTreeInstances, 1, 50))
            {
                if (MaxTreeInstances < 0) MaxTreeInstances = 0;
                if (MaxTreeInstances > 10000) MaxTreeInstances = 10000;
            }
            PopItemWidth();
            SameLine();
            PushItemWidth(150);
            if (InputInt("Max # of Grass", ref MaxGrassInstances, 1, 50))
            {
                if (MaxGrassInstances < 0) MaxGrassInstances = 0;
                if (MaxGrassInstances > 10000) MaxGrassInstances = 10000;
            }
            PopItemWidth();

            TextWrapped("Current # of Trees is " +
                CurrentTreeInstances.ToString());
            TextWrapped("Current # of Grass is " +
                CurrentGrassInstances.ToString());
            TextWrapped("Total # of Tree Types is " + MaxTreeTypes.ToString());
            TextWrapped("Total # of Grass Types is " + MaxGrassTypes.ToString());
            PushItemWidth(200);
            SliderFloat("Repulsion Distance", ref Repulsion_Distance,
                0.01f, MathF.Sqrt(m_Width * m_Width + m_Height * m_Height));
            SameLine();
            if(SliderFloat("Object Visibility", ref ObjectVisibility, 
                20.0f, 1000.0f))
            {
              //  AreaHandleModels.ToggleAreaModels(
                  //      Game.Services.GetService<SceneSystem>().
                  //      GraphicsCompositor.Cameras[0].Camera);
            }
            PopItemWidth();
            if (Button("Remove All Objects##RemoveAllTerrainPropertiesTreeLocs"))
            {
//                TerrainEditModeSelected = 3;
                foreach (IAreaObject obj in AreaXML.GetAreaObjects())
                {
                    for (int i =obj.NumInstances-1;i>=0; i--)
                    {
                        AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                    }
                }
                AreaHandleModels.ResetPoints();
                TerrainEditorView.CurrentTreeInstances = 0;
                TerrainEditorView.CurrentGrassInstances = 0;
                MSGlog.Add2Log("All vegetation removed...");
            }

            // SameLine();
            if (Checkbox("Place Randomly Chosen Tree Type", ref TreeEditorPlaceRandom))
            {
            }
            PushItemWidth(150);
            if (!TreeEditorPlaceRandom)
            {
                SameLine();
                if (Combo("Select Vegetation Type", ref TerrainTreeModeSelected, TerrainTreeEditMode,
                TerrainTreeEditMode.Length))
                { }
            }
            PopItemWidth();

            if (Checkbox("Place Randomly Chosen Grass Type", ref GrassEditorPlaceRandom))
            {
            }
            PushItemWidth(150);
            if (!GrassEditorPlaceRandom)
            {
                SameLine();
                if (Combo("Select Grass Type", ref TerrainGrassModeSelected,
                    TerrainGrassEditMode, TerrainGrassEditMode.Length))
                { }
            }
            PopItemWidth();
            //Objectvisibility handles this every frame
/*
            if (Checkbox("Show Trees", ref ShowTrees))
            {
               AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
            }
            SameLine();
            if (Checkbox("Show Grass", ref ShowGrass))
            {
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
            }
            if (Checkbox("Show Water", ref ShowWater))
            {
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Water, ShowWater);
            }*/
            
            SameLine(); 
            if (Checkbox("Use Repulsion", ref UseRepulsion))
            { 
            }

            if (Button("Place Grass##PlaceGrass"))
            {
                TerrainEditModeSelected = 2;
                ShowGrass = true;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                Radius = 10;
                MSGlog.Add2Log("Radius set to 10 units...");
            }
            SameLine();
            if (Button("Place Trees##PlaceTrees"))
            {
                TerrainEditModeSelected = 3;
                ShowTrees = true;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                Radius = 10;
                MSGlog.Add2Log("Radius set to 10 units...");
            }
            SameLine();
            if (Button("Update Heights##TerrainPropertiesTreeLocsUpdateHeights"))
            {
              //  ShowTrees = true;
              //  ShowGrass = true;
              //  AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
              //  AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                Radius = 10;
                MSGlog.Add2Log("Radius set to 10 units...");
                AreaXML.UpdateObjectLocations(tcomp);
                MSGlog.Add2Log("Vegetation Heights updated...");
            }
            Separator();
            Separator();
            TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().
            AsNumericVec4(), "Basic Water Plane");
            Text("Dimensions");
            PushItemWidth(100);
            if (InputInt("Wide X", ref WaterWideX, 1, 500))
            {
            }    
            SameLine(200);
            if (InputInt("High Z", ref WaterHighZ, 1, 500))
            {
            }
            if (SliderFloat("Water Transparency", ref WaterTransparency, 0, 1))
            {
            }
            if (SliderFloat("Water Displacement Speed", ref DisplacementSpeed, 0, 10))
            {
            }
            SameLine();
            if (SliderFloat("Water Displacement Amplitude", ref DisplacementAmplitude, 0, 10))
            {
            }
            PopItemWidth();
            Text("Place Water at the current center of the selection ball...");
            SameLine();
            if (Button("Place Water##PlaceWater"))
            {
                // Texture texture = PerlinNoise.MakeFlat(WaterWideX, 
                //   WaterHighZ,Stride.Core.Mathematics.Color.Black).
                //   ToTexture(WaterWideX, WaterHighZ, Game.GraphicsDevice,
                //    Game.GraphicsContext.CommandList);
                // Create an entity and add it to the scene.
                var entity = new Entity("WaterModelComponent");
                WaterScript wt = new WaterScript();
                entity.Add(wt);
                wt.WaterTransparency = WaterTransparency;
                wt.DisplacementSpeed = DisplacementSpeed;
                wt.DisplacementAmplitude = DisplacementAmplitude;
                wt.Setup(Game.SceneSystem);
                Game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
                var model = new Model();
                entity.GetOrCreate<ModelComponent>().Model = model;
                var meshDraw = //wt.BuildWaterSurface(GraphicsDevice, 1, 1,
                               //  WaterWideX, WaterHighZ, true).ToMeshDraw();
                    GeometricPrimitive.Plane.New(GraphicsDevice,
                 WaterWideX, WaterHighZ, normalDirection: NormalDirection.UpY,
                 generateBackFace:true).ToMeshDraw();
                Mesh mesh =
                    new Mesh
                    {
                        Draw = meshDraw,
                        BoundingBox = new BoundingBox(
                            Stride.Core.Mathematics.Vector3.Zero,
                        new Stride.Core.Mathematics.Vector3(WaterWideX, 0, WaterHighZ))
                    };
                //      entity.GetOrCreate<ModelComponent>().RenderGroup = RenderGroup.Group0;
                model.Meshes.Add(mesh);
                if (model.Materials != null)
                    model.Materials.Clear();

                //load water material
                Material material = Content.Load<Material>(
                     //"Materials/Other/Sphere Material"
                     "Water/WaterMaterialShader"
                    //"Water/WaterMaterial"//loading this does not work at runtime, only if you place a plane in the editor and attach the material...
                    );
                model.Materials.Add(material);
                var normalPhase = wt.NormalTextureFlow.CurrentPhase;
                var diffusePhase = wt.DiffuseTextureFlow.CurrentPhase;
                material.Passes[0].Parameters.Set(TransformationKeys.WorldViewProjection, wt.World * wt.Camera.ViewProjectionMatrix);
                material.Passes[0].Parameters.Set(TransformationKeys.World, wt.World);
                material.Passes[0].Parameters.Set(GlobalKeys.Time, (float)Game.UpdateTime.Elapsed.TotalSeconds);
                material.Passes[0].Parameters.Set(GlobalKeys.TimeStep, (float)Game.UpdateTime.Elapsed.TotalSeconds);
                material.Passes[0].Parameters.Set(WaterShaderKeys.SkyTexture, TerrainEditorView.SkyTexture);
                material.Passes[0].Parameters.Set(WaterShaderKeys.WaterFloorTexture, TerrainEditorView.WaterFloorTexture);
                material.Passes[0].Parameters.Set(WaterShaderKeys.NoiseTexture, TerrainEditorView.NoiseMapTexture);
                material.Passes[0].Parameters.Set(WaterShaderKeys.FlowMapTexture, TerrainEditorView.FlowMapTexture);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseTexture0, TerrainEditorView.DiffuseTexture1);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseTexture1, TerrainEditorView.DiffuseTexture2);
                material.Passes[0].Parameters.Set(WaterShaderKeys.NormalTexture0, TerrainEditorView.NormalTexture1);
                material.Passes[0].Parameters.Set(WaterShaderKeys.NormalTexture1, TerrainEditorView.NormalTexture2);
                material.Passes[0].Parameters.Set(WaterShaderKeys.SunColor, wt.SunColor);
                material.Passes[0].Parameters.Set(WaterShaderKeys.CameraPosition, wt.Camera.Entity.Transform.Position);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DirectionToLight, Stride.Core.Mathematics.Vector3.Normalize(new Stride.Core.Mathematics.Vector3(2f, 2f, 4f)));
                material.Passes[0].Parameters.Set(WaterShaderKeys.WaterTransparency, wt.WaterTransparency);
                material.Passes[0].Parameters.Set(WaterShaderKeys.SkyTextureOffset, wt.SkyTextureOffset);
                material.Passes[0].Parameters.Set(WaterShaderKeys.NormalOffsets, wt.NormalTextureFlow.RandomOffsets);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseOffsets, wt.DiffuseTextureFlow.RandomOffsets);
                material.Passes[0].Parameters.Set(WaterShaderKeys.NormalPhase, new Stride.Core.Mathematics.Vector2((normalPhase + 0.5f) % 1, normalPhase));
                material.Passes[0].Parameters.Set(WaterShaderKeys.DiffusePhase, new Stride.Core.Mathematics.Vector2((normalPhase + 0.5f) % 1, normalPhase));
                material.Passes[0].Parameters.Set(WaterShaderKeys.NormalPulseReduction, wt.NormalTextureFlow.PulseReduction);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DiffusePulseReduction, wt.DiffuseTextureFlow.PulseReduction);
                material.Passes[0].Parameters.Set(WaterShaderKeys.TextureScale, wt.TextureScale);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DisplacementSpeed, wt.DisplacementSpeed);
                material.Passes[0].Parameters.Set(WaterShaderKeys.DisplacementAmplitude, wt.DisplacementAmplitude);
                material.Passes[0].Parameters.Set(WaterShaderKeys.UseCaustics, wt.UseCaustics ? 1 : 0);
                entity.Transform.Position = MultiTypeCameraController.ClickBallModelEntity.Transform.Position;
                new AreaObject("WaterPlane", entity,
                    entity.Transform.Position.AsNumericVec3(), Vector3.Zero, Vector3.One,
                    Vector4.One, AreaObjectType.Water);
                MSGlog.Add2Log("Water plane placed at the current selection ball center location...");
            }
            Separator();
        }
        public static float WaterTransparency = 0.85f;
        public static float DisplacementSpeed = 0.25f;
        public static float DisplacementAmplitude = 0.15f;
        public static int WaterWideX = 256, WaterHighZ = 256;

        public static bool RenderMesh = true, RenderWireFrame = false,
            ZoomInTexture = false, RenderPoints=false, ShowSelectionBall=true,
            ShowTextureFormat = false;
        public static float WireFrameLineWidth = 1.0f;
        public static Vector3 WireFrameLineColor = Vector3.UnitX;
        void ShowHUD()
        {  
       //     PushItemWidth(GetFontSize() * -12);

            Text("Application average " + (1000.0f / GetIO().Framerate).ToString("0.0") +
                "ms/frame (" + GetIO().Framerate.ToString("0.0") + " FPS)");
            if(tcomp==null) return; 
            #region camera
            CameraComponent camera = Game.Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera;
            if (camera != null)
            {
                Vector3 pos = camera.Entity.Transform.Position.AsNumericVec3();
                if (InputFloat3("Camera Position", ref pos))
                {
                    camera.Entity.Transform.Position = pos.AsStrideVec3();
                }
            }
            //SameLine(GetWindowWidth()*0.7f);
  //          Text("Camera Position: x=%.3f y=%.3f z=%.3f", vpos.x, vpos.y, vpos.z);
            SameLine();
            HelpMarker("Left Ctrl+R: Reset Camera to Original Settings");
            if (camera != null)
            {
                Vector3 pos = camera.Entity.Transform.RotationEulerXYZ.AsNumericVec3();
                if (InputFloat3("Camera Rotation (Euler)", ref pos))
                {
                    camera.Entity.Transform.RotationEulerXYZ = pos.AsStrideVec3();
                }

            }
            if (camera != null)
            {
                float speed = MultiTypeCameraController.CameraSpeed;
                if (SliderFloat("Camera Speed", ref speed, 1.0f, 1000.0f))
                {
                    MultiTypeCameraController.CameraSpeed = speed;
                }
            }
            if (camera != null)
            {
                float val = camera.AspectRatio;
                if (SliderFloat("Camera Aspect Ratio", ref val, 1.0f, 1000.0f))
                {
                    camera.AspectRatio = val;
                }
            }
            if (camera != null)
            {
                float val = camera.VerticalFieldOfView;
                if (SliderFloat("Camera Vertical Field Of View", ref val, 0.1f, 100.0f))
                {
                    camera.VerticalFieldOfView = val;
                }
            }
            if (camera != null)
            {
                float val = camera.NearClipPlane;
                if (SliderFloat("Camera Near Clip Plane", ref val, 0.1f, 100.0f))
                {
                    camera.NearClipPlane = val;
                }
            }
            if (camera != null)
            {
                float val = camera.FarClipPlane;
                if (SliderFloat("Camera Far Clip Plane", ref val, 100.0f, 10000.0f))
                {
                    camera.FarClipPlane = val;
                }
            }
            #endregion camera

            Separator();
            Entity TerrainEnt = Game.SceneSystem.SceneInstance.FirstOrDefault(
                e => e.Name == "TerrainComponentModelComponent");
            WireframeScript scr = TerrainEnt.Get<WireframeScript>();
            if (Checkbox("Render Terrain Mesh", ref RenderMesh))
            {
                tcomp.ToggleVisible(RenderMesh);
               /* if (!RenderMesh)
                {
                    RenderWireFrame = false;
                    scr.Enabled = false;
                    tcomp.ShowWireframe = false;
                }*/
            }
            SameLine();
            if (Checkbox("Render WireFrame", ref RenderWireFrame))
            {
                scr.Enabled = RenderWireFrame;
                tcomp.ShowWireframe = RenderWireFrame;
            }
            SameLine();
            if (Checkbox("Render Points", ref RenderPoints))
            {
                //tcomp.ShowPoints = RenderPoints;
                Entity Cubeinstancing_Entity = Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "CubeInstancing");
                CubeInstancingRenderScript instancecubescript = Cubeinstancing_Entity.Get<CubeInstancingRenderScript>();
                instancecubescript.Enabled = RenderPoints;
                if (RenderPoints)
                {
                    instancecubescript.InitializeInstanceBuffersPosCol(
                        tcomp, GraphicsDevice);
                    ModelComponent comp= instancecubescript.Entity.Get<ModelComponent>();
                    //comp.Model = Content.Load<Model>("CubeModelInstancing");
                    comp.Enabled = true;
                }
                else
                {
                    ModelComponent comp = instancecubescript.Entity.Get<ModelComponent>();
                    comp.Enabled = false;
                    //comp.Model = null;
                }
            }
            SameLine();
            HelpMarker("Potential FPS reduction...");
            SameLine();
            if (Checkbox("Zoom in Textures", ref ZoomInTexture))
            {
            }
            if (Checkbox("Show Texture Format", ref ShowTextureFormat))
            {
            }
            WireFrameLineWidth = scr.LineWidth;
            if (SliderFloat("Line Width", ref WireFrameLineWidth, 0.1f, 10.0f))
            {
                   scr.LineWidth = WireFrameLineWidth;
            //    tcomp.Material.Passes[0].Parameters.Set(TerrainBlendShaderKeys.LineWidth, TerrainEditorView.WireFrameLineWidth);// tcomp.TerrainTexture8);
            }
            WireFrameLineColor = scr.Color.ToVector3().AsNumericVec3();
            if (ColorEdit3("Line Color", ref WireFrameLineColor))
            {
             //   tcomp.Material.Passes[0].Parameters.Set(TerrainBlendShaderKeys.LineColor, TerrainEditorView.WireFrameLineColor.AsStrideVec3());// tcomp.TerrainTexture8);
                scr.Color=new Stride.Core.Mathematics.Color3(
                    WireFrameLineColor.X, WireFrameLineColor.Y, WireFrameLineColor.Z);
            }
            Separator();
            #region lights
            Entity ambientlightEnt = Game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault
                (c => c.Name == "Ambient light");
            if (ambientlightEnt != null)
            {
                LightComponent ambientlight = ambientlightEnt.Get<LightComponent>();
                Stride.Core.Mathematics.Color3 c3 = ambientlight.GetColor();
                Vector3 color = new Vector3(c3.R, c3.G, c3.B);
                if (ColorEdit3("Ambient Color", ref color))
                {
                    ambientlight.SetColor(new Stride.Core.Mathematics.Color3(
                        color.X, color.Y, color.Z));
                }
            }
            Entity DirectionalEnt = Game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault
    (c => c.Name == "Directional light");
            if (DirectionalEnt != null)
            {
                LightComponent Directionallight = DirectionalEnt.Get<LightComponent>();
                Stride.Core.Mathematics.Color3 c3 = Directionallight.GetColor();
                Vector3 color = new Vector3(c3.R, c3.G, c3.B);
                if (ColorEdit3("Directional Light Color", ref color))
                {
                    Directionallight.SetColor(new Stride.Core.Mathematics.Color3(
                        color.X, color.Y, color.Z));
                }
                Vector3 vec = DirectionalEnt.Transform.Position.AsNumericVec3();
                if (InputFloat3("Directional Light Position", ref vec))
                {
                    DirectionalEnt.Transform.Position = vec.AsStrideVec3();
                }
                vec = DirectionalEnt.Transform.RotationEulerXYZ.AsNumericVec3();
                if (InputFloat3("Directional Light Rotation", ref vec))
                {
                    DirectionalEnt.Transform.RotationEulerXYZ = vec.AsStrideVec3();
                }
            }
            #endregion lights
        }

        public static AppLog MSGlog=new AppLog();
        void ShowMessagesGUI()
        {
            MSGlog.Draw();
        }
   
        //tabed items in bot right window
        void ShowSpecificInfoGUI()
        {
            SetNextWindowPos(new Vector2(xloc, yloc), ImGuiCond.Appearing);
            SetNextWindowSize(new Vector2(Game.GraphicsContext.CommandList.Viewport.Width
                / 2.0f, Game.GraphicsContext.CommandList.Viewport.Height
                / 2.0f), ImGuiCond.Appearing);
            if (!Begin("Specific Editor Information", ImGuiWindowFlags.HorizontalScrollbar))
            {
                End();
                return;
            }

            if (BeginTabBar("##footabtitlespecific", 0))
            {
                bool p_open = true;
                ImGuiTabItemFlags flags0 = ImGuiTabItemFlags.None,
                    flags1 = ImGuiTabItemFlags.None, flags2 = ImGuiTabItemFlags.None,
                    flags3 = ImGuiTabItemFlags.None, flags4 = ImGuiTabItemFlags.None
                    , flags5 = ImGuiTabItemFlags.None;
                switch (SwitchtoSpecificEditorTab)
                {
                    case 0: flags0 = ImGuiTabItemFlags.SetSelected; break;
                    case 1: flags1 = ImGuiTabItemFlags.SetSelected; break;
                    case 2: flags2 = ImGuiTabItemFlags.SetSelected; break;
                    case 3: flags3 = ImGuiTabItemFlags.SetSelected; break;
                    case 4: flags4 = ImGuiTabItemFlags.SetSelected; break;
                    case 5: flags5 = ImGuiTabItemFlags.SetSelected; break;
                }

                if (BeginTabItem("Render Info", ref p_open, flags0))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 0;
                    }
                    //ImGuiNative.igIsItemActive();
                    Separator();
                    Separator();
                    //       ShowHUD();
                    EndTabItem();
                }
                if (BeginTabItem("Terrain Resources", ref p_open, flags1))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 1;
                    }
                    Separator();
                    Separator();
                    // ShowTerrainResourcesGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Texture Blend", ref p_open, flags2))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 2;
                    }
                    Separator();
                    Separator();
                    // ShowTextureBlendEditorGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Texture Edit", ref p_open, flags3))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 3;
                    }
                    Separator();
                    Separator();
                    //   ShowTextureEditGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Picking", ref p_open, flags4))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 4;
                    }
                    Separator();
                    Separator();
                    //  ShowPickingGUI();
                    EndTabItem();
                }
                if (BeginTabItem("Model Editor", ref p_open, flags5))
                {
                    if (IsItemHovered())// IsMouseClicked(0))
                    {
                        SwitchtoSpecificEditorTab = 5;
                    }
                    Separator();
                    Separator();
                    // ShowModelGUI();
                    EndTabItem();
                }

                EndTabBar();
            }

            End();
        }

        public static int GenMethod = 20,Octave = 5, Type = 0, Type2 = 0,
            Voronoi_num_vertices = 100;
        public static float Persistance = 1.0f, PowValue = 1.0f, Freq = 1.0f, FreqX = 1.0f, FreqZ = 1.0f,
            ShiftX = 0, ShiftZ = 0, VarianceX = 100.0f, VarianceZ = 100.0f,
            Error = 0, NormalizationConst = 1.0f, PixelCutoff = 0, TargetHeightValue = 0,
            Mincutoff = 0, Maxcutoff = 1.0f, Radius = 30.0f,
            BallSelectionStrength=0.3f, BallSelectionPower=25.0f;
        public static Vector3 ColorStart = new Vector3(0, 0, 0), ColorEnd = Vector3.One;
        public static Vector4 BallColor = new Vector4(0, 0, 1, 1);
        public static Vector2 LocationStart = Vector2.Zero, LocationEnd = Vector2.One;
        static string[] GenMethods = {
        "Perlin Noise",//0
		"Perlin Noise (red channel)",//1 
		"Perlin Noise (green channel)",//2
		"Perlin Noise (blue channel)",//3
		"Perlin Noise for each channel",//4
		"Fixed with solid color (Start Color field used)",//5
		"Add uniform error to current",//6
		"Cloud (Low-High=Blue-White)",//7
		"Colored Transitions (smooth from Start to End color, e.g., alpha mapping)",//8
		"Moisture (Low-High=Black-Blue)",//9
		"Random Cloud Interpolation (Low-High=Start Color-End Color)",//10
		"Voronoi Tesselation (Without boundary, blend textures)",//11
		"Perlin Bands (black within cutoffs, white otherwise, e.g., Rivers)",//12
		"Shade Transitions (smooth from black to white, e.g., alpha mapping)",//13
		"Elevation Map: Ridged Mountains",//14
		"Elevation Map: Bivariate Normal Mixtures (Hills)",//15
		"Elevation Map: Bivariate Normals Fixed Means (Hills)",//16
		"Elevation Map: Curve based",//17
		"Elevation Map: Single Bivariate Normal",//18
		"Elevation Map: Deserts (Perlin Based, Ridged)",//19
		"Elevation Map: Mountain",//20
		"Biome World Map (Perlin Based)",//21
        "Biome World Map (Voronoi Based)",//22
        "Voronoi Tesselation (With boundary)",//23
     };
        static string[] TypeMethods = {
        "Circle Curve",//0
		"Cosine Curve",//1 
		"Sine Curve",//2
		"Astroid Curve",//3
		"Folium of Descartes",//4
		"Involute of a Circle",//5
		"Nephroid Curves",//6
		"Witch of Agnesi",//7
		"Snake Curve",//8
	 };
        static string[] Type2Methods = {
        "Bell Shape",//0
		"Exponential Decay",//1 
		"Mixture",//2
	 };

        public static void ResetRandomizationValues()
        {
            Voronoi_num_vertices = 100;
            Octave = 5; Type = 0; Type2 = 0;
            PowValue = 1.0f; Freq = 1.0f; FreqX = 1.0f; FreqZ = 1.0f;
            ShiftX = 0; ShiftZ = 0; VarianceX = 100.0f; VarianceZ = 100.0f;
            Error = 0; NormalizationConst = 1.0f; PixelCutoff = 0; TargetHeightValue = 0;
            Mincutoff = 0; Maxcutoff = 1.0f; Radius = 10.0f; Persistance = 1.0f;
            ColorStart = Vector3.Zero; ColorEnd = Vector3.One;
            LocationStart = Vector2.Zero; LocationEnd = Vector2.One;
            GenMethod = 0;
        }
        void ShowRandomizationGUI()
        {
            Columns(2, null, true);

            if (Button("Reset Randomization Values"))
            {
                Voronoi_num_vertices = 100;
                Octave = 5; Type = 0; Type2 = 0;
                PowValue = 1.0f; Freq = 1.0f; FreqX = 1.0f; FreqZ = 1.0f;
                ShiftX = 0; ShiftZ = 0; VarianceX = 100.0f; VarianceZ = 100.0f;
                Error = 0; NormalizationConst = 1.0f; PixelCutoff = 0; TargetHeightValue = 0;
                Mincutoff = 0; Maxcutoff = 1.0f; Radius = 10.0f; Persistance = 1.0f;
                ColorStart = Vector3.Zero; ColorEnd = Vector3.One;
                LocationStart = Vector2.Zero; LocationEnd = Vector2.One;
                GenMethod = 0;
            }

            if (Button("Generate HeightMap##GenHeightmap"))
            {
                FilenameType = "HeightMap";
                Generate();
            }
            SameLine();
            bool IsGrayScaleHeightMap = PerlinNoise.IsGrayScaleHeightMap;
            if (Checkbox("Single Channel", ref IsGrayScaleHeightMap))
            {
                PerlinNoise.IsGrayScaleHeightMap = IsGrayScaleHeightMap;
            }
            SameLine();
            HelpMarker("A Single Channel HeightMap is a Gray Scale HeightMap, where all channels have the same value (and only the Red Channel is used for the height value), but this leads to 256 levels of height values. Uncheck this to get heights from all channels in the generated heightmap (leads to a short height -32,768 to 32,767, which yields 65,535 levels for much smoother height maps)");
            if (Checkbox("Save after Creation", ref SaveGeneratedBMP))
            {
            }
            SameLine();
            HelpMarker("After any generation operation, saves a BMP file corresponding to the texture created.");
            if (Button("Create an Image##CreatedImageRandomization"))
            {
                FilenameType = "Image";
                Generate();
                //SwitchtoGeneralTab = 4;
            }
            SameLine();
            HelpMarker("Creates a BMP image using the current randomization method, with the given filename and saves it if Save after creation is true.");
            DrawImage(CreatedImageSourceIntPtr, new Vector2(64.0f, 64.0f));
            if (IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(CreatedImageSourceIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            NextColumn();
            BeginChild("texturesrandomize", new Vector2(400, 150), true,
                ImGuiWindowFlags.HorizontalScrollbar);

            Columns(3, null, false);
            Text("Heightmap");// +": " + TerrainTextures[i].Name);
 //           SameLine();
            DrawImage(TerrainHeightMapTextureIntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture&& IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainHeightMapTextureIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }           
            NextColumn();

            Text("Weights1");// +": " + TerrainTextures[i].Name);
            DrawImage(TerrainWeights1IntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainWeights1IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            NextColumn();

            Text("Weights2");// +": " + TerrainTextures[i].Name);
            DrawImage(TerrainWeights2IntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainWeights2IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }

            EndChild();
            
            Columns(1);
            Separator();

            PushItemWidth(350);
            Combo("Choose a Generating Method", ref GenMethod, GenMethods, GenMethods.Length);
           // SameLine();
          //  Text(GenMethod.ToString());
            PopItemWidth();

            PushItemWidth(GetWindowWidth() * 0.7f);//System.GetGraphicsClass()->GetWindowWidth()-80.0f);

            if (SliderFloat("Power Value", ref PowValue, 0.001f, 10.0f))// -50.0f, 50.0f);
            {
     //          if (PowValue < -100.0f) PowValue = -100.0f;
      //          if (PowValue > 100.0f) PowValue = 100.0f;
            }

            PushItemWidth(350);
            Combo("Choose a Curve Type", ref Type, TypeMethods, TypeMethods.Length);
           // SameLine();
           // Text(Type.ToString());
            PopItemWidth();

            /* if (InputInt("Type", ref Type, 1, 5))
             {
                 if (Type < 0) Type = 0;
                 if (Type > 15) Type = 15;
             }
             SameLine();
             HelpMarker("Curve Types; 0=Circular, 1=Cosine, 2=Sine, 3=Astroid, 4=Folium of Descartes, 5=Involute of a Circle, 6=Nephroid Curves, 7=Witch of Agnesi Curves, >7=snake curve ",
                 new Vector4(255, 255, 0, 1), "(?)");*/

            PushItemWidth(350);
            Combo("Choose a Surface Type", ref Type2, Type2Methods, Type2Methods.Length);
            //SameLine();
          //  Text(Type2.ToString());
            PopItemWidth();
            SliderInt("Vertices Number", ref Voronoi_num_vertices, 2, 1000);
            //  if (InputInt("Type 2", ref Type2, 1, 5))            {                if (Type2 < 0) Type = 0;                if (Type2 > 15) Type = 15;            }
            SliderFloat("Shift X-coord", ref ShiftX, -500.0f, 500.0f);
            SliderFloat("Shift Z-coord", ref ShiftZ, -500.0f, 500.0f);
            SliderFloat("Variability X-coord", ref VarianceX, 0.001f, 250.0f);
            SliderFloat("Variability Z-coord", ref VarianceZ, 0.001f, 250.0f);
            PopItemWidth();

            ColorEdit3("Start Color", ref ColorStart);
            ColorEdit3("End Color", ref ColorEnd);
            if (InputFloat2("Start Location", ref LocationStart))
            {
                if (LocationStart.X < 0.0f) LocationStart.X = 0.0f;
                if (LocationStart.X > 1.0f) LocationStart.X = 1.0f;
                if (LocationStart.Y < 0.0f) LocationStart.Y = 0.0f;
                if (LocationStart.Y > 1.0f) LocationStart.Y = 1.0f;
            }
            if (InputFloat2("End Location", ref LocationEnd))
            {
                if (LocationEnd.X < 0.0f) LocationEnd.X = 0.0f;
                if (LocationEnd.X > 1.0f) LocationEnd.X = 1.0f;
                if (LocationEnd.Y < 0.0f) LocationEnd.Y = 0.0f;
                if (LocationEnd.Y > 1.0f) LocationEnd.Y = 1.0f;
            }

            if (InputFloat("Frequency", ref Freq, 0.1f, 1.0f))
            {
                if (Freq < 0.01f) Freq = 0.01f;
                if (Freq > 100.0f) Freq = 100.0f;
            }
            SliderInt("Octave", ref Octave, 1, 10);

            if (SliderFloat("Persistance", ref Persistance, 0.0f, 10.0f))
            {
                if (Persistance < 0.01f) Persistance = 0.01f;
                if (Persistance > 100.0f) Persistance = 100.0f;
            }

            if (InputFloat("Random Error", ref Error, 0.1f, 100.0f))
            {
                if (Error < 0.0f) Error = 0.0f;
                if (Error > 100.0f) Error = 100.0f;
            }

            SliderFloat("Normalization", ref NormalizationConst, 0.1f, 10.0f);
            SliderFloat("X frequency", ref FreqX, -5.0f, 5.0f);
            SliderFloat("Z frequency", ref FreqZ, -5.0f, 5.0f);
            SliderFloat("Pixel Cutoff", ref PixelCutoff, 0.0f, 1.0f);
            SliderFloat("Minimum Pixel Cutoff", ref Mincutoff, 0.0f, 1.0f);
            SliderFloat("Maximum Pixel Cutoff", ref Maxcutoff, 0.0f, 1.0f);

            SliderFloat("Target Height Value", ref TargetHeightValue, -250.0f, 250.0f);
        }

        public static void HelpMarker(string desc)
        {
            HelpMarker(desc, "(?)", new Vector4(255, 255, 0, 1));
        }
        public static void HelpMarker(string desc,string textdisable, Vector4 color)
        {
            TextDisabled(textdisable);
            if (IsItemHovered())
            {
                BeginTooltip();
                PushTextWrapPos(GetFontSize() * 35.0f);
                TextColored(color, desc);
                //TextUnformatted(desc);
                PopTextWrapPos();
                EndTooltip();
            }
        }
        public void DrawSlider(int x, int y, int width, int height, float min,
            float max, ref float value, string label = "")
        {
            SetNextWindowPos(new Vector2(x - 2, y - 2));
            Begin("slider1", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground);
            SetNextItemWidth(width + 2);
            var style = GetStyle();
            var framePadding = style.FramePadding;
            style.FramePadding = new Vector2(0, (height - GetFontSize() + 1) * 0.5f);
            SliderFloat(label, ref value, min, max, "");
            style.FramePadding = framePadding;
            End();
        }

        public static int texrepeat = 1, Selectedtexture =0,
            TerrainNumofTextures =8, TerrainHeightBasedNumofTextures=10,
            TerrainLOD = 1, TerrainEditModeSelected = 0,
            TerrainDisplayModeSelected = 0;
        public static bool EditingActive = false;
        static string FilenameType = "HeightMap", SelectedTextureFilename="" ;
        static string[] FilenameTypes = new string[] { "HeightMap",
        "Texture","Weights1","Weights2"};
        public static int m_Width = 1024, m_Height = 1024; //lenx=1024,leny= 1024;
        public static float quadlenx = 1.0f, quadlenz = 1.0f;
        public static IntPtr[] TerrainTexturesIntPtr = new IntPtr[TerrainNumofTextures];
        public static IntPtr[] TerrainHeightBasedTexturesIntPtr = new IntPtr[TerrainHeightBasedNumofTextures];
        public static IntPtr TerrainDetaiMapTextureIntPtr,TerrainHeightMapTextureIntPtr, TerrainWeights1IntPtr,
            TerrainWeights2IntPtr, TerrainBlendedTextureIntPtr,
            CreatedImageSourceIntPtr, CreatedImageResultIntPtr;
        public static Texture TerrainWeights1=new Texture(), 
            TerrainWeights2 = new Texture(),
            TerrainBlendedTexture = new Texture();
        static string[] TerrainEditMode = {
        "Edit Locations",//0
		"Paint Textures",//1 
		"Place Grass",//2 
		"Place Trees",//3 
		"Place Water",//4 
		//"Place Collisions",//5 
		//"Game Mode",//6
	 };
        static string[] TerrainDisplayMode = {
        "Single Texture",//0
		"Height Based",//1 
		"Multi Pass",//2
	 };

        public static void CreateFlatWeights(TerrainComponent tcomp)
        {
            TerrainWeights1 = PerlinNoise.MakeFlat(m_Width, m_Height,
                new Stride.Core.Mathematics.Color(255, 0, 0, 0)).ToTexture(
                m_Width, m_Height, tcomp.GraphicsDevice,
                tcomp.Game.GraphicsContext.CommandList);
            ImGuiSystem.UpdateTexture(TerrainWeights1IntPtr, TerrainWeights1);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainWeights1);
            TerrainWeights2 = PerlinNoise.MakeFlat(m_Width, m_Height,
            new Stride.Core.Mathematics.Color(0, 0, 0, 0)).ToTexture(
             m_Width, m_Height, tcomp.GraphicsDevice,
             tcomp.Game.GraphicsContext.CommandList);
            ImGuiSystem.UpdateTexture(TerrainWeights2IntPtr, TerrainWeights2);
            tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainWeights2);
            TerrainScript.SelectedPoints = new bool[m_Width * m_Height];
        }
        private void ShowTerrainEditorGUI()
        {
            #region terrain
            Columns(2, null, true);
            //            Separator();
            if (Button("New##NewTerrain"))//OpenPopup("New Terrain");
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                if (MessageBox.Show("This operation will reset/overwrite most textures (heightmaps, weights etc) and you will lose your work! Are you sure you want to proceed?",
                    "Attention!", MessageBoxButtons.OKCancel) ==DialogResult.Cancel)
                { return; }
                if (m_Width * 3 % 4 != 0 || m_Height * 3 % 4 != 0)
                {
                    MSGlog.Add2Log("ERROR: New terrain does not have width or height divisible by 4");
                    m_Width = 1024;
                    m_Height = 1024;
                    return;
                }
                if (m_Width < 32 || m_Width > 1024 || m_Height < 32 || m_Height > 1024)
                {
                    MSGlog.Add2Log("Bad range of values; use 32-1024.");
                    m_Width = 1024;
                    m_Height = 1024;
                    return;
                }
                else
                {
                    foreach (IAreaObject obj in AreaXML.GetAreaObjects())
                    {
                        for (int i = obj.NumInstances - 1; i >= 0; i--)
                        {
                            AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                        }
                    }
                    AreaHandleModels.ResetPoints();
                    TerrainEditorView.CurrentTreeInstances = 0;
                    TerrainEditorView.CurrentGrassInstances = 0;

                    //SwitchtoGeneralTab = 2;//messages
                    FilenameType = "HeightMap";
                    SwitchtoSpecificEditorTab = 0;//terrain
                    MSGlog.Add2Log("CREATING NEW TERRAIN...");
                    PerlinNoise.IsGrayScaleHeightMap = false;
                    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    Texture texture = PerlinNoise.MakeFlat(m_Width, m_Height,
                     (0.0f).AsStrideColor()
//                        new Stride.Core.Mathematics.Color(ColorStart.X,ColorStart.Y, ColorStart.Z)
                    ).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
                    tcomp.Width = m_Width;
                    tcomp.Height = m_Height;
                    tcomp.m_QuadSideWidthX = quadlenx;
                    tcomp.m_QuadSideWidthZ = quadlenz;
                    tcomp.TEXTURE_REPEAT = texrepeat;

                    CreateFlatWeights(tcomp);
                    tcomp.FullUpdateAll(texture,TerrainWeights1, TerrainWeights2); //terrain.Heightmap;
       //             tcomp.TerrainEntity.Add(new WireframeScript());
                    CubeInstancingRenderScript.UpdatingMode = 0;
                    AreaXML.UpdateObjectLocations(tcomp); // TerrainScript.UpdateObjectLocations(Game.GraphicsContext, GraphicsDevice);
                    ShowTrees = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                    ShowGrass = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);

                    if (SaveGeneratedBMP)
                    {
                        MSGlog.Add2Log("Terrain with specified dimensions created and heightmap was saved.");
                        SaveTex(texture, FilenameType + ".bmp", Game.GraphicsContext);
                    }
                    else
                        MSGlog.Add2Log("Terrain with specified dimensions created and heightmap was NOT saved");
                    if (ImGuiSystem._loadedTextures.ContainsKey(TerrainHeightMapTextureIntPtr))
                    {
                        ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
                    }
                    else
                        ImGuiSystem._loadedTextures.Add(TerrainHeightMapTextureIntPtr, texture);
                    //                    TerrainScript.Update(Game.GraphicsDevice, texture, Game.GraphicsContext.CommandList,Game.Content);
                }
            }
            SameLine();
            HelpMarker("Creates a new terrain with properties listed below and default Height, Color, and Moisture Maps that you can change later at the terrain resources dialog.Size Range Alllowed 32-1024.New terrain must have width and height divisible by 4.");
            SameLine();

            bool IsGrayScaleHeightMap = PerlinNoise.IsGrayScaleHeightMap;
            if (Checkbox("Single Channel Image", ref IsGrayScaleHeightMap))
            {
                PerlinNoise.IsGrayScaleHeightMap = IsGrayScaleHeightMap;
            }
            SameLine();
            HelpMarker("A Single Channel HeightMap is a Gray Scale HeightMap, where all channels have the same value (and only the Red Channel is used for the height value), but this leads to 256 levels of height values. Uncheck this to get heights from all channels in the generated heightmap (leads to a short height -32,768 to 32,767, which yields 65,535 levels for much smoother height maps)");
            if (Checkbox("Save after Creation", ref SaveGeneratedBMP))
            {
            }
            SameLine();
            HelpMarker("After any generation operation, saves a BMP file corresponding to the texture created.");

            PushItemWidth(150);
            if (Combo("Edit Mode", ref TerrainEditModeSelected, TerrainEditMode,
                TerrainEditMode.Length))
            {
                if (TerrainEditModeSelected == 0)
                {
                    FilenameType = "HeightMap";
                    ShowTrees = false;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                    ShowGrass = false;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                    ShowWater = false;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Water, ShowGrass);
                }
                else if (TerrainEditModeSelected == 1)
                    FilenameType = "TextureWeights";
                else if (TerrainEditModeSelected == 2 || TerrainEditModeSelected == 3 ||
                    TerrainEditModeSelected == 4 || TerrainEditModeSelected == 5)
                {
                    FilenameType = "Properties";
                    SwitchtoGeneralTab = 5;
                    ShowTrees = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                    ShowGrass = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                    Radius = 10;
                    MSGlog.Add2Log("Radius set to 10 units...");
                }
            }
            // SameLine();
            //   Text(TerrainEditModeSelected.ToString());
            PopItemWidth();
            SameLine();
            HelpMarker("Affects how the terrain mesh vertices are modified: in location mode operations with LMB, RMB and MMB change the location values of the vertex. In Paint mode, the weights of each texture are changed for the selected vertices and for the currently selected texture.");
            SameLine();
            PushItemWidth(150);
            if (Combo("Display Mode", ref TerrainDisplayModeSelected, TerrainDisplayMode,
                TerrainDisplayMode.Length))
            {
                // tcomp.MaterialBlend.Passes[0].Parameters.Set(TerrainBlendShaderKeys.BlendType, TerrainEditorView.TerrainDisplayModeSelected);
                if (TerrainDisplayModeSelected == 0)
                {
                    ModelComponent mod = tcomp.TerrainEntity.Get<ModelComponent>();
                    //   Material material = Content.Load<Material>("Terrain/TerrainObjectMaterialShader");
                    mod.Model.Materials.Clear();
                    tcomp.CurrentMaterialName = "MaterialBlendSingle";
                    mod.Model.Materials.Add(tcomp.MaterialBlendSingle);
                    texrepeat = 1;
                    tcomp.TEXTURE_REPEAT = 1;
                }
                else if (TerrainDisplayModeSelected == 1)
                {
                    ModelComponent mod = tcomp.TerrainEntity.Get<ModelComponent>();
                    mod.Model.Materials.Clear();
                    tcomp.CurrentMaterialName = "MaterialBlendHeight";
                    //   tcomp.MaterialBlendHeight = Content.Load<Material>("Terrain/TerrainHeightMapMaterialShader");
                    //  tcomp.SetMaterial();
                    mod.Model.Materials.Add(tcomp.MaterialBlendHeight);
                    texrepeat = 1;
                    tcomp.TEXTURE_REPEAT = 1;
                }
                else if (TerrainDisplayModeSelected == 2)
                {
                    ModelComponent mod = tcomp.TerrainEntity.Get<ModelComponent>();
                    Material material = Content.Load<Material>(
                        // "Terrain/BlendTerrainMaterial"
                        "Terrain/TerrainMultiBlendMaterialShader");
                    mod.Model.Materials.Clear();
                    tcomp.CurrentMaterialName = "MaterialBlendMulti";
                    mod.Model.Materials.Add(tcomp.MaterialBlendMulti);
                    texrepeat = 0;
                    tcomp.TEXTURE_REPEAT = 0;
                }
                TerrainLOD = 1;
                tcomp.TerrainLOD = 1;
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);

            }
            // SameLine();
            //   Text(TerrainEditModeSelected.ToString());
            PopItemWidth();
            SameLine();
            HelpMarker("Affects how the terrain texture is displayed. Single mode uses a single texture for the whole terrain. Height based uses height and distance from camera to determine the texture. Multi Blend uses weighted per vertex colors and allows us to paint per vertex, so texture repeat is set to 0 so we cover the whole mesh and paint per quad.");

            if (Button("Smooth All Heights"))
            {
                Cursor.Current = Cursors.WaitCursor;
                m_Width = TerrainScript.GetHeightMapTexture().Width;
                m_Height = TerrainScript.GetHeightMapTexture().Height;
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "HeightMap";
                Texture texture = PerlinNoise.PerlinSmooth(
                    tcomp, NormalizationConst).ToTexture(m_Width, m_Height, Game.GraphicsDevice,
                    Game.GraphicsContext.CommandList);
                if (SaveGeneratedBMP)
                {
                    MSGlog.Add2Log("Smoothed current image using nearest neighbor values");
                    SaveTex(texture, FilenameType + ".bmp", Game.GraphicsContext);
                }
                else
                    MSGlog.Add2Log("Smoothed current image using nearest neighbor values but was NOT saved");
                tcomp.FullUpdate(texture, TerrainWeights1, TerrainWeights2);
                if (ImGuiSystem._loadedTextures.ContainsKey(TerrainHeightMapTextureIntPtr))
                {
                    ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
                }
                else
                    ImGuiSystem._loadedTextures.Add(TerrainHeightMapTextureIntPtr, texture);
                CubeInstancingRenderScript.UpdatingMode = 0;
                AreaXML.UpdateObjectLocations(tcomp);
                Cursor.Current = Cursors.Default;
            }
            SameLine();
            if (Button("Smooth All Weights"))
            {
                Cursor.Current = Cursors.WaitCursor;
                m_Width = TerrainScript.GetHeightMapTexture().Width;
                m_Height = TerrainScript.GetHeightMapTexture().Height;
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "TextureWeights";
                PerlinNoise.SmoothAllVertexWeights(tcomp, TerrainEditorView.NormalizationConst);
         
                TerrainWeights1 = tcomp.GetWeights1FromVertices();
                if (SaveGeneratedBMP)
                {
                    MSGlog.Add2Log("Smoothed first weights using nearest neighbor values");
                    SaveTex(TerrainWeights1, FilenameType + ".bmp", Game.GraphicsContext);
                }
                else
                    MSGlog.Add2Log("Smoothed first weights using nearest neighbor values but was NOT saved");
                ImGuiSystem.UpdateTexture(TerrainWeights1IntPtr, TerrainWeights1);

                TerrainWeights2 = tcomp.GetWeights2FromVertices();
                if (SaveGeneratedBMP)
                {
                    MSGlog.Add2Log("Smoothed second weights using nearest neighbor values");
                    SaveTex(TerrainWeights2, FilenameType + ".bmp", Game.GraphicsContext);
                }
                else
                    MSGlog.Add2Log("Smoothed second weights using nearest neighbor values but was NOT saved");
                ImGuiSystem.UpdateTexture(TerrainWeights2IntPtr, TerrainWeights2);
             
            //    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                CubeInstancingRenderScript.UpdatingMode = 1;
                AreaXML.UpdateObjectLocations(tcomp);
                Cursor.Current = Cursors.Default;
            }
            SameLine(); 
            if (Button("Make Flat"))
            {
                m_Width = TerrainScript.GetHeightMapTexture().Width;
                m_Height = TerrainScript.GetHeightMapTexture().Height;
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "HeightMap";
                Texture texture = PerlinNoise.MakeFlat(m_Width, m_Height,
                    TargetHeightValue.AsStrideColor()).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
                if (SaveGeneratedBMP)
                {
                    MSGlog.Add2Log("Made a flat image.");
                    SaveTex(texture, FilenameType + ".bmp", Game.GraphicsContext);
                }
                else MSGlog.Add2Log("Image created but was NOT saved");
                tcomp.FullUpdate(texture, TerrainWeights1, TerrainWeights2);
                if (ImGuiSystem._loadedTextures.ContainsKey(TerrainHeightMapTextureIntPtr))
                {
                    ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
                }
                else
                    ImGuiSystem._loadedTextures.Add(TerrainHeightMapTextureIntPtr, texture);
                CubeInstancingRenderScript.UpdatingMode = 0;
                AreaXML.UpdateObjectLocations(tcomp);
            }
            SameLine();
            if (Button("Edges to Target Level"))
            {
                Cursor.Current = Cursors.WaitCursor;
                m_Width = TerrainScript.GetHeightMapTexture().Width;
                m_Height = TerrainScript.GetHeightMapTexture().Height;
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "HeightMap";
                Texture texture = ImGuiSystem._loadedTextures[TerrainEditorView.TerrainHeightMapTextureIntPtr].
                    GetColorData(Game.GraphicsContext).EdgesToHeightLevel(m_Width,
                    m_Height, TargetHeightValue).ToTexture(m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
                if (SaveGeneratedBMP)
                {
                    MSGlog.Add2Log("The heightmap edges where smoothed to height level " +
                        TargetHeightValue);
                    SaveTex(texture, FilenameType + ".bmp", Game.GraphicsContext);
                }
                else
                {
                    MSGlog.Add2Log("The heightmap edges where smoothed to height level " +
                        TargetHeightValue);
                    MSGlog.Add2Log("Image created but was NOT saved");
                }
                tcomp.FullUpdate(texture, TerrainWeights1, TerrainWeights2);
                if (ImGuiSystem._loadedTextures.ContainsKey(TerrainHeightMapTextureIntPtr))
                {
                    ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
                }
                else
                    ImGuiSystem._loadedTextures.Add(TerrainHeightMapTextureIntPtr, texture);
                CubeInstancingRenderScript.UpdatingMode = 0;
                AreaXML.UpdateObjectLocations(tcomp);
                Cursor.Current = Cursors.Default;
            }

            #region terrain props

            //         Separator();
            PushItemWidth(150);//System.GetGraphicsClass()->GetWindowWidth()-80.0f);
            InputText("File Name", ref FilenameType, 100);
            if (IsItemHovered())//IsItemEdited())
                EditingActive = true;// SetKeyboardFocusHere(-1);
            else EditingActive = false;
            PopItemWidth();
            PushItemWidth(100);//System.GetGraphicsClass()->GetWindowWidth()-80.0f);
            Vector2 vec2 = new Vector2(tcomp.HeightRange.X, tcomp.HeightRange.Y);
            int minheight = tcomp.HeightRange.X,
                maxheight = tcomp.HeightRange.Y;
            if (InputInt("Minimum Height", ref minheight))
            {
                if (minheight > tcomp.HeightRange.Y)
                    minheight = tcomp.HeightRange.X;
                else tcomp.HeightRange = new Stride.Core.Mathematics.Int2(
                    minheight, tcomp.HeightRange.Y);
            }
            SameLine();
            if (InputInt("Maximum Height", ref maxheight))
            {
                if (maxheight < tcomp.HeightRange.X)
                    maxheight = tcomp.HeightRange.Y;
                else tcomp.HeightRange = new Stride.Core.Mathematics.Int2(
                    tcomp.HeightRange.X, maxheight);
            }

            InputInt("Terrain Wide (x-coord)", ref m_Width, 1, 100);
            if (IsItemHovered())
            {
                if (IsKeyPressed(ImGuiKey.LeftArrow)) m_Width--;
                if (IsKeyPressed(ImGuiKey.RightArrow)) m_Width++;
            }
            if (m_Width < 32) m_Width = 32;
            if (m_Width > 1024) m_Width = 1024;
            //         SameLine();
            InputInt("Terrain High (z-coord)", ref m_Height, 1, 100);
            if (IsItemHovered())
            {
                if (IsKeyPressed(ImGuiKey.LeftArrow)) m_Height--;
                if (IsKeyPressed(ImGuiKey.RightArrow)) m_Height++;
            }
            if (m_Height < 32) m_Height = 32;
            if (m_Height > 1024) m_Height = 1024;
            InputFloat("Quad Wide Length", ref quadlenx, 0.1f, 10.0f);
            if (IsItemHovered())
            {
                if (IsKeyPressed(ImGuiKey.LeftArrow)) quadlenx -= 0.1f;
                if (IsKeyPressed(ImGuiKey.RightArrow)) quadlenx += 0.1f;
            }
            if (quadlenx < 0.1) quadlenx = 0.1f;
            if (quadlenx > 10.0) quadlenx = 10.0f;
            //	SameLine();
            InputFloat("Quad High Length", ref quadlenz, 0.1f, 10.0f);
            if (IsItemHovered())
            {
                if (IsKeyPressed(ImGuiKey.LeftArrow)) quadlenz -= 0.1f;
                if (IsKeyPressed(ImGuiKey.RightArrow)) quadlenz += 0.1f;
            }
            if (quadlenz < 0.1) quadlenz = 0.1f;
            if (quadlenz > 10.0) quadlenz = 10.0f;

            if (InputInt("Repeat Texture", ref texrepeat, 1, 8))
            {
                if (texrepeat < 0) texrepeat = 0;
                if (texrepeat > 64) texrepeat = 64;
                if(TerrainDisplayModeSelected == 2)
                {
                    texrepeat = 0;
                    MSGlog.Add2Log("In this mode, texture repeat is 0 (painting per quad)");
                }
                else
                {
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.TextureRepeat, TerrainEditorView.texrepeat);
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.TextureRepeat, TerrainEditorView.texrepeat);
                }
                // TerrainLOD = 1;
                //  tcomp.TerrainLOD = 1;
                tcomp.TEXTURE_REPEAT = texrepeat;
                if (TerrainDisplayModeSelected == 0)
                {
                    if(tcomp.TerrainLOD==1)
                        tcomp.FullUpdate(TerrainScript.GetHeightMapTexture(), TerrainWeights1, TerrainWeights2);
                    else
                        tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                }
                MSGlog.Add2Log("Terrain texture repeated " + texrepeat.ToString() + " times...");
            }
            SameLine();
            HelpMarker("Repeats the main texture over whole terrain. If 1, the texture covers the whole terrain only once. If 0, the texture covers each quad fully then repeat on all quads.");

            if (InputInt("Terrain LOD", ref TerrainLOD, 1, 10))
            {
                if (TerrainLOD < 1) TerrainLOD = 1;
                if (TerrainLOD > 50) TerrainLOD = 50;
                tcomp.TerrainLOD = TerrainLOD;
                // tcomp.NeedsUpdating = true;
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                MSGlog.Add2Log("Terrain Level of Detail (LOD) is set to " +
                    TerrainLOD.ToString());
                ShowTrees = true;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                ShowGrass = true;
                AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                if (TerrainLOD > 1)
                {
                    ShowTrees = false;
                }
                else
                    AreaXML.UpdateObjectLocations(tcomp);
            }
            SameLine();
            int vertsnum = ((TerrainWeights1.Height - 1) / TerrainLOD + 1) *
                ((TerrainWeights1.Width - 1) / TerrainLOD + 1);
            HelpMarker("# of vertices in the Mesh is " + vertsnum.ToString() + ". Level of Detail for the terrain. The higher the value, the fewer vertices used in the terrain mesh (worse details). Only used here for visualization. Editing the terrain requires the full terrain. Area Objects are not shown when LOD>1 is present.");

            PopItemWidth();

            if (Button("Open Resources Dir##OpenResourcesDir"))
            {
                string argument = Utility.Resources_Directory;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
            SameLine();
            if (Button("Open Terrain Dir##OpenTerrainDir"))
            {
                string argument = Utility.Resources_TerrainEditor_Directory;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
            #endregion terrain props

            NextColumn();

            BeginChild("texturesTerrain", new Vector2(350, 250), true,
                ImGuiWindowFlags.HorizontalScrollbar);
            Columns(4, null, false);
            // SameLine();
            Text("Heightmap");// +": " + TerrainTextures[i].Name);
                              //           SameLine();
            DrawImage(TerrainHeightMapTextureIntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainHeightMapTextureIntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Generate##HeightMap"))
            {
                FilenameType = "HeightMap";
                Generate();
            }
            if (Button("Load##HeightMap"))
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                if (MessageBox.Show("This operation will reset/overwrite most textures (heightmaps, weights etc) and you will lose your work! Are you sure you want to proceed?",
                    "Attention!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                { return; }

                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Heightmap File";
                theDialog.Filter = "bmp files|*.*";
                //string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                { 
                    Texture texture = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
                    if (texture.CheckGrayScale(Game.GraphicsContext))
                        PerlinNoise.IsGrayScaleHeightMap = true;
                    else
                        PerlinNoise.IsGrayScaleHeightMap = false;
                    m_Width= texture.Width;
                    m_Height= texture.Height;
                    tcomp.Width = m_Width;
                    tcomp.Height = m_Height;

                    CreateFlatWeights(tcomp);
                    tcomp.FullUpdate(texture, TerrainWeights1, TerrainWeights2);
                    CubeInstancingRenderScript.UpdatingMode = 0;
                    AreaXML.UpdateObjectLocations(tcomp);
                    ShowTrees = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Tree, ShowTrees);
                    ShowGrass = true;
                    AreaHandleModels.ToggleAreaModels(AreaObjectType.Grass, ShowGrass);
                }
            }
            if (Button("Save##HeightMap"))//OpenPopup("New Terrain");
            {
                if (ImGuiSystem._loadedTextures[TerrainHeightMapTextureIntPtr] != null)
                {
                    FilenameType = "HeightMap";
                    MSGlog.Add2Log("Saved current HeightMap image.");
                    SaveTex(ImGuiSystem._loadedTextures[TerrainHeightMapTextureIntPtr], FilenameType + ".bmp", Game.GraphicsContext);
                }
            }
            if (Button("SaveAs##HeightMap"))
            {
                if (ImGuiSystem._loadedTextures[TerrainHeightMapTextureIntPtr] != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save Heightmap File";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current HeightMap image.");
                        SaveTex(ImGuiSystem._loadedTextures[TerrainHeightMapTextureIntPtr], theDialog.FileName.ToString(),
                            Game.GraphicsContext, false);
                    }
                }
            }
            if (Button("Apply##HeightMap"))
            {
                TerrainEditModeSelected = 0;
            }

            NextColumn();
            Text("Texture");// +": " + TerrainTextures[i].Name);
            DrawImage(TerrainTexturesIntPtr[Selectedtexture], new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainTexturesIntPtr[Selectedtexture], new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Generate##Texture"))
            {
                FilenameType = "Texture";
                Generate();
            }
            if (Button("Save##Texture"))//OpenPopup("New Terrain");
            {
                if (TerrainScript.GetTerrainTexture(Selectedtexture + 1) != null)
                {
                    FilenameType = "Texture";
                    MSGlog.Add2Log("Current Texture saved.");
                    SaveTex(TerrainScript.GetTerrainTexture(Selectedtexture + 1),
                         FilenameType + ".bmp", Game.GraphicsContext);
                }
            }
            if (Button("SaveAs##Texture"))
            {
                if (TerrainScript.GetTerrainTexture(Selectedtexture + 1) != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save Texture File";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current Texture image.");
                        SaveTex(TerrainScript.GetTerrainTexture(Selectedtexture + 1), theDialog.FileName.ToString(), Game.GraphicsContext, false);
                    }
                }
            }
            if (Button("Apply##Texture"))
            {
                TerrainEditModeSelected = 1;
            }

            NextColumn();

            Text("Weights1");// +": " + TerrainTextures[i].Name);
            DrawImage(TerrainWeights1IntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainWeights1IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Generate##Weights1"))
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "Weights1";
                Generate();
                TerrainBlendedTexture = TerrainScript.GetBlendedTexture(
                tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
            }
            if (Button("Load##Weights1"))
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load First Weights File";
                theDialog.Filter = "bmp files|*.*";
                //string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    Texture tex = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, //PixelFormat.R8G8B8A8_UNorm,
                      false);
                    if(tex.Width!=m_Width || tex.Height!=m_Height)
                    {
                        MSGlog.Add2Log("The dimensions of the weight texture are not the same as that of the heightmap texture. Operation failed...");
                        return;
                    }
                    TerrainWeights1 = tex;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    //        using (var inStream = System.IO.File.OpenRead(theDialog.FileName.ToString()))
                    //            TerrainWeights1 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                    ImGuiSystem.UpdateTexture(TerrainWeights1IntPtr, TerrainWeights1);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainWeights1);

                    TerrainBlendedTexture = TerrainScript.GetBlendedTexture(
                     tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                    ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
                }
            }
            if (Button("Save##Weights1"))//OpenPopup("New Terrain");
            {
                if (ImGuiSystem._loadedTextures[TerrainWeights1IntPtr] != null)
                {
                    FilenameType = "Weights1";
                    MSGlog.Add2Log("Current Weights1 image saved...");
                    SaveTex(ImGuiSystem._loadedTextures[TerrainWeights1IntPtr],
                        FilenameType + ".bmp", Game.GraphicsContext);
                }
            }
            if (Button("SaveAs##Weights1"))
            {
                if (ImGuiSystem._loadedTextures[TerrainWeights1IntPtr] != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save Weights1 File";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current Weights1 image.");
                        SaveTex(ImGuiSystem._loadedTextures[TerrainWeights1IntPtr], theDialog.FileName.ToString(), Game.GraphicsContext, false);
                    }
                }
            }
            if (Button("Reset##Weights1"))
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                TerrainWeights1 =
                    PerlinNoise.MakeFlat(m_Width, m_Height,
                    new Stride.Core.Mathematics.Color(255, 0,
                    0, 0)).ToTexture(m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
                ImGuiSystem.UpdateTexture(TerrainWeights1IntPtr, TerrainWeights1);
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainWeights1);
                TerrainBlendedTexture = TerrainScript.GetBlendedTexture(
                    tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
                MSGlog.Add2Log("Weights1 were reset to (1,0,0,0) everywhere");
            }
            NextColumn();
            Text("Weights2");// +": " + TerrainTextures[i].Name);
            DrawImage(TerrainWeights2IntPtr, new Vector2(64.0f, 64.0f));
            if (ZoomInTexture && IsItemHovered())
            {
                if (BeginTooltip())
                {
                    DrawImage(TerrainWeights2IntPtr, new Vector2(512.0f, 512.0f));
                    EndTooltip();
                }
            }
            if (Button("Generate##Weights2"))
            {
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                FilenameType = "Weights2";
                Generate();
                TerrainBlendedTexture = TerrainScript.GetBlendedTexture
                    (tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
            }
            if (Button("Load##Weights2"))
            {
                OpenFileDialog theDialog = new OpenFileDialog();
                theDialog.Title = "Load Second Weights File";
                theDialog.Filter = "bmp files|*.*";
                //string startupPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName + "\\Resources";
                theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
                if (theDialog.ShowDialog() == DialogResult.OK)
                {
                    Texture tex = Utility.LoadTex(theDialog.FileName.ToString(),
                        GraphicsDevice, Game.GraphicsContext, false);
                    if (tex.Width != m_Width || tex.Height != m_Height)
                    {
                        MSGlog.Add2Log("The dimensions of the weight texture are not the same as that of the heightmap texture. Operation failed...");
                        return;
                    }
                    TerrainWeights2 = tex;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    //   using (var inStream = System.IO.File.OpenRead(theDialog.FileName.ToString()))
                    //       TerrainWeights2 = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                    ImGuiSystem.UpdateTexture(TerrainWeights2IntPtr, TerrainWeights2);
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainWeights2);
                    TerrainBlendedTexture = TerrainScript.
                        GetBlendedTexture(tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                    ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
                }
            }
            if (Button("Save##Weights2"))//OpenPopup("New Terrain");
            {
                if (ImGuiSystem._loadedTextures[TerrainWeights2IntPtr] != null)
                {
                    FilenameType = "Weights2";
                    MSGlog.Add2Log("Current Weights2 image saved...");
                    SaveTex(ImGuiSystem._loadedTextures[TerrainWeights2IntPtr],
           FilenameType + ".bmp", Game.GraphicsContext);
                }
            }
            if (Button("SaveAs##Weights2"))
            {
                if (ImGuiSystem._loadedTextures[TerrainWeights2IntPtr] != null)
                {
                    SaveFileDialog theDialog = new SaveFileDialog();
                    theDialog.Title = "Save Weights2 File";
                    theDialog.Filter = "bmp files|*.*";
                    if (theDialog.ShowDialog() == DialogResult.OK)
                    {
                        MSGlog.Add2Log("Saved current Weights2 image.");
                        SaveTex(ImGuiSystem._loadedTextures[TerrainWeights2IntPtr], theDialog.FileName.ToString(), Game.GraphicsContext, false);
                    }
                }
            }
            if (Button("Reset##Weights2"))
            {

                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh.");
                    TerrainEditorView.TerrainLOD = 1;
                    m_Width = TerrainScript.GetHeightMapTexture().Width;
                    m_Height = TerrainScript.GetHeightMapTexture().Height;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                    return;
                }
                TerrainWeights2 =
                    PerlinNoise.MakeFlat(m_Width, m_Height,
                    new Stride.Core.Mathematics.Color(0,
                    0, 0, 0)).ToTexture(m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                ImGuiSystem.UpdateTexture(TerrainWeights2IntPtr, TerrainWeights2);
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainWeights2);// tcomp.TerrainWeights1);
                MSGlog.Add2Log("Weights2 were reset to (0,0,0,0) everywhere");
                TerrainBlendedTexture = TerrainScript.GetBlendedTexture(tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
            }
            EndChild();

            Columns(1);
            #endregion terrain

            #region ball
            Separator();
            Text("Selection Ball Properties");
            if (SliderFloat("Radius", ref Radius, 0.1f, 100.0f))
            {
                ModelComponent modelComponent = MultiTypeCameraController.ClickBallModelEntity.GetOrCreate<ModelComponent>();
                MultiTypeCameraController.ClickBallModelEntity.Transform.Scale = new Stride.Core.Mathematics.Vector3(Radius);
            }
            SameLine();
            if (Checkbox("Show/Hide##SelectionBall", ref ShowSelectionBall))
            {
                if (ShowSelectionBall)
                {
                    ModelComponent modelComponent = MultiTypeCameraController.ClickBallModelEntity.GetOrCreate<ModelComponent>();
                    modelComponent.Model = Content.Load<Model>("TerrainSelectionSphere");
                }
                else
                {
                    ModelComponent modelComponent = MultiTypeCameraController.ClickBallModelEntity.GetOrCreate<ModelComponent>();
                    modelComponent.Model = null;
                }
            }
            if (SliderFloat("Selection Strength", ref BallSelectionStrength, 0.001f, 1.0f))
            {
            }
            SameLine();
            HelpMarker("The chance to select a vertex within the range of the selection ball.");
            if (SliderFloat("Power", ref BallSelectionPower, 0.1f, 100.0f))
            {
            }
            SameLine();
            HelpMarker("Power multiplier when applying texture weights to a selected vertex.");

            if (ShowSelectionBall && ColorEdit4("Ball Color", ref BallColor))
            {
                ModelComponent modelComponent = MultiTypeCameraController.ClickBallModelEntity.GetOrCreate<ModelComponent>();
                modelComponent.GetMaterial(0).Passes[0].Parameters.Set(MaterialKeys.DiffuseValue,
                         Utility.Vec4Color(TerrainEditorView.BallColor));
            }
            Vector3 vec = MultiTypeCameraController.ClickBallModelEntity.Transform.Position.AsNumericVec3();
            if (ShowSelectionBall && InputFloat3("Ball Center Position (selected vertex in terrain messh)", ref vec))
            {
                MultiTypeCameraController.ClickBallModelEntity.Transform.Position = vec.AsStrideVec3();
            }
            Text("Selected Vertex Properties");
            Stride.Core.Mathematics.Int2 pos = new Stride.Core.Mathematics.Int2((int)vec.X, (int)vec.Z);
            Vector4 col = tcomp.GetCPUColorAt(pos.X, pos.Y).ToVector4().AsNumericVec4();
            Vector4 wt1 = tcomp.GetCPUWeight1At(pos.X, pos.Y).ToVector4().AsNumericVec4();
            Vector4 wt2 = tcomp.GetCPUWeight2At(pos.X, pos.Y).ToVector4().AsNumericVec4();
            bool update = false; 
            if (ColorEdit4("Vertex Color", ref col))
            {
                update = true;
            }
            if (ColorEdit4("Vertex First Weights (Color1)", ref wt1))
            {
                update = true;
            }
            if (ColorEdit4("Vertex Second Weights (Color2)", ref wt2))
            {
                update = true;
            }
            if (update)
            {
                tcomp.SetVertexColor(pos, col.ToStrideColor(), 
                    wt1.ToStrideColor(), wt2.ToStrideColor());
            }

            #endregion ball

            #region textures
            Separator();
            if (TerrainDisplayModeSelected == 0 || TerrainDisplayModeSelected == 2)
            {
                Text("Paint Texture Terrain using the following textures...");

                Columns(4, null, false);

                for (int i = 0; i < TerrainNumofTextures; i++)
                {
                    if (TerrainTexturesIntPtr[i].ToInt64() > 0)
                    {
                        if (Button("Load Texture " + (i + 1).ToString()))
                        {
                            LoadTexture(i);
                        }
                        DrawImage(TerrainTexturesIntPtr[i], new Vector2(64.0f, 64.0f));
                        if (IsItemHovered())
                        {
                            if (ZoomInTexture && BeginTooltip())
                            {
                                DrawImage(TerrainTexturesIntPtr[i], new Vector2(512.0f, 512.0f));
                                EndTooltip();
                            }
                            if (BeginTooltip())
                            {
                                Text("Click to select");
                                EndTooltip();
                            }
                            if (IsMouseDown(ImGuiMouseButton.Left))
                            {
                                Selectedtexture = i;
                                TerrainEditModeSelected = 1;
                            }
                        }
                    }
                    NextColumn();
                }
                Columns(1);

                Separator();

                if (Button("Update##BlendedTexture"))
                {
                    TerrainBlendedTexture = TerrainScript.GetBlendedTexture(
                       tcomp, Game.GraphicsContext, Game.GraphicsDevice);
                    ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
                    MSGlog.Add2Log("Terrain Blended Texture updated...");
                }
                SameLine();
                HelpMarker("Slow operation in the CPU so update on your own to see the current blended texture. This is not the best blended operation (the GPU shader is better), but it gives you a way to blend up to 8 textures and create your own texture resources.");

                SameLine();
                if (Button("Save##BlendedTexture"))//OpenPopup("New Terrain");
                {
                    if (ImGuiSystem._loadedTextures[TerrainBlendedTextureIntPtr] != null)
                    {
                        FilenameType = "BlendedTexture";
                        MSGlog.Add2Log("Current Terrain Blended Texture saved...");
                        SaveTex(ImGuiSystem._loadedTextures[TerrainBlendedTextureIntPtr],
                         FilenameType + ".bmp", Game.GraphicsContext);
                    }
                }
                SameLine();
                if (Button("SaveAs##BlendedTexture"))
                {
                    if (ImGuiSystem._loadedTextures[TerrainBlendedTextureIntPtr] != null)
                    {
                        SaveFileDialog theDialog = new SaveFileDialog();
                        theDialog.Title = "Save Blended Texture File";
                        theDialog.Filter = "bmp files|*.*";
                        if (theDialog.ShowDialog() == DialogResult.OK)
                        {
                            MSGlog.Add2Log("Saved current Blended Texture image.");
                            SaveTex(ImGuiSystem._loadedTextures[TerrainBlendedTextureIntPtr], theDialog.FileName.ToString(), Game.GraphicsContext, false);
                        }
                    }
                }
                Text("Blended Texture");
                DrawImage(TerrainBlendedTextureIntPtr, new Vector2(512.0f, 512.0f));
                if (ZoomInTexture && IsItemHovered())
                {
                    if (BeginTooltip())
                    {
                        DrawImage(TerrainBlendedTextureIntPtr, new Vector2(512.0f, 512.0f));
                        EndTooltip();
                    }
                }
                SameLine();
            }
            #endregion textures

            #region heighttexs
            if (TerrainDisplayModeSelected == 1)
            {
                Text("Choose the textures for height based terrain texturing, from lowest level to highest...");

                Columns(5, null, false);

                for (int i = 0; i < TerrainHeightBasedNumofTextures; i++)
                {
                    if (TerrainHeightBasedTexturesIntPtr[i].ToInt64() > 0)
                    {
                        if (Button("Load Height Texture " + (i + 1).ToString() + "##heightbasedptr"))
                        {
                            LoadHeightBasedTexture(i);
                        }
                        DrawImage(TerrainHeightBasedTexturesIntPtr[i], new Vector2(64.0f, 64.0f));
                        if (IsItemHovered())
                        {
                            if (ZoomInTexture && BeginTooltip())
                            {
                                DrawImage(TerrainHeightBasedTexturesIntPtr[i], new Vector2(512.0f, 512.0f));
                                EndTooltip();
                            }
                        }
                    }
                    NextColumn();
                }
                Columns(1);


                PushItemWidth(200);
                if (SliderFloat("Slope Cutoff", ref SlopeCutoff,
                    0.01f, 1.0f))
                {
                    //        if (SlopeCutoff < 0.01f) SlopeCutoff=0.01f;
                    //      if (SlopeCutoff > 1.0f) SlopeCutoff = 1f;
                    tcomp.SlopeCutoff = SlopeCutoff;
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.SlopeCutoff, SlopeCutoff);
                }
                SameLine();
                if (SliderFloat("Distance Multiplier", ref DistanceMultiplier,
                    0.1f, 20.0f))
                {
                    tcomp.DistanceMultiplier = DistanceMultiplier;
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DistanceMultiplier, DistanceMultiplier);
                }
                SameLine();
                if (SliderFloat("Detail Mapping Distance", ref DetailMappingDistance,
                    0.01f, 1500.00f))
                {
                    tcomp.DetailMappingDistance = DetailMappingDistance;
                    tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.DetailMappingDistance, DetailMappingDistance);
                    tcomp.MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DetailMappingDistance, DetailMappingDistance);
                }
                PopItemWidth();
              /* Not using it 
                Text("Detailed Map Texture, used to map nearby pixels in the depth buffer so they don't look blurry...");
                DrawImage(TerrainDetaiMapTextureIntPtr, new Vector2(128.0f, 128.0f));
                if (IsItemHovered())
                {
                    if (ZoomInTexture && BeginTooltip())
                    {
                        DrawImage(TerrainDetaiMapTextureIntPtr, new Vector2(512.0f, 512.0f));
                        EndTooltip();
                    }
                }*/
            }
            #endregion heighttexs
        }
        public static float SlopeCutoff = 0.2f, 
            DistanceMultiplier = 10.0f, DetailMappingDistance=500.0f;
        //every level is 10% of the height range
        private float[] HeightLevels = new float[] 
        {0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f,1.0f};
        private enum HeightLevelTypes
        {
            Seabed,//below water
            Sand,//about water level
            Dirt,//past water level
            Grass,
            Swamp,
            Desert,
            Rock,
            Mountain,
            Snow
        }
        /* 
         height = sample from Height Map
slope = sample several points around a point on the height map to get the slope

if( height < water level )
    sample mud texture;
else if( height >= water level && height < sand level )
    sample sand texture;
else if( height >= sand level && height < mountain level )
    sample grass texture;
else
    sample mountain top snow texture;

if( slope < modest )
    weight = 0%;
else if( slope >= modest && slope < gradual incline )
    weight = 40% and sample rock texture;
else if( slope >= gradual incline && slope < vertical face )
    weight = 70% and sample craggy rock texture;
else if( slope >= vertical face )
    weight = 90% and sample vertical rock texture;

color = blend(groundTextureSample.rgb, slopeTextureSample.rgb * weight);
        */
        private void LoadHeightBasedTexture(int i)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Open Texture File";
            theDialog.Filter = "bmp files|*.*";
            theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Texture texture;
                using (var inStream = System.IO.File.OpenRead(theDialog.FileName.ToString()))
                    texture = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                ImGuiSystem.UpdateTexture(TerrainHeightBasedTexturesIntPtr[i], texture);
                TerrainScript.SetHeightBasedTerrainTexture(tcomp, i + 1);
            }
        }

        private void LoadTexture(int i)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Open Texture File";
            theDialog.Filter = "bmp files|*.*";
            theDialog.InitialDirectory = Utility.Resources_TerrainEditor_Directory;//startupPath;
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Texture texture;
                using (var inStream = System.IO.File.OpenRead(theDialog.FileName.ToString()))
                    texture = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                ImGuiSystem.UpdateTexture(TerrainTexturesIntPtr[i], texture);
                TerrainScript.SetTerrainTexture(tcomp,i + 1);
                Selectedtexture = i;
            }
        }

        private void Generate()
        {
            Cursor.Current = Cursors.WaitCursor;
            if (m_Width < 0) m_Width = 16;
            if (m_Width > 1024) m_Width = 1024;
            if (m_Height < 0) m_Height = 16;
            if (m_Height > 1024) m_Height = 1024;
            if (Mincutoff >= Maxcutoff)
            {
                Mincutoff = 0.0f; Maxcutoff = 1.0f;
            }
            tcomp.Width = m_Width;
            tcomp.Height = m_Height;
            tcomp.m_QuadSideWidthX = quadlenx;
            tcomp.m_QuadSideWidthZ = quadlenz;
            tcomp.TEXTURE_REPEAT = texrepeat;


            #region methods
            Texture texture =new Texture();
            if (GenMethod == 0)// == "Greyscale (All Channels Same Value)")
            {
                texture = PerlinNoise.RandomizeBMPPerlin2(m_Width, m_Height, NormalizationConst,
                FreqX, FreqZ, PixelCutoff, Freq, Error, PowValue,
                Persistance, Octave, Mincutoff, Maxcutoff, "Yes").ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList); }
            else if (GenMethod == 1)//"Red Channel Only")
            {
                texture = PerlinNoise.RandomizePerlinOneChannelOnly(m_Width, m_Height,
             NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
             0, TargetHeightValue).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList); }
            else if (GenMethod == 2)//"Green Channel Only")
            {
                texture = PerlinNoise.RandomizePerlinOneChannelOnly(m_Width, m_Height,
             NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
             1, TargetHeightValue).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList); }
            else if (GenMethod == 3)//"Blue Channel Only")
            {
                texture = PerlinNoise.RandomizePerlinOneChannelOnly(m_Width, m_Height,
                 NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
                 Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
                 2, TargetHeightValue).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 4)//"Color: All Channels")
            {
                texture = PerlinNoise.RandomizeBMPPerlin2(m_Width, m_Height, NormalizationConst,
    FreqX, FreqZ, PixelCutoff, Freq, Error, PowValue,
    Persistance, Octave, Mincutoff, Maxcutoff, "No").ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 5)
            {
                texture = PerlinNoise.MakeFlat(m_Width, m_Height,
                    new Stride.Core.Mathematics.Color(ColorStart.X, 
                    ColorStart.Y, ColorStart.Z )).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 6)//"Uniform Noise")
            {
                if(Error == 0)
                {
                    Error = 1.0f;
                    MSGlog.Add2Log("Error field was zero, made it 1.0.");
                }
                //Texture currenttex=ImGuiSystem._loadedTextures[
                //        TerrainHeightMapTextureIntPtr];
                //                Stride.Core.Mathematics.Color[] heightValues = 
                //                   new Stride.Core.Mathematics.Color[currenttex.Width * currenttex.Height];
                //               currenttex.GetData(Game.GraphicsContext.CommandList, heightValues);
                texture = PerlinNoise.RandomizeAdd2Existing(m_Width,
                    m_Height, Error, tcomp).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 7)//"Clouds")
            {
                ColorStart = new System.Numerics.Vector3(0, 0, 1);//blue
                ColorEnd = new System.Numerics.Vector3(1, 1, 1);//white
                PerlinNoise.IsGrayScaleHeightMap = false;
                texture = PerlinNoise.RandomCloud(m_Width, m_Height,
            NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff,
             Maxcutoff, ColorStart, ColorEnd).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 8)//"Colored Smooth Transitions")
            {
              //  PerlinNoise.IsGrayScaleHeightMap = true;
                texture = PerlinNoise.RandomizeSmoothTransitions(m_Width, m_Height,
                        PowValue, Mincutoff, Maxcutoff,
                        ColorStart, ColorEnd , LocationStart,
                        LocationEnd).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 9)//"Moisture")
            {
                ColorStart = new System.Numerics.Vector3(0, 0, 0);//black
                ColorEnd = new System.Numerics.Vector3(5.0f, 13.0f, 38.0f)/255.0f;//dark blue
                PerlinNoise.IsGrayScaleHeightMap = false;
                texture = PerlinNoise.RandomCloud(m_Width, m_Height,
            NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff,
             Maxcutoff, ColorStart, ColorEnd).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 10)//"Random Cloud")
            {
                PerlinNoise.IsGrayScaleHeightMap = false;
                texture = PerlinNoise.RandomCloud(m_Width, m_Height,
            NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff,
             Maxcutoff, ColorStart, ColorEnd).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 11)//"Voronoi Tesselation Generator")
            {
                //PerlinNoise.IsGrayScaleHeightMap = false;
                texture = PerlinNoise.RandomVoronoiForTextures(m_Width, m_Height,
                NormalizationConst, Freq, PowValue, Mincutoff, Maxcutoff,
                ColorStart, ColorEnd, true).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 12)//"River/Mountain Ridge/Islands")
            {
                Persistance = 1;
                Octave = 1;
                PowValue = 1;
                FreqX = 3; FreqZ = 3;
                Mincutoff = 0.7f;
                texture = PerlinNoise.RandomizePerlinBand(m_Width, m_Height,
                 NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
                 Error, PowValue, 1.0f, 1, Mincutoff, Maxcutoff,
                 -1).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 13)//"Shaded Smooth Transitions")
            {
                texture = PerlinNoise.RandomizeSmoothTransitions(m_Width, m_Height,
                        PowValue, Mincutoff, Maxcutoff,
                        ColorStart, ColorEnd, LocationStart, 
                        LocationEnd).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);// Utility.RandomFloat() < 0.5f);
            }
            else if (GenMethod == 14)//Ridged Mountains Perlin Elevation Map
            {
                Type = 5; Type2 = 1;Freq = 5;
                texture = PerlinNoise.RandomizeFromCurve(m_Width, m_Height,
             NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
              ColorStart, ColorEnd, LocationStart, LocationEnd,
             Type, Type2, TargetHeightValue, VarianceX, VarianceZ).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 15)//Hills (Bivariate Gaussians)")
            {
                NormalizationConst = 2.5f;
                VarianceX = m_Width / 5;
                VarianceZ = m_Height / 5;
                texture = PerlinNoise.RandomizeBivNormalMap(m_Width, m_Height,
                Mincutoff, Maxcutoff, PowValue, VarianceX, VarianceZ,
                NormalizationConst, TargetHeightValue, PixelCutoff,
                Voronoi_num_vertices).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 16)//Hills (Fixed means Bivariate Gaussians)")
            {
                NormalizationConst = 2.5f;
                VarianceX = m_Width / 5;
                VarianceZ = m_Height / 5;
                texture = PerlinNoise.RandomHillsBivNormalMap(m_Width, m_Height,
                Mincutoff, Maxcutoff, PowValue, VarianceX, VarianceZ,
                NormalizationConst, TargetHeightValue, PixelCutoff).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 17)//"Randomize from Curve")
            {
                texture = PerlinNoise.RandomizeFromCurve(m_Width, m_Height,
             NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
              ColorStart, ColorEnd, LocationStart, LocationEnd,
             Type, Type2, TargetHeightValue, VarianceX, VarianceZ).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 18)//"Single Hill (Bivariate Gaussian)")
            {
                texture = PerlinNoise.RandomizeSingleBivNormalMap(m_Width, m_Height,
                           NormalizationConst,
                      FreqX, FreqZ, PixelCutoff, Freq, Error
                       , PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
                       VarianceX, VarianceZ).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 19)//desert "Ridged Perlin Elevation Map")
            {
                PowValue = 1.1f;
                Persistance = 2.5f;
                texture = PerlinNoise.RandomizeElevationMapPerlinBased(m_Width, m_Height,
                            NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
                            Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,                           
                            1).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 20)//"Mountain Elevation")
            {
                VarianceX = m_Width / 5;
                VarianceZ = m_Height / 5;
                Type = 4; 
                Type2 = 1;//PowValue = 0.5f;
                FreqX = 2; FreqZ = 2; Persistance = 2; Freq = 1.5f;
                Error = 0.1f;
                texture = PerlinNoise.RandomizeElevationMountain(m_Width, m_Height,
                Mincutoff, Maxcutoff, PowValue, VarianceX, VarianceZ,
                NormalizationConst, TargetHeightValue, PixelCutoff,
                FreqX, FreqZ, Persistance,Freq, Error, Octave,Type, Type2)
                    .Smooth(m_Width, m_Height).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 21)//Biome)
            {
                //  Persistance = 1.1f;
                //  Octave = 9;
                texture = PerlinNoise.GenerateBiome(m_Width, m_Height,
                            NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
                            Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
                             Type2).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 22)//Biome Voronoi)
            {
                texture = PerlinNoise.RandomVoronoiBiome(m_Width, m_Height,
             NormalizationConst, FreqX, FreqZ, PixelCutoff, Freq,
             Error, PowValue, Persistance, Octave, Mincutoff, Maxcutoff,
             Voronoi_num_vertices).ToTexture(m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            else if (GenMethod == 23)// Voronoi, from color start-end
            {
                texture = PerlinNoise.RandomVoronoiForTextures(m_Width, m_Height,
                NormalizationConst, Freq, PowValue, Mincutoff, Maxcutoff,
                ColorStart, ColorEnd, false).ToTexture(
                     m_Width, m_Height, Game.GraphicsDevice,
                     Game.GraphicsContext.CommandList);
            }
            #endregion methods

            if (SaveGeneratedBMP)
            {
                MSGlog.Add2Log("Image created and saved.");
                SaveTex(texture, FilenameType + ".bmp", Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(CreatedImageSourceIntPtr, texture);
            }
            else MSGlog.Add2Log("Image created but was NOT saved");
            if (TerrainEditorView.FilenameType == "HeightMap")
            {
                m_Width = TerrainScript.GetHeightMapTexture().Width;
                m_Height = TerrainScript.GetHeightMapTexture().Height;
                //     texture = texture.ReFormat(Game.GraphicsContext); 
                if (TerrainEditorView.TerrainLOD != 1)
                {
                    TerrainEditorView.MSGlog.Add2Log("Terrain LOD set to 1. Cannot manipulate vertices unless on full mesh display...");
                    TerrainEditorView.TerrainLOD = 1;
                    tcomp.TerrainLOD = 1;
                    tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
                }
                else
                tcomp.FullUpdate(texture, TerrainWeights1, TerrainWeights2);
                ImGuiSystem.UpdateTexture(TerrainHeightMapTextureIntPtr, texture);
               // tcomp.MaterialBlend.Passes[0].Parameters.Set(TerrainBlendShaderKeys.HeightMap, texture);// tcomp.TerrainTexture8);
                CubeInstancingRenderScript.UpdatingMode = 0;
                AreaXML.UpdateObjectLocations(tcomp);//  //TerrainScript.UpdateObjectLocations(Game.GraphicsContext, GraphicsDevice);
            }
            else if (TerrainEditorView.FilenameType == "Image")
            {
                //      texture = texture.ReFormat(Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(TerrainEditorView.CreatedImageSourceIntPtr, texture);
            }
            else if (TerrainEditorView.FilenameType == "Weights1")
            {
                //      texture = texture.ReFormat(Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr, texture);
                TerrainEditorView.TerrainWeights1 = texture;
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.FirstWeights, TerrainEditorView.TerrainWeights1);// tcomp.TerrainWeights1);
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
            }
            else if (TerrainEditorView.FilenameType == "Weights2")
            {
                //     texture = texture.ReFormat(Game.GraphicsContext);
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr, texture);
                TerrainEditorView.TerrainWeights2 = texture;
                tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.SecondWeights, TerrainEditorView.TerrainWeights2);// tcomp.TerrainWeights1);
                tcomp.FullUpdateLOD(TerrainWeights1, TerrainWeights2);
            }
            else if (TerrainEditorView.FilenameType == "Texture")
            {
                ImGuiSystem.UpdateTexture(TerrainTexturesIntPtr[Selectedtexture], texture);
                switch (Selectedtexture)
                {
                    case 0:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_1, texture);
                        break;
                    case 1:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_2, texture);
                        break;
                    case 2:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_3, texture);
                        break;
                    case 3:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_4, texture);
                        break;
                    case 4:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_5, texture);
                        break;
                    case 5:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_6, texture);
                        break;
                    case 6:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_7, texture);
                        break;
                    case 7:
                        tcomp.MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.Texture_8, texture);
                        break;
                }
                TerrainBlendedTexture = TerrainScript.GetBlendedTexture(
                  tcomp,Game.GraphicsContext, Game.GraphicsDevice);
                ImGuiSystem.UpdateTexture(TerrainBlendedTextureIntPtr, TerrainBlendedTexture);
            }
            Cursor.Current = Cursors.Default;
        }

        public static void DrawImage(IntPtr Index, Vector2 size)
        {
            //for testing the format

            if(ShowTextureFormat && ImGuiSystem._loadedTextures[Index]!=null)
            TextWrapped(ImGuiSystem._loadedTextures[Index].Description.Format.
                ToString()+"\nDims:"+ ImGuiSystem._loadedTextures[Index].Width.ToString()
                +"x"+ ImGuiSystem._loadedTextures[Index].Height.ToString());
            ImGuiNative.igImage(Index,//id,
                new System.Numerics.Vector2(size.X, size.Y),
                new System.Numerics.Vector2(0, 0),
                new System.Numerics.Vector2(1, 1),
                System.Numerics.Vector4.One, System.Numerics.Vector4.UnitW);
        }

        public static bool SaveTex(Texture texout, string filename_in_resources_dir,
            GraphicsContext GraphicsContext, bool saveindir = true)
        {
            try
            {
                /*  if (!texout.CheckFormat(PixelFormat.R8G8B8A8_UNorm))
                  {
                      MSGlog.Add2Log("Format is not R8G8B8A8_UNorm. Converted and Saved in this format.");
                      TerrainEditorView.SwitchtoGeneralTab = 2;
                      texout.Resize(texout.Width,texout.Height,GraphicsContext);
                  }*/
                //string filename = Utility.Resources_TerrainEditor_Directory + filename_in_resources_dir;

                /*  TextureTool texTool = new TextureTool();
                  Stride.Graphics.Image im = texout.GetDataAsImage(GraphicsContext.CommandList);
                  PixelBuffer buff=texout.GetDataAsImage(GraphicsContext.CommandList).GetPixelBuffer(0, 0);
                  TexImage texim = new TexImage(im.DataPointer, im.TotalSizeInBytes,
                      texout.Width,texout.Height,texout.Depth,
                      PixelFormat.B8G8R8A8_UNorm_SRgb,
                      //im.Description.Format,
                      im.Description.MipLevels,im.Description.ArraySize,
                      TexImage.TextureDimension.Texture2D);
                 //texTool.Convert(texim,Utility.QaLPixelFormat);
                  Stride.Graphics.Image strideim = texTool.ConvertToStrideImage(texim);
                  strideim.ConvertFormatToSRgb();
                  MSGlog.Add2Log("Saved BMP in: " + filename);
                  texTool.Save(texim, filename, Utility.QaLPixelFormat);
                  return true;*/
                if (!(texout.Description.Format == Stride.Graphics.PixelFormat.R8G8B8A8_UNorm
                    || texout.Description.Format == 
                    Stride.Graphics.PixelFormat.B8G8R8A8_UNorm))
                {
                    //possibly compressed texture, use stride function
                    string fname = filename_in_resources_dir;
                    if (saveindir)
                    {
                        fname = Utility.Resources_TerrainEditor_Directory + filename_in_resources_dir;
                    }
                    texout.ReFormat(GraphicsContext);
                    using (var outStream = System.IO.File.OpenWrite(fname))
                       texout.Save(GraphicsContext.CommandList, outStream,ImageFileType.Bmp);
                    MSGlog.Add2Log("Saved BMP: " + fname);
                    return true;
                }
                // texout = texout.ReFormat(GraphicsContext);
                Stride.Core.Mathematics.Color[] Colors = texout.GetColorData(GraphicsContext);
                if (Colors == null)
                {
                    MSGlog.Add2Log("DIDNT SAVE FILE. Colors were null");
                    TerrainEditorView.SwitchtoGeneralTab = 2;
                    return false;
                }
                //Colors.ToBitmap(texout.Width, texout.Height, filename_in_resources_dir, true);
                Colors.SaveBMP32Image(filename_in_resources_dir, texout.Width,
                    texout.Height, saveindir);
                //using (var outStream = System.IO.File.OpenWrite(filename))
                //    texout.Save(GraphicsContext.CommandList, outStream,ImageFileType.Bmp);
                /*   Stride.Core.Mathematics.Color[] ColorValues = new 
                       Stride.Core.Mathematics.Color[texout.Width * texout.Height];
                   texout.GetData(GraphicsContext.CommandList, ColorValues);
                   Bitmap im = new Bitmap(texout.Width , texout.Height);
                   Color[] colors = new Color[texout.Width * texout.Height];
                   for (int i = 0; i < texout.Width; i++)
                   {
                       for (int j = 0; j < texout.Height; j++)
                       {
                           System.Drawing.Color pixel = System.Drawing.Color.FromArgb(
                               colors[i + j * texout.Width].A, 
                               colors[i + j * texout.Width].R,
                               colors[i + j * texout.Width].G, 
                               colors[i + j * texout.Width].B);
                           im.SetPixel(i, j, pixel);
                       }
                   }
                   using (var outStream = System.IO.File.OpenWrite(filename))                    
                       im.Save(outStream, System.Drawing.Imaging.ImageFormat.Bmp);
                   texture = Texture.New2D<Color>(GraphicsDevice, im.Width, im.Height,
                       Utility.QaLPixelFormat, colors, TextureFlags.ShaderResource);
                   using (var outStream = System.IO.File.OpenWrite(filename))
                       texin.//ToBitmap(GraphicsContext).
                             Save(GraphicsContext.CommandList,outStream,
                           //System.Drawing.Imaging.ImageFormat.Jpeg
                           ImageFileType.bmp
                           );*/
                MSGlog.Add2Log("Saved BMP: " + filename_in_resources_dir);
                return true;
            }
            catch
            {
                MSGlog.Add2Log("DIDNT SAVE FILE. EXEMPTION");
                TerrainEditorView.SwitchtoGeneralTab = 2;
                return false;
            }
        }
    }
}

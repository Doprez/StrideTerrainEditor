//by Idomeneas
using TerrainEditor;
using System.Linq;
using HeightMapEditor;
using Stride.Engine;
using Stride.Graphics;

namespace ImGui
{
    using System;
    using System.Numerics;
    using System.Collections.Generic;
    using Stride.Core.Serialization.Contents;
    using Stride.Core;
    using Stride.Engine;
    using Stride.Games;
    using ImGuiNET;
    using static ImGuiNET.ImGui;
    using static ImGuiExtension;
    using System.Linq;
    using System.IO;

    public class StrideAssetsView : BaseWindow
    {
        string _searchTerm = "";
        public static bool show_AllStrideAssetsViewGUI;
        protected override ImGuiWindowFlags WindowFlags => 
            ImGuiWindowFlags.NoTitleBar|
            ImGuiWindowFlags.HorizontalScrollbar;
        float xloc, yloc, screenwidth, screenheight;
        public static ImGuiIOPtr ImguiIO;
        public static Vector2 WinDims;

        public StrideAssetsView( IServiceRegistry service ) : base( service ) 
        {
            ImguiIO = GetIO();
            screenwidth = Game.GraphicsContext.CommandList.Viewport.Width;
            screenheight = Game.GraphicsContext.CommandList.Viewport.Height;
            xloc = screenwidth - Game.GraphicsContext.CommandList.Viewport.Width / 4.0f;
            yloc = 55.0f;
            WinDims = new Vector2(800.0f, 600.0f);
            show_AllStrideAssetsViewGUI = false;
            _searchTerm = "prefab";
            Entity Terrain_Entity =
                Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            tcomp = Terrain_Entity.Get<TerrainComponent>();
            if (tcomp == null) throw new Exception("Terrain component not created yet...");

        }
        TerrainComponent tcomp;

        public override void Update(GameTime gameTime)
        {
            if (TerrainEditorView.CurrentEditorMode != EditorMode.TerrainEditor) return;
            if (!show_AllStrideAssetsViewGUI) return;
            base.Update( gameTime );
        }

        protected override void OnDraw(bool collapsed)
        {
            if (!show_AllStrideAssetsViewGUI) return;
            if (collapsed) return;

            if (show_AllStrideAssetsViewGUI) Show_AllStrideAssetsView();

        }

        protected override void OnDestroy(){}

        /// <summary>
        /// Key: is the asset URL: Content.Load looks in the assets folder and doesnt use extension of the file
        /// Value: is the asset type, texture, material etc
        /// </summary>
        public static Dictionary<string, string> m_Assets = new Dictionary<string, string>();

        /// <summary>
        /// Loads all asset files
        /// </summary>
        public void LoadAssets()//ContentManager Content)
        {
            m_Assets.Clear();
            string startupPath = Directory.GetParent(Utility.Resources_Directory).Parent.FullName;
            DirectoryInfo dir = new DirectoryInfo(
                startupPath + "/TerrainEditor/Assets/");
            //Content.FileProvider.ListFiles(dir, name, VirtualSearchOption.AllDirectories);
            FileInfo[] Files = dir.GetFiles("*",//".sd",
                  SearchOption.AllDirectories);
            if (Files.Length == 0)
            {
                throw new Exception("No assets exist within the assets folder...");
            }
            //parse all files in ..\\Assets folder
            for (int j = 0; j < Files.Length; j++)
            {
                string fullfilename = Files[j].FullName;
                //open the *.sd**** file and read the typre
                var lines = File.ReadAllLines(fullfilename);
                //first character in the first line is ! always
                string asset_type = lines[0].Substring(1);
                //need to remove the part up to the assets directory and the extension
                string filenamenoext = Files[j].Name.Substring(0, Files[j].Name.
                    IndexOf("."));
                string URL = fullfilename.Substring(fullfilename.IndexOf("Assets") + 7);
                URL= URL.Substring(0, URL.IndexOf("."));
                URL=URL.Replace("\\", "/");
                m_Assets.TryAdd(URL, asset_type);

            }
            if (!string.IsNullOrEmpty(_searchTerm))
            {
                _searchResult.Clear();
                foreach (var o in m_Assets.Where(
                    o => o.Value.ToLower().Contains(_searchTerm.ToLower())))
                    _searchResult.Add(new string[2] { o.Key, o.Value });
            }
        }
        List<string[]> _searchResult = new List<string[]>();

        protected void Show_AllStrideAssetsView()
        {
            SetWindowPos(new Vector2(xloc, yloc), ImGuiCond.Appearing);
            SetWindowSize(WinDims, ImGuiCond.Always);
            ImGuiExtension.DrawSplitter();
            BeginChild("1##Show_AllStrideAssetsView",
                new Vector2(.98f * GetWindowSize().X, GetWindowSize().Y / 10), true);
            if (InputText("Search for asset type", ref _searchTerm, 64))
            {
                if (!string.IsNullOrEmpty(_searchTerm))
                {
                    _searchResult.Clear();
                    foreach (var o in m_Assets.Where(
                        o => o.Value.ToLower().Contains(_searchTerm.ToLower())))
                        _searchResult.Add(new string[2] {o.Key,o.Value }); 
                }
            }
            if (string.IsNullOrEmpty(_searchTerm))
            {
                _searchResult.Clear();
                foreach (var o in m_Assets)
                    _searchResult.Add(new string[2] { o.Key, o.Value });
            }

            TextWrapped("Showing "+ _searchResult .Count+ " Asset Types based on your search...");
            EndChild();

            BeginChild("2##Show_AllStrideAssetsView",
                new Vector2(.98f * GetWindowSize().X, .85f * GetWindowSize().Y), true);

            foreach (var obj in _searchResult)
            {
                TextWrapped("Asset Name: " + obj[0]);
                SameLine();
                if (obj[1].ToLower()=="model" && 
                    Button("Add to Scene##AddtoScenemodel" + obj[0]))
                {
                    AreaXML.AddModel(obj[0], tcomp);
                }
                if (obj[1].ToLower() == "prefabasset" &&  
                    Button("Add to Scene##AddtoSceneprefabasset" + obj[0]))
                {  AreaXML.AddPrefab(obj[0], tcomp); }
                SameLine();
                TextWrapped("Asset Type: " + obj[1]);
                Spacing();
                Spacing();
                Separator();
            }

            EndChild();
        }

    }
}
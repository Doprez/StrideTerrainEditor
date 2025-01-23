//by Idomeneas
using TerrainEditor;
using System.Linq;
using HeightMapEditor;

namespace ImGui
{
    using System.Numerics;
    using System.Collections.Generic;
    using Guid = System.Guid;
    
    using Stride.Core;
    using Stride.Engine;
    using Stride.Games;

    using ImGuiNET;
    using static ImGuiNET.ImGui;
    using static ImGuiExtension;
    using System.Linq;
    using System;

    public class AreaObjectsView : BaseWindow
    {
        List<IAreaObject> _searchResult = new List<IAreaObject>();
        string _searchTerm = "";
        public static bool show_AreaObjectsViewGUI;
        protected override ImGuiWindowFlags WindowFlags => 
            ImGuiWindowFlags.NoTitleBar|
            ImGuiWindowFlags.HorizontalScrollbar;
        float xloc, yloc, screenwidth, screenheight;
        public static ImGuiIOPtr ImguiIO;
        public static Vector2 WinDims;

        public AreaObjectsView( IServiceRegistry service ) : base( service ) 
        {
            ImguiIO = GetIO();
            screenwidth = Game.GraphicsContext.CommandList.Viewport.Width;
            screenheight = Game.GraphicsContext.CommandList.Viewport.Height;
            xloc = screenwidth - Game.GraphicsContext.CommandList.Viewport.Width / 4.0f;
            yloc = 55.0f;
            WinDims = new Vector2(800.0f, 600.0f);
            show_AreaObjectsViewGUI = false;
            Entity Terrain_Entity =
             Game.SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            tcomp = Terrain_Entity.Get<TerrainComponent>();
            if (tcomp == null) throw new Exception("Terrain component not created yet...");
        }
        TerrainComponent tcomp;

        public override void Update(GameTime gameTime)
        {
            if (TerrainEditorView.CurrentEditorMode != EditorMode.TerrainEditor) return;
            if (!show_AreaObjectsViewGUI) return;
            base.Update( gameTime );
        }

        protected override void OnDraw(bool collapsed)
        {
            if (!show_AreaObjectsViewGUI) return;
            if (collapsed) return;

            if (show_AreaObjectsViewGUI) Show_AreaObjectsView();

        }

        protected override void OnDestroy(){}

        protected void Show_AreaObjectsView()
        {
            SetWindowPos(new Vector2(xloc, yloc), ImGuiCond.Appearing);
            SetWindowSize(WinDims, ImGuiCond.Always);

            ImGuiExtension.DrawSplitter();
            BeginChild("1##Show_AreaObjectsView", 
                new Vector2(.98f * GetWindowSize().X, GetWindowSize().Y/10), true); 
 
            if (InputText("Search for object", ref _searchTerm, 64))
            {
                if (!string.IsNullOrEmpty(_searchTerm))
                {
                    _searchResult = AreaXML.GetAreaObjects().Where(
                        o => o.ObjectName.ToLower().Contains(_searchTerm.ToLower())).ToList();
                }
            }
            if (string.IsNullOrEmpty(_searchTerm))
                _searchResult = AreaXML.GetAreaObjects().ToList<IAreaObject>();
            TextWrapped("Showing "+ _searchResult.Count+ " Area Objects based on your search...");
            EndChild();

            BeginChild("2##Show_AreaObjectsView",
                new Vector2(.98f*GetWindowSize().X, .85f* GetWindowSize().Y), true);
            foreach (IAreaObject obj in _searchResult)
            {
                Separator();
                Separator();
                TextColored(Stride.Core.Mathematics.Color.DarkGoldenrod.ToVector4().
                    AsNumericVec4(), "Object Name: " + obj.ObjectName+
                    ", Instances: " + obj.NumInstances+
                    ", ObjectType: " + AreaXML.GetObjectTypeString(obj.ObjType));
                Separator();
                for (int i = 0; i < obj.NumInstances; i++)
                {
                    TextWrapped("Instance "+(i+1));
                    SameLine();
                    if (Button("Go To##GoTo1" + obj.ObjectName+i))
                    {
                        CameraComponent camera = Game.Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera;
                        if (camera != null)
                        {
                            camera.Entity.GetParent().Transform.Position =
                                new Vector3(obj.objectInstances[i].Position.X, 
                                obj.objectInstances[i].Position.Y + 100,
                                obj.objectInstances[i].Position.Z + 10).AsStrideVec3();
                            camera.Entity.GetParent().Transform.RotationEulerXYZ =
                                new Vector3(-1.345f, -0.122f, 0.03f).AsStrideVec3();
                        }
                    }
                    SameLine();
                    if (Button("Remove from Scene##RemovefromScene" + obj.ObjectName + i))
                    {
                        AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                        return;
                    }
                    TextWrapped("Entity Name: " + obj.objectInstances[i].ObjectEntity.Name);
//                    TerrainEditorView.HelpMarker("The instancing is not implemented until you save and/or load the area xml");
                    Vector3 vec = obj.objectInstances[i].Position;
                    if (InputFloat3("Object Position##" + obj.ObjectName + i, ref vec))
                    {
                        obj.objectInstances[i].Position = vec;
                        obj.objectInstances[i].ObjectEntity.Transform.Position = vec.AsStrideVec3();
                    }
                    vec = obj.objectInstances[i].Rotation;
                    if (InputFloat3("Object Rotation##" + obj.ObjectName + i, ref vec))
                    {
                        obj.objectInstances[i].Rotation = vec;
                        obj.objectInstances[i].ObjectEntity.Transform.RotationEulerXYZ = vec.AsStrideVec3();
                    }
                    vec = obj.objectInstances[i].Scale;
                    if (InputFloat3("Object Scale##" + obj.ObjectName + i, ref vec))
                    {
                        obj.objectInstances[i].Scale = vec;
                        obj.objectInstances[i].ObjectEntity.Transform.Scale = vec.AsStrideVec3();
                    }
                    Vector4 vec1 = obj.objectInstances[i].Hue;
                    if (InputFloat4("Object Hue##" + obj.ObjectName + i, ref vec1))
                    {
                        obj.objectInstances[i].Hue = vec1;
                    }
                    Separator();
                }
            }
            EndChild();

        }

    }
}
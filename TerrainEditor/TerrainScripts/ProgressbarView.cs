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

    public class ProgressbarView : BaseWindow
    {
        protected override ImGuiWindowFlags WindowFlags => 
            ImGuiWindowFlags.NoTitleBar|//ImGuiWindowFlags.NoBackground|
            ImGuiWindowFlags.NoResize| ImGuiWindowFlags.NoMove;
        float xloc, yloc, screenwidth, screenheight;
        public static ImGuiIOPtr ImguiIO;
        Vector2 WinDims,WinPos;

        public ProgressbarView( IServiceRegistry service ) : base( service ) 
        {
            ImguiIO = GetIO();
            screenwidth = Game.GraphicsContext.CommandList.Viewport.Width;
            screenheight = Game.GraphicsContext.CommandList.Viewport.Height;
            xloc = screenwidth / 2 -150;
            yloc = screenheight/2-35;
            WinDims = new Vector2(300.0f, 70.0f);
            WinPos = new Vector2(xloc, yloc);
        }

        public override void Update(GameTime gameTime)
        {
            if (TerrainEditorView.CurrentEditorMode != EditorMode.TerrainEditor) return;
            if (!ShowProgressBar) return;
            base.Update( gameTime );
        }

        protected override void OnDraw(bool collapsed)
        {
            if (!ShowProgressBar) return;
            if (collapsed) return;
            DisplayProgressBar();
        }

        protected override void OnDestroy(){}

        public static bool ShowProgressBar = false;
        float ProgressBar_Perc = 0;
        public static int ProgressBar_Max = 100, ProgressBar_CurrVal = 0;
        public void DisplayProgressBar()
        {
            SetWindowPos(WinPos, ImGuiCond.Always);
            SetWindowSize(WinDims, ImGuiCond.Always);

            Text("Progress Bar...");
            ProgressBar_Perc = 1.0f*ProgressBar_CurrVal / ProgressBar_Max;
            PushItemWidth(100);
            ProgressBar(ProgressBar_Perc); // Will draw the label as a percent
            PopItemWidth();
            // ProgressBar(3, 10); // Will draw the label as a fraction
            if (ProgressBar_Perc >= 1.0f) ShowProgressBar = false;
        }

    }
}
//by Idomeneas
using ImGui;
using ImGuiNET;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System;
using System.Numerics;
using static ImGuiNET.ImGui;

namespace TerrainEditor
{
    /// <summary>
    /// all in game gumps, popups etc go in here
    /// </summary>
    public class TerrainInGameGumps : BaseWindow
    {
        public static bool Show_MiniMap = true,minimapdragging=false;
        public static int MiniMapHeight = 20;
        public static Vector2 MiniMapDims;
        protected override ImGuiWindowFlags WindowFlags => 
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | 
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground;

        float xloc, yloc;
        public static ImGuiIOPtr ImguiIO;
        public static IntPtr MinimapImageIntPtr;
        public static Texture MinimapTexture = new Texture();
        public TerrainInGameGumps(Stride.Core.IServiceRegistry services) : base(services)
        {
            ImguiIO = GetIO();
            xloc = MathF.Max(Game.GraphicsContext.CommandList.Viewport.Width
                / 2.0f, Game.GraphicsContext.CommandList.Viewport.Width - 290.0f);
            yloc = 20.0f;
            MiniMapDims = new Vector2(276.0f, 276.0f);
            Show_MiniMap = false;
        }

        protected override void OnDestroy() { }

        public override void Update(GameTime gameTime)
        {
            if (!Show_MiniMap) return;
            base.Update(gameTime);
        }

        protected override void OnDraw(bool collapsed)
        {
            if (collapsed) return;
            SetWindowPos(new Vector2(xloc,yloc),ImGuiCond.Always);
            SetWindowSize(MiniMapDims, ImGuiCond.Always);
            TerrainEditorView.DrawImage(MinimapImageIntPtr, new Vector2(256.0f, 256.0f));
            if (IsItemHovered())
            {
                if (ImguiIO.MouseWheel != 0)
                { 
                    if(ImguiIO.MouseWheel>0)
                        MiniMapHeight -= 5; 
                    else
                        MiniMapHeight += 5;
                    if (MiniMapHeight < 5) MiniMapHeight = 5;
                    if (MiniMapHeight > 100) MiniMapHeight = 100;
                }
                if (IsMouseDown(ImGuiMouseButton.Left)) //dragging
                {
                   // Cursor.Position = new Point((int)(xloc + MiniMapDims.X / 2), (int)(yloc + MiniMapDims.Y / 2));
                    minimapdragging = true;
                }
            }
            if (!IsMouseDown(ImGuiMouseButton.Left) 
                //||                xloc<0 || yloc<0 || xloc+ MiniMapDims.X>                 Game.GraphicsContext.CommandList.Viewport.Width ||                yloc + MiniMapDims.Y>                 Game.GraphicsContext.CommandList.Viewport.Height
                ) //reset all dragging here
            {                
                minimapdragging = false;
            }
            if (xloc < 10) xloc = 10;
            if (yloc < 10) yloc = 10;
            if(xloc + MiniMapDims.X > Game.GraphicsContext.CommandList.Viewport.Width)
              xloc= Game.GraphicsContext.CommandList.Viewport.Width-MiniMapDims.X-10;
            if (yloc + MiniMapDims.Y > Game.GraphicsContext.CommandList.Viewport.Height)
                yloc = Game.GraphicsContext.CommandList.Viewport.Height - MiniMapDims.Y-10;
            if (minimapdragging)//MathF.Abs(pos.X - xloc) < 10 && MathF.Abs(pos.Y - yloc) < 10)
            {
                Vector2 pos = GetMousePos();
                xloc = xloc * .9f + .1f * (pos.X- MiniMapDims.X/2);
                yloc = yloc * .9f + .1f * (pos.Y - MiniMapDims.Y / 2);
            }
        }

    }
}

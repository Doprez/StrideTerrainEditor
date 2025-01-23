//by Idomeneas
using ImGui;
using ImGuiNET;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System;
using System.Linq;
using System.Numerics;
using static ImGuiNET.ImGui;

namespace TerrainEditor
{
    /// <summary>
    /// all in game gumps, popups etc go in here
    /// </summary>
    public class TerrainTilesGumps : BaseWindow
    {
        public static bool Show_WorldMap = true, WorldMapdragging = false;
        public static int MiniMapHeight = 20;
        public static Vector2 WorldMapDims;
        protected override ImGuiWindowFlags WindowFlags =>
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground;

        float xloc, yloc;
        public static ImGuiIOPtr ImguiIO;
        public static IntPtr WorldMapIntPtr;
        public static Texture WorldMapTexture = new Texture();
        public TerrainTilesGumps(Stride.Core.IServiceRegistry services) : base(services)
        {
            ImguiIO = GetIO();
            xloc = MathF.Max(Game.GraphicsContext.CommandList.Viewport.Width
                / 2.0f, Game.GraphicsContext.CommandList.Viewport.Width - 290.0f);
            yloc = 20.0f;
            WorldMapDims = new Vector2(276.0f, 276.0f);
            Show_WorldMap = false;
        }

        protected override void OnDestroy() { }

        public override void Update(GameTime gameTime)
        {
            if (!Show_WorldMap) return;
            base.Update(gameTime);
        }

        protected override void OnDraw(bool collapsed)
        {
            if (collapsed) return;
            SetWindowPos(new Vector2(xloc, yloc), ImGuiCond.Always);
            SetWindowSize(WorldMapDims, ImGuiCond.Always);
            TerrainEditorView.DrawImage(WorldMapIntPtr, new Vector2(256.0f, 256.0f));
            if (IsItemHovered())
            {
                if (ImguiIO.MouseWheel != 0)
                {
                    if (ImguiIO.MouseWheel > 0)
                        MiniMapHeight -= 5;
                    else
                        MiniMapHeight += 5;
                    if (MiniMapHeight < 5) MiniMapHeight = 5;
                    if (MiniMapHeight > 100) MiniMapHeight = 100;
                }
                if (IsMouseDown(ImGuiMouseButton.Right)) //dragging
                {
                    // Cursor.Position = new Point((int)(xloc + MiniMapDims.X / 2), (int)(yloc + MiniMapDims.Y / 2));
                    WorldMapdragging = true;
                }
            }
            if (!IsMouseDown(ImGuiMouseButton.Right)
                //||                xloc<0 || yloc<0 || xloc+ MiniMapDims.X>                 Game.GraphicsContext.CommandList.Viewport.Width ||                yloc + MiniMapDims.Y>                 Game.GraphicsContext.CommandList.Viewport.Height
                ) //reset all dragging here
            {
                WorldMapdragging = false;
            }
            if (!WorldMapdragging && IsItemClicked(ImGuiMouseButton.Left)) //clicked
            {
                TerrainTiles.Index_inWorld.X = (int)(xloc-10+ WorldMapDims.X - GetMousePos().X);
                TerrainTiles.Index_inWorld.Y = (int)(yloc-10+ WorldMapDims.Y - GetMousePos().Y);
                if (TerrainTiles.Index_inWorld.X < 0)
                    TerrainTiles.Index_inWorld.X = 0;
                if (TerrainTiles.Index_inWorld.X >= TerrainTiles.m_NumTilesWideX)
                    TerrainTiles.Index_inWorld.X = TerrainTiles.m_NumTilesWideX-1;
                if (TerrainTiles.Index_inWorld.Y < 0)
                    TerrainTiles.Index_inWorld.Y = 0;
                if (TerrainTiles.Index_inWorld.Y >= TerrainTiles.m_NumTilesHighZ)
                    TerrainTiles.Index_inWorld.Y = TerrainTiles.m_NumTilesHighZ-1;
                Entity a = Game.Services.GetService<SceneSystem>().SceneInstance.
                    FirstOrDefault(a => a.Name == "CameraMultiType");
                a.Transform.Position = TerrainTiles.Tiles[
                    TerrainTiles.Index_inWorld.X +
                    TerrainTiles.m_NumTilesWideX *
                    TerrainTiles.Index_inWorld.Y].WorldPosition;
                a.Transform.Position.Y = 
                    TerrainEditorView.InGameTiles_Camera_Height;
                a.Transform.Position.X += TerrainTiles.Width / 2;
                a.Transform.Position.Z += TerrainTiles.Height / 2;
                return;
            }

            if (xloc < 10) xloc = 10;
            if (yloc < 10) yloc = 10;
            if (xloc + WorldMapDims.X > Game.GraphicsContext.CommandList.Viewport.Width)
                xloc = Game.GraphicsContext.CommandList.Viewport.Width - WorldMapDims.X - 10;
            if (yloc + WorldMapDims.Y > Game.GraphicsContext.CommandList.Viewport.Height)
                yloc = Game.GraphicsContext.CommandList.Viewport.Height - WorldMapDims.Y - 10;
            if (WorldMapdragging)//MathF.Abs(pos.X - xloc) < 10 && MathF.Abs(pos.Y - yloc) < 10)
            {
                Vector2 pos = GetMousePos();
                xloc = xloc * .9f + .1f * (pos.X - WorldMapDims.X / 2);
                yloc = yloc * .9f + .1f * (pos.Y - WorldMapDims.Y / 2);
            }
        }

    }
}

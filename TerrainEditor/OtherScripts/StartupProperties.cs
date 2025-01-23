//by Idomeneas
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace TerrainEditor
{
    public class CustomGameWindows : Game
    {
        protected override void Initialize()
        {
            // Add custom code here
            base.Initialize();
           // GraphicsDeviceManager.IsFullScreen = true;
        }
    }
    
    public class StartupProperties : StartupScript
    {
        // Declared public member fields and properties will show in the game studio

        public override void Start()
        {
            this.Game.Window.Title = "Stride Terrain Editor";
            this.Game.Window.Name = "Stride Terrain Editor";
            this.Game.Window.AllowUserResizing = true;
            this.Game.Window.FullscreenIsBorderlessWindow = true;
            //this.Game.Window.IsFullscreen = true;
            this.Game.Window.Position=new Int2(0, 0);
            this.Game.Window.SetSize(new Int2(
                90 * GraphicsDevice.Adapter.Outputs[0].DesktopBounds.Width/100,
                90*GraphicsDevice.Adapter.Outputs[0].DesktopBounds.Height/100));
        }

        enum FullscreenMode
        {
            Windowed,
            Fullscreen,
            Borderless
        }

        void SetResolution(int width, int height, FullscreenMode mode)
        {
            var game = (Game)Game;

            game.GraphicsDeviceManager.PreferredBackBufferWidth = width;
            game.GraphicsDeviceManager.PreferredBackBufferHeight = height;
            game.GraphicsDeviceManager.ApplyChanges();

            game.Window.IsFullscreen = mode == FullscreenMode.Fullscreen;
            game.Window.IsBorderLess = mode == FullscreenMode.Borderless;

            if (mode == FullscreenMode.Borderless)
            {
                game.Window.Position = new Int2(0, 0);
            }
        }
    }
}

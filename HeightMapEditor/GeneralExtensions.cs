//by Idomeneas
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Color = Stride.Core.Mathematics.Color;
using PixelFormat = Stride.Graphics.PixelFormat;

namespace HeightMapEditor
{
    public static class GeneralExtensions
    {
        public static Vector4 Fix01(this Vector4 vec,float min=0,float max=1)
        {
            Vector4 ret = vec;
            if (ret.X < min) ret.X = min;
            if (ret.Y < min) ret.Y = min;
            if (ret.Z < min) ret.Z = min;
            if (ret.W < min) ret.W = min;
            if(ret.X > max) ret.X = max;
            if (ret.Y > max) ret.Y = max;
            if (ret.Z > max) ret.Z = max;
            if (ret.W > max) ret.W = max;
            return ret;
        }
        
        public static Vector3 GetWorldPosition(this CameraComponent camera)
        {
            var viewMatrix = camera.ViewMatrix;
            viewMatrix.Invert();

            var cameraPosition = viewMatrix.TranslationVector;

            return cameraPosition;
        }
        
        public static CameraComponent TryGetMainCamera(this SceneSystem sceneSystem)
        {
            CameraComponent camera = null;
            if (sceneSystem.GraphicsCompositor.Cameras.Count == 0)
            {
                // The compositor wont have any cameras attached if the game is running in editor mode
                // Search through the scene systems until the camera entity is found
                // This is what you might call "A Hack"
                foreach (var system in sceneSystem.Game.GameSystems)
                {
                    if (system is SceneSystem editorSceneSystem)
                    {
                        foreach (var entity in editorSceneSystem.SceneInstance.RootScene.Entities)
                        {
                            if (entity.Name == "Camera Editor Entity")
                            {
                                camera = entity.Get<CameraComponent>();
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                camera = sceneSystem.GraphicsCompositor.Cameras[0].Camera;
            }

            return camera;
        }

        /// <summary>
        /// A cute routine that smooths and linearly interpolates the edges from 
        /// the height within 50 pixels from the edge towards a height of level at the edge
        /// </summary>
        /// <param name="Colors"></param>
        /// <param name="m_Width"></param>
        /// <param name="m_Height"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Color[] EdgesToHeightLevel(this Color[] Colors, 
            int m_Width, int m_Height,float level=0)
        {
            if (m_Width < 50 || m_Height < 50)
                throw new Exception("Height and Width must be over 50...");
            int i, j, index;
            float mux = m_Width / 2, muy = m_Height / 2,
                sigx = m_Width / 6,
                sigy = m_Width / 6, dist_from_edgex, dist_from_edgey,
                oright, htdist;
            float[] ImageHeights = new float[m_Width * m_Height];
            //do this after the first height reduction
            //first pass: set edges to target height level
     /*       for (i = 0; i < m_Width; i++)
            {
                ImageHeights[i + m_Width] = level.AsStrideColor().ToFloat();
                ImageHeights[i + (m_Height - 1) * m_Width] = level.AsStrideColor().ToFloat();
            }
            for (j = 0; j < m_Height; j++)
            {
                ImageHeights[0 + j * m_Width] = level.AsStrideColor().ToFloat();
                ImageHeights[m_Height - 1 + j * m_Width] = level.AsStrideColor().ToFloat();
            }*/

            //now dont touch the edges
            for (i = 1; i < m_Width-1; i++)
            {
                for (j = 1; j < m_Height-1; j++)
                {
                    //do not touch anything within half a distance from center
                  /*  if (MathF.Abs(i - m_Width / 2) < m_Width / 4 &&
                        MathF.Abs(j - m_Height / 2) < m_Height / 4)
                    {
                        ImageHeights[i + j * m_Width] = Colors[i + j * m_Width].ToFloat();
                        continue;
                    }*/
                    oright = Colors[i + j * m_Width].ToFloat();
                    htdist = MathF.Abs(oright - level);
                    //dont drop too fast or it will look pretty bad
                    //get min distance from edges
                    dist_from_edgex = MathF.Min(i, MathF.Abs(i - m_Width));
                    dist_from_edgey = MathF.Min(j, MathF.Abs(j - m_Height));
                    float pow =
                        MathF.Abs(0.15f-1 + Utility.GetYBellShape(
                            i,j,//
                           //dist_from_edgex,dist_from_edgey,
                           mux, muy,
                            sigx, sigy, 0, 1));// /
                    //need to reduce height gradually, depending on its
                    //magnitude and distance to the edges
                    ImageHeights[i + j * m_Width] = //wtstart * level.AsStrideColor().ToFloat() +
                      (oright - pow * htdist)/1.2f;//could normalize more here
                }
            }

            //second pass: set edges to target height level within 10 pixels
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < 50; j++)
                {
                    float wt1=j,//distance from edge
                     wt2= MathF.Abs(j - 50);
                    float wt=wt1/(wt1+wt2);
                    ImageHeights[i + m_Width*j] = 
                        level.AsStrideColor().ToFloat()*(1-wt)
                        + wt*ImageHeights[i + m_Width * 50];
                    ImageHeights[i + (m_Height-j - 1) * m_Width] = 
                        level.AsStrideColor().ToFloat() * (1 - wt)
                        + wt * ImageHeights[i + (m_Height - 50 - 1) * m_Width];
                }
            }

            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < 50; i++)
                {
                    float wt1 = i,//distance from edge
                        wt2 = MathF.Abs(i - 50);
                    float wt = wt1 / (wt1 + wt2);
                    ImageHeights[i + j * m_Width] = 
                        level.AsStrideColor().ToFloat() * (1 - wt)
                        + wt * ImageHeights[50 + m_Width * j];
                    ImageHeights[m_Height-i - 1 + j* m_Width] = 
                        level.AsStrideColor().ToFloat() * (1 - wt)
                        + wt * ImageHeights[m_Height - 50 - 1 + j * m_Width];
                }
            }

            Color[] HeightMapColors = new Color[ImageHeights.Length];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float ht = ImageHeights[index];
                    // float height = (ht - tcomp.HeightRange.X) *                        PerlinNoise.HeightMultiplier /                        (tcomp.HeightRange.Y - tcomp.HeightRange.X);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte b = ht.ToByte();
                        HeightMapColors[j * m_Width + i] =
                            new Color(b, b, b, 255);
                    }
                    else
                        HeightMapColors[j * m_Width + i] =
                            ht.AsStrideColor();
                }
            }
            Array.Clear(ImageHeights, 0, ImageHeights.Length);
            return HeightMapColors;
        }

        public static Color[] Smooth(this Color[] Colors, int m_Width, int m_Height)
        {
            int i, j, index;
            float[] ImageHeights = new float[m_Width * m_Height];
            for (i = 1; i < m_Width - 1; i++)
            {
                for (j = 1; j < m_Height - 1; j++)
                {
                    ImageHeights[i + j * m_Width] = Colors[i+j*m_Width].ToFloat();
                }
            }
                    
            #region smoothers
            for (i = 1; i < m_Width - 1; i++)
            {
                for (j = 1; j < m_Height - 1; j++)
                {
                    ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 80.0f);
                }
            }
            i = 0; j = 0;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 30.0f);
            i = 0; j = m_Height - 1;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 30.0f);
            i = m_Width - 1; j = 0;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 30.0f);
            i = m_Width - 1; j = m_Height - 1;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 30.0f);
            for (j = 1; j < m_Height - 1; j++)
            {
                i = 0;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                            +ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 50.0f);
                i = m_Width - 1;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);
            }

            for (i = 1; i < m_Width - 1; i++)
            {
                j = 0;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                    ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);

                j = m_Height - 1;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);
            }
            #endregion smoothers

            Color[] HeightMapColors=new Color[ImageHeights.Length];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float ht = ImageHeights[index];
                   // float height = (ht - tcomp.HeightRange.X) *                        PerlinNoise.HeightMultiplier /                        (tcomp.HeightRange.Y - tcomp.HeightRange.X);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte b = ht.ToByte();
                        HeightMapColors[j * m_Width + i] =
                            new Color(b, b, b, 255);
                    }
                    else
                        HeightMapColors[j * m_Width + i] =
                            ht.AsStrideColor();
                }
            }
            Array.Clear(ImageHeights, 0, ImageHeights.Length);
            return HeightMapColors;
        }

        public static Texture ToTexture(this float[] floats,
    int m_Width, int m_Height,
GraphicsDevice GraphicsDevice, CommandList CommandList)
        {
            int i, j, index;
            Texture tex = Texture.New2D(GraphicsDevice, m_Width,
                m_Height, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
            Color[] heightValues = new Color[m_Width * m_Height];
            // Get the height information and put it in the array
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    heightValues[index] =
                       /*HeightmapUtils.ConvertToShortHeight(
                           short.MinValue, short.MaxValue,
                           (floats[index] -PerlinNoise.HeightMin) * 
                           PerlinNoise.HeightMultiplier /
                            (PerlinNoise.HeightMax - PerlinNoise.HeightMin)
                            )*/
                        floats[index].AsStrideColor();
                }
            }
            tex.SetData(CommandList, heightValues);
            return tex;
        }

        public static Texture ToTexture(this Color[] Colors,
            int m_Width, int m_Height, GraphicsDevice GraphicsDevice, 
            CommandList CommandList)
        {
            Texture tex = Texture.//New2D(GraphicsDevice, m_Width, m_Height,                 PixelFormat.R8G8B8A8_UNorm,TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
            New2D<Color>(GraphicsDevice, m_Width, m_Height, PixelFormat.R8G8B8A8_UNorm,Colors, TextureFlags.ShaderResource, GraphicsResourceUsage.Dynamic);
  //          tex.SetData(CommandList, Colors);
 //           tex.ToStaging();
            return tex;
        }
        /// <summary>
        /// Channel=0-R, 1-G, 2-B, 3-A
        /// </summary>
        /// <param name="Colors"></param>
        /// <param name="Size"></param>
        /// <returns></returns>
        public static (int CountRed, int CountGreen, int CountBlue, int CountAlpha) 
            CountChannel(this Color[] Colors,Int2 Size)
        {
            int CountRed=0, CountGreen=0, CountBlue=0, CountAlpha = 0, i, j, index;
            for (i = 0; i < Size.X; i++)
            {
                for (j = 0; j < Size.Y; j++)
                {
                    index = (Size.X * j) + i;
                    if (Colors[index].R > 0) CountRed++;
                    if (Colors[index].G > 0) CountGreen++;
                    if (Colors[index].B > 0) CountBlue++;
                    if (Colors[index].A > 0) CountAlpha++;
                }
            }
            return (CountRed, CountGreen, CountBlue, CountAlpha);
        }

        public static Color GetColorAt(this Color[] Colors, Int2 Size, int x, int y)
        {
            Color col = Color.Black;
            if (!(x >= 0 && x <= Size.X-1 && y >= 0 && y <= Size.Y-1))
            {
                return col;
            }
            col = Colors[y * Size.X + x];
            return col;
        }
        public static float CountNeighbors(Int2 Size, int x, int y)
        {
            float sum = 0;
            if (x >= 0) sum++;
            if (y >= 0) sum++;
            if (x <= Size.X) sum++;
            if (y <= Size.Y) sum++;
            if (x - 1 >= 0) sum++;
            if (y - 1 >= 0) sum++;
            if (x + 1 <= Size.X) sum++;
            if (y + 1 <= Size.Y) sum++;
            return sum;
        }

        public static short[] ToShorts(this float[] heights)
        {
            int i, len = heights.Length;
            short[] shorts = new short[len];
            // Get the height information and put it in the array
            for (i = 0; i < len; i++)
            {
                shorts[i] = heights[i].ToShort();

            }
            return shorts;
        }

        public static bool IsAlphaBitmap(ref System.Drawing.Imaging.BitmapData BmpData)
        {
            byte[] Bytes = new byte[BmpData.Height * BmpData.Stride];
            Marshal.Copy(BmpData.Scan0, Bytes, 0, Bytes.Length);
            for (var p = 3; p < Bytes.Length; p += 4)
            {
                if (Bytes[p] != 255) return true;
            }
            return false;
        }

        /// <summary>
        /// Cant handle 0 alpha channel
        /// </summary>
        /// <param name="Colors"></param>
        /// <param name="m_Width"></param>
        /// <param name="m_Height"></param>
        /// <param name="filename"></param>
        /// <param name="saveit"></param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this Color[] Colors,
    int m_Width, int m_Height, string filename, bool saveit = true)
        {
            Bitmap im = new Bitmap(m_Width, m_Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            string fname = Utility.Resources_TerrainEditor_Directory +
                filename;
            for (int i = 0; i < m_Width; i++)
            {
                for (int j = 0; j < m_Height; j++)
                {
                    System.Drawing.Color pixel = System.Drawing.Color.FromArgb(
                        Colors[i + j * m_Width].A,
                        Colors[i + j * m_Width].R,
                        Colors[i + j * m_Width].G,
                        Colors[i + j * m_Width].B);
                    im.SetPixel(i, j, pixel);
                }
            }
            if (saveit)
            {
                //all methods below set the alpha channel to 255 even if it is zero...
                 using (var outStream = System.IO.File.OpenWrite(fname))
                 im.Save(outStream, System.Drawing.Imaging.ImageFormat.Bmp);
                // im.Save(outStream, System.Drawing.Imaging.ImageFormat.Bmp);
               // File.WriteAllBytes(Utility.Resources_TerrainEditor_Directory +"1"+
                 //   filename, channelValues);
                /*ImageCodecInfo Encoder = GetEncoder(ImageFormat.Bmp);

                // Create an Encoder object based on the GUID
                // for the Quality parameter category.
                System.Drawing.Imaging.Encoder myEncoder =
                    System.Drawing.Imaging.Encoder.Quality;

                // Create an EncoderParameters object.
                // An EncoderParameters object has an array of EncoderParameter
                // objects. In this case, there is only one
                // EncoderParameter object in the array.
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder,
                    50L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                im.Save(Utility.Resources_TerrainEditor_Directory+"50"+filename, Encoder,
                    myEncoderParameters);

                myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                im.Save(Utility.Resources_TerrainEditor_Directory + "100" + filename, Encoder,
                    myEncoderParameters);

                // Save the bitmap as a JPG file with zero quality level compression.
                myEncoderParameter = new EncoderParameter(myEncoder, 0L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                im.Save(Utility.Resources_TerrainEditor_Directory + "0" + filename, 
                    Encoder, myEncoderParameters);*/
            }
            return im;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        /// <summary>
        /// The only function that properly allows saving 0 alpha channel
        /// </summary>
        /// <param name="Colors"></param>
        /// <param name="filename"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static bool SaveBMP32Image(this Color[] Colors,
            string filename, int w, int h,bool saveindir=true)
        {
            string fname = filename;
            if(saveindir)
            {
                fname = Utility.Resources_TerrainEditor_Directory + filename;
            }
            Bitmap im = new Bitmap(w, h, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            //Colors.ToBitmap(w, h, fname, true);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    System.Drawing.Color pixel = System.Drawing.Color.FromArgb(
                        Colors[i + j * w].A,
                        Colors[i + j * w].R,
                        Colors[i + j * w].G,
                        Colors[i + j * w].B);
                    im.SetPixel(i, j, pixel);
                }
            }
            // using (var outStream = System.IO.File.OpenWrite(fname))
            im.Save(fname);// outStream, System.Drawing.Imaging.ImageFormat.Bmp);
            return true;
        }

    }
}

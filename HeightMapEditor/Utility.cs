//by Idomeneas
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector4 = System.Numerics.Vector4;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;
using Point = System.Drawing.Point;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;
using Stride.Engine;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;
using Stride.Rendering;
using System.Reflection;
using System.Drawing;
using Image = System.Drawing.Image;
using Color = Stride.Core.Mathematics.Color;
using Stride.TextureConverter;
using System.Xml;
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;

namespace HeightMapEditor
{
    public static class Utility
    {
        public static PixelFormat QaLPixelFormat = PixelFormat.R8G8B8A8_UNorm;
        public static string Resources_Directory = "";
        public static string Resources_TerrainEditor_Directory = "";
        public static string Resources_TerrainEditorAreas_Directory = "";
        public static string Resources_WorldTile_Directory = "";

        private static Color GetNormalMapColor(Vector2 uvCoords)
        {
            int a = 255;
            float fr = 255f, fg = 255f, fb = 255f;
            fr *= uvCoords.X;
            fg *= 1f - uvCoords.Y;
            fb *= 1f - Utility.BoundValue01(Vector2.Distance(uvCoords, new Vector2(0.5f, 0.5f)) + 0.14f, 0, 1);
            int r = (int)Utility.BoundValue01(MathF.Floor(fr), 0, 255);
            int g = (int)Utility.BoundValue01(MathF.Floor(fg), 0, 255);
            int b = (int)Utility.BoundValue01(MathF.Floor(fb), 0, 255);
            Color finalColor = new Color(r, g, b,a);
            return finalColor;
        }

        static float[,] sobel_kernel_x_dir = new float[,] { { -1f, 0f, 1f }, { -2f, 0f, 2f }, { -1f, 0f, 1f } };
        static float[,] sobel_kernel_y_dir = new float[,] { { -1f, -2f, -1f }, { 0f, 0f, 0f }, { 1f, 2f, 1f } };
        
        private static Color GetSobelFilter(int x,int y, Color[] sourceColors,
            int Width, int Height, float power)
        {
            int posx = x, posy = y;
            Color center = sourceColors[x + y * Width] * power;            
            Color up = sourceColors[x + (y-1) * Width] * power;
            Color down = sourceColors[x + (y+1) * Width] * power;
            Color left = sourceColors[x-1 + y * Width] * power;
            Color right = sourceColors[x+1 + y * Width] * power;
            Color upleft = sourceColors[x-1 + (y-1) * Width] * power;
            Color upright = sourceColors[x+1 + (y - 1) * Width] * power;
            Color downleft = sourceColors[x-1 + (y + 1) * Width] * power;
            Color downright = sourceColors[x+1 + (y + 1) * Width] * power;

            #region pixel calcs
            Color pixel_x = sobel_kernel_x_dir[0, 0] * upleft +
                sobel_kernel_x_dir[0, 1] * up + 
                sobel_kernel_x_dir[0, 2] * upright + 
                sobel_kernel_x_dir[1, 0] * left +  
                sobel_kernel_x_dir[1, 1] * center + 
                sobel_kernel_x_dir[1, 2] * right + 
                sobel_kernel_x_dir[2, 0] * downleft + 
                sobel_kernel_x_dir[2, 1] * down +
                sobel_kernel_x_dir[2, 2] * downright;
            Color pixel_y = sobel_kernel_y_dir[0, 0]* upleft +
                sobel_kernel_y_dir[0, 1] * up +
                sobel_kernel_y_dir[0, 2] * upright + 
                sobel_kernel_y_dir[1, 0] * left + 
                sobel_kernel_y_dir[1, 1] * center + 
                sobel_kernel_y_dir[1, 2] * right +
                sobel_kernel_y_dir[2, 0] * downleft + 
                sobel_kernel_y_dir[2, 1] * down +   
                sobel_kernel_y_dir[2, 2] * downright;
            #endregion pixel calcs

            float valR = Utility.BoundValue0255( MathF.Ceiling(MathF.Sqrt((pixel_x.R * pixel_x.R) + (pixel_y.R * pixel_y.R))));
            float valG = Utility.BoundValue0255(MathF.Ceiling(MathF.Sqrt((pixel_x.G * pixel_x.G) + (pixel_y.G * pixel_y.G))));
            float valB = Utility.BoundValue0255(MathF.Ceiling(MathF.Sqrt((pixel_x.B * pixel_x.B) + (pixel_y.B * pixel_y.B))));
            return new Color(valR/255.0f, valG / 255.0f, valB / 255.0f, 1f);
        }
        public static Vector2 GetVector2(XmlElement node, Vector2 defaultValue)
        {
            if (node == null)
            {
                return defaultValue;
            }
            var inputString = node.InnerText;
            var coordinates = inputString.Replace("\"", string.Empty)
                .Replace("<", string.Empty).Replace(">", string.Empty).Split(',').ToList();
            return new Vector2(ToFloat(coordinates[0]), ToFloat(coordinates[1]));
        }

        public static Vector3 GetVector3(XmlElement node, Vector3 defaultValue)
        {
            if (node == null)
            {
                return defaultValue;
            }
            var inputString = node.InnerText;
            var coordinates = inputString.Replace("\"",string.Empty)
                .Replace("<", string.Empty).Replace(">", string.Empty).Split(',').ToList();
            return new Vector3(ToFloat(coordinates[0]), ToFloat(coordinates[1]),
                ToFloat(coordinates[2]));
        }
        public static Vector4 GetVector4(XmlElement node, Vector4 defaultValue)
        {
            if (node == null)
            {
                return defaultValue;
            }
            var inputString = node.InnerText;
            var coordinates = inputString.Split(',').ToList();
            return new Vector4(ToFloat(coordinates[0]), ToFloat(coordinates[1]),
                ToFloat(coordinates[2]), ToFloat(coordinates[3]));
        }

        public static float ToFloat(string value)
        {
            float d;
            float.TryParse(value, out d);

            return d;
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
            {
                return defaultValue;
            }

            return node.InnerText;
        }

        /// <summary>
        /// Applies a Sobel filter to a grayscale image to get the bump map...
        /// </summary>
        /// <param name="source"></param>
        /// <param name="power"></param>
        /// <param name="GraphicsDevice"></param>
        /// <param name="GraphicsContext"></param>
        /// <returns></returns>
        public static Texture CalculateBumpMap(Texture source, float power,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
        {
            power = Utility.BoundValue01(power, 0.001f, 10.0f);
            Color[] colors = new Color[source.Width * source.Height];
            Color[]? sourceColors = source.GetColorData(GraphicsContext);
            if (sourceColors == null) return new Texture();
            int x, y;
            for (x = 1; x < source.Width-1; x++)
            {
                for (y = 1; y < source.Height-1; y++)
                {
                    Color val = GetSobelFilter(x,y,sourceColors,source.Width,source.Height,power);
                    colors[x + y * source.Width] = val;// new Color(val,val,val,255);
                }
            }
            return colors.ToTexture(source.Width, source.Height, GraphicsDevice, GraphicsContext.CommandList);
        }


        public static double getPixel(Color[] sourceColors,int Width,int Height,
            bool wrap, int x, int y)
        {
            if (x < 0) x = wrap ? (x + Width) : 0;
            if (y < 0) y = wrap ? (y + Height) : 0;
            if (x >= Width) x = wrap ? (x - Width) : (Width - 1);
            if (y >= Height) y = wrap ? (y - Height) : (Height - 1);
            int idx = x + y * Width;
            return (sourceColors[idx].R + sourceColors[idx].G +
                sourceColors[idx].B) / (256.0 * 3.0);
        }
        
        public static Texture CalculateNormalMap(Texture source, float extrusion,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext,
            bool wrap=false)
        {
            extrusion = Utility.BoundValue01(extrusion, 0.001f, 10.0f);
            Color[] colors = new Color[source.Width * source.Height];
            Color[]? sourceColors = source.GetColorData(GraphicsContext);
            if (sourceColors == null) return new Texture();
            int x, y;
            for (y = 0; y < source.Height; y++)
            {
                for (x = 0; x < source.Width; x++)
                {
                    double center = getPixel(sourceColors, source.Width, source.Height, wrap, x, y);
                    double up = getPixel(sourceColors, source.Width, source.Height, wrap, x, y - 1);
                    double down = getPixel(sourceColors, source.Width, source.Height, wrap, x, y + 1);
                    double left = getPixel(sourceColors, source.Width, source.Height, wrap, x - 1, y);
                    double right = getPixel(sourceColors, source.Width, source.Height, wrap, x + 1, y);
                    double upleft = getPixel(sourceColors, source.Width, source.Height, wrap, x - 1, y - 1);
                    double upright = getPixel(sourceColors, source.Width, source.Height, wrap, x + 1, y - 1);
                    double downleft = getPixel(sourceColors, source.Width, source.Height, wrap, x - 1, y + 1);
                    double downright = getPixel(sourceColors, source.Width, source.Height, wrap, x + 1, y + 1);

                    double vert = (down - up) * 2.0 + downright + downleft - upright - upleft;
                    double horiz = (right - left) * 2.0 + upright + downright - upleft - downleft;
                    double depth = 1.0 / extrusion;
                    double scale = 127.0 / Math.Sqrt(vert * vert + horiz * horiz + depth * depth);

                    byte r = (byte)(128 - horiz * scale);
                    byte g = (byte)(128 + vert * scale);
                    byte b = (byte)(128 + depth * scale);
                    Color val=new Color(r, g, b,255);
                    colors[x + y * source.Width] = val;// new Color(val,val,val,255);
                }
            }
            return colors.ToTexture(source.Width, source.Height, GraphicsDevice, GraphicsContext.CommandList);
        }

        private static bool IsValidPixel(int row, int col, int rows, int cols)
        {
            return row >= 0 && row < rows && col >= 0 && col < cols;
        }

        private static Color GetAvgPixel(Color[] sourceColors, int Width, int Height,
            int x, int y)
        {
            Color sum = Color.Zero;
            int count = 0;
            // Calculate the sum of the surrounding pixels.
            for (int i = Height - 1; i <= Height + 1; i++)
            {
                for (int j = Width - 1; j <= Width + 1; j++)
                {
                    if (IsValidPixel(i, j, Width, Height))
                    {
                        sum += sourceColors[j+i* Width];
                        count++;
                    }
                }
            }
            return new Color(sum.R/count, sum.G / count, sum.B / count, sum.A / count);
        }

        public static Texture CalculateAveragedPixels(Texture source, float power,
    GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext,
    bool wrap = false)
        {
            power = Utility.BoundValue01(power, 0.001f, 10.0f);
            Color[] colors = new Color[source.Width * source.Height];
            Color[]? sourceColors = source.GetColorData(GraphicsContext);
            if (sourceColors == null) return new Texture();
            int x, y;
            for (y = 0; y < source.Height; y++)
            {
                for (x = 0; x < source.Width; x++)
                {
                    colors[x + y * source.Width] = GetAvgPixel(sourceColors, source.Width,
                        source.Height, x, y);
                }
            }
            return colors.ToTexture(source.Width, source.Height, GraphicsDevice, GraphicsContext.CommandList);
        }

        public static string FindAssetSourceDir(string AssetName)
        {
            string? startupPath = Directory.GetParent(Assembly.
                GetExecutingAssembly().Location)?.Parent?.Parent?.Parent?.
                FullName;
            DirectoryInfo dir = new DirectoryInfo(
                startupPath + "\\TerrainEditor\\Assets");
            //Content.FileProvider.ListFiles(dir, name, VirtualSearchOption.AllDirectories);
            FileInfo[] Files = dir.GetFiles(AssetName, SearchOption.AllDirectories);
            if (Files.Length == 0)
            {
                return "";
                //throw new Exception(name+" does not exist within the assets folder...");
            }
            string filename = Files[0].FullName;
            //open the *.sdtex file and read the Source
            var lines = File.ReadAllLines(filename);
            string outfilename = "";
            for (var i = 0; i < lines.Length; i += 1)
            {
                string line = lines[i];
                // Process line
                if (line.IndexOf("Source: !file") >= 0)
                {
                    outfilename = line.Substring(13);
                    break;
                }
            }
            return outfilename;
        }

        public static Bitmap CopyDataToBitmap(byte[] data,int width,int height)
        {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(width, height,
                //System.Drawing. PixelFormat.Format24bppRgb
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //Create a BitmapData and Lock all pixels to be written 
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            //Unlock the pixels
            bmp.UnlockBits(bmpData);

            //Return the bitmap 
            return bmp;
        }

        public static Texture? LoadTex(string filename_in_resources_dir,
          GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext,
          //PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm,
          bool useResourcesDir=true//, bool fixformat = false
            )
        {
            Texture texture;
            try
            {
                string filename = "";
                if (useResourcesDir)
                    filename = Resources_Directory + filename_in_resources_dir;
                else
                    filename = filename_in_resources_dir;

                /* TextureTool texTool = new TextureTool();
                   TexImage texim = texTool.Load(filename,true);
                   texTool.Convert(texim, QaLPixelFormat);
                   Stride.Graphics.Image strideim=texTool.ConvertToStrideImage(texim);
                   strideim.ConvertFormatToSRgb();
                   texture=Texture.New(GraphicsDevice, strideim);
               texture = texture.ReFormat(GraphicsContext);
               return texture;*/
                using (var inStream = System.IO.File.OpenRead(filename))
                    texture = Texture.Load(GraphicsDevice, inStream);//, loadAsSRGB: true);

               // texture = texture.Resize(texture.Width, texture.Height, GraphicsContext);
                //texture = texture.ReFormat(GraphicsContext);
                return texture;
                /*   
                           Bitmap im = (Bitmap)Image.FromFile(filename);
                           texture = new Texture();
                           Color [] colors = new Color[im.Width* im.Height];
                           for (int i = 0; i < im.Width; i++)
                           {
                               for (int j = 0; j < im.Height; j++)
                               {
                                   System.Drawing.Color pixel = im.GetPixel(i, j);
                                   colors[i + j * im.Width].R = pixel.R;
                                   colors[i + j * im.Width].G = pixel.G;
                                   colors[i + j * im.Width].B = pixel.B;
                                   colors[i + j * im.Width].A = pixel.A;
                               }
                           }
                           texture = Texture.New2D<Color>(GraphicsDevice, im.Width, im.Height,
                               Utility.QaLPixelFormat, colors,TextureFlags.ShaderResource);
                           //texture.SetData(GraphicsContext.CommandList, colors);
                           //  using (var inStream = System.IO.File.OpenRead(filename))
                           //    texture = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: true);
                           im.Dispose();*/
                // if (!texture.CheckFormat(PixelFormat.R8G8B8A8_UNorm))
                //   texture.Resize(texture.Width, texture.Height, GraphicsContext);
                //  return texture;
            }
            catch
            {
                return null;
            }
        }
        public static Texture? FlatTex(int width,int height,Color col,
          GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
        {
            Texture texture;
            try
            {
                texture = PerlinNoise.MakeFlat(width, height, col).ToTexture(
                        width, height, GraphicsDevice, GraphicsContext.CommandList);
                return texture;
            }
            catch
            {
                return null;
            }
        }
        public static Material GetBlackMaterial(GraphicsDevice GraphicsDevice)
        {
            var materialDescriptor = new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    CullMode = CullMode.Back,
                    Diffuse = new MaterialDiffuseMapFeature(
                 new ComputeColor(Color4.Black)

                ),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            };
            var material = Material.New(GraphicsDevice, materialDescriptor);
            return material;
        }
        public static Texture? TreeLocationsTexture(List<Int2> locs,int width, int height, 
            GraphicsDevice GraphicsDevice,
            CommandList CommandList, int MaxTreeTypes=3, int size = 100)
        {
            Texture texture;
            try
            {
                int i, j,k,count=0, index;
                Color[] Colors = new Color[width * height];
                for (i = 0; i < width; i++)
                {
                    for (j = 0; j < height; j++)
                    {
                        index = (width * j) + i;
                        byte col = 0;
                        for (k = 0; k < locs.Count; k++)
                        {
                            if (locs[k].X == i && locs[k].Y == j
                                && count<size)
                            {
                                col = (byte)Utility.DUnif(1, MaxTreeTypes);                               
                                count++;
                                break;
                            }
                        }
                        Colors[index] = new Color(col, 0, 0, 255);
                    }
                }
                texture = Colors.ToTexture(
                        width, height, GraphicsDevice, CommandList);
                return texture;
            }
            catch
            {
                return null;
            }
        }
        public static List<Int2> GetTreeLocations(int m_Width,
            int m_Height, int size=100,float repulsion_distance=10.0f)
        {
            int l,count=0;
            if (size > m_Width * m_Height) size = m_Width * m_Height;
            List<Int2> Points = new List<Int2>();
            Vector2 Point = new Vector2(Utility.DUnif(0, m_Width - 1),
                     Utility.DUnif(0, m_Height - 1));
            Points.Add(new Int2((int)Point.X, (int)Point.Y));
            count++;
            bool valid = false;
            while (count < 1000000 && Points.Count < size)
            {
                Point = new Vector2(Utility.DUnif(0, m_Width - 1),
                     Utility.DUnif(0, m_Height - 1));
                valid = true;
                for (l = 0; l < Points.Count; l++)
                {
                    if (Vector2.Distance(Point, new Vector2(Points[l].X,
                     Points[l].Y)) <= repulsion_distance)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                    Points.Add(new Int2((int)Point.X, (int)Point.Y));
                count++;
            }
            return Points;
        }

        public static List<Int2> GetTreeLocationsinBox(int centerx,
            int centery,int radius, int size = 100, float repulsion_distance = 10.0f)
        {
            int l, count = 0;
            int numpts = 4 * radius * radius;
            if (size > numpts) size = numpts;
            List<Int2> Points = new List<Int2>();
            Vector2 Point = new Vector2(Utility.DUnif(centerx-radius, centerx + radius),
                     Utility.DUnif(centery - radius, centery + radius));
            Points.Add(new Int2((int)Point.X, (int)Point.Y));
            count++;
            bool valid = false;
            while (count < 1000000 && Points.Count < size)
            {
                Point = new Vector2(Utility.DUnif(centerx - radius, centerx + radius),
                     Utility.DUnif(centery - radius, centery + radius));
                valid = true;
                for (l = 0; l < Points.Count; l++)
                {
                    if (Vector2.Distance(Point, new Vector2(Points[l].X,
                     Points[l].Y)) <= repulsion_distance)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                    Points.Add(new Int2((int)Point.X, (int)Point.Y));
                count++;
            }
            return Points;
        }

        public static Vector3 GetCameraPosition(CameraComponent camera)
        {
            var viewMatrix = camera.ViewMatrix;
            viewMatrix.Invert();

            Vector3 cameraPosition = viewMatrix.TranslationVector.AsNumericVec3();

            return cameraPosition;
        }

        public static Stride.Core.Mathematics.Vector3 AsStrideVec3(this
           System.Numerics.Vector3 vec)
        {
            return new Stride.Core.Mathematics.Vector3(vec.X, vec.Y, vec.Z);
        }
        public static System.Numerics.Vector3 AsNumericVec3(this
            Stride.Core.Mathematics.Vector3 vec)
        {
            return new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        }
        public static Stride.Core.Mathematics.Vector4 AsStrideVec4(this
          System.Numerics.Vector4 vec)
        {
            return new Stride.Core.Mathematics.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }
        public static System.Numerics.Vector4 AsNumericVec4(this
            Stride.Core.Mathematics.Vector4 vec)
        {
            return new System.Numerics.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }
        public static System.Numerics.Vector4 Color2Vec4(Stride.Core.Mathematics.Color col)
        {
            return new System.Numerics.Vector4(col.R / 255.0f, col.G / 255.0f, col.B / 255.0f,
                col.A / 255.0f);
        }
        public static System.Numerics.Vector4 Color2Vec4(System.Windows.Media.Color col)
        {
            return new System.Numerics.Vector4(col.R / 255.0f, col.G / 255.0f, col.B / 255.0f,
                col.A / 255.0f);
        }
        public static Stride.Core.Mathematics.Color Vec4Color(
            System.Numerics.Vector4 vec)
        {
            return new Stride.Core.Mathematics.Color(
                new Stride.Core.Mathematics.Vector4(vec.X, vec.Y, vec.Z, vec.W));
        }
        public static Stride.Core.Mathematics.Color AsStrideColor(
            this System.Numerics.Vector3 vec)
        {
            return new Stride.Core.Mathematics.Color(
                new Stride.Core.Mathematics.Vector3(vec.X, vec.Y, vec.Z));
        }
        public static System.Windows.Media.Color Vec4ColorNumerics(
     System.Numerics.Vector4 vec)
        {
            return System.Windows.Media.Color.FromArgb((byte)vec.X, (byte)vec.Y,
                (byte)vec.Z, (byte)vec.W);
        }

        public static bool IntersectsTriangle(Vector3 rayOrigin, Vector3 rayDirection, Vector3 V0, Vector3 V1, Vector3 V2)
        {
            // Compute the offset origin, edges, and normal.
            Vector3 diff = rayOrigin - V0;
            Vector3 edge1 = V1 - V0;
            Vector3 edge2 = V2 - V0;
            Vector3 normal = Vector3.Cross(edge1, edge2);

            // Solve Q + t*D = b1*E1 + b2*E2 (Q = kDiff, D = ray direction,
            // E1 = kEdge1, E2 = kEdge2, N = Cross(E1,E2)) by
            //   |Dot(D,N)|*b1 = sign(Dot(D,N))*Dot(D,Cross(Q,E2))
            //   |Dot(D,N)|*b2 = sign(Dot(D,N))*Dot(D,Cross(E1,Q))
            //   |Dot(D,N)|*t = -sign(Dot(D,N))*Dot(Q,N)
            double DdN = Vector3.Dot(rayDirection, normal);
            double sign;
            if (DdN > MathUtil.ZeroTolerance)
            {
                sign = 1;
            }
            else if (DdN < -MathUtil.ZeroTolerance)
            {
                sign = -1;
                DdN = -DdN;
            }
            else
            {
                // Ray and triangle are parallel, call it a "no intersection"
                // even if the ray does intersect.
                return false;
            }

            double DdQxE2 = sign * //rayDirection.Dot(diff.Cross(edge2));
            Vector3.Dot(rayDirection, Vector3.Cross(diff, edge2));
            if (DdQxE2 >= 0)
            {
                double DdE1xQ = sign * //rayDirection.Dot(edge1.Cross(diff));
                Vector3.Dot(rayDirection, Vector3.Cross(edge1, diff));
                if (DdE1xQ >= 0)
                {
                    if (DdQxE2 + DdE1xQ <= DdN)
                    {
                        // Line intersects triangle, check if ray does.
                        double QdN = -sign * //diff.Dot(normal);
                        Vector3.Dot(diff, normal);
                        if (QdN >= 0)
                        {
                            // Ray intersects triangle.
                            return true;
                        }
                        // else: t < 0, no intersection
                    }
                    // else: b1+b2 > 1, no intersection
                }
                // else: b2 < 0, no intersection
            }
            // else: b1 < 0, no intersection
            return false;

        }

        public static BoundingBox FromPoints(Stride.Core.Mathematics.Vector3[] verts)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < verts.Length; ++i)
            {
                Min(ref min, ref verts[i], out min);
                Max(ref max, ref verts[i], out max);
            }
            return new BoundingBox(new Stride.Core.Mathematics.Vector3(min.X, min.Y, min.Z),
                new Stride.Core.Mathematics.Vector3(max.X, max.Y, max.Z));
        }
        public static void Max(ref Vector3 left, ref Stride.Core.Mathematics.Vector3 right, out Vector3 result)
        {
            result.X = (left.X > right.X) ? left.X : right.X;
            result.Y = (left.Y > right.Y) ? left.Y : right.Y;
            result.Z = (left.Z > right.Z) ? left.Z : right.Z;
        }
        public static void Min(ref Vector3 left, ref Stride.Core.Mathematics.Vector3 right, out Vector3 result)
        {
            result.X = (left.X < right.X) ? left.X : right.X;
            result.Y = (left.Y < right.Y) ? left.Y : right.Y;
            result.Z = (left.Z < right.Z) ? left.Z : right.Z;
        }

        public static float RandomFloat(float MinValue = 0.0f, float MaxValue = 1.0f)
        {
            return (float)(new Random().NextDouble()) * (MaxValue - MinValue) + MinValue;
        }
        /// <summary>
        /// Random integer from minvalue to maxvalue
        /// </summary>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static int DUnif(int MinValue = 0, int MaxValue = 1)
        {
            return new Random().Next(MinValue, MaxValue+1);
        }

        public static float BoundValue01(float val, float mincutoff=0.0f, float maxcutoff=1.0f)
        {
            float newval = val;
            if (val < mincutoff) newval = mincutoff;
            if (val > maxcutoff) newval = maxcutoff;
            return newval;
        }
        public static int RList(List<int> ints)
        {
            if(ints==null || ints.Count == 0) return 0;
            return ints[DUnif(0,ints.Count-1)];
        }
        public static float Runif(float a = 0.0f, float b = 1.0f)
        {
            if (a > b) return a;
            return (float)(a + (b - a) * 1.0f * Utility.RandomFloat());
        }

        public static float RGamma(float a,float b)
        {
            return (float)Gamma.Sample(a, b);
        }
        public static float[] RDirichlet(float []weights)
        {
            int dim=weights.Length;
            if(weights.Any(w => w < 0.0f))
            {
                weights = new float[dim];
                for(int i=0; i<dim; i++)
                    weights[i] = Runif(1,10);
            }
            double[] wts = new double[dim];
            for (int i = 0; i < dim; i++)
                wts[i] = weights[i];
            System.Random rng= new System.Random();
            double [] samp=Dirichlet.Sample(rng, wts);
            float[]ret= new float[dim];
            for (int i = 0; i < dim; i++)
                ret[i] = (float)samp[i];
            return ret;
        }

        public static float NormalError(float mean=0.0f,float var = 1.0f)
        {
            if (var <= 0.0f) return 0.0f;
            float gen= mean+MathF.Sqrt(-2*MathF.Log(Runif()))*
                MathF.Cos(2*3.14f* Runif())* MathF.Sqrt(var);
            return gen;// Utility.RandomFloat(-3.0f * std, 3.0f * std);
        }
        public static System.Drawing.Color AsDrawingColor(this float val)
        {
            FloatRGBAConverter converter = new FloatRGBAConverter(val);
            return System.Drawing.Color.FromArgb(converter.A, converter.R,
                 converter.G, converter.B);
        }
        public static Stride.Core.Mathematics.Color AsStrideColor(this short val)
        {
            FloatRGBAConverter converter = new FloatRGBAConverter((float)val);
            return new Stride.Core.Mathematics.Color(converter.R, converter.G,
                 converter.B, converter.A);
        }
        public static Stride.Core.Mathematics.Color AsStrideColor(this float val)
        {
            FloatRGBAConverter converter = new FloatRGBAConverter((float)val);
            return new Stride.Core.Mathematics.Color(converter.R, converter.G,
                 converter.B, converter.A);
        }

        public static void FloatToRGBA(float val, out byte R, out byte G, out byte B, out byte A)
        {
            FloatRGBAConverter converter = new FloatRGBAConverter(val);
            R = converter.R; G = converter.G; B = converter.B; A = converter.A;
            //byte[] bytes = BitConverter.GetBytes(val);
            //R = bytes[0];            G = bytes[1];            B = bytes[2];            A = bytes[3];
        }

        /// <summary>
        /// use R and G to create a short, used as height value
        /// </summary>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static short RGBAToShort(Stride.Core.Mathematics.Color Color, float minValue = -1000000000.0f, float maxValue = 1000000000.0f)
        {
            byte[] bytes = new byte[] { 0, 0, Color.R, Color.G };
            FloatRGBAConverter converter = new FloatRGBAConverter(0, 0, Color.R, Color.G);

            float value = converter.Float;// BitConverter.ToSingle(bytes, 0);
            return HeightmapUtils.ConvertToShortHeight(minValue, maxValue, value);
        }
        public static short ColorToShort(Stride.Core.Mathematics.Color Color, float minValue = -1000000000.0f, float maxValue = 1000000000.0f)
        {
            byte[] bytes = new byte[] { Color.R, Color.G, Color.B, Color.A };
            FloatRGBAConverter converter = new FloatRGBAConverter(
                Color.R, Color.G, Color.B, Color.A);

            float value = converter.Float;// BitConverter.ToSingle(bytes, 0);
            return HeightmapUtils.ConvertToShortHeight(minValue, maxValue, value);
        }
        public static short ToShort(this float x)
        {
            if (x < short.MinValue)
            {
                return short.MinValue;
            }
            if (x > short.MaxValue)
            {
                return short.MaxValue;
            }
            return (short)MathF.Round(x);
        }
        public static byte ToByte(this float x)
        {
            if (x < byte.MinValue)
            {
                return byte.MinValue;
            }
            if (x > byte.MaxValue)
            {
                return byte.MaxValue;
            }
            return (byte)MathF.Round(x);
        }
        public static float ToFloat(this Stride.Core.Mathematics.Color Color)
        {
            byte[] bytes = new byte[] { Color.R, Color.G, Color.B, Color.A };
            FloatRGBAConverter converter = new FloatRGBAConverter(Color.R,
                Color.G, Color.B, Color.A);
            return converter.Float;
        }
        public static float RGBAToFloat(byte R, byte G, byte B, byte A)
        {
            Span<byte> rgbaBytes = stackalloc byte[] { R, G, B, A };
            return BitConverter.ToSingle(rgbaBytes);
            //byte[] bytes = new byte[] { R, G, B, A };
            //return BitConverter.ToSingle(bytes, 0);
        }

        public static byte GetRedFromFloat(float val)
        {
            FloatToRGBA(val, out byte R, out byte G, out byte B, out byte A);
            return R;
        }
        public static byte GetGreenFromFloat(float val)
        {
            FloatToRGBA(val, out byte R, out byte G, out byte B, out byte A);
            return G;
        }
        public static byte GetBlueFromFloat(float val)
        {
            FloatToRGBA(val, out byte R, out byte G, out byte B, out byte A);
            return B;
        }
        public static byte GetAlphaFromFloat(float val)
        {
            FloatToRGBA(val, out byte R, out byte G, out byte B, out byte A);
            return A;
        }

        public static float BoundValue0255(float val, float mincutoff = 0.0f, float maxcutoff = 255.0f)
        {
            float newval = val;
            if (newval < mincutoff) newval = mincutoff;
            if (newval > maxcutoff) newval = maxcutoff;
            return newval;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FloatRGBAConverter
        {
            [FieldOffset(0)]
            public float Float;

            [FieldOffset(0)]
            public byte R;

            [FieldOffset(1)]
            public byte G;

            [FieldOffset(2)]
            public byte B;

            [FieldOffset(3)]
            public byte A;

            public FloatRGBAConverter(float @float) : this()
            {
                Float = @float;
            }

            public FloatRGBAConverter(byte r, byte g, byte b, byte a) : this()
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
            /*//float to RGBA
Span<byte> floatBytes = stackalloc byte[4];
BitConverter.TryWriteBytes(floatBytes, value);
R = floatBytes[0];
G = floatBytes[1];
B = floatBytes[2];
A = floatBytes[3];

//RGBA to float
Span<byte> rgbaBytes = stackalloc byte[] { r, g, b, a };
return BitConverter.ToSingle(rgbaBytes);*/
        }


        public static Vector3 LERPColor(Vector3 low, Vector3 high, float w)
        {
            return (1 - w) * low + w * high;
        }

        /*
           Return a RGB colour value given a scalar v in the range [vmin,vmax]
           In this case each colour component ranges from 0 (no contribution) to
           1 (fully saturated), modifications for other ranges is trivial.
           The colour is clipped at the end of the scales if v is outside
           the range [vmin,vmax]
           low is blue-high is White
        */
        public static Vector3 GetColourFromVec1toVec2(float v, float vmin, float vmax,
                                         Vector3 mincol, Vector3 maxcol)
        {
            if (v <= vmin)
                return mincol;
            if (v >= vmax)
                return maxcol;
            return LERPColor(mincol, maxcol, v);
        }

        public static float Dist2d(Vector2 v1, Vector2 v2)
        {
            return MathF.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));
        }
        public struct VoronoiVertex
        {
            public int x;
            public int y;
            public byte R;
            public byte G;
            public byte B;
            public byte A;
            //bool IsEdgePt;
        };

        public class ConvexHull
        {
            public static bool IsInHull(Point testPoint, List<Point> Hull)
            {
                //n>2 Keep track of cross product sign changes
                int pos = 0, neg = 0;

                for (int i = 0; i < (int)Hull.Count; i++)
                {
                    //If point is in the polygon
                    if (Hull[i] == testPoint) return true;

                    //Form a segment between the i'th point
                    float x1 = Hull[i].X;
                    float y1 = Hull[i].Y;

                    //And the i+1'th, or if i is the last, with the first point
                    int i2 = i < (int)Hull.Count - 1 ? i + 1 : 0;

                    float x2 = Hull[i2].X;
                    float y2 = Hull[i2].Y;

                    float x = testPoint.X;
                    float y = testPoint.Y;

                    //Compute the cross product
                    float d = (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);

                    if (d > 0) pos++;
                    if (d < 0) neg++;

                    //If the sign changes, then point is outside
                    if (pos > 0 && neg > 0)
                        return false;
                }

                return true;
            }

            // 3D cross product of OA and OB vectors, (i.e z-component of their "2D" cross product, but remember that it is not defined in "2D").
            // Returns a positive value, if OAB makes a counter-clockwise turn,
            // negative for clockwise turn, and zero if the points are collinear.
            public static double cross(Point O, Point A, Point B)
            {
                return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
            }
            public static List<Point> GetConvexHull(List<Point> points)
            {
                if (points == null)
                    return new List<Point>();

                if (points.Count() <= 1)
                    return points;

                int n = points.Count(), k = 0;
                List<Point> H = new List<Point>(new Point[2 * n]);

                points.Sort((a, b) =>
                     a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

                // Build lower hull
                for (int i = 0; i < n; ++i)
                {
                    while (k >= 2 && cross(H[k - 2], H[k - 1], points[i]) <= 0)
                        k--;
                    H[k++] = points[i];
                }

                // Build upper hull
                for (int i = n - 2, t = k + 1; i >= 0; i--)
                {
                    while (k >= t && cross(H[k - 2], H[k - 1], points[i]) <= 0)
                        k--;
                    H[k++] = points[i];
                }
                return H.Take(k - 1).ToList();
            }
        }

        public static float GetYMixture(float x, float z, float centx, float centz, float stdx, float stdz,
            float stdx1, float stdz1, float muxminus, float muzminus, float muxplus,
            float muzplus, float prob1, float prob2, float prob3, float prob4, float prob5,
            float minbound, float maxbound)
        {
            return prob1 * GetYBellShape(x, z, centx, centz,
                centx , centz , minbound, maxbound)
                //+ Runif(-0.02f, 0.02f)
                + prob2 * GetYBellShape(x, z, centx + muxminus, centz + muzminus,
                    stdx1, stdz, minbound, maxbound) +
                prob3 * GetYBellShape(x, z, centx + muxminus, centz + muzplus,
                    stdx, stdz1, minbound, maxbound) +
                prob4 * GetYBellShape(x, z, centx + muxplus, centz + muzminus,
                    stdx1, stdz1, minbound, maxbound) +
                prob5 * GetYBellShape(x, z, centx + muxplus, centz + muzplus,
                    stdx, stdz, minbound, maxbound);
        }

        public static float GetNormalMixture(float x, float z,
            float[] meansx, float[] meansy, float[]stdsx, float[] stdsy, 
            float []probs, float minbound, float maxbound)
        {
            int len=meansx.Length;
            float ret = 0;
            for (int i = 0; i < len; i++) 
            {
                ret += GetYBellShape(x, z, meansx[i], meansy[i],
                stdsx[i], stdsy[i], minbound, maxbound);
            }
            return ret;
        }

        public static float GetYBellShape(float x, float z, float meanx, float meanz, float stdx,
            float stdz, float minbound, float maxbound)
        {
            return MathF.Exp(-0.5f * (x - meanx) * (x - meanx) / (stdx * stdx)
                - 0.5f * (z - meanz) * (z - meanz) / (stdz * stdz))
                //sqrtf(2.0f * (float)D3DX_PI * stdx * stdx * stdz * stdz) 
                * (maxbound - minbound) + minbound;
        }

        public static float GetYFromCurve(int type_curve, int type_surface, float x, float z,
            float shiftx, float shiftz, float param1, float param2, float minbound, float maxbound,
            float theta1, float theta2, float prob, float dropfactorx, float dropfactorz, float pow)
        {
            float theta, ret, xx, zz,
                curvex, curvez, curvenearx = 0, curvenearz = 0,
                dist, mindist = 10000000.0f;
            int n = 100;//moves from theta1 to theta2
                        //surface is z=f(x,y)
            for (int k = 0; k <= n; k++)
            {
                //arc-circle curve
                //		float tx = 2.0f * (float)D3DX_PI * x / pow;
                //		float tz = -1.0f + 2.0f * z / pow;//(z<16)?sin(D3DX_PI*z/32.0f):-sin(D3DX_PI*z/32.0f);
                theta = theta1 + 1.0f * k * (theta2 - theta1) / n;
                if (type_curve == 0)
                {
                    curvex = param1 * MathF.Cos(theta);
                    curvez = param2 * MathF.Sin(theta);//+runif(-param2,param2);//(sin(theta)+1)/2.0f;
                }
                else
                //cosine curve
                if (type_curve == 1)
                {
                    curvex = theta;
                    curvez = param2 * MathF.Cos(param1 * theta);
                }
                else
                //sine curve
                if (type_curve == 2)
                {
                    curvex = theta;
                    curvez = param2 * MathF.Sin(param1 * theta);
                }
                else if (type_curve == 3)//Astroid
                {
                    curvex = param1 * MathF.Pow(MathF.Cos(theta), 3.0f);
                    curvez = param1 * MathF.Pow(MathF.Sin(theta), 3.0f);
                }
                else if (type_curve == 4)//Folium of Descartes
                {
                    curvex = 3 * param1 * theta / (1 + MathF.Pow(theta, 3.0f));
                    curvez = 3 * param1 * theta * theta / (1 + MathF.Pow(theta, 3.0f));
                }
                else if (type_curve == 5)//Involute of a Circle
                {
                    curvex = param1 * (MathF.Cos(theta) + theta * MathF.Sin(theta));
                    curvez = param1 * (MathF.Sin(theta) - theta * MathF.Cos(theta));
                }
                else if (type_curve == 6)//	Nephroid Curves
                {
                    curvex = param1 * (MathF.Cos(param2 * theta) - MathF.Cos(param2 * theta));
                    curvez = param1 * (MathF.Sin(param2 * theta) - MathF.Sin(param2 * theta));
                }
                else if (type_curve == 7)//	Witch of Agnesi Curves
                {
                    curvex = param1 * theta;
                    curvez = param1 / (1.0f + theta);
                }
                else// if (type_curve == 8)//	snake curve
                {
                    curvex = param1 * theta;
                    curvez = param1 / (1.0f + theta);
                }

                xx = curvex + shiftx;
                zz = curvez + shiftz;
                //find the closest point (xx,zz) from this curve to the given point (x,z)
                dist = MathF.Sqrt((x - xx) * (x - xx) + (z - zz) * (z - zz));
                if (dist < mindist)
                {
                    mindist = dist;
                    curvenearx = xx;
                    curvenearz = zz;
                }
            }
            if (type_surface == 0)
                ret = pow * GetYBellShape(x, z, curvenearx, curvenearz,
                    dropfactorx, dropfactorz, minbound, maxbound);
            else if (type_surface == 1)
            {
                ret = (pow * MathF.Exp(-MathF.Abs(x - curvenearx) / dropfactorx
                    - MathF.Abs(z - curvenearz) / dropfactorz)
                    //(4* curvenearx* curvenearz) 
                    ) * (maxbound - minbound) + minbound;
            }
            else
            {
                ret = pow * (prob * MathF.Exp(-MathF.Abs(x - curvenearx) / dropfactorx
                    - MathF.Abs(z - curvenearz) / dropfactorz) / (4 * curvenearx * curvenearz)
                     + (1.0f - prob) * GetYBellShape(x, z, curvenearx, curvenearz,
                         dropfactorx, dropfactorz, minbound, maxbound)
                    / MathF.Sqrt(2.0f * (float)MathF.PI * dropfactorx * dropfactorx * dropfactorz * dropfactorz)
                    )
                     * (maxbound - minbound) + minbound;
            }

            return ret;
        }

        public static short[] GetShortsFromTexture(Texture texture, CommandList commandList)
        {
            short[] ret = new short[texture.Width * texture.Height];
            //        Stride.Graphics.Image im = texture.GetDataAsImage(commandList);
            //       PixelBuffer buff = im.GetPixelBuffer(0, 0);
            ret = texture.GetData<short>(commandList);

            int i, j, index;
            //short[] data = texture.GetData<short>(commandList);
            for (i = 0; i < texture.Width; i++)
            {
                for (j = 0; j < texture.Height; j++)
                {
                    index = (texture.Width * j) + i;
                    // float height = MathUtil.Clamp(
                    //      HeightmapUtils.ConvertToFloatHeight(short.MinValue, short.MaxValue,
                    //                  data[index]), short.MinValue, short.MaxValue);
                    //float height
                    ret[index] = (short)MathF.Round(//buff.GetPixel<short>(i, j) *
                                                   ret[index] * short.MaxValue,
                        MidpointRounding.AwayFromZero);

                    // HeightmapUtils.ConvertToShortHeight(short.MinValue, short.MaxValue,buff.GetPixel<short>(i, j));
                    //data[index], short.MinValue, short.MaxValue);
                    //     buff.GetPixel<float>(i, j), short.MinValue, short.MaxValue);//);
                    // MathUtil.Clamp(buff.GetPixel<short>(i, j), short.MinValue, short.MaxValue);
                    //height *= Heightmap.HeightRange.Y;
                    //      Heightmap.Floats[index] = height;// * Heightmap.HeightScale;
                    //      Heightmap.Bytes[index] = (byte)Heightmap.Floats[index];
                    //      Heightmap.Shorts[index] = (short)Heightmap.Floats[index];
                    //ret[index] = buff.GetPixel<short>(i, j);// (short)MathUtil.Clamp(data[index], short.MinValue,  short.MaxValue);
                    // heightmap.Shorts.Select((h) => (byte)MathUtil.Clamp(MathUtil.Lerp(byte.MinValue, byte.MaxValue, MathUtil.InverseLerp(min, max, h)), byte.MinValue, byte.MaxValue)).ToArray());

                }
            }
            return ret;
        }
    }

}

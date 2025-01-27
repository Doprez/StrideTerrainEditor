//by Idomeneas. Credit given to the authors of other methods otherwise.

using Stride.Core.Mathematics;
using Stride.Physics;
using Stride.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Graphics;
using System.Threading.Tasks;
using TerrainEditor;
using Stride.Engine;
using System.Drawing;
using Color = Stride.Core.Mathematics.Color;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PixelFormat = Stride.Graphics.PixelFormat;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpFont;
using Stride.Core.Serialization;
using Stride.Core;
using Stride.Games;
using Matrix = Stride.Core.Mathematics.Matrix;
using Point = Stride.Core.Mathematics.Point;

using Bitmap = System.Drawing.Bitmap;
using InterpolationMode = System.Drawing.Drawing2D.InterpolationMode;
using System.Reflection;
using Stride.TextureConverter.Requests;
using Stride.Assets.Textures;
using SpriteBatch = Stride.Graphics.SpriteBatch;
using RectangleF = Stride.Core.Mathematics.RectangleF;
using TextAlignment = Stride.Graphics.TextAlignment;
using System.Buffers.Binary;
using System.Numerics;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;
using Vector4 = Stride.Core.Mathematics.Vector4;
using Stride.Rendering.RenderTextures;
using Stride.Core.IO;
using System.Security.Policy;
using System;
using System.Reflection.Metadata;

namespace HeightMapEditor
{
    public static class TextureExtensions
    {
        public static bool IsValidCoordinate(this Texture heightmap, int x, int y)
       => x >= 0 && x < heightmap.Width && y >= 0 && y < heightmap.Height;

        public static float GetHeightAt(this Color[] ColorValues,
            Texture heightmap,int x, int y,Int2 HeightRange)
        {
            if (!heightmap.IsValidCoordinate(x, y))
            {
                return 0;//no contribution for this point
            }
            float ht = 0;
            if (PerlinNoise.IsGrayScaleHeightMap)
                ht = ColorValues.GetColor(heightmap, x, y).R;
            else
                ht = ColorValues.GetColor(heightmap, x, y).ToFloat();
            float height = HeightRange.X +
                  (HeightRange.Y - HeightRange.X)
                  * ht / PerlinNoise.HeightMultiplier;
            return height;
        }

        public static bool IntersectsRay(this Texture heightmap,
        Ray ray, GraphicsContext GraphicsContext, out Vector3 point, float m_QuadSideWidthX = 1.0f,
        float m_QuadSideWidthZ = 1.0f)
        {
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 1.5f);
            int x, z;
            float mindist = 1000000000.0f;
            point = Vector3.Zero;
            bool foundit = false;
            Color[] ColorValues = heightmap.GetColorData(GraphicsContext);
            for (z = 0; z < heightmap.Height; z++)
            {
                for (x = 0; x < heightmap.Width; x++)
                {
                    float ht = PerlinNoise.HeightMin;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                        ht = ColorValues[x + heightmap.Width *
                        z].R;
                    else
                        ht = ColorValues[x+heightmap.Width*
                        z].ToFloat().ToShort();
                    float height = PerlinNoise.HeightMin + 
                        (PerlinNoise.HeightMax - PerlinNoise.HeightMin) * ht 
                        / PerlinNoise.HeightMultiplier;
                    sphere.Center = new Vector3(x * m_QuadSideWidthX, height,
                        z * m_QuadSideWidthZ);
                    if (sphere.Intersects(ref ray, out Vector3 pt))
                    {
                        //get nearest hit
                        float dist = Vector3.Distance(pt, ray.Position);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            point = pt;
                            foundit = true;
                        }
                        //return true;//gets the first hit, replace out Vector3 pt with out point and comment the above
                    }
                }
            }
            return foundit;
        }

        /// <summary>
        /// Rotates all pixels in the image about the center pixel
        /// Could resize the image but lets keep it fixed to the original size
        /// This way we can rotate by 90, 160 and 270 degrees each heightmap and get
        /// 4 versions out of a single map
        /// </summary>
        /// <param name="source"></param>
        /// <param name="angleInDegrees"></param>
        /// <param name="GraphicsDevice"></param>
        /// <param name="GraphicsContext"></param>
        /// <returns></returns>
        public static Texture Rotate(this Texture source, float angleInDegrees,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
        {
            Texture texout = new Texture();
            Color[] sourceColors = source.GetColorData(GraphicsContext);
            Color[] colors = new Color[source.Width * source.Height];
            int i, j, Width = source.Width, Height = source.Height;
            Point pointToRotate=new Point();
            Point centerPoint=new Point(Width/2, Height/2);
            for (i = 0; i < Width; i++)
            {
                for (j = 0; j < Height; j++)
                {
                    pointToRotate.X=i; pointToRotate.Y=j;
                    Point newp=RotatePoint(pointToRotate,
                        centerPoint, angleInDegrees);
                    if (!source.IsValidCoordinate(newp.X, newp.Y)) continue;
                    colors[newp.X+ newp.Y * Width] =
                        sourceColors[i + j * Width];
                }
            }
            return colors.ToTexture(source.Width, source.Height, GraphicsDevice, GraphicsContext.CommandList);
        }

        /// <summary>
        /// Rotates one point around another, by Fraser (internet search). Changes by Idomeneas
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        public static Point RotatePoint(Point pointToRotate,
            Point centerPoint, float angleInDegrees)
        {
            float angleInRadians = angleInDegrees * (MathF.PI / 180);
            float cosTheta = MathF.Cos(angleInRadians);
            float sinTheta = MathF.Sin(angleInRadians);
            return new Point
            {
                X = (int)MathF.Round(cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (int)MathF.Round(sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        /// <summary>
        /// Original by Dewald Esterhuizen, modified by Idomeneas
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Texture ConvolutionFilter<T>(this Texture source, T filter,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
                                     where T : ConvolutionFilterBase
        {
            Texture texout = new Texture();
            Color[] colors = new Color[source.Width * source.Height];
            Color[]? sourceColors = source.GetColorData(GraphicsContext);
            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;
            int filterWidth = filter.FilterMatrix.GetLength(1);
            int filterHeight = filter.FilterMatrix.GetLength(0);
            int filterOffset = (filterWidth - 1) / 2;
            int calcOffset = 0;
            int byteOffset = 0;
            for (int offsetY = filterOffset; offsetY <
                 source.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <
                     source.Width - filterOffset; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;
                    byteOffset = offsetY * source.Width + offsetX;
                    for (int filterY = -filterOffset;
                         filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;
                             filterX <= filterOffset; filterX++)
                        {
                            calcOffset = byteOffset + filterX +
                                         (filterY * source.Width);
                            blue += (double)(sourceColors[calcOffset].B) *
                                     filter.FilterMatrix[filterY + filterOffset,
                                     filterX + filterOffset];
                            green += (double)(sourceColors[calcOffset].G) *
                                      filter.FilterMatrix[filterY + filterOffset,
                                      filterX + filterOffset];
                            red += (double)(sourceColors[calcOffset].R) *
                                    filter.FilterMatrix[filterY + filterOffset,
                                    filterX + filterOffset];
                        }
                    }
                    blue = filter.Factor * blue + filter.Bias;
                    green = filter.Factor * green + filter.Bias;
                    red = filter.Factor * red + filter.Bias;
                    if (blue > 255)                    { blue = 255; }                    else if (blue < 0)                    { blue = 0; }
                    if (green > 255)                    { green = 255; }                    else if (green < 0)                    { green = 0; }
                    if (red > 255)                    { red = 255; }                    else if (red < 0)                    { red = 0; }
                    colors[offsetX + offsetY * source.Width] = 
                        new Color((byte)red, (byte)green, (byte)blue, 255);
                }
            }
            return colors.ToTexture(source.Width, source.Height, GraphicsDevice, GraphicsContext.CommandList);
        }

        public static Vector3 GetTangent(this Color[] ColorValues,
            Texture heightmap, int x, int z,Int2 range)
        {
            var flip = 1;
            var here = new Vector3(x, ColorValues.GetHeightAt(
                heightmap, x, z, range), z);
            var left = new Vector3(x - 1, ColorValues.GetHeightAt(
                heightmap, x - 1, z, range), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, ColorValues.GetHeightAt(
                    heightmap, x + 1, z, range), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }

        public static Vector3 GetNormal(this Color[] ColorValues,
            Texture heightmap, int x, int y, Int2 range)
        {
            var heightL = ColorValues.GetHeightAt(heightmap,x - 1, y, range);
            var heightR = ColorValues.GetHeightAt(heightmap, x + 1, y, range);
            var heightD = ColorValues.GetHeightAt(heightmap, x, y - 1, range);
            var heightU = ColorValues.GetHeightAt(heightmap, x, y + 1, range);
            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }

        public static Mesh ToMesh(this Texture texture,
            GraphicsContext GraphicsContext, GraphicsDevice GraphicsDevice,
            float m_QuadSideWidthX, float m_QuadSideWidthZ, 
            float TEXTURE_REPEAT, int Tesselation,
            Vector3 WorldLocation,Int2 range)
        {
            Mesh Mesh = new Mesh();
            Color[] ColorValues = texture.GetColorData(GraphicsContext);
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = (texture.Height - 1) / Tesselation,
                m_num_quads_x = (texture.Width - 1) / Tesselation;
            Vector3 maxBounds = new Vector3(texture.Width * m_QuadSideWidthX, 0,
                texture.Height * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = Tesselation * (maxBounds.X - minBounds.X) / 
                (texture.Width-1) ;// m_num_quads_x;
            float stepZ = Tesselation * (maxBounds.Z - minBounds.Z) /
                (texture.Height - 1);// m_num_quads_z;
            int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            //	R = 149.0f / 255.0f, G = 135.0f / 255.0f, B = 118.0f / 255.0f;
            // Create the vertex array.

            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            Vector3[] TerrainPoints = new Vector3[m_vertexCount];

            // Vector3[] Normals = heightmap.CalculateNormals();
            // Initialize the index to the vertex buffer.
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    float ht = 0;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                        ht = ColorValues[z * texture.Width + x].R;
                    else
                        ht = ColorValues[z * texture.Width + x].ToFloat();
                    float height = range.X +
                          (range.Y - range.X)
                          * ht / PerlinNoise.HeightMultiplier;
                    m_vertices[count].Position = new Vector3(pos.X + WorldLocation.X,
                        height + WorldLocation.Y,
                        pos.Z + WorldLocation.Z);
                    TerrainPoints[count] = m_vertices[count].Position;
                    if (TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[count].TexCoord.X = //m_QuadSideWidthX * 
                            TEXTURE_REPEAT * x / (float)numVertsX * Tesselation;
                        m_vertices[count].TexCoord.Y =// m_QuadSideWidthZ * 
                            TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * Tesselation;
                    }
                    else //if (comp.TEXTURE_REPEAT == 0)//make each quad have the texture
                    {
                        m_vertices[count].TexCoord.X =// m_QuadSideWidthX * 
                            x * Tesselation;
                        m_vertices[count].TexCoord.Y =// m_QuadSideWidthZ * 
                            z * Tesselation;
                    }
                    m_vertices[count].Normal = ColorValues.GetNormal(
                        texture,x, z, range);
                    m_vertices[count].Tangent = ColorValues.GetTangent(
                        texture,x, z, range);
                    m_vertices[count].Color = new Color(R / 255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    m_vertices[count].Color1 = new Color(0.1f, 0, 0, 0.0f);// / 255.0f;
                    m_vertices[count].Color2 = new Color(0);// / 255.0f;
                    pos.X += stepX;
                    count++;
                }
                // Increment Z
                pos.Z += stepZ;
            }
            int[] indices = new int[m_vertexCount * 6];
            count = 0;
            for (z = 0; z < m_num_quads_z; z++)
            {
                for (x = 0; x < m_num_quads_x; x++)
                {
                    var vbase = numVertsX * z + x;
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + 1 + numVertsX);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase);
                }
            }
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, m_vertices, GraphicsResourceUsage.Dynamic);
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices);
            return new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexTypePosTexNormColor.Layout, vertexBuffer.ElementCount) },
                },
                BoundingBox = BoundingBox.FromPoints(TerrainPoints),
                BoundingSphere = BoundingSphere.FromPoints(TerrainPoints)
            };
        }

        public static Heightmap ToHeightMap(this Texture texture,
      GraphicsContext GraphicsContext, Vector2 HeightRange, float HeightScale,
      bool isflat = false)
        {
            Heightmap Heightmap = new Heightmap();
            Heightmap.Size = new Int2(texture.Width, texture.Height);
            Heightmap.HeightType = HeightfieldTypes.Float;
            Heightmap.HeightRange = HeightRange;
            Heightmap.HeightScale = HeightScale;
            int i, j, index;
            Heightmap.Floats = new float[texture.Width * texture.Height];
            if (isflat) return Heightmap;
            Color[] heightValues = texture.GetColorData(GraphicsContext);
            // Get the height information and put it in the array
            //short   -32,768 to 32,767, sum is 65,535
            //All channels are used to build the map height as a short value
            for (i = 0; i < texture.Width; i++)
            {
                for (j = 0; j < texture.Height; j++)
                {
                    index = (texture.Width * j) + i;
                    float ht = 0;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                        ht = heightValues[index].R;
                    else
                        ht = heightValues[index].ToFloat();
                    Heightmap.Floats[index] =
                        (HeightRange.X +
                          (HeightRange.Y - HeightRange.X)
                          * ht / PerlinNoise.HeightMultiplier);
                }
            }
            return Heightmap;
        }

        public static bool CheckGrayScale(this Texture texture,
            GraphicsContext GraphicsContext)
        {
            Color[]? Colors = texture.GetColorData(GraphicsContext);
            for (int i = 0; i < Colors?.Length; i++)
            {
                if (Colors[i].R != Colors[i].G || Colors[i].R != Colors[i].B
                    )//|| Colors[i].A<255)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if the Texture Asset is of type BC1_UNorm_SRgb, BC2_UNorm_SRgb, BC3_UNorm_SRgb
        /// which is the typical format for assigned textures in the game studio
        /// and then finds the actual Source of the texture and loads it from disk to decompress.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="GraphicsDevice"></param>
        /// <param name="GraphicsContext"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Texture DeCompress(this Texture source,string AssetName,
            GraphicsDevice GraphicsDevice, GraphicsContext GraphicsContext)
        {
            Texture? texout=new Texture();
            if (source.Description.Format == PixelFormat.BC1_UNorm_SRgb ||
                source.Description.Format == PixelFormat.BC2_UNorm_SRgb ||
                source.Description.Format == PixelFormat.BC3_UNorm_SRgb ||
                source.Description.Format == PixelFormat.BC7_UNorm_SRgb)
            {
                string texSource = Utility.Resources_Directory + Utility.FindAssetSourceDir(AssetName);
                using (var inStream = System.IO.File.OpenRead(texSource))
                    texout = Texture.Load(GraphicsDevice, inStream, loadAsSRGB: false);//false a must
                texout = texout?.ReFormat(GraphicsContext, PixelFormat.R8G8B8A8_UNorm);//dont put SRgb
                if (texout == null) throw new Exception("Bad texture file detected while DeCompress");
            }
 
            return texout;
        }

        public static Color GetColor(this Color[] ColorValues,
           Texture texin,int i,int j)
        {
            try
            {
                if (texin.Description.Format == PixelFormat.R8G8B8A8_UNorm)
                {
                    return ColorValues[i+j*texin.Width];
                }
                else if (texin.Description.Format == PixelFormat.B8G8R8A8_UNorm)
                {
                    for (int k = 0; k < ColorValues.Length; k++)
                    {
                        byte r = ColorValues[k].R;
                        ColorValues[k].R = ColorValues[k].B;
                        ColorValues[k].B = r;
                    }
                    return ColorValues[i + j * texin.Width];
                }
                else
                {
                    //NEED TO HANDLE OTHER FORMATS HERE
                    //compressed
                    if (texin.Description.Format == PixelFormat.BC1_UNorm_SRgb)
                    {

                    }
                }
                return Color.Zero;
            }
            catch { return Color.Zero; }
        }

        public static Color[] GetColorData(this Texture texin,
            GraphicsContext GraphicsContext)
        {
            Color[] ColorValues = new Color[texin.Width * texin.Height];
            try
            {
                if (texin.Description.Format == PixelFormat.R8G8B8A8_UNorm)
                {
                    texin.GetData(GraphicsContext.CommandList, ColorValues);
                    return ColorValues;
                }
                else if (texin.Description.Format == PixelFormat.B8G8R8A8_UNorm)
                {
                    texin.GetData(GraphicsContext.CommandList, ColorValues);
                    for (int i = 0; i < ColorValues.Length; i++)
                    {
                        byte r = ColorValues[i].R;
                        ColorValues[i].R = ColorValues[i].B;
                        ColorValues[i].B = r;
                    }
                }
                else
                {
                    //NEED TO HANDLE OTHER FORMATS HERE
                    //compressed
                    if (texin.Description.Format == PixelFormat.BC1_UNorm_SRgb)
                    {

                    }
                }
                return ColorValues;
            }
            catch { return null; }
        }

        public static Color[]? GetColorDataReformat(this Texture texin,
            GraphicsContext GraphicsContext)
        {
            Color[] ColorValues = new Color[texin.Width * texin.Height];
            try
            {
                if (texin.Description.Format != PixelFormat.R8G8B8A8_UNorm)
                {
                    //return ColorValues;
                    texin.ReFormat(GraphicsContext);
                }
                texin.GetData(GraphicsContext.CommandList, ColorValues);
                /* if (texin.Description.Format == PixelFormat.B8G8R8A8_UNorm)
                           {
                               for (int i = 0; i < ColorValues.Length; i++)
                               {
                                   byte r = ColorValues[i].R;
                                   ColorValues[i].R = ColorValues[i].B;
                                   ColorValues[i].B = r;
                               }
                           }*/
                return ColorValues;
            }
            catch { return null; }
        }

        public static unsafe Bitmap ToBitmap(this Texture texin,
            GraphicsContext GraphicsContext
            // PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm
            )
        {
         //   TextureHelper.ImportTextureImage
            Bitmap bitmap = new Bitmap(texin.Width, texin.Height);
            var sourceArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //PixelFormat pixelformat = texin.Format;
            //if(!texin.CheckFormat(PixelFormat.R8G8B8A8_UNorm)) return null;
            Stride.Graphics.Image im = texin.GetDataAsImage(
                GraphicsContext.CommandList);//.GetPixelBuffer(0,0);
            PixelBuffer pixelBuffers = im.GetPixelBuffer(0, 0);
            // Lock System.Drawing.Bitmap
            var bitmapData = bitmap.LockBits(sourceArea, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            CopyMemoryBGRA(bitmapData.Scan0, pixelBuffers.DataPointer, pixelBuffers.BufferStride);
            return bitmap;
        }

        public static unsafe void CopyMemoryBGRA(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
        {
            if ((sizeInBytesToCopy & 3) != 0)
                throw new ArgumentException("Should be a multiple of 4.", "sizeInBytesToCopy");

            var bufferSize = sizeInBytesToCopy / 4;
            var srcPtr = (uint*)src;
            var destPtr = (uint*)dest;
            for (int i = 0; i < bufferSize; ++i)
            {
                var value = *srcPtr++;
                // value: 0xAARRGGBB or in reverse 0xAABBGGRR
                value = BinaryPrimitives.ReverseEndianness(value);
                // value: 0xBBGGRRAA or in reverse 0xRRGGBBAA
                value = BitOperations.RotateRight(value, 8);
                // value: 0xAABBGGRR or in reverse 0xAARRGGBB
                *destPtr++ = value;
            }
        }

        public static Image resizeImage1(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }

        public static PixelFormat QaLPixelFormat = PixelFormat.R8G8B8A8_UNorm;
        public static bool CheckFormat(this Texture texin,
            PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm )
        {
            if (texin.Description.Format == pixelformat) return true;
            return false;
        }

        /// <summary>
        /// Performs render to texture in a single function in order to resize and reformat a 
        /// given texture. if the texture was loaded using Content.Load<Texture>(...)
        /// the texture is usually compressed and it will be by default 32x32.
        /// So make sure you decompress it first by loading the Source file from the disc that this
        /// asset refers to.
        /// </summary>
        /// <param name="texin"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="GraphicsContext"></param>
        /// <param name="pixelformat"></param>
        /// <returns></returns>
        public static Texture? Resize(this Texture texin, int width, int height,
            GraphicsContext GraphicsContext, 
            PixelFormat pixelformat= PixelFormat.R8G8B8A8_UNorm
            // ,SpriteFont arial
            )
        {
            try
            {
                if (texin.Width == 0 || texin.Height == 0) return null;
                //if(texin.Width==width && texin.Height == height) return texin;
                GraphicsDevice GraphicsDevice = texin.GraphicsDevice;
                Texture offlineTarget = Texture.New2D(GraphicsDevice, width, height,
                    pixelformat, TextureFlags.ShaderResource |
                    TextureFlags.RenderTarget);
                Texture depthBuffer = Texture.New2D(GraphicsDevice, width, height,
                    PixelFormat.//D32_Float_S8X24_UInt//
                                                    D24_UNorm_S8_UInt//D32_Float//.D16_UNorm
                     , TextureFlags.DepthStencil);
                SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);

                // render into texture
                GraphicsContext.CommandList.Clear(offlineTarget, new Color4(0, 0, 0, 0));
                GraphicsContext.CommandList.Clear(depthBuffer, DepthStencilClearOptions.DepthBuffer);
                GraphicsContext.CommandList.SetRenderTargetAndViewport(depthBuffer, offlineTarget);

                spriteBatch.Begin(GraphicsContext);
                spriteBatch.Draw(texin, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
                //          spriteBatch.DrawString(arial, "Text on Top", new Vector2(75, 75), Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 2f, TextAlignment.Left);
                spriteBatch.End();

                // copy texture on screen
                GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
                GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
                GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

                spriteBatch.Begin(GraphicsContext);
                spriteBatch.Draw(offlineTarget, new RectangleF(0, 0, width, height), null, Color.White, 0, Vector2.Zero);
                spriteBatch.End();
                // offlineTarget.ToStaging();
                return offlineTarget;
            }
            catch { return null; }
        }

        /// <summary>
        /// Reformats the pixels of a given texture via a rendering to texture approach.
        /// </summary>
        /// <param name="texin"></param>
        /// <param name="GraphicsContext"></param>
        /// <param name="pixelformat"></param>
        /// <returns></returns>
        public static Texture? ReFormat(
            this Texture texin, GraphicsContext GraphicsContext,
            PixelFormat pixelformat = PixelFormat.R8G8B8A8_UNorm)
        {
            return texin.Resize(texin.Width,texin.Height, GraphicsContext, pixelformat);
        }
    }
}

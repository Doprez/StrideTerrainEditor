//by Idomeneas
using Stride.Core.Mathematics;
using Stride.Physics;
using Stride.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Graphics;
using System.Threading.Tasks;
using TerrainEditor;
using static Stride.Graphics.Buffer;
using System.Windows.Forms;
using Stride.Engine;
using Stride.Core;

namespace HeightMapEditor
{
    public static class HeightmapExtensions
    {

        public static float[] ToFloats(this Heightmap heightmap)
        {
            int i, j, index, m_Width = heightmap.Size.X, m_Height = heightmap.Size.Y;
            float[] heightValues = new float[m_Width * m_Height];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    heightValues[index] = heightmap.Shorts[index];
                }
            }
            return heightValues;
        }
        public static byte[] ToBytes(this Heightmap heightmap)
        {
            int i, j, index, m_Width = heightmap.Size.X, m_Height = heightmap.Size.Y;
            byte[] heightValues = new byte[m_Width * m_Height];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    heightValues[index] = (byte)heightmap.Shorts[index];
                }
            }
            return heightValues;
        }
        public static Texture ToTexture(this Heightmap heightmap,
            GraphicsDevice GraphicsDevice, CommandList CommandList)
        {
            int i, j, index, m_Width = heightmap.Size.X, m_Height = heightmap.Size.Y;
            Texture tex = Texture.New2D(GraphicsDevice, m_Width,
                m_Height, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource,1,GraphicsResourceUsage.Dynamic);
            Color[] heightValues = new Color[m_Width * m_Height];
            // Get the height information and put it in the array
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    heightValues[index]= 
                        heightmap.Shorts[index].AsStrideColor();
                }
            }
            tex.SetData(CommandList, heightValues);
            return tex;
        }


        /// <summary>
        /// Creates the terrain mesh from a given heightmap. Tesselation divides
        /// the quad numbers.
        /// </summary>
        /// <param name="heightmap"></param>
        /// <param name="GraphicsDevice"></param>
        /// <param name="m_QuadSideWidthX"></param>
        /// <param name="m_QuadSideWidthZ"></param>
        /// <param name="TEXTURE_REPEAT"></param>
        /// <param name="TerrainPoints"></param>
        /// <param name="Tesselation"></param>
        /// <returns></returns>
        public static Mesh ToMesh(this Heightmap heightmap,
            GraphicsDevice GraphicsDevice,
            float m_QuadSideWidthX, float m_QuadSideWidthZ,float TEXTURE_REPEAT,
            int Tesselation,Vector3 WorldLocation)
        {
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = (heightmap.Size.Y - 1)/ Tesselation, 
                m_num_quads_x = (heightmap.Size.X - 1)/ Tesselation;
            Vector3 maxBounds = new Vector3(heightmap.Size.X  * m_QuadSideWidthX, 0,
                heightmap.Size.Y * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = Tesselation*(maxBounds.X - minBounds.X) / 
                (heightmap.Size.X-1) ;// m_num_quads_x;
            float stepZ = Tesselation*(maxBounds.Z - minBounds.Z) / 
                (heightmap.Size.Y-1);// m_num_quads_z;
            int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            //	R = 149.0f / 255.0f, G = 135.0f / 255.0f, B = 118.0f / 255.0f;
            // Create the vertex array.

            //VertexTypePosTexNormColor[] 
            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            Vector3 []TerrainPoints = new Vector3[m_vertexCount];

           // Vector3[] Normals = heightmap.CalculateNormals();
            // Initialize the index to the vertex buffer.
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    m_vertices[count].Position = new Vector3(pos.X+ WorldLocation.X,
                        heightmap.GetHeightAt(x, z)+ WorldLocation.Y,
                        pos.Z+ WorldLocation.Z);
                    TerrainPoints[count] = m_vertices[count].Position;
                    if (TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[count].TexCoord.X = m_QuadSideWidthX * TEXTURE_REPEAT * x / (float)numVertsX * Tesselation;
                        m_vertices[count].TexCoord.Y = m_QuadSideWidthZ * TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ* Tesselation;
                    }
                    else //if (comp.TEXTURE_REPEAT == 0)//make each quad have the texture
                    {
                        m_vertices[count].TexCoord.X = m_QuadSideWidthX * x * Tesselation;
                        m_vertices[count].TexCoord.Y = m_QuadSideWidthZ * z * Tesselation;
                    }
                    m_vertices[count].Normal = heightmap.GetNormal(x, z);
                    m_vertices[count].Tangent = heightmap.GetTangent(x, z);
                    m_vertices[count].Color = new Color(R / 255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    m_vertices[count].Color1 = new Color(0.1f, 0, 0, 0.0f);// / 255.0f;
                    m_vertices[count].Color2 = new Color(0);// / 255.0f;
                    pos.X += stepX;
                    count++;
                }
                // Increment Z
                pos.Z += stepZ;
            }
            //   Array.Clear(Normals, 0, Normals.Length);

            //int[] 
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

            /*           count = 0;
                       for (z = 0; z < m_num_quads_z; z++)
                       {
                           for (x = 0; x < m_num_quads_x; x++)
                           {

                               // Now create two triangles for that quad.
                               // Triangle 1 - Upper left.
                               m_vertices[count].TexCoord.X = 0.0f;
                               m_vertices[count++].TexCoord.Y = 0.0f;

                                // Triangle 1 - Upper right.
                               m_vertices[count].TexCoord.X = 1.0f;
                               m_vertices[count++].TexCoord.Y = 0.0f;

                               // Triangle 1 - Bottom left.
                               m_vertices[count].TexCoord.X = 0.0f;
                               m_vertices[count++].TexCoord.Y = 1.0f;

                               // Triangle 2 - Bottom left.
                               m_vertices[count].TexCoord.X = 0.0f;
                               m_vertices[count++].TexCoord.Y = 1.0f;

                               // Triangle 2 - Upper right.
                               m_vertices[count].TexCoord.X = 1.0f;
                               m_vertices[count++].TexCoord.Y = 0.0f;

                               // Triangle 2 - Bottom right.
                               m_vertices[count].TexCoord.X = 1.0f;
                               m_vertices[count++].TexCoord.Y = 1.0f;

                           }
                       }

           */
            //    Vector3[] Normals = CalculateVertexNormals(points, indices);
            //    for (int i = 0; i < points.Length; i++) m_vertices[i].Normal = Normals[i];

            Stride.Graphics.Buffer vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, m_vertices, GraphicsResourceUsage.Dynamic);
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

        public static Vector3[] ToWorldPoints(this Heightmap heightmap,   
    float m_QuadSideWidthX, float m_QuadSideWidthZ)
        {
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = heightmap.Size.Y - 1, m_num_quads_x = heightmap.Size.X - 1;
            Vector3 maxBounds = new Vector3(heightmap.Size.X * m_QuadSideWidthX, 0,
                heightmap.Size.Y * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = (maxBounds.X - minBounds.X) / (heightmap.Size.X-1);
            float stepZ = (maxBounds.Z - minBounds.Z) / (heightmap.Size.Y-1);
            int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            Vector3[] points = new Vector3[m_vertexCount];

            // Vector3[] Normals = heightmap.CalculateNormals();
            // Initialize the index to the vertex buffer.
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    points[count] = new Vector3(pos.X,
                        heightmap.GetHeightAt(x, z), pos.Z);
                     pos.X += stepX;
                    count++;
                }
                // Increment Z
                pos.Z += stepZ;
            }

            return points;
        }
        
        public static Vector3[] ToWorldTreePoints(this Heightmap heightmap,
    float m_QuadSideWidthX, float m_QuadSideWidthZ)
        {
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = heightmap.Size.Y - 1, m_num_quads_x = heightmap.Size.X - 1;
            Vector3 maxBounds = new Vector3(m_num_quads_x * m_QuadSideWidthX, 0,
                m_num_quads_z * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = (maxBounds.X - minBounds.X) / m_num_quads_x;
            float stepZ = (maxBounds.Z - minBounds.Z) / m_num_quads_z;
            int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            Vector3[] points = new Vector3[m_vertexCount];

            // Vector3[] Normals = heightmap.CalculateNormals();
            // Initialize the index to the vertex buffer.
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    points[count] = new Vector3(pos.X,
                        heightmap.GetHeightAt(x, z), pos.Z);
                    pos.X += stepX;
                    count++;
                }
                // Increment Z
                pos.Z += stepZ;
            }

            return points;
        }

        public static Vector3 GetNormal(this Heightmap heightmap, int x, int y)
        {
            var heightL = GetHeightAt(heightmap, x - 1, y);
            var heightR = GetHeightAt(heightmap, x + 1, y);
            var heightD = GetHeightAt(heightmap, x, y - 1);
            var heightU = GetHeightAt(heightmap, x, y + 1);
          /*  Vector3 vertex1 = new Vector3(x, heightmap.GetHeightAt(x, y), y);
            Vector3 vertex2 = new Vector3(x + 1, heightmap.GetHeightAt(x + 1, y), y);
            Vector3 vertex3 = new Vector3(x+1, heightmap.GetHeightAt(x+1, y + 1), y + 1);
            Vector3 vector1 = vertex1 - vertex3;
            Vector3 vector2 = vertex3 - vertex2;
            // Calculate the cross product of those two vectors to get the un-normalized value for this face normal.
            Vector3 normal = Vector3.Cross(vector1, vector2);*/

            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }

        public static Vector3[] CalculateVertexNormals(Vector3[] vertexPositions, 
            int[] triangleIndices)
        {
            Vector3[] vertexNormals = new Vector3[vertexPositions.Length];
            // Zero-out our normal buffer to start from a clean slate.
            for (int vertex = 0; vertex < vertexPositions.Length; vertex++)
                vertexNormals[vertex] = Vector3.Zero;

            // For each face, compute the face normal, and accumulate it into each vertex.
            for (int index = 0; index < triangleIndices.Length; index += 3)
            {
                int vertexA = triangleIndices[index];
                int vertexB = triangleIndices[index + 1];
                int vertexC = triangleIndices[index + 2];

                var edgeAB = vertexPositions[vertexB] - vertexPositions[vertexA];
                var edgeAC = vertexPositions[vertexC] - vertexPositions[vertexA];

                // The cross product is perpendicular to both input vectors (normal to the plane).
                // Flip the argument order if you need the opposite winding.    
                var areaWeightedNormal = Vector3.Cross(edgeAC, edgeAB); //Vector3.Cross(edgeAB, edgeAC);

                // Don't normalize this vector just yet. Its magnitude is proportional to the
                // area of the triangle (times 2), so this helps ensure tiny/skinny triangles
                // don't have an outsized impact on the final normal per vertex.

                // Accumulate this cross product into each vertex normal slot.
                vertexNormals[vertexA] += areaWeightedNormal;
                vertexNormals[vertexB] += areaWeightedNormal;
                vertexNormals[vertexC] += areaWeightedNormal;
            }

            // Finally, normalize all the sums to get a unit-length, area-weighted average.
            for (int vertex = 0; vertex < vertexPositions.Length; vertex++)
                vertexNormals[vertex].Normalize();

            return vertexNormals;
        }

        public static Vector3[] CalculateNormals(this Heightmap heightmap)
        {
            int i, j, index, count;
            int m_num_quads_z = heightmap.Size.Y - 1, m_num_quads_x = heightmap.Size.X - 1;
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1; Vector3 vertex1 = new Vector3(), vertex2 = new Vector3(),
                vertex3 = new Vector3(), vector1 = new Vector3(),
                vector2 = new Vector3();
            float []sum = new float[3];
            float length;
            //basic normals per face
            Vector3[] normals=new Vector3[m_num_quads_x * m_num_quads_z];
            // Go through all the faces in the mesh and calculate their normals.
            // 	for(j=m_terrainHeight-2; j>=0; j--){	for(i=0; i<m_terrainWidth-1; i++)		{
            for (i = 0; i < m_num_quads_x; i++)
            {
                for (j = 0; j < m_num_quads_z; j++)
                {
                    //	for(i=0;i<NumFaces;i++)	{		
                   // index1 = (j * numVertsX) + i;
                  //  index2 = (j * numVertsX) + (i + 1);
                  //  index3 = ((j + 1) * numVertsX) + i;
                    // Get three vertices from the face=triangle.
                    vertex1 = new Vector3(i, heightmap.GetHeightAt(i, j), j);
                    vertex2 = new Vector3(i+1, heightmap.GetHeightAt(i+1, j), j);
                    vertex3 = new Vector3(i, heightmap.GetHeightAt(i, j+1), j+1);
                    // Calculate the two vectors for this face.
                    vector1 = vertex1 - vertex3;
                    vector2 = vertex3 - vertex2;
                    index = (j * (numVertsX - 1)) + i;
                    // Calculate the cross product of those two vectors to get the un-normalized value for this face normal.
                    normals[index] = Vector3.Cross(vector1, vector2);
            //        normals[index].X = (vector1[1] * vector2[2]) - (vector1[2] * vector2[1]);
            //        normals[index].Y = (vector1[2] * vector2[0]) - (vector1[0] * vector2[2]);
            //        normals[index].Z = (vector1[0] * vector2[1]) - (vector1[1] * vector2[0]);
                }
            }

            //average normals per face
            Vector3[] AverageNormals = new Vector3[numVertsX * numVertsZ];
            // Now go through all the vertices and take an average of each face normal 	
            // that the vertex touches to get the averaged normal for that vertex.
            //	for(j=m_terrainHeight-1; j>=0; j--){	for(i=0; i<m_terrainWidth; i++)		{
            for (i = 0; i < numVertsX; i++)
            {
                for (j = 0; j < numVertsZ; j++)
                {
                    // Initialize the sum.
                    sum[0] = 0.0f;
                    sum[1] = 0.0f;
                    sum[2] = 0.0f;

                    // Initialize the count.
                    count = 0;

                    // Bottom left face.
                    if (((i - 1) >= 0) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (numVertsX - 1)) + (i - 1);

                        sum[0] += normals[index].X;
                        sum[1] += normals[index].Y;
                        sum[2] += normals[index].Z;
                        count++;
                    }

                    // Bottom right face.
                    if ((i < (numVertsX - 1)) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (numVertsX - 1)) + i;

                        sum[0] += normals[index].X;
                        sum[1] += normals[index].Y;
                        sum[2] += normals[index].Z;
                        count++;
                    }

                    // Upper left face.
                    if (((i - 1) >= 0) && (j < (numVertsZ - 1)))
                    {
                        index = (j * (numVertsX - 1)) + (i - 1);

                        sum[0] += normals[index].X;
                        sum[1] += normals[index].Y;
                        sum[2] += normals[index].Z;
                        count++;
                    }

                    // Upper right face.
                    if ((i < (numVertsX - 1)) && (j < (numVertsZ - 1)))
                    {
                        index = (j * (numVertsX - 1)) + i;

                        sum[0] += normals[index].X;
                        sum[1] += normals[index].Y;
                        sum[2] += normals[index].Z;
                        count++;
                    }

                    // Take the average of the faces touching this vertex.
                    sum[0] = (sum[0] / (float)count);
                    sum[1] = (sum[1] / (float)count);
                    sum[2] = (sum[2] / (float)count);

                    // Calculate the length of this normal.
                    length = MathF.Sqrt((sum[0] * sum[0]) + (sum[1] * sum[1]) + (sum[2] * sum[2]));

                    // Get an index to the vertex location in the height map array.
                    index = (j * numVertsX) + i;

                    // Normalize the final shared normal for this vertex and store it in the height map array.
                    AverageNormals[index].X = sum[0] / length;
                    AverageNormals[index].Y = sum[1] / length;
                    AverageNormals[index].Z = sum[2] / length;
                }
            }

            // Release the temporary normals.
            Array.Clear(normals, 0, normals.Length);
            return AverageNormals;
        }

        public static Vector3 GetTangent(this Heightmap heightmap, int x, int z)
        {
            var flip = 1;
            var here = new Vector3(x, GetHeightAt(heightmap, x, z), z);
            var left = new Vector3(x - 1, GetHeightAt(heightmap, x - 1, z), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, GetHeightAt(heightmap, x + 1, z), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }

        public static bool IsValidCoordinate(this Heightmap heightmap, int x, int y)
            => x >= 0 && x < heightmap.Size.X && y >= 0 && y < heightmap.Size.Y;

        public static int GetHeightIndex(this Heightmap heightmap, int x, int y)
            => y * heightmap.Size.X + x;

        public static float GetHeightAt(this Heightmap heightmap, int x, int y)
        {
            if (!IsValidCoordinate(heightmap, x, y))// || x == 0 || y == 0 || x == heightmap.Size.X - 1 || y == heightmap.Size.Y - 1)
            {
                return heightmap.HeightRange.X;
            }
            var index = GetHeightIndex(heightmap, x, y);
            float ht = 0;
            if (PerlinNoise.IsGrayScaleHeightMap)
                ht = heightmap.Floats[index].AsStrideColor().R;
            else
                ht = heightmap.Floats[index].AsStrideColor().ToFloat();
            float height = heightmap.HeightRange.X +
                  (heightmap.HeightRange.Y - heightmap.HeightRange.X)
                  * ht / PerlinNoise.HeightMultiplier;
            return height;
        }

        public static float GetHeightAt(this Heightmap heightmap, float x, float z)
        {
            if (x < 0.0f || x >= heightmap.Size.X || z < 0 || z >= heightmap.Size.Y)
                return -1;
            int xi = (int)x, zi = (int)z;
            //return heightmap.GetHeightAt(xi,zi);
            float xpct = x - xi, zpct = z - zi;

            if (xi == heightmap.Size.X - 1)
            {
                --xi;
                xpct = 1.0f;
            }
            if (zi == heightmap.Size.Y - 1)
            {
                --zi;
                zpct = 1.0f;
            }

            var heights = new float[]
            {
                GetHeightAt(heightmap, xi, zi),
                GetHeightAt(heightmap, xi, zi + 1),
                GetHeightAt(heightmap, xi + 1, zi),
                GetHeightAt(heightmap, xi + 1, zi + 1)
            };

            var w = new float[]
            {
                (1.0f - xpct) * (1.0f - zpct),
                (1.0f - xpct) * zpct,
                xpct * (1.0f - zpct),
                xpct * zpct
            };

            var height = w[0] * heights[0] + w[1] * heights[1] + w[2] * heights[2] + w[3] * heights[3];

            return height;
        }

        public static bool IntersectsRay(this Heightmap heightmap, 
            Ray ray, out Vector3 point,float m_QuadSideWidthX=1.0f,
            float m_QuadSideWidthZ = 1.0f)
        {
            //point = ray.Position;
            //check each quad
           // int quadnumx = (heightmap.Size.X - 1), quadnumz = (heightmap.Size.Y - 1);
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 1.5f);
            int x, z;
            float mindist = 1000000000.0f;
            point = Vector3.Zero;
            bool foundit = false;
            for (z = 0; z < heightmap.Size.Y; z++)
            {
                for (x = 0; x < heightmap.Size.X; x++)
                {
                    sphere.Center = new Vector3(x* m_QuadSideWidthX, 
                        heightmap.GetHeightAt(x, z), z* m_QuadSideWidthZ);
                    if (sphere.Intersects(ref ray, out Vector3 pt))
                    {          
                        //get nearest hit
                        float dist = Vector3.Distance(pt, ray.Position);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            point = sphere.Center;// pt;
                            foundit = true;
                        }
                        //return true;//gets the first hit, replace out Vector3 pt with out point and comment the above
                    }
                }
            }            
            return foundit;
        }

        /// <summary>
        /// The entity that will contain the new collider
        /// </summary>
        /// <param name="heightmap"></param>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static void GenerateCollider(this Heightmap heightmap,Entity ent)
        {
            ent.RemoveAll<StaticColliderComponent>();
            //                    StaticColliderComponent comp = scr.TerrainModelEntity.GetOrCreate<StaticColliderComponent>();
            int Width = heightmap.Size.X, Height = heightmap.Size.Y,
                  size = Width * Height;
          //  (ICollection<Vector3> vertices, ICollection<int> indices) =
         //   heightmap.GetVerticesIndices();
          //  StaticMeshColliderShape meshShape = new StaticMeshColliderShape(vertices, indices);
          
            UnmanagedArray<float> Heightfield = new UnmanagedArray<float>(size);
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    Heightfield[heightmap.GetHeightIndex(i, j)] =
                        heightmap.GetHeightAt(i, j);// hts[i];
                }
            HeightfieldColliderShape meshShape = new HeightfieldColliderShape(
                Width, Height, Heightfield, heightmap.HeightScale,
                heightmap.HeightRange.X, heightmap.HeightRange.Y, false);
            StaticColliderComponent comp = new StaticColliderComponent();
            comp.ColliderShape = meshShape;
            meshShape.LocalOffset = new Vector3(Width / 2, 0.0f, Height / 2);
            meshShape.UpdateLocalTransformations();
            ent.Add(comp);
        }

    }
}

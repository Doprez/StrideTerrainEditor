//by Idomeneas
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Importer.Assimp;
using Stride.Rendering;
using TerrainEditor;

namespace HeightMapEditor
{
    //yalm file !HeightMapEditor.TerrainComponent,HeightMapEditor
    //namespace it is in, class name, project name
    //   [DataContract("TerrainComponentEditor")]
    public class TerrainComponent : StartupScript
    {
        [DataMember(0)]
        public Material? MaterialBlendSingle { get; set; }

        [DataMember(1)]
        public Material? MaterialBlendHeight { get; set; }
        
        [DataMember(2)]
        public Material? MaterialBlendMulti { get; set; }

        [DataMember(10)]
        public int Width { get; set; } = 1024;//=> Heightmap.Size.X;
        
        [DataMember(20)]
        public int Height { get; set; } = 1024;//=> Heightmap.Size.Y;
        
        [DataMember(25)]
        public Int2 HeightRange { get; set; } = new Int2(-100,100);//=> Heightmap.Size.Y;

        [DataMember(30)]
        public bool CastShadows { get; set; } = true;
 
        [DataMember(40)]
        public float m_QuadSideWidthX { get; set; } = 1.0f;

        [DataMember(50)]
        public float m_QuadSideWidthZ { get; set; } = 1.0f;

        [DataMember(60)]
        public int TEXTURE_REPEAT { get; set; } = 1;

        [DataMember(61)]
        public int TerrainLOD { get; set; } = 1;
        
        /// <summary>
        /// Used to hide or show the Wireframe terrain
        /// </summary>
        [DataMember(83)]
        public bool ShowWireframe { get; set; } = false;

        [DataMember(200)]
        public Model? VegetationModel1 { get; set; }

        [DataMember(210)]
        public Model? VegetationModel2 { get; set; }

        [DataMember(220)]
        public Model? VegetationModel3 { get; set; }

        [DataMemberIgnore]
        public Model[]? AllTreeModels;

        [DataMember(230)]
        public Model? GrassModel1 { get; set; }

        [DataMember(231)]
        public Model? GrassModel2 { get; set; }

        [DataMember(232)]
        public Model? GrassModel3 { get; set; }

        [DataMemberIgnore]
        public Model[]? AllGrassModels;

        [DataMemberIgnore]
        public List<VertexTypePosTexNormColor>? VertexCPUBuffer=new List<VertexTypePosTexNormColor>();

        [DataMemberIgnore]
        public Stride.Graphics.Buffer? VertexGPUBuffer;

        GraphicsDevice GraphicsDevice { get; set; }
        public CommandList GraphicsCommandList { get; internal set; }
        SceneSystem SceneSystem { get; set; }
        CameraComponent Camera { get; set; }

        [DataMemberIgnore]
        public Entity TerrainEntity { get; set; }

       [DataMemberIgnore]
       public Color[] HeightMapColors {  get; set; }
        [DataMemberIgnore]
        public float SlopeCutoff = 0.2f, DistanceMultiplier = 10.0f,
            DetailMappingDistance=0.9f;
        [DataMemberIgnore]
        public string CurrentMaterialName;

        public override void Start()
        {
            GraphicsDevice = Services.GetService<IGraphicsDeviceService>().GraphicsDevice;
            SceneSystem = Services.GetService<SceneSystem>();
            Camera = GeneralExtensions.TryGetMainCamera(SceneSystem);
            GraphicsCommandList=Game.GraphicsContext.CommandList;
           // if (AllTreeModels != null)Array.Clear(AllTreeModels);
            AllTreeModels = new Model[3];
            AllTreeModels[0] = VegetationModel1;
            AllTreeModels[1] = VegetationModel2;
            AllTreeModels[2] = VegetationModel3;

         //   if (AllGrassModels != null)Array.Clear(AllGrassModels);
            AllGrassModels = new Model[3];
            AllGrassModels[0] = GrassModel1;
            AllGrassModels[1] = GrassModel2;
            AllGrassModels[2] = GrassModel3;
        }

        public void ToggleVisible(bool visible)
        {
            TerrainEntity.Enable<ActivableEntityComponent>(visible);
        }
        public bool IntersectsRay(Ray ray, out Vector3 point, out Int2 index)
        {
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, 1.0f);
            int x, z;
            float mindist = 1000000000.0f;
            point = Vector3.Zero;
            index=Int2.Zero;
            bool foundit = false;
            for (z = 0; z < Height; z++)
            {
                for (x = 0; x < Width; x++)
                {
                    sphere.Center = new Vector3(x * m_QuadSideWidthX,
                        GetCPUHeightAt(x, z), z * m_QuadSideWidthZ);
                    if (sphere.Intersects(ref ray, out Vector3 pt))
                    {
                        //get nearest hit
                        float dist = Vector3.Distance(pt, ray.Position);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            point = sphere.Center;// pt;
                            index=new Int2(x, z);
                            foundit = true;
                        }
                        //return true;//gets the first hit, replace out Vector3 pt with out point and comment the above
                    }
                }
            }
            //no hits
            return foundit;
        }

        public void UpdateAtPos(Int2 pos, IVertex vertex)
        {
            if (this is null)
            {
                throw new System.Exception("Terrain component is null");
            }
            int index = (Width * pos.Y) + pos.X;
             VertexTypePosTexNormColor x = (VertexTypePosTexNormColor)vertex;
            VertexCPUBuffer[index] = x;
            // Update the vertex directly in the GPU buffer at the correct offset
            VertexGPUBuffer.SetData(
                GraphicsCommandList,
                ref x, // Update only this single vertex
                index * VertexTypePosTexNormColor.Layout.CalculateSize()
            );
        }

        public bool IsValidCoordinate(int x, int y)
          => x >= 0 && x < Width && y >= 0 && y < Height;

        public Vector3 GetCPUPosAt(Int2 pos)
        {
            return GetCPUPosAt(pos.X, pos.Y);
        }
        public Vector3 GetCPUPosAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return Vector3.Zero;//no contribution for this point
            }
            return VertexCPUBuffer[j * Width + i].Position;
        }
        
        public float GetCPUHeightAt(Int2 pos)
        {
            return GetCPUHeightAt(pos.X, pos.Y);
        }
        public float GetCPUHeightAt(float i, float j)
        {
            return GetCPUHeightAt((int) i, (int) j);
        }
        public float GetCPUHeightAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return HeightRange.X;//no contribution for this point
            }
            return VertexCPUBuffer[j * Width + i].Position.Y;
        }
        public void SetVertexHeight(int i, int j, float ht)
        {
            SetVertexHeight(new Int2(i, j), ht);
        }

        public void SetHeightAt(int i,int j, float ht)
        {
            HeightMapColors[j*Width+i] = ht.AsStrideColor();
        }
        public void SetVertexHeight(Int2 pos, float ht)
        {
            int index = (Width * pos.Y) + pos.X;
            var currentVertex = VertexCPUBuffer[index];
            currentVertex.Position.Y = ht;
            VertexCPUBuffer[index] = currentVertex;
//            HeightMapColors[index] = ht.AsStrideColor();
            UpdateAtPos(pos, currentVertex);
        }
        public Color GetCPUColorAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return new Color(0);
            }
            return VertexCPUBuffer[j * Width + i].Color;
        }
        public Color GetCPUWeight1At(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return new Color(0);
            }
            return VertexCPUBuffer[j * Width + i].Color1;
        }
        public Color GetCPUWeight2At(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return new Color(0);
            }
            return VertexCPUBuffer[j * Width + i].Color2;
        }
        public void SetVertexColor(Int2 pos, Vector4 col, Vector4 wt1, Vector4 wt2)
        {
            SetVertexColor(pos, col.AsNumericVec4().ToStrideColor(), 
                wt1.AsNumericVec4().ToStrideColor(), wt2.AsNumericVec4().ToStrideColor());
        }            
        public void SetVertexColor(Int2 pos, Color col, Color wt1, Color wt2)
        {
            int index = (Width * pos.Y) + pos.X;
            var currentVertex = VertexCPUBuffer[index];
            currentVertex.Color = col;
            currentVertex.Color1 = wt1;
            currentVertex.Color2 = wt2;
            VertexCPUBuffer[index] = currentVertex;
            UpdateAtPos(pos, currentVertex);
        }

        public void UpdateHeightMapColors(Texture texture)
        {
            HeightMapColors = texture.GetColorData(Game.GraphicsContext);
        }

        public Texture GetWeights1FromVertices()
        {
            Color[] cols = new Color[Width * Height];
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                {
                    int index = i + j * Width;
                    Color c = GetCPUWeight1At(i, j);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        cols[j * Width + i] = new Color(c.R, c.R, c.R, 255);
                    }
                    else
                        cols[j * Width + i] = c;
                }
            return cols.ToTexture(Width, Height, GraphicsDevice, Game.GraphicsContext.CommandList);
        }
        public Texture GetWeights2FromVertices()
        {
            Color[] cols = new Color[Width * Height];
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                {
                    int index = i + j * Width;
                    Color c = GetCPUWeight2At(i, j);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        cols[j * Width + i] = new Color(c.R, c.R, c.R, 255);
                    }
                    else
                        cols[j * Width + i] = c;
                }
            return cols.ToTexture(Width, Height, GraphicsDevice, Game.GraphicsContext.CommandList);
        }

        /// <summary>
        /// recreates buffers and mesh
        /// </summary>
        /// <param name="texture"></param>
        public void FullUpdate(Texture texture, Texture wt1, Texture wt2)
        {
            HeightMapColors = texture.GetColorData(Game.GraphicsContext);
            VertexCPUBuffer = GenerateVertices(wt1,wt2);
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    var currentVertex = VertexCPUBuffer[j*Width+i];
                    UpdateAtPos(new Int2(i,j), currentVertex);
                }
        }
        public void FullUpdateAll(Texture texture, Texture wt1, Texture wt2)
        {
            HeightMapColors = texture.GetColorData(Game.GraphicsContext);
      //      SceneSystem.SceneInstance.RootScene.Entities.Remove(TerrainEntity);
      //      TerrainEntity = new Entity("TerrainComponentModelComponent");
      //      SceneSystem.SceneInstance.RootScene.Entities.Add(TerrainEntity);
            if (VertexCPUBuffer != null) VertexCPUBuffer.Clear();
            VertexCPUBuffer = GenerateVertices(wt1, wt2);
            var indices = GenerateIndices();
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                GraphicsDevice, VertexCPUBuffer.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            if (VertexGPUBuffer != null) VertexGPUBuffer.Dispose();
            VertexGPUBuffer = vertexBuffer;
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(VertexGPUBuffer, VertexTypePosTexNormColor.Layout, VertexGPUBuffer.ElementCount) },
                },
                MaterialIndex = 0,
            };
            var model = new Model();
            model.Meshes.Add(mesh);
            var comp = TerrainEntity.GetOrCreate<ModelComponent>();
            comp.Model = model;
            if (CurrentMaterialName == "MaterialBlendSingle")
            {
                model.Materials.Add(MaterialBlendSingle);
                MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.DetailMappingDistance, DetailMappingDistance);
                MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.TextureRepeat, TEXTURE_REPEAT);
            }
            else if (CurrentMaterialName == "MaterialBlendHeight")
            {
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.TextureRepeat, TEXTURE_REPEAT);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DetailMappingDistance, DetailMappingDistance);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.SlopeCutoff, SlopeCutoff);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.HeightRange, new Vector2(HeightRange.X, HeightRange.Y));
                model.Materials.Add(MaterialBlendHeight);
            }
            else if (CurrentMaterialName == "MaterialBlendMulti")
            {
                model.Materials.Add(MaterialBlendMulti);
            }
            //        SetMaterial();
            ToggleVisible(true);
        }
        public void FullUpdateLOD(Texture wt1, Texture wt2)
        {
     //       SceneSystem.SceneInstance.RootScene.Entities.Remove(TerrainEntity);
     //       TerrainEntity = new Entity("TerrainComponentModelComponent");
     //       SceneSystem.SceneInstance.RootScene.Entities.Add(TerrainEntity);
            if (VertexCPUBuffer!=null) VertexCPUBuffer.Clear();
            VertexCPUBuffer = GenerateVertices(wt1,wt2);
            var indices = GenerateIndices();
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                GraphicsDevice, VertexCPUBuffer.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            if (VertexGPUBuffer != null) VertexGPUBuffer.Dispose();
            VertexGPUBuffer = vertexBuffer;
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(VertexGPUBuffer, VertexTypePosTexNormColor.Layout, VertexGPUBuffer.ElementCount) },
                },
                MaterialIndex = 0,
            };
            var model = new Model();
            model.Meshes.Add(mesh);
            var comp = TerrainEntity.GetOrCreate<ModelComponent>();
            comp.Model = model;
            if (CurrentMaterialName == "MaterialBlendSingle")
            {
                model.Materials.Add(MaterialBlendSingle);
                MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.DetailMappingDistance, DetailMappingDistance);
                MaterialBlendSingle.Passes[0].Parameters.Set(TerrainBlendShaderKeys.TextureRepeat, TEXTURE_REPEAT);
            }
            else if (CurrentMaterialName == "MaterialBlendHeight")
            {
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.TextureRepeat, TEXTURE_REPEAT);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.DetailMappingDistance, DetailMappingDistance);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.SlopeCutoff, SlopeCutoff);
                MaterialBlendHeight.Passes[0].Parameters.Set(TerrainHeightShaderKeys.HeightRange, new Vector2(HeightRange.X, HeightRange.Y));
                model.Materials.Add(MaterialBlendHeight);
            }
            else if (CurrentMaterialName == "MaterialBlendMulti")
            {
                model.Materials.Add(MaterialBlendMulti);
            }
            //        SetMaterial();
            ToggleVisible(true);
        }

        public Texture GetHeightmapTex()
        {
            float [] heights = GetAllHeights();
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                {
                    int index = i + j * Width;
                    float ht=GetCPUHeightAt(i,j);
                    float height = (ht - HeightRange.X) * 
                        PerlinNoise.HeightMultiplier /
                        (HeightRange.Y - HeightRange.X);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte b = height.ToByte();
                        HeightMapColors[j * Width + i] =
                            new Color(b,b,b, 255);
                    }
                    else
                        HeightMapColors[j * Width + i] =
                            height.AsStrideColor();
                }
            return HeightMapColors.ToTexture(Width,Height,
                GraphicsDevice, GraphicsCommandList);
        }

        public float[] GetAllHeights()
        {
            float[] allpos = new float[VertexCPUBuffer.Count];
            for (int i = 0; i < VertexCPUBuffer.Count; i++)
                allpos[i] = VertexCPUBuffer[i].Position.Y;
            return allpos;
        }
        public Color[] GetAllWeights(int which)
        {
            Color[] cols = new Color[VertexCPUBuffer.Count];
            for (int i = 0; i < VertexCPUBuffer.Count; i++)
                if(which==1)
                    cols[i] = VertexCPUBuffer[i].Color1;
            else
                if (which == 2)
                    cols[i] = VertexCPUBuffer[i].Color2;
            return cols;
        }

        public Vector3[] GetPositions()
        {
            Vector3[] allpos = new Vector3[VertexCPUBuffer.Count];
            for(int i=0;i<VertexCPUBuffer.Count;i++)
                allpos[i] = VertexCPUBuffer[i].Position;
            return allpos;
        }

      //  float minColorHeight = 0,maxColorHeight=0;
        public List<VertexTypePosTexNormColor> GenerateVertices(
            Texture wt1,Texture wt2)
        {
            Color[] Wt1ColorValues = wt1.GetColorData(Game.GraphicsContext);
            Color[] Wt2ColorValues = wt2.GetColorData(Game.GraphicsContext);
            //       HeightMapColors= Heightmap.ToTexture(GraphicsDevice,
            //           GraphicsCommandList).GetColorData(Game.GraphicsContext);
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = (Height - 1) / TerrainLOD,
                m_num_quads_x = (Width - 1) / TerrainLOD;
            Vector3 maxBounds = new Vector3(Width * m_QuadSideWidthX, 0,
                Height * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = TerrainLOD * (maxBounds.X - minBounds.X) / (Width - 1);// m_num_quads_x;
            float stepZ = TerrainLOD * (maxBounds.Z - minBounds.Z) / (Height-1);// m_num_quads_z;
            int index = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    index=z*numVertsX + x;
                    m_vertices[index].Position = new Vector3(
                        pos.X, GetHeightAt(x,z), pos.Z);
                    if (TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[index].TexCoord.X = //m_QuadSideWidthX * 
                            TEXTURE_REPEAT * x / (float)numVertsX * TerrainLOD;
                        m_vertices[index].TexCoord.Y =// m_QuadSideWidthZ * 
                            TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * TerrainLOD;
                    }
                    else //comp.TEXTURE_REPEAT == 0//make each quad have the texture
                    {
                        m_vertices[index].TexCoord.X = //m_QuadSideWidthX *
                                                       x * TerrainLOD;
                        m_vertices[index].TexCoord.Y =// m_QuadSideWidthZ * 
                            z * TerrainLOD;
                    }
                    m_vertices[index].Normal = GetNormal(x, z);
                    m_vertices[index].Tangent = GetTangent(x, z);
                    m_vertices[index].Color = new Color(R/255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    //weight textures here
                    m_vertices[index].Color1 = Wt1ColorValues[index];// new Color(.1f, 0, 0, 0.0f);// / 255.0f;
                    m_vertices[index].Color2 = Wt2ColorValues[index];//new Color(0);// / 255.0f;
                    pos.X += stepX;
                }
                pos.Z += stepZ;
            }
            Array.Clear(Wt1ColorValues);
            Array.Clear(Wt2ColorValues);

            return m_vertices.ToList();
        }

        public int[] GenerateIndices()
        {
            int m_num_quads_z = (Height - 1) / TerrainLOD,
            m_num_quads_x = (Width - 1) / TerrainLOD;
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            int count = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            int[] indices = new int[m_vertexCount * 6];
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
            return indices;
        }

        public float GetHeightAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return HeightRange.X;//no contribution for this point
            }
            float ht = 0;
            if (PerlinNoise.IsGrayScaleHeightMap)
                ht = HeightMapColors[j * Width + i].R;
            else
                ht = HeightMapColors[j * Width + i].ToFloat();
            float height = HeightRange.X +
                  (HeightRange.Y - HeightRange.X)
                  * ht / PerlinNoise.HeightMultiplier;
            return height; 
        }
        public Vector3 GetTangent(int x, int z)
        {
            var flip = 1;
            var here = new Vector3(x, GetHeightAt(x, z), z);
            var left = new Vector3(x - 1, GetHeightAt(x - 1, z), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, GetHeightAt(x + 1, z), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }
        public Vector3 GetNormal(int x, int y)
        {
            var heightL = GetHeightAt(x - 1, y);
            var heightR = GetHeightAt(x + 1, y);
            var heightD = GetHeightAt(x, y - 1);
            var heightU = GetHeightAt(x, y + 1);
            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }
        public Vector3 GetCPUTangent(int x, int z)
        {
            var flip = 1;
            var here = new Vector3(x, GetCPUHeightAt(x, z), z);
            var left = new Vector3(x - 1, GetCPUHeightAt(x - 1, z), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, GetCPUHeightAt(x + 1, z), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }
        public Vector3 GetCPUNormal(int x, int y)
        {
            var heightL = GetCPUHeightAt( x - 1, y);
            var heightR = GetCPUHeightAt(x + 1, y);
            var heightD = GetCPUHeightAt( x, y - 1);
            var heightU = GetCPUHeightAt( x, y + 1);
            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }

    }


}

//by Idomeneas
using HeightMapEditor;
using ImGui;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrainEditor
{
    /// <summary>
    /// Attach to an empty entity in your scene, fill in the required members
    /// and you're good to go. Can be used on as many terrain tiles/chunks as you want.
    /// </summary>
    public class TerrainScriptInGame : StartupScript
    {
        [DataMember(0)]
        public bool Enabled = false;

        [DataMember(10)]
        public Material Material { get; set; }

        [DataMember(20)]
        public Texture HeightmapTexture { get; set; }

        [DataMember(30)]
        public Texture BlendedTexture { get; set; }

        [DataMemberIgnore]
        public Entity TerrainModelEntity { get; set; }

        [DataMemberIgnore]
        public Color[] HeightMapColors { get; set; }

        [DataMemberIgnore]
        public int Width { get; set; } = 1024;//=> Heightmap.Size.X;
        [DataMemberIgnore]
        public int Height { get; set; } = 1024;//=> Heightmap.Size.Y;
        [DataMemberIgnore]
        public Vector2 HeightRange = new Vector2(-100, 100);
        [DataMemberIgnore]
        public float HeightScale = 1;
        [DataMemberIgnore]
        public float m_QuadSideWidthX { get; set; } = 1.0f;
        [DataMemberIgnore]
        public float m_QuadSideWidthZ { get; set; } = 1.0f;
        [DataMemberIgnore]
        public int TEXTURE_REPEAT { get; set; } = 1;
        [DataMemberIgnore]
        public int TerrainLOD { get; set; } = 1; public Vector3 WorldLocation { get; set; } = Vector3.Zero;

        public override void Start()
        {
            if (!Enabled) return;
        }

        /// <summary>
        /// creates the terrain mesh and material as it will be displayed in game
        /// </summary>
        /// <param name="tcomp"></param>
        public void BuildTerrainComponent(TerrainComponent tcomp,
             Texture wt1, Texture wt2)
        {
            Width=tcomp.Width;
            Height=tcomp.Height;
            HeightmapTexture = ImGuiSystem._loadedTextures[TerrainEditorView.TerrainHeightMapTextureIntPtr];
            HeightMapColors = HeightmapTexture.GetColorData(Game.GraphicsContext);
            SceneSystem.SceneInstance.RootScene.Entities.Remove(
                TerrainModelEntity);
            TerrainModelEntity = new Entity();
            SceneSystem.SceneInstance.RootScene.Entities.Add(
                TerrainModelEntity);
            // scr.modcomp.Enabled = true;
            TerrainEditorView.TerrainBlendedTexture = TerrainScript.GetBlendedTexture(tcomp, Game.GraphicsContext, GraphicsDevice);
            BlendedTexture = TerrainEditorView.TerrainBlendedTexture;
            m_QuadSideWidthX = tcomp.m_QuadSideWidthX;
            m_QuadSideWidthZ = tcomp.m_QuadSideWidthZ;
            TEXTURE_REPEAT = tcomp.TEXTURE_REPEAT;
            TerrainLOD = tcomp.TerrainLOD;
            HeightRange = new Vector2(
                tcomp.HeightRange.X, tcomp.HeightRange.Y);// tcomp.Heightmap.HeightRange;
            HeightScale = 1;
            bool isGrayscale = HeightmapTexture.CheckGrayScale(Game.GraphicsContext);
            meshShapevertices = new List<Vector3>();
            meshShapeindices = new List<int>();
            List<VertexTypePosTexNormColor> VertexCPUBuffer = GenerateVertices(wt1, wt2);
            var indices = GenerateIndices();
            meshShapeindices = indices.ToList();
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                GraphicsDevice, VertexCPUBuffer.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            Stride.Graphics.Buffer VertexGPUBuffer = vertexBuffer;
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
            var comp = TerrainModelEntity.GetOrCreate<ModelComponent>();
            comp.Model = model;
            var materialDescription = new MaterialDescriptor
            {
                Attributes = {
                                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                                Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor
                                { Texture = BlendedTexture})
                            }
            };
            var material = //Content.Load<Material>("Materials/TerrainHeightBased/Terrain_material");//
                        Material.New(GraphicsDevice, materialDescription);
            if (TerrainEditorView.TerrainDisplayModeSelected == 0)
            {
                model.Materials.Add(tcomp.MaterialBlendSingle);
            } else if (TerrainEditorView.TerrainDisplayModeSelected == 1)
            {
                model.Materials.Add(tcomp.MaterialBlendHeight);
            } else
            if (TerrainEditorView.TerrainDisplayModeSelected == 2)
            {
                model.Materials.Add(tcomp.MaterialBlendMulti);
            }
            if(meshShape!=null)meshShape.Dispose();
            meshShape = new //StaticMeshColliderShape(model,Services);
              StaticMeshColliderShape(meshShapevertices, meshShapeindices);
                //  model.Materials.Add(material);
            GenerateCollider();
            /*
            Heightmap =
             HeightmapTexture.ToHeightMap(Game.GraphicsContext,
             HeightRange, HeightScale, false);
            var mesh = //HeightmapTexture.ToMesh(//tcomp.TerrainMesh;
             Heightmap.ToMesh(GraphicsDevice,
                //Game as Game,
                m_QuadSideWidthX,
             m_QuadSideWidthZ, TEXTURE_REPEAT,
             TerrainLOD, WorldLocation);//,tcomp.HeightRange);
            var materialDescription = new MaterialDescriptor
            {
                Attributes = {
                                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                                Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor
                                { Texture = BlendedTexture})
                            }
            };
            var material = //Content.Load<Material>("Materials/TerrainHeightBased/Terrain_material");//
                        Material.New(GraphicsDevice, materialDescription);
            model.Meshes.Add(mesh);
            model.Materials.Add(material);
            ModelComponent modcomp = TerrainModelEntity.GetOrCreate<ModelComponent>();
            modcomp.Model = model;
            Heightmap.GenerateCollider(TerrainModelEntity);*/

        }
        ICollection<Vector3> meshShapevertices;
        ICollection<int> meshShapeindices;
        StaticMeshColliderShape meshShape;
        public void GenerateCollider()
        {
            TerrainModelEntity.RemoveAll<StaticColliderComponent>();
            int size = Width * Height;
            //the heightfield collider doesnt work if the quad lengths are not 1...
            //need to use mesh based collider
            /*    UnmanagedArray<float> Heightfield = new UnmanagedArray<float>(size);
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                    {
                        Heightfield[i+j*Width] = GetHeightAt(i, j);
                    }
                HeightfieldColliderShape meshShape = new 
                    HeightfieldColliderShape(
                    Width, Height, Heightfield, HeightScale,
                    HeightRange.X, HeightRange.Y, false);*/

            StaticColliderComponent comp = new StaticColliderComponent();
            comp.ColliderShape = meshShape;
          //  meshShape.LocalOffset = new Vector3(Width * m_QuadSideWidthX / 2,
         //       -0.01f, Height * m_QuadSideWidthZ / 2);
        //    meshShape.UpdateLocalTransformations();
            TerrainModelEntity.Add(comp);
        }

        public List<VertexTypePosTexNormColor> GenerateVertices(Texture wt1, Texture wt2)
        {
            Color[] Wt1ColorValues = wt1.GetColorData(Game.GraphicsContext);
            Color[] Wt2ColorValues = wt2.GetColorData(Game.GraphicsContext);
            //       HeightMapColors= Heightmap.ToTexture(GraphicsDevice,
            //           GraphicsCommandList).GetColorData(Game.GraphicsContext);
            Vector3 minBounds = Vector3.Zero;
            int m_num_quads_z = (Height - 1) / TerrainLOD,
                m_num_quads_x = (Width - 1) / TerrainLOD;
            Vector3 maxBounds = new Vector3((Width - 1)//m_num_quads_x
                * m_QuadSideWidthX, 0,
                (Height - 1)//m_num_quads_z
                * m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = TerrainLOD * (maxBounds.X - minBounds.X) / (Width -1);// m_num_quads_x;
            float stepZ = TerrainLOD * (maxBounds.Z - minBounds.Z) / (Height - 1);// m_num_quads_z;
            int index = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            Vector3 []vertices = new Vector3 [m_vertexCount];
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    index = z * numVertsX + x;
                    m_vertices[index].Position = new Vector3(
                        pos.X, GetHeightAt(x, z), pos.Z);
                    vertices[index] = m_vertices[index].Position;
                    if (TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[index].TexCoord.X = m_QuadSideWidthX * TEXTURE_REPEAT * x / (float)numVertsX * TerrainLOD;
                        m_vertices[index].TexCoord.Y = m_QuadSideWidthZ * TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * TerrainLOD;
                    }
                    else //if (comp.TEXTURE_REPEAT == 0)//make each quad have the texture
                    {
                        m_vertices[index].TexCoord.X = m_QuadSideWidthX * x * TerrainLOD;
                        m_vertices[index].TexCoord.Y = m_QuadSideWidthZ * z * TerrainLOD;
                    }
                    m_vertices[index].Normal = GetNormal(x, z);
                    m_vertices[index].Tangent = GetTangent(x, z);
                    m_vertices[index].Color = new Color(R / 255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    m_vertices[index].Color1 = Wt1ColorValues[index];// new Color(0.1f, 0, 0, 0.0f);// / 255.0f;
                    m_vertices[index].Color2 = Wt2ColorValues[index];//new Color(0);// / 255.0f;
                    pos.X += stepX;
                }
                pos.Z += stepZ;
            }
            Array.Clear(Wt1ColorValues);
            Array.Clear(Wt2ColorValues);
            meshShapevertices=vertices.ToList();
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

        public bool IsValidCoordinate(int x, int y)
         => x >= 0 && x < Width && y >= 0 && y < Height;
        public float GetHeightAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return 0;//no contribution for this point
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
    }
}

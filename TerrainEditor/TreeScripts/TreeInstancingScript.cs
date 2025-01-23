//by Idomeneas
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Extensions;
using System.Runtime.InteropServices;
using HeightMapEditor;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using static TerrainEditor.TreeInstancingScript;
using ImGuiNET;

namespace TerrainEditor
{

    public class TreeInstancingScript : SyncScript
    {
        public bool Enabled = true;
        public bool NeedsUpdating = true;
        public Model[] VegetationModels;
   //     public Mesh RenderedMesh;
        public override void Start()
        {
            if (!Enabled) return;
       //     InitializeInstanceBuffersPosCol(GraphicsDevice);
            CreateDeviceObjects();

        }

        private TrackingDictionary<Guid, TreeObjectInstance> objects = new TrackingDictionary<Guid, TreeObjectInstance>();
//        private FastList<Matrix> matrices = new FastList<Matrix>(TerrainEditorView.MaxTreeInstances);
 //       private FastList<Color4> colors = new FastList<Color4>(TerrainEditorView.MaxTreeInstances);

        public override void Update()
        {
            if (NeedsUpdating)
            {
                NeedsUpdating = false;
                UpdateInstanceData();
            }
            Draw();
        }
        public Buffer<Vector3> InstanceLocations;
        public Buffer<Vector4> InstanceColors;

        private void UpdateInstanceData()
        {
            Vector4[] instancescol = new Vector4[TerrainEditorView.MaxTreeInstances];
            Vector3[] instancespos = new Vector3[TerrainEditorView.MaxTreeInstances];
            for (int i = 0; i < TerrainEditorView.MaxTreeInstances; i++)
            {
                instancespos[i] = new Vector3(Utility.Runif(-120, 120),
                            Utility.Runif(-120, 120), Utility.Runif(-120, 120));
                instancescol[i] = new Vector4(Utility.Runif(), 
                    Utility.Runif(),Utility.Runif(), Utility.Runif());
            }
            InstanceLocations = Stride.Graphics.Buffer.Structured.New<Vector3>(
     GraphicsDevice, instancespos);
            InstanceColors = Stride.Graphics.Buffer.Structured.New<Vector4>(
                 GraphicsDevice, instancescol);
        }

        public System.Guid AddInstance(TreeObjectInstance someObject)
        {
            objects[someObject.Id] = someObject;
            return someObject.Id;
        }

        public bool RemoveInstance(TreeObjectInstance someObject)
        {
            return objects.Remove(someObject.Id);
        }

        private void Draw()
        {
            var commandList = Game.GraphicsContext.CommandList;
            commandList.SetPipelineState(pipelineState);
            var meshDraw = VegetationModels[0].Meshes[0].Draw;// RenderedMesh.Draw;

            for (int i = 0; i < meshDraw.VertexBuffers.Length; i++)
            {
                var vertexBufferBinding = meshDraw.VertexBuffers[i];
                commandList.SetVertexBuffer(i, vertexBufferBinding.Buffer, meshDraw.StartLocation, vertexBufferBinding.Declaration.VertexStride);
            }

            var indexBuffer = meshDraw.IndexBuffer.Buffer;
            commandList.SetIndexBuffer(indexBuffer, 0, meshDraw.IndexBuffer.Is32Bit);

            Shader.UpdateEffect(GraphicsDevice);
            Shader.Parameters.Set(TreeInstancingShaderKeys.InstanceLocations, InstanceLocations);
            Shader.Parameters.Set(TreeInstancingShaderKeys.InstanceColors, InstanceColors);
             
            Shader.Apply(Game.GraphicsContext);

            // Render the models.
            if (meshDraw.IndexBuffer != null)
            {
                commandList.DrawIndexedInstanced(indexBuffer.ElementCount, objects.Count);
            }
            else
            {
                commandList.DrawInstanced(indexBuffer.ElementCount, objects.Count);
            }
        
        }

        private PipelineState pipelineState;
        private EffectInstance Shader;
        
        private void CreateDeviceObjects()
        {
            var commandList = Game.GraphicsContext.CommandList;
            var shader = new EffectInstance(EffectSystem.LoadEffect("CubeInstancingShader").WaitForResult());
            shader.UpdateEffect(GraphicsDevice);
            Shader = shader;

            var outputDesc = new RenderOutputDescription(GraphicsDevice.Presenter.BackBuffer.Format);
            outputDesc.CaptureState(commandList);

            var pipeline = new PipelineStateDescription()
            {
                /* TODO: do we need all these? */
                BlendState = BlendStates.Default,
                RasterizerState = RasterizerStateDescription.Default,
                DepthStencilState = DepthStencilStates.None,
                Output = outputDesc,
                PrimitiveType = PrimitiveType.TriangleList,
                InputElements = VertexPositionNormalTexture.Layout.CreateInputElements(),
                EffectBytecode = shader.Effect.Bytecode,
                RootSignature = shader.RootSignature,
            };

            var newPipelineState = PipelineState.New(GraphicsDevice, ref pipeline);
            pipelineState = newPipelineState;
 
        }

        /// <summary>
        /// Describes a custom vertex format structure that contains position and color information. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VertexPositionNormalTextureColor : IEquatable<VertexPositionNormalTextureColor>, IVertex
        {
            /// <summary>
            /// Initializes a new <see cref="VertexPositionNormalTextureColor"/> instance.
            /// </summary>
            /// <param name="position">The position of this vertex.</param>
            /// <param name="color">The color of this vertex.</param>
            /// <param name="textureCoordinate">UV texture coordinates.</param>
            public VertexPositionNormalTextureColor(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Color4 color)
                : this()
            {
                Position = position;
                Normal = normal;
                TextureCoordinate = textureCoordinate;
                Color = color;
            }

            /// <summary>
            /// XYZ position.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// XYZ position.
            /// </summary>
            public Vector3 Normal;

            /// <summary>
            /// UV texture coordinates.
            /// </summary>
            public Vector2 TextureCoordinate;

            /// <summary>
            /// The vertex color.
            /// </summary>
            public Color4 Color;


            /// <summary>
            /// Defines structure byte size.
            /// </summary>
            public static readonly int Size = 48;

            /// <summary>
            /// The vertex layout of this struct.
            /// </summary>
            public static readonly VertexDeclaration Layout = new VertexDeclaration(
                VertexElement.Position<Vector3>(),
                VertexElement.Normal<Vector3>(),
                VertexElement.TextureCoordinate<Vector2>(),
                VertexElement.Color<Color4>());


            public bool Equals(VertexPositionNormalTextureColor other)
            {
                return Position.Equals(other.Position) && Normal.Equals(other.Normal) && Color.Equals(other.Color) && TextureCoordinate.Equals(other.TextureCoordinate);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is VertexPositionNormalTextureColor && Equals((VertexPositionNormalTextureColor)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Position.GetHashCode();
                    hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                    hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                    hashCode = (hashCode * 397) ^ Color.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(VertexPositionNormalTextureColor left, VertexPositionNormalTextureColor right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VertexPositionNormalTextureColor left, VertexPositionNormalTextureColor right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return string.Format("Position: {0}, Normal {1} Color: {2}, Texcoord: {3}", Position, Normal, TextureCoordinate, Color);
            }

            public VertexDeclaration GetLayout()
            {
                return Layout;
            }

            public void FlipWinding()
            {
                TextureCoordinate.X = (1.0f - TextureCoordinate.X);
            }
        }

    }

    public class TreeObjectInstance : SyncScript
    {
        /*       [DataMember(70)]
               public List<VegetationInstance>? VegetationInstances { get; set; } = new List<VegetationInstance>();

               [DataContract]
               public struct VegetationInstance
               {
                   /// <summary>
                   /// Index that points to the vegetation model we display for this instance.
                   /// Value in 1-MaxTreeTypes
                   /// </summary>
                   public int VegModelType { get; set; }
                   public Vector3 VegPosition { get; set; }
                   public Vector3 VegScale { get; set; }
                   public Vector3 VegRotation { get; set; }

                   public VegetationInstance(int VegModelType1, Vector3 VegPosition1,
                       Vector3 VegScale1, Vector3 VegRotation1)
                   {
                       VegModelType = VegModelType1;
                       VegPosition = VegPosition1;
                       VegScale = VegScale1;
                       VegRotation = VegRotation1;
                   }
               }
        */

        public int InstanceType;
        public Vector3 InstancePosition;
        public Vector3 InstanceScale;
        public Vector3 InstanceRotation;
        public Vector3 Velocity;
        public Vector3 RotVelocity;
        public Color4 InstanceColor;
        public Model InstanceModel;
        public bool ObjectUseInstancing = true;

        public override void Start()
        {
            if(!ObjectUseInstancing && InstanceModel != null)
            Entity.GetOrCreate<ModelComponent>().Model = InstanceModel;
        }

        public override void Update()
        {

            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (Entity.Transform.Position.X > 64.0f || Entity.Transform.Position.X < -64.0f)
            {
                Velocity.X = -Velocity.X;
            }

            if (Entity.Transform.Position.Y > 64.0f || Entity.Transform.Position.Y < -64.0f)
            {
                Velocity.Y = -Velocity.Y;
            }

            if (Entity.Transform.Position.Z > 64.0f || Entity.Transform.Position.Z < -64.0f)
            {
                Velocity.Z = -Velocity.Z;
            }

            Entity.Transform.Rotation *=
                Quaternion.RotationX(RotVelocity.X * dt) *
                Quaternion.RotationY(RotVelocity.Y * dt) *
                Quaternion.RotationZ(RotVelocity.Z * dt);

            Entity.Transform.Position += Velocity * dt;

        }

    }

    public class InstancedTrees : StartupScript
    {
        public bool Enabled = true;

        public bool UseInstancing = true;

        public int InstanceCount { get; private set; }

        /// <summary>
        /// Maximum number of vegetation models loaded 
        /// </summary>
        public int MaxTreeTypes = 3;
 /*
        public Vector3[] InstanceLocations;

        public Vector4[] InstanceColors;

        public int[] InstanceType;

        public Vector3[] InstanceScale;

        public Vector3[] InstanceRotation;*/

        /* GPU side data */
        private TreeInstancingScript TreeInstancesScript;
        public Model VegetationModel1 { get; set; }
        public Model VegetationModel2 { get; set; }
        public Model VegetationModel3 { get; set; }

        //        [Display("Material")]
        //       public Material meshMaterial;

        public override void Start()
        {
            if (!Enabled) return;

            if (UseInstancing)
            {
                var ourPrimitive = GeometricPrimitive.Cube.New(GraphicsDevice, 1.0f);
                var primitiveMeshDraw = ourPrimitive.ToMeshDraw();

                TreeInstancesScript = new TreeInstancingScript()
                {
                    VegetationModels = new Model[]
                    {
                        VegetationModel1,VegetationModel2,VegetationModel3
                    }
                };
            }

            InstanceCount = TerrainEditorView.MaxTreeInstances;
            var random = new Random();
            for (int i = 0; i < InstanceCount; ++i)
            {
                //var randX = random.Next(-164, 164);
                //var randY = random.Next(-164, 164);
                // var randZ = random.Next(-164, 164);
                var velX = random.NextDouble() * 4.0;
                var velY = random.NextDouble() * 4.0;
                var velZ = random.NextDouble() * 4.0;
                var vel = new Vector3((float)velX, (float)velY, (float)velZ);
                var rotVelX = random.NextDouble();
                var rotVelY = random.NextDouble();
                var rotVelZ = random.NextDouble();
                var rotVel = new Vector3((float)rotVelX, (float)rotVelY, (float)rotVelZ);

                var r = random.NextDouble();
                var g = random.NextDouble();
                var b = random.NextDouble();
                var col = new Color4((float)r, (float)g, (float)b);

                int type = Utility.DUnif(1, MaxTreeTypes);
                Model model = new Model();
                if (type == 1)
                    model = VegetationModel1;
                else if (type == 2)
                    model = VegetationModel2;
                else if (type == 3)
                    model = VegetationModel3;

                var newEntity = new Entity();
                var newTreeObjectInstance = new TreeObjectInstance()
                {
                    Velocity = vel,
                    RotVelocity = rotVel,
                    InstanceColor = col,
                    InstanceScale = new Vector3(Utility.Runif(0.9f, 1.1f),
                        Utility.Runif(0.9f, 1.1f), Utility.Runif(0.9f, 1.1f)),
                    InstanceRotation = Vector3.Zero,
                    InstancePosition = new Vector3(Utility.Runif(-50, 50),
                        Utility.Runif(-50, 50), Utility.Runif(-50, 50)),
                    InstanceType = type,
                    InstanceModel = model,
                    ObjectUseInstancing = UseInstancing
                };

                newEntity.Add(newTreeObjectInstance);

                if (UseInstancing)
                    TreeInstancesScript.AddInstance(newTreeObjectInstance);
                Entity.AddChild(newEntity);
                // SceneSystem.SceneInstance.RootScene.Entities.Add(newEntity);
            }
            if (UseInstancing)
            {
                Entity.Add(TreeInstancesScript);
            }
        }

  /*      private bool InitializeInstanceBuffersPosCol(GraphicsDevice GraphicsDevice)
        {
            try
            {
                TerrainComponent tcomp = TerrainScript.Terrain_Entity.GetOrCreate<TerrainComponent>();
                // Set the number of instances in the array.
                List<Stride.Core.Mathematics.Int2> locs = Utility.GetTreeLocations(
                    TerrainEditorView.TerrainPropertiesTreeLocs.Width,
                    TerrainEditorView.TerrainPropertiesTreeLocs.Height,
                    TerrainEditorView.MaxTreeInstances, TerrainEditorView.Repulsion_Distance);
                InstanceCount = locs.Count;
                TerrainEditorView.CurrentTreeInstances = InstanceCount;// tcomp.Heightmap.Size.X * tcomp.Heightmap.Size.Y;// 1024 *1024;
                if (InstanceCount <= 0) return false;
                //            Vector3[] instancespos = tcomp.Heightmap.ToWorldPoints(
                //          tcomp.m_QuadSideWidthX, tcomp.m_QuadSideWidthZ);// new Vector3[InstanceCount];
                InstanceColors = new Vector4[InstanceCount];
                InstanceType = new int[InstanceCount];
                InstanceLocations = new Vector3[InstanceCount];
                InstanceScale = new Vector3[InstanceCount];
                InstanceRotation = new Vector3[InstanceCount];
                for (int i = 0; i < InstanceCount; i++)
                {
                    InstanceScale[i] = new Vector3(Utility.Runif(0.9f, 1.1f),
                       Utility.Runif(0.9f, 1.1f), Utility.Runif(0.9f, 1.1f));
                    InstanceRotation[i] = Vector3.Zero;
                    InstanceLocations[i] = new Vector3(Utility.Runif(-150, 150),
                       Utility.Runif(-150, 150), Utility.Runif(-150, 150));
                    InstanceColors[i] = new Vector4(1, 0, 0, 1);// Utility.Runif(), Utility.Runif(),Utility.Runif(), Utility.Runif());
                    InstanceType[i] = Utility.DUnif(1, MaxTreeTypes);// Utility.Runif(), Utility.Runif(),Utility.Runif(), Utility.Runif());
                }
                /*   InstanceLocations = Stride.Graphics.Buffer.Structured.New<Vector3>(
                        GraphicsDevice, instancespos);
                   InstanceColors = Stride.Graphics.Buffer.Structured.New<Vector4>(
                        GraphicsDevice, instancescol);
                   InstanceType = Stride.Graphics.Buffer.Structured.New<int>(
                        GraphicsDevice, instancesType);
                   InstanceScale = Stride.Graphics.Buffer.Structured.New<Vector3>(
                        GraphicsDevice, instancesScale);
                   InstanceRotation = Stride.Graphics.Buffer.Structured.New<Vector3>(
                        GraphicsDevice, instancesRotation);
                return true;
            }
            catch
            {
                return false;
            }

        }
*/
    }

}

//altered by Idomeneas, Original by tebjan,https://github.com/tebjan/StrideTransformationInstancing

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
using System;

namespace TerrainEditor
{
    public class InstancingviaUserArray : InstanceUpdaterBase
    {
        [DataMember(0)]
        public Model Vegetation1 { get; set; }
        [DataMember(1)]
        public Model Vegetation2 { get; set; }
        [DataMember(2)]
        public Model Vegetation3 { get; set; }

        protected override int InstanceCountSqrt => 10;

        InstancingUserArray instancingUserArray;

        protected override IInstancing GetInstancingType()
        {
            instancingUserArray = new InstancingUserArray();
            return instancingUserArray;
        }

        protected override void ManageInstancingData()
        {
            var transformUsage = (ModelTransformUsage)(((int)Game.UpdateTime.Total.TotalSeconds) % 3);
            instancingUserArray.ModelTransformUsage = ModelTransformUsage.PostMultiply;
            instancingUserArray.UpdateWorldMatrices(instanceWorldTransformations);
        }

    }

    public abstract class InstanceUpdaterBase : SyncScript
    {
        [DataMember(0)]
        public bool Enabled = true;

//        public Model modelcomp;
        //     private ModelComponent ModelComponent=new ModelComponent();
        //    private MeshDraw Model_MeshDraw;
        protected abstract int InstanceCountSqrt { get; }
        protected Matrix[] instanceWorldTransformations;
        public override void Start()
        {
            if (!Enabled) return;
            /*  Model_MeshDraw = GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw();
              Mesh Model_Mesh = new Mesh { Draw = Model_MeshDraw };
              Model model = new Model();
              model.Meshes.Add(Model_Mesh);
              ModelComponent.Model = model;
              Entity.Add(ModelComponent);*/
            
            var ic = InstanceCountSqrt * InstanceCountSqrt;
            var instancingComponent = Entity.GetOrCreate<InstancingComponent>();
            instancingComponent.Type = GetInstancingType();
            instanceWorldTransformations = new Matrix[ic];
            UpdateMatrices();
            ManageInstancingData();
            Entity.Transform.Scale = new Vector3(5.1f);

            //  InitializeInstanceBuffersPosCol(GraphicsDevice);
        }
        protected abstract IInstancing GetInstancingType();

        public override void Update()
        {
            //UpdateMatrices();

           // Entity.Transform.Scale = new Vector3(0.1f);

          //  ManageInstancingData();
        }

        void UpdateMatrices()
        {            // generate some matrices
            var offset = InstanceCountSqrt / 2;
            var seconds = (float)Game.UpdateTime.Total.TotalSeconds;
            for (int i = 0; i < InstanceCountSqrt; i++)
            {
                var col = i * InstanceCountSqrt;
                for (int j = 0; j < InstanceCountSqrt; j++)
                {
                    var x = i * 2 - offset;
                    var y = j * 2 - offset;
                    var z = (float)Math.Cos(new Vector2(x, y).Length() * 0.5f + seconds);

                    instanceWorldTransformations[col + j] = Matrix.RotationY(seconds) *
                        Matrix.Translation(x, y + 50, z);
                }
            }
        }

        protected abstract void ManageInstancingData();

        /*      [StructLayout(LayoutKind.Sequential)]
              public struct DVertexType
              {
                  public Vector3 position;
                  public Vector2 texture;
              };
              [StructLayout(LayoutKind.Sequential)]
              public struct DInstanceType
              {
                  public Vector3 position;
              };

              public int VertexCount { get; set; }
              public int InstanceCount { get; private set; }
              public Buffer VertexBuffer { get; set; }
              public Buffer InstanceBuffer { get; set; }

              public bool InitializeInstanceBuffersPosCol(GraphicsDevice GraphicsDevice)
              {
                  try
                  {
                      // Set number of vertices in the vertex array.
                      VertexCount = 3;

                      // Create the vertex array and load it with data.
                      DVertexType[] vertices1 = new[]
                      {
                          // Bottom left.
                          new DVertexType()
                          {
                              position = new Vector3(-1, -1, 0),
                              texture = new Vector2(0, 1)
                          },
                          // Top middle.
                          new DVertexType()
                          {
                              position = new Vector3(0, 1, 0),
                              texture = new Vector2(.5f, 0)
                          },
                          // Bottom right.
                          new DVertexType()
                          {
                              position = new Vector3(1, -1, 0),
                              texture = new Vector2(1, 1)
                          }
                      };

                      // Set the number of instances in the array.
                      InstanceCount = 40;

                      DInstanceType[] instances1 = new DInstanceType[]
                      {
                          new DInstanceType()
                          {
                              position = new Vector3(-10.5f, -10.5f, 5.0f)
                          },
                          new DInstanceType()
                          {
                              position = new Vector3(-10.5f,  10.5f, 5.0f)
                          },
                          new DInstanceType()
                          {
                              position = new Vector3( 10.5f, -10.5f, 5.0f)
                          },
                          new DInstanceType()
                          {
                              position = new Vector3( 10.5f,  10.5f, 5.0f)
                          }
                      };
                      VertexPositionColor[] instances = new VertexPositionColor[InstanceCount];
                      for(int i=0;i<instances.Length;i++)
                      {
                          instances[i].Position = new Vector3(Utility.Runif(-50,50));
                          instances[i].Color = new Vector4(Utility.Runif(), Utility.Runif(),
                              Utility.Runif(), Utility.Runif());
                      }
                      // Create the vertex buffer.
                      MeshDraw drawData = Model_MeshDraw;
                      VertexPositionTexture[] vertices = new 
                          VertexPositionTexture[Model_MeshDraw.DrawCount];
                      VertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice,
                            vertices);

                      // Create the Instance instead of an Index Buffer buffer.
                      InstanceBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, instances);

                      return true;
                  }
                  catch
                  {
                      return false;
                  }

              }*/

    }

}

//by Idomeneas
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;

namespace TerrainEditor
{
    public class CubeInstancingRenderFeature : RootRenderFeature
    {
        DynamicEffectInstance shader;
        MutablePipelineState pipelineState;

        public override Type SupportedRenderObjectType => typeof(RenderMesh);

        public CubeInstancingRenderFeature()
        {
            //pre adjust render priority, low numer is early, high number is late
            SortKey = 255;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // initialize shader
            shader = new DynamicEffectInstance("CubeInstancingShader");
            shader.Initialize(Context.Services);

            // create the pipeline state and set properties that won't change
            pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            pipelineState.State.SetDefaults();
           /* InputElementDescription[] InputElementDescr = new InputElementDescription[]
     {
                              new InputElementDescription ()
                              {
                                  SemanticName = "POSITION",
                                  SemanticIndex = 0,
                                  Format = PixelFormat.R32G32B32_Float,
                                  InputSlot = 0,
                                  AlignedByteOffset = 0,
                                  InputSlotClass  = InputClassification.Vertex,
                                  InstanceDataStepRate = 0
                              },
                              new InputElementDescription ()
                              {
                                  SemanticName = "TEXCOORD",
                                  SemanticIndex = 0,
                                  Format = PixelFormat.R32G32_Float,
                                  InputSlot = 0,
                                  AlignedByteOffset = -1,
                                  InputSlotClass  = InputClassification.Vertex,
                                  InstanceDataStepRate = 0
                              },
                              new InputElementDescription ()
                              {
                                  SemanticName = "InstancePos",
                                  SemanticIndex = 0,
                                  Format = PixelFormat.R32G32B32_Float,
                                  InputSlot = 1,
                                  AlignedByteOffset = 0,
                                  InputSlotClass  = InputClassification.Instance,
                                  InstanceDataStepRate = 1
                              },
                             new InputElementDescription ()
                              {
                                  SemanticName = "InstanceCol",
                                  SemanticIndex = 0,
                                  Format = PixelFormat.R32G32B32A32_Float,
                                  InputSlot = 1,
                                  AlignedByteOffset = -1,
                                  InputSlotClass  = InputClassification.Instance,
                                  InstanceDataStepRate = 1
                              }
     };*/
            pipelineState.State.InputElements = VertexPositionColor.Layout.CreateInputElements();
            ///InputElementDescr;
            pipelineState.State.BlendState = BlendStates.Opaque;
            pipelineState.State.RasterizerState.CullMode = CullMode.Back;
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
            shader.UpdateEffect(context.GraphicsDevice);

            foreach (var renderNode in renderViewStage.SortedRenderNodes)
            {
                var renderMesh = renderNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                {
                    continue;
                }

                // get TerrainRenderScript script
                CubeInstancingRenderScript CubeInstancingRenderScript = null;
                if (renderMesh.Source is ModelComponent)
                {
                    CubeInstancingRenderScript = (renderMesh.Source as ModelComponent).Entity.Get<CubeInstancingRenderScript>();
                }

                //CubeInstancingRenderScript = null;

                if (CubeInstancingRenderScript == null || !CubeInstancingRenderScript.Enabled)
                {
                    continue;
                }

                MeshDraw drawData = renderMesh.ActiveMeshDraw;
                // bind VB
               for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
                {
                    var vertexBuffer = drawData.VertexBuffers[slot];
                    context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
                }
                // Set the vertex buffer to active in the input assembler so it can be rendered.
                //context.CommandList.SetVertexBuffer(0,
                //   CubeInstancingRenderScript.VertexBuffer,// drawData.VertexBuffers[0].Buffer,
                //  0, 0);
                // context.CommandList.SetVertexBuffer(0,
                //     CubeInstancingRenderScript.VertexBuffer,0,0);
                //sets per instance data
                //   context.CommandList.SetVertexBuffer(1,
                //       CubeInstancingRenderScript.InstanceBuffer,0, 0);
                // set shader parameters
     //           shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * renderView.ViewProjection); // matrix
                shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderView.ViewProjection); // matrix
                                                                                                                             // shader.Parameters.Set(CubeInstancingShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
                shader.Parameters.Set(TexturingKeys.Sampler, context.GraphicsDevice.SamplerStates.LinearWrap);
            //    shader.Parameters.Set(CubeInstancingShaderKeys.shaderTexture, TerrainScript.GetTerrainTexture(1));
                shader.Parameters.Set(CubeInstancingShaderKeys.InstanceLocations, CubeInstancingRenderScript.InstanceLocations);
                shader.Parameters.Set(CubeInstancingShaderKeys.InstanceColors, CubeInstancingRenderScript.InstanceColors);
                //shader.Parameters.Set(CubeInstancingShaderKeys.Phase, DateTime.UtcNow.Millisecond);
                
                // prepare pipeline state
                pipelineState.State.RootSignature = shader.RootSignature;
                pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
                pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;// drawData.PrimitiveType;

                pipelineState.State.Output.CaptureState(context.CommandList);
                pipelineState.Update();

                context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
                context.CommandList.SetPipelineState(pipelineState.CurrentState);

              //  context.CommandList.SetStreamTargets(CubeInstancingRenderScript.streamOutBufferBinding.Buffer);

                // apply the effect
                shader.Apply(context.GraphicsContext);

                // Render the models.
                if (drawData.IndexBuffer != null)
                {
                    //context.CommandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                    context.CommandList.DrawIndexedInstanced(drawData.DrawCount,
                 CubeInstancingRenderScript.InstanceCount);
                }
                else
                {
                    context.CommandList.DrawInstanced(drawData.DrawCount,
                      CubeInstancingRenderScript.InstanceCount);
                   // context.CommandList.Draw(drawData.DrawCount, 
                    //    drawData.StartLocation);
                }
              //  context.CommandList.SetStreamTargets(null);

            }
        }
    }
}

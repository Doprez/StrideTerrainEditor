//by Idomeneas
using HeightMapEditor;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using static TerrainEditor.TreeInstancingRenderScript;

namespace TerrainEditor
{
    public class TreeInstancingRenderFeature : RootRenderFeature
    {
        DynamicEffectInstance shader;
        MutablePipelineState pipelineState;

        public override Type SupportedRenderObjectType => typeof(RenderMesh);

        public TreeInstancingRenderFeature()
        {
            //pre adjust render priority, low numer is early, high number is late
            SortKey = 255;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // initialize shader
            shader = new DynamicEffectInstance("TreeInstancingShader");
            shader.Initialize(Context.Services);

            // create the pipeline state and set properties that won't change
            pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            pipelineState.State.SetDefaults();
            pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
            ///InputElementDescr;
            pipelineState.State.BlendState = BlendStates.Opaque;
            pipelineState.State.RasterizerState.CullMode = CullMode.Back;
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);
        }
        float ScaleAdjust = 0.001f;
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

                TreeInstancingRenderScript TreeInstancingRenderScript = null;
                if (renderMesh.Source is ModelComponent)
                {
                    TreeInstancingRenderScript = (renderMesh.Source as ModelComponent).Entity.Get<TreeInstancingRenderScript>();
                }

                //TreeInstancingRenderScript = null;
                
                if (TreeInstancingRenderScript == null || !TreeInstancingRenderScript.Enabled)
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
                // set shader parameters
                shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * renderView.ViewProjection); // matrix
                shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(ScaleAdjust + 1.0f)); // increase size to avoid z-fight
            //    shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
    //            shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World *
       //                               renderView.ViewProjection); // matrix
                                                                  // shader.Parameters.Set(CubeInstancingShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
         //      shader.Parameters.Set(TexturingKeys.Sampler, context.GraphicsDevice.SamplerStates.LinearWrap);
               // Texture tex1 = TreeInstancingRenderScript.VegetationModel2.Materials[0].Material.
               //                     Passes[0].Parameters.Get<Texture>(MaterialKeys.DiffuseMap);
              //  shader.Parameters.Set(MaterialKeys.DiffuseMap, tex1);
                shader.Parameters.Set(TreeInstancingShaderKeys.InstanceLocations, TreeInstancingRenderScript.InstanceLocations);
                shader.Parameters.Set(TreeInstancingShaderKeys.InstanceColors, TreeInstancingRenderScript.InstanceColors);
                //shader.Parameters.Set(CubeInstancingShaderKeys.Phase, DateTime.UtcNow.Millisecond);
                
                // prepare pipeline state
                pipelineState.State.RootSignature = shader.RootSignature;
                pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
                pipelineState.State.PrimitiveType = drawData.PrimitiveType;

                pipelineState.State.Output.CaptureState(context.CommandList);
                pipelineState.Update();

                context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
                context.CommandList.SetPipelineState(pipelineState.CurrentState);

              //  context.CommandList.SetStreamTargets(TreeInstancingRenderScript.streamOutBufferBinding.Buffer);

                // apply the effect
                shader.Apply(context.GraphicsContext);

                // Render the models.
                if (drawData.IndexBuffer != null)
                {
                    //context.CommandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                    context.CommandList.DrawIndexedInstanced(drawData.DrawCount,
                 TreeInstancingRenderScript.InstanceCount);
                }
                else
                {
                    context.CommandList.DrawInstanced(drawData.DrawCount,
                      TreeInstancingRenderScript.InstanceCount);
                   // context.CommandList.Draw(drawData.DrawCount, 
                    //    drawData.StartLocation);
                }
              //  context.CommandList.SetStreamTargets(null);

            }
        }
    }
}

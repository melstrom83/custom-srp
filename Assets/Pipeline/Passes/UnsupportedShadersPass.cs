using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace Graphics
{   public class UnsupportedShadersPass
    {
    #if UNITY_EDITOR
        static ShaderTagId[] shaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        private static Material errorMaterial;
        static readonly ProfilingSampler sampler = new("Unsupported Shaders");
        RendererListHandle list;

        void Render(RenderGraphContext context) 
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }
    #endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults)
        {
    #if UNITY_EDITOR
            using var builder = renderGraph.AddRenderPass(sampler.name, out UnsupportedShadersPass pass,sampler);
            
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    overrideMaterial = errorMaterial,
                    renderQueueRange = RenderQueueRange.all
                }));

            //pass.renderer = renderer;
            builder.SetRenderFunc<UnsupportedShadersPass>(static (pass, context) => pass.Render(context));
    #endif
        }
    }
}

using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public class GizmosPass
    {
    #if UNITY_EDITOR
        static readonly ProfilingSampler sampler = new("Gizmos");

        bool requiresDepthCopy;
        CameraRendererCopier copier;
        TextureHandle depthAttachment;

        void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;

            if(requiresDepthCopy)
            {
                copier.CopyByDrawing(buffer, depthAttachment, BuiltinRenderTextureType.CameraTarget, true);
                context.renderContext.ExecuteCommandBuffer(buffer);
                buffer.Clear();
            }
            context.renderContext.DrawGizmos(copier.Camera, GizmoSubset.PreImageEffects);
            context.renderContext.DrawGizmos(copier.Camera, GizmoSubset.PostImageEffects);
        }
    #endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(
            RenderGraph renderGraph, 
            bool useIntermediateBuffer,
            CameraRendererCopier copier,
            in CameraRendererTextures textures)
        {
    #if UNITY_EDITOR
            if(Handles.ShouldRenderGizmos())
            {
                using var builder = renderGraph.AddRenderPass(sampler.name, out GizmosPass pass, sampler);
                pass.requiresDepthCopy = useIntermediateBuffer;
                pass.copier = copier;
                if(useIntermediateBuffer)
                {
                    pass.depthAttachment = builder.ReadTexture(textures.depthAttachment);
                }
                builder.SetRenderFunc<GizmosPass>(static (pass, context) => pass.Render(context));
            }
    #endif
        }
    }
}
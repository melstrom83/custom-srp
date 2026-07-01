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

        CameraRenderer renderer;

        void Render(RenderGraphContext context)
        {
            if (renderer.useIntermediateBuffer)
            {
                renderer.Draw(CameraRenderer.depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                renderer.ExecuteBuffer();
            }
            context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PreImageEffects);
            context.renderContext.DrawGizmos(renderer.camera, GizmoSubset.PostImageEffects);
        }
    #endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
    #if UNITY_EDITOR
            if(Handles.ShouldRenderGizmos())
            {
                using var builder = renderGraph.AddRenderPass(sampler.name, out GizmosPass pass, sampler);
                pass.renderer = renderer;
                builder.SetRenderFunc<GizmosPass>((pass, context) => pass.Render(context));
            }
    #endif
        }
    }
}
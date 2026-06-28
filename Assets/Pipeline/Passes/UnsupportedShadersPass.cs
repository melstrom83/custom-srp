using System.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{    public class UnsupportedShadersPass
    {
    #if UNITY_EDITOR
        CameraRenderer renderer;

        void Render(RenderGraphContext context) => renderer.DrawUnsupportedShaders();
    #endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
    #if UNITY_EDITOR
            using var builder = renderGraph.AddRenderPass("Unsupported Shaders", out UnsupportedShadersPass pass);
            pass.renderer = renderer;
            builder.SetRenderFunc<UnsupportedShadersPass>((pass, context) => pass.Render(context));
    #endif
        }
    }
}

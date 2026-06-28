using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class SetupPass
    {
        CameraRenderer renderer;

        void Render(RenderGraphContext context) => renderer.Setup();
        
        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using var builder = renderGraph.AddRenderPass("Setup", out SetupPass pass);
            pass.renderer = renderer;
            builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));
        }
    }
}
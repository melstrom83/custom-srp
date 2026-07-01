using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class FinalPass
    {
        static readonly ProfilingSampler sampler = new("Final");
        CameraRenderer renderer;

        CameraSettings.FinalBlendMode finalBlendMode;

        void Render(RenderGraphContext context)
        {
            renderer.Draw(CameraRenderer.colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            renderer.ExecuteBuffer();
        }

        public static void Record(
            RenderGraph renderGraph,
            CameraRenderer renderer,
            CameraSettings.FinalBlendMode finalBlendMode)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out FinalPass pass, sampler);
            pass.renderer = renderer;
            pass.finalBlendMode = finalBlendMode;
            builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
        }
    }
}
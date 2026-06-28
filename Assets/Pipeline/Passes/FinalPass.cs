using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class FinalPass
    {
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
            using var builder = renderGraph.AddRenderPass("Final", out FinalPass pass);
            pass.renderer = renderer;
            pass.finalBlendMode = finalBlendMode;
            builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
        }
    }
}
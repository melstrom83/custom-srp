using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class FinalPass
    {
        static readonly ProfilingSampler sampler = new("Final");
        CameraRendererCopier copier;
        TextureHandle colorAttachment;

        void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;
            copier.CopyToCameraTarget(buffer, colorAttachment);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(
            RenderGraph renderGraph,
            CameraRendererCopier copier,
            in CameraRendererTextures textures)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out FinalPass pass, sampler);
            pass.copier = copier;
            pass.colorAttachment = builder.ReadTexture(textures.colorAttachment);
            builder.SetRenderFunc<FinalPass>((pass, context) => pass.Render(context));
        }
    }
}
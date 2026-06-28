using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class VisibleGeometryPass
    {
        CameraRenderer renderer;

        bool useDynamicBatching, useGPUInstancing;

        void Render(RenderGraphContext context) => renderer.DrawVisibleGeometry(
            useDynamicBatching, useGPUInstancing);

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer,
            bool useDynamicBatching, bool useGPUInstancing)
        {
            using var builder = renderGraph.AddRenderPass("Visible Geometry", out VisibleGeometryPass pass);
            pass.renderer = renderer;
            pass.useDynamicBatching = useDynamicBatching;
            pass.useGPUInstancing = useGPUInstancing;
            builder.SetRenderFunc<VisibleGeometryPass>((pass, context) => pass.Render(context));
        }
    }
}
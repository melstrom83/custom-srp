using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Graphics
{
    public class LightingPass
    {
        static readonly ProfilingSampler sampler = new("Lighting");
        Lighting lighting;
        CullingResults cullingResults;
        ShadowSettings shadowSettings;
        bool useLightPerObject;
        int renderingLayerMask;

        void Render(RenderGraphContext context) => lighting.Setup(
            context, cullingResults, shadowSettings);

        public static void Record(RenderGraph renderGraph, Lighting lighting,
            CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out LightingPass pass, sampler);
            pass.lighting = lighting;
            pass.cullingResults = cullingResults;
            pass.shadowSettings = shadowSettings;
            builder.SetRenderFunc<LightingPass>((pass, context) => pass.Render(context));
        }
    }
}
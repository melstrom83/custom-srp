using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace Graphics
{   public class GeometryPass
    {
        static readonly ProfilingSampler samplerOpaque = new("Opaque Geometry");
        static readonly ProfilingSampler samplerTransparent = new ("Transparent Geometry");

        static ShaderTagId[] shaderTagIds =
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("CustomLit")
        };

        RendererListHandle list;

        void Render(RenderGraphContext context) 
        {
            context.cmd.DrawRendererList(list);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        }

        public static void Record(RenderGraph renderGraph, 
            Camera camera, CullingResults cullingResults, bool opaque)
        {
            var sampler = opaque ? samplerOpaque : samplerTransparent;
            using var builder = renderGraph.AddRenderPass(sampler.name, out GeometryPass pass, sampler);

            pass.list = builder.UseRendererList(renderGraph.CreateRendererList(
                new RendererListDesc(shaderTagIds, cullingResults, camera)
                {
                    sortingCriteria = opaque 
                        ? SortingCriteria.CommonOpaque
                        : SortingCriteria.CommonTransparent,
                    rendererConfiguration = PerObjectData.ReflectionProbes
                        | PerObjectData.LightData | PerObjectData.LightIndices
                        | PerObjectData.Lightmaps | PerObjectData.ShadowMask
                        | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
                        | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume,
                    renderQueueRange = opaque
                        ? RenderQueueRange.opaque
                        : RenderQueueRange.transparent
                }));

            builder.SetRenderFunc<GeometryPass>((pass, context) => pass.Render(context));
        }
    }
}

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

        public static void Record(
            RenderGraph renderGraph, 
            Camera camera, 
            CullingResults cullingResults, 
            bool opaque,
            in CameraRendererTextures textures,
            in LightResources lightResources)
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

            builder.ReadWriteTexture(textures.colorAttachment);
            builder.ReadWriteTexture(textures.depthAttachment);

            if(!opaque)
            {
                if(textures.colorCopy.IsValid())
                {
                    builder.ReadTexture(textures.colorCopy);
                }

                if(textures.depthCopy.IsValid())
                {
                    builder.ReadTexture(textures.depthCopy);
                }
            }

            builder.ReadBuffer(lightResources.directionalLightDataBuffer);
            builder.ReadBuffer(lightResources.additionalLightDataBuffer);
            builder.ReadTexture(lightResources.shadowResources.directionalAtlas);
            builder.ReadTexture(lightResources.shadowResources.additionalAtlas);
            builder.ReadBuffer(lightResources.shadowResources.directionalShadowCascadesBuffer);
            builder.ReadBuffer(lightResources.shadowResources.directionalShadowMatricesBuffer);
            builder.ReadBuffer(lightResources.shadowResources.additionalShadowDataBuffer);
            builder.SetRenderFunc<GeometryPass>(static (pass, context) => pass.Render(context));
        }
    }
}

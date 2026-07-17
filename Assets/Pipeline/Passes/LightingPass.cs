using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Collections;

namespace Graphics
{
    public partial class LightingPass
    {
        static readonly ProfilingSampler sampler = new("Lighting");

        private CullingResults cullingResults;

        private const int directionalLightLimit = 4;
        private const int additionalLightLimit = 64;
        
        Shadows shadows = new Shadows();

        private static int directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int directionalLightDataId = Shader.PropertyToID("_DirectionalLightData");
        private static int additionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static int additionalLightDataId = Shader.PropertyToID("_AdditionalLightData");

        static readonly DirectionalLightData[] directionalLightData = new DirectionalLightData[directionalLightLimit];
        static readonly AdditionalLightData[] additionalLightData = new AdditionalLightData[additionalLightLimit];
        private int directionalLightCount;
        private int additionalLightCount;

        BufferHandle directionalLightDataBuffer;
        BufferHandle additionalLightDataBuffer;

        CommandBuffer buffer;

        public void Setup(CullingResults cullingResults, 
            ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;

            shadows.Setup(cullingResults, shadowSettings);
            
            SetupLights();
        }

        void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

            directionalLightCount = 0;
            additionalLightCount = 0;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
                var visibleLight = visibleLights[i];
                switch(visibleLight.lightType)
                {
                case LightType.Directional:
                    if (directionalLightCount < directionalLightLimit)
                    {
                        directionalLightData[directionalLightCount] = 
                            SetupDirectionalLight(directionalLightCount, i, ref visibleLight);
                        directionalLightCount++;
                    }
                    break;
                case LightType.Point:
                    if(additionalLightCount < additionalLightLimit)
                    {
                        additionalLightData[additionalLightCount] = 
                            SetupPointLight(additionalLightCount, i, ref visibleLight);
                        additionalLightCount++;
                    }
                    break;
                case LightType.Spot:
                    if(additionalLightCount < additionalLightLimit)
                    {
                        additionalLightData[additionalLightCount] = 
                            SetupSpotLight(additionalLightCount, i, ref visibleLight);
                        additionalLightCount++;
                    }
                    break;
                }
            }
        }

        void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;

            buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
            buffer.SetBufferData(directionalLightDataBuffer, directionalLightData, 0, 0, directionalLightCount);
            buffer.SetGlobalBuffer(directionalLightDataId, directionalLightDataBuffer);

            buffer.SetGlobalInt(additionalLightCountId, additionalLightCount);
            buffer.SetBufferData(additionalLightDataBuffer, additionalLightData, 0, 0, additionalLightCount);
            buffer.SetGlobalBuffer(additionalLightDataId, additionalLightDataBuffer);

            shadows.Render(context);
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        public static LightResources Record(
            RenderGraph renderGraph,
            CullingResults cullingResults, 
            ShadowSettings shadowSettings)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out LightingPass pass, sampler);
            pass.Setup(cullingResults, shadowSettings);

            pass.directionalLightDataBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(new BufferDesc
            {
                name = "Directional Light Data",
                count = directionalLightLimit,
                stride = DirectionalLightData.stride
            }));

            pass.additionalLightDataBuffer = builder.WriteBuffer(renderGraph.CreateBuffer(new BufferDesc
            {
                name = "Additional Light Data",
                count = additionalLightLimit,
                stride = AdditionalLightData.stride
            }));

            builder.SetRenderFunc<LightingPass>(static (pass, context) => pass.Render(context));
            builder.AllowPassCulling(false);

            return new LightResources(
                pass.directionalLightDataBuffer,
                pass.additionalLightDataBuffer,
                pass.shadows.GetResources(renderGraph, builder));
        }
    }
}
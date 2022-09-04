using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public class Lighting
    {
        private const string _bufferName = "Lighting";
        private CullingResults _cullingResults;

        private const int directionalLightLimit = 4;
        
        Shadows shadows = new Shadows();

        private static int directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int directionalLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int directionalLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
        private static int directionalLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private static Vector4[] directionalLightColors = new Vector4[directionalLightLimit];
        private static Vector4[] directionalLightDirections = new Vector4[directionalLightLimit];
        private static Vector4[] directionalLightShadowData = new Vector4[directionalLightLimit];
        
        
        private CommandBuffer buffer = new CommandBuffer
        {
            name = _bufferName
        };

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, 
            ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            buffer.BeginSample(_bufferName);
            //SetupDirectionalLight();
            shadows.Setup(context, cullingResults, shadowSettings);
            SetupLights();
            shadows.Render();
            buffer.EndSample(_bufferName);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            directionalLightColors[index] = visibleLight.finalColor;
            directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            directionalLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }

        void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

            var directionalLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                var visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    SetupDirectionalLight(directionalLightCount++, ref visibleLight);

                    if (directionalLightCount >= directionalLightLimit)
                    {
                        break;
                    }
                }
            }

            buffer.SetGlobalInt(directionalLightCountId, visibleLights.Length);
            buffer.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
            buffer.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
            buffer.SetGlobalVectorArray(directionalLightShadowDataId, directionalLightShadowData);
        }


        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}
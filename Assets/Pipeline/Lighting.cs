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
        private const int additionalLightLimit = 64;
        
        Shadows shadows = new Shadows();

        private static int directionalLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int directionalLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int directionalLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
        private static int directionalLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private static int additionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static int additionalLightColorsId = Shader.PropertyToID("_AdditionalLightColors");
        private static int additionalLightPositionsId = Shader.PropertyToID("_AdditionalLightPositions");
        private static int additionalLightDirectionsId = Shader.PropertyToID("_AdditionalLightDirections");
        private static int additionalLightSpotAnglesId = Shader.PropertyToID("_AdditionalLightSpotAngles");
        private static int additionalLightShadowDataId = Shader.PropertyToID("_AdditionalLightShadowData");

        private static Vector4[] directionalLightColors = new Vector4[directionalLightLimit];
        private static Vector4[] directionalLightDirections = new Vector4[directionalLightLimit];
        private static Vector4[] directionalLightShadowData = new Vector4[directionalLightLimit];

        private static Vector4[] additionalLightColors = new Vector4[additionalLightLimit];
        private static Vector4[] additionalLightPositions = new Vector4[additionalLightLimit];
        private static Vector4[] additionalLightDirections = new Vector4[additionalLightLimit];
        private static Vector4[] additionalLightSpotAngles = new Vector4[additionalLightLimit];
        private static Vector4[] additionalLightShadowData = new Vector4[additionalLightLimit];


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

        void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            directionalLightColors[index] = visibleLight.finalColor;
            directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            directionalLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
        }

        void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            additionalLightColors[index] = visibleLight.finalColor;
            var position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            additionalLightPositions[index] = position;
            additionalLightSpotAngles[index] = new Vector4(0.0f, 1.0f);
            var light = visibleLight.light;
            additionalLightShadowData[index] = shadows.ReserveAdditionalShadows(light, visibleIndex);
        }

        void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight) 
        {
            additionalLightColors[index] = visibleLight.finalColor;
            var position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            additionalLightPositions[index] = position;
            additionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

            var light = visibleLight.light;
            var innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            var outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            var angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
            additionalLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            additionalLightShadowData[index] = shadows.ReserveAdditionalShadows(light, visibleIndex);
        }

        void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

            var directionalLightCount = 0;
            var additionalLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                var visibleLight = visibleLights[i];
                switch(visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (directionalLightCount < directionalLightLimit)
                        {
                            SetupDirectionalLight(directionalLightCount++, i, ref visibleLight);
                        }
                        break;
                    case LightType.Point:
                        if(additionalLightCount < additionalLightLimit)
                        {
                            SetupPointLight(additionalLightCount++, i, ref visibleLight);
                        }
                        break;
                    case LightType.Spot:
                        if(additionalLightCount < additionalLightLimit)
                        {
                            SetupSpotLight(additionalLightCount++, i, ref visibleLight);
                        }
                        break;
                }
            }

            buffer.SetGlobalInt(directionalLightCountId, directionalLightCount);
            if (directionalLightCount > 0)
            {
                buffer.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
                buffer.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
                buffer.SetGlobalVectorArray(directionalLightShadowDataId, directionalLightShadowData);
            }

            buffer.SetGlobalInt(additionalLightCountId, additionalLightCount);
            if (additionalLightCount > 0)
            {
                buffer.SetGlobalVectorArray(additionalLightColorsId, additionalLightColors);
                buffer.SetGlobalVectorArray(additionalLightPositionsId, additionalLightPositions);
                buffer.SetGlobalVectorArray(additionalLightDirectionsId, additionalLightDirections);
                buffer.SetGlobalVectorArray(additionalLightSpotAnglesId, additionalLightSpotAngles);
                buffer.SetGlobalVectorArray(additionalLightShadowDataId, additionalLightShadowData);
            }
        }


        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}
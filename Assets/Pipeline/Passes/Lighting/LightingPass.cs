using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    partial class LightingPass
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DirectionalLightData
        {
            public const int stride = 4 * 4 * 3;
            public Vector4 color, directionAndMask, shadowData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AdditionalLightData
        {
            public const int stride = 4 * 4 * 5;
            public Vector4 color, position, directionaAndMask, spotAngle, shadowData;
        }

        public DirectionalLightData SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            DirectionalLightData data;

            data.color = visibleLight.finalColor;
            data.directionAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            data.shadowData = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);

            return data;
        }

        public AdditionalLightData SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
        {
            AdditionalLightData data;

            data.color = visibleLight.finalColor;
            data.position = visibleLight.localToWorldMatrix.GetColumn(3);
            data.position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            data.directionaAndMask = Vector4.zero;
            data.spotAngle = new Vector4(0.0f, 1.0f);
            var light = visibleLight.light;
            data.shadowData = shadows.ReserveAdditionalShadows(light, visibleIndex);
            
            return data;
        }

        public AdditionalLightData SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight) 
        {
            AdditionalLightData data;

            data.color = visibleLight.finalColor;
            data.position = visibleLight.localToWorldMatrix.GetColumn(3);
            data.position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            data.directionaAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);

            var light = visibleLight.light;
            var innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            var outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            var angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
            data.spotAngle = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            data.shadowData = shadows.ReserveAdditionalShadows(light, visibleIndex);
            
            return data;
        }
    }
}
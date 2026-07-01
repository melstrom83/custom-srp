using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        public bool UseSRPBatcher = true;

        [SerializeField] 
        ShadowSettings ShadowSettings = default;

        [SerializeField]
        PostFXSettings PostFXSettings = default;

        [SerializeField]
        CameraBufferSettings cameraBuffer = new CameraBufferSettings
        { 
            renderScale = 1.0f,
            allowHDR = true,
            fxaa = new CameraBufferSettings.FXAA
            {
                fixedThreshold = 0.0833f,
                relativeThreshold = 0.166f,
                subpixelBlending = 0.75f
            }
        };

        public enum ColorLUTResolution
        {
            _16 = 16, _32 = 32, _64 = 64,
        }

        [SerializeField]
        ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

        [SerializeField]
        Shader cameraRendererShader = default;

        [Header("Deprecated Settings")]
        [SerializeField, Tooltip("Dynamic batching is no longer used.")]
        bool useDynamicBatching;

        [SerializeField, Tooltip("GPU instancing is always enabled.")]
        bool useGPUInstancing;

        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(cameraBuffer, UseSRPBatcher,
                ShadowSettings, PostFXSettings, (int)colorLUTResolution, cameraRendererShader);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        //public bool UseSRPBatcher = true;

        [SerializeField]
        CustomRenderPipelineSettings settings;

        [Header("Deprecated Settings")]

        [SerializeField, Tooltip("Moved to settings")] 
        ShadowSettings shadowSettings = default;

        [SerializeField, Tooltip("Moved to settings")]
        PostFXSettings postFXSettings = default;

        [SerializeField, Tooltip("Moved to settings")]
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

        [SerializeField, Tooltip("Moved to settings")]
        ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

        [SerializeField, Tooltip("Moved to settings")]
        Shader cameraRendererShader = default;


        protected override RenderPipeline CreatePipeline()
        {
            if((settings == null || settings.cameraRendererShader == null) &&
                cameraRendererShader != null)
            {
                settings = new CustomRenderPipelineSettings
                {
                    cameraBuffer = cameraBuffer,
                    shadowSettings = shadowSettings,
                    postFXSettings = postFXSettings,
                    colorLUTResolution = (CustomRenderPipelineSettings.ColorLUTResolution)colorLUTResolution,
                    cameraRendererShader = cameraRendererShader
                };
            }
            return new CustomRenderPipeline(settings);
        }
    }
}

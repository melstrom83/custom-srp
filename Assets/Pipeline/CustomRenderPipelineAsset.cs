using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        public bool UseDynamicBatching = true;
        public bool UseGPUInstancing = true;
        public bool UseSRPBatcher = true;

        [SerializeField] 
        ShadowSettings ShadowSettings = default;

        [SerializeField]
        PostFXSettings PostFXSettings = default;

        [SerializeField]
        bool allowHDR = true;

        public enum ColorLUTResolution
        {
            _16 = 16, _32 = 32, _64 = 64,
        }

        [SerializeField]
        ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

        [SerializeField]
        Shader cameraRendererShader = default;

        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(allowHDR, UseDynamicBatching, UseGPUInstancing, UseSRPBatcher,
                ShadowSettings, PostFXSettings, (int)colorLUTResolution, cameraRendererShader);
        }
    }
}

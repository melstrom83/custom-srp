using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        private CameraRenderer _renderer = new CameraRenderer();

        bool _allowHDR;
        private bool _useDynamicBatching;
        private bool _useGPUInstancing;
        private ShadowSettings _shadowSettings;
        private PostFXSettings _postFXSettings;

        public CustomRenderPipeline(bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings)
        {
            _allowHDR = allowHDR;
            _useDynamicBatching = useDynamicBatching;
            _useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
            GraphicsSettings.lightsUseLinearIntensity = true;

            _shadowSettings = shadowSettings;
            _postFXSettings = postFXSettings;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                _renderer.Render(context, camera, _allowHDR, _useDynamicBatching, _useGPUInstancing,
                    _shadowSettings, _postFXSettings);
            }
        }
    }
}

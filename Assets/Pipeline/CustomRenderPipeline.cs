using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        private CameraRenderer renderer; // = new CameraRenderer();

        bool allowHDR;
        private bool useDynamicBatching;
        private bool useGPUInstancing;
        private ShadowSettings shadowSettings;
        private PostFXSettings postFXSettings;
        int colorLUTResolution;

        partial void DisposeForEditor();

        public CustomRenderPipeline(bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution, Shader cameraRendererShader)
        {
            this.allowHDR = allowHDR;
            this.useDynamicBatching = useDynamicBatching;
            this.useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
            GraphicsSettings.lightsUseLinearIntensity = true;

            this.shadowSettings = shadowSettings;
            this.postFXSettings = postFXSettings;
            this.colorLUTResolution = colorLUTResolution;

            InitializeForEditor();

            renderer = new CameraRenderer(cameraRendererShader);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                renderer.Render(context, camera, allowHDR, useDynamicBatching, useGPUInstancing,
                    shadowSettings, postFXSettings, colorLUTResolution);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeForEditor();
            renderer.Dispose();
        }
    }
}

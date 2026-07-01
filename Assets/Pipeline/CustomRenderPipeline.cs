using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        private CameraRenderer renderer; // = new CameraRenderer();

        CameraBufferSettings cameraBufferSettings;
        private ShadowSettings shadowSettings;
        private PostFXSettings postFXSettings;
        int colorLUTResolution;

        readonly RenderGraph renderGraph = new("Custom SRP Rendcer Graph");

        partial void DisposeForEditor();

        public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings,
            bool useSRPBatching,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution, Shader cameraRendererShader)
        {
            this.cameraBufferSettings = cameraBufferSettings;
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
                renderer.Render(renderGraph, context, camera, cameraBufferSettings,
                    shadowSettings, postFXSettings, colorLUTResolution);
            }

            renderGraph.EndFrame();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            DisposeForEditor();
            renderer.Dispose();

            renderGraph.Cleanup();
        }
    }
}

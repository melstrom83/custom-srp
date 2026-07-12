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

        readonly CustomRenderPipelineSettings settings;

        readonly RenderGraph renderGraph = new("Custom SRP Rendcer Graph");

        partial void DisposeForEditor();

        public CustomRenderPipeline(CustomRenderPipelineSettings settings)
        {
            this.settings = settings;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            GraphicsSettings.lightsUseLinearIntensity = true;

            InitializeForEditor();

            renderer = new CameraRenderer(settings.cameraRendererShader);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                renderer.Render(renderGraph, context, camera, settings);
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

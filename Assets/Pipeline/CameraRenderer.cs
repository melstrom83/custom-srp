using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public class CameraRenderer
    {
        private const string name = "Render Camera";

        static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        
        static CameraSettings defaultCameraSettings = new CameraSettings();

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

        public static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        public static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static int sourceTextureId = Shader.PropertyToID("_SourceTexture");
        
        PostFXStack postFXStack = new PostFXStack();

        Material material;
        Texture2D missingTexture;

        public CameraRenderer(Shader shader) =>
            material = CoreUtils.CreateEngineMaterial(shader);
        


        
        public void Render(
            RenderGraph renderGraph, 
            ScriptableRenderContext context, 
            Camera camera, 
            CameraBufferSettings cameraBufferSettings,
            ShadowSettings shadowSettings, 
            PostFXSettings postFXSettings,
            int colorLUTResolution)
        {
            ProfilingSampler cameraSampler = ProfilingSampler.Get(camera.cameraType);
            CameraSettings cameraSettings = defaultCameraSettings;
            
            if(camera.TryGetComponent(out CustomRenderPipelineCamera crpCamera))
            {
                cameraSampler = crpCamera.Sampler;
                cameraSettings = crpCamera.Settings;
            }

            bool useColorTexture, useDepthTexture;
            if(camera.cameraType == CameraType.Reflection)
            {
                useColorTexture = cameraBufferSettings.copyColorReflection;
                useDepthTexture = cameraBufferSettings.copyDepthReflection;
            }
            else 
            {
                useColorTexture = cameraBufferSettings.copyColor && cameraSettings.copyColor;
                useDepthTexture = cameraBufferSettings.copyDepth && cameraSettings.copyDepth;
            }

            if(cameraSettings.overridePostFX)
            {
                postFXSettings = cameraSettings.postFXSettings;
            }

            var useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;
            var renderScale = cameraBufferSettings.renderScale;
            var useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;


#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                useScaledRendering = false;
            }
#endif
            var bufferSize = useScaledRendering
                ? new Vector2Int((int)(camera.pixelWidth * renderScale), (int)(camera.pixelHeight * renderScale))
                : new Vector2Int(camera.pixelWidth, camera.pixelHeight);
            

            if(!camera.TryGetCullingParameters(out var scriptableCullingParameters))
            {
                return;
            }
            scriptableCullingParameters.shadowDistance = 
                Mathf.Min(shadowSettings.MaxDistance, camera.farClipPlane);
            var cullingResults = context.Cull(ref scriptableCullingParameters);


            
            postFXStack.Setup(camera, bufferSize, postFXSettings,
                useHDR, colorLUTResolution, 
                cameraSettings.finalBlendMode, cameraSettings.keepAlpha,
                cameraBufferSettings.bicubicRescaling, cameraBufferSettings.fxaa
            );

            bool useIntermediateBuffer = useScaledRendering ||
                useColorTexture || useDepthTexture || postFXStack.IsActive;

            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                rendererListCulling = true,
                scriptableRenderContext = context
            };

            renderGraph.BeginRecording(renderGraphParameters);
            {
                using var _ = new RenderGraphProfilingScope(renderGraph, cameraSampler);
                var shadowTextures = LightingPass.Record(renderGraph, cullingResults, shadowSettings);
                var textures = SetupPass.Record(renderGraph, useIntermediateBuffer, 
                    useColorTexture, useDepthTexture, useHDR, bufferSize, camera);
                GeometryPass.Record(renderGraph, camera, cullingResults, true, textures, shadowTextures);
                SkyboxPass.Record(renderGraph, camera, textures);
                var copier = new CameraRendererCopier(material, camera, cameraSettings.finalBlendMode);
                CopyAttachmentsPass.Record(renderGraph,
                    useColorTexture, useDepthTexture, copier, textures);
                GeometryPass.Record(renderGraph, camera, cullingResults, false, textures, shadowTextures);
                UnsupportedShadersPass.Record(renderGraph, camera, cullingResults);
                if(postFXStack.IsActive)
                {
                    PostFXPass.Record(renderGraph, postFXStack, textures);
                }
                else if(useIntermediateBuffer)
                {
                    FinalPass.Record(renderGraph, copier, textures);
                }
                GizmosPass.Record(renderGraph, useIntermediateBuffer, copier, textures);
            }
            renderGraph.EndRecordingAndExecute();

            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
        }
    }
}
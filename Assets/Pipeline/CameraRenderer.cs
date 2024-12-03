using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public partial class CameraRenderer
    {
        private const string name = "Render Camera";

        static CameraSettings defaultCameraSettings = new CameraSettings();

        private CommandBuffer buffer = new CommandBuffer
        {
            name = name
        };

        private CullingResults cullingResults;
        private ScriptableRenderContext context;
        private Camera camera;

        bool useHDR;

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

        //static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

        static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");

        partial void DrawUnsupportedShaders ();
        partial void DrawGizmosBeforeFX();
        partial void DrawGizmosAfterFX();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();
        
        Lighting lighting = new Lighting();
        PostFXStack postFXStack = new PostFXStack();
        
        public void Render(ScriptableRenderContext context, Camera camera,
           bool allowHDR, bool useDynamicBatching, bool useGPUInstancing,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings,
            int colorLUTResolution)
        {
            this.camera = camera;
            this.context = context;

            var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
            var cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

            if(cameraSettings.overridePostFX)
            {
                postFXSettings = cameraSettings.postFXSettings;
            }

            useHDR = allowHDR && camera.allowHDR;

            PrepareBuffer();
            PrepareForSceneWindow();
            
            if (!Cull(shadowSettings.MaxDistance))
            {
                return;
            }

            buffer.BeginSample(SampleName);
            ExecuteBuffer();
            lighting.Setup(context, cullingResults, shadowSettings);
            postFXStack.Setup(context, camera, postFXSettings, useHDR,
                colorLUTResolution, cameraSettings.finalBlendMode);
            buffer.EndSample(SampleName);
            Setup();
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmosBeforeFX();
            if(postFXStack.IsActive)
            {
                postFXStack.Render(colorAttachmentId);
            }
            DrawGizmosAfterFX();
            Cleanup();
            
            Submit();
        }

        void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
                perObjectData = PerObjectData.None
                | PerObjectData.ReflectionProbes
                | PerObjectData.Lightmaps | PerObjectData.ShadowMask
                | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
                | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            context.DrawSkybox(camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref p);
                return true;
            }

            return false;
        }
        
        void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;

            if (postFXStack.IsActive)
            {
                if(flags > CameraClearFlags.Color)
                {
                    flags = CameraClearFlags.Color;
                }

                buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight,
                    0, FilterMode.Bilinear, useHDR 
                    ? RenderTextureFormat.DefaultHDR 
                    : RenderTextureFormat.Default);

                buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight,
                    32, FilterMode.Point, RenderTextureFormat.Depth);

                buffer.SetRenderTarget(
                    colorAttachmentId, 
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear);
            buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        void Cleanup()
        {
            lighting.Cleanup();
            if (postFXStack.IsActive)
            {
                buffer.ReleaseTemporaryRT(colorAttachmentId);
                buffer.ReleaseTemporaryRT(depthAttachmentId);
            }
        }

        void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            context.Submit();
        }
        
        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}
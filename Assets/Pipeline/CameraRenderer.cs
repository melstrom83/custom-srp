using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public partial class CameraRenderer
    {
        private const string name = "Render Camera";

        static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        
        static CameraSettings defaultCameraSettings = new CameraSettings();

        private CommandBuffer buffer = new CommandBuffer
        {
            name = name
        };

        private CullingResults cullingResults;
        private ScriptableRenderContext context;
        public Camera camera;

        bool useHDR, useScaledRendering;

        Vector2Int bufferSize;

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

        //static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

        static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize");
        public static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        public static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static int sourceTextureId = Shader.PropertyToID("_SourceTexture");
        
        partial void DrawUnsupportedShaders ();
        //partial void DrawGizmosBeforeFX();
        //partial void DrawGizmosAfterFX();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();
        
        Lighting lighting = new Lighting();
        PostFXStack postFXStack = new PostFXStack();

        public bool useColorTexture;
        public bool useDepthTexture;
        public bool useIntermediateBuffer;

        Material material;
        Texture2D missingTexture;

        public CameraRenderer(Shader shader)
        {
            material = CoreUtils.CreateEngineMaterial(shader);
            missingTexture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Missing"
            };
            missingTexture.SetPixel(0, 0, Color.gray);
            missingTexture.Apply(true, true);
        }
        
        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, 
            Camera camera, CameraBufferSettings cameraBufferSettings,
            bool useDynamicBatching, bool useGPUInstancing,
            ShadowSettings shadowSettings, PostFXSettings postFXSettings,
            int colorLUTResolution)
        {
            this.camera = camera;
            this.context = context;

            var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
            var cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

            //useDepthTexture = true;
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

            useHDR = cameraBufferSettings.allowHDR && camera.allowHDR;
            
            var renderScale = cameraBufferSettings.renderScale;
            useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
            if (useScaledRendering)
            {
                bufferSize.x = (int)(camera.pixelWidth * renderScale);
                bufferSize.y = (int)(camera.pixelHeight * renderScale);
            }
            else
            {
                bufferSize.x = camera.pixelWidth;
                bufferSize.y = camera.pixelHeight;
            }



            PrepareBuffer();
            PrepareForSceneWindow();
            
            if (!Cull(shadowSettings.MaxDistance))
            {
                return;
            }

            buffer.BeginSample(SampleName);
            buffer.SetGlobalVector(bufferSizeId, new Vector4(
                1.0f / bufferSize.x, 1.0f / bufferSize.y,
                bufferSize.x, bufferSize.y
            ));
            ExecuteBuffer();
            lighting.Setup(context, cullingResults, shadowSettings);
            
            //cameraBufferSettings.fxaa.enabled &= cameraSettings.allowFXAA;
            postFXStack.Setup(context, camera, bufferSize, postFXSettings,
            useHDR, colorLUTResolution, 
            cameraSettings.finalBlendMode, cameraSettings.keepAlpha,
            cameraBufferSettings.bicubicRescaling, cameraBufferSettings.fxaa);
            buffer.EndSample(SampleName);
            Setup();
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            DrawUnsupportedShaders();
            //DrawGizmosBeforeFX();
            //if(postFXStack.IsActive)
            //{
            //    postFXStack.Render(colorAttachmentId);
            //}
            //else if(useIntermediateBuffer)
            //{
            //    Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            //    ExecuteBuffer();
            //}
            //DrawGizmosAfterFX();

            var renderGraphParameters = new RenderGraphParameters
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                //executionName = "Render Camera",
                scriptableRenderContext = context
            };
            renderGraph.BeginRecording(renderGraphParameters);
            {
                if(postFXStack.IsActive)
                {
                    PostFXPass.Record(renderGraph, postFXStack);
                }
                else if(useIntermediateBuffer)
                {
                    FinalPass.Record(renderGraph, this, cameraSettings.finalBlendMode);
                }
                GizmosPass.Record(renderGraph, this);
            }
            renderGraph.EndRecordingAndExecute();

            //context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            //context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
            
            Cleanup();
            Submit();
        }

        public void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
        {
            buffer.SetGlobalTexture(sourceTextureId, from);
            buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
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
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            context.DrawSkybox(camera);

            if (useColorTexture || useDepthTexture)
            {
                CopyAttachments();
            }

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
        
        public void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;

            useIntermediateBuffer = useScaledRendering ||
                useColorTexture || useDepthTexture || postFXStack.IsActive;

            if (useIntermediateBuffer)
            {
                if(flags > CameraClearFlags.Color)
                {
                    flags = CameraClearFlags.Color;
                }

                buffer.GetTemporaryRT(colorAttachmentId, bufferSize.x, bufferSize.y,
                    0, FilterMode.Bilinear, useHDR 
                    ? RenderTextureFormat.DefaultHDR 
                    : RenderTextureFormat.Default);

                buffer.GetTemporaryRT(depthAttachmentId, bufferSize.x, bufferSize.y,
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
            buffer.SetGlobalTexture(colorTextureId, missingTexture); 
            buffer.SetGlobalTexture(depthTextureId, missingTexture);
            ExecuteBuffer();
        }

        public void Cleanup()
        {
            lighting.Cleanup();
            if (useIntermediateBuffer)
            {
                buffer.ReleaseTemporaryRT(colorAttachmentId);
                buffer.ReleaseTemporaryRT(depthAttachmentId);

                if(useColorTexture)
                {
                    buffer.ReleaseTemporaryRT(colorAttachmentId);
                }

                if (useDepthTexture)
                {
                    buffer.ReleaseTemporaryRT(depthAttachmentId);
                }
            }
        }

        void CopyAttachments()
        {
            if(useColorTexture)
            {
                buffer.GetTemporaryRT(colorTextureId, bufferSize.x, bufferSize.y,
                    0, FilterMode.Bilinear, useHDR ?
                        RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

                if(copyTextureSupported)
                {
                    buffer.CopyTexture(colorAttachmentId, colorTextureId);
                }
                else
                {
                    Draw(colorAttachmentId, colorTextureId);
                }
            }

            if(useDepthTexture)
            {
                buffer.GetTemporaryRT(depthTextureId, bufferSize.x, bufferSize.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth);

                if(copyTextureSupported)
                {
                    buffer.CopyTexture(depthAttachmentId, depthTextureId);
                }
                else
                {
                    Draw(depthAttachmentId, depthTextureId, true);
                }

                if(!copyTextureSupported)
                {
                    buffer.SetRenderTarget(
                        colorAttachmentId,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                        depthAttachmentId,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                    );
                }
                
                ExecuteBuffer();
            }
        }

        public void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            context.Submit();
        }
        
        public void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            CoreUtils.Destroy(missingTexture);
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public partial class CameraRenderer
    {
        private const string _name = "Render Camera";

        private CommandBuffer _buffer = new CommandBuffer
        {
            name = _name
        };

        private CullingResults _cullingResults;
        
        private ScriptableRenderContext _context;

        private Camera _camera;

        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

        partial void DrawUnsupportedShaders ();
        partial void DrawGizmos ();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();
        
        Lighting lighting = new Lighting();
        
        public void Render(ScriptableRenderContext context, Camera camera,
            bool useDynamicBatching, bool useGPUInstancing,
            ShadowSettings shadowSettings)
        {
            _camera = camera;
            _context = context;

            PrepareBuffer();
            PrepareForSceneWindow();
            
            if (!Cull(shadowSettings.MaxDistance))
            {
                return;
            }

            _buffer.BeginSample(SampleName);
            ExecuteBuffer();
            lighting.Setup(context, _cullingResults, shadowSettings);
            _buffer.EndSample(SampleName);
            Setup();
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmos();
            
            lighting.Cleanup();
            
            Submit();
        }

        void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
                perObjectData = PerObjectData.Lightmaps
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            _context.DrawSkybox(_camera);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        bool Cull(float maxShadowDistance)
        {
            if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
                _cullingResults = _context.Cull(ref p);
                return true;
            }

            return false;
        }
        
        void Setup()
        {
            _context.SetupCameraProperties(_camera);
            CameraClearFlags flags = _camera.clearFlags;
            _buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? _camera.backgroundColor : Color.clear);
            _buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        void Submit()
        {
            _buffer.EndSample(SampleName);
            ExecuteBuffer();
            _context.Submit();
        }
        
        void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}
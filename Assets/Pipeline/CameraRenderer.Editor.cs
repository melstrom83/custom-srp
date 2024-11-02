using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Graphics
{
    public partial class CameraRenderer
    {
#if UNITY_EDITOR

        static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        private static Material _errorMaterial;
        
        private string SampleName { set; get; }

        partial void DrawUnsupportedShaders()
        {
            if (_errorMaterial == null)
            {
                _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
            {
                overrideMaterial = _errorMaterial

            };
            for (int i = 0; i < legacyShaderTagIds.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }

            var filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }


        partial void DrawGizmosBeforeFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
        }

        partial void DrawGizmosAfterFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void PrepareForSceneWindow()
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            buffer.name = SampleName = camera.name;
            Profiler.EndSample();
        }
#else
        const string SampleName = name;
#endif
    }
}
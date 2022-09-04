using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public class Shadows
    {
        private const string _bufferName = "Shadows";
        private CullingResults _cullingResults;

        private ShadowSettings _shadowSettings;

        private static int directionalShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static int directionalShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        private static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

        private const int cascadeLimit = 4;
        private Vector4[] cascadeCullingSpheres = new Vector4[cascadeLimit];
        private Vector4[] cascadeData = new Vector4[cascadeLimit];

        private const int shadowedDirectionalLightLimit = 4;
        private int shadowedDirectionalLightCount;

        private Matrix4x4[] directionalShadowMatrices =
            new Matrix4x4[shadowedDirectionalLightLimit * cascadeLimit];

        struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private ShadowedDirectionalLight[] ShadowedDirectionalLights =
            new ShadowedDirectionalLight[shadowedDirectionalLightLimit];

        static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        private ScriptableRenderContext _context;

        private CommandBuffer _buffer = new CommandBuffer
        {
            name = _bufferName
        };

        public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (shadowedDirectionalLightCount < shadowedDirectionalLightLimit
            && light.shadows != LightShadows.None
            && light.shadowStrength > 0.0f
            && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out var b))
            {
                ShadowedDirectionalLights[shadowedDirectionalLightCount] =
                    new ShadowedDirectionalLight
                    {
                        visibleLightIndex = visibleLightIndex,
                        slopeScaleBias = light.shadowBias,
                        nearPlaneOffset = light.shadowNearPlane
                    };
                return new Vector3(light.shadowStrength, 
                    _shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount++,
                    light.shadowNormalBias);
            }

            return Vector3.zero;
        }

        public void Setup(ScriptableRenderContext context,
            CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            shadowedDirectionalLightCount = 0;
            
            _context = context;
            _shadowSettings = shadowSettings;
            _cullingResults = cullingResults;
        }

        void SetKeywords()
        {
            var enabledIndex = (int)_shadowSettings.directional.filter - 1;
            for (var i = 0; i < directionalFilterKeywords.Length; ++i)
            {
                if (i == enabledIndex)
                {
                    _buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
                }
                else
                {
                    _buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
                }
            }
        }

        public void Render()
        {
            var atlasSize = (int)_shadowSettings.directional.atlasSize;
            
            if (shadowedDirectionalLightCount > 0)
            {
                _buffer.GetTemporaryRT(directionalShadowAtlasId, 
                    atlasSize,
                    atlasSize,
                    32,
                    FilterMode.Bilinear,
                    RenderTextureFormat.Shadowmap);
            }
            else
            {
                _buffer.GetTemporaryRT(directionalShadowAtlasId, 
                    1,
                    1,
                    32,
                    FilterMode.Bilinear,
                    RenderTextureFormat.Shadowmap);
            }
            
            _buffer.SetRenderTarget(directionalShadowAtlasId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            _buffer.ClearRenderTarget(true, false, Color.clear, 1.0f);
            
            _buffer.BeginSample(_bufferName);
            ExecuteBuffer();

            var tiles = _shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount;
            var split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            var tileSize = atlasSize / split;

            for (var i = 0; i < shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            var f = 1.0f - _shadowSettings.directional.cascadeFade;
            
            _buffer.SetGlobalInt(cascadeCountId, _shadowSettings.directional.cascadeCount);
            _buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            _buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            _buffer.SetGlobalMatrixArray(directionalShadowMatricesId, directionalShadowMatrices);
            _buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
                1.0f / _shadowSettings.MaxDistance,
                1.0f / _shadowSettings.FadeDistance,
                1.0f / (1.0f - f * f)));
            SetKeywords();
            _buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(
                atlasSize,
                1.0f / atlasSize));
            _buffer.EndSample(_bufferName);
            ExecuteBuffer();
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            var scale = 1.0f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            
            return m;
        }

        Vector2 SetTileViewport(int index, int split, int tileSize)
        {
            var offset = new Vector2(index % split, index / split);
            _buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            var texelSize = 2.0f * cullingSphere.w / tileSize;
            var filterSize = texelSize * ((float)_shadowSettings.directional.filter + 1.0f);

            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            
            cascadeData[index] = new Vector4(
                1.0f / cullingSphere.w,
                filterSize * Mathf.Sqrt(2.0f));
        }

        void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            var light = ShadowedDirectionalLights[index];
            var shadowSettings = 
                new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex);
            var cascadeCount = _shadowSettings.directional.cascadeCount;
            var tileOffset = index * cascadeCount;
            var ratios = _shadowSettings.directional.CascadeRatios;

            var cullingFactor = Mathf.Max(0.0f, 0.8f - _shadowSettings.directional.cascadeFade);

            for (var i = 0; i < cascadeCount; ++i)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex, i, cascadeCount,
                    ratios, tileSize, light.nearPlaneOffset,
                    out var view, out var projection, out var splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }
                
                var tileIndex = tileOffset + i;
                directionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projection * view,
                    SetTileViewport(tileIndex, split, tileSize), split);
                _buffer.SetViewProjectionMatrices(view, projection);
                _buffer.SetGlobalDepthBias(0.0f, light.slopeScaleBias);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowSettings);
                _buffer.SetGlobalDepthBias(0.0f, 0.0f);
            }
        }

        public void Cleanup()
        {
            _buffer.ReleaseTemporaryRT(directionalShadowAtlasId);
            ExecuteBuffer();
        }

        void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}
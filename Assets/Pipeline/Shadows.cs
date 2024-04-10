using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public class Shadows
    {
        private const string _bufferName = "Shadows";
        private CullingResults _cullingResults;

        private ShadowSettings _shadowSettings;

        private static readonly int directionalShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static readonly int directionalShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static readonly int additionalShadowAtlasId = Shader.PropertyToID("_AdditionalShadowAtlas");
        private static readonly int additionalShadowMatricesId = Shader.PropertyToID("_AdditionalShadowMatrices");
        private static readonly int additionalShadowTilesId = Shader.PropertyToID("_AdditionalShadowTiles");
        private static readonly int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static readonly int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static readonly int cascadeDataId = Shader.PropertyToID("_CascadeData");
        private static readonly int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        private static readonly int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        private static readonly int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

        private const int cascadeLimit = 4;
        private readonly Vector4[] cascadeCullingSpheres = new Vector4[cascadeLimit];
        private readonly Vector4[] cascadeData = new Vector4[cascadeLimit];
        
        private Vector4 atlasSizes;

        private const int shadowedDirectionalLightLimit = 4;
        private int shadowedDirectionalLightCount;
        private const int shadowedAdditionalLightLimit = 16;
        private int shadowedAdditionalLightCount;

        private readonly Matrix4x4[] directionalShadowMatrices =
            new Matrix4x4[shadowedDirectionalLightLimit * cascadeLimit];
        private readonly Matrix4x4[] additionalShadowMatrices =
            new Matrix4x4[shadowedAdditionalLightLimit];

        private readonly Vector4[] additionalShadowTiles = new Vector4[shadowedAdditionalLightLimit];

        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private struct ShadowedAdditionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public bool isPoint;
        }

        private readonly ShadowedDirectionalLight[] ShadowedDirectionalLights =
            new ShadowedDirectionalLight[shadowedDirectionalLightLimit];

        private readonly ShadowedAdditionalLight[] ShadowedAdditionalLights =
            new ShadowedAdditionalLight[shadowedAdditionalLightLimit];

        private static readonly string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        private static readonly string[] additionalFilterKeywords =
        {
            "_ADDITIONAL_PCF3",
            "_ADDITIONAL_PCF5",
            "_ADDITIONAL_PCF7",
        };

        private static readonly string[] shadowMasKeywords =
        {
            "_SHADOW_MASK_ALWAYS",
            "_SHADOW_MASK_DISTANCE"
        };

        private bool useShadowMask;

        private ScriptableRenderContext _context;

        private readonly CommandBuffer _buffer = new CommandBuffer
        {
            name = _bufferName
        };

        public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (shadowedDirectionalLightCount < shadowedDirectionalLightLimit
                && light.shadows != LightShadows.None
                && light.shadowStrength > 0.0f)
            {
                var maskChannel = -1;
                var lightBaking = light.bakingOutput;
                if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed
                    && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    useShadowMask = true;
                    maskChannel = lightBaking.occlusionMaskChannel;
                }

                if (_cullingResults.GetShadowCasterBounds(visibleLightIndex, out var b))
                {
                    ShadowedDirectionalLights[shadowedDirectionalLightCount] =
                        new ShadowedDirectionalLight
                        {
                            visibleLightIndex = visibleLightIndex,
                            slopeScaleBias = light.shadowBias,
                            nearPlaneOffset = light.shadowNearPlane
                        };
                    return new Vector4(light.shadowStrength,
                        _shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount++,
                        light.shadowNormalBias, maskChannel);
                }
                return new Vector4(-light.shadowStrength, 0.0f, 0.0f, maskChannel);
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        public Vector4 ReserveAdditionalShadows(Light light, int visibleLightIndex)
        {
            if(light.shadows == LightShadows.None || light.shadowStrength <= 0.0f)
            {
                return new Vector4(0f, 0f, 0f, -1f);
            }
            
            var maskChannel = -1;
            var lightBaking = light.bakingOutput;
            if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed
                && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            var isPoint = light.type == LightType.Point;
            var newLightCount = shadowedAdditionalLightCount + (isPoint ? 6 : 1);

            if(newLightCount > shadowedAdditionalLightLimit
                || !_cullingResults.GetShadowCasterBounds(visibleLightIndex, out var b))
            {
                return new Vector4(-light.shadowStrength, 0.0f, 0.0f, maskChannel);
            }

            ShadowedAdditionalLights[shadowedAdditionalLightCount] = new ShadowedAdditionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias,
                isPoint = isPoint
            };

            var data = new Vector4(light.shadowStrength, shadowedAdditionalLightCount,
                isPoint ? 1.0f : 0.0f, maskChannel);

            shadowedAdditionalLightCount = newLightCount;    

            return data;
        }

        public void Setup(ScriptableRenderContext context,
            CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            shadowedDirectionalLightCount = 0;
            shadowedAdditionalLightCount = 0;
            useShadowMask = false;
            
            _context = context;
            _shadowSettings = shadowSettings;
            _cullingResults = cullingResults;
        }

        private void SetKeywords(string[] keywords, int enabledIndex) 
        {
            for (var i = 0; i < keywords.Length; ++i)
            {
                if (i == enabledIndex)
                {
                    _buffer.EnableShaderKeyword(keywords[i]);
                }
                else
                {
                    _buffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }

        public void Render()
        {
            if(shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }

            if (shadowedAdditionalLightCount > 0)
            {
                RenderAdditionalShadows();
            }

            SetKeywords(shadowMasKeywords, useShadowMask ? 
                QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ?
                0 : 1 :-1);
            
            _buffer.SetGlobalInt(cascadeCountId, shadowedDirectionalLightCount > 0 
                ? _shadowSettings.directional.cascadeCount
                : 0);
            var f = 1.0f - _shadowSettings.directional.cascadeFade;
            _buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(
                1.0f / _shadowSettings.MaxDistance,
                1.0f / _shadowSettings.FadeDistance,
                1.0f / (1.0f - f * f)));
            _buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
            
            ExecuteBuffer();
        }

        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

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

        private Vector2 SetTileViewport(int index, int split, int tileSize)
        {
            var offset = new Vector2(index % split, index / split);
            _buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
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

        private void RenderDirectionalShadows()
        {
            var atlasSize = (int)_shadowSettings.directional.atlasSize;
            atlasSizes.x = atlasSize;
            atlasSizes.y = 1.0f / atlasSize;

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
            _buffer.SetGlobalFloat(shadowPancakingId, 1.0f);

            //_buffer.BeginSample("Directional Shadows");
            ExecuteBuffer();

            var tiles = _shadowSettings.directional.cascadeCount * shadowedDirectionalLightCount;
            var split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            var tileSize = atlasSize / split;

            for (var i = 0; i < shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            _buffer.SetGlobalVectorArray(cascadeCullingSpheresId, 
                cascadeCullingSpheres);
            _buffer.SetGlobalVectorArray(cascadeDataId, 
                cascadeData);
            _buffer.SetGlobalMatrixArray(directionalShadowMatricesId,
                directionalShadowMatrices);
            SetKeywords(directionalFilterKeywords, 
                (int)_shadowSettings.directional.filter - 1);
            
            //_buffer.EndSample("Directional Shadows");
            ExecuteBuffer();
        }

        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            var light = ShadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults,
                light.visibleLightIndex, BatchCullingProjectionType.Orthographic);
            var cascadeCount = _shadowSettings.directional.cascadeCount;
            var tileOffset = index * cascadeCount;
            var ratios = _shadowSettings.directional.CascadeRatios;
            var tileScale = 1.0f / split;
            var cullingFactor = Mathf.Max(0.0f, 0.8f - _shadowSettings.directional.cascadeFade);

            for (var i = 0; i < cascadeCount; ++i)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex, i, cascadeCount,
                    ratios, tileSize, light.nearPlaneOffset,
                    out var view, out var proj, out var splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }
                
                var tileIndex = tileOffset + i;
                directionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(proj * view,
                    SetTileViewport(tileIndex, split, tileSize), tileScale);
                _buffer.SetViewProjectionMatrices(view, proj);
                _buffer.SetGlobalDepthBias(0.0f, light.slopeScaleBias);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowSettings);
                _buffer.SetGlobalDepthBias(0.0f, 0.0f);
            }
        }

        private void RenderAdditionalShadows()
        {
            var atlasSize = (int)_shadowSettings.additional.atlasSize;
            atlasSizes.z = atlasSize;
            atlasSizes.w = 1.0f / atlasSize;

            if (shadowedAdditionalLightCount > 0)
            { 
                _buffer.GetTemporaryRT(additionalShadowAtlasId,
                    atlasSize,
                    atlasSize,
                    32,
                    FilterMode.Bilinear,
                    RenderTextureFormat.Shadowmap);
            }
            else
            {
                _buffer.GetTemporaryRT(additionalShadowAtlasId,
                    1,
                    1,
                    32,
                    FilterMode.Bilinear,
                    RenderTextureFormat.Shadowmap);
            }

            _buffer.SetRenderTarget(additionalShadowAtlasId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            _buffer.ClearRenderTarget(true, false, Color.clear, 1.0f);
            _buffer.SetGlobalFloat(shadowPancakingId, 0.0f);

            //_buffer.BeginSample("Additional Shadows");
            ExecuteBuffer();

            var tiles = shadowedAdditionalLightCount;
            var split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            var tileSize = atlasSize / split;

            for (var i = 0; i < shadowedAdditionalLightCount;)
            {
                if (ShadowedAdditionalLights[i].isPoint)
                {
                    RenderPointShadows(i, split, tileSize);
                    i += 6;
                }
                else
                {
                    RenderSpotShadows(i, split, tileSize);
                    i += 1;
                }
            }

            _buffer.SetGlobalMatrixArray(additionalShadowMatricesId,
                additionalShadowMatrices);
            _buffer.SetGlobalVectorArray(additionalShadowTilesId,
                additionalShadowTiles);
            SetKeywords(additionalFilterKeywords,
                (int)_shadowSettings.additional.filter - 1);

            //_buffer.EndSample("Additional Shadows");
            ExecuteBuffer();
        }

        private void RenderSpotShadows(int index, int split, int tileSize)
        {
            var light = ShadowedAdditionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults,
                light.visibleLightIndex, BatchCullingProjectionType.Perspective);
            _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex,
                out var view, out var proj, out var splitData);
            shadowSettings.splitData = splitData;
            var texelSize = 2.0f / (tileSize * proj.m00);
            var filterSize = texelSize * ((float)_shadowSettings.additional.filter + 1.0f);
            var bias = light.normalBias * filterSize * Mathf.Sqrt(2.0f);
            var offset = SetTileViewport(index, split, tileSize);
            var tileScale = 1.0f / split;
            SetAdditionalTileData(index, offset, tileScale, bias);
            additionalShadowMatrices[index] = ConvertToAtlasMatrix(
                proj * view, offset, tileScale);
            _buffer.SetViewProjectionMatrices(view, proj);
            _buffer.SetGlobalDepthBias(0.0f, light.slopeScaleBias);
            ExecuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _buffer.SetGlobalDepthBias(0.0f, 0.0f);
        }

        private void RenderPointShadows(int index, int split, int tileSize)
        {
            var light = ShadowedAdditionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults,
                light.visibleLightIndex, BatchCullingProjectionType.Perspective);

            var texelSize = 2.0f / tileSize;
            var filterSize = texelSize * ((float)_shadowSettings.additional.filter + 1.0f);
            var bias = light.normalBias * filterSize * Mathf.Sqrt(2.0f);
            var tileScale = 1.0f / split;
            var fovBias = Mathf.Atan(1.0f + bias + filterSize) * Mathf.Rad2Deg * 2.0f - 90.0f;
            for (var i = 0; i < 6; ++i)
            {
                _cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex, (CubemapFace)i, fovBias,
                    out var view, out var proj, out var splitData);
                view.m11 = -view.m11;
                view.m12 = -view.m12;
                view.m13 = -view.m13;
                shadowSettings.splitData = splitData;
                var tileIndex = index + i;
                var offset = SetTileViewport(tileIndex, split, tileSize);
                SetAdditionalTileData(tileIndex, offset, tileScale, bias);
                additionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                    proj * view, offset, tileScale);
                _buffer.SetViewProjectionMatrices(view, proj);
                _buffer.SetGlobalDepthBias(0.0f, light.slopeScaleBias);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowSettings);
                _buffer.SetGlobalDepthBias(0.0f, 0.0f);
            }
        }

        private void SetAdditionalTileData(int index, Vector2 offset, float scale, float bias)
        {
            var border = atlasSizes.w * 0.5f;
            var data = Vector4.zero;
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            data.z = scale - border - border;
            data.w = bias;
            additionalShadowTiles[index] = data;
        }

        public void Cleanup()
        {
            _buffer.ReleaseTemporaryRT(directionalShadowAtlasId);
            _buffer.ReleaseTemporaryRT(additionalShadowAtlasId);
            ExecuteBuffer();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}
#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_point_clamp);

TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_DistortionMap);
SAMPLER(sampler_BaseMap);
SAMPLER(sampler_DistortionMap);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)


struct InputConfig
{
    float4 color;
    float3 flipbookUVB;
    bool flipbookBlending;
    float2 baseUV;
    float2 detailUV;
    float2 positionSS;
    float2 screenUV;
    float depth;
    float bufferDepth;
    bool useMask;
    bool useDetail;
    bool nearFade;
    bool softParticles;
};

InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float2 detailUV = 0)
{
    InputConfig config;
    config.color = 1.0;
    config.flipbookUVB = 0.0;
    config.flipbookBlending = false;
    config.baseUV = baseUV;
    config.detailUV = detailUV;
    config.positionSS = positionSS.xy;
    config.screenUV = positionSS.xy / _ScreenParams.xy;
    config.depth = IsOrthographicCamera() ?
      OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    config.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,
      sampler_point_clamp, config.screenUV, 0);
    config.bufferDepth = IsOrthographicCamera() ?
      OrthographicDepthBufferToLinear(config.bufferDepth) :
      LinearEyeDepth(config.bufferDepth, _ZBufferParams);
    config.useMask = false;
    config.useDetail = false;
    config.nearFade = false;
    config.softParticles = false;
    return config;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV(float2 detailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

float4 GetBufferColor(float2 uv, float2 offset = float2(0.0, 0.0))
{
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_linear_clamp, uv + offset, 0);
}

float2 GetDistortion(InputConfig config)
{
    float4 rawMap = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, config.baseUV);
    if (config.flipbookBlending)
    {
      rawMap = lerp(rawMap,
              SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, config.flipbookUVB.xy),
              config.flipbookUVB.z);
    }
    return DecodeNormal(rawMap, INPUT_PROP(_DistortionStrength)).xy;
}

float4 GetDetail(InputConfig config)
{
    if (config.useDetail)
    {
        float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, config.detailUV);
        return map * 2.0 - 1.0;
    }
    return 0.0;
}

float4 GetMask(InputConfig config)
{
    if (config.useMask)
    {
        return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, config.baseUV);
    }
  
    return 1.0;
}

float4 GetBase(InputConfig config)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.baseUV);
    if(config.flipbookBlending)
    {
        float4 add = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.flipbookUVB.xy);
        map = lerp(map, add, config.flipbookUVB.z);
    }
  
    if(config.nearFade)
    {
        float nearAttenuation = (config.depth - INPUT_PROP(_NearFadeDistance)) / INPUT_PROP(_NearFadeRange);
        map.a *= saturate(nearAttenuation);
    }
  
    if(config.softParticles)
    {
        float depthDelta = config.bufferDepth - config.depth;
        float softAttenuation =(depthDelta - INPUT_PROP(_SoftParticlesDistance)) / INPUT_PROP(_SoftParticlesRange);
        map.a *= saturate(softAttenuation);
    }
  
    float4 color = INPUT_PROP(_BaseColor);
    
    if (config.useDetail)
    {
        float detail = GetDetail(config).r * INPUT_PROP(_DetailAlbedo);
        float mask = GetMask(config).b;
        map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
        map.rgb *= map.rgb;
    }
  
    return map * color * config.color;
}

float3 GetEmission(InputConfig config)
{
    return 0.0;
}

float GetClipping(InputConfig config)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(InputConfig config)
{
    return 0.0;
}

float GetSmoothness(InputConfig config)
{
    return 0.0;
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}

#endif
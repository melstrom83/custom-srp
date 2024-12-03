#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
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
    float depth;
    bool useMask;
    bool useDetail;
    bool nearFade;
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
    config.depth = IsOrthographicCamera() ?
      OrtographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    config.useMask = false;
    config.useDetail = false;
    config.nearFade = false;
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
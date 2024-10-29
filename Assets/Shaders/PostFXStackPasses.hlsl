#ifndef CUSTOM_POST_FX_STACK_PASSES_INCLUDED
#define CUSTOM_POST_FX_STACK_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);

float4 _ProjectionParams;
float4 _PostFXSource_TexelSize;

float4 _BloomThreshold;
bool _BloomBicubicUpsampling;
float _BloomIntensity;


struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varying DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varying varying;

    varying.positionCS = float4(
      vertexID <= 1 ?-1.0 : 3.0,
      vertexID == 1 ? 3.0 :-1.0,
      0.0, 1.0
    );

    varying.screenUV = float2(
      vertexID <= 1 ? 0.0 : 2.0,
      vertexID == 1 ? 2.0 : 0.0
    );
  
    if(_ProjectionParams.x < 0.0)
    {
      varying.screenUV.y = 1.0 - varying.screenUV.y;
    }

    return varying;
}

float4 GetSource(float2 screenUV)
{
  return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(
        TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp),
        screenUV, _PostFXSource_TexelSize.zwxy, 1.0, 0.0);
}

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 CopyPassFragment(Varying varying) : SV_TARGET
{
    return GetSource(varying.screenUV);
}

float4 BloomHorizontalPassFragment(Varying varying) : SV_TARGET
{
    float offsets[] =
    {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    
    float weights[] =
    {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    
    float3 color = 0.0;
    for (int i = 0; i < 5; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSource(varying.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varying varying) : SV_TARGET
{
    float offsets[] =
    {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    
    float weights[] =
    {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    
    float3 color = 0.0;
    for (int i = 0; i < 5; ++i)
    {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += GetSource(varying.screenUV + float2(0.0, offset)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BloomCombinePassFragment(Varying varying) : SV_TARGET
{
    float3 lowRes = _BloomBicubicUpsampling
        ? GetSourceBicubic(varying.screenUV).rgb
        : GetSource(varying.screenUV).rgb;
    float3 highRes = GetSource2(varying.screenUV).rgb;
    return float4(lowRes * _BloomIntensity + highRes, 1.0);
}

float3 ApplyBloomThreshold(float3 color)
{
    float brightness = max(color.x, max(color.y, color.z));
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 BloomPrefilterPassFragment(Varying varying) : SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(varying.screenUV).rgb);
    return float4(color, 1.0);
}

float4 BloomPrefilterFirefliesPassFragment(Varying varying) : SV_TARGET
{
    float2 offsets[] =
    {
        float2(0.0, 0.0),
		float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
    };
    
    float3 color = 0.0;
    float weightSum = 0.0;
    for (int i = 0; i < 5; ++i)
    {
        float3 c = GetSource(varying.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        c = ApplyBloomThreshold(c);
        float w = 1.0 / (Luminance(c) + 1.0);
        color += c * w;
        weightSum += w;

    }
    color /= weightSum;
    return float4(color, 1.0);
}

float4 BloomScatterPassFragment(Varying varying) : SV_TARGET
{
    float3 lowRes = _BloomBicubicUpsampling
        ? GetSourceBicubic(varying.screenUV).rgb
        : GetSource(varying.screenUV).rgb;
    float3 highRes = GetSource2(varying.screenUV).rgb;
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float3 ColorGrade(float3 color)
{
    color = min(color, 60.0);
    return color;
}

float4 ToneMappingNonePassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetSource(varying.screenUV);
    color = ColorGrade(color);
    return float4(color, 1.0);
}

float4 ToneMappingACESPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetSource(varying.screenUV);
    color = ColorGrade(color);
    color = AcesTonemap(unity_to_ACES(color));
    return float4(color, 1.0);
}

float4 ToneMappingNeutralPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetSource(varying.screenUV);
    color = ColorGrade(color);
    color = NeutralTonemap(color);
    return float4(color, 1.0);
}

float4 ToneMappingReinhardPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetSource(varying.screenUV);
    color = ColorGrade(color);
    color /= color + 1.0;
    return float4(color, 1.0);
}

#endif
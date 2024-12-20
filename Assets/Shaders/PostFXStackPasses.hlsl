#ifndef CUSTOM_POST_FX_STACK_PASSES_INCLUDED
#define CUSTOM_POST_FX_STACK_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
TEXTURE2D(_ColorGradingLUT);
SAMPLER(sampler_linear_clamp);

float4 _PostFXSource_TexelSize;

float4 _BloomThreshold;
bool _BloomBicubicUpsampling;
float _BloomIntensity;

float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;

float4 _SplitToningShadows;
float4 _SplitToningHighlights;

float4 _ChannelMixerR, _ChannelMixerG, _ChannelMixerB;
float4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;

float4 _ColorGradingLUTParameters;
bool _ColorGradingLUTInLogC;

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
    float4 highRes = GetSource2(varying.screenUV);
    return float4(lowRes * _BloomIntensity + highRes.rgb, highRes.a);
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
    float4 highRes = GetSource2(varying.screenUV);
    lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
    return float4(lerp(highRes.rgb, lowRes, _BloomIntensity), highRes.a);
}

float Luminance(float3 color, bool useACES)
{
    return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ColorGradePostExposure(float3 color)
{
    return color * _ColorAdjustments.x;
}

float3 ColorGradeWhiteBalance(float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}

float3 ColorGradingContrast(float3 color, bool useACES)
{
    color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
    color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
    return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 ColorGradingHueShift(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustments.z;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

float3 ColorGradingSaturation(float3 color, bool useACES)
{
    float luminance = Luminance(color, useACES);
    return (color - luminance) * _ColorAdjustments.w + luminance;
}

float3 ColorGradeSplitToning(float3 color, bool useACES)
{
    color = PositivePow(color, 1.0 / 2.2);
    float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
    float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
    float3 highlights = lerp(0.5, _SplitToningHighlights.rgb, t);
    color = SoftLight(color, shadows);
    color = SoftLight(color, highlights);
    return PositivePow(color, 2.2);
}

float3 ColorGradingChannelMixer(float3 color)
{
    float3x3 mixer = float3x3(_ChannelMixerR.rgb, _ChannelMixerG.rgb, _ChannelMixerB.rgb);
    return mul(mixer, color);
}

float3 ColorGradingShadowsMidtonesHighlights(float3 color, bool useACES)
{
    float luminance = Luminance(color, useACES);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
  
    return
      color * _SMHShadows.rgb * shadowsWeight +
      color * _SMHMidtones.rgb * midtonesWeight + 
      color * _SMHHighlights.rgb * highlightsWeight;
}

float3 ColorGrade(float3 color, bool useACES = false)
{
    //color = min(color, 60.0);
    color = ColorGradePostExposure(color);
    color = ColorGradeWhiteBalance(color);
    color = ColorGradingContrast(color, useACES);
    color = ColorGradeColorFilter(color);
    color = max(color, 0.0);
    color = ColorGradeSplitToning(color, useACES);
    color = ColorGradingChannelMixer(color);
    color = max(color, 0.0);
    color = ColorGradingShadowsMidtonesHighlights(color, useACES);
    color = ColorGradingHueShift(color);
    color = ColorGradingSaturation(color, useACES);
    return max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}

float3 GetColorGradedLUT(float2 uv, bool useACES = false)
{
    float3 color = GetLutStripValue(uv, _ColorGradingLUTParameters);
    return ColorGrade(_ColorGradingLUTInLogC ? LogCToLinear(color) : color, useACES);
}

float4 ColorGradingNonePassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetColorGradedLUT(varying.screenUV);
    return float4(color, 1.0);
}

float4 ColorGradingACESPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetColorGradedLUT(varying.screenUV);
    color = AcesTonemap(color);
    return float4(color, 1.0);
}

float4 ColorGradingNeutralPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetColorGradedLUT(varying.screenUV);
    color = NeutralTonemap(color);
    return float4(color, 1.0);
}

float4 ColorGradingReinhardPassFragment(Varying varying) : SV_TARGET
{
    float3 color = GetColorGradedLUT(varying.screenUV);
    color /= color + 1.0;
    return float4(color, 1.0);
}

float3 ApplyColorGradingLUT(float3 color)
{
  return ApplyLut2D(TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp),
  saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color),
  _ColorGradingLUTParameters.xyz);
}

float4 FinalPassFragment(Varying varying) : SV_Target
{
    float4 color = GetSource(varying.screenUV);
    color.rgb = ApplyColorGradingLUT(color.rgb);
    return color;
}

#endif
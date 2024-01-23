#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define SHADOWED_DIRECTIONAL_LIGHT_LIMIT 4
#define CASCADE_LIMIT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[CASCADE_LIMIT];
    float4 _CascadeData[CASCADE_LIMIT]; //x - 1.0 / (radius * radius), y - texelSize * sqrt(2.0)
    float4x4 _DirectionalShadowMatrices[SHADOWED_DIRECTIONAL_LIGHT_LIMIT * CASCADE_LIMIT];
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
};

struct ShadowData
{
    int cascadeIndex;
    float strength;
};

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
    #ifdef DIRECTIONAL_FILTER_SETUP
        real weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.yyxx;
        DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0.0;
        for(int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; ++i)
        {
            float3 samplePosition = float3(positions[i].xy, positionSTS.z);
            shadow += weights[i] * SampleDirectionalShadowAtlas(samplePosition);
        }
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

float FadedShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surface)
{
    ShadowData data;
    data.strength = FadedShadowStrength(surface.depth,
        _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    
    int i = 0;
    for(i = 0; i < _CascadeCount; ++i)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            if(i == _CascadeCount - 1)
            {
                data.strength *= FadedShadowStrength(distanceSqr,
                    _CascadeData[i].x, _ShadowDistanceFade.z);
            }
            break;
        }
    }

    if(i == _CascadeCount)
    {
        data.strength = 0.0;
    }
    
    data.cascadeIndex = i;
    return data;
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global,
    Surface surface)
{
    if(directional.strength <= 0.0)
    {
        return 1.0;
    }
    float3 normalBias = surface.normal *
        (directional.normalBias * _CascadeData[global.cascadeIndex].y);
    float4x4 shadowMatrix = _DirectionalShadowMatrices[directional.tileIndex];
    float3 positionSTS = mul(shadowMatrix, float4(surface.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);

    return lerp(1.0, shadow, directional.strength);
}

#endif
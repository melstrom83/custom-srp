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

#if defined(_ADDITIONAL_PCF3)
    #define ADDITIONAL_FILTER_SAMPLES 4
    #define ADDITIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_ADDITIONAL_PCF5)
    #define ADDITIONAL_FILTER_SAMPLES 9
    #define ADDITIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_ADDITIONAL_PCF7)
    #define ADDITIONAL_FILTER_SAMPLES 16
    #define ADDITIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define SHADOWED_DIRECTIONAL_LIGHT_LIMIT 4
#define CASCADE_LIMIT 4
#define SHADOWED_ADDITIONAL_LIGHT_LIMIT 16

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_AdditionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[CASCADE_LIMIT];
    float4 _CascadeData[CASCADE_LIMIT]; //x - 1.0 / (radius * radius), y - texelSize * sqrt(2.0)
    float4x4 _DirectionalShadowMatrices[SHADOWED_DIRECTIONAL_LIGHT_LIMIT * CASCADE_LIMIT];
    float4x4 _AdditionalShadowMatrices[SHADOWED_ADDITIONAL_LIGHT_LIMIT * CASCADE_LIMIT];
    float4 _AdditionalShadowTiles[SHADOWED_ADDITIONAL_LIGHT_LIMIT];
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
    int maskChannel;
};

struct AdditionalShadowData
{
    float strength;
    int tileIndex;
    bool isPoint;
    int maskChannel;
    float3 lightPositionWS;
    float3 lightDirectionWS;
    float3 spotDirectionWS;
};

struct ShadowMask
{
    bool always;
    bool distance;
    float4 shadows;
};

struct ShadowData
{
    int cascadeIndex;
    float strength;
    ShadowMask shadowMask;
};

static const float3 pointShadowPlanes[6] =
{
    float3(-1.0, 0.0, 0.0),
    float3( 1.0, 0.0, 0.0),
    float3( 0.0,-1.0, 0.0),
    float3( 0.0, 1.0, 0.0),
    float3( 0.0, 0.0,-1.0),
    float3( 0.0, 0.0, 1.0)
};

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
    real weights[DIRECTIONAL_FILTER_SAMPLES];
    real2 positions[DIRECTIONAL_FILTER_SAMPLES];
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

float SampleAdditionalShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_AdditionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterAdditionalShadow(float3 positionSTS, float3 bounds)
{
#if defined(ADDITIONAL_FILTER_SETUP)
    real weights[ADDITIONAL_FILTER_SAMPLES];
    real2 positions[ADDITIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    ADDITIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0.0;
    for(int i = 0; i < ADDITIONAL_FILTER_SAMPLES; ++i)
    {
        float3 samplePosition = float3(positions[i].xy, positionSTS.z);
        shadow += weights[i] * SampleAdditionalShadowAtlas(samplePosition);
    }
    return shadow;
#else
    return SampleAdditionalShadowAtlas(positionSTS, bounds);
#endif
}

float FadedShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.strength = FadedShadowStrength(surfaceWS.depth,
        _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    
    int i = 0;
    for(i = 0; i < _CascadeCount; ++i)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
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

    if (i == _CascadeCount && _CascadeCount > 0)
    {
        data.strength = 0.0;
    }
    
    data.cascadeIndex = i;
    return data;
}

float GetCascadedShadow(DirectionalShadowData directional,
    ShadowData global, Surface surfaceWS)
{
    float3 normalBias = surfaceWS.interpolatedNormal *
          (directional.normalBias * _CascadeData[global.cascadeIndex].y);
    float4 positionSTS = mul(
          _DirectionalShadowMatrices[directional.tileIndex],
          float4(surfaceWS.position /*+ normalBias*/, 1.0));
    float shadow = FilterDirectionalShadow(positionSTS.xyz);
  
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if (mask.always || mask.distance)
    {
        if (channel >= 0)
        {
            shadow = mask.shadows[channel];
        }  
    }
    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if (mask.always || mask.distance)
    {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }
    return 1.0;
}

float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int maskChannel, float strength)
{
    float baked = GetBakedShadow(global.shadowMask, maskChannel);
    if (global.shadowMask.always)
    { 
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    if(global.shadowMask.distance)
    {
        shadow = lerp(baked, shadow, global.strength);
        return lerp(1.0, shadow, strength);
    }
    return lerp(1.0, shadow, strength * global.strength);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional,
    ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
  
    float shadow;
    if (directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, 
            directional.maskChannel, abs(directional.strength));
    }
    else
    {
        shadow = GetCascadedShadow(directional, global, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global, shadow, 
            directional.maskChannel, directional.strength);
    }
    return shadow;
}


float GetAdditionalShadow(AdditionalShadowData additional,
    ShadowData global, Surface surfaceWS)
{
    float tileIndex = additional.tileIndex;
    float3 lightPlane = additional.spotDirectionWS;
    if(additional.isPoint)
    {
        float faceOffset = CubeMapFaceID(-additional.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }
    float4 tileData = _AdditionalShadowTiles[tileIndex];
    float3 surfaceToLight = additional.lightPositionWS - surfaceWS.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);
    float3 normalBias = surfaceWS.interpolatedNormal * (distanceToLightPlane * tileData.w);
    float4 positionSTS = mul(
        _AdditionalShadowMatrices[tileIndex],
        float4(surfaceWS.position /*+ normalBias*/, 1.0));
    return FilterAdditionalShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

float GetAdditionalShadowAttenuation(AdditionalShadowData additional,
    ShadowData global, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    
    float shadow;
    if(additional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask,
            additional.maskChannel, abs(additional.strength));
    }
    else
    {
        shadow = GetAdditionalShadow(additional, global, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(global, shadow,
            additional.maskChannel, additional.strength);
    }
    return shadow;
}

#endif
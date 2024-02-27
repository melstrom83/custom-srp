#ifndef CUSTOM_GI_INCLUDED
#define CUSTOM_GI_INCLUDED

#if defined LIGHTMAP_ON
    #define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
    #define GI_VARYING_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
    #define TRANSFER_GI_DATA(attributes, varyings) \
        varyings.lightMapUV = attributes.lightMapUV * \
        unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(varyings) varyings.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYING_DATA
    #define TRANSFER_GI_DATA(attributes, varyings)
    #define GI_FRAGMENT_DATA(varyings) 0.0f
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(sampler_unity_Lightmap);

float3 SampleLightMap(float2 lightmapUV)
{
#if defined LIGHTMAP_ON
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, sampler_unity_Lightmap), lightmapUV, 
    float4(1.0, 1.0, 0.0, 0.0),
#if defined (UNITY_LIGHTMAP_FULL_HDR)
    true,
#else
    false,
#endif
    float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
#else
    return 0.0f;
#endif
}

struct GI
{
    float3 diffuse;
};

GI GetGI(float2 lightmapUV)
{
    GI gi;
    gi.diffuse = SampleLightMap(lightmapUV);
    
    return gi;
}


#endif
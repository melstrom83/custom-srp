#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attribute
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varying ShadowCasterPassVertex(Attribute attribute)
{
    Varying varying;
    UNITY_SETUP_INSTANCE_ID(attribute);
    UNITY_TRANSFER_INSTANCE_ID(attribute, varying);
    
    float3 positionWS = TransformObjectToWorld(attribute.positionOS.xyz);
    varying.positionCS = TransformWorldToHClip(positionWS);

    #if UNITY_REVERSED_Z
    varying.positionCS.z =
        min(varying.positionCS.z, varying.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    varying.positionCS.z =
        max(varying.positionCS.z, varying.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    varying.baseUV = attribute.baseUV * baseST.xy + baseST.zw;
    
    return varying;
}

void ShadowCasterPassFragment(Varying varying)
{
    UNITY_SETUP_INSTANCE_ID(varying);

    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, varying.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMap * baseColor;

    #if defined(_CLIPPING)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
}

#endif
#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attribute
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : POSITION_WS;
    float3 normalWS : NORMAL_WS;
    float2 baseUV : TEXCOORD0;
    GI_VARYING_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varying LitPassVertex(Attribute attribute)
{
    Varying varying;
    UNITY_SETUP_INSTANCE_ID(attribute);
    UNITY_TRANSFER_INSTANCE_ID(attribute, varying);
            
    TRANSFER_GI_DATA(attribute, varying);
    
    varying.positionWS = TransformObjectToWorld(attribute.positionOS.xyz);
    varying.positionCS = TransformWorldToHClip(varying.positionWS);

    varying.normalWS = TransformObjectToWorldNormal(attribute.normalOS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    varying.baseUV = attribute.baseUV * baseST.xy + baseST.zw;
    
    return varying;
}

float4 LitPassFragment(Varying varying) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(varying);

    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, varying.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMap * baseColor;

    Surface surface;
    surface.position = varying.positionWS;
    surface.normal = normalize(varying.normalWS);
    surface.view = normalize(_WorldSpaceCameraPos - varying.positionWS);
    surface.depth = -TransformWorldToView(varying.positionWS).z;
    surface.color = base.xyz;
    surface.alpha = base.w;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    

    #if defined(_CLIPPING)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    BRDF brdf = GetBRDF(surface);
    GI gi = GetGI(GI_FRAGMENT_DATA(varying));
    float3 color = GetLighting(surface, brdf, gi);
    return float4(color, surface.alpha);
}

#endif
#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

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
    varying.baseUV = TransformBaseUV(attribute.baseUV);
    
    return varying;
}

float4 LitPassFragment(Varying varying) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(varying);

    float4 base = GetBase(varying.baseUV);

    Surface surface;
    surface.position = varying.positionWS;
    surface.normal = normalize(varying.normalWS);
    surface.view = normalize(_WorldSpaceCameraPos - varying.positionWS);
    surface.depth = -TransformWorldToView(varying.positionWS).z;
    surface.color = base.xyz;
    surface.alpha = base.w;
    surface.metallic = GetMetallic(varying.baseUV);
    surface.smoothness = GetSmoothness(varying.baseUV);
    

    #if defined(_CLIPPING)
        clip(base.a - GetClipping(varying.baseUV);
    #endif

    BRDF brdf = GetBRDF(surface);
    GI gi = GetGI(GI_FRAGMENT_DATA(varying), surface);
    float3 color = GetLighting(surface, brdf, gi);
    return float4(color, surface.alpha);
}

#endif
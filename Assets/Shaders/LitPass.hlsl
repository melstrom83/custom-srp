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
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : POSITION_WS;
    float3 normalWS : NORMAL_WS;
#if defined (_NORMAL_MAP)
    float4 tangentWS : TANGENT_WS;
#endif
    float2 baseUV : TEXCOORD0;
#if defined (_DETAIL_MAP)
    float2 detailUV : TEXCOORD1;
#endif
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
#if defined (_NORMAL_MAP) 
    varying.tangentWS.xyz = TransformObjectToWorldDir(attribute.tangentOS.xyz);
    varying.tangentWS.w = attribute.tangentOS.w;
#endif 
    varying.baseUV = TransformBaseUV(attribute.baseUV);
#if defined (_DETAIL_MAP)    
    varying.detailUV = TransformDetailUV(attribute.baseUV);
#endif    
    
    return varying;
}

float4 LitPassFragment(Varying varying) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(varying);
  
    InputConfig config = GetInputConfig(varying.positionCS, varying.baseUV);
    //return float4(config.depth.xxx / 20.0, 1.0);
#if defined(_DETAIL_MAP)
    config.detailUV = varying.detailUV;
    config.useDetail = true;
#endif
#if defined(_MASK_MAP)
    config.useMask = true;
#endif
  
    float4 base = GetBase(config);

    Surface surface;
    surface.position = varying.positionWS;
#if defined (_NORMAL_MAP)
    surface.normal = NormalTangentToWorld(
      GetNormalTS(config), varying.normalWS, varying.tangentWS);
    surface.interpolatedNormal = varying.normalWS;
#else
    surface.normal = normalize(varying.normalWS);
    surface.interpolatedNormal = surface.normal;
#endif
    surface.view = normalize(_WorldSpaceCameraPos - varying.positionWS);
    surface.depth = -TransformWorldToView(varying.positionWS).z;
    surface.color = base.xyz;
    surface.alpha = base.w;
    surface.occlusion = GetOcclusion(config);
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    surface.fresnelStrength = GetFresnel(config);
    

#if defined(_CLIPPING)
    clip(base.a - GetClipping(config));
#endif

    BRDF brdf = GetBRDF(surface);
    GI gi = GetGI(GI_FRAGMENT_DATA(varying), surface, brdf);
    float3 color = GetLighting(surface, brdf, gi);
    color += GetEmission(config);
    return float4(color, GetFinalAlpha(surface.alpha));
}

#endif
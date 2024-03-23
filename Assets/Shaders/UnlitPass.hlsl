#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

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
#if defined (_DETAIL_MAP)
    float2 detailUV : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varying UnlitPassVertex(Attribute attribute)
{
    Varying varying;
    UNITY_SETUP_INSTANCE_ID(attribute);
    UNITY_TRANSFER_INSTANCE_ID(attribute, varying);
    
    float3 positionWS = TransformObjectToWorld(attribute.positionOS.xyz);
    varying.positionCS = TransformWorldToHClip(positionWS);

    varying.baseUV = TransformBaseUV(attribute.baseUV);
#if defined (_DETAIL_MAP)    
    varying.detailUV = TransformDetailUV(attribute.baseUV);
#endif
    return varying;
}

float4 UnlitPassFragment(Varying varying) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(varying);
  
    InputConfig config = GetInputConfig(varying.baseUV);
#if defined(_DETAIL_MAP)
    config.detailUV = varying.detailUV;
    config.useDetail = true;
#endif
#if defined(_MASK_MAP)
    config.useMask = true;
#endif
  
    float4 base = GetBase(config);

    #if defined(_CLIPPING)
        clip(base.a - GetClipping(config));
    #endif
    
    return base;
}

#endif
#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

struct Attribute
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
#if defined (_FLIPBOOK_BLENDING)
    float4 baseUV : TEXCOORD0;
    float flipbookBlend : TEXCOORD1;
#else
    float2 baseUV : TEXCOORD0;
 #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : TEXCOORD0;
#if defined (_FLIPBOOK_BLENDING)
    float3 flipbookUVB : FLIPBOOK;
#endif
#if defined (_DETAIL_MAP)
    float2 detailUV : TEXCOORD1;
#endif
#if defined (_VERTEX_COLORS)
    float4 color : COLOR;
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

    varying.baseUV.xy = TransformBaseUV(attribute.baseUV.xy);
#if defined (_FLIPBOOK_BLENDING)
    varying.flipbookUVB.xy = TransformBaseUV(attribute.baseUV.zw);
    varying.flipbookUVB.z = attribute.flipbookBlend;
#endif
#if defined (_DETAIL_MAP)    
    varying.detailUV = TransformDetailUV(attribute.baseUV);
#endif
#if defined (_VERTEX_COLORS)    
    varying.color = attribute.color;
#endif
    return varying;
}

float4 UnlitPassFragment(Varying varying) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(varying);
  
    InputConfig config = GetInputConfig(varying.baseUV);
#if defined(_VERTEX_COLORS)
    config.color = varying.color;
#endif
#if defined (_FLIPBOOK_BLENDING)
    config.flipbookUVB = varying.flipbookUVB;
    config.flipbookBlending = true;
#endif
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
    
    return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
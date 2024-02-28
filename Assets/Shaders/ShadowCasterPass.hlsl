#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

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
    
    varying.baseUV = TransformBaseUV(attribute.baseUV);
    
    return varying;
}

void ShadowCasterPassFragment(Varying varying)
{
    UNITY_SETUP_INSTANCE_ID(varying);

    float4 base = GetBase(varying.baseUV);

    #if defined(_CLIPPING)
        clip(base.a - GetClipping(varying.baseUV);
#endif
}

#endif
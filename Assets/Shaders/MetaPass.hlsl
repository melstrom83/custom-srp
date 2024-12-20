#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attribute
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : TEXCOORD0;
};

Varying MetaPassVertex(Attribute attribute)
{
    Varying varying;
    attribute.positionOS.xy =
    attribute.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    attribute.positionOS.z = attribute.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    varying.positionCS = TransformWorldToHClip(attribute.positionOS);
    varying.baseUV = TransformBaseUV(attribute.baseUV);
    
    return varying;
}

float4 MetaPassFragment(Varying varying) : SV_TARGET
{
    InputConfig config = GetInputConfig(varying.positionCS, varying.baseUV);

    float4 base = GetBase(config);

    Surface surface = (Surface)0;
    surface.color = base.rgb;
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    BRDF brdf = GetBRDF(surface);
    
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse + brdf.specular * brdf.roughness * 0.5, 1.0);
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);

    }
    else if (unity_MetaFragmentControl.y)
    {
        meta = float4(10, 10, 10, 1.0);
    }
    return meta;
}

#endif
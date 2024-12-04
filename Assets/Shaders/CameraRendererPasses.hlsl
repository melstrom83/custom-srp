#ifndef CUSTOM_CAMERA_RENDERER_INCLUDED
#define CUSTOM_CAMERA_RENDERER_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_SourceTexture);
SAMPLER(sampler_linear_clamp);

struct Varying
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varying DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varying varying;

    varying.positionCS = float4(
      vertexID <= 1 ?-1.0 : 3.0,
      vertexID == 1 ? 3.0 :-1.0,
      0.0, 1.0
    );

    varying.screenUV = float2(
      vertexID <= 1 ? 0.0 : 2.0,
      vertexID == 1 ? 2.0 : 0.0
    );
  
    if(_ProjectionParams.x < 0.0)
    {
      varying.screenUV.y = 1.0 - varying.screenUV.y;
    }

    return varying;
}

float4 CopyPassFragment(Varying varying) : SV_Target
{
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, varying.screenUV, 0);
}

#endif
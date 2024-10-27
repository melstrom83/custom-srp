#ifndef CUSTOM_POST_FX_STACK_PASSES_INCLUDED
#define CUSTOM_POST_FX_STACK_PASSES_INCLUDED

TEXTURE2D(_PostFXSource);
SAMPLER(sampler_linear_clamp);

float4 _ProjectionParams;

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

float4 GetSource(float2 screenUV)
{
  return SAMPLE_TEXTURE2D(_PostFXSource, sampler_linear_clamp, screenUV);
}

float4 CopyPassFragment(Varying varying) : SV_TARGET
{
    return GetSource(varying.screenUV);
}

#endif
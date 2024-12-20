#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

#include "UnityInput.hlsl"

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject

#define UNITY_PREV_MATRIX_M     (float4x4)0
#define UNITY_PREV_MATRIX_I_M   (float4x4)0

#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_P     unity_MatrixP
#define UNITY_MATRIX_VP    unity_MatrixVP

#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_I_P   unity_MatrixInvP
#define UNITY_MATRIX_I_VP  unity_MatrixInvVP

#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

//float3 TransformObjectToWorld (float3 positionOS)
//{
//	return mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
//}

//float4 TransformWorldToHClip(float3 positionWS)
//{
//	return mul(unity_MatrixVP, float4(positionWS, 1.0));
//}

float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA - pB, pA - pB);
}

float3 DecodeNormal(float4 sample, float scale)
{
  #if defined(UNITY_NO_DXT5nm)
    return normalize(UnpackNormalRGB(sample, scale));
  #else
    return normalize(UnpackNormalmapRGorAG(sample, scale));
  #endif
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
  float3x3 tangentToWorld = 
    CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
  
  return TransformTangentToWorld(normalTS, tangentToWorld);
}

bool IsOrthographicCamera()
{
  return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
  #if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
  #endif
  
  return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

#endif
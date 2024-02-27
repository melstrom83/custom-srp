#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;
CBUFFER_END

float4x4 unity_MatrixV;
float4x4 unity_MatrixP;
float4x4 unity_MatrixVP;

float4x4 unity_MatrixInvV;
float4x4 unity_MatrixInvP;
float4x4 unity_MatrixInvVP;

//float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos;

#endif
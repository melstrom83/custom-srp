Shader "CustomSRP/Lit"
{
	Properties 
	{
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Int) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
		[Toggle(_MASK_MAP)] _MaskMapToogle ("Mask Map", Int) = 0
		[NoScaleOffset] _MaskMap("Mask (MODS)", 2D) = "white" {}
		_Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
		_Occlusion ("Occlusion", Range(0.0, 1.0)) = 1.0
		_Smoothness("Smoothness", Range (0.0, 1.0)) = 0.5
		_Fresnel("Fresnel", Range (0.0, 1.0)) = 1.0
		[Toggle(_NORMAL_MAP)] _NormalMapToogle ("Normal Map", Int) = 0
		[NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(0.0, 1.0)) = 1.0
		[NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
		[HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)
		[Toggle(_DETAIL_MAP)] _DetailMapToogle ("Detail Map", Int) = 0
		_DetailMap("Details", 2D) = "linearGrey" {}
		[NoScaleOffset] _DetailNormalMap("Detail Normals", 2D) = "bump" {}
		_DetailAlbedo("Detail Albedo", Range(0.0, 1.0)) = 1
		_DetailSmoothness("Detail Smoothness", Range(0.0, 1.0)) = 1
		_DetailNormalScale("Detail Normal Scale", Range(0.0, 1.0)) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Int) = 4
	}
	
	SubShader
	{
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "LitInput.hlsl"
		ENDHLSL

		Pass
		{
			Tags 
			{
				"LightMode" = "CustomLit"
			}
			
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma shader_feature _MASK_MAP
			#pragma shader_feature _NORMAL_MAP
			#pragma shader_feature _DETAIL_MAP
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
			ENDHLSL
		}

	
		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}
			
			ColorMask 0
			Cull Front
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Tags
			{
				"LightMode" = "Meta"
			}

			Cull Off
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex MetaPassVertex
			#pragma fragment MetaPassFragment
			#include "MetaPass.hlsl"
			ENDHLSL
		}
	}
			
	
	CustomEditor "CustomShaderGUI"
}
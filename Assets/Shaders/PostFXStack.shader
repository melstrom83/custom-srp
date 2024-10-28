Shader "Hidden/CustomSRP/PostFXStack"
{
	//Properties 
	//{
	//	_BaseMap("Texture", 2D) = "white" {}
	//	_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	//	_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	//	[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Int) = 0
	//	[Toggle(_MASK_MAP)] _MaskMapToogle ("Mask Map", Int) = 0
	//	[NoScaleOffset] _MaskMap("Mask (MODS)", 2D) = "white" {}[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
	//	[Toggle(_DETAIL_MAP)] _DetailMapToogle ("Detail Map", Int) = 0
	//	_DetailMap("Details", 2D) = "linearGrey" {}
	//	_DetailAlbedo("Detail Albedo", Range(0.0, 1.0)) = 1
	//	[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0
	//	[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Int) = 1
	//	[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Int) = 4
	//}
	
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "PostFXStackPasses.hlsl"
		ENDHLSL

		Pass
		{
			Name "Copy"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DefaultPassVertex
			#pragma fragment CopyPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Horizontal"
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DefaultPassVertex
			#pragma fragment BloomHorizontalPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Vertical"
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DefaultPassVertex
			#pragma fragment BloomVerticalPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "Bloom Combine"
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex DefaultPassVertex
			#pragma fragment BloomCombinePassFragment
			ENDHLSL
		}
	}
}
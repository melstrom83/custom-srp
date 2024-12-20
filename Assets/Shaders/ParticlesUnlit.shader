Shader "CustomSRP/Partices/Unlit"
{
	Properties 
	{
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[Toggle(_VERTEX_COLORS)] _VertexColors("Vertex Colors", Float) = 0
		[Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending("Flipbook Blending", Float) = 0
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Int) = 0
		[Toggle(_MASK_MAP)] _MaskMapToogle ("Mask Map", Int) = 0
		[NoScaleOffset] _MaskMap("Mask (MODS)", 2D) = "white" {}
		[Toggle(_DISTORTION)] _Distortion ("Distortion", Int) = 0
		[NoScaleOffset] _DistortionMap ("Distortion Vectors", 2D) = "bumb" {}
		_DistortionStrength("Distortion Strength", Range(0.0, 0.2)) = 0.1
		[Toggle(_DETAIL_MAP)] _DetailMapToogle ("Detail Map", Int) = 0
		_DetailMap("Details", 2D) = "linearGrey" {}
		_DetailAlbedo("Detail Albedo", Range(0.0, 1.0)) = 1
		[Toggle(_NEAR_FADE)] _NearFade ("Near Fade", Int) = 0
		_NearFadeDisctance("Near Fade Distance", Range(0.0, 10.0)) = 1.0
		_NearFadeRange("Near Fade Range", Range(0.0, 10.0)) = 1.0
		[Toggle(_SOFT_PARTICLES)] _SoftParticles ("Soft Particles", Int) = 0
		_SoftParticleDisctance("Soft Particle Distance", Range(0.0, 10.0)) = 0.0
		_SoftParticleRange("Soft Particle Range", Range(0.0, 10.0)) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Int) = 4
	}
	
	SubShader
	{
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "UnlitInput.hlsl"
		ENDHLSL

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			
			HLSLPROGRAM
			#pragma shader_feature _VERTEX_COLORS
			#pragma shader_feature _FLIPBOOK_BLENDING
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _MASK_MAP
			#pragma shader_feature _DISTORTION
			#pragma shader_feature _DETAIL_MAP
			#pragma shader_feature _NEAR_FADE
			#pragma shader_feature _SOFT_PARTICLES
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
			ENDHLSL
		}
	}
	
	CustomEditor "CustomShaderGUI"
}
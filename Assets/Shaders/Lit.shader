Shader "CustomSRP/Lit"
{
	Properties 
	{
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Int) = 0
		_Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
		_Smoothness("Smoothness", Range (0.0, 1.0)) = 0.5
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Int) = 4
	}
	
	SubShader
	{
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
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
			ENDHLSL
		}
	}
	
	SubShader
	{
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
	}
			
	
	CustomEditor "CustomShaderGUI"
}
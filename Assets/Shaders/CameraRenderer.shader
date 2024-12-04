Shader "Hidden/CustomSRP/CameraRenderer"
{
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "CameraRendererPasses.hlsl"
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
	}
}
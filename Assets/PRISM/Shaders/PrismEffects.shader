Shader "Hidden/PrismEffects" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE
	
		#include "UnityCG.cginc"
		#include "Prism.cginc"
		#pragma fragmentoption ARB_precision_hint_fastest
		//#pragma fragmentoption ARB_precision_hint_nicest
		#pragma multi_compile _ PRISM_USE_BLOOM PRISM_USE_STABLEBLOOM
		#pragma multi_compile _ PRISM_HDR_BLOOM PRISM_SIMPLE_BLOOM
		#pragma shader_feature PRISM_BLOOM_SCREENBLEND
		#pragma shader_feature PRISM_UIBLUR 
		
		//Change this to shader feature, since we don't change tonemap types in-game
		#pragma multi_compile _ PRISM_FILMIC_TONEMAP PRISM_ROMB_TONEMAP PRISM_ACES_TONEMAP
		
		#pragma shader_feature PRISM_USE_NIGHTVISION
		
		#pragma multi_compile _ PRISM_DOF_LOWSAMPLE PRISM_DOF_MEDSAMPLE
		#pragma shader_feature PRISM_DOF_USENEARBLUR
		#pragma multi_compile _ PRISM_GAMMA_LOOKUP PRISM_LINEAR_LOOKUP 	 	
		#pragma multi_compile _ PRISM_USE_EXPOSURE 
		
		#pragma target 3.0
		//#pragma glsl
	ENDCG
	
	
	SubShader {
	//Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}

	ZTest Always Cull Off ZWrite Off
	
	//Combine
	//-----------------------------------------------------
	// Pass 0
	//-----------------------------------------------------		
	Pass{
			
		CGPROGRAM
		#pragma vertex vertPRISM
		#pragma fragment fragFinal
		ENDCG			
	}
	
	
	
	
	
	
	
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	FallBack off
}

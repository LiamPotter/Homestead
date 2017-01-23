Shader "Hidden/PrismEffectsTertiary" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"
		#include "Prism.cginc"
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma shader_feature _ PRISM_UIBLUR
		#pragma shader_feature _ PRISM_HDR_BLOOM PRISM_SIMPLE_BLOOM
		#pragma target 3.0
	ENDCG
	
	SubShader {
	//Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}

	ZTest Always Cull Off ZWrite Off
	
	//0 - Sun pre
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSunPrepass
		ENDCG
	}
	
	//1 - Sun blur
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSunBlur
		ENDCG
	}
	
	//2 - Combie
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSunCombine
		ENDCG
	}
	
	//3 - Debug
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragSunDebug
		ENDCG
	}
	
	//4 - bloom debug
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragBloomDebug
		ENDCG
	}
	
	//5 - depth
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDepthCopy
		ENDCG
	}
	
	//6 - depthdebug
	Pass {  
		CGPROGRAM
			#pragma vertex vertPRISM
			#pragma fragment fragDepthDebug
		ENDCG
	}
	
	
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	FallBack off
}

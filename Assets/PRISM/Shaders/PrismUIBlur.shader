Shader "Prism/UI/PrismUIBlur"
{
	Properties
	{
		_Flip ("Flip", int) = 1
	}
	SubShader
	{
        // Draw ourselves after all opaque geometry
        Tags { "Queue" = "Transparent-1" }
        
		// No culling or depth
		Cull off ZWrite Off ZTest On
		
        // Grab the screen behind the object into _GrabTexture
        GrabPass { "_GrabTex" }
        
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature PRISM_UIBLUR
			#pragma target 3.0
			
			#include "UnityCG.cginc"

			struct data {
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f {
			    float4 position : POSITION;
			    float4 screenPos : TEXCOORD0;
				float2 uv : TEXCOORD1;
				float4 color : COLOR;
			};
			
			#if PRISM_UIBLUR
			uniform sampler2D _BlurredScreenTex;
			uniform half4 _BlurredScreenTex_TexelSize;
			#else
			uniform sampler2D _GrabTex;
			uniform half4 _GrabTex_TexelSize;
			#endif

			v2f vert (data i)
			{
			    v2f o;
			    o.position = mul(UNITY_MATRIX_MVP, i.vertex);
			    o.screenPos = o.position;
			    o.uv = i.uv;	
			    o.color = i.color;	    
			    
	        	#if UNITY_UV_STARTS_AT_TOP		
    				#if PRISM_UIBLUR
	    			//if (_BlurredScreenTex_TexelSize.y < 0.0)
	        			//o.screenPos.y = 1.0 - o.screenPos.y;
					#else
	    			//if (_GrabTex_TexelSize.y < 0.0)
	        			//o.screenPos.y = 1.0 - o.screenPos.y;
					#endif
		    	#endif	
			    return o;
			}
			
			int _Flip;
			
			static const int DOF_SAMPLES = 5;
			
			half3 BlurColor(sampler2D blurSampler, float2 pixelSize, half3 col, float2 uv)
			{
				#if SHADER_TARGET < 30
				return col;
				#else 
				
				float accumW = 0.0;
				float3 accumCol = (float3)0.0;
					
				#if SHADER_API_D3D11
				[unroll]
				#endif
				for (int j = -DOF_SAMPLES; j < DOF_SAMPLES; j += 1)
				#if SHADER_API_D3D11
				[unroll]
				#endif
				for (int i = -DOF_SAMPLES; i < DOF_SAMPLES; i += 1){
					float2 offset = pixelSize * float2(i + j, j - i);
					float3 col = tex2D(blurSampler, uv + offset).rgb;
					accumCol += min(col, 1.0);
					accumW += 1.0;
				}
				
				float3 ret = accumCol/accumW;
				
				return ret;
				#endif
			}
			
			half3 GetPreBlurredCol(sampler2D blurSampler, float2 pixelSize, half3 col, float2 uv)
			{
				return tex2D(blurSampler, uv).rgb;
			}

			half4 frag (v2f i) : SV_Target
			{
				i.screenPos.y *= _Flip;
			    float2 screenPos = i.screenPos.xy / i.screenPos.w;
		        screenPos.x = (screenPos.x + 1) * 0.5;
				screenPos.y = 1-(screenPos.y + 1) * 0.5;
				
				
				half3 col = (half3)0.0;//tex2D(_GrabTexture, screenPos).rgb;
				
				#if PRISM_UIBLUR
				col = GetPreBlurredCol(_BlurredScreenTex, _BlurredScreenTex_TexelSize.xy, col, screenPos);
				#else
				col = BlurColor(_GrabTex, _GrabTex_TexelSize.xy, col, screenPos);
				#endif
				
				col *= i.color.rgb;
    
				// just invert the colors
				//col = col * _Tint;
				return half4(col.rgb, i.color.a);
			}
			ENDCG
		}
	}
}

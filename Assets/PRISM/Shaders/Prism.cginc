/*
																	 ;
		                                                            ;;     
                                                                   ;;;     
      PRISM ---All-In-One Post-Processing for Unity              .;;;;     
      Created by  Alex Blaikie, @Gadget_Games                   :;;;;;     
                                                               ;;;;;;;     
                                                              ;;;;;;;;     
                                                             ;;;;;;;;;     
                                                           ,;;;;;;;;;;     
                                                          ;;;;;;;;;;;;     
                                                         ;;;;;;;;;;;;,     
                                                        ;;;;;;;;;;;,,,     
                                                      `;;;;;;;;;;;,,,,     
                                                     ,;;;;;;;;;;,,,,,,     
                                                    ;;;;;;;;;;:,,,,,,,     
                                                   ;;;;;;;;;;,,,,,,,,,     
                                                  ;;;;;;;;;,,,,,,,,,,,     
                                                `;;;;;;;;:,,,,,,,,,,,,     
                                               :;;;;;;;;,,,,,,,,,,,,,,     
                                              ;;;;;;;;,,,,,,,,,,,,,,,,     
                                             ;;;;;;;:,,,,,,,,,,,,,,,,,     
                                            ;;;;;;;,,,,,,,,,,,,,,,,,,,     
                                          .;;;;;;,,,,,,,,,,,,,,,,,,,,,     
                                         :;;;;;;,,,,,,,,,,,,,,,,,,,,,,     
                                        ;;;;;;,,,,,,,,,,,,,,,,,,,,,,,:     
                                       ;;;;;,,,,,,,,,,,,,,,,,,,,,,:;;;     
                                      ;;;;;,,,,,,,,,,,,,,,,,,,,;;;;;;;     
                                    ,;;;;,,,,,,,,,,,,,,,,,,:;;;;;;;;;;     
                                   ;;;;,,,,,,,,,,,,,,,,,:;;;;;;;;;;;;;     
                                  ;;;;,,,,,,,,,,,,,,:;;;;;;;;;;;;;;;;;     
                                 ;;;,,,,,,,,,,,,,:;;;;;;;;;;;;;;;:.        
        @@@@@@@@;              `;;:,,,,,,,,,,:;;;;;;;;;;;;;;,              
        @@@@@@@@@@            ,;;,,,,,,,,,:;;;;;;;;;;;:`                   
        @@@@@@@@@@@          ;;,,,,,,,,;;;;;;;;;:.                         
        @@@@;:'@@@@#       @;:,,,,,:;;;;;;;:`                              
        @@@@    @@@#      ,@:,,,:;;;;:.                                    
     ,::@@@@::::@@@@,,,,,,#@@;;;,`                                         
        @@@@    ,@@@     #@@@'                                             
        @@@@    +@@@ '''':'''.'''`  ''#@@@;  +@@@,                         
        @@@@   .@@@@@@@@@'@@@,@@@`  #@@@@@@@:@@@@@#                        
        @@@@@@@@@@@,@@@@@'@@@,@@@.  #@@@@@@@@@@@@@@                        
        @@@@@@@@@@@ @@@@@'@@@,@@@@` #@@@@@@@@@@@@@@                        
        @@@@@@@@@+  @@@  '@@@, +@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,  @@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@, @@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@@@@ #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@@@` #@@@ ;@@@  @@@@                        
        @@@@        @@@  '@@@,@@;   #@@@ ;@@@  @@@@                        
*/                                                                           
		
	//s2 = a becomes smallest
	//b becomes biggest
	//switches a and b into smallest and biggest
	//
	//mn3 a b c = sorts from low to high
	//mx3 a b c = sorts from high to low
	
	
	//#define PRISM_DEBUG 0
	
	//You must set this to 1 if you are using a two-camera setup with depth effects
	#define PRISM_TWOCAMSETUP 0
	
	//#define PRISM_DOF_USENEARBLUR 1
		
	#define s2(a, b)				temp = a; a = min(a, b); b = max(temp, b);
	#define mn3(a, b, c)			s2(a, b); s2(a, c);
	#define mx3(a, b, c)			s2(b, c); s2(a, c);

	#define mnmx3(a, b, c)				mx3(a, b, c); s2(a, b);                                   // 3 exchanges
	#define mnmx4(a, b, c, d)			s2(a, b); s2(c, d); s2(a, c); s2(b, d);                   // 4 exchanges
	#define mnmx5(a, b, c, d, e)		s2(a, b); s2(c, d); mn3(a, c, e); mx3(b, d, e);           // 6 exchanges
	#define mnmx6(a, b, c, d, e, f) 	s2(a, d); s2(b, e); s2(c, f); mn3(a, b, c); mx3(d, e, f); // 7 exchanges
    
    //If potato, shouldn't be using stochastic anyway
	#if SHADER_TARGET < 30
	  	static const int SAMPLE_COUNT = 1;
	#else 
	    #if PRISM_SAMPLE_LOW
	  	static const int SAMPLE_COUNT = 16;
	    #elif PRISM_SAMPLE_MEDIUM
	   	static const int SAMPLE_COUNT = 32;
	    #elif PRISM_SAMPLE_HIGH
	    static const int SAMPLE_COUNT = 64;
	    #else
	    static const int SAMPLE_COUNT = 12;    
	    #endif
	#endif
    
	#if SHADER_TARGET < 30
	  	static const int DOF_SAMPLES = 1;
	#else 
	    #if PRISM_DOF_LOWSAMPLE
	    static const int DOF_SAMPLES = 2;
	    #elif PRISM_DOF_MEDSAMPLE
	 	static const int DOF_SAMPLES = 3;//Default = 3
	    #else 
	    static const int DOF_SAMPLES = 5;//THIS IS UNUSED
	    #endif
	#endif
            
	static const float4 ONES = (float4)1.0;// float4(1.0, 1.0, 1.0, 1.0);
	static const float4 ZEROES = (float4)0.0;
	sampler2D _MainTex;
	half4 _MainTex_TexelSize;
	sampler2D _CameraGBufferTexture3;
	
	//Normal
	sampler2D _CameraDepthTexture;
	
	//For 2-cam setup
	sampler2D_float _LastCameraDepthTexture1;
	
	//Set by scripts
	uniform float useNoise = 0.0;
	uniform float useNightVision = 0.0;
	
	//START SUN VARIABLES
	uniform half4 _SunColor;
	uniform half4 _SunPosition;
	uniform half4 _SunThreshold;
	uniform float _SunWeight;
	sampler2D _RaysTexture;
	//END SUN VARIABLES
	
	//START BLOOM VARIABLES=====================
	int currentIteration = 1;
	uniform float _BloomIntensity = 1.5;	
	uniform float _DirtIntensity = 1.5;	
	uniform float _BloomThreshold = 0.5;
	
	#if SHADER_TARGET < 30
	uniform sampler2D _Bloom1;
	#else
	uniform sampler2D _Bloom1;
	uniform sampler2D _Bloom2;
	uniform sampler2D _Bloom3;
	uniform sampler2D _Bloom4;
	#endif

	uniform sampler2D _BloomAcc;
	sampler2D _CurTex;
	sampler2D _AdaptExposureTex;
	uniform sampler2D _DirtTex;
	//END BLOOM VARIABLES=======================
	
	//START VIGNETTE VARIABLES=====================
	uniform float _VignetteStart = 0.5;
	uniform float _VignetteEnd = 0.45;
	uniform float _VignetteIntensity = 1.0;
	uniform half4 _VignetteColor;
	//END VIGNETTE VARIABLES=======================
	
	//START NIGHTVISION VARIABLES==================
	#if PRISM_USE_NIGHTVISION
	sampler2D _CameraGBufferTexture0;
	float4 _MainTex_ST;
	uniform float4 _NVColor;
	uniform float4 _TargetWhiteColor;
	uniform float _BaseLightingContribution;
	uniform float _LightSensitivityMultiplier;
	#endif
	//END NIGHTVISION VARIABLES=======================	
	
	//START NOISE VARIABLES========================
	uniform sampler2D _GrainTex;
	uniform float4 _GrainOffsetScale;
	uniform float2 _GrainIntensity;
	uniform int2 _RandomInts;
	//END NOISE VARIABLES========================
	
	//START CHROMATIC VARIABLES====================
	uniform float2 _BlurVector;
	uniform float _BlurGaussFactor;
	
	uniform float2 _ChromaticStartEnd;
	uniform float _ChromaticIntensity = 0.05;
	
	uniform float4 _ChromaticParams;
	//END CHROMATIC VARIABLES======================
	
	//START EXPOSURE AND TONEMAP VARIABLES=======================
	uniform float _TonemapStrength;
	uniform float4 _ToneParams;
	uniform float4 _SecondaryToneParams;
	
	uniform float _ExposureSpeed;	
	uniform float _ExposureMiddleGrey;
	uniform float _ExposureLowerLimit;
	uniform float _ExposureUpperLimit;	
	uniform sampler2D _BrightnessTexture;
	
	uniform float _Gamma;
	//END EXPOSURE VARIABLES=======================
	
	//START DOF VARIABLES==========================
	uniform sampler2D _DoF1;
	uniform float _DofUseNearBlur = 0.0;
	uniform float _DofFactor = 64.0;
	uniform float _DofRadius = 2.0;
	uniform float _DofUseLerp = 0.0;
	
	uniform float _DofFocusPoint = 15.0;	
	uniform float _DofFocusDistance = 35.0;	
	uniform float _DofNearFocusDistance;
	
	//0.99999 if we DONT Want to blur the skybox
	//And transparents
	uniform float _DofBlurSkybox;	
	//END DOF VARIABLES============================
	
	//Start LUT variables
	#if SHADER_TARGET < 30    
	
	#else
	uniform sampler3D _LutTex;
	#endif
	uniform float _LutScale;
	uniform float _LutOffset;
	uniform float _LutAmount;
	
	#if SHADER_TARGET < 30    
	
	#else
	uniform sampler3D _SecondLutTex;
	#endif
	uniform float _SecondLutScale;
	uniform float _SecondLutOffset;
	uniform float _SecondLutAmount;
	//End LUT variables
	
	//Start Sharpen variables
	uniform float _SharpenAmount = 1.0;
	//End sharpen variables
	
	//Start fog variables
	uniform float _FogHeight = 2.0;
    uniform float4 _FogColor;
	uniform float _FogBlurSkybox = 1.0;
    uniform float4 _FogEndColor;
	uniform float _FogStart = 2.0;
	uniform float _FogDistance = 2.0;
	uniform float _FogIntensity = 1.0;
	uniform float _FogVerticalDistance = 1.0;
	uniform float4x4 _InverseView;
		
    samplerCUBE _SkyboxCubemap;
    uniform float4 _SkyboxCubemap_HDR;
    uniform float4 _SkyboxTint;
    uniform float _SkyboxExposure;
    uniform float _SkyboxRotation;
	//End fog variables
	
	//AO
	uniform float _AOIntensity;
	uniform float _AOLuminanceWeighting;
	uniform sampler2D _AOTex;
	
	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		float2 texcoord1 : TEXCOORD1;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		
        #if UNITY_UV_STARTS_AT_TOP
			float2 uv2 : TEXCOORD1;
		#endif
	};
	
	float3 Sample(float2 uv, float2 offsets, float weight)//float mipbias - done with weight
	{
	    float2 PixelSize = _MainTex_TexelSize.xy;
	    return tex2D(_MainTex, uv + offsets * PixelSize).rgb * weight;
	}    	
	float PRISM_SAMPLE_DEPTH_TEXTURE(float2 uv)
	{
		#if PRISM_TWOCAMSETUP
		return SAMPLE_DEPTH_TEXTURE(_LastCameraDepthTexture1, uv);
		#else
		return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
		#endif
	}
	
    //Noises====================
    //
    // Interleaved gradient function from CoD AW.
    // http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
    float gnoise(float2 uv, float2 offs)
    {
        uv = uv / _MainTex_TexelSize.xy + offs;
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        return frac(magic.z * frac(dot(uv, magic.xy)));
    }
    
    float nrand(float2 uv, float dx, float dy)
    {
        uv += float2(dx, dy);// + _Time.x);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }
    
    float nrandtime(float2 uv, float dx, float dy)
    {
        uv += float2(dx, dy + _Time.x);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }
    //End noises================
	
	//START BLURS============================================================

	//From Kawase, 2003 GDC presentation
	//http://developer.amd.com/wordpress/media/2012/10/Oat-ScenePostprocessing.pdf
	float4 KawaseBlur(sampler2D s, float2 uv, int iteration)//float2 pixelSize, 
	{
		//Dont need anymorefloat2 texCoordSample = 0;
		float2 pixelSize = _MainTex_TexelSize.xy;//(float2)(1.0 / _ScreenParams.xy); //_MainTex_TexelSize.wx;
		float2 halfPixelSize = pixelSize / 2.0f;
		float2 dUV = (pixelSize.xy * float(iteration)) + halfPixelSize.xy;
		float4 cOut;
		//We probably save like 1 operation from this, lol
		float4 cheekySample = float4(uv.x, uv.x, uv.y, uv.y) + float4(-dUV.x, dUV.x, dUV.y, dUV.y);
		float4 cheekySample2 = float4(uv.x, uv.x, uv.y, uv.y) + float4(dUV.x, -dUV.x, -dUV.y, -dUV.y);
		
		// Sample top left pixel
		cOut = tex2D(s, cheekySample.rb);
		// Sample top right pixel
		cOut += tex2D(s, cheekySample.ga);
		// Sample bottom right pixel
		cOut += tex2D(s, cheekySample2.rb);
		// Sample bottom left pixel
		cOut += tex2D(s, cheekySample2.ga);
		// Average
		cOut *= 0.25f;
		return cOut;
	}
	
	
	//END BLURS=====================================================================
		
	v2f vertPRISM (appdata_t v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord;
		
    	#if UNITY_UV_STARTS_AT_TOP
    		o.uv2 = v.texcoord.xy;				
    		if (_MainTex_TexelSize.y < 0.0)
    			o.uv.y = 1.0 - o.uv.y;
    	#endif
		
		return o;
	}
			
	//Blurs
	float4 fragBlurBox (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
	
		//float4 col = tex2D(_MainTex, i.texcoord);
		float4 col;
		col = KawaseBlur(_MainTex, uv, currentIteration);//BlurColor(i.texcoord).rgb;
		
		return col;
	}  	
		
	///.. -1 ..
	///-1  5 -1
	///.. -1 ..
	//Tried prewitt sharpen - holy god that looks terrible. 
	half4 sharpen(float2 uv) {//Halfs because this don't work in HDR anyway
	
		half2 PixelSize = _MainTex_TexelSize.xy;
	
		half4 midCol;
		half4 col = float4 (0.0, 0.0, 0.0, 0.0);
		half4 temp;

		// Top
		temp = tex2D(_MainTex, uv + half2(0.0, -PixelSize.y));
		col += temp * -1.0;

		// Middle
		temp = tex2D(_MainTex, uv + half2(-PixelSize.x, 0.0));
		col += temp * -1.0;

		midCol = tex2D(_MainTex, uv);
		col += midCol * 5.0;

		temp = tex2D(_MainTex, uv + half2(PixelSize.x, 0.0));
		col += temp * -1.0;

		// Bottom
		temp = tex2D(_MainTex, uv + half2(0.0, PixelSize.y));
		col += temp * -1.0;

		return lerp(midCol, col, _SharpenAmount);
	}

	
	half4 fragSharpen (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		return sharpen(uv);
	}
	
	//Standard filmic tonemapping (heji dawson)
	half3 filmicTonemap(in float3 col)
	{
	    col = max(0, col - _SecondaryToneParams.x);
	    col = (col * (col * _ToneParams.x + _ToneParams.y)) / (col * (col * _ToneParams.x + _ToneParams.z)+ _SecondaryToneParams.y);
	    //#endif
	    col *= col;
	    
	    return col;
	}	
			
	//Roman Galashov (RomBinDaHouse) operator - https://www.shadertoy.com/view/MssXz7
	half3 filmicTonemapRomB(in float3 col)
	{
	    col.rgb = exp( _ToneParams.x / ( _ToneParams.y * col.rgb + _ToneParams.z ) );
		
		return col;
	}
	
	//ACES filmic tonemapping curve. Used in UE4 https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
	half3 ACESFilm( float3 x )
	{
	    float aa = _ToneParams.x;
	    float bb = _ToneParams.y;
	    float cc = _ToneParams.z;
	    float dd = _SecondaryToneParams.x;
	    float ee = _SecondaryToneParams.y;
	    x = saturate((x*(aa*x+bb))/(x*(cc*x+dd)+ee));
	    
	    return x;
	}
	
	inline float3 ApplyExposure(float3 col, float linear_exposure)
	{
		return col *= max(_ExposureLowerLimit, min(_ExposureUpperLimit, _ExposureMiddleGrey / linear_exposure));
	}	
	
	//Tiltshift - unused atm
	float WeightIrisMode (float2 uv)
	{
		float2 tapCoord = uv*2.0-1.0;
		return (abs(tapCoord.y * _ChromaticIntensity));
	}
	
	inline half4 lookupGamma(sampler3D lookup, float4 c, half2 uv)
	{
		//float4 c = tex2D(_MainTex, uv);
		c.rgb = tex3D(lookup, c.rgb * _LutScale + _LutOffset).rgb;
		return c;
	}

	inline half4 lookupLinear(sampler3D lookup, float4 c, half2 uv)
	{
		//float4 c = tex2D(_MainTex, uv);
		c.rgb= sqrt(c.rgb);
		c.rgb = tex3D(lookup, c.rgb * _LutScale + _LutOffset).rgb;
		c.rgb = c.rgb*c.rgb; 
		return c;
	}
	
	//TV-style vignette (more rectangular than circular)
	inline float4 tvVignette(float4 col, float2 uv)
	{
		float vignette = (2.0 * 2.0 * uv.x * uv.y * (1.0 - uv.x) * (1.0 - uv.y));
		col.rgb *= (float3)(pow(vignette, _VignetteIntensity));
		return col;
	}
	
	inline float GetChromaticVignetteAmount(float2 uv)
	{
		if(_ChromaticParams.z > 0.0)//Vertical
		{
			uv.y = uv.x;
		}
		float distChrom = distance(uv, float2(0.5,0.5));
		distChrom = smoothstep(_ChromaticParams.x,_ChromaticParams.y,distChrom);
		return distChrom;
	}
		
	inline float3 ChromaticAberration(float3 col, float2 uv)
	{
		float distChrom = 0.0;
	
		distChrom = GetChromaticVignetteAmount(uv);
		float amount = _ChromaticIntensity;
		amount *= distChrom;

		float3 tempChromaCol = (float3)(1.0);
		tempChromaCol.r = tex2D( _MainTex, float2(uv.x+amount,uv.y+amount) ).r;
		tempChromaCol.g = col.g;
		tempChromaCol.b = tex2D( _MainTex, float2(uv.x-amount,uv.y-amount) ).b;

		tempChromaCol *= (1.0 - amount * 0.5);
		col = lerp(col, tempChromaCol, distChrom);	
		
		return col;
	}
	
	inline float3 IncreaseContrast(float3 col, float contrast)
	{
		return ((col.rgb - 0.5) * max(contrast + 1.0, 0.0)) + 0.5;
	}
	
	inline float3 GlobalFog(float3 col, float2 uv, float4 blmAmount)
	{
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
		float depth_o = LinearEyeDepth(depth_r);	
		
        float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
        float3 viewPos = float3((uv * 2 - 1) / p11_22, -1) * depth_o;
        float4 worldPos = mul(_InverseView, float4(viewPos, 1));

		float d = length(worldPos - _WorldSpaceCameraPos);
		float firstBit = (d-_FogStart);
		float secondBit = (_FogDistance-_FogStart);
		float l = saturate(firstBit/secondBit);
		
		float4 finalFogCol = _FogEndColor;
		
		secondBit *= _FogHeight;
		float fl = saturate(firstBit/secondBit);
		
		finalFogCol = lerp(finalFogCol, _FogColor, fl);
		
		//UNCOMMENT THESE LINES FOR VERTICAL FOG #VERTICALFOG
		//const float _FogVerticalDistance = 22.0;
    	//float vertFade = abs(((worldPos.y - _FogHeight) / _FogVerticalDistance));
    	//vertFade = clamp(vertFade, 0.0, 1.0);
		//finalFogCol = lerp(finalFogCol, ZEROES, vertFade);
		
	   	//If we have bloom, color the fog towards the blooms color based on it's intensity
	   	#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM
	   	finalFogCol.rgb += blmAmount.rgb;
		#endif
		
        //If 1 > 0.99999 when _DofBlurSkybox is set
        if(depth_r > _FogBlurSkybox)
        {
        	l = 0.0;
        }
		
	    return float3(lerp(col,finalFogCol.rgb, l*finalFogCol.a));
	}
	
	float4 fragBloomPrePassCheap (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;	
		#endif
		
		float4 blm = ZEROES;
		blm.rgb = tex2D(_MainTex, uv).rgb; 
		
		//Actually prefer doing it this way, simply subtracting the threshold. Gives a bit richer bloom colors
		//If we're using the near blur, not using low or med dof
		#if !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR
			//This is fullscreen blur
			blm.rgb = max(half3(0,0,0), blm.rgb-(float3)_BloomThreshold);
			blm.rgb *= _BloomIntensity;
			blm.a = Luminance(blm.rgb);
		#else 
		//Just standard bloom
			blm.rgb = max(half3(0,0,0), blm.rgb-(float3)_BloomThreshold);
			blm.rgb *= _BloomIntensity;
			blm.a = Luminance(blm.rgb);
		#endif
		
		return blm;
	}
	
	inline float3 CombineBloomOld(float3 col,float2 uv)
	{
		return col;
	}
	
	inline float3 CombineBloom(float3 col,float2 uv)
	{
		#if	PRISM_HDR_BLOOM 
		
		#if SHADER_TARGET > 20  
		float3 addedBlm = tex2D(_Bloom1,uv).rgb * 1.8;
		
			#if PRISM_USE_STABLEBLOOM
			float3 accValue = tex2D(_BloomAcc, uv).rgb * 1.8;
			addedBlm = lerp(accValue.rgb, addedBlm.rgb, 0.7);
			#endif
		
		addedBlm += tex2D(_Bloom2,uv).rgb * 1.5;
		addedBlm += tex2D(_Bloom3,uv).rgb;
		addedBlm += tex2D(_Bloom4,uv).rgb;
		
		addedBlm *= _BloomIntensity;
		
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, uv).r;
			addedBlm.rgb = ApplyExposure(addedBlm.rgb, linear_exposure);
			#endif
		
		return col + addedBlm;
		#else 
		return ONES;
		#endif
		
		#else
		float4 blmTex = tex2D(_Bloom1, uv);
		
			#if PRISM_USE_STABLEBLOOM
			float3 accValue = tex2D(_BloomAcc, uv).rgb;
			blmTex.rgb = lerp(accValue.rgb, blmTex.rgb, 0.7);
			#endif
		
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, uv).r;
			blmTex.rgb = ApplyExposure(blmTex.rgb, linear_exposure);
			#endif
		
			#if !PRISM_BLOOM_SCREENBLEND
			col.rgb += blmTex.rgb;
			#endif
			
			#if PRISM_BLOOM_SCREENBLEND			
			col.rgb = 1-(1-blmTex.rgb)*(1-col.rgb);
			#endif
		#endif
		
		return col;
	}
	
	float4 fragBloomDebug (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;	
		#endif
		
		float4 blm = tex2D(_MainTex, uv);
		
		#if PRISM_UIBLUR
		blm.rgb = lerp(blm.rgb, (float3)0.0, min((_BloomThreshold - blm.a), 1.0));
		blm.rgb *= _BloomIntensity;
		#endif
		
		return blm;
	}
	
	//Diamond-shaped bokeh blur.
	float4 bokehBlurFull(float2 uv, float2 uvSec, float midEyeDepth){
		
		#if UNITY_UV_STARTS_AT_TOP
		float4 origCol = tex2D(_MainTex, uvSec);
		#else
		float4 origCol = tex2D(_MainTex, uv);
		#endif
		
		#if SHADER_TARGET < 30   
			return origCol;
		#else
	
			float2 pixelSize = _MainTex_TexelSize.xy;
			
			float4 ret = ZEROES;
		
			float4 accumCol = ZEROES;
			float4 accumW = ZEROES;
			
			float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
			float depth_o = LinearEyeDepth(depth_r);	
			
			float fadeAmount = 0.0;
			
	        fadeAmount = depth_o - _DofFocusPoint;
	        float tempFade = fadeAmount / _DofFocusDistance;
	        
	        #if PRISM_DOF_USENEARBLUR
	    	//if the sign of fadeAmount is negative, we're behind
	    	if(sign(tempFade) == -1)
	    	{
	    		fadeAmount = fadeAmount / _DofNearFocusDistance;
	    		fadeAmount = abs(fadeAmount);
	    	} else {
	    		fadeAmount = tempFade;
	    	}
	        #else	
	    	fadeAmount = tempFade;
	        #endif
	        
	        //If 1 > 0.99999 when _DofBlurSkybox is set
	        if(depth_r > _DofBlurSkybox)
	        {
	        	fadeAmount = 0.0;
	        }
	        
	        #if PRISM_TWOCAMSETUP
	        if(midEyeDepth < 0.999)
	        {
	        	fadeAmount = 0.0;
	        }
	        #endif
				
	           
	        fadeAmount = (clamp (fadeAmount, 0.0, 1.0)); //(clamp( fadeAmount / _DofFocusDistance, 0.0, 1.0 ));
	        		
			//Tiltshift 
			//fadeAmount = clamp(WeightIrisMode(uv), 0, 1.0);
			
			#if SHADER_API_D3D11	
			[unroll]	
			#endif
				for (int j = -DOF_SAMPLES; j < DOF_SAMPLES; j += 1)
				#if SHADER_API_D3D11
				[unroll]
				#endif
					for (int i = -DOF_SAMPLES; i < DOF_SAMPLES; i += 1){
						float2 offset = pixelSize * float2(i + j, j - i);
						#if UNITY_UV_STARTS_AT_TOP
						float4 col = tex2D(_DoF1, uv + offset * (_DofRadius * fadeAmount));
						#else 
						float4 col = tex2D(_DoF1, uv + offset * (_DofRadius * fadeAmount));
						#endif
						float4 bokeh = ONES + (col * col) * col * (float4)_DofFactor;
						accumCol += col * bokeh;
						accumW += bokeh;
				}		
			ret = accumCol/accumW;
			
			if(_DofUseLerp > 0.0)
			{
				ret = lerp(origCol, ret, fadeAmount);
			}
			
			
			return ret;
				
		#endif
		
	}
	
	float4 fragDoFDebug (v2f i) : SV_Target
	{		
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
	
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(i.uv);
		float depth_o = LinearEyeDepth(depth_r);	
		
		float fadeAmount = 0.0;
		
        fadeAmount = depth_o - _DofFocusPoint;
        
        float tempFade = fadeAmount / _DofFocusDistance;
        
        if(_DofUseNearBlur > 0.0)
        {
        	//if the sign of fadeAmount is negative, we're behind
        	if(sign(tempFade) == -1)
        	{
        		fadeAmount = fadeAmount / _DofNearFocusDistance;
        		fadeAmount = abs(fadeAmount);
        	} else {
        		fadeAmount = tempFade;
        	}
        } else {
        	fadeAmount = tempFade;
        }
        
        //If 1 > 0.99999 when _DofBlurSkybox is set
        if(depth_r > _DofBlurSkybox)
        {
        	fadeAmount = 0.0;
        }
           //
        fadeAmount = (clamp (fadeAmount, 0.0, 1.0)); 
        
        return lerp(ONES, ZEROES, fadeAmount);
	}
	
	//Just outputs luminance
	float4 fragLuminance (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float4 col = tex2D(_MainTex, uv);
		return (float4)Luminance(col.rgb);//float4((float3)Luminance(col.rgb), col.a);
	}	
	
	//Don't make it harder than it needs to be :)
	float4 fragAdapt (v2f i) : SV_Target
	{
		//MainTex = current luminance
		//BrightTex = last adapted luminance
	
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif		
		
		float4 col = (float4)tex2D(_MainTex, uv).r;
		
		col.a = saturate(0.0125 * _ExposureSpeed);
		return col;
	}
	
	float4 fragApplyAdaptDebug(v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		float4 col = tex2D(_MainTex, uv);
		float linear_exposure = tex2D(_BrightnessTexture, uv).r;//float2(0.5, 0.5));
	    
	    if(uv.x < 0.50)
	    {
	    	return (float4)linear_exposure;
	    }
	    
	    col.rgb = ApplyExposure(col.rgb, linear_exposure);
	    
	    return col;
	}
	
	// Gamma encoding function for AO value
	// (do nothing if in the linear mode)
	half EncodeAO(half x)
	{
		//This is probably faster than the pow
	    // ColorSpaceLuminance.w is 0 (gamma) or 1 (linear).
		if(unity_ColorSpaceLuminance.w == 1)
		{
			return x;
		}
		
	    // Gamma encoding
	    return (1 - pow(1 - x, 1 / 2.2));
	}
	
	// Final ao combiner function
	inline float3 CombineObscurance(float3 src, float3 ao, float2 uv)
	{
		float3 hdrSrcZero = (float3)0.0;
		
		//Need to pow it so that all areas don't get -'ed a bit
		#if SHADER_TARGET < 30 || SHADER_API_MOBILE
		
		#else
		ao -= (pow(Luminance(src), 2.) * _AOLuminanceWeighting);
		#endif
		
	    return lerp(src, hdrSrcZero,max(EncodeAO(ao),0.0));
	}
	
	inline float3 FilmicGrain(float3 col, float2 uv)
	{
		float rand = nrandtime(uv, _RandomInts.x, _RandomInts.y);
		
		uv.xy += rand;
		
		float3 grain = tex2D(_GrainTex, uv).rgb * rand;
		
		//return lerp(col, col - grain, _GrainIntensity.x);
		//return grain;
		// 3x V_ADD_F32, 3x V_MIN_F32, 3x V_MAC_F32
		return grain * min(col + _GrainIntensity.x, _GrainIntensity.y) + col; 
		//
		return col;
	}
	
	half TransformColor (half4 skyboxValue) {
		return dot(max(skyboxValue.rgb - _SunThreshold.rgb, half3(0,0,0)), half3(1,1,1)); // threshold and convert to greyscale
	}
	
	inline float3 TransformBloomColor (half3 bloomValue) {

		float3 blmMinusThresh = bloomValue.rgb - _BloomThreshold;
		float3 ret = max(blmMinusThresh, float3(0,0,0));
		//
		if(dot(blmMinusThresh, half3(0.1, 0.1, 0.1)) > 0.0)
		{
			ret += (float3)_BloomThreshold;
		}
		
		return ret;
	}
	
	float4 fragSunPrepass (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float depthSample = PRISM_SAMPLE_DEPTH_TEXTURE(uv);
		depthSample = Linear01Depth (depthSample);
		
		half2 vec = _SunPosition.xy - uv;		
		half dist = saturate (_SunPosition.w - length (vec.xy));
		
		half4 outColor = 0;
		
		float4 tex = tex2D (_MainTex, uv);
		
		// consider shafts blockers
		if (depthSample > 0.9999)
			outColor = TransformColor (tex) * dist;	
		
		return outColor;
	}
	
	float4 fragSunDebug (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		return tex2D (_MainTex, uv);
	}
	
	inline float3 CombineSun(float3 realColor, float2 uv)
	{
		float _SunExposure=0.2;
		
		half4 col = tex2D (_RaysTexture, uv);
 		return ((float4((col.rgb * _SunExposure),1.))+(realColor*(1.1)));
	}

	float4 fragSunCombine (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		half4 realColor = tex2D(_MainTex, uv);
		
		realColor.rgb = CombineSun(realColor.rgb, uv);
		return realColor;
	}
	
	float4 fragSunBlur (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		#if SHADER_TARGET < 30
		return ONES;
		#endif
		
		const float _SunDecay=0.96815;
		const float _SunExposure=0.2;
		const float _SunDensity=0.926;
		//float _SunWeight0.58767;
		
		const int NUM_SUN_SAMPLES = 98;
		
		float2 deltaTexCoord = (uv - _SunPosition.xy);
		deltaTexCoord *= 1.0 / (float)NUM_SUN_SAMPLES * _SunDensity;
		float illuminationDecay = 1.0;
		
		float4 col = tex2D(_MainTex, uv)*0.3;
		
		for(int i = 0; i < NUM_SUN_SAMPLES; i++)
		{
			uv -= deltaTexCoord;
			float4 sunSampleBeep = tex2D(_MainTex, uv)*0.4;
			sunSampleBeep *= (illuminationDecay * _SunWeight);
			col += sunSampleBeep;
			illuminationDecay *= _SunDecay;
		}
		
		col.rgb = saturate (col.rgb * _SunColor.rgb);

		return col;
	}
	
	inline float3 CombineVignette(float3 col, float2 uv)
	{
		if(_VignetteIntensity > 0.0)
		{
			float d = distance(uv, float2(0.5,0.5));
			float vignette_black = smoothstep(_VignetteStart, _VignetteEnd, d * _VignetteIntensity);
			col.rgb = lerp(_VignetteColor.rgb * col.rgb, col.rgb, vignette_black); // here!
		}
		
		return col;
	}
	
	//Final combine 
	float4 fragFinal (v2f i) : SV_Target
	{			
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
			
		float4 col = ONES;
		float midEyeDepth = 0.0;
		
		//Potato devices and WebGL. Let's try for the usual ones...
		#if SHADER_TARGET < 30
		//test
			col = tex2D(_MainTex, uv);
			
			if(_ChromaticIntensity > 0.0)
			{
				col.rgb = ChromaticAberration(col.rgb, uv);
			}
		
			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, uv).r;
			col.rgb = ApplyExposure(col.rgb, linear_exposure);
			#endif
			
			//Add bloom	- CAN'T BE STABLE BLOOM
			#if PRISM_USE_BLOOM
			
				#if !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR
				float4 blmTex = tex2D(_Bloom1, i.uv);
				col.rgb = blmTex.rgb;
				#elif PRISM_UIBLUR
				#endif
			
				col.rgb = CombineBloom(col.rgb, i.uv);
			#endif			
			
			col.rgb = CombineVignette(col.rgb, uv);
			
			#if PRISM_FILMIC_TONEMAP
			col.rgb = filmicTonemap(col.rgb);
			#endif
			
			#if PRISM_ROMB_TONEMAP		
			col.rgb = filmicTonemapRomB(col.rgb);
			#endif
			
			#if PRISM_ACES_TONEMAP		
			col.rgb = ACESFilm(col.rgb);
			#endif
			
		    if(_Gamma > 0.0)
		    {
		    	col = pow(col, _Gamma);
		    }
				
			if(useNoise > 0.0)
			{
				col.rgb = FilmicGrain(col.rgb, uv);
			}
		
		#else
		
			#if PRISM_TWOCAMSETUP
	        midEyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
	        #endif
			//	
			#if PRISM_DOF_LOWSAMPLE || PRISM_DOF_MEDSAMPLE
				#if SHADER_API_GLCORE || SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
				col = bokehBlurFull(i.uv, uv, midEyeDepth);
				#else
				col = bokehBlurFull(i.uv, i.uv2, midEyeDepth);
				#endif		
			#else 
			
				#if PRISM_DOF_USENEARBLUR
				
				#else
				col = tex2D(_MainTex, uv);
				if(_ChromaticIntensity > 0.0)
				{
					col.rgb = ChromaticAberration(col.rgb, uv);
				}
				#endif
			
			#endif		

			#if PRISM_USE_EXPOSURE		
			float linear_exposure = tex2D(_BrightnessTexture, uv).r;
			col.rgb = ApplyExposure(col.rgb, linear_exposure);
			#endif
			
			if(_AOIntensity > 0.0)
			{
				half ao = tex2D(_AOTex, i.uv).r;
				
				#if PRISM_TWOCAMSETUP
				if(midEyeDepth < 0.9) ao = 0.0;
				#endif
				
				col.rgb = CombineObscurance(col.rgb, (float3)ao, uv);
			}
			
			//Add bloom		
			#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM
			
				#if !PRISM_DOF_LOWSAMPLE && !PRISM_DOF_MEDSAMPLE && PRISM_DOF_USENEARBLUR
				float4 blmTex = tex2D(_Bloom1, i.uv);
				col.rgb = blmTex.rgb;
				#elif PRISM_UIBLUR
				#endif
				
				col.rgb = CombineBloom(col.rgb, i.uv);
				
			#endif
			//End add bloom
			
			#if PRISM_USE_BLOOM || PRISM_USE_STABLEBLOOM	
			if(_DirtIntensity > 0.0)
			{
				//We now have the blurred texture. Now blend it with the original brightpass
				float4 dirtVal = tex2D(_DirtTex, uv);
				//return blmTex;
				float4 dirtCol = float4(dirtVal.rgb, 1.0);
				#if PRISM_HDR_BLOOM
				col.rgb += lerp((float3)0.0, dirtCol.rgb * ( tex2D(_Bloom4, i.uv).rgb * _DirtIntensity)						, dirtVal.a);
				#else
				col.rgb += lerp((float3)0.0, dirtCol.rgb * ( KawaseBlur(_Bloom1, i.uv, 40).rgb * _DirtIntensity)						, dirtVal.a);
				#endif
			}	
			#endif
			
			if(_FogIntensity > 0.0)
			{
				#if PRISM_HDR_BLOOM || PRISM_SIMPLE_BLOOM
				col.rgb = GlobalFog(col.rgb, i.uv, tex2D(_Bloom1, i.uv));
				#else
				col.rgb = GlobalFog(col.rgb, i.uv, ZEROES);
				#endif
			}		
			
			#if PRISM_USE_NIGHTVISION
			//Add nightvision
			half4 dfse = tex2D(_CameraGBufferTexture0, uv);			

			//Get the luminance of the pixel				
			float lumc = Luminance (col.rgb);
			lumc = min(1.0, lumc);

			//Desat + green the image
			col = dot(col, _NVColor);	

			//Make bright areas/lights too bright
			col.rgb = lerp(col.rgb, _TargetWhiteColor.rgb, lumc * _LightSensitivityMultiplier);

			//Add some of the regular diffuse texture based off how bright each pixel is
			col.rgb = lerp(col.rgb, dfse.rgb, lumc+_BaseLightingContribution);
			
			//Increase the brightness of all normal areas by a certain amount
			col.rb = max (col.r - 0.75, 0)*4;
			#endif
			
			//#if USE_VIGNETTE
			col.rgb = CombineVignette(col.rgb, uv);
			//#endif	
			
			#if PRISM_FILMIC_TONEMAP
			col.rgb = filmicTonemap(col.rgb);
			#endif
			
			#if PRISM_ROMB_TONEMAP		
			col.rgb = filmicTonemapRomB(col.rgb);
			#endif
			
			#if PRISM_ACES_TONEMAP		
			col.rgb = ACESFilm(col.rgb);
			#endif
			
		    //#if USE_GAMMA
		    if(_Gamma > 0.0)
		    {
		    	col = pow(col, _Gamma);
		    }
			
			if(_SunWeight > 0.0)
			{
				#if SHADER_API_GLCORE || SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
				col.rgb = CombineSun(col.rgb, i.uv);//Maybe UV2?
				#else
				col.rgb = CombineSun(col.rgb, i.uv);
				#endif	
			}
			
			#if PRISM_GAMMA_LOOKUP
			col = lerp(col, lookupGamma(_LutTex, col, uv), _LutAmount);
			
			if(_SecondLutAmount > 0.0)
			{
				col = lerp(col, lookupGamma(_SecondLutTex, col, uv), _SecondLutAmount);
			}
				
			#elif PRISM_LINEAR_LOOKUP
			col = lerp(col, lookupLinear(_LutTex, col, uv), _LutAmount);
			
			if(_SecondLutAmount > 0.0)
			{
				col = lerp(col, lookupLinear(_SecondLutTex, col, uv), _SecondLutAmount);
			}
			#endif
			if(useNoise > 0.0)
			{
				col.rgb = FilmicGrain(col.rgb, uv);
			}
		
		#endif
		return col;
	}
	
	float4 fragChromatic (v2f i) : SV_Target
	{		
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		float4 col = tex2D(_MainTex, uv);
		col.rgb = ChromaticAberration(col.rgb, uv);
		return col;
	}
		
	static const half totSmall = 0.728784974;
	static const half gaussWeights[7] = { 
		0.126090729,
		0.106639087,
		0.080656342,
		0.054556872,
		0.033002683,
		0.017854096,
		0.008638042,
  	};
	
	static const half totMed = 0.841052644;
	static const half gaussWeightsMed[13] = {  
		0.072808141,
		0.069138678,
		0.063429257,
		0.05621932,
		0.048140321,
		0.039825367,
		0.031830101,
		0.024577838,
		0.018334824,
		0.013214089,
		0.00920079,
		0.006189285,
		0.004022381,
	};
	
	static const half totLarge = 0.474315098;
	static const half gaussWeightsLarge[25] = {
		0.039026678,
		0.038465102,
		0.037547041,
		0.03629845,
		0.034753935,
		0.032955159,
		0.030948983,
		0.02878544,
		0.026515688,
		0.024190033,
		0.021856145,
		0.019557538,
		0.017332386,
		0.015212691,
		0.013223831,
		0.01138445,
		0.009706671,
		0.00819657,
		0.006854843,
		0.005677621,
		0.00465735,
		0.003783683,
		0.003044348,
		0.002425925,
		0.001914537,
	};

	float4 GaussianBlur(float2 uv)
	{
		float2 offs = _BlurVector * _MainTex_TexelSize.xy;
		float4 col = ZEROES;
		float4 midSamp = tex2D(_MainTex, uv);
		col.a = midSamp.a;
       	col.rgb += gaussWeights[0] * midSamp.rgb;//(gauss(ci*_BlurGaussFactor)*_BlurGaussFactor) * tex2D(_MainTex, uv+offs*ci).rgb;
       	//col += gaussWeightsMed[0] * tex2D(_MainTex, uv - offs * 0.0).rgb;
       	col.rgb += gaussWeights[1] * tex2D(_MainTex, uv + offs).rgb;
       	col.rgb += gaussWeights[1] * tex2D(_MainTex, uv - offs).rgb;
       	col.rgb += gaussWeights[2] * tex2D(_MainTex, uv + offs * 2.).rgb;
       	col.rgb += gaussWeights[2] * tex2D(_MainTex, uv - offs * 2.).rgb;
       	col.rgb += gaussWeights[3] * tex2D(_MainTex, uv + offs * 3.).rgb;
       	col.rgb += gaussWeights[3] * tex2D(_MainTex, uv - offs * 3.).rgb;
       	col.rgb += gaussWeights[4] * tex2D(_MainTex, uv + offs * 4.).rgb;
       	col.rgb += gaussWeights[4] * tex2D(_MainTex, uv - offs * 4.).rgb;
       	col.rgb += gaussWeights[5] * tex2D(_MainTex, uv + offs * 5.).rgb;
       	col.rgb += gaussWeights[5] * tex2D(_MainTex, uv - offs * 5.).rgb;
       	col.rgb += gaussWeights[6] * tex2D(_MainTex, uv + offs * 6.).rgb;
       	col.rgb += gaussWeights[6] * tex2D(_MainTex, uv - offs * 6.).rgb;
       	
       	col.rgb = col.rgb / totSmall;
	    
	    return col;
	}
	
	float3 GaussianBlurLarge(float2 uv)
	{
		float2 offs = _BlurVector * _MainTex_TexelSize.xy;
		float3 col = (float3)0.0;
		
       	col += gaussWeightsMed[0] * tex2D(_MainTex, uv).rgb;//(gauss(ci*_BlurGaussFactor)*_BlurGaussFactor) * tex2D(_MainTex, uv+offs*ci).rgb;
       	//col += gaussWeightsMed[0] * tex2D(_MainTex, uv - offs * 0.0).rgb;
       	col += gaussWeightsMed[1] * tex2D(_MainTex, uv + offs).rgb;
       	col += gaussWeightsMed[1] * tex2D(_MainTex, uv - offs).rgb;
       	col += gaussWeightsMed[2] * tex2D(_MainTex, uv + offs * 2.).rgb;
       	col += gaussWeightsMed[2] * tex2D(_MainTex, uv - offs * 2.).rgb;
       	col += gaussWeightsMed[3] * tex2D(_MainTex, uv + offs * 3.).rgb;
       	col += gaussWeightsMed[3] * tex2D(_MainTex, uv - offs * 3.).rgb;
       	col += gaussWeightsMed[4] * tex2D(_MainTex, uv + offs * 4.).rgb;
       	col += gaussWeightsMed[4] * tex2D(_MainTex, uv - offs * 4.).rgb;
       	col += gaussWeightsMed[5] * tex2D(_MainTex, uv + offs * 5.).rgb;
       	col += gaussWeightsMed[5] * tex2D(_MainTex, uv - offs * 5.).rgb;
       	col += gaussWeightsMed[6] * tex2D(_MainTex, uv + offs * 6.).rgb;
       	col += gaussWeightsMed[6] * tex2D(_MainTex, uv - offs * 6.).rgb;
       	col += gaussWeightsMed[7] * tex2D(_MainTex, uv + offs * 7.).rgb;
       	col += gaussWeightsMed[7] * tex2D(_MainTex, uv - offs * 7.).rgb;
       	col += gaussWeightsMed[8] * tex2D(_MainTex, uv + offs * 8.).rgb;
       	col += gaussWeightsMed[8] * tex2D(_MainTex, uv - offs * 8.).rgb;
       	col += gaussWeightsMed[9] * tex2D(_MainTex, uv + offs * 9.).rgb;
       	col += gaussWeightsMed[9] * tex2D(_MainTex, uv - offs * 9.).rgb;
       	col += gaussWeightsMed[10] * tex2D(_MainTex, uv + offs * 10.).rgb;
       	col += gaussWeightsMed[10] * tex2D(_MainTex, uv - offs * 10.).rgb;
       	col += gaussWeightsMed[11] * tex2D(_MainTex, uv + offs * 11.).rgb;
       	col += gaussWeightsMed[11] * tex2D(_MainTex, uv - offs * 11.).rgb;
       	col += gaussWeightsMed[12] * tex2D(_MainTex, uv + offs * 12.).rgb;
       	col += gaussWeightsMed[12] * tex2D(_MainTex, uv - offs * 12.).rgb;
       	
       	col = col / totMed;
	    
	    return col;
	}
	
	float4 fragChromaticBlur (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		//No blur on SM2
		#if SHADER_TARGET < 30   
		
		float chromAmount = GetChromaticVignetteAmount(uv); 
		float3 col = (float3)0.0;
	    
	    col = lerp(tex2D(_MainTex, uv).rgb, col, chromAmount);
	    
	    return float4(col, 1.0);
		#else
		float chromAmount = GetChromaticVignetteAmount(uv); 
		float3 col = (float3)0.0;
		
	    if(chromAmount > 0.0)
	    {
			col = GaussianBlur(uv).rgb;
	    }
	    
	    col = lerp(tex2D(_MainTex, uv).rgb, col, chromAmount);
	    
	    return float4(col, 1.0);
		#endif
	}
	
	float4 fragGaussianBlur (v2f i) : SV_Target
	{
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		//No blur on SM2
		#if SHADER_TARGET < 30   
		float3 col = (float3)0.0;
	    return float4(col, 1.0);
		#else
		float4 col = GaussianBlur(uv);
	    
	    return col;
		#endif
	}
	
	struct v2fDepthCopy {
	    float4 pos : SV_POSITION;
	    float2 depth : TEXCOORD0;
	};
//
	v2fDepthCopy vertDepthCopy (appdata_base v) {
	    v2fDepthCopy o;
	    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	    UNITY_TRANSFER_DEPTH(o.depth);
	    return o;
	}

	float4 fragDepthCopy(v2f i) : SV_Target {
	    //UNITY_OUTPUT_DEPTH(i.depth);
		float depth_r = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
		
		return depth_r;//LinearEyeDepth(depth_r);
		//return lerp(ONES, ZEROES, depth_o);
		//return float4(depth_o, 0.0,0.0, 1.0);//(float4)depth_o;
	}
	
	
	float4 fragDepthDebug(v2f i) : SV_Target {
	    //UNITY_OUTPUT_DEPTH(i.depth);
		float depth_r = PRISM_SAMPLE_DEPTH_TEXTURE(i.uv);
		
		//return depth_r;
		return lerp(ONES, ZEROES, depth_r);
		//return float4(depth_o, 0.0,0.0, 1.0);//(float4)depth_o;
	}
	
     //Start median==========================================================================================================     
	///*
	//3x3, 5x5etc Median
	//Morgan McGuire and Kyle Whitson
	//http://graphics.cs.williams.edu
	//
	float4 fragMedRGB (v2f i) : SV_Target
	{
		float2 ooRes = _MainTex_TexelSize.xy;//_ScreenParams.w;

		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		
		//This doesn't work on SM2
		#if SHADER_TARGET < 30        
		float3 ofs = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		//
		float3 v[5];
		
		float4 midCol = tex2D(_MainTex, uv);
		
        v[0] = midCol.rgb;
        v[1] = tex2D(_MainTex, uv - ofs.xz).rgb;
        v[2] = tex2D(_MainTex, uv + ofs.xz).rgb;
        v[3] = tex2D(_MainTex, uv - ofs.zy).rgb;
        v[4] = tex2D(_MainTex, uv + ofs.zy).rgb;
		
		float3 temp;
		mnmx5(v[0], v[1], v[2], v[3], v[4]);
		mnmx3(v[1], v[2], v[3]);
		
		return float4(v[2].rgb, midCol.a);
		#else
		
		float4 midCol = tex2D(_MainTex, uv);
		
		// -1  1  1
		// -1  0  1 
		// -1 -1  1  //3x3=9	
		float3 v[9];

		// Add the pixels which make up our window to the pixel array.
		for(int dX = -1; dX <= 1; ++dX) 
		{
			for(int dY = -1; dY <= 1; ++dY) 
			{
				float2 ofst = float2(float(dX), float(dY));

				// If a pixel in the window is located at (x+dX, y+dY), put it at index (dX + R)(2R + 1) + (dY + R) of the
				// pixel array. This will fill the pixel array, with the top left pixel of the window at pixel[0] and the
				// bottom right pixel of the window at pixel[N-1].
				v[(dX + 1) * 3 + (dY + 1)] = (float3)tex2D(_MainTex, uv + ofst * ooRes).rgb;
			}
		}

		float3 temp;

		// Starting with a subset of size 6, remove the min and max each time
		mnmx6(v[0], v[1], v[2], v[3], v[4], v[5]);
		mnmx5(v[1], v[2], v[3], v[4], v[6]);
		mnmx4(v[2], v[3], v[4], v[7]);
		mnmx3(v[3], v[4], v[8]);
			
		return float4(v[4].rgb, midCol.a);
		#endif
	}
	
	float4 fragBloomMedRGB (v2f i) : SV_Target
	{
		float2 ooRes = _MainTex_TexelSize.xy;//_ScreenParams.w;

		#if UNITY_UV_STARTS_AT_TOP
		float2 uv = i.uv2;
		#else
		float2 uv = i.uv;
		#endif
		    
		float3 ofs = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		//
		float3 v[5];
		
		float4 midCol = tex2D(_MainTex, uv);
		
        v[0] = midCol.rgb;
        v[1] = tex2D(_MainTex, uv - ofs.xz).rgb;
        v[2] = tex2D(_MainTex, uv + ofs.xz).rgb;
        v[3] = tex2D(_MainTex, uv - ofs.zy).rgb;
        v[4] = tex2D(_MainTex, uv + ofs.zy).rgb;
		
		float3 temp;
		mnmx5(v[0], v[1], v[2], v[3], v[4]);
		mnmx3(v[1], v[2], v[3]);
		
		v[2].rgb *= saturate(Luminance(v[4].rgb) / _BloomThreshold);
		
		return float4(v[2].rgb, midCol.a);
	}
              
                   
    //End median=======================================================================================================                            

	//Custom downsample
	struct v2fDownsample { 
		float4 position : SV_POSITION;  
		float2 uv[4]    : TEXCOORD0;
	}; 
	
	v2fDownsample vertDownsample (appdata_img v)
	{
		v2fDownsample o;
		o.position = mul(UNITY_MATRIX_MVP, v.vertex);
		float2 uv = v.texcoord;
		
		// Compute UVs to sample pixel block.
		o.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1,1);
		o.uv[1] = uv + _MainTex_TexelSize.xy * half2(1,1);
		o.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1,-1);
		o.uv[3] = uv + _MainTex_TexelSize.xy * half2(1,-1);
		
		return o;
	}

	half4 fragDownsample (v2fDownsample i) : SV_Target
	{
		// Sample pixel block
		half3 topLeft = tex2D(_MainTex, i.uv[0]).rgb;
		half3 topRight = tex2D(_MainTex, i.uv[1]).rgb;
		half3 bottomLeft = tex2D(_MainTex, i.uv[2]).rgb;
		half3 bottomRight = tex2D(_MainTex, i.uv[3]).rgb;
		
		half3 avg = ((topLeft + topRight + bottomLeft + bottomRight) / 4.0);
					
		return float4(avg.rgb, 1.0);
	}                                                                    
                                                                                         
                                                                                              
                                                                                                   
                                                                                                        
                                                                                                             
                                                                                                                  
                                                                                                                       
                                                                                                                            
                                                                                                                                 
                                                                                                                                      
                                                                                                                                           
      
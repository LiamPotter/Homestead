//
// Kino/Obscurance - SSAO (screen-space ambient obscurance) effect for Unity
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
Shader "Hidden/PrismKinoObscurance" {

    Properties
    {
        _MainTex("", 2D) = ""{}
        _AOTex("", 2D) = ""{}
    }
	
	CGINCLUDE
    // Source texture type (CameraDepthNormals or G-buffer)
    //Intentionally uses the same keywords as Keijiro's
    #pragma multi_compile _ _SOURCE_GBUFFER

    // Sample count; given-via-uniform (default) or lowest
    //Intentionally uses the same keywords as Keijiro's
    #pragma multi_compile _AOSAMPLECOUNT_CUSTOM _AOSAMPLECOUNT_LOWEST
    
    #pragma multi_compile _ _AOCUTOFF_ON
	
	#include "UnityCG.cginc"
	#include "PrismKO.cginc"
	#pragma target 3.0
	ENDCG
	
SubShader {
        Pass//0
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_ao
            ENDCG
        }
        Pass//1
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_blur
            ENDCG
        }
        Pass//2
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragAOdebug
            ENDCG
        }  
        
         Pass//3 - newblur first pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBlurInitial
            ENDCG
        }  
        
        Pass//4 - newblur second pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBlurSecondary
            ENDCG
        }        
    }
	
	
	
	
	
	FallBack off
}

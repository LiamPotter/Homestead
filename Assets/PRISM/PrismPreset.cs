using UnityEngine;
using System.Collections;

namespace Prism.Utils {

[System.Serializable]
	/// <summary>
	/// The PRISM Preset is a scriptableobject class that holds values for a PRISM Effects editor.
	/// </summary>
	public class PrismPreset : ScriptableObject {

		public string PresetDescription = "Enter description for your preset.";

		public PrismPresetType presetType = PrismPresetType.Full;

		//Bloom===============================
		[SerializeField]
		public bool useBloom;

		[SerializeField]
		public BloomType bloomType = BloomType.HDR;

		[SerializeField]
		public bool bloomUseScreenBlend = false;
		
		[SerializeField]
		public int bloomDownsample = 2;
		
		[SerializeField]
		public int bloomBlurPasses = 4;
		
		[SerializeField]
		public float bloomIntensity = 0.2f;
		
		[SerializeField]
		public float bloomThreshold = 0.02f;
		
		[SerializeField]
		public bool useBloomStability = true;
		
		[SerializeField]
		public bool useBloomLensdirt = false;

		[SerializeField]
		public float bloomLensdirtIntensity = 2f;

		[SerializeField]
		public Texture2D bloomLensdirtTexture;

		[SerializeField]
		public bool useFullScreenBlur = false;

		//UIBlur
		[SerializeField]
		public bool useUIBlur = false;

		[SerializeField]
		public int uiBlurGrabTextureFromPassNumber = 2;
		//EndUIBLUR

		//Fog===============================

		[SerializeField]
		public bool useFog;

		[SerializeField]
		public bool fogAffectSkybox = false;

		[SerializeField]
		public float fogIntensity = 1.0f;

		[SerializeField]
		public float fogHeight = 2.0f;

		[SerializeField]
		public float fogStartPoint = 50f;

		[SerializeField]
		public float fogDistance = 30f;

		[SerializeField]
		public Color fogColor = Color.gray;

		[SerializeField]
		public Color fogEndColor = Color.black;


		//DoF=================================
		[SerializeField]
		public bool useDoF;

		[SerializeField]
		public float dofRadius = 0.6f;

		[SerializeField]
		public DoFSamples dofSampleCount = DoFSamples.Low;

		[SerializeField]
		public float dofBokehFactor = 60f;
		
		[SerializeField]
		public float dofFocusPoint = 15f;

		[SerializeField]
		public float dofFocusDistance = 20f;

		[SerializeField]
		public bool useNearBlur = false;

		[SerializeField]
		public bool dofBlurSkybox = false;

		[SerializeField]
		public float dofNearFocusDistance = 20f;

		[SerializeField]
		public bool dofForceEnableMedian = false;

		//AO
		[SerializeField]
		public bool useAmbientObscurance = false;
		
		[SerializeField]
		public float aoIntensity = 0.7f;
		
		[SerializeField]
		public float aoRadius = 1.0f;
		
		[SerializeField]
		public float aoBias = 0.1f;
		
		[SerializeField]
		public bool aoDownsample = false;
		
		[SerializeField]
		public int aoBlurIterations = 1;
		
		[SerializeField]
		public float aoBlurFilterDistance = 1.25f;

		[SerializeField]
		public bool useAODistanceCutoff = false;

		[SerializeField]
		public float aoDistanceCutoffLength = 50.00f;

		[SerializeField]
		public float aoDistanceCutoffStart = 500.0f;	

		[SerializeField]
		public SampleCount aoSampleCount = SampleCount.Medium;

		[SerializeField]
		public AOBlurType aoBlurType = AOBlurType.Fast;

		[SerializeField]
		public float aoLightingContribution = 1.0f;

		//Chromatic ab=================================
		[SerializeField]
		public bool useChromaticAb;
		
		[SerializeField]
		public AberrationType aberrationType = AberrationType.Vignette;
		
		[SerializeField]
		public float chromIntensity = 0.05f;
		
		[SerializeField]
		public float chromStart = 0.9f;
		
		[SerializeField]
		public float chromEnd = 0.4f;

		[SerializeField]
		public bool useChromaticBlur = false;

		[SerializeField]
		public float chromaticBlurWidth = 1f;

		//Vignette=================================
		[SerializeField]
		public bool useVignette;
		
		[SerializeField]
		public float vignetteStart = 0.9f;
		
		[SerializeField]
		public float vignetteEnd = 0.4f;

		[SerializeField]
		public float vignetteIntensity = 1.0f;

		[SerializeField]
		public Color vignetteColor = Color.black;

		//Noise=================================
		[SerializeField]
		public bool useNoise;

		[SerializeField]
		public float noiseIntensity = 0.2f;

		//Tonemap=================================
		[SerializeField]
		public bool useTonemap = false;

		[SerializeField]
		public TonemapType toneType = TonemapType.RomB;

		[SerializeField]
		public Vector3 toneValues = new Vector3(-1.0f, 2.72f, 0.15f);

		[SerializeField]
		public Vector3 secondaryToneValues = new Vector3(0.59f, 0.14f, 0.14f);

		//Gamma
		[SerializeField]
		public bool useGammaCorrection = false;

		[SerializeField]
		public float gammaValue = 1.0f;

		//Exposure=================================
		[SerializeField]
		public bool useExposure = false;

		[SerializeField]
		public float exposureSpeed = 0.18f;

		[SerializeField]
		public float exposureMiddleGrey = 0.12f;

		[SerializeField]
		public float exposureLowerLimit = -6f;

		[SerializeField]
		public float exposureUpperLimit = 6f;

		//Lookup=================================
		[SerializeField]
		public bool useLUT;

		[SerializeField]
		public string lutPath;
		
		[SerializeField]
		public float lutIntensity = 1f;

		[SerializeField]
		public Texture2D twoDLookupTex;

		[SerializeField]
		public bool useSecondLut = false;

		[SerializeField]
		public string secondaryLutPath;

		[SerializeField]
		public Texture2D secondaryTwoDLookupTex;

		[SerializeField]
		public float secondaryLutLerpAmount = 0.0f;

		//NightVision=================================
		[SerializeField]
		public bool useNV;
		
		[SerializeField]
		public Color nvColor = new Color(0f,1f,0.1724138f,0f);
		
		[SerializeField]
		public Color nvBleachColor = new Color(1f,1f,1f,0f);
		
		[SerializeField]
		public float nvLightingContrib = 0.025f;
		
		[SerializeField]
		public float nvLightSensitivity = 100f;

		//Godrays
		[SerializeField]
		public bool useRays = false;

		[SerializeField]
		public float rayWeight = 0.58767f;

		[SerializeField]
		public Color rayColor = Color.white;

		[SerializeField]
		public Color rayThreshold = new Color(0.87f,0.74f,0.65f);


	}

}

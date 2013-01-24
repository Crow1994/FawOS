//*+++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// P R O J E C T  M. A. T. S. O.
// Masterly Advanced Technical Skyrim Overhaul
// Codes by Matso
// Sample setting by Kyo
//*+++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// V I B R A N T - O B G E - F U L L  C O D E
// Hexagonal - Movie Grain
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// ENBSeries effect file
// visit http://enbdev.com for updates
// Copyright (c) 2007-2011 Boris Vorontsov
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// Matso Immersive Effects v4.1.7
//	- depth of field,
//	- bokeh and smooth blur,
//	- sharpening,
//	- image grain,
//	- chromatic aberration
//	- etc.
// cmaster.matso@gmail.com
// Credits to Boris Vorontsov (ENB Series) and Tomerk (OBGE)
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//+++++++++++++++++++++++++++++
// Internal parameters, can be modified
//+++++++++++++++++++++++++++++

// Effects options (if the 'USE_...' or 'ENABLE_...' flags are disabled they are commented, prefixed with '//')
#define ENABLE_DOF	1				// comment to disable depth of field (DoF)
//#define ENABLE_CHROMA	1				// comment to disable chromatic aberration (ChA, additional chromatic aberration applied beyond depth of field)
//#define ENABLE_PREPASS	1			// comment to disable prepass effects (done before DoF)
#define ENABLE_POSTPASS	1				// comment to disable postpass effects (done after DoF)

// Methods enabling options
#define USE_CHROMA_DOF	1				// comment it to disable chromatic aberration sampling in DoF (dChA)
#define USE_SMOOTH_DOF	1				// comment it to disable smooth DoF
#define USE_BOKEH_DOF	1				// comment it to disable bokeh DoF
//#define USE_SINGLE_PASS	1			// comment it to disable single pass DoF (for better performance, usable only with False DoF)
//#define USE_SHARPENING	1			// comment it to disable sharpening
//#define USE_ANAMFLARE	1				// comment it to disable anamorphic lens flare (ALF) [NOTE: use the one in 'enbbloom.fx' file instead of this for better performance]
#define USE_IMAGEGRAIN	1				// comment it to disable image grain
//#define USE_MANUAL_FOCUS	1			// comment it to disable manual focus (focal distance)
//#define USE_MANUAL_BLUR_SCALE	1			// comment it to disable manual DoF blur scale (applies only to Complex 1.0)
//#define USE_SWITCHABLE_MODE		1		// comment it to disable DoF switchable mode (switching between bokeh and smooth)

// Useful constants
#define SEED			Timer.w						// seed for random number generation (set to 1 to make it time independent)
#define PI				3.1415926535897932384626433832795	// this one does not need commenting (I hope so) ;)
#define CHROMA_POW		65.0						// the bigger the value, the more visible chomatic aberration effect in DoF

// OBGE-like DoF stuff
#define FAR_CLIP_DIST	10000000.0		// do not change it!
#define NEAR_CLIP_DIST	10.0			// do not change it!
#define DEPTH_RANGE		-(NEAR_CLIP_DIST-FAR_CLIP_DIST)*0.01	// do not change it!
#define linear(t)		((2.0 * NEAR_CLIP_DIST) / (NEAR_CLIP_DIST + FAR_CLIP_DIST - t * (FAR_CLIP_DIST - NEAR_CLIP_DIST)))
#define fRetinaFocusPoint 		159.95		// retina focus point, dpt (little less the 'fRelaxedFocusPower' causes myopia)
#define fRelaxedFocusPower 		160.0 		// eye relaxed focus power, dpt (change it along with 'fRetinaFocusPoint' to alter the depth of field)
#define fAccomodation 			10.0 		// accomodation, dpt (little above 'fRetinaFocusPoint' + 'fAccomodation' leads to hyperopia)
#define fBaseBlurRadius 		0.006		// base blur radius (higher values mean more blur when out of depth of field)
#define fRadius 			16	 	// maximum blur radius in pixels (size of the blur shape)
#define K				0.00001		// do not change it!

// DoF constants (do not change DoF passes sequance - performance issue)
#define DOF_SCALE		2356.1944901923449288469825374596	// PI * 750
#define FIRST_PASS		0		// [NOTE: in case of Complex 1.0 DoF do not change this value]
#define SECOND_PASS		1		// [NOTE: in case of Complex 1.0 DoF do not change this value]
#define THIRD_PASS		2		// [NOTE: in case of Complex 1.0 DoF do not change this value]
#define FOURTH_PASS		3		// [NOTE: in case of Complex 1.0 DoF do not change this value]

// Different types of DoF formula [NOTE: only Complex 1.0]
#ifndef USE_MANUAL_FOCUS
 #define DOF1(sd,sf)		fBlurScale * abs(sd - sf) * 5.0					// blur is acquired directly as a scaled distance of the pixel depth from focus depth
 #define DOF2(sd,sf)		fBlurScale * pow(abs(sd - sf) * 2.0, 2.0) * 10.0		// blur is acquired as a scaled power 2 of the distance of the pixel depth from focus depth
 #define DOF3(sd,sf)		fBlurScale * smoothstep(fDofBias, fDofCutoff, abs(sd - sf))	// blur is acquired as a scaled cubic interpolation according to the distance of the pixel depth from focus depth
 #define DOF 			DOF3								// select one of the above
#else
 #define DOF(sd,sf)			fBlurScale * smoothstep(fDofBias * tempF1.y, fDofCutoff * tempF1.z, abs(sd - sf)) // manual DoF modification [NOTE: better use Complex 1.1 version]
#endif
#define BOKEH_DOWNBLUR		0.24		// bokeh deblurification factor, the default blur scale is too big for bokeh

// First person view weapon deblurification (applies only to Complex 1.1, OBGE-like and False shaders)
#define DONT_BLUR_WEAPON	1		// makes weapon held in first person not blurred (like in OBGE DoF)
#define fWeaponBlurCutoff	1.3		// weapon blur cutoff (adjust to Your liking)

// DoF shaders (use only one of the below) [NOTE: manual DoF works differently in Complex 1.0 then in other shaders]
//#define PS_ProcessPass_DepthOfField DepthOfField_Complex_1_0
//#define PS_ProcessPass_DepthOfField DepthOfField_Complex_1_1
#define PS_ProcessPass_DepthOfField DepthOfField_OBGE
//#define PS_ProcessPass_DepthOfField DepthOfField_False	// [NOTE: it can look differently with 'USE_SINGLE_PASS' option enabled]
//#define PS_ProcessPass_DepthOfField DepthOfField_LOD	// [NOTE: experimental - not working yet :P]

// Chromatic aberration parameters
float3 fvChroma = float3(0.9995, 1.000, 1.0005);// displacement scales of red, green and blue respectively
#define fBaseRadius 0.9				// below this radius the effect is less visible
#define fFalloffRadius 1.8			// over this radius the effect is max
#define fChromaPower 5.0			// power of the chromatic displacement (curve of the 'fvChroma' vector)

// Sharpen parameters
#define fSharpScale 0.032					// intensity of sharpening
float2 fvTexelSize = float2(1.0 / 1920.0, 1.0 / 1080.0);	// set your resolution sizes
//float2 fvTexelSize = float2(1.0 / 2560.0, 1.0 / 1440.0);
//float2 fvTexelSize = float2(1.0 / 3840.0, 1.0 / 2160.0);
//float2 fvTexelSize = float2(1.0 / 2880.0, 1.0 / 1620.0);

// Depth of field parameters
#define fFocusBias 0.055						// bigger values for nearsightedness, smaller for farsightedness (lens focal point distance) [NOTE: only Complex 1.0]
#define fDofCutoff 0.25							// manages the smoothness of the DoF (bigger value results in wider depth of field) [NOTE: only Complex 1.0]
#define fDofBias 0.07							// distance not taken into account in DoF (all closer then the distance is in focus) [NOTE: only Complex 1.0]
#define fBlurScale 0.004						// governs image blur scale (the bigger value, the stronger blur) [NOTE: only Complex 1.0]
#define fBlurCutoff 0.2							// bluring tolerance depending on the pixel and sample depth (smaller causes objects edges to be preserved) [NOTE: only Complex 1.0]
#define fFocusDistance	1.0						// manual focus distance base

// Bokeh parameters
#define fBokehCurve 5.0							// the larger the value, the more visible the bokeh effect is (not used with brightness limiting)
#define fBokehIntensity 0.95					// governs bokeh brightness (not used with brightness limiting)
#define fBokehConstant 0.1						// constant value of the bokeh weighting
#define fBokehMaxLevel 45.0						// bokeh max brightness level (scale factor for bokeh samples)
#define fBokehMin 0.001							// min input cutoff (anything below is 0)
#define fBokehMax 1.925							// max input cutoff (anything above is 1)
#define fBokehMaxWeight 25.0					// any weight above will be clamped

// Bokeh formulas
#define fBokehLuminance	0.956			// bright pass of the bokeh weight used with radiant version of the bokeh
#define BOKEH_RADIANT	float3 bct = ct.rgb;float b = GrayScale(bct) + fBokehConstant + length(bct)			// classic, prior 4.0.0 version of the bokeh formula
#define BOKEH_PASTEL	float3 bct = BrightBokeh(ct.rgb);float b = dot(bct, bct) + fBokehConstant			// new, less disturbing and more smooth version
#define BOKEH_VIBRANT	float3 bct = BrightBokeh(ct.rgb);float b = GrayScale(ct.rgb) + dot(bct, bct) + fBokehConstant	// compilation of the two above, bokeh apears only for very bright pixels
#define BOKEH_FORMULA	BOKEH_VIBRANT 		// choose one of the above

// Bokeh options	[NOTE: in case of using hexagonal shape, adjust 'fRadius' to make the effect more pronounced]
//#define USE_NATURAL_BOKEH	1		// more natural bokeh shape (comment to disable)
//#define USE_OCTAGONAL_BOKEH	1		// octagonal bokeh shape (comment to disable)
#define USE_HEXAGONAL_BOKEH	1		// hexagonal bokeh shape (comment to disable)
// [NOTE: above bokeh shape options (octagonal and hexagonal) work only when natural shape is disabled; to prevent errors use only one of them]
//#define USE_BRIGHTNESS_LIMITING		1	// bokeh brightness limiting (comment to disable)
#define USE_WEIGHT_CLAMP	1		// bokeh weight clamping (comment to disable)
#define USE_ENHANCED_BOKEH	1		// more pronounced bokeh (comment to disable)

// Grain parameters
#define fGrainFreq 2000.0			// image grain frequency
#define fGrainScale 0.070			// grain effect scale

// Anamorphic flare parameters [NOTE: for better performance use only the one in 'enbbloom.fx'][KYO : both ALF effects are stackable]
#define fFlareLuminance 2.0			// bright pass luminance value 
#define fFlareBlur 200.0			// manages the size of the flare
#define fFlareIntensity 0.02			// effect intensity
#define fFlareTint	float3(0.0, 0.0, 0.7)	// effect tint
#define fFlareAxis	0			// blur axis [KYO : 0 is horizontal, 1 is vertical]

// Vectors - it is recommended not to change their values, unless you exactly know what you're doing ;)
// Bokeh shape offset weights (scales of the sampling positions)
//#define DEFAULT_OFFSETS	{ -1.282, -0.524, 0.524, 1.282 }	// [NOTE: use this one with non-bokeh DoF]
//#define DEFAULT_OFFSETS	{ -1.0, -0.5, 0.5, 1.0 }
#define DEFAULT_OFFSETS	{ -0.9, -0.3, 0.3, 0.9 }

// Sampling vectors (directions for sampling positions)
float offset[4] = DEFAULT_OFFSETS;
#ifndef USE_NATURAL_BOKEH
 #ifdef USE_OCTAGONAL_BOKEH
  //float2 tds[4] = { float2(1.0, 0.0), float2(0.0, 1.0), float2(0.707, 0.707), float2(-0.707, 0.707) };	// Octagonal bokeh sampling directions [old]
  float2 tds[4] = { float2(-0.306, 0.739), float2(0.306, 0.739), float2(-0.739, 0.306), float2(-0.739, -0.306) };	// Octagonal bokeh sampling directions
 #endif
 #ifdef USE_HEXAGONAL_BOKEH
  float2 tds[4] = { float2(0.0, 0.75), float2(0.6495, 0.375), float2(-0.6495, 0.375), float2(0.0, 0.0) };	// Hexagonal bokeh sampling directions
 #endif
#else
 float2 tds[16] = { 
	float2(0.2007, 0.9796),
	float2(-0.2007, 0.9796), 
	float2(0.2007, 0.9796),
	float2(-0.2007, 0.9796), 
		
	float2(0.8240, 0.5665),
	float2(0.5665, 0.8240),
	float2(0.8240, 0.5665),
	float2(0.5665, 0.8240),

	float2(0.9796, 0.2007),
	float2(0.9796, -0.2007),
	float2(0.9796, 0.2007),
	float2(0.9796, -0.2007),
		
	float2(-0.8240, 0.5665),
	float2(-0.5665, 0.8240),
	float2(-0.8240, 0.5665),
	float2(-0.5665, 0.8240)
};			// Natural bokeh sampling directions
#endif

// Boris code (those variables are not used in my code - Matso)
float	EBlurSamplingRange = 4.0;
float	EApertureScale = 1.0;

//+++++++++++++++++++++++++++++
// External parameters, do not modify
//+++++++++++++++++++++++++++++
// Keyboard controlled temporary variables (in some versions exists in the config file). Press and hold key 1,2,3...8 together with PageUp or PageDown to modify.
// By default all set to 1.0
float4	tempF1; //0,1,2,3
float4	tempF2; //5,6,7,8
float4	tempF3; //9,0
// x=Width, y=1/Width, z=ScreenScaleY, w=1/ScreenScaleY
float4	ScreenSize;
// x=generic timer in range 0..1, period of 16777216 ms (4.6 hours), w=frame time elapsed (in seconds)
float4	Timer;
// Adaptation delta time for focusing
float	FadeFactor;

// Textures & samplers
texture2D texColor;
texture2D texDepth;
texture2D texNoise;
texture2D texPalette;
texture2D texFocus; // computed focusing depth
texture2D texCurr; // 4*4 texture for focusing
texture2D texPrev; // 4*4 texture for focusing

sampler2D SamplerColor = sampler_state
{
	Texture   = <texColor>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = LINEAR;
	AddressU  = Mirror;
	AddressV  = Mirror;
	SRGBTexture = FALSE;
	MaxMipLevel = 9;
	MipMapLodBias = 0;
};

sampler2D SamplerDepth = sampler_state
{
	Texture   = <texDepth>;
	MinFilter = POINT;
	MagFilter = POINT;
	MipFilter = NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

sampler2D SamplerNoise = sampler_state
{
	Texture   = <texNoise>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = LINEAR;
	AddressU  = Wrap;
	AddressV  = Wrap;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

sampler2D SamplerPalette = sampler_state
{
	Texture   = <texPalette>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

// for focus computation
sampler2D SamplerCurr = sampler_state
{
	Texture   = <texCurr>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

// For focus computation
sampler2D SamplerPrev = sampler_state
{
	Texture   = <texPrev>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

// For DoF only in PostProcess techniques
sampler2D SamplerFocus = sampler_state
{
	Texture   = <texFocus>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture = FALSE;
	MaxMipLevel = 0;
	MipMapLodBias = 0;
};

// Shaders input/output structures
struct VS_OUTPUT_POST
{
	float4 vpos  : POSITION;
	float2 txcoord : TEXCOORD0;
};

struct VS_INPUT_POST
{
	float3 pos  : POSITION;
	float2 txcoord : TEXCOORD0;
};

////////////////////////////////////////////////////////////////////
// Begin focusing (by Boris Vorontsov)
////////////////////////////////////////////////////////////////////
VS_OUTPUT_POST VS_Focus(VS_INPUT_POST IN)
{
	VS_OUTPUT_POST OUT;

	float4 pos = float4(IN.pos.x,IN.pos.y,IN.pos.z,1.0);

	OUT.vpos = pos;
	OUT.txcoord.xy = IN.txcoord.xy;

	return OUT;
}

//SRCpass1X=ScreenWidth;
//SRCpass1Y=ScreenHeight;
//DESTpass2X=4;
//DESTpass2Y=4;
float4 PS_ReadFocus(VS_OUTPUT_POST IN) : COLOR
{
#ifndef USE_MANUAL_FOCUS
	float res = tex2D(SamplerDepth, 0.5).x;
#else
	float res = fFocusDistance * tempF1.x;
#endif
	return res;
}

//SRCpass1X=4;
//SRCpass1Y=4;
//DESTpass2X=4;
//DESTpass2Y=4;
float4 PS_WriteFocus(VS_OUTPUT_POST IN) : COLOR
{
	float res = 0.0;
	float curr = tex2D(SamplerCurr, 0.5).x;
	float prev = tex2D(SamplerPrev, 0.5).x;

	res = lerp(prev, curr, saturate(FadeFactor));	// Time elapsed factor (application of the smooth transition of DoF when changing the focus distance)
	res = max(res, 0.0);

	return res;
}

technique ReadFocus
{
	pass P0
	{
		VertexShader = compile vs_3_0 VS_Focus();
		PixelShader  = compile ps_3_0 PS_ReadFocus();

		ZEnable = FALSE;
		CullMode = NONE;
		ALPHATESTENABLE = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		AlphaBlendEnable = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
}

technique WriteFocus
{
	pass P0
	{
		VertexShader = compile vs_3_0 VS_Focus();
		PixelShader  = compile ps_3_0 PS_WriteFocus();

		ZEnable = FALSE;
		CullMode = NONE;
		ALPHATESTENABLE = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		AlphaBlendEnable = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
}
////////////////////////////////////////////////////////////////////
// End focusing code
////////////////////////////////////////////////////////////////////

// Routines ////////////////////////////////////////////////////////
/**
 * Chromatic aberration function - given texture coordinate and a focus value
 * retrieves chromatically distorted color of the pixel. Each of the color
 * channels are displaced according to the pixel coordinate and its distance
 * from the center of the image.
 * (http://en.wikipedia.org/wiki/Chromatic_aberration)
 */
float4 ChromaticAberration(float2 tex)
{
	float d = distance(tex, float2(0.5, 0.5));
	float f = smoothstep(fBaseRadius, fFalloffRadius, d);
	float3 chroma = pow(f + fvChroma, fChromaPower);
	
	float2 tr = ((2.0 * tex - 1.0) * chroma.r) * 0.5 + 0.5;
	float2 tg = ((2.0 * tex - 1.0) * chroma.g) * 0.5 + 0.5;
	float2 tb = ((2.0 * tex - 1.0) * chroma.b) * 0.5 + 0.5;
	
	float3 color = float3(tex2D(SamplerColor, tr).r, tex2D(SamplerColor, tg).g, tex2D(SamplerColor, tb).b) * (1.0 - f);
	
	return float4(color, 1.0);
}

/**
 * Chromatic aberration done according to the focus factor provided. DoF out-of-focus value is applied.
 */
float4 ChromaticAberration(float2 tex, float outOfFocus)
{
	float d = distance(tex, float2(0.5, 0.5));
	float f = smoothstep(fBaseRadius, fFalloffRadius, d);
	float3 chroma = pow(f + fvChroma, CHROMA_POW * outOfFocus * fChromaPower);

	float2 tr = ((2.0 * tex - 1.0) * chroma.r) * 0.5 + 0.5;
	float2 tg = ((2.0 * tex - 1.0) * chroma.g) * 0.5 + 0.5;
	float2 tb = ((2.0 * tex - 1.0) * chroma.b) * 0.5 + 0.5;
	
	float3 color = float3(tex2D(SamplerColor, tr).r, tex2D(SamplerColor, tg).g, tex2D(SamplerColor, tb).b) * (1.0 - outOfFocus);
	
	return float4(color, 1.0);
}

/**
 * Pseudo-random number generator - returns a number generated according to the provided vector.
 */
float Random(float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

/**
 * Pseudo-random number generator - returns a vector generated according to the provided one.
 */
float2 Random2(float2 coord)
{
	float noiseX = ((frac(1.0-coord.x*(1920.0/2.0))*0.25)+(frac(coord.y*(1080.0/2.0))*0.75))*2.0-1.0;
	float noiseY = ((frac(1.0-coord.x*(1920.0/2.0))*0.75)+(frac(coord.y*(1080.0/2.0))*0.25))*2.0-1.0;
	
	noiseX = clamp(frac(sin(dot(coord ,float2(12.9898,78.233))) * 43758.5453),0.0,1.0)*2.0-1.0;
	noiseY = clamp(frac(sin(dot(coord ,float2(12.9898,78.233)*2.0)) * 43758.5453),0.0,1.0)*2.0-1.0;
	
	return float2(noiseX, noiseY);
}

/**
 * Movie grain function - returns a random, time scaled value for the given pixel coordinate.
 */
float Grain(float3 tex)
{
	float r = Random(tex.xy);
	float grain = sin(PI * tex.z * r * fGrainFreq) * fGrainScale * r;
	return grain;
}

/**
 * Bright pass - rescales sampled pixel to emboss bright enough value. Samples texture directly.
 */
float3 BrightPass(float2 tex)
{
	float3 c = tex2D(SamplerColor, tex).rgb;
    float3 bC = max(c - float3(fFlareLuminance, fFlareLuminance, fFlareLuminance), 0.0);
    float bright = dot(bC, 1.0);
    bright = smoothstep(0.0f, 0.5, bright);
    return lerp(0.0, c, bright);
}

/**
 * Bright pass - rescales given color to emboss bright enough value. Works on provided color.
 */
float3 BrightColor(float3 c)
{
    float3 bC = max(c - float3(fFlareLuminance, fFlareLuminance, fFlareLuminance), 0.0);
    float bright = dot(bC, 1.0);
    bright = smoothstep(0.0f, 0.5, bright);
    return lerp(0.0, c, bright);
}

/**
 * Bright pass - rescales given color to emboss bright enough value. Used for bokeh calculations.
 */
float3 BrightBokeh(float3 c)
{
    float3 bC = max(c - float3(fBokehLuminance, fBokehLuminance, fBokehLuminance), 0.0);
    float bright = dot(bC, 1.0);
    bright = smoothstep(0.0f, 0.5, bright);
    return lerp(0.0, c, bright);
}

/**
 * Anamorphic sampling function - scales pixel coordinate
 * to stratch the image along one of the axels.
 * (http://en.wikipedia.org/wiki/Anamorphosis)
 */
float3 AnamorphicSample(int axis, float2 tex, float blur)
{
	tex = 2.0 * tex - 1.0;
	if (!axis) tex.x /= -blur;
	else tex.y /= -blur;
	tex = 0.5 * tex + 0.5;
	return BrightPass(tex);
}

/**
 * Converts pixel color to gray-scale.
 */
float GrayScale(float3 sample)
{
	return dot(sample, float3(0.3, 0.59, 0.11));
}

/**
 * Returns an OBGE-like DoF factor for the given coordinates.
 */
float2 GetDoFFactor(float2 tex)
{
	float depth = linear(tex2D(SamplerDepth, tex).x);
	float z = depth * DEPTH_RANGE;
#ifndef USE_MANUAL_FOCUS
	float focus = linear(tex2D(SamplerFocus, 0.5).x);
	float s = focus * DEPTH_RANGE;
	float fpf = clamp(1.0 / s + fRetinaFocusPoint, fRelaxedFocusPower, fRelaxedFocusPower + fAccomodation);
	float c = fBaseBlurRadius * (fRetinaFocusPoint - fpf + 1.0 / z) / fRetinaFocusPoint / K;
#else
	float s = 10.0 * fFocusDistance * tempF1.x;
	float rfp = fRetinaFocusPoint * tempF1.y, rfpow = fRelaxedFocusPower * tempF1.y;
	float fpf = clamp(1.0 / s + rfp, rfpow, rfpow + fAccomodation);
	float c = fBaseBlurRadius * (rfp - fpf + 1.0 / z) / rfp / K;
#endif
	
	c = sign(z-s) * min(abs(c), fRadius);
	
#ifdef DONT_BLUR_WEAPON
	c *= smoothstep(fWeaponBlurCutoff - 0.05, fWeaponBlurCutoff, z);
#endif
	
	return float2(c, z);
}

///// Shaders ////////////////////////////////////////////////////////////////////////////////
// Vertex shader (Boris code)
VS_OUTPUT_POST VS_PostProcess(VS_INPUT_POST IN)
{
	VS_OUTPUT_POST OUT;

	float4 pos = float4(IN.pos.x, IN.pos.y, IN.pos.z, 1.0);

	OUT.vpos = pos;
	OUT.txcoord.xy = IN.txcoord.xy;

	return OUT;
}

// Sharpen pixel shader (Matso code)
float4 PS_ProcessPass_Sharpen(VS_OUTPUT_POST IN, float2 vPos : VPOS) : COLOR0
{
	float2 coord = IN.txcoord.xy;
	float4 Color = 9.0 * tex2D(SamplerColor, coord.xy);
	
	Color -= tex2D(SamplerColor, coord.xy + float2(-fvTexelSize.x, fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(0.0, fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(fvTexelSize.x, fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(fvTexelSize.x, 0.0) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(fvTexelSize.x, -fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(0.0, -fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(-fvTexelSize.x, -fvTexelSize.y) * fSharpScale);
	Color -= tex2D(SamplerColor, coord.xy + float2(-fvTexelSize.x, 0.0) * fSharpScale);
	
	Color.a = 1.0;
	return Color;
}

// Anamorphic lens flare pixel shader (Matso code)
float4 PS_ProcessPass_Anamorphic(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float4 res;
	float2 coord = IN.txcoord.xy;
	float3 anamFlare = AnamorphicSample(axis, coord.xy, fFlareBlur) * fFlareTint;
	
	res.rgb = anamFlare * fFlareIntensity;
	res.a = 1.0;

#if !defined(USE_SHARPENING)
	res.rgb += tex2D(SamplerColor, coord.xy).rgb;
#endif
	
	return res;
}

// Image grain pixel shader (Matso code)
float4 PS_ProcessPass_ImageGrain(VS_OUTPUT_POST IN, float2 vPos : VPOS) : COLOR0
{
	float4 res;
	float2 coord = IN.txcoord.xy;
	res.rgb = tex2D(SamplerColor, coord.xy).rgb;
	res.rgb += tex2D(SamplerNoise, coord.xy * 1024).rgb * Grain(float3(coord.xy, SEED));
	res.a = 1.0;
	return res;
}

// Simple pass through shader (Matso code) [NOTE: not used]
float4 PS_ProcessPass_None(VS_OUTPUT_POST IN, float2 vPos : VPOS) : COLOR0
{
	float4 res;
	float2 coord = IN.txcoord.xy;
	res.rgb = tex2D(SamplerColor, coord.xy).rgb;
	res.a = 1.0;
	return res;
}

// Complex 1.0 depth of field pixel shader (Matso code)
float4 DepthOfField_Complex_1_0(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float4 res;
	float2 base = IN.txcoord.xy;
	float4 tcol = tex2D(SamplerColor, base.xy);
	float sd = (axis == FIRST_PASS) ? tex2D(SamplerDepth, base).x : tcol.a;		// acquire scene depth for the pixel
	res = tcol;

#ifndef USE_SMOOTH_DOF								// sample focus value
	float sf = tex2D(SamplerDepth, 0.5).x - fFocusBias;
#else
	float sf = tex2D(SamplerFocus, 0.5).x - fFocusBias * 2.0;
#endif
	float outOfFocus = DOF(sd, sf);
	float blur = DOF_SCALE * outOfFocus;
	float wValue = 1.0;

#ifdef USE_MANUAL_BLUR_SCALE
	blur *= tempF1.w;
#endif

#ifdef USE_BOKEH_DOF
	blur *= BOKEH_DOWNBLUR;							// should bokeh be used, decrease blur a bit
#endif

	for (int i = 0; i < 4; i++)
	{
#ifndef USE_NATURAL_BOKEH
		float2 tdir = tds[axis] * fvTexelSize * blur * offset[i];
#else
		float2 tdir = tds[axis * 4 + i] * fvTexelSize * blur * offset[i];
#endif
		
		float2 coord = base + tdir.xy;
#ifdef USE_CHROMA_DOF
		float4 ct = ChromaticAberration(coord, outOfFocus);		// chromatic aberration sampling
#else
		float4 ct = tex2D(SamplerColor, coord);
#endif
		float sds = tex2D(SamplerDepth, coord).x;
		
		if ((abs(sds - sd) / sd) < fBlurCutoff) {			// blur 'bleeding' control
#ifndef USE_BOKEH_DOF
			float w = 1.0 + abs(offset[i]);				// weight blur for better effect
#else		
  #if USE_BOKEH_DOF == 1
  			BOKEH_FORMULA;
    #ifndef USE_BRIGHTNESS_LIMITING						// all samples above max input will be limited to max level
			float w = pow(b * fBokehIntensity, fBokehCurve);
    #else
	 #ifdef USE_ENHANCED_BOKEH
			float w = smoothstep(fBokehMin, fBokehMax, b * b) * fBokehMaxLevel;
	 #else
	 		float w = smoothstep(fBokehMin, fBokehMax, b) * fBokehMaxLevel;
	 #endif
    #endif
	#ifdef USE_WEIGHT_CLAMP
			w = min(w, fBokehMaxWeight);
	#endif
			w += abs(offset[i]) + blur;
  #endif
  #ifdef USE_SWITCHABLE_MODE
  			float w = 1.0 + abs(offset[i]);
  			
			if (tempF2.z > 0.99f) {
				BOKEH_FORMULA;
				w += smoothstep(fBokehMin, fBokehMax, b * b) * fBokehMaxLevel + blur;
			}
  #endif
#endif	
			tcol += ct * w;
			wValue += w;
		}
	}
	tcol /= wValue;
	
	res.rgb = tcol.rgb;
	res.w = (axis == FOURTH_PASS) ? 1.0 : sd;
	return res;
}

// Complex 1.1 depth of field pixel shader (Matso code)
float4 DepthOfField_Complex_1_1(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float4 res;
	float2 base = IN.txcoord.xy;
	float4 tcol = tex2D(SamplerColor, base.xy);
	res = tcol;

	float2 cz = GetDoFFactor(base);
	float outOfFocus = cz.x;
	//float depth = cz.y;
	float blur = 1.128 * outOfFocus;
	float wValue = 1.0;

#ifdef USE_MANUAL_BLUR_SCALE
	blur *= tempF1.w;
#endif

#ifndef USE_BOKEH_DOF
	blur *= 4.0;
#endif

	for (int i = 0; i < 4; i++)
	{
#ifndef USE_NATURAL_BOKEH
		float2 tdir = tds[axis] * fvTexelSize * blur * offset[i];
#else
		float2 tdir = tds[axis * 4 + i] * fvTexelSize * blur * offset[i];
#endif
		
		float2 coord = base + tdir.xy;
#ifdef USE_CHROMA_DOF
		float4 ct = ChromaticAberration(coord, outOfFocus * 0.001);	// chromatic aberration sampling
#else
		float4 ct = tex2D(SamplerColor, coord);
#endif
		//float2 s_cz = GetDoFFactor(coord);
		//float s_outOfFocus = s_cz.x;
		//float s_depth = s_cz.y;
		
#ifndef USE_BOKEH_DOF
		float w = 1.0 + abs(offset[i]);					// weight blur for better effect
#else		
  #if USE_BOKEH_DOF == 1
  		BOKEH_FORMULA;
    #ifndef USE_BRIGHTNESS_LIMITING						// all samples above max input will be limited to max level
		float w = pow(b * fBokehIntensity, fBokehCurve);
    #else
	 #ifdef USE_ENHANCED_BOKEH
		float w = smoothstep(fBokehMin, fBokehMax, b * b) * fBokehMaxLevel;
	 #else
	 	float w = smoothstep(fBokehMin, fBokehMax, b) * fBokehMaxLevel;
	 #endif
    #endif
	#ifdef USE_WEIGHT_CLAMP
		w = min(w, fBokehMaxWeight);
	#endif
		w += (1.0 + abs(offset[i]));
  #endif
  #ifdef USE_SWITCHABLE_MODE
  		float w = 1.0 + abs(offset[i]);
  			
		if (tempF2.z > 0.99f) {
			BOKEH_FORMULA;
			w += smoothstep(fBokehMin, fBokehMax, b * b) * fBokehMaxLevel;
		}
  #endif
#endif
		tcol += ct * w;
		wValue += w;
	}
	tcol /= wValue;
	
	res.rgb = tcol.rgb;
	res.w = outOfFocus / (2.0 * fRadius) + 0.5;
	return res;
}

// OBGE-like depth of field pixel shader (Matso code, based on Tomerk's)
float4 DepthOfField_OBGE(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float4 res;
	float2 base = IN.txcoord.xy;
	float c = GetDoFFactor(base).x;	
	float4 color = tex2D(SamplerColor, base.xy);	
	float weight = (1.0 / (c * c + 1)) * dot(color.rgb + 0.01, float3(0.2126, 0.7152, 0.0722));
    float amount = weight;
	
	color *= weight;
	
	for (int i = 0; i < 4; i++)
	{
#ifndef USE_NATURAL_BOKEH
		float2 tdir = tds[axis] * fvTexelSize * c * offset[i];
#else
		float2 tdir = tds[axis * 4 + i] * fvTexelSize * c * offset[i];
#endif
		float2 coord = base + tdir.xy;
#ifdef USE_CHROMA_DOF
		float4 ct = ChromaticAberration(coord, c * 0.001);
#else
		float4 ct = tex2D(SamplerColor, coord);
#endif
		float s_c = abs(GetDoFFactor(coord).x);
		
		BOKEH_FORMULA;
		weight = abs(offset[i]) + smoothstep(fBokehMin, fBokehMax, b * b) * fBokehMaxLevel;
		weight *= (1.0 / (s_c * s_c + 1)) * dot(ct.rgb + 0.01, float3(0.2126, 0.7152, 0.0722)) * (1-smoothstep(s_c, s_c * 1.1, abs(c)));
		
		color += ct * weight;
        amount += weight;
	}
	
	color.rgb /= amount;
	res.rgb = color.rgb;
	res.a = c / (2.0 * fRadius) + 0.5;
	
	return res;
}

// False (foggy) depth of field pixel shader (Matso code)
float4 DepthOfField_False(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float4 res;
	float2 coord = IN.txcoord.xy;
	float c = GetDoFFactor(coord).x;
#ifdef USE_CHROMA_DOF
	float4 color = ChromaticAberration(coord, c * 0.002);
#else
	float4 color = tex2D(SamplerColor, coord);
#endif

	float f = (1.0 / (c * c + 1.0));
#ifndef USE_SINGLE_PASS
	float3 fog = pow(color.rgb + 0.01, 0.75) * 0.65;
#else
	float3 fog = pow(color.rgb + 0.25, 0.75) * 0.35;
#endif
	
	color.rgb = lerp(fog, color.rgb, f);
	
	res = color;
	res.a = 1.0;
	
	return res;
}

// LOD (texture level-of-detail mip-map) depth of field pixel shader (Matso code) [NOTE: not working yet]
float4 DepthOfField_LOD(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR0
{
	float2 coord = IN.txcoord.xy;
	float4 res;
	float c = GetDoFFactor(coord).x;
	float f = 1.0 / (c * c + 1.0);
	float4 lod = float4(coord, 0.0, (1.0 - f) * 9.0);
	float4 color = tex2Dbias(SamplerColor, lod);	// [NOTE: the frame texture need to have 9 mipmaps, while has not]
		
	res = color;
	res.a = 1.0;
	
	return res;
}

// Chromatic aberration with no DoF (Matso code)
float4 PS_ProcessPass_Chroma(VS_OUTPUT_POST IN, float2 vPos : VPOS) : COLOR0
{
	float2 coord = IN.txcoord.xy;
	float4 result = ChromaticAberration(coord.xy);
	result.a = 1.0;
	return result;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifdef ENABLE_PREPASS	// you can add your shaders in here (prior DoF), just like the existing sharpening and ALF effects are placed here.
	technique PostProcess
	{
	#ifdef USE_SHARPENING
		pass P0
		{
			VertexShader = compile vs_3_0 VS_PostProcess();
			PixelShader  = compile ps_3_0 PS_ProcessPass_Sharpen();

			DitherEnable = FALSE;
			ZEnable = FALSE;
			CullMode = NONE;
			ALPHATESTENABLE = FALSE;
			SEPARATEALPHABLENDENABLE = FALSE;
			AlphaBlendEnable = FALSE;
			StencilEnable = FALSE;
			FogEnable = FALSE;
			SRGBWRITEENABLE = FALSE;
		}
	#endif
	#ifdef USE_ANAMFLARE
		pass P1
		{
		#if defined(USE_SHARPENING)
			AlphaBlendEnable = true;
			SrcBlend = One;
			DestBlend = One;
			
			PixelShader = compile ps_3_0 PS_ProcessPass_Anamorphic(fFlareAxis);
		#else
		
			VertexShader = compile vs_3_0 VS_PostProcess();
			PixelShader  = compile ps_3_0 PS_ProcessPass_Anamorphic(fFlareAxis);
		
			DitherEnable = FALSE;
			ZEnable = FALSE;
			CullMode = NONE;
			ALPHATESTENABLE = FALSE;
			SEPARATEALPHABLENDENABLE = FALSE;
			AlphaBlendEnable = FALSE;
			StencilEnable = FALSE;
			FogEnable = FALSE;
			SRGBWRITEENABLE = FALSE;
		#endif
		}
	#endif
		// Place Your effects here as an additional PREPASS pass...
	}
#endif

#ifndef ENABLE_DOF
	#ifdef ENABLE_CHROMA
		#ifndef ENABLE_PREPASS
			technique PostProcess
		#else
			technique PostProcess2
		#endif
		{
			pass P0
			{
				VertexShader = compile vs_3_0 VS_PostProcess();
				PixelShader  = compile ps_3_0 PS_ProcessPass_Chroma();

				DitherEnable = FALSE;
				ZEnable = FALSE;
				CullMode = NONE;
				ALPHATESTENABLE = FALSE;
				SEPARATEALPHABLENDENABLE = FALSE;
				AlphaBlendEnable = FALSE;
				StencilEnable = FALSE;
				FogEnable = FALSE;
				SRGBWRITEENABLE = FALSE;
			}
		}
	#endif
#endif

#ifndef ENABLE_CHROMA
	#ifdef ENABLE_DOF
		#ifndef ENABLE_PREPASS
			technique PostProcess
		#else
			technique PostProcess2
		#endif
		{
			pass P0
			{
				VertexShader = compile vs_3_0 VS_PostProcess();
				PixelShader  = compile ps_3_0 PS_ProcessPass_DepthOfField(FIRST_PASS);

				DitherEnable = FALSE;
				ZEnable = FALSE;
				CullMode = NONE;
				ALPHATESTENABLE = FALSE;
				SEPARATEALPHABLENDENABLE = FALSE;
				AlphaBlendEnable = FALSE;
				StencilEnable = FALSE;
				FogEnable = FALSE;
				SRGBWRITEENABLE = FALSE;
			}
		}
	  #ifndef USE_SINGLE_PASS
		#ifndef ENABLE_PREPASS
			technique PostProcess2
		#else
			technique PostProcess3
		#endif
		{
			pass P0
			{
				VertexShader = compile vs_3_0 VS_PostProcess();
				PixelShader  = compile ps_3_0 PS_ProcessPass_DepthOfField(SECOND_PASS);

				DitherEnable = FALSE;
				ZEnable = FALSE;
				CullMode = NONE;
				ALPHATESTENABLE = FALSE;
				SEPARATEALPHABLENDENABLE = FALSE;
				AlphaBlendEnable = FALSE;
				StencilEnable = FALSE;
				FogEnable = FALSE;
				SRGBWRITEENABLE = FALSE;
			}
		}

		#ifndef ENABLE_PREPASS
			technique PostProcess3
		#else
			technique PostProcess4
		#endif
		{
			pass P0
			{
				VertexShader = compile vs_3_0 VS_PostProcess();
				PixelShader  = compile ps_3_0 PS_ProcessPass_DepthOfField(THIRD_PASS);

				DitherEnable = FALSE;
				ZEnable = FALSE;
				CullMode = NONE;
				ALPHATESTENABLE = FALSE;
				SEPARATEALPHABLENDENABLE = FALSE;
				AlphaBlendEnable = FALSE;
				StencilEnable = FALSE;
				FogEnable = FALSE;
				SRGBWRITEENABLE = FALSE;
			}
		}

		#ifndef USE_HEXAGONAL_BOKEH
		
		#ifndef ENABLE_PREPASS
			technique PostProcess4
		#else
			technique PostProcess5
		#endif
		{
			pass P0
			{
				VertexShader = compile vs_3_0 VS_PostProcess();
				PixelShader  = compile ps_3_0 PS_ProcessPass_DepthOfField(FOURTH_PASS);

				DitherEnable = FALSE;
				ZEnable = FALSE;
				CullMode = NONE;
				ALPHATESTENABLE = FALSE;
				SEPARATEALPHABLENDENABLE = FALSE;
				AlphaBlendEnable = FALSE;
				StencilEnable = FALSE;
				FogEnable = FALSE;
				SRGBWRITEENABLE = FALSE;
			}
		}
		#endif
	  #endif
	#endif
#endif

#ifdef ENABLE_POSTPASS	// you can add your shaders in here (post DoF), just like the existing image grain effect placed here.
  #ifndef USE_SINGLE_PASS
	#ifndef ENABLE_PREPASS
	  #ifdef USE_HEXAGONAL_BOKEH
		technique PostProcess4
	  #else
		technique PostProcess5
	  #endif
	#else
	  #ifdef USE_HEXAGONAL_BOKEH
		technique PostProcess5
	  #else
		technique PostProcess6
	  #endif
	#endif
  #else
	#ifndef ENABLE_PREPASS
	  #ifdef USE_HEXAGONAL_BOKEH
		technique PostProcess
	  #else
		technique PostProcess2
	  #endif
	#else
	  #ifdef USE_HEXAGONAL_BOKEH
		technique PostProcess2
	  #else
		technique PostProcess3
	  #endif
	#endif	
  #endif
	{
	#ifdef USE_IMAGEGRAIN
		pass P0
		{
			VertexShader = compile vs_3_0 VS_PostProcess();
			PixelShader = compile ps_3_0 PS_ProcessPass_ImageGrain();
					
			DitherEnable = FALSE;
			ZEnable = FALSE;
			CullMode = NONE;
			ALPHATESTENABLE = FALSE;
			SEPARATEALPHABLENDENABLE = FALSE;
			AlphaBlendEnable = FALSE;
			StencilEnable = FALSE;
			FogEnable = FALSE;
			SRGBWRITEENABLE = FALSE;
		}
	#endif
		// Place Your effects here as an additional POSTPASS pass...
	}
#endif

//++++++++++++++++++++++++++++++++++++++++++++
// ENBSeries effect file
// visit http://enbdev.com for updates
// Copyright (c) 2007-2011 Boris Vorontsov
//++++++++++++++++++++++++++++++++++++++++++++
// Matso Immersive Bloom v4.1.7
// cmaster.matso@gmail.com
// Credits to Boris Vorontsov
//++++++++++++++++++++++++++++++++++++++++++++

//+++++++++++++++++++++++++++++
//internal parameters, can be modified
//+++++++++++++++++++++++++++++

// Effects options (if the 'USE_...' flags are disabled they are commented, prefixed with '//')
#define USE_TINTING	1						// commant it to disable tinting (1 - fixed color tinting, 2 - texture sample tinting)
//#define USE_ANAMFLARE	1						// comment it to disable anamorphic lens flare (ALF)

#define TINT_COLOR		float3(0.3, 0.2, 1.0)						// fixed color
#define TINT_TEXTURE	tex2D(SamplerBloom6, In.txcoord0.xy).rgb	// sampled color
#define TINT_LEVEL		0.75										// level of tint used in the process

#if USE_TINTING == 1							// definition of tint color
 #define TINT			TINT_COLOR
#elif USE_TINTING == 2
 #define TINT			TINT_TEXTURE
#endif

// Anamorphic flare parameters
#define fFlareLuminance 1.0						// bright pass luminance value 
#define fFlareBlur 20.0						// manages the size of the flare
#define fFlareIntensity 0.7					// effect intensity
#define fFlareTint	float3(0.0, 0.0, 1.0)		// effect tint
#define fFlareAxis	1							// blur axis

// Additional bloom parameters (radius indicates what area of the image will take part in bloom calculations accordining to single pixel)
#define BloomRadius0 0.122
#define BloomRadius1 0.524
#define BloomRadius2 1.282

//+++++++++++++++++++++++++++++
//external parameters, do not modify
//+++++++++++++++++++++++++++++
//keyboard controlled temporary variables (in some versions exists in the config file). Press and hold key 1,2,3...8 together with PageUp or PageDown to modify. By default all set to 1.0
float4	tempF1; //0,1,2,3
float4	tempF2; //5,6,7,8
float4	tempF3; //9,0
//x=Width, y=1/Width, z=ScreenScaleY, w=1/ScreenScaleY
float4	ScreenSize;
//x=generic timer in range 0..1, period of 16777216 ms (4.6 hours), w=frame time elapsed (in seconds)
float4	Timer;
//additional info for computations
float4	TempParameters; 
//Lenz reflection intensity, lenz reflection power
float4	LenzParameters;
//BloomRadius1, BloomRadius2, BloomBlueShiftAmount, BloomContrast
float4	BloomParameters;

// Textures & samplers
texture2D texBloom1;
texture2D texBloom2;
texture2D texBloom3;
texture2D texBloom4;
texture2D texBloom5;
texture2D texBloom6;
texture2D texBloom7;//additional bloom tex
texture2D texBloom8;//additional bloom tex

sampler2D SamplerBloom1 = sampler_state
{
    Texture   = <texBloom1>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom2 = sampler_state
{
    Texture   = <texBloom2>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom3 = sampler_state
{
    Texture   = <texBloom3>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom4 = sampler_state
{
    Texture   = <texBloom4>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom5 = sampler_state
{
    Texture   = <texBloom5>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom6 = sampler_state
{
    Texture   = <texBloom6>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom7 = sampler_state
{
    Texture   = <texBloom7>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D SamplerBloom8 = sampler_state
{
    Texture   = <texBloom8>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;//NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

// Shaders input/output structures
struct VS_OUTPUT_POST
{
	float4 vpos  : POSITION;
	float2 txcoord0 : TEXCOORD0;
};
struct VS_INPUT_POST
{
	float3 pos  : POSITION;
	float2 txcoord0 : TEXCOORD0;
};

/**
 * Bright pass - rescales sampled pixel to emboss bright enough value.
 */
float3 BrightPass(float2 tex)
{
	float3 c = tex2D(SamplerBloom2, tex).rgb;
    float3 bC = max(c - float3(fFlareLuminance, fFlareLuminance, fFlareLuminance), 0.0);
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

// Constants ///////////////////////////////////////////////////////////////
const float2 bloomOffset[8]=
{
	float2(0.707, 0.707),
	float2(0.707, -0.707),
	float2(-0.707, 0.707),
	float2(-0.707, -0.707),
	float2(0.0, 1.0),
	float2(0.0, -1.0),
	float2(1.0, 0.0),
	float2(-1.0, 0.0)
};

// deepness, curvature, inverse size
const float3 offset[4]=
{
	float3(1.6, 4.0, 1.0),
	float3(0.7, 0.25, 2.0),
	float3(0.3, 1.5, 0.5),
	float3(-0.5, 1.0, 1.0)
};

// color filter per reflection
const float3 factors[4]=
{
	float3(0.3, 0.4, 0.4),
	float3(0.2, 0.4, 0.5),
	float3(0.5, 0.3, 0.7),
	float3(0.1, 0.2, 0.7)
};

// Shaders //////////////////////////////////////////////////////////////////

// Vertex shader (screen-aligned mesh)
VS_OUTPUT_POST VS_Bloom(VS_INPUT_POST IN)
{
	VS_OUTPUT_POST OUT;
	OUT.vpos = float4(IN.pos.x, IN.pos.y, IN.pos.z, 1.0);
	OUT.txcoord0.xy = IN.txcoord0.xy + TempParameters.xy;

	return OUT;
}

// Anamorphic lens flare pixel shader (Matso code)
float4 PS_ProcessPass_Anamorphic(VS_OUTPUT_POST IN, float2 vPos : VPOS, uniform int axis) : COLOR
{
	float4 res;
	float2 coord = IN.txcoord0.xy;
	float3 anamFlare = AnamorphicSample(axis, coord.xy, fFlareBlur) * fFlareTint;
	
	res.rgb = anamFlare * fFlareIntensity;
	res.a = 1.0;
	
	return res;
}

//zero pass HQ, input texture is fullscreen
//SamplerBloom1 - fullscreen texture
float4 PS_BloomPrePass(VS_OUTPUT_POST In) : COLOR
{
	float4 bloomuv;
	float4 bloom = 0.0;
	float2 screenfact = TempParameters.z;
	float4 srcbloom = bloom;
	
	screenfact.y *= ScreenSize.z;
	
	for (int i = 0; i < 4; i++)
	{
		bloomuv.xy = bloomOffset[i] * BloomRadius0;
		bloomuv.xy = (bloomuv.xy * screenfact.xy) + In.txcoord0.xy;		
		bloom.xyz += tex2D(SamplerBloom1, bloomuv.xy).xyz * 1.524;
	}
	bloom.xyz *= 0.164041994;

	bloom.xyz = min(bloom.xyz, 32768.0);
	bloom.xyz = max(bloom.xyz, 0.0);	
	return bloom;
}

//first and second passes draw to every texture
//twice, after computations of these two passes,
//result is set as input to next cycle

//first pass
//SamplerBloom1 is result of prepass or second pass from cycle
float4 PS_BloomTexture1(VS_OUTPUT_POST In) : COLOR
{
	float4 bloomuv;
	float4 bloom = tex2D(SamplerBloom1, In.txcoord0);
	float2 screenfact = TempParameters.z;
	float4 srcbloom = bloom;
	float4 bloomadd = bloom;
	float step = BloomRadius1 * BloomParameters.x;
	
	screenfact.y *= ScreenSize.z;
	screenfact.xy *= step;

	for (int i = 0; i < 8; i++)
	{
		bloomuv.xy = bloomOffset[i];
		bloomuv.xy = (bloomuv.xy * screenfact.xy) + In.txcoord0.xy;
		bloom += tex2D(SamplerBloom1, bloomuv.xy) * 1.524;
	}
	bloom *= 0.082020997;

#ifdef USE_TINTING
	float3 tint = TINT * TINT_LEVEL;
	float ttt = max(dot(bloom.xyz, 0.333) - dot(srcbloom.xyz, 0.333), 0.0);
	float gray = BloomParameters.z * ttt * 10.0;	
	float mixfact = (gray / (1.0 + gray));
	
	mixfact *= 1.0 - saturate((TempParameters.w - 1.0) * 0.2);
	tint.xy += saturate((TempParameters.w - 1.0) * 0.3);
	tint.xy = saturate(tint.xy);
	
	bloom.xyz *= lerp(1.0, tint.xyz, mixfact);
#endif

	bloom.w = 1.0;
	
	return bloom;
}

//second pass
//SamplerBloom1 is result of first pass
float4 PS_BloomTexture2(VS_OUTPUT_POST In) : COLOR
{
	float4 bloomuv;
	float4 bloom = tex2D(SamplerBloom1, In.txcoord0);
	float2 screenfact = TempParameters.z;
	float4 srcbloom = bloom;
	float step = BloomRadius2 * BloomParameters.y;
	float4 rotvec = 0.0;
	
	screenfact.y *= ScreenSize.z;
	screenfact.xy *= step;
	sincos(0.3927, rotvec.x, rotvec.y);
	
	for (int i = 0; i < 8; i++)
	{
		bloomuv.xy = bloomOffset[i];
		bloomuv.xy = reflect(bloomuv.xy, rotvec.xy);///????????
		bloomuv.xy = (bloomuv.xy * screenfact.xy) + In.txcoord0.xy;
		bloom += tex2D(SamplerBloom1, bloomuv.xy) * 1.524;
	}
	bloom *= 0.082020997;
	bloom.w = 1.0;
	return bloom;
}

//last pass, mix several bloom textures
//SamplerBloom5 is the result of prepass
//float4 PS_BloomPostPass(float2 vPos : VPOS ) : COLOR
float4 PS_BloomPostPass(VS_OUTPUT_POST In) : COLOR
{
	float4 bloom;
	//v1
	bloom = tex2D(SamplerBloom1, In.txcoord0);
	bloom += tex2D(SamplerBloom2, In.txcoord0);
	bloom += tex2D(SamplerBloom3, In.txcoord0);
	bloom += tex2D(SamplerBloom4, In.txcoord0);
	bloom += tex2D(SamplerBloom7, In.txcoord0);
	bloom += tex2D(SamplerBloom8, In.txcoord0);
	bloom += tex2D(SamplerBloom5, In.txcoord0);
	bloom *= 0.142857142;

	float3 lenz = 0;
	float2 lenzuv = 0.0;

	if (LenzParameters.x > 0.00001)
	{
		for (int i = 0; i < 4; i++)
		{
			float2 distfact = (In.txcoord0.xy - 0.5);
			lenzuv.xy = offset[i].x * distfact;
			lenzuv.xy *= pow(2.0 * length(float2(distfact.x * ScreenSize.z, distfact.y)), offset[i].y);
			lenzuv.xy *= offset[i].z;
			lenzuv.xy = 0.5 - lenzuv.xy;
			
			float3 templenz = tex2D(SamplerBloom2, lenzuv.xy);
			templenz = templenz * factors[i];
			distfact = (lenzuv.xy-0.5);
			distfact *= 2.0;
			templenz *= saturate(1.0 - dot(distfact, distfact));//limit by uv 0..1
			
			float maxlenz = max(templenz.x, max(templenz.y, templenz.z));
			float tempnor = (maxlenz / (1.0 + maxlenz));
			tempnor = pow(tempnor, LenzParameters.y);
			templenz.xyz *= tempnor;
			
			lenz += templenz;
		}
		lenz.xyz *= 0.25 * LenzParameters.x;

		bloom.xyz += lenz.xyz;
		bloom.w = max(lenz.xyz, max(lenz.y, lenz.z));
	}

	return bloom;
}

//---------------------------------------------------------------------------------

technique BloomPrePass
{
    pass p0
    {
		VertexShader = compile vs_3_0 VS_Bloom();
		PixelShader  = compile ps_3_0 PS_BloomPrePass();

		ColorWriteEnable = ALPHA|RED|GREEN|BLUE;
		CullMode = NONE;
		AlphaBlendEnable = FALSE;
		AlphaTestEnable = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
}

technique BloomTexture1
{
    pass p0
    {
		VertexShader = compile vs_3_0 VS_Bloom();
		PixelShader  = compile ps_3_0 PS_BloomTexture1();

		ColorWriteEnable = ALPHA|RED|GREEN|BLUE;
		CullMode = NONE;
		AlphaBlendEnable = FALSE;
		AlphaTestEnable = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
}


technique BloomTexture2
{
    pass p0
    {
		VertexShader = compile vs_3_0 VS_Bloom();
		PixelShader  = compile ps_3_0 PS_BloomTexture2();

		ColorWriteEnable = ALPHA|RED|GREEN|BLUE;
		CullMode = NONE;
		AlphaBlendEnable = FALSE;
		AlphaTestEnable = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
}

technique BloomPostPass
{
    pass p0
    {
		VertexShader = compile vs_3_0 VS_Bloom();
		PixelShader  = compile ps_3_0 PS_BloomPostPass();

		ColorWriteEnable = ALPHA|RED|GREEN|BLUE;
		CullMode = NONE;
		AlphaBlendEnable = FALSE;
		AlphaTestEnable = FALSE;
		SEPARATEALPHABLENDENABLE = FALSE;
		FogEnable = FALSE;
		SRGBWRITEENABLE = FALSE;
	}
	
#ifdef USE_ANAMFLARE
	pass p1
	{
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
			
		PixelShader = compile ps_3_0 PS_ProcessPass_Anamorphic(fFlareAxis);
	}
#endif
}

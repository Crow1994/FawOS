
//++++++++++++++++++++++++++++++++++++++++++++
// ENBSeries effect file
// visit http://enbdev.com for updates
// Copyright (c) 2007-2011 Boris Vorontsov
//
// Using decompiled shader of TES Skyrim
//++++++++++++++++++++++++++++++++++++++++++++
// Additions and Tweaking by HD6 (HeliosDoubleSix) v11.3
// MOD by HD6: http://www.skyrimnexus.com/downloads/file.php?id=4142
// MOD by ENB: http://www.skyrimnexus.com/downloads/file.php?id=2971
// these are typically denoted by 'HD6'
// given I have no shader writing experience,
// will undoubtedly be retarded to a seasoned professional, I welcome any advice!
// thanks Boris!
//++++++++++++++++++++++++++++++++++++++++++++

// Remove or add '//' infront of '#define' below to disable / enable the various options

// ENB - use original game processing first, then mine
	#define APPLYGAMECOLORCORRECTION

// HD6 - Color Balance and Brightness, Contrast adjustment
	//
	#define HD6_COLOR_TWEAKS
	//#define HD6_DARKER_NIGHTS
	// Right now if you do NOT darken the nights, the night sky will be very bright unless changes in enbseries.ini
	//
	// Put // or remove the // infront of "#define HD6_DARKER_NIGHTS" above, to darken the nights or not.. // will put nights back to normal
	//
	// This is by default and for examples sake set to make the nights even darker
	// When you decrease brightness you will need to increase contrast or it will look washed out
	// and if you increrase contrast that naturally increases saturation so you need to reduce saturation
	// this is just the nature of color correction :-)
	//
	// Note nights and days are being desaturated per channel(RGB) using: HD6_COLORSAT_DAYNIGHT elsewhere
	// Note Bloom is not affected by this and gets its own color/contrast adjustment in HD6_BLOOM_CRISP and HD6_BLOOM_DEBLUEIFY if used
	//
	// If the 3 numbers in rgb below do not add up to 3 it will either be darker or lighter than before
	float3 rgbd 	= float3( 1, 1, 1 ); 		// RGB balance day
	float3 rgbn 	= float3( 1, 1, 1 ); 		// RGB balance night
	//
	// First column of the 3 here is for key control only during night
	// ie you can press number '1' with pageup or pagedown to alter the brightness while playing
	// As you press pagedown it moves further towards the values in this first column.
	// As a helpful indicator you can see if you have moved away from default values by
	// noticing a very small white dot in top left corner of screen.
	// Adjust with keys till this dot vanishes, then it is back to default
	// The Keycontrol ONLY affects the night right now. So dont try using it during the day it wont do anything.
	
	
		// Darker Nights ( Night Keypress 1+Pageup/down, Night, Day )
		// Only uses these values if "#define HD6_DARKER_NIGHTS" does not have '//' infront
		
		//		keypress 1,2:	  night, day		night, day
		float4 uctbrt1 	= float4( 0.50, 0.30, 		0.875, 1.155 ); 	// Brightness Night, Day (Alters before contrast adjustment)
		float4 uctbrt2	= float4( 0.50, 0.30, 		0.925, 1.185 ); 	// Brightness Night, Day (Alters after contrast adjustment)
		float4 uctcon 	= float4( 0.5, 0.6, 		1.12, 1.075 ); 	// Contrast Night, Day, v11.2: 1.0, 0.97, 0.85
		float4 uctsat	= float4( -0.3, 0.3, 		0.8, 1.02 ); 	// Saturation Night, Day (Remember if using HD6_COLORSAT_DAYNIGHT that will also be desaturating the night)

	#ifdef HD6_DARKER_NIGHTS
		float4 darkenby1 = float4( 0.0, 0.0, 		0.0, 0.0 );
	#endif
				
	//
	// I have stopped relying on the palette map to darken nights, now I do all darkening here
	// When reducing brightness it seems increasing saturation is needed to restore color, anda slight increase in contrast to maintain bright spots/flames etc
	// Remember this is not darkening the bloom, which in itself has as impact on the overall brightness and hazyness of the scene
	//
	// Palette map right now is increasing contrast so I have compensated by reducing contrast here (lazy)

// HD6 - Enable Vignette - darkens and blurs edges of the screen which increasesfocus on center, film/camera type effect/look
	// didnt bother adding blur, could do without muddying and fuzzing things really
	// and the effect is only meant to be super subtle not a pin hole camera -_-
	//
	//#define HD6_VIGNETTE
	//
	// Defaults below, I darken the corners and the bottom only, leaving the top light
	// darkening all sides feels ike you are trapping/closing in the view too much, so it is not a normal vignette
	// And it is subtle, till you turn it off I doubt you would ever even notice it
	// Also is turned off at night
	//
	float rovigpwr = 100.0; // For Round vignette // 0.2
	float2 sqvigpwr = float2( 1.5 , 1.5); // For square vignette: (top, bottom)
	//
	float vsatstrength = 0.0; // How saturated vignette is
	float vignettepow = 0.0; // For Round vignette, higher pushes it to the corners and increases contrast/sharpness
	//
	float vstrengthatnight = 1.0; // How strong vignette is as night, 0-1
//

// HD6 - Desaturate Nights, can alter saturation seperately from day and night, will affect caves and indoors also for now
	//
	#define HD6_COLORSAT_DAYNIGHT
	//
	// Nighttime Saturation, Red, Green, Blue
	float3 dnsatn = float3( 1.0, 1, 1 );
	//
	// Daytime Saturation, Red, Green, Blue
	float3 dnsatd = float3( 1, 1, 1 );
//

// HD6 - removes blue tint from bloom, most apparent in distant fog
	// HeliosDoubleSix cobbled code to deblueify bloom without loosing brightness huzah! - First time writing shader code so be gentle
	// desaturates bloom, to do this you cant just remove a color or tone it down you need to redistribute it evenly across all channels to make it grey
	// well evenly would make sense but the eye has different sensetivities to color so its actually RGB (0.3, 0.59, 0.11) to achieve greyscale
	// Careful as removing too muchblue can send snow and early morning pink	
	//	
//	float3 nsat = float3( 0.75, 0.762, 0.36 );
//	float3 nsat = float3( 2.5245, 2.7027, 1.85625 );
//	float3 nsat = float3( 1,1,0.7 );
//	float3 nsat = float3( 9.1,8.9,6.2 );
//	float3 nsat = float3( 3.1,3.04,2.032 );
//	float3 nsat = float3( 2.17,2.128,1.4224 );
//	float3 nsat = float3( 1.605,1.596,1.2918 );
	float3 nsat = float3( 3.255,3.192,2.6436 );
//

// Keyboard controlled temporary variables (in some versions exists in the config file).
// Press and hold key 1,2,3...8 together with PageUp or PageDown to modify. By default all set to 1.0
	float4	tempF1; // 0,1,2,3
	float4	tempF2; // 5,6,7,8
	float4	tempF3; // 9,0

// x=generic timer in range 0..1, period of 16777216 ms (4.6 hours), w=frame time elapsed (in seconds)
	float4	Timer;
// x=Width, y=1/Width, z=ScreenScaleY, w=1/ScreenScaleY
	float4	ScreenSize;
// changes in range 0..1, 0 means that night time, 1 - day time
	float	ENightDayFactor;
	
// enb version of bloom applied, ignored if original post processing used
	float	EBloomAmount;

//+++++++++++++++++++++++++++++
	// HD6 - Adaptation is now ignored by my choice
	float	EAdaptationMinV2 = 0.19; // 0.28 // lower gets brighter
	
	// Increase this to darken days, but darkening them will kill the sky a bit unless you enable the SKY overirde in enberies.ini
	float	EAdaptationMaxV2 = 0.30; // 0.30 // 0.65 // 0.35 // 0.29

	// Set ridiculously high, was 8, was in attempt to keep hair colour intact
	float	EToneMappingCurveV2 = 2; // 130

	// Adjusting this will throw out all the other values, icreased to high levels to combat how high I increased ToneMappingCurve to bring some contrast back in to Qdaytime
	float	EIntensityContrastV2 = 2.1; // 3.375 // 4.75 // 3.975

	// high saturation also helps pop the pink/orange sunsets/mornings at 6.30pm and 7.30am, but also nights then get very blue
	// Increasing this will darken things in the process
	// v11.2 = 3.0, 1.0 increased to put even more color into the game
	float	EColorSaturationV2 = 1.875; // 1.65;
	float 	HCompensateSat = 1; // Compensate for darkening caused by increasing EColorSaturationV2	

	// Not using this now anymore
	float	EToneMappingOversaturationV2 = 1000.0;
//+++++++++++++++++++++++++++++
//external parameters, do not modify
//+++++++++++++++++++++++++++++
	texture2D texs0; // color
	texture2D texs1; // bloom skyrim
	texture2D texs2; // adaptation skyrim
	texture2D texs3; // bloom enb
	texture2D texs4; // adaptation enb
	texture2D texs7; // palette enb

sampler2D _s0 = sampler_state {
	Texture   = <texs0>;
	MinFilter = POINT;
	MagFilter = POINT;
	MipFilter = NONE; // LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D _s1 = sampler_state {
	Texture   = <texs1>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE; // LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D _s2 = sampler_state {
	Texture   = <texs2>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE; // LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D _s3 = sampler_state {
	Texture   = <texs3>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE; // LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D _s4 = sampler_state {
	Texture   = <texs4>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE; // LINEAR;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

sampler2D _s7 = sampler_state {
	Texture   = <texs7>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	MipFilter = NONE;
	AddressU  = Clamp;
	AddressV  = Clamp;
	SRGBTexture=FALSE;
	MaxMipLevel=0;
	MipMapLodBias=0;
};

struct VS_OUTPUT_POST {
	float4 vpos  	: POSITION;
	float2 txcoord0 : TEXCOORD0;
};

struct VS_INPUT_POST {
	float3 pos 		: POSITION;
	float2 txcoord0 : TEXCOORD0;
};

VS_OUTPUT_POST VS_Quad(VS_INPUT_POST IN){
	VS_OUTPUT_POST OUT;
	OUT.vpos=float4(IN.pos.x,IN.pos.y,IN.pos.z,1.0);
	OUT.txcoord0.xy=IN.txcoord0.xy;
	return OUT;
};

//skyrim shader specific externals, do not modify
	float4 _c1 : register(c1); float4 _c2 : register(c2); float4 _c3 : register(c3);
	float4 _c4 : register(c4); float4 _c5 : register(c5);

float4 PS_D6EC7DD1(VS_OUTPUT_POST IN, float2 vPos : VPOS) : COLOR {
	float4 _oC0 = 0.0; // output
	float4 _c6 = float4(0, 0, 0, 0);
	float4 _c7 = float4(0.212500006, 0.715399981, 0.0720999986, 1.0);
	float4 r0; float4 r1; float4 r2; float4 r3; float4 r4; float4 r5; float4 r6;
	float4 r7; float4 r8; float4 r9; float4 r10; float4 r11; float4 _v0=0.0;
	_v0.xy = IN.txcoord0.xy;
    r1=tex2D(_s0, _v0.xy); // color
	r11=r1; // my bypass
	_oC0.xyz=r1.xyz; // for future use without game color corrections
	
	// HD6 - You can play with the night/day value here, not that its advisable to :-D
	// Visualize this with 'debug triangle' further down
	float hnd = ENightDayFactor;
	
	float2 hndtweak = float2( 3.1 , 1.5 );
	float vhnd = hnd; // effects vignette stregth;
	float bchnd = hnd; // effects hd6 bloom crisp
	float cdhnd = hnd; // effects hd6 colorsat daynight
	
	// Some caves are seen as daytime, so I set key 3 to force nightime
	// This doesnt work very well >_<
	hnd = tempF1.z < 1 ? 0 : hnd;
	hndtweak.x = tempF1.z < 1 ? hndtweak.y : hndtweak.x; // Dont ask, I have no idea why I need this lol	
	
	// HD6 - Alter Brightness using keyboard during gameplay
		
		float4 tuctbrt1 = uctbrt1;
		float4 tuctbrt2 = uctbrt2;
		float4 tuctcon  = uctcon;
		float4 tuctsat  = uctsat;
		
		#ifdef HD6_DARKER_NIGHTS
			tuctbrt1 -= darkenby1;
		#endif
				
		float h1 = lerp(-1,1,tempF1.x); // Increases speed it changes by when pressing key		
		h1 = lerp( h1, 1, hnd ); // Removes affect during day		
		h1 = h1 - (h1 % 0.1); // Changes it so incriments are in steps, remove this if you want smooth changes when pressing keys
		//float hbs = EBloomAmount;
		float hbs = lerp( EBloomAmount/2, EBloomAmount, h1); // Reduce bloom as it gets darker, otherwise it just gets hazier, higher number reduces bloom more as it gets darker
		
		float h2 = lerp(-1,1,tempF1.y); // Increases speed it changes by when pressing key
		h2 = lerp( 1, h2, hnd ); // Removes affect during night
		h2 = h2 - (h2 % 0.1); // Changes it so incriments are in steps, remove this if you want smooth changes when pressing keys
		hbs = lerp( (hbs/2)-1, hbs, h2); // Reduce bloom as it gets darker, otherwise it just gets hazier, higher number reduces bloom more as it gets darker
		hbs = max(0,hbs);
		hbs = min(2,hbs); // should be able to go above 1, but not 2
		
		vhnd = lerp(-2,hnd,h2);
		vhnd = max(0,vhnd); // do not go below 0;
		vhnd = min(1,vhnd); // not above 1, just incase people like surface of sun

		cdhnd=bchnd=vhnd;
	
		#ifdef HD6_COLOR_TWEAKS
			float2 uctbrt1t = 	float2( lerp( tuctbrt1.x, 	tuctbrt1.z, h1), lerp( tuctbrt1.y, 	tuctbrt1.w, h2) );
			float2 uctbrt2t = 	float2( lerp( tuctbrt2.x, 	tuctbrt2.z,	h1), lerp( tuctbrt2.y, 	tuctbrt2.w, h2) );			
			float2 uctcont  =	float2( lerp( tuctcon.x, 	tuctcon.z, 	h1), lerp( tuctcon.y, 	tuctcon.w, h2) );
			float2 uctsatt  =	float2( lerp( tuctsat.x, 	tuctsat.z, 	h1), lerp( tuctsat.y, 	tuctsat.w, h2) );
		#endif
		
	////
	
	
	
	#ifdef APPLYGAMECOLORCORRECTION
		//apply original
		r0.x=1.0/_c2.y;
		r1=tex2D(_s2, _v0);

		//r1.xyz = lerp( 0.28, 0.5, hnd ); // HD6 - disable vanilla adapation... because it drives me CRAAAZY!!!!! >_<
		//r1.xyz+=1.0;
		r1.xyz = lerp( min( 0.28, r1.xyz ), 0.5, hnd ); // Ligthen if dark, but do not darken if too light, we do this elsewhere for extreme bright situations
		// No seriously it screws up when looking at bright lights at night and the sky during day
		
		
		
		r0.yz=r1.xy * _c1.y;
		r0.w=1.0/r0.y;
		r0.z=r0.w * r0.z;
		r1=tex2D(_s0, _v0);
		r1.xyz=r1 * _c1.y;
		r0.w=dot(_c7.xyz, r1.xyz);
		r1.w=r0.w * r0.z;
		r0.z=r0.z * r0.w + _c7.w;
		r0.z=1.0/r0.z;
		r0.x=r1.w * r0.x + _c7.w;
		r0.x=r0.x * r1.w;
		r0.x=r0.z * r0.x;
		if (r0.w<0) r0.x=_c6.x;
		r0.z=1.0/r0.w;
		r0.z=r0.z * r0.x;
		r0.x=saturate(-r0.x + _c2.x);

		r2=tex2D(_s1, _v0);//skyrim bloom
		r2.xyz=0.0; // Screw it bloom should not happen here at all so just set to 0
		r2+=0.0; // HD6 - I add 0.1 to lighten it a bit, probably not great place to do it now

		r2.xyz=r2 * _c1.y;
		r2.xyz=r0.x * r2;
				
		r1.xyz=r1 * r0.z + r2;
		r0.x=dot(r1.xyz, _c7.xyz);
		r1.w=_c7.w;
		
		r2=lerp(r0.x, r1, _c3.x);
			
		r1=r0.x * _c4 - r2;
		r1=_c4.w * r1 + r2;
		r1=_c3.w * r1 - r0.y; // khajiit night vision _c3.w
		r0=_c3.z * r1 + r0.y;
		r1=-r0 + _c5;
		
		_oC0=_c5.w * r1 + r0;
	#endif // APPLYGAMECOLORCORRECTION

	float4 color=_oC0;		
	
	//HD6 brighten when not using original gamma, so they are at least similiar
	// Bloom is diminshed for some reason, oh well, i dont use this
	#ifndef APPLYGAMECOLORCORRECTION
		color*=1.2;
		color+=0.1;			
	#endif
	
	#ifdef HD6_COLORSAT_DAYNIGHT
		// HeliosDoubleSix code to Desaturate at night	
		// What channels to desaturate by how much, so you could just reduce blue at night and nothing else	
		// doesnt seem quite right will tinge things green if you just remove blue :-/ thought perhaps that makes perfect sense :-) *brain hurts*
		// Remember this affects caves, so might be best to remove saturation from nighttime direct and ambient light
		float3 nsatn=lerp(dnsatd,dnsatn,1-cdhnd); // So it has less to different/no effect during day
			//nsatn*=(1-cdhnd); // affect by night time value:
		float3 oldcoln = color.xyz; // store old values
		color.xyz *= nsatn; // adjust saturation	
		
		// spread lost luminace over everything
			//float3 greycn = float3(0.299, 0.587, 0.114); // perception of color luminace
		float3 greycn = float3(0.333,0.333,0.333); // screw perception
			//greycn = float3(0.811,0.523,0.996);
		color.xyz += (oldcoln.x-(oldcoln.x*nsatn.x)) * greycn.x;
		color.xyz += (oldcoln.y-(oldcoln.y*nsatn.y)) * greycn.y;
		color.xyz += (oldcoln.z-(oldcoln.z*nsatn.z)) * greycn.z;
	#endif	

	//adaptation in time
	float4	Adaptation=tex2D(_s4, 0.5);
	float	grayadaptation=max(max(Adaptation.x, Adaptation.y), Adaptation.z);
		//grayadaptation=1.0/grayadaptation;

	float4	xcolorbloom=tex2D(_s3, _v0.xy); //bloom
	// store old values
	float3 oldcol=xcolorbloom.xyz;
			
	// adjust saturation
	xcolorbloom.xyz *= nsat;
	float3 greyc = float3(0.333,0.333,0.333); // screw perception
	xcolorbloom.xyz += (oldcol.x-(oldcol.x*nsat.x)) * greyc.x;
	xcolorbloom.xyz += (oldcol.y-(oldcol.y*nsat.y)) * greyc.y;
	xcolorbloom.xyz += (oldcol.z-(oldcol.z*nsat.z)) * greyc.z;


	// Altering color balance is confusing, also Im not entirely sure it works properly :-D
	#ifdef HD6_COLOR_TWEAKS
		float ctbrt1 = lerp(uctbrt1t.x,uctbrt1t.y,hnd); // Brightness Night, Day (Alters before contrast adjustment)
		float ctbrt2 = lerp(uctbrt2t.x,uctbrt2t.y,hnd); // Brightness Night, Day (Alters after contrast adjustment)
		float ctcon = lerp(uctcont.x,uctcont.y,hnd); // Contrast Night, Day
		float ctsat = lerp(uctsatt.x,uctsatt.y,hnd); // Saturation Night, Day
		
		float3 ctLumCoeff = float3(0.2125, 0.7154, 0.0721);				
		float3 ctAvgLumin = float3(0.5, 0.5, 0.5);
		float3 ctbrtColor = color.rgb * ctbrt1;

		float3 ctintensity = dot(ctbrtColor, ctLumCoeff);
		float3 ctsatColor = lerp(ctintensity, ctbrtColor, ctsat); 
		float3 cconColor = lerp(ctAvgLumin, ctsatColor, ctcon);
		
		color.xyz = cconColor * ctbrt2;
		float3 cbalance = lerp(rgbn,rgbd,hnd);
		color.xyz=cbalance.xyz * color.xyz;
	#endif

			float3 LumCoeff = float3( 0.2125, 0.7154, 0.0721 );				
			float3 AvgLumin = float3( 0.5, 0.5, 0.5 );
			float3 brightbloom = ( xcolorbloom - lerp( 0.18, 0.0, bchnd )); // darkens and thus limits what triggers a bloom, used in part to stop snow at night glowing blue
			brightbloom = max( brightbloom , 0);
			
			float3 superbright = xcolorbloom - 0.7; // crop it to only include superbright elemnts like sky and fire
			superbright = max( superbright , 0 ) ; // crop so dont go any lower than black
			superbright *= 0.6;
		
			// HD6 - Bloom - Brightntess, Contrast, Saturation adjustment 1,1,1 for no change // Remember this is bloom only being altered
			float brt = lerp( 1.0, 1.0, bchnd ) ; // doesnt work properly, should be done after contrast no?
				//
			float con = lerp( 1.1, 1.0, bchnd ); // 1.0, 0.8 // 1.1, 1.1				
			float sat = lerp( 0.8, 0.7, bchnd ); // 0.5, 0.7 // 0.7, 0.7 
				//
				
			float3 brtColor = brightbloom * brt;
			float3 cintensity = dot( brtColor, LumCoeff );
			float3 satColor = lerp( cintensity, brtColor, sat ); 
			float3 conColor = lerp( AvgLumin, satColor, con );
			conColor -= 0.3;
			brightbloom = conColor;
				
				
				
				// These 2 should compensate so when even when no bloom exists it still matches brightness of scene without ENB
			color.xyz += lerp( 0.12, 0.23, bchnd ); color.xyz *= lerp( 1.1, 1.4, bchnd );
			color.xyz += (( superbright * hbs ) * lerp( 1.0, 1.0, bchnd ));
			brightbloom -= ( superbright * 2 ); // removes superbright from brightbloom so I dont bloom the brightest area twice
			brightbloom = max( brightbloom , 0.0 );
			color.xyz += (( brightbloom * hbs ) * lerp( 1.0, 1.0, bchnd ));

				// Blend in some of the orignal bloom to bring back SOME of the hazy glow of the day, none at night
			color.xyz += (xcolorbloom.xyz * hbs) * lerp( 0.7, 0.6, bchnd );
			color.xyz *= lerp( 0.8, 0.7, bchnd ); // compensate for brightening caused by above bloom
				// End Blend
		
	//+++++++++++++++++++++++++++++
		
	#ifdef HD6_VIGNETTE		
		// yes this is my own crazy creation after seing how boring the usual linear circle vignettes typically are
		// no doubt I have done it in an overly convoluted way :-)
		float2 inTex = _v0;	
		float4 voriginal = r1;
		float4 vcolor = voriginal;
		vcolor.xyz=1;
		inTex -= 0.5; // Centers vignette
		inTex.y += 0.01; // Move it off center and up so it obscures sky less
		float vignette = 1.0 - dot( inTex, inTex );
		vcolor *= pow( vignette, vignettepow );
		
		
		// Round Vignette
		float4 rvigtex = vcolor;
		rvigtex.xyz = pow( vcolor, 1 );
		rvigtex.xyz = lerp(float3(0.5, 0.5, 0.5), rvigtex.xyz, 2.0); // Increase Contrast
		rvigtex.xyz = lerp(float3(1,1,1),rvigtex.xyz,rovigpwr); // Set strength of round vignette
		
		// Square Vignette (just top and bottom of screen)
		float4 vigtex = vcolor;
		vcolor.xyz = float3(1,1,1);
		float3 topv = min((inTex.y+0.5)*2,0.5) * 2; // Top vignette
		float3 botv = min(((0-inTex.y)+0.5)*2,0.5) * 2; // Bottom vignette
		
		topv= lerp(float3(1,1,1), topv, sqvigpwr.x);
		botv= lerp(float3(1,1,1), botv, sqvigpwr.y);
		vigtex.xyz = (topv)*(botv);			
		// Add round and square together
		vigtex.xyz*=rvigtex.xyz; 
		
		vigtex.xyz = lerp(vigtex.xyz,float3(1,1,1),(1-vstrengthatnight)*(1-vhnd)); // Alter Strength at night
		
			vigtex.xyz = min(vigtex.xyz,1);
			vigtex.xyz = max(vigtex.xyz,0);
				
		// Increase saturation where edges were darkenned
		float3 vtintensity = dot(color.xyz, float3(0.2125, 0.7154, 0.0721));
		color.xyz = lerp(vtintensity, color.xyz, ((((1-(vigtex.xyz*2))+2)-1)*vsatstrength)+1  );
		color.xyz *= (vigtex.xyz);
		
	#endif
	
	// HD6 - Warning, Code below appears to reduce 'color.xyz' to 8bit / LDR
		
		
	// HD6 - Eye Adaptation for extreme extreme over bright areas only, such as stupid stupid snow
	// 0.3, 0.9 - affects day time sunny day = bad
	float toobright = max(0,tex2D(_s2, _v0).xyz - 0.4); // 0.5
	color.xyz *= 1-(1 * toobright); // 1.3
	
	// <Lazy> HD6 - dopey arse code to alter enbpalette because im too lazy to open photoshop
		// when using your own palette remove this.. ill fix this.. next
		float palmix = 0.4; // 0.4
		color.xyz*=lerp( 1.0, 0.90, palmix); // 0.9
	// </Lazy>

	//+++++++++++++++++++++++++++++ HD6 version
	grayadaptation=max(grayadaptation, 0.0);
	grayadaptation=min(grayadaptation, 50.0);
	// HD6 - Screw eye adaptation it drives me mad, bright sky causes everything else to darken yuck
	// Human eye adaptation happens instantly or so slowly you dont notice it in reality
 	// it would make sense if the game was calibrated for true brightness values of indoors and outdoors being 10000x brighter
	// but it isnt, and thus all the pseudo tone mapping and linear colorspace adaption shenanigans just drives me mad and for little gain
	// So now simple adjust brightness based on time of day
	// with all the other effects turned off this should roughly equal the brightness when ENB is disabled
	color.xyz*=lerp( hndtweak.x, hndtweak.y, hnd );

	float3 xncol=normalize(color.xyz);
	float3 scl=color.xyz/xncol.xyz;
	scl=pow(scl, EIntensityContrastV2);
	xncol.xyz=pow(xncol.xyz, EColorSaturationV2);
	color.xyz=scl*xncol.xyz;
	color.xyz*=HCompensateSat; // compensate for darkening caused my EcolorSat above
	color.xyz=color.xyz/(color.xyz + EToneMappingCurveV2);
	//+++++++++++++++++++++++++++++
	_oC0.w=1.0;
	_oC0.xyz=color.xyz;
	
	float h11 = h1 == 1 ? 0 : 0.4;
	_oC0.xyz += _v0.x+_v0.y < 0.004 ? (h11) : (0);
	
	float h22 = h2 == 1 ? 0 : 0.4;
	_oC0.x += _v0.x+_v0.y < 0.004 ? (h22) : (0);
	
	return _oC0;
}
technique Shader_ORIGINALPOSTPROCESS
{
	pass p0
	{
		VertexShader = compile vs_3_0 VS_Quad();
		PixelShader = compile ps_3_0 PS_D6EC7DD1();

		ColorWriteEnable=ALPHA|RED|GREEN|BLUE;
		ZEnable=FALSE;
		ZWriteEnable=FALSE;
		CullMode=NONE;
		AlphaTestEnable=FALSE;
		AlphaBlendEnable=FALSE;
		SRGBWRITEENABLE=FALSE;
	}
}

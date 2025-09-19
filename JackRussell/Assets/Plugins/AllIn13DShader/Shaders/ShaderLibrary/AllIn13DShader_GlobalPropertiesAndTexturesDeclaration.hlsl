#ifndef ALLIN13DSHADER_GLOBALPROPERTIESANDTEXTURESDECLARATION
#define ALLIN13DSHADER_GLOBALPROPERTIESANDTEXTURESDECLARATION

DECLARE_TEX2D(_MainTex)

#ifdef _TEXTURE_BLENDING_ON
	#ifdef _TEXTUREBLENDINGMODE_RGB
		DECLARE_TEX2D(_BlendingTextureG)
		DECLARE_TEX2D(_BlendingTextureB)
	#else
		DECLARE_TEX2D(_BlendingTextureWhite)
	#endif

	#ifdef _TEXTUREBLENDINGSOURCE_TEXTURE
		DECLARE_TEX2D(_TexBlendingMask)
	#endif

	#ifdef _NORMAL_MAP_ON
		#ifdef _TEXTUREBLENDINGMODE_RGB
			DECLARE_TEX2D(_BlendingNormalMapG)
			DECLARE_TEX2D(_BlendingNormalMapB)
		#else
			DECLARE_TEX2D(_BlendingNormalMapWhite)
		#endif
	#endif
#endif

#ifdef REQUIRE_TANGENT_WS
	DECLARE_TEX2D(_NormalMap)
#endif

#ifdef _COLOR_RAMP_ON
	DECLARE_TEX2D(_ColorRampTex)
#endif

// ----- Global Properties
float global_MinDepth;
float global_DepthZoneLength;
float global_DepthGradientFallOff;

#ifdef REQUIRE_SCENE_DEPTH
	DECLARE_TEX2D(global_DepthGradient)
#endif

#ifdef _CUSTOM_SHADOW_COLOR_ON
	float4 global_shadowColor;
#endif

float4 allIn13DShader_globalTime;

#ifdef _LIGHTMODEL_FASTLIGHTING
	float3 global_lightDirection;
	float4 global_lightColor;
#endif

#ifdef _WIND_ON 
	DECLARE_TEX2D(global_windNoiseTex)
	float global_windForce;
	float2 global_noiseSpeed;
	float global_useWindDir;
	float3 global_windDir;
	float global_minWindValue;
	float global_maxWindValue;
	float global_windWorldSize;
#endif
//------------------------

#if defined(_SHADINGMODEL_PBR)
	#if defined(_METALLIC_MAP_ON)
		DECLARE_TEX2D(_MetallicMap)
	#endif
#endif

#ifdef SPECULAR_ON
	DECLARE_TEX2D(_SpecularMap)
#endif

#ifdef _LIGHTMODEL_TOONRAMP
	DECLARE_TEX2D(_ToonRamp)
#endif

#ifdef _AOMAP_ON
	DECLARE_TEX2D(_AOMap)
#endif

#ifdef _SUBSURFACE_SCATTERING_ON
	DECLARE_TEX2D(_SSSMap)
#endif

#ifdef _FADE_ON
	DECLARE_TEX2D(_FadeTex)
#endif

#if defined(_VERTEX_DISTORTION_ON) || defined(URP_PASS)
	DECLARE_TEX2D(_VertexDistortionNoiseTex)
#endif

#ifdef _MATCAP_ON
	DECLARE_TEX2D(_MatcapTex)
#endif

#ifdef _UV_DISTORTION_ON
	DECLARE_TEX2D(_DistortTex)
#endif

#ifdef _EMISSION_ON
	DECLARE_TEX2D(_EmissionMap)
#endif

#ifdef _TRIPLANAR_MAPPING_ON
	DECLARE_TEX2D(_TriplanarTopTex)
	DECLARE_TEX2D(_TriplanarTopNormalMap)
#endif

#endif //ALLIN13DSHADER_GLOBALPROPERTIESANDTEXTURESDECLARATION
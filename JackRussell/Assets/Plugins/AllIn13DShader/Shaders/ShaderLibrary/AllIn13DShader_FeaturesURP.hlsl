#ifndef ALLIN13DSHADER_FEATURESURP
#define ALLIN13DSHADER_FEATURESURP

//---------------------------------------------------------------------
// Features to enable disable. You will want to disable features you don't use to speed up compilation and build times
// You can also enable/disable these features in the Asset Window, in the URP Settings tab

// Core rendering features
#define ALLIN1_GPU_INSTANCING_SUPPORT           // Enables GPU Instancing for better performance
#define ALLIN1_DOTS_INSTANCING_SUPPORT          // Supports Unity ECS instancing
#define ALLIN1_FOG_SUPPORT                      // Unity fog system integration

// Lighting and shadows
#define ALLIN1_LIGHTMAPS_SUPPORT                // Enables baked lightmap support
#define ALLIN1_ADDITIONAL_LIGHTS_SUPPORT        // Support for additional real-time lights beyond main directional
#define ALLIN1_CAST_SHADOWS_SUPPORT             // Enables shadow casting from this material
#define ALLIN1_SHADOW_MASK_SUPPORT              // Mixed lighting shadowmask support

// Advanced rendering (Unity 6+)
#define ALLIN1_FORWARD_PLUS_SUPPORT_UNITY6      // Forward+ rendering path (Unity 6+)
//#define ALLIN1_REFLECTIONS_PROBES_SUPPORT_UNITY6 // Probe blending (Unity 6+ only, enable if using probe blending)

// Screen-space effects
#define ALLIN1_SSO_SUPPORT                      // Screen Space Ambient Occlusion support

//Light Cookies
#define ALLIN1_LIGHT_COOKIES_SUPPORT

//Decals
#define ALLIN1_DECALS_SUPPORT

// Specialized features
//#define ALLIN1_LIGHT_LAYERS_SUPPORT             // Unity light layer system (if using light layers)
//---------------------------------------------------------------------

#if defined(ALLIN1_FORWARD_PASS)
	#ifdef ALLIN1_GPU_INSTANCING_SUPPORT
		#pragma multi_compile_instancing
	#endif

	#ifdef ALLIN1_DOTS_INSTANCING_SUPPORT 
		#pragma multi_compile _ DOTS_INSTANCING_ON
	#endif

	#ifdef ALLIN1_FOG_SUPPORT
		#pragma multi_compile_fog
	#endif

	#ifdef ALLIN1_CAST_SHADOWS_SUPPORT
		#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
		#pragma multi_compile _ _SHADOWS_SOFT
	#endif

	#ifdef ALLIN1_SSO_SUPPORT
		#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
	#endif

	#ifdef ALLIN1_ADDITIONAL_LIGHTS_SUPPORT
		#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
		#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
	#endif

	#ifdef ALLIN1_REFLECTIONS_PROBES_SUPPORT_UNITY6
		#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
		#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
		#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
	#endif

	#ifdef ALLIN1_LIGHT_LAYERS_SUPPORT
		#pragma multi_compile _ _LIGHT_LAYERS
	#endif

	#ifdef ALLIN1_SHADOW_MASK_SUPPORT
		#pragma multi_compile _ SHADOWS_SHADOWMASK // v10+ only
	#endif

	#ifdef ALLIN1_FORWARD_PLUS_SUPPORT_UNITY6
		#pragma multi_compile _ _FORWARD_PLUS
	#endif

	#ifdef ALLIN1_LIGHTMAPS_SUPPORT
		#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING // v10+ only, renamed from "_MIXED_LIGHTING_SUBTRACTIVE"
		#pragma multi_compile _ LIGHTMAP_ON
		#pragma multi_compile _ DIRLIGHTMAP_COMBINED
	#endif

	#ifdef ALLIN1_LIGHT_COOKIES_SUPPORT
		#pragma multi_compile_fragment _ _LIGHT_COOKIES
	#endif

	#ifdef ALLIN1_DECALS_SUPPORT
		#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
	#endif

#elif defined(SHADOW_CASTER_PASS)
	#pragma multi_compile_fwdadd_fullshadows
	
	#ifdef ALLIN1_GPU_INSTANCING_SUPPORT
		#pragma multi_compile_instancing
	#endif

	#ifdef ALLIN1_DOTS_INSTANCING_SUPPORT
		#pragma multi_compile _ DOTS_INSTANCING_ON
	#endif

	#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

#elif defined(ALLIN1_DEPTH_ONLY_PASS)
	#ifdef ALLIN1_GPU_INSTANCING_SUPPORT
		#pragma multi_compile_instancing
	#endif

	#ifdef ALLIN1_DOTS_INSTANCING_SUPPORT
		#pragma multi_compile _ DOTS_INSTANCING_ON
	#endif

#elif defined(ALLIN1_DEPTH_NORMALS_PASS)
	#ifdef ALLIN1_GPU_INSTANCING_SUPPORT
		#pragma multi_compile_instancing
	#endif

	#ifdef ALLIN1_DOTS_INSTANCING_SUPPORT
		#pragma multi_compile _ DOTS_INSTANCING_ON
	#endif

#elif defined(ALLIN1_OUTLINE_PASS)
	#ifdef ALLIN1_GPU_INSTANCING_SUPPORT
		#pragma multi_compile_instancing
	#endif

	#ifdef ALLIN1_DOTS_INSTANCING_SUPPORT
		#pragma multi_compile _ DOTS_INSTANCING_ON
	#endif

	#ifdef ALLIN1_FOG_SUPPORT
		#pragma multi_compile_fog
	#endif
	
#endif



#endif //ALLIN13DSHADER_FEATURESURP
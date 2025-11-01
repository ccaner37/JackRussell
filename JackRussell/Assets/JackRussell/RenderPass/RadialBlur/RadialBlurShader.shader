Shader "Blit/RadialBlur"
{
    Properties
    {
        _Samples ("Samples", Float) = 24.0
        _Decay ("Decay", Float) = 0.97
        _Density ("Density", Float) = 0.5
        _Weight ("Weight", Float) = 0.1
        _CenterX ("Center X", Float) = 0.5
        _CenterY ("Center Y", Float) = 0.5
        _BlurStrength ("Blur Strength", Float) = 1.0
        _EffectIntensity ("Effect Intensity", Float) = 0.0
        _Brightness ("Brightness", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off Cull Off
        Pass
        {
            Name "RadialBlurPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
            float _Samples;
            float _Decay;
            float _Density;
            float _Weight;
            float _CenterX;
            float _CenterY;
            float _BlurStrength;
            float _EffectIntensity;
            float _Brightness;
            CBUFFER_END

            // 2x1 hash. Used to jitter the samples.
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(41, 289))) * 45758.5453);
            }


            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.
            float4 Frag(Varyings input) : SV_Target0
            {
                // this is needed so we account XR platform differences in how they handle texture arrays
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Screen coordinates.
                float2 uv = input.texcoord.xy;

                // Radial blur factors.
                float decay = _Decay;
                float density = _Density;
                float weight = _Weight;
                float samples = _Samples;

                // Center position
                float2 center = float2(_CenterX, _CenterY);

                // Offset texture position (uv - center), which centers the blur on the specified point
                float2 tuv = uv - center;

                // Dividing the direction vector by the sample number and a density factor
                float2 dTuv = tuv * density / samples;

                // Grabbing the initial texture sample.
                half4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);

                // Jittering to get rid of banding
                uv += dTuv * (hash(uv + frac(_Time.y)) * 2. - 1.);

                // Store original color for blending
                half4 originalCol = col;

                // The radial blur loop.
                for(float i = 0.; i < samples; i++)
                {
                    uv -= dTuv;
                    col += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel) * weight;
                    weight *= decay;
                }

                // Multiplying the final color with a spotlight centered on the focal point
                col *= (1. - dot(tuv, tuv) * 0.75);

                // Apply brightness control to the gamma correction
                float brightnessFactor = lerp(1.0, 0.5, 1.0 - _Brightness); // 1.0 = full brightness, 0.5 = more natural
                col = pow(smoothstep(0., 1., col), brightnessFactor);

                // Apply blur strength
                half4 blurredCol = col * _BlurStrength;
                return lerp(originalCol, blurredCol, _EffectIntensity);
            }

            ENDHLSL
        }
    }
}

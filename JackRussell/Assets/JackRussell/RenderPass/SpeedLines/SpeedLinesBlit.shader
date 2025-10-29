Shader "BlitWithMaterial"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 1.0
        _Speed ("Animation Speed", Float) = 0.07
        _LineCount ("Line Count", Float) = 24.0
        _InnerRadius ("Inner Radius", Float) = 0.0
        _OuterRadius ("Outer Radius", Float) = 0.8
        _LineWidth ("Line Width", Float) = 0.03
        _FadeStrength ("Fade Strength", Float) = 1.0
        _Color ("Speed Lines Color", Color) = (1,1,1,1)
    }

    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "BlitWithMaterialPass"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           CBUFFER_START(UnityPerMaterial)
           float _Intensity;
           float _InnerRadius;
           float _OuterRadius;
           half4 _Color;
           CBUFFER_END

           float rand(float2 p)
           {
               return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
           }

           float noise(float2 p)
           {
               float2 i = floor(p);
               float2 f = frac(p);
               float2 u = f * f * (3.0 - 2.0 * f);
               float a = rand(i + float2(0, 0));
               float b = rand(i + float2(1, 0));
               float c = rand(i + float2(0, 1));
               float d = rand(i + float2(1, 1));
               return (a + (b - a) * u.x + (c - a) * u.y + (a - b - c + d) * u.x * u.y) / 4.0;
           }

           float mirror(float t, float r)
           {
               t = frac(t + r);
               return 2.0 * abs(t - 0.5);
           }

           float radialNoise(float t, float d)
           {
               float2x2 m2 = float2x2(0.90, 0.44, -0.44, 0.90);
               const float SCALE = 45.0;
               d = pow(d, 0.01);
               float doff = -_Time.y * 0.07;
               float2 p = float2(mirror(t, 0.1), d + doff);
               float f1 = noise(p * SCALE);
               p = 2.1 * float2(mirror(t, 0.4), d + doff);
               float f2 = noise(p * SCALE);
               p = 3.7 * float2(mirror(t, 0.8), d + doff);
               float f3 = noise(p * SCALE);
               p = 5.8 * float2(mirror(t, 0.0), d + doff);
               float f4 = noise(p * SCALE);
               return pow((f1 + 0.5 * f2 + 0.25 * f3 + 0.125 * f4) * 3.0, 1.0);
           }

           float3 colorize(float x)
           {
               x = clamp(x, 0.0, 1.0);
               float3 c = lerp(float3(0, 0, 1.1), float3(0, 1, 1), x);
               c = lerp(c, float3(1, 1, 1), x * 4.0 - 3.0) * x;
               c = max(c, float3(0, 0, 0));
               c = lerp(c, float3(1, 0.25, 1), smoothstep(1.0, 0.2, x) * smoothstep(0.15, 0.9, x));
               return c;
           }

           // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.
           float4 Frag(Varyings input) : SV_Target0
           {
               // this is needed so we account XR platform differences in how they handle texture arrays
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // sample the texture using the SAMPLE_TEXTURE2D_X_LOD
               float2 uv = input.texcoord.xy;
               half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);

               // Compute speed lines UV
               float aspect = _ScreenParams.x / _ScreenParams.y;
               float2 speedUV = (uv * 2.0 - 1.0) * float2(aspect, 1.0) * 0.5;
               float d = length(speedUV / float2(aspect * 0.5, 0.5));
               float t = atan2(speedUV.y, speedUV.x) / 6.28318530718;
               float v = radialNoise(t, d);
               v = v * 2.5 - 1.4;
               v = lerp(0.0, v, 0.8 * smoothstep(_InnerRadius, _OuterRadius, d));
               float3 speedColor = colorize(v) * _Color.rgb * _Intensity;

               // Modify the sampled color by adding speed lines
               return half4(color.rgb + speedColor, color.a);
           }

           ENDHLSL
       }
   }
}
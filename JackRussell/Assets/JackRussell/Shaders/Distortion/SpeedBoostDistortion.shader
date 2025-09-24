Shader "JackRussell/SpeedBoostDistortion"
{
    Properties
    {
        [Header(Main Settings)]
        _Color ("Tint Color", Color) = (1,1,1,0.1)
        _Alpha ("Alpha", Range(0,1)) = 0.3
        
        [Header(Distortion Settings)]
        _DistortionStrength ("Distortion Strength", Range(0,0.1)) = 0.02
        _DistortionSpeed ("Distortion Speed", Range(0,5)) = 1.0
        _DistortionScale ("Distortion Scale", Range(0.1,10)) = 2.0
        _DistortionDirection ("Distortion Direction", Vector) = (1,0,0,0)
        
        [Header(Curved Edge Settings)]
        _EdgeSmoothness ("Edge Smoothness", Range(0,1)) = 0.3
        _CurveIntensity ("Curve Intensity", Range(0,2)) = 1.0
        _EdgeFalloff ("Edge Falloff", Range(0.1,2)) = 0.5
        
        [Header(Animation Settings)]
        _FlowSpeed ("Flow Speed", Range(0,10)) = 2.0
        _WaveFrequency ("Wave Frequency", Range(0,20)) = 5.0
        _WaveAmplitude ("Wave Amplitude", Range(0,0.05)) = 0.01
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        LOD 100
        
        Pass
        {
            Name "SpeedBoostDistortion"
            Tags { "LightMode"="UniversalForward" }
            
            // Enable transparency and double-sided rendering
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Properties
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Alpha;
                float _DistortionStrength;
                float _DistortionSpeed;
                float _DistortionScale;
                float4 _DistortionDirection;
                float _EdgeSmoothness;
                float _CurveIntensity;
                float _EdgeFalloff;
                float _FlowSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;
            CBUFFER_END
            
            // Noise function for distortion
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }
            
            float noise(float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2;
                const float K2 = 0.211324865; // (3-sqrt(3))/6;
                
                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float2 o = (a.x > a.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, hash22(i + 0.0)), dot(b, hash22(i + o)), dot(c, hash22(i + 1.0)));
                
                return dot(n, float3(70.0, 70.0, 70.0));
            }
            
            // Fractal noise for more complex distortion
            float fbm(float2 p)
            {
                float f = 0.0;
                float2x2 m = float2x2(1.6, 1.2, -1.2, 1.6);
                f += 0.5000 * noise(p); p = mul(m, p);
                f += 0.2500 * noise(p); p = mul(m, p);
                f += 0.1250 * noise(p); p = mul(m, p);
                f += 0.0625 * noise(p);
                return f;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.uv;
                float time = _Time.y;
                
                // Get screen position for sampling the background
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Create flowing distortion based on direction
                float2 flowDirection = normalize(_DistortionDirection.xy);
                float2 flowUV = uv + flowDirection * time * _FlowSpeed;
                
                // Generate distortion using fractal noise
                float2 distortionOffset = float2(
                    fbm(flowUV * _DistortionScale + time * _DistortionSpeed),
                    fbm(flowUV * _DistortionScale + time * _DistortionSpeed + 100.0)
                ) * _DistortionStrength;
                
                // Add wave-like motion for more dynamic effect
                float wave = sin(uv.x * _WaveFrequency + time * _FlowSpeed) * _WaveAmplitude;
                distortionOffset.y += wave;
                
                // Apply curved distortion - stronger at edges
                float2 centeredUV = uv - 0.5;
                float distanceFromCenter = length(centeredUV);
                float curveFactor = pow(distanceFromCenter * 2.0, _CurveIntensity);
                distortionOffset *= curveFactor;
                
                // Calculate edge smoothing
                float2 edgeDistance = min(uv, 1.0 - uv);
                float edgeFactor = min(edgeDistance.x, edgeDistance.y);
                edgeFactor = smoothstep(0.0, _EdgeSmoothness, edgeFactor);
                
                // Apply curved edge falloff
                float edgeAlpha = pow(edgeFactor, _EdgeFalloff);
                
                // Apply distortion to screen coordinates
                float2 distortedScreenUV = screenUV + distortionOffset * edgeAlpha;
                
                // Sample the background scene with distorted coordinates
                half4 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortedScreenUV);
                
                // Add some shimmer effect based on viewing angle
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnel = 1.0 - saturate(dot(input.normalWS, viewDir));
                
                // Combine scene color with tint and fresnel effect
                half4 finalColor = sceneColor;
                finalColor.rgb = lerp(sceneColor.rgb, sceneColor.rgb * _Color.rgb, _Color.a);
                finalColor.rgb += fresnel * 0.1 * _Color.rgb;
                finalColor.a = _Alpha * edgeAlpha;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}
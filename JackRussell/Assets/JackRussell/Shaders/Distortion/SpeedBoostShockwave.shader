Shader "Custom/SpeedBoostShockwave"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1.0
        _Amplitude ("Amplitude", Float) = 0.1
        _Frequency ("Frequency", Float) = 10.0
        _CenterPosition ("Center Position", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Speed;
                float _Amplitude;
                float _Frequency;
                float3 _CenterPosition;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center
                float3 center = _CenterPosition;
                float dist = distance(i.worldPos, center);

                // Wave propagation along Z (assuming mesh oriented along Z)
                float wavePos = i.worldPos.z - _Time.y * _Speed;
                float wave = sin(wavePos * _Frequency) * _Amplitude * exp(-dist * 0.1); // falloff

                // Distort screen UV
                float4 screenPos = ComputeScreenPos(i.vertex);
                float2 screenUV = screenPos.xy / screenPos.w;
                float2 offset = wave * normalize(screenUV - 0.5);
                float2 distortedUV = screenUV + offset;

                // Sample opaque texture
                half4 col = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortedUV);
                col.a = _Color.a * (1 - abs(wave)); // modulate alpha

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
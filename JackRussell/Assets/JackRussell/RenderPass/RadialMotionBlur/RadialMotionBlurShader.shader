Shader "Blit/RadialMotionBlur"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off Cull Off
        Pass
        {
            Name "RadialMotionBlurPass"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            
            // Shader properties
            float _BlurStrength;
            float2 _BlurCenter;
            int _SampleCount;
            
            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord.xy;
                
                // Calculate direction from blur center to current pixel
                float2 direction = uv - _BlurCenter;
                
                // Calculate blur amount based on distance from center
                float distance = length(direction);
                
                // Normalize direction
                float2 blurVector = normalize(direction) * _BlurStrength * distance;
                
                // Accumulate samples along the blur direction
                half4 color = half4(0, 0, 0, 0);
                
                // Sample multiple times along the blur vector
                for(int i = 0; i < _SampleCount; i++)
                {
                    float t = (float)i / (float)(_SampleCount - 1);
                    // Offset from center outward
                    float2 offset = blurVector * (t - 0.5);
                    float2 sampleUV = uv - offset;
                    
                    // Sample the texture
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, sampleUV, _BlitMipLevel);
                }
                
                // Average the samples
                color /= (float)_SampleCount;
                
                return color;
            }
            ENDHLSL
        }
    }
}
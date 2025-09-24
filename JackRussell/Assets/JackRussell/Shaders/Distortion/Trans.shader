Shader "Custom/TransparentPassThrough"
{
    Properties
    {
        [Header(Transparency)]
        _Alpha ("Alpha", Range(0, 1)) = 1.0
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
        
        [Header(Edge Control)]
        _EdgeSoftness ("Edge Softness", Range(0.1, 2.0)) = 0.5
        _FalloffPower ("Falloff Power", Range(0.5, 5.0)) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent"
        }
        
        Pass
        {
            Name "PassThroughPass"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _Alpha;
                float4 _Tint;
                float _EdgeSoftness;
                float _FalloffPower;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.uv = input.uv;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // === SCREEN POSITION CALCULATION ===
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // === SAMPLE SCENE COLOR (NO DISTORTION) ===
                //float3 sceneColor = SampleSceneColor(screenUV);

                screenUV.x += sin(screenUV.y*10.0)/10.0;
                screenUV.y += cos(screenUV.x*10.0)/10.0;

                // Sample the texture
                float3 color = SampleSceneColor(screenUV);


                // float dist = distance(input.uv, center);
                // float alpha = saturate(1.0 - dist * 2);
                // float3 centerWS = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz; // object origin in world space
                // float distWS = distance(input.positionWS, centerWS); // world-space distance

                // // _FadeRadius should be a material property (meters) that controls how far the fade extends
                // float normalized = saturate(distWS / 1); // 0 = center, 1 = at _FadeRadius

                // // keep alpha almost unchanged until 90% of the radius, then fade to 0 by the radius
                // float fadeStart = 0.90; // start fading when normalized >= 0.90 (90%)
                // float falloff = smoothstep(fadeStart, 1.0, normalized);

                // // shape the falloff curve (optional) — increases subtlety of early fade
                // float shaped = pow(falloff, 1.0); // use >1 for sharper fade, <1 for softer

                // float alpha = lerp(1.0, 0.0, shaped);     // 1 at center, 0 at/after _FadeRadius;



                return float4(color,  1);
              //  return float4(color, 2 * distUV);
                
                //return float4(color, 1.0 * pow(0.5, distance(input.uv, center)));
                
                // === EDGE SOFTNESS (OPTIONAL) ===
                // float3 normalWS = normalize(input.normalWS);
                // float3 viewDirWS = normalize(input.viewDirWS);
                
                // Create smooth edge falloff
                // float edgeFactor = abs(dot(normalWS, viewDirWS));
                // float edgeMask = 1.0 - pow(edgeFactor, _FalloffPower);
                // edgeMask = smoothstep(0.0, _EdgeSoftness, edgeMask);
                
                // === APPLY TINT AND ALPHA ===
                // float3 finalColor = sceneColor * _Tint.rgb;
                // float finalAlpha = _Alpha * _Tint.a * edgeMask;
                
                //return float4(sceneColor, 1);
            }
            ENDHLSL
        }
    }
    
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
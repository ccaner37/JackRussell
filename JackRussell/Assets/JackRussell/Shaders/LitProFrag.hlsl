// Smoke flow calculation method with glass pipe enhancements
half4 CalculateSmokeFlow(half4 baseColor, Varyings input)
{
    // === GLASS PIPE FRESNEL EFFECT ===
    half fresnel = 0.5; // Default fallback
    
    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    // Calculate world space view direction
    float3 worldPos = input.positionWS;
    float3 worldNormal = normalize(input.normalWS);
    float3 worldViewDir = normalize(GetWorldSpaceViewDir(worldPos));
    
    // Calculate Fresnel effect for glass-like appearance
    half NdotV = saturate(dot(worldNormal, worldViewDir));
    fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;
    #endif
    
    // === GLASS THICKNESS SIMULATION ===
    // Use radial distance to simulate glass tube thickness
    half2 centeredUV = input.uv * 2.0 - 1.0;
    half radialDistance = length(centeredUV);
    half glassEdge = smoothstep(1.0 - _GlassThickness, 1.0, radialDistance);
    
    // Calculate animated UV coordinates for smoke flow
    half time = _Time.y * _FlowSpeed;
    
    // Main flow UV - scrolling along flow direction
    float2 flowUV1 = input.uv * _NoiseScale + _FlowDirection.xy * time;
    
    // Secondary turbulence UV for more complex patterns
    float2 flowUV2 = input.uv * (_NoiseScale * 0.7) + 
                     _FlowDirection.xy * time * 0.4 + 
                     float2(sin(time * 0.6), cos(time * 0.8)) * _Turbulence;
    
    // Sample noise textures
    half noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV1).r;
    half noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV2).r;
    
    // Combine noises for complex flow pattern
    half combinedNoise = lerp(noise1, noise2 * 0.7, 0.5);
    combinedNoise = pow(saturate(combinedNoise), 1.5); // Enhance contrast
    
    // Create radial mask for pipe interior using UV distance from center
    half radialMask = 1.0 - saturate(radialDistance);
    radialMask = smoothstep(_EdgeFade, 1.0, radialMask);
    
    // Calculate final smoke density
    half smokeDensity = combinedNoise * radialMask * _SmokeIntensity;
    
    // Add animated wispy variations
    half wispyEffect = sin(input.uv.y * 8.0 + time * 3.0) * 0.15 + 0.85;
    smokeDensity *= wispyEffect;
    
    // Blend smoke with base color
    half smokeAlpha = smokeDensity * _SmokeColor.a;
    half4 finalColor = baseColor;
    
    // Additive blending for glowing smoke effect
    finalColor.rgb += _SmokeColor.rgb * smokeAlpha;
    
    // === ADD GLASS PIPE EFFECTS ===
    // Add Fresnel rim lighting for glass appearance
    half3 fresnelGlow = _FresnelColor.rgb * fresnel;
    finalColor.rgb += fresnelGlow * _GlassGlow;
    
    // Add glass edge definition
    finalColor.rgb += _FresnelColor.rgb * glassEdge * _GlassGlow * 0.3;
    
    // Increase alpha if smoke is present, also add glass edge alpha
    finalColor.a = max(baseColor.a, smokeAlpha);
    finalColor.a = max(finalColor.a, fresnel * glassEdge * 0.2); // Subtle glass edge visibility
    
    return finalColor;
}
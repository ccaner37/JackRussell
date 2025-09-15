// Sample noise from texture
float SampleNoise(float2 uv)
{
    return SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uv).r;
}

float FractalNoise(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    
    for (int i = 0; i < 4; i++)
    {
        value += amplitude * SampleNoise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    
    return value;
}

// Glitch distortion function using noise texture
float2 GlitchDistortion(float2 uv, float time, float scale)
{
    float2 distortedUV = uv;

    // Chaotic horizontal glitches using noise texture
    float glitchLine = floor(uv.y * 50.0 * scale + sin(time * 10.0) * 5.0);
    float glitchNoise = SampleNoise(float2(glitchLine * 0.01, floor(time * 8.0) * 0.01));

    if (glitchNoise > 0.95)
    {
        distortedUV.x += sin(time * 20.0 + glitchLine) * _ChaoticDistortion;
    }

    // Vertical data corruption using noise texture
    float verticalGlitch = step(0.98, SampleNoise(float2(uv.x * 1.0 * scale, time * 0.3)));
    distortedUV.x += verticalGlitch * sin(time * 15.0) * _ChaoticDistortion * 0.5;

    return distortedUV;
}

// Main glitch rail effect calculation
half4 CalculateGlitchRailEffect(float4 positionCS, float2 baseUV, float3 worldPos)
{
    float time = _Time.y * _GlitchSpeed;

    // No distance scaling needed - cylindrical mapping handles consistency automatically

    // Use world space cylindrical coordinates for flawless texture mapping on curved rails
    // This completely eliminates UV stretching issues

    // Use configurable rail direction
    float3 railDirection = normalize(_RailDirection);

    // Project world position onto rail direction to get length along rail
    float railLength = dot(worldPos, railDirection);

    // Get perpendicular vector to rail for circumferential coordinate
    float3 perpendicular = normalize(cross(railDirection, float3(0, 1, 0)));
    if (length(perpendicular) < 0.1) {
        perpendicular = normalize(cross(railDirection, float3(1, 0, 0)));
    }

    // Calculate angle around the rail (cylindrical coordinate)
    float3 radialVector = worldPos - dot(worldPos, railDirection) * railDirection;
    float angle = atan2(dot(radialVector, perpendicular), dot(radialVector, cross(railDirection, perpendicular)));

    // Create cylindrical UV coordinates
    float2 cylindricalUV = float2(
        angle / (2 * 3.14159) + 0.5,  // Circumferential coordinate (0-1)
        railLength * _Scale            // Length coordinate (configurable density)
    );

    // Use cylindrical UV for consistent texture mapping
    float2 correctedUV = cylindricalUV;

    // Apply unified scaling
    float2 glitchUV = GlitchDistortion(correctedUV, time, _Scale);
    float2 noiseCoord = glitchUV * _Scale;

    // Generate base chaotic noise using texture
    float baseNoise = FractalNoise(noiseCoord + time * 0.3);
    float fastNoise = FractalNoise(noiseCoord * 3.0 * _Scale + time * 1.5);

    // Create data stream lines
    float2 dataCoord = float2(glitchUV.x * 100.0 * _Scale, glitchUV.y * 20.0 + time * 2.0);
    float dataLines = abs(frac(dataCoord.y) - 0.5) < _DataLineThickness;

    // Robotic grid pattern
    float2 gridCoord = glitchUV * 40.0 * _Scale;
    float gridPattern = step(0.95, max(abs(frac(gridCoord.x) - 0.5), abs(frac(gridCoord.y) - 0.5)));

    // Chaotic pulse effect using noise texture
    float pulse = sin(time * _GlitchFrequency + baseNoise * 10.0) * 0.5 + 0.5;
    pulse = pow(pulse, 3.0);

    // Digital corruption blocks using noise texture
    float2 blockCoord = floor(glitchUV * 30.0 * _Scale);
    float blockNoise = SampleNoise(blockCoord * 0.01 + floor(time * 4.0) * 0.01);
    float corruptionBlocks = step(0.9, blockNoise);

    // Glitch scan lines
    float scanLines = sin(glitchUV.y * 300.0 * _Scale + time * 10.0) * 0.5 + 0.5;
    scanLines = step(0.8, scanLines);
    
    // Combine all effects
    float glitchMask = saturate(
        baseNoise * 2.0 +
        dataLines * 3.0 +
        gridPattern * 2.0 +
        pulse * 1.5 +
        corruptionBlocks * 4.0 +
        scanLines * 0.5 +
        fastNoise * 1.0
    );
    
    // Color mixing with chaotic behavior using noise texture
    float colorMix = sin(time * 3.0 + baseNoise * 6.28) * 0.5 + 0.5;
    half3 finalGlitchColor = lerp(_GlitchColor.rgb, _SecondaryGlitchColor.rgb, colorMix);
    
    // Add some random color corruption using noise texture
    float colorCorruption = step(0.95, fastNoise);
    finalGlitchColor = lerp(finalGlitchColor, half3(1, 0, 0), colorCorruption * 0.3);
    
    // Apply intensity and create final output
    glitchMask *= _GlitchIntensity;
    
    return half4(finalGlitchColor * glitchMask, glitchMask);
}
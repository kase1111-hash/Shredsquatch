// Shredsquatch Common Shader Functions
// Include this file in custom shaders for shared utilities

#ifndef SHREDSQUATCH_COMMON_INCLUDED
#define SHREDSQUATCH_COMMON_INCLUDED

// ============================================================================
// HASH FUNCTIONS
// ============================================================================

// Simple 1D hash
float Hash1D(float n)
{
    return frac(sin(n) * 43758.5453);
}

// 2D hash
float Hash2D(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// 3D hash
float Hash3D(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// ============================================================================
// NOISE FUNCTIONS
// ============================================================================

// Value noise 2D
float ValueNoise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = Hash2D(i);
    float b = Hash2D(i + float2(1, 0));
    float c = Hash2D(i + float2(0, 1));
    float d = Hash2D(i + float2(1, 1));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Value noise 3D
float ValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(lerp(Hash3D(i + float3(0,0,0)), Hash3D(i + float3(1,0,0)), f.x),
             lerp(Hash3D(i + float3(0,1,0)), Hash3D(i + float3(1,1,0)), f.x), f.y),
        lerp(lerp(Hash3D(i + float3(0,0,1)), Hash3D(i + float3(1,0,1)), f.x),
             lerp(Hash3D(i + float3(0,1,1)), Hash3D(i + float3(1,1,1)), f.x), f.y),
        f.z);
}

// Fractional Brownian Motion (FBM) 2D
float FBM2D(float2 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * ValueNoise2D(p);
        p *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// ============================================================================
// COLOR UTILITIES
// ============================================================================

// HSV to RGB conversion
half3 HSVtoRGB(half3 hsv)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

// RGB to HSV conversion
half3 RGBtoHSV(half3 rgb)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 p = lerp(half4(rgb.bg, K.wz), half4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    half4 q = lerp(half4(p.xyw, rgb.r), half4(rgb.r, p.yzx), step(p.x, rgb.r));

    half d = q.x - min(q.w, q.y);
    half e = 1.0e-10;
    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// Adjust saturation
half3 AdjustSaturation(half3 color, half saturation)
{
    half3 hsv = RGBtoHSV(color);
    hsv.y *= saturation;
    return HSVtoRGB(hsv);
}

// ============================================================================
// EFFECT UTILITIES
// ============================================================================

// Fresnel effect
float FresnelEffect(float3 viewDir, float3 normal, float power)
{
    return pow(1.0 - saturate(dot(viewDir, normal)), power);
}

// Pulse function (0-1 oscillation)
float Pulse(float time, float speed)
{
    return sin(time * speed) * 0.5 + 0.5;
}

// Sparkle effect
float Sparkle(float3 worldPos, float scale, float threshold)
{
    float3 sparklePos = worldPos * scale;
    float sparkle = Hash3D(floor(sparklePos));
    return step(threshold, sparkle);
}

// ============================================================================
// UV UTILITIES
// ============================================================================

// Rotate UV around center
float2 RotateUV(float2 uv, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    float2x2 rotMatrix = float2x2(c, -s, s, c);
    return mul(rotMatrix, uv - 0.5) + 0.5;
}

// Scroll UV
float2 ScrollUV(float2 uv, float2 speed, float time)
{
    return uv + speed * time;
}

// ============================================================================
// WEATHER EFFECTS
// ============================================================================

// Snow accumulation mask (based on surface normal)
float SnowAccumulation(float3 worldNormal, float threshold)
{
    return saturate((worldNormal.y - threshold) / (1.0 - threshold));
}

// Frost overlay strength
float FrostStrength(float3 worldNormal, float3 viewDir, float amount)
{
    float rim = FresnelEffect(viewDir, worldNormal, 2.0);
    return rim * amount;
}

#endif // SHREDSQUATCH_COMMON_INCLUDED

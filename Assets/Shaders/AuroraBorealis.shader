Shader "Shredsquatch/Aurora Borealis"
{
    Properties
    {
        [Header(Aurora Colors)]
        [HDR] _Color1("Primary Color (Green)", Color) = (0.2, 2, 0.5, 1)
        [HDR] _Color2("Secondary Color (Teal)", Color) = (0.1, 1.5, 1.5, 1)
        [HDR] _Color3("Accent Color (Purple)", Color) = (1, 0.3, 2, 1)
        _ColorBlend("Color Blend Speed", Range(0, 2)) = 0.5

        [Header(Wave Animation)]
        _WaveSpeed("Wave Speed", Range(0, 2)) = 0.3
        _WaveFrequency("Wave Frequency", Range(1, 10)) = 3
        _WaveAmplitude("Wave Amplitude", Range(0, 1)) = 0.3

        [Header(Curtain Effect)]
        _CurtainLayers("Curtain Layers", Range(1, 5)) = 3
        _CurtainSpeed("Curtain Speed", Range(0, 1)) = 0.1
        _CurtainScale("Curtain Scale", Range(1, 10)) = 4

        [Header(Shimmer)]
        _ShimmerSpeed("Shimmer Speed", Range(0, 10)) = 2
        _ShimmerScale("Shimmer Scale", Range(1, 50)) = 15
        _ShimmerIntensity("Shimmer Intensity", Range(0, 1)) = 0.3

        [Header(Fade)]
        _VerticalFade("Vertical Fade", Range(0.1, 2)) = 0.8
        _HorizontalFade("Horizontal Fade", Range(0, 1)) = 0.3
        _OverallAlpha("Overall Alpha", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Aurora"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One // Additive for sky glow
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half _ColorBlend;
                half _WaveSpeed;
                half _WaveFrequency;
                half _WaveAmplitude;
                half _CurtainLayers;
                half _CurtainSpeed;
                half _CurtainScale;
                half _ShimmerSpeed;
                half _ShimmerScale;
                half _ShimmerIntensity;
                half _VerticalFade;
                half _HorizontalFade;
                half _OverallAlpha;
            CBUFFER_END

            // Smooth noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            // Aurora curtain function
            float auroraCurtain(float2 uv, float time, float layer)
            {
                float2 p = uv;

                // Horizontal wave
                float wave = sin(p.x * _WaveFrequency + time * _WaveSpeed + layer * 1.5) * _WaveAmplitude;
                p.y += wave;

                // Vertical curtain pattern
                float curtain = fbm(p * _CurtainScale + float2(time * _CurtainSpeed * (1.0 + layer * 0.3), 0), 4);

                // Shape the curtain - narrow at top, wider at bottom
                float verticalShape = 1.0 - pow(abs(uv.y - 0.5) * 2.0, _VerticalFade);
                verticalShape = saturate(verticalShape);

                // Horizontal fade at edges
                float horizontalShape = 1.0 - pow(abs(uv.x - 0.5) * 2.0, 1.0 / (1.0 - _HorizontalFade + 0.01));
                horizontalShape = saturate(horizontalShape);

                curtain *= verticalShape * horizontalShape;

                return curtain;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Generate multiple aurora layers
                float aurora = 0.0;
                float3 auroraColor = float3(0, 0, 0);

                for (int i = 0; i < (int)_CurtainLayers; i++)
                {
                    float layer = (float)i / _CurtainLayers;
                    float layerOffset = layer * 0.3;

                    // Each layer has slightly different timing
                    float layerTime = time * (1.0 + layer * 0.2);

                    float curtain = auroraCurtain(uv + float2(layerOffset, 0), layerTime, layer);

                    // Color varies by layer and position
                    float colorPhase = uv.x * 2.0 + layer + time * _ColorBlend;
                    float3 layerColor;

                    float colorMix = frac(colorPhase);
                    if (colorMix < 0.33)
                        layerColor = lerp(_Color1.rgb, _Color2.rgb, colorMix * 3.0);
                    else if (colorMix < 0.66)
                        layerColor = lerp(_Color2.rgb, _Color3.rgb, (colorMix - 0.33) * 3.0);
                    else
                        layerColor = lerp(_Color3.rgb, _Color1.rgb, (colorMix - 0.66) * 3.0);

                    // Layer intensity decreases with depth
                    float layerIntensity = 1.0 - layer * 0.3;

                    aurora += curtain * layerIntensity;
                    auroraColor += layerColor * curtain * layerIntensity;
                }

                // Normalize color
                if (aurora > 0.01)
                    auroraColor /= aurora;

                // Add shimmer
                float shimmer = noise(uv * _ShimmerScale + time * _ShimmerSpeed);
                shimmer = pow(shimmer, 3.0) * _ShimmerIntensity;
                auroraColor += shimmer * _Color1.rgb;

                // Final alpha
                float alpha = saturate(aurora) * _OverallAlpha;

                // Fade based on view (more visible looking up)
                float viewFade = saturate(uv.y * 1.5);
                alpha *= viewFade;

                return half4(auroraColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

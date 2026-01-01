Shader "Shredsquatch/Trail Fire"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (3, 1, 0.2, 1)

        [Header(Fire Animation)]
        _ScrollSpeed("Scroll Speed", Range(0, 5)) = 2
        _DistortionStrength("Distortion Strength", Range(0, 1)) = 0.3
        _NoiseScale("Noise Scale", Range(1, 20)) = 8

        [Header(Fire Shape)]
        _FireIntensity("Fire Intensity", Range(0, 3)) = 1.5
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.5
        _FlickerSpeed("Flicker Speed", Range(0, 10)) = 5
        _FlickerAmount("Flicker Amount", Range(0, 1)) = 0.2

        [Header(Color Gradient)]
        [HDR] _CoreColor("Core Color", Color) = (5, 3, 0.5, 1)
        [HDR] _MidColor("Mid Color", Color) = (4, 1, 0.1, 1)
        [HDR] _EdgeColor("Edge Color", Color) = (2, 0.2, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _ScrollSpeed;
                half _DistortionStrength;
                half _NoiseScale;
                half _FireIntensity;
                half _EdgeSoftness;
                half _FlickerSpeed;
                half _FlickerAmount;
                half4 _CoreColor;
                half4 _MidColor;
                half4 _EdgeColor;
            CBUFFER_END

            // Simplex-like noise
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

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Scroll UV upward (fire rises)
                float2 scrollUV = uv + float2(0, -time * _ScrollSpeed);

                // Distort UVs with noise
                float2 noiseUV = uv * _NoiseScale + time * 0.5;
                float distortion = fbm(noiseUV) * _DistortionStrength;
                scrollUV.x += distortion;

                // Calculate fire shape
                float fireNoise = fbm(scrollUV * _NoiseScale);

                // Edge mask - fire tapers at edges
                float edgeMask = 1.0 - abs(uv.x - 0.5) * 2.0;
                edgeMask = pow(edgeMask, 0.5);

                // Vertical gradient - fire fades at top
                float verticalFade = 1.0 - uv.y;
                verticalFade = pow(verticalFade, 1.5);

                // Combine into fire shape
                float fire = fireNoise * edgeMask * verticalFade * _FireIntensity;

                // Flicker effect
                float flicker = 1.0 + sin(time * _FlickerSpeed) * _FlickerAmount;
                flicker *= 1.0 + sin(time * _FlickerSpeed * 1.7 + 2.0) * _FlickerAmount * 0.5;
                fire *= flicker;

                // Gradient based on intensity
                half3 color;
                if (fire > 0.7)
                    color = lerp(_MidColor.rgb, _CoreColor.rgb, (fire - 0.7) / 0.3);
                else if (fire > 0.3)
                    color = lerp(_EdgeColor.rgb, _MidColor.rgb, (fire - 0.3) / 0.4);
                else
                    color = _EdgeColor.rgb * (fire / 0.3);

                // Alpha based on fire intensity
                float alpha = smoothstep(0, _EdgeSoftness, fire);
                alpha *= input.color.a;

                // Apply vertex color tint
                color *= input.color.rgb;
                color *= _Color.rgb;

                // Apply fog
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}

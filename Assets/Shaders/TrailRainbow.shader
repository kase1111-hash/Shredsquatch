Shader "Shredsquatch/Trail Rainbow"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}

        [Header(Rainbow Animation)]
        _ScrollSpeed("Color Scroll Speed", Range(0, 5)) = 1
        _ColorCycles("Color Cycles", Range(0.5, 5)) = 2
        _Saturation("Saturation", Range(0, 2)) = 1.2
        _Brightness("Brightness", Range(0, 3)) = 1.5

        [Header(Trail Shape)]
        _TrailWidth("Trail Width", Range(0, 2)) = 1
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.3
        _Glow("Glow Intensity", Range(0, 2)) = 0.5

        [Header(Shimmer Effect)]
        _ShimmerSpeed("Shimmer Speed", Range(0, 10)) = 3
        _ShimmerScale("Shimmer Scale", Range(1, 50)) = 20
        _ShimmerStrength("Shimmer Strength", Range(0, 1)) = 0.3
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

            Blend SrcAlpha One // Additive for glow
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
                float fogFactor : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _ScrollSpeed;
                half _ColorCycles;
                half _Saturation;
                half _Brightness;
                half _TrailWidth;
                half _EdgeSoftness;
                half _Glow;
                half _ShimmerSpeed;
                half _ShimmerScale;
                half _ShimmerStrength;
            CBUFFER_END

            // HSV to RGB conversion
            half3 HSVtoRGB(half3 hsv)
            {
                half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                half3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
                return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Rainbow hue based on position and time
                float hue = uv.y * _ColorCycles + time * _ScrollSpeed;
                hue = frac(hue);

                // Convert to RGB
                half3 rainbowColor = HSVtoRGB(half3(hue, _Saturation, 1.0));

                // Shimmer effect
                float shimmer = hash(floor(uv * _ShimmerScale) + floor(time * _ShimmerSpeed));
                shimmer = shimmer * _ShimmerStrength;
                rainbowColor += shimmer;

                // Edge softness - fade at horizontal edges
                float edgeDist = abs(uv.x - 0.5) * 2.0;
                float edgeFade = 1.0 - smoothstep(_TrailWidth - _EdgeSoftness, _TrailWidth, edgeDist);

                // Vertical fade at end of trail
                float verticalFade = 1.0 - uv.y;

                // Combine
                float alpha = edgeFade * verticalFade;
                alpha *= input.color.a;

                // Apply brightness and glow
                rainbowColor *= _Brightness;
                rainbowColor += rainbowColor * _Glow * alpha;

                // Apply vertex color modulation
                rainbowColor *= input.color.rgb;

                // Apply fog
                rainbowColor = MixFog(rainbowColor, input.fogFactor);

                return half4(rainbowColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}

Shader "Shredsquatch/Coin Glow"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 0.85, 0.1, 1)

        [Header(Metal Properties)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.9
        _Metallic("Metallic", Range(0, 1)) = 1

        [Header(Glow Effect)]
        [HDR] _GlowColor("Glow Color", Color) = (2, 1.5, 0.3, 1)
        _GlowIntensity("Glow Intensity", Range(0, 3)) = 1
        _PulseSpeed("Pulse Speed", Range(0, 5)) = 2
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.3

        [Header(Fresnel Rim)]
        [HDR] _RimColor("Rim Color", Color) = (3, 2, 0.5, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 2
        _RimStrength("Rim Strength", Range(0, 1)) = 0.6

        [Header(Sparkle)]
        _SparkleScale("Sparkle Scale", Range(1, 50)) = 20
        _SparkleSpeed("Sparkle Speed", Range(0, 10)) = 3
        _SparkleIntensity("Sparkle Intensity", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                half4 _GlowColor;
                half _GlowIntensity;
                half _PulseSpeed;
                half _PulseAmount;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _SparkleScale;
                half _SparkleSpeed;
                half _SparkleIntensity;
            CBUFFER_END

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // PBR-style metallic workflow
                half NdotL = saturate(dot(normalWS, mainLight.direction));

                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, 128 * _Smoothness) * _Smoothness * _Metallic;

                // Diffuse (reduced for metallic surface)
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL * (1 - _Metallic * 0.8);

                // Metallic reflection tint
                half3 specular = lerp(half3(1,1,1), baseColor.rgb, _Metallic) * spec * mainLight.color;

                // Fresnel rim glow
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower) * _RimStrength;
                half3 rimContrib = rim * _RimColor.rgb;

                // Pulsing glow
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, pulse, _PulseAmount);
                half3 glow = _GlowColor.rgb * _GlowIntensity * pulse;

                // Sparkles - view dependent
                float3 sparklePos = input.positionWS * _SparkleScale;
                sparklePos += _Time.y * _SparkleSpeed * float3(0.3, 0.1, 0.2);
                float sparkle = hash(floor(sparklePos));
                sparkle = step(0.97, sparkle) * sparkle;

                float VdotN = saturate(dot(viewDirWS, normalWS));
                sparkle *= VdotN; // Only on surfaces facing camera
                half3 sparkleContrib = sparkle * _SparkleIntensity * half3(1, 0.9, 0.5);

                // Ambient with metallic color
                half3 ambient = SampleSH(normalWS) * baseColor.rgb * (1 - _Metallic * 0.5);

                // Combine
                half3 finalColor = ambient + diffuse + specular + rimContrib + glow * 0.3 + sparkleContrib;

                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}

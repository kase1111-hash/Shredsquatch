Shader "Shredsquatch/Snow Sparkle"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.95, 0.97, 1, 1)

        [Header(Snow Properties)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.3
        _Metallic("Metallic", Range(0, 1)) = 0

        [Header(Sparkle Effect)]
        _SparkleScale("Sparkle Scale", Range(1, 100)) = 50
        _SparkleIntensity("Sparkle Intensity", Range(0, 2)) = 1
        _SparkleSpeed("Sparkle Speed", Range(0, 5)) = 1
        _SparkleThreshold("Sparkle Threshold", Range(0.9, 1)) = 0.97
        _SparkleColor("Sparkle Color", Color) = (1, 1, 1, 1)

        [Header(Subsurface Scattering)]
        _SubsurfaceColor("Subsurface Color", Color) = (0.8, 0.9, 1, 1)
        _SubsurfaceStrength("Subsurface Strength", Range(0, 1)) = 0.3

        [Header(Fresnel Rim)]
        _RimColor("Rim Color", Color) = (0.9, 0.95, 1, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimStrength("Rim Strength", Range(0, 1)) = 0.5
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
                half _SparkleScale;
                half _SparkleIntensity;
                half _SparkleSpeed;
                half _SparkleThreshold;
                half4 _SparkleColor;
                half4 _SubsurfaceColor;
                half _SubsurfaceStrength;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
            CBUFFER_END

            // Hash function for sparkle noise
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // 3D noise for sparkles
            float sparkleNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                    f.z);
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
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Basic lighting
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Sparkle effect - view and light dependent
                float3 sparklePos = input.positionWS * _SparkleScale;
                sparklePos += _Time.y * _SparkleSpeed * float3(0.1, 0.05, 0.15);

                float sparkle = sparkleNoise(sparklePos);

                // Make sparkles dependent on view and light angle
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float viewFactor = pow(NdotH, 32);

                sparkle = sparkle * viewFactor;
                sparkle = smoothstep(_SparkleThreshold, 1.0, sparkle);
                half3 sparkleContrib = sparkle * _SparkleColor.rgb * _SparkleIntensity * mainLight.color;

                // Subsurface scattering approximation
                float3 lightBack = -mainLight.direction;
                float sss = saturate(dot(viewDirWS, lightBack));
                sss = pow(sss, 3) * _SubsurfaceStrength;
                half3 subsurface = sss * _SubsurfaceColor.rgb * mainLight.color;

                // Fresnel rim lighting
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower) * _RimStrength;
                half3 rimContrib = rim * _RimColor.rgb;

                // Specular
                float spec = pow(NdotH, 64 * _Smoothness) * _Smoothness;
                half3 specular = spec * mainLight.color;

                // Ambient
                half3 ambient = SampleSH(normalWS) * baseColor.rgb;

                // Combine all
                half3 finalColor = ambient + diffuse + specular + sparkleContrib + subsurface + rimContrib;

                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Shadow caster pass
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

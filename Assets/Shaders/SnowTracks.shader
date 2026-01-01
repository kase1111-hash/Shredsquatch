Shader "Shredsquatch/Snow Tracks"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Snow Color", Color) = (0.95, 0.97, 1, 1)

        [Header(Track Properties)]
        _TrackColor("Track Color", Color) = (0.85, 0.88, 0.95, 1)
        _TrackDepth("Track Depth", Range(0, 1)) = 0.3
        _TrackSmoothness("Track Smoothness", Range(0, 1)) = 0.5

        [Header(Snow Surface)]
        _Smoothness("Snow Smoothness", Range(0, 1)) = 0.3
        _SparkleScale("Sparkle Scale", Range(1, 100)) = 50
        _SparkleIntensity("Sparkle Intensity", Range(0, 2)) = 0.8

        [Header(Displacement - Requires Tessellation)]
        _DisplacementMap("Displacement Map (R = depth)", 2D) = "black" {}
        _DisplacementStrength("Displacement Strength", Range(0, 1)) = 0.2
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
            TEXTURE2D(_DisplacementMap);
            SAMPLER(sampler_DisplacementMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DisplacementMap_ST;
                half4 _BaseColor;
                half4 _TrackColor;
                half _TrackDepth;
                half _TrackSmoothness;
                half _Smoothness;
                half _SparkleScale;
                half _SparkleIntensity;
                half _DisplacementStrength;
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

                // Sample displacement for vertex offset
                float2 dispUV = TRANSFORM_TEX(input.uv, _DisplacementMap);
                float displacement = SAMPLE_TEXTURE2D_LOD(_DisplacementMap, sampler_DisplacementMap, dispUV, 0).r;

                // Offset vertex along normal
                float3 posOS = input.positionOS.xyz;
                posOS -= input.normalOS * displacement * _DisplacementStrength;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
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
                // Sample displacement/track map
                float trackMask = SAMPLE_TEXTURE2D(_DisplacementMap, sampler_DisplacementMap, input.uv).r;

                // Blend between snow and track color
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 snowColor = baseColor.rgb * _BaseColor.rgb;
                half3 trackColor = baseColor.rgb * _TrackColor.rgb * (1.0 - trackMask * _TrackDepth);

                half3 surfaceColor = lerp(snowColor, trackColor, trackMask);

                // Blend smoothness
                half smoothness = lerp(_Smoothness, _TrackSmoothness, trackMask);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Diffuse
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = surfaceColor * mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, 64 * smoothness) * smoothness;
                half3 specular = spec * mainLight.color * mainLight.shadowAttenuation;

                // Sparkles (reduced in tracks)
                float3 sparklePos = input.positionWS * _SparkleScale + _Time.y * 0.5;
                float sparkle = hash(floor(sparklePos));
                sparkle = step(0.97, sparkle) * (1.0 - trackMask) * _SparkleIntensity;
                half3 sparkleContrib = sparkle * mainLight.color;

                // Ambient
                half3 ambient = SampleSH(normalWS) * surfaceColor;

                // Combine
                half3 finalColor = ambient + diffuse + specular + sparkleContrib;

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

Shader "Shredsquatch/Sasquatch Fur"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.3, 0.2, 0.1, 1)

        [Header(Fur Properties)]
        _FurLength("Fur Length", Range(0, 0.5)) = 0.1
        _FurDensity("Fur Density", Range(1, 100)) = 30
        _FurShading("Fur Shading", Range(0, 1)) = 0.25
        _FurThinness("Fur Thinness", Range(0.01, 2)) = 1

        [Header(Lighting)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.1
        _Metallic("Metallic", Range(0, 1)) = 0

        [Header(Subsurface Scattering)]
        _SubsurfaceColor("Subsurface Color", Color) = (0.5, 0.3, 0.2, 1)
        _SubsurfaceRadius("Subsurface Radius", Range(0, 2)) = 0.5
        _SubsurfaceStrength("Subsurface Strength", Range(0, 1)) = 0.4

        [Header(Eye Glow - Controlled by Script)]
        _EyeGlowColor("Eye Glow Color", Color) = (1, 0.2, 0.1, 1)
        _EyeGlowIntensity("Eye Glow Intensity", Range(0, 5)) = 0

        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (0.4, 0.3, 0.25, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 2
        _RimStrength("Rim Strength", Range(0, 1)) = 0.4

        [Header(Frost Effect - For Blizzard)]
        _FrostAmount("Frost Amount", Range(0, 1)) = 0
        _FrostColor("Frost Color", Color) = (0.9, 0.95, 1, 1)
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
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _FurLength;
                half _FurDensity;
                half _FurShading;
                half _FurThinness;
                half _Smoothness;
                half _Metallic;
                half4 _SubsurfaceColor;
                half _SubsurfaceRadius;
                half _SubsurfaceStrength;
                half4 _EyeGlowColor;
                half _EyeGlowIntensity;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _FrostAmount;
                half4 _FrostColor;
            CBUFFER_END

            // Simple hash for fur pattern
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Fur strand function
            float furStrand(float2 uv, float density)
            {
                float2 cell = floor(uv * density);
                float2 local = frac(uv * density);

                float rand = hash21(cell);
                float2 center = float2(0.5, 0.5) + (rand - 0.5) * 0.3;

                float dist = length(local - center);
                float strand = 1.0 - smoothstep(0.0, 0.1 * _FurThinness, dist);

                return strand * step(0.3, rand);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
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

                // Calculate fur variation
                float furPattern = furStrand(input.uv, _FurDensity);

                // Vary color based on fur pattern for depth
                half3 furColor = lerp(baseColor.rgb * (1.0 - _FurShading), baseColor.rgb, furPattern);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Kajiya-Kay style anisotropic highlights for fur
                float3 tangent = normalize(input.tangentWS);
                float TdotL = dot(tangent, mainLight.direction);
                float TdotV = dot(tangent, viewDirWS);
                float sinTL = sqrt(1.0 - TdotL * TdotL);
                float sinTV = sqrt(1.0 - TdotV * TdotV);

                // Primary specular (shifted towards tip)
                float kajiyaSpec = sinTL * sinTV - TdotL * TdotV;
                kajiyaSpec = saturate(kajiyaSpec);
                kajiyaSpec = pow(kajiyaSpec, 20) * _Smoothness * 0.5;

                // Basic diffuse
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = furColor * mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Subsurface scattering for organic look
                float3 lightBack = -mainLight.direction;
                float sss = saturate(dot(viewDirWS, lightBack + normalWS * _SubsurfaceRadius));
                sss = pow(sss, 2) * _SubsurfaceStrength;
                half3 subsurface = sss * _SubsurfaceColor.rgb * mainLight.color * mainLight.shadowAttenuation;

                // Rim lighting for silhouette pop
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower) * _RimStrength;
                half3 rimContrib = rim * _RimColor.rgb * mainLight.color;

                // Apply frost overlay for blizzard conditions
                half3 finalFurColor = lerp(furColor, _FrostColor.rgb, _FrostAmount * 0.5);

                // Ambient
                half3 ambient = SampleSH(normalWS) * finalFurColor;

                // Specular from Kajiya-Kay
                half3 specular = kajiyaSpec * mainLight.color * mainLight.shadowAttenuation;

                // Combine all lighting
                half3 finalColor = ambient + diffuse + specular + subsurface + rimContrib;

                // Add eye glow (controlled externally for menacing effect)
                finalColor += _EyeGlowColor.rgb * _EyeGlowIntensity;

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

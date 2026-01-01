Shader "Shredsquatch/Powerup Glow"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR][MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Glow Animation)]
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 3
        _PulseMin("Pulse Minimum", Range(0, 1)) = 0.5
        _PulseMax("Pulse Maximum", Range(1, 3)) = 1.5

        [Header(Rotation)]
        _RotationSpeed("Rotation Speed", Range(0, 5)) = 1

        [Header(Outline Glow)]
        [HDR] _OutlineColor("Outline Color", Color) = (2, 2, 2, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
        _OutlinePulse("Outline Pulse", Range(0, 1)) = 0.5

        [Header(Fresnel)]
        [HDR] _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 2
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 1

        [Header(Particles)]
        _ParticleScale("Particle Scale", Range(1, 50)) = 20
        _ParticleSpeed("Particle Speed", Range(0, 5)) = 1
        _ParticleIntensity("Particle Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        // Main pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                half _PulseSpeed;
                half _PulseMin;
                half _PulseMax;
                half _RotationSpeed;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _OutlinePulse;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelStrength;
                half _ParticleScale;
                half _ParticleSpeed;
                half _ParticleIntensity;
            CBUFFER_END

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // Rotate UV
            float2 rotateUV(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                return mul(rotMatrix, uv - 0.5) + 0.5;
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
                float time = _Time.y;

                // Rotating UV for texture
                float2 rotatedUV = rotateUV(input.uv, time * _RotationSpeed);
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, rotatedUV) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Pulsing intensity
                float pulse = sin(time * _PulseSpeed) * 0.5 + 0.5;
                float pulseIntensity = lerp(_PulseMin, _PulseMax, pulse);

                // Fresnel glow
                float fresnel = 1.0 - saturate(dot(viewDirWS, normalWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelStrength;
                half3 fresnelContrib = fresnel * _FresnelColor.rgb * pulseIntensity;

                // Floating particles effect
                float3 particlePos = input.positionWS * _ParticleScale;
                particlePos.y += time * _ParticleSpeed;
                float particle = hash(floor(particlePos));
                particle = step(0.95, particle) * _ParticleIntensity;
                half3 particleContrib = particle * baseColor.rgb;

                // Get main light for basic lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));

                // Combine
                half3 finalColor = baseColor.rgb * pulseIntensity;
                finalColor += fresnelContrib;
                finalColor += particleContrib;

                // Add some ambient light interaction
                finalColor *= (0.5 + NdotL * 0.5);

                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Outline pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _PulseSpeed;
                half _PulseMin;
                half _PulseMax;
                half _RotationSpeed;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _OutlinePulse;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelStrength;
                half _ParticleScale;
                half _ParticleSpeed;
                half _ParticleIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Pulse the outline width
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float outlineWidth = _OutlineWidth * (1.0 + pulse * _OutlinePulse);

                // Expand along normal
                float3 posOS = input.positionOS.xyz + input.normalOS * outlineWidth;

                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Pulsing outline color
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                half3 color = _OutlineColor.rgb * (0.8 + pulse * 0.4);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}

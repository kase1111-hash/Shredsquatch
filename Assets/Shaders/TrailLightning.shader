Shader "Shredsquatch/Trail Lightning"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
        [HDR] _Color("Main Color", Color) = (0.5, 0.8, 3, 1)

        [Header(Lightning Animation)]
        _BoltFrequency("Bolt Frequency", Range(1, 20)) = 8
        _BoltSpeed("Bolt Speed", Range(1, 20)) = 10
        _BoltThickness("Bolt Thickness", Range(0.01, 0.3)) = 0.08
        _BranchChance("Branch Chance", Range(0, 1)) = 0.3

        [Header(Color)]
        [HDR] _CoreColor("Core Color", Color) = (2, 2, 5, 1)
        [HDR] _GlowColor("Glow Color", Color) = (0.3, 0.5, 2, 1)
        _GlowRadius("Glow Radius", Range(0, 0.5)) = 0.15

        [Header(Flicker)]
        _FlickerSpeed("Flicker Speed", Range(0, 50)) = 25
        _FlickerIntensity("Flicker Intensity", Range(0, 1)) = 0.5

        [Header(Trail)]
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.3
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

            Blend SrcAlpha One // Additive blend for electric glow
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
                half4 _Color;
                half _BoltFrequency;
                half _BoltSpeed;
                half _BoltThickness;
                half _BranchChance;
                half4 _CoreColor;
                half4 _GlowColor;
                half _GlowRadius;
                half _FlickerSpeed;
                half _FlickerIntensity;
                half _EdgeSoftness;
            CBUFFER_END

            // Hash functions
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Noise for bolt displacement
            float noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), f);
            }

            // Generate lightning bolt path
            float lightningBolt(float2 uv, float time, float seed)
            {
                float bolt = 0.0;

                // Main bolt path - zigzag pattern
                float segments = _BoltFrequency;
                float y = uv.y;

                // Jitter the x position based on segment
                float segment = floor(y * segments);
                float localY = frac(y * segments);

                // Random offset per segment
                float offset1 = (hash(segment + seed) - 0.5) * 0.4;
                float offset2 = (hash(segment + 1.0 + seed) - 0.5) * 0.4;

                // Interpolate between segment endpoints
                float boltX = lerp(offset1, offset2, localY) + 0.5;

                // Distance to bolt
                float dist = abs(uv.x - boltX);

                // Bolt core
                float core = 1.0 - smoothstep(0, _BoltThickness, dist);

                // Glow around bolt
                float glow = 1.0 - smoothstep(0, _GlowRadius, dist);
                glow *= 0.5;

                bolt = core + glow;

                // Add branches
                if (hash(segment + seed * 2.0) < _BranchChance)
                {
                    float branchDir = sign(hash(segment + seed * 3.0) - 0.5);
                    float branchStart = localY;
                    float branchX = boltX + branchDir * localY * 0.3;

                    float branchDist = abs(uv.x - branchX);
                    float branchCore = 1.0 - smoothstep(0, _BoltThickness * 0.5, branchDist);
                    branchCore *= step(0.3, localY) * (1.0 - localY);

                    bolt += branchCore * 0.7;
                }

                return bolt;
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
                float time = _Time.y * _BoltSpeed;

                // Generate multiple bolts with time-varying seeds
                float bolt1 = lightningBolt(uv, time, floor(time * 0.5));
                float bolt2 = lightningBolt(uv, time, floor(time * 0.5) + 100.0);

                // Flicker effect - rapid intensity changes
                float flicker = 1.0 + (hash(floor(time * _FlickerSpeed)) - 0.5) * _FlickerIntensity * 2.0;

                float totalBolt = max(bolt1, bolt2 * 0.7) * flicker;

                // Color based on intensity
                half3 color;
                if (totalBolt > 0.8)
                    color = _CoreColor.rgb;
                else if (totalBolt > 0.3)
                    color = lerp(_GlowColor.rgb, _CoreColor.rgb, (totalBolt - 0.3) / 0.5);
                else
                    color = _GlowColor.rgb * (totalBolt / 0.3);

                // Apply main color tint
                color *= _Color.rgb;

                // Vertical fade
                float verticalFade = 1.0 - uv.y;

                // Edge softness
                float edgeDist = abs(uv.x - 0.5) * 2.0;
                float edgeFade = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, edgeDist);

                float alpha = saturate(totalBolt) * verticalFade * edgeFade;
                alpha *= input.color.a;

                // Apply vertex color
                color *= input.color.rgb;

                // Apply fog
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}

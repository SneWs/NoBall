Shader "NoBall/Atom"
{
    Properties
    {
        _White ("White", Color) = (0.96, 0.95, 0.92, 1)
        _Red ("Red", Color) = (0.84, 0.23, 0.18, 1)
        _Outline ("Outline", Color) = (0.10, 0.05, 0.05, 1)
        _Seed ("Seed", Float) = 0
        _Flash ("Flash", Float) = 0
        _AnimTime ("AnimTime", Float) = 0
        _LightDir ("Light Dir", Vector) = (0.35, 0.8, -0.5, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Atom"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _White;
                float4 _Red;
                float4 _Outline;
                float _Seed;
                float _Flash;
                float _AnimTime;
                float4 _LightDir;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetCameraPositionWS() - positionWS;
                return output;
            }

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll]
                for (int n = 0; n < 4; n++)
                {
                    v += a * Noise(p);
                    p = p * 2.07 + 11.3;
                    a *= 0.5;
                }
                return v;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float3 l = normalize(_LightDir.xyz);
                float3 h = normalize(l + v);
                float ndl = saturate(dot(n, l) * 0.65 + 0.35);
                float spec = pow(saturate(dot(n, h)), 42.0);
                float fresnel = pow(1.0 - saturate(dot(n, v)), 2.6);

                float t = _AnimTime + _Seed * 12.7;
                float3 os = input.positionOS;
                float2 flowA = float2(os.x * 4.2 + os.z * 1.4, os.y * 4.2 - t * 0.35);
                float2 flowB = float2(os.z * 5.1 + t * 0.22, os.y * 3.4 + os.x * 2.1);
                float veins = Fbm(flowA);
                float swirl = Fbm(flowB + veins * 1.4);

                float stripe = smoothstep(0.22, 0.12, abs(os.x));
                float3 white = _White.rgb * (0.82 + 0.22 * swirl);
                white = lerp(white, white * float3(0.78, 0.88, 1.02), veins * 0.28);
                float pulse = 0.55 + 0.45 * sin(t * 3.1 + swirl * 6.0);
                float3 red = _Red.rgb * (0.78 + 0.35 * veins * pulse);
                red += float3(0.35, 0.08, 0.04) * saturate(swirl - 0.45) * pulse;

                float3 col = lerp(white, red, stripe);
                col *= 0.42 + 0.72 * ndl;
                col += spec * lerp(float3(1.0, 0.96, 0.88), float3(1.0, 0.55, 0.35), stripe) * 0.55;
                col = lerp(col, _Outline.rgb, saturate(fresnel * 1.15));
                col += stripe * red * fresnel * 0.25 * pulse;
                col = lerp(col, float3(1.0, 0.86, 0.45), saturate(_Flash) * 0.7);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}

Shader "NoBall/GameBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Effect ("Effect", Float) = 1
        _Aspect ("Aspect", Float) = 1.777
        _UseTex ("UseTex", Float) = 1
        _AnimTime ("AnimTime", Float) = 0
        _Darken ("Darken", Float) = 0
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
            Name "Background"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Effect;
                float _Aspect;
                float _UseTex;
                float _AnimTime;
                float _Darken;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
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
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return v;
            }

            float Bubbles(float2 uv, float t)
            {
                float acc = 0.0;
                [unroll]
                for (int i = 0; i < 10; i++)
                {
                    float fi = (float)i + 1.3;
                    float x = Hash(float2(fi, 2.17));
                    float speed = 0.07 + Hash(float2(fi, 5.91)) * 0.14;
                    float y = frac(Hash(float2(fi, 8.44)) + t * speed);
                    float r = 0.007 + Hash(float2(fi, 3.62)) * 0.012;
                    float2 d = (uv - float2(x, y)) * float2(_Aspect, 1.0);
                    float dist = length(d);
                    float ring = abs(dist - r);
                    acc += smoothstep(r * 0.55, 0.0, ring) * 0.65;
                    acc += smoothstep(r * 0.35, 0.0, dist) * 0.2;
                }
                return saturate(acc);
            }

            float Steam(float2 uv, float t)
            {
                float2 p = uv * float2(2.2, 1.5);
                p.y -= t * 0.06;
                p.x += t * 0.02 + Fbm(uv * 1.4 + t * 0.04) * 0.4;
                float n = Fbm(p);
                float n2 = Fbm(p * 2.15 + 12.0);
                float wisp = smoothstep(0.36, 0.62, n) * smoothstep(0.58, 0.32, n2);
                float height = smoothstep(0.08, 0.38, uv.y) * smoothstep(1.08, 0.52, uv.y);
                float2 p2 = uv * float2(1.6, 1.1) + float2(t * 0.015, -t * 0.035);
                float layer = smoothstep(0.4, 0.68, Fbm(p2 + 7.0));
                return saturate(wisp * height * 1.25 + layer * height * 0.35);
            }

            float BrokenFlicker(float t, float seed)
            {
                float period = Hash(float2(floor(t * 0.28 + seed * 5.0), seed));
                float failing = step(0.86, period);
                float chatter = Hash(float2(floor(t * 20.0 + seed * 9.0), seed + 2.1));
                float buzz = 0.93 + 0.07 * sin(t * 52.0 + seed * 18.0);
                return lerp(buzz, chatter, failing);
            }

            float3 ProceduralWaterfall(float2 uv, float t)
            {
                float nRock = Fbm(float2(uv.x * 5.0, uv.y * 2.4));
                float left = 0.20 + 0.09 * nRock;
                float right = 0.80 - 0.09 * Fbm(float2(uv.x * 5.0 + 9.0, uv.y * 2.4));
                float water = smoothstep(left - 0.03, left + 0.04, uv.x) * smoothstep(right + 0.03, right - 0.04, uv.x);

                float3 rock = lerp(float3(0.09, 0.11, 0.07), float3(0.16, 0.30, 0.14), Fbm(uv * 7.0));
                float flow = Fbm(float2(uv.x * 16.0, uv.y * 3.4 - t * 1.35));
                float streak = Fbm(float2(uv.x * 40.0, uv.y * 7.0 - t * 2.35));
                float3 waterCol = lerp(float3(0.10, 0.30, 0.36), float3(0.52, 0.80, 0.86), flow);
                waterCol = lerp(waterCol, float3(0.92, 0.96, 0.98), saturate(streak * 0.55));
                float mist = saturate(1.0 - uv.y * 2.6) * (0.35 + 0.65 * Fbm(float2(uv.x * 5.0, t * 0.35)));
                waterCol = lerp(waterCol, float3(0.84, 0.91, 0.94), mist);
                float3 col = lerp(rock, waterCol, water);
                col += float3(0.55, 0.62, 0.35) * saturate(uv.y - 0.72) * 0.25;
                return col;
            }

            float3 ProceduralLaboratory(float2 uv, float t)
            {
                float3 col = lerp(float3(0.04, 0.07, 0.08), float3(0.07, 0.10, 0.12), uv.y);
                float floorMask = smoothstep(0.26, 0.22, uv.y);
                float2 tile = float2(uv.x * 9.0, (0.24 - uv.y) * 16.0);
                float checker = abs(floor(tile.x) + floor(tile.y));
                checker = fmod(checker, 2.0);
                float3 floorCol = lerp(float3(0.06, 0.07, 0.08), float3(0.12, 0.14, 0.15), checker);
                col = lerp(col, floorCol, floorMask);

                [unroll]
                for (int k = 0; k < 3; k++)
                {
                    float cx = 0.22 + (float)k * 0.28;
                    float2 p = float2((uv.x - cx) * _Aspect, uv.y - 0.52);
                    float tank = 1.0 - saturate(max(abs(p.x) * 14.0, abs(p.y) * 3.15));
                    float liquid = step(uv.y, 0.70 + 0.02 * sin(t * 1.4 + (float)k));
                    float3 glow = lerp(float3(0.08, 0.55, 0.22), float3(0.12, 0.75, 0.80), (float)k / 2.0);
                    col = lerp(col, glow * (0.35 + 0.25 * sin(t * 1.6 + k)), tank * liquid);
                    col += glow * tank * 0.12;
                }

                float steam = Steam(uv, t);
                col = lerp(col, float3(0.58, 0.66, 0.64), steam * 0.48);
                float mist = saturate(0.24 - uv.y) * (0.4 + 0.6 * Fbm(float2(uv.x * 3.5 + t * 0.05, uv.y * 9.0)));
                col = lerp(col, float3(0.42, 0.50, 0.48), mist * 0.4);

                [unroll]
                for (int L = 0; L < 3; L++)
                {
                    float2 lampPos = L == 0 ? float2(0.72, 0.84) : (L == 1 ? float2(0.88, 0.86) : float2(0.55, 0.90));
                    float flicker = BrokenFlicker(t, 0.17 + (float)L * 1.9);
                    float2 d = (uv - lampPos) * float2(_Aspect, 1.0);
                    float cone = saturate(1.0 - length(d * float2(1.6, 0.7)) * 3.4);
                    cone *= smoothstep(lampPos.y + 0.02, lampPos.y - 0.45, uv.y);
                    col += float3(0.70, 0.52, 0.22) * cone * flicker * 0.45;
                    float spark = step(0.96, Hash(float2(floor(t * 6.0), 4.0 + L))) *
                                  pow(saturate(1.0 - length(d) * 14.0), 2.0);
                    col += spark * float3(1.0, 0.82, 0.4);
                }

                return col;
            }

            float3 ProceduralAquarium(float2 uv, float t)
            {
                float3 deep = float3(0.02, 0.08, 0.18);
                float3 shallow = float3(0.10, 0.38, 0.55);
                float3 col = lerp(deep, shallow, saturate(uv.y * 0.85 + 0.1));
                float caustic = sin((uv.x * 18.0 + uv.y * 4.0) + t * 1.4) * sin((uv.x * 7.0 - uv.y * 11.0) + t * 0.9);
                col += float3(0.20, 0.45, 0.50) * caustic * 0.08 * uv.y;
                col += Bubbles(uv, t) * float3(0.75, 0.90, 1.0) * 0.55;

                [unroll]
                for (int s = 0; s < 5; s++)
                {
                    float fs = (float)s;
                    float speed = 0.04 + Hash(float2(fs, 1.4)) * 0.05;
                    float y = 0.22 + Hash(float2(fs, 7.2)) * 0.5;
                    float x = frac(Hash(float2(fs, 3.8)) + t * speed);
                    float dir = Hash(float2(fs, 9.1)) > 0.5 ? 1.0 : -1.0;
                    float2 fp = float2((uv.x - x) * _Aspect * dir, uv.y - y);
                    float body = 1.0 - saturate(length(fp * float2(7.5, 14.0)));
                    float3 fish = lerp(float3(0.95, 0.75, 0.12), float3(0.95, 0.40, 0.10), Hash(float2(fs, 4.4)));
                    col = lerp(col, fish, saturate(body * 1.4));
                }

                float weed = 0.0;
                weed += saturate(1.0 - abs(uv.x - 0.08 - 0.02 * sin(uv.y * 14.0 + t)) * 28.0) * saturate(0.55 - uv.y);
                weed += saturate(1.0 - abs(uv.x - 0.92 + 0.02 * sin(uv.y * 12.0 + t * 1.2)) * 28.0) * saturate(0.6 - uv.y);
                col = lerp(col, float3(0.18, 0.55, 0.22), weed * 0.85);
                col += float3(0.55, 0.45, 0.12) * saturate(uv.y - 0.55) * 0.2;
                return col;
            }

            float3 ProceduralAurora(float2 uv, float t)
            {
                float3 sky = lerp(float3(0.02, 0.03, 0.10), float3(0.08, 0.05, 0.16), uv.y);
                float sweep = sin(t * 0.55);
                float pulse = 0.5 + 0.5 * sin(t * 1.05);
                float pulse2 = 0.5 + 0.5 * sin(t * 0.62 + 1.7);

                float band = Fbm(float2(uv.x * 2.2 + sweep * 0.35, uv.y * 3.0 + t * 0.04));
                float curtain = saturate(sin((uv.x + band * 0.4 + sweep * 0.12) * 5.5 + t * 0.7) * 0.5 + 0.5);
                curtain *= pow(saturate(sin((uv.x * 3.2 - t * 0.85) + band) * 0.5 + 0.5), 1.4);
                curtain *= smoothstep(0.18, 0.42, uv.y) * smoothstep(1.08, 0.58, uv.y);
                float3 aurora = lerp(float3(0.08, 0.85, 0.32), float3(0.90, 0.18, 0.55), Fbm(uv * 2.4 + t * 0.06));
                float shine = (0.45 + 0.55 * pulse) * (0.4 + 0.6 * pulse2);
                sky += aurora * curtain * shine * 1.15;
                sky += float3(0.18, 0.55, 0.28) * curtain * pulse * 0.35;

                float shaft = pow(saturate(sin((uv.x + sweep * 0.15) * 6.2831 + t * 0.65) * 0.5 + 0.5), 3.2);
                shaft *= smoothstep(0.0, 0.22, uv.y) * smoothstep(0.78, 0.32, uv.y);
                sky += float3(0.16, 0.48, 0.24) * shaft * (0.14 + 0.22 * pulse);

                float ground = smoothstep(0.28, 0.18, uv.y);
                float trees = step(uv.y, 0.22 + 0.06 * Noise(float2(uv.x * 22.0, 4.0)));
                float3 land = lerp(float3(0.05, 0.07, 0.12), float3(0.12, 0.16, 0.20), uv.y * 2.0);
                land += float3(0.10, 0.38, 0.22) * ground * curtain * pulse * 0.55;
                sky = lerp(sky, land, max(ground, trees * (1.0 - ground)));
                return sky;
            }

            float3 SampleBase(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            }

            float3 TexturedWaterfall(float2 uv, float t)
            {
                float3 baseCol = SampleBase(uv);
                float water = saturate(baseCol.g * 0.35 + baseCol.b * 0.55 - baseCol.r * 0.4 + 0.12);
                float2 warp = float2(
                    (Fbm(float2(uv.x * 9.0, uv.y * 3.0 - t)) - 0.5) * 0.014,
                    (Fbm(float2(uv.x * 5.0, uv.y * 4.0 - t * 1.25)) - 0.5) * 0.022 - t * 0.018
                ) * water;
                float3 col = SampleBase(saturate(uv + warp));
                float streak = pow(saturate(Fbm(float2(uv.x * 28.0, uv.y * 8.0 - t * 2.1))), 2.5) * water;
                col += streak * 0.22;
                float mist = saturate(0.24 - uv.y) * (0.4 + 0.6 * Fbm(float2(uv.x * 4.0, t * 0.3)));
                col = lerp(col, float3(0.86, 0.93, 0.96), mist * 0.4);
                return col;
            }

            float3 TexturedLaboratory(float2 uv, float t)
            {
                float lum = dot(SampleBase(uv), float3(0.30, 0.50, 0.20));
                float2 steamWarp = float2(
                    (Fbm(float2(uv.x * 3.0, uv.y * 2.0 + t * 0.08)) - 0.5) * 0.012,
                    (Fbm(float2(uv.x * 2.0 + 4.0, uv.y * 3.0 - t * 0.06)) - 0.5) * 0.018
                );
                steamWarp *= saturate(lum * 1.6) * smoothstep(0.12, 0.4, uv.y);
                float3 col = SampleBase(saturate(uv + steamWarp));

                float green = saturate(col.g - max(col.r, col.b) * 0.65);
                col += green * float3(0.04, 0.16, 0.07) * (0.4 + 0.6 * sin(t * 1.35));

                float2 lampCell = floor(uv * float2(7.0, 4.5));
                float seed = Hash(lampCell + 3.7);
                float warm = saturate(col.r * 1.2 + col.g * 0.5 - col.b * 1.4 - 0.08);
                warm *= smoothstep(0.48, 0.74, uv.y);
                float flicker = BrokenFlicker(t, seed);
                col *= 1.0 - warm * (1.0 - flicker) * 0.82;
                col += warm * flicker * float3(0.45, 0.28, 0.06) * 0.55;

                float sparkGate = step(0.965, Hash(float2(floor(t * 5.2), seed * 8.0)));
                float2 sparkPos = float2(0.78 + 0.06 * sin(t * 0.7), 0.81);
                float spark = pow(saturate(1.0 - length((uv - sparkPos) * float2(_Aspect, 1.0)) * 16.0), 2.4);
                col += sparkGate * spark * float3(1.0, 0.84, 0.42) * 1.15;

                float2 sparkPos2 = float2(0.90, 0.84);
                float spark2 = pow(saturate(1.0 - length((uv - sparkPos2) * float2(_Aspect, 1.0)) * 18.0), 2.4);
                float sparkGate2 = step(0.972, Hash(float2(floor(t * 4.4 + 2.0), 11.0)));
                col += sparkGate2 * spark2 * float3(1.0, 0.78, 0.32);

                float sat = length(col - dot(col, float3(0.33, 0.33, 0.33)));
                float led = saturate(sat * 4.2 - 0.4) * (1.0 - warm) * smoothstep(0.58, 0.12, uv.y);
                float ledFlick = 0.12 + 0.88 * step(0.32, Hash(float2(lampCell.x + lampCell.y, floor(t * (1.1 + seed * 2.4)))));
                col *= 1.0 - led * (1.0 - ledFlick) * 0.55;
                col += led * ledFlick * float3(0.12, 0.08, 0.04);

                float steam = Steam(uv, t);
                col = lerp(col, float3(0.60, 0.68, 0.66), steam * 0.4);
                float mist = saturate(0.22 - uv.y) * (0.35 + 0.65 * Fbm(float2(uv.x * 4.0 + t * 0.05, uv.y * 10.0)));
                col = lerp(col, float3(0.46, 0.54, 0.52), mist * 0.38);
                return col;
            }

            float3 TexturedAquarium(float2 uv, float t)
            {
                float2 warp = float2(
                    (Fbm(uv * 5.0 + t * 0.15) - 0.5) * 0.012,
                    (Fbm(uv * 4.0 + 8.0 - t * 0.12) - 0.5) * 0.01
                );
                float3 col = SampleBase(saturate(uv + warp));
                float caustic = sin(uv.x * 20.0 + uv.y * 6.0 + t * 1.5) * sin(uv.x * 9.0 - uv.y * 13.0 + t * 0.8);
                col += float3(0.25, 0.55, 0.65) * caustic * 0.07;
                col += Bubbles(uv, t) * float3(0.80, 0.92, 1.0) * 0.4;
                return col;
            }

            float3 TexturedAurora(float2 uv, float t)
            {
                float sweep = sin(t * 0.52);
                float pulse = 0.5 + 0.5 * sin(t * 1.12);
                float pulse2 = 0.5 + 0.5 * sin(t * 0.68 + 1.9);

                float2 auroraUv = uv;
                auroraUv.x += sweep * 0.03 * smoothstep(0.16, 0.55, uv.y);
                auroraUv.y += (Fbm(float2(uv.x * 2.0, t * 0.18)) - 0.5) * 0.012 * smoothstep(0.22, 0.6, uv.y);
                float3 col = SampleBase(saturate(auroraUv));

                float green = saturate(col.g - col.r * 0.5 - col.b * 0.18);
                float magenta = saturate(col.r * 0.55 + col.b * 0.75 - col.g * 0.45);
                float aurora = saturate(green * 1.5 + magenta * 1.2) * smoothstep(0.16, 0.4, uv.y);

                float wave = 0.5 + 0.5 * sin(uv.x * 7.5 - t * 1.35 + Fbm(uv * 2.0) * 2.2);
                float wave2 = 0.5 + 0.5 * sin(uv.x * 4.6 + t * 0.95 + sweep);
                float shine = aurora * (0.3 + 0.7 * wave) * (0.45 + 0.55 * pulse);
                col += float3(0.10, 0.48, 0.16) * shine * 0.95;
                col += float3(0.48, 0.10, 0.38) * aurora * wave2 * pulse2 * 0.55;
                col += float3(0.20, 0.62, 0.30) * aurora * pulse * 0.32;

                float shaft = pow(saturate(sin((uv.x + sweep * 0.18) * 6.2831 + t * 0.7) * 0.5 + 0.5), 3.0);
                shaft *= smoothstep(0.0, 0.22, uv.y) * smoothstep(0.8, 0.34, uv.y);
                col += float3(0.14, 0.42, 0.22) * shaft * (0.16 + 0.28 * pulse);

                float ice = saturate(0.34 - uv.y);
                col += float3(0.10, 0.36, 0.22) * ice * (0.2 + 0.55 * wave) * pulse * 0.7;
                return col;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float t = _AnimTime;
                float3 col;

                if (_UseTex > 0.5)
                {
                    if (_Effect < 1.5)
                        col = TexturedWaterfall(uv, t);
                    else if (_Effect < 2.5)
                        col = TexturedLaboratory(uv, t);
                    else if (_Effect < 3.5)
                        col = TexturedAquarium(uv, t);
                    else
                        col = TexturedAurora(uv, t);
                }
                else
                {
                    if (_Effect < 1.5)
                        col = ProceduralWaterfall(uv, t);
                    else if (_Effect < 2.5)
                        col = ProceduralLaboratory(uv, t);
                    else if (_Effect < 3.5)
                        col = ProceduralAquarium(uv, t);
                    else
                        col = ProceduralAurora(uv, t);
                }

                float vig = smoothstep(1.15, 0.28, length((uv - 0.5) * float2(1.15, 1.0)));
                col *= 0.88 + 0.12 * vig;
                col *= 1.0 - saturate(_Darken);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}

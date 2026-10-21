// The bar over a tree being felled or a boulder being chipped (HarvestProgressBar), dressed like
// the rest of the interface (the user, 2026-10-21: "сделать полоску добычи более красивой"): a
// round parchment medallion on the left with the resource's icon in it, and a pill to its right
// -- an ink rim, a dark track, and a gold fill with a highlight along its top and slow diagonal
// stripes running through it while the work goes on.
//
// One quad, one material for every bar: the fill fraction and the icon come per bar through a
// MaterialPropertyBlock. Layout is in units of the quad's HEIGHT (x runs 0.._Aspect), so the
// medallion stays round whatever the bar's width.
Shader "CityBuilder/HarvestBar"
{
    Properties
    {
        _Fill ("Fill 0..1", Range(0, 1)) = 0.5
        _Aspect ("Width / height", Float) = 4
        [NoScaleOffset] _IconTex ("Icon texture", 2D) = "white" {}
        _IconRect ("Icon UV rect (x, y, w, h)", Vector) = (0, 0, 1, 1)
        _HasIcon ("Has an icon", Float) = 0
        _Ink ("Rim", Color) = (0.23, 0.14, 0.08, 1)
        _Track ("Track", Color) = (0.17, 0.11, 0.07, 0.92)
        _Parchment ("Medallion", Color) = (0.95, 0.89, 0.72, 1)
        _FillTop ("Fill, top", Color) = (1.0, 0.85, 0.42, 1)
        _FillBottom ("Fill, bottom", Color) = (0.88, 0.56, 0.16, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Fill;
                float _Aspect;
                float4 _IconRect;
                float _HasIcon;
                half4 _Ink;
                half4 _Track;
                half4 _Parchment;
                half4 _FillTop;
                half4 _FillBottom;
            CBUFFER_END

            TEXTURE2D(_IconTex); SAMPLER(sampler_IconTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // signed distance to a capsule running from x0 to x1 at height 0.5, radius r
            float Capsule(float2 p, float x0, float x1, float r)
            {
                float2 q = float2(p.x - clamp(p.x, x0, x1), p.y - 0.5);
                return length(q) - r;
            }

            // alpha of a shape edge, antialiased over about a pixel
            float Edge(float d)
            {
                float w = max(fwidth(d), 1e-4);
                return saturate(0.5 - d / w);
            }

            half4 Over(half4 under, half3 colour, float alpha)
            {
                return half4(lerp(under.rgb, colour, alpha), max(under.a, alpha));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = float2(input.uv.x * _Aspect, input.uv.y);
                half4 c = half4(0, 0, 0, 0);

                // the pill: rim, track, fill
                float x0 = 0.5, x1 = _Aspect - 0.3;
                float outer = Capsule(p, x0, x1, 0.3);
                float inner = Capsule(p, x0, x1, 0.22);
                c = Over(c, _Ink.rgb, Edge(outer));
                c = Over(c, _Track.rgb, Edge(inner) * _Track.a);

                float fillStart = 0.98;                      // just clear of the medallion
                float fillEnd = lerp(fillStart, x1 + 0.22, saturate(_Fill));
                float inFill = Edge(inner) * Edge(p.x - fillEnd);
                float t = saturate((p.y - 0.28) / 0.44);     // 0 at the track's bottom, 1 at its top
                half3 gold = lerp(_FillBottom.rgb, _FillTop.rgb, t);
                gold += 0.28 * smoothstep(0.68, 0.74, p.y) * (1.0 - smoothstep(0.74, 0.8, p.y)); // highlight
                float stripe = frac((p.x - p.y) * 2.2 - _Time.y * 0.6);
                gold *= 1.0 - 0.1 * step(0.5, stripe);
                c = Over(c, gold, inFill);

                // the medallion over the pill's left end
                float disc = length(p - float2(0.5, 0.5));
                c = Over(c, _Ink.rgb, Edge(disc - 0.5));
                c = Over(c, _Parchment.rgb, Edge(disc - 0.42));
                if (_HasIcon > 0.5)
                {
                    float2 iconUv = (p - float2(0.5, 0.5)) / 0.62 + 0.5;  // the icon fills most of the disc
                    if (all(iconUv >= 0) && all(iconUv <= 1))
                    {
                        half4 icon = SAMPLE_TEXTURE2D(_IconTex, sampler_IconTex, _IconRect.xy + iconUv * _IconRect.zw);
                        c = Over(c, icon.rgb, icon.a * Edge(disc - 0.42));
                    }
                }
                return c;
            }
            ENDHLSL
        }
    }
}

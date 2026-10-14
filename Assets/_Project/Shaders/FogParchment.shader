// The fog of war as an old map on parchment: the land the player has not explored is covered by a
// sheet of paper with the land under it drawn in ink -- coastlines and a wash over the water,
// contour lines over the hills, little tree crowns over the forests, stipple over the stony
// ground -- and a dark ink line along the border of what has been explored.
//
// The other face of the same fog as FogClouds.shader (the player picks one in the settings): the
// same blanket mesh, the same reveal mask, read the same way -- where the view ray meets the GROUND,
// so the drawing lies exactly over the land it depicts and the holes sit over the explored land.
//
// _ChartTex is the survey FogOfWarManager makes once from the map, one texel per cell, every channel
// softened so its 0.5 line is a smooth curve: R water, G land height / _HeightScale, B forest,
// A stony ground. Lines are drawn where a channel crosses a value, as wide as fwidth says a pixel is,
// so they stay one crisp stroke at every zoom.
Shader "CityBuilder/FogParchment"
{
    Properties
    {
        _MaskTex ("Reveal mask (R: 1 = revealed)", 2D) = "black" {}
        _ChartTex ("Chart (R water, G height, B forest, A stone)", 2D) = "black" {}
        _NoiseTex ("Noise (R broad, G fine)", 2D) = "gray" {}
        _PaperColor ("Paper", Color) = (0.93, 0.86, 0.68, 1)
        _StainColor ("Paper stain", Color) = (0.80, 0.66, 0.45, 1)
        _InkColor ("Ink", Color) = (0.30, 0.19, 0.10, 1)
        _WaterColor ("Water wash", Color) = (0.66, 0.74, 0.71, 1)
        _ForestColor ("Forest wash", Color) = (0.72, 0.76, 0.55, 1)
        _MapRect ("Map rect (x0, z0, 1/width, 1/depth)", Vector) = (0, 0, 0.005, 0.005)
        _MaskStep ("Mask blur step (uv)", Vector) = (0.0075, 0.0075, 0, 0)
        _Clearance ("Blanket height above the land", Float) = 3.5
        _HeightScale ("Metres the chart's height channel spans", Float) = 32
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Parchment"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_ChartTex);
            SAMPLER(sampler_ChartTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskTex_ST;
                float4 _ChartTex_ST;
                float4 _NoiseTex_ST;
                half4 _PaperColor;
                half4 _StainColor;
                half4 _InkColor;
                half4 _WaterColor;
                half4 _ForestColor;
                float4 _MapRect;
                float4 _MaskStep;
                float _Clearance;
                float _HeightScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half Revealed(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
            }

            // How much of a pen stroke of `width` pixels covers this pixel, for the line where
            // `value` crosses `level`.
            half Stroke(float value, float level, float width)
            {
                float distancePixels = abs(value - level) / max(fwidth(value), 1e-4);
                return saturate(width * 0.5 + 0.5 - distancePixels);
            }

            float2 Hash22(float2 p)
            {
                float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                q += dot(q, q.yzx + 33.33);
                return frac((q.xx + q.yz) * q.zy);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = IsPerspectiveProjection()
                    ? normalize(input.positionWS - _WorldSpaceCameraPos)
                    : GetViewForwardDir();
                float drop = _Clearance / max(-view.y, 0.25);
                float2 ground = input.positionWS.xz + view.xz * drop;

                float2 uv = (ground - _MapRect.xy) * _MapRect.zw;
                float2 s = _MaskStep.xy;
                half revealed = (Revealed(uv) * 2.0
                    + Revealed(uv + float2(s.x, s.y)) + Revealed(uv + float2(-s.x, s.y))
                    + Revealed(uv + float2(s.x, -s.y)) + Revealed(uv + float2(-s.x, -s.y))) / 6.0;
                float2 outside = max(-uv, uv - 1.0);
                float inMap = step(max(outside.x, outside.y), 0.0);
                revealed *= inMap;

                // ------------------------------------------------ the paper
                half broad = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, ground * 0.013).r;
                half fine = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, ground * 0.31).g;
                half3 colour = _PaperColor.rgb * (0.94 + fine * 0.08);
                colour = lerp(colour, _StainColor.rgb, saturate((broad - 0.62) * 2.2) * 0.45);

                // ------------------------------------------------ the land, in ink
                half4 chart = SAMPLE_TEXTURE2D(_ChartTex, sampler_ChartTex, uv) * inMap;
                half ink = 0;

                // Water: a pale wash, the shore drawn as a line, and two fainter lines echoing it
                // out on the water, the way old maps drew a coast.
                half water = chart.r;
                colour = lerp(colour, _WaterColor.rgb, smoothstep(0.42, 0.58, water));
                ink = max(ink, Stroke(water, 0.5, 1.6));
                ink = max(ink, Stroke(water, 0.68, 1.0) * 0.55);
                ink = max(ink, Stroke(water, 0.84, 0.9) * 0.35);

                // Hills: contour lines every 1.5 m of height, a bolder one every 6 m.
                float height = chart.g * _HeightScale;
                half onRelief = smoothstep(0.6, 1.2, height) * (1.0 - smoothstep(0.3, 0.5, water));
                ink = max(ink, Stroke(frac(height / 1.5 + 0.5), 0.5, 1.0) * onRelief * 0.75);
                ink = max(ink, Stroke(frac(height / 6.0 + 0.5), 0.5, 1.8) * onRelief);
                colour *= 1.0 - saturate(height / 20.0) * 0.12;

                // Forests: a wash, an outline, and a little crown for every tree-sized patch.
                half forest = chart.b * (1.0 - smoothstep(0.3, 0.5, water));
                half inForest = smoothstep(0.42, 0.58, forest + (broad - 0.5) * 0.15);
                colour = lerp(colour, _ForestColor.rgb, inForest * 0.55);
                ink = max(ink, Stroke(forest + (broad - 0.5) * 0.15, 0.5, 1.4));
                float2 crownCell = ground / 1.7;
                float2 crownId = floor(crownCell);
                float2 crownAt = frac(crownCell) - 0.5 - (Hash22(crownId) - 0.5) * 0.35;
                float crownR = length(crownAt);
                half crown = Stroke(crownR, 0.27, 1.2) * inForest * step(0.25, Hash22(crownId + 17.0).x);
                ink = max(ink, crown * 0.85);

                // Stony ground: stipple.
                float2 dotCell = ground / 0.8;
                float2 dotId = floor(dotCell);
                float dotR = length(frac(dotCell) - 0.5 - (Hash22(dotId) - 0.5) * 0.5);
                half stone = smoothstep(0.4, 0.6, chart.a) * step(0.45, Hash22(dotId + 5.0).y);
                ink = max(ink, saturate((0.11 - dotR) / max(fwidth(dotR), 1e-4)) * stone * 0.8);

                colour = lerp(colour, _InkColor.rgb, ink);

                // ------------------------------------------------ the border of the explored land
                // A wobbling edge, the paper browned towards it as if singed, a bold ink line
                // along it, and the sheet's shadow on the explored ground beside it.
                half cover = 1.0 - revealed + (broad - 0.5) * 0.18 + (fine - 0.5) * 0.06;
                half sheet = smoothstep(0.40, 0.46, cover);
                half nearEdge = 1.0 - saturate((cover - 0.46) * 5.0);
                colour = lerp(colour, _StainColor.rgb * 0.82, nearEdge * 0.55);
                colour = lerp(colour, _InkColor.rgb, Stroke(cover, 0.47, 2.4));

                // Narrow and faint: a sheet of paper lies close to the ground and throws a thin shadow.
                half shadowCover = 1.0 - Revealed(uv + _MaskStep.xy * float2(-0.6, -0.9)) * inMap + (broad - 0.5) * 0.18;
                half shadow = saturate((shadowCover - 0.40) * 8.0) * 0.18 * (1.0 - sheet);

                half total = sheet + shadow;
                half3 mixed = (colour * sheet + half3(0.16, 0.10, 0.05) * shadow) / max(total, 1e-3);
                return half4(mixed, saturate(total));
            }
            ENDHLSL
        }
    }
}

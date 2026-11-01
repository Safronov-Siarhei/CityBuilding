// «Zzz» over a sleeping dungeon monster (Dungeon/DungeonMonsters, the user 2026-11-01: «эффект,
// который показывает что они спят»): three Z letters rising from its head, growing, drifting and
// fading in turn. Drawn on a quad turned to the camera in the vertex stage (like CrystalHalo), the
// letters themselves cut from distance fields here -- no texture. _Phase staggers the sleepers.
Shader "CityBuilder/SleepZ"
{
    Properties
    {
        _BaseColor ("Letters", Color) = (0.92, 0.95, 1, 1)
        _EdgeColor ("Outline", Color) = (0.2, 0.25, 0.45, 1)
        _Size ("Size (world)", Float) = 0.8
        _Phase ("Phase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "DisableBatching" = "True" }

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
                half4 _BaseColor;
                half4 _EdgeColor;
                float _Size;
                float _Phase;
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
                float3 centre = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float3 centreVS = TransformWorldToView(centre);
                float2 corner = (input.uv - 0.5) * _Size;
                output.positionCS = TransformWViewToHClip(centreVS + float3(corner, 0.0));
                output.uv = input.uv;
                return output;
            }

            // distance to a Z in a -0.5..0.5 box: the top bar, the bottom bar and the diagonal
            float ZDistance(float2 p)
            {
                float top = abs(p.y - 0.38);
                float bottom = abs(p.y + 0.38);
                float bars = min(top, bottom) + max(abs(p.x) - 0.38, 0.0);
                float2 a = float2(0.38, 0.38), b = float2(-0.38, -0.38);
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                float diagonal = length(pa - ba * h);
                return min(bars, diagonal);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y + _Phase;
                float4 colour = 0;
                [unroll] for (int i = 0; i < 3; i++)
                {
                    float life = frac(t * 0.33 + i / 3.0);
                    float size = 0.16 + life * 0.14;
                    float2 at = float2(0.32 + life * 0.3 + 0.05 * sin(t * 2.0 + i), 0.18 + life * 0.62);
                    float2 p = (input.uv - at) / size;
                    if (any(abs(p) > 0.6)) continue;
                    float d = ZDistance(p);
                    float fade = sin(life * 3.14159);
                    float letter = 1.0 - smoothstep(0.07, 0.1, d);
                    float edge = (1.0 - smoothstep(0.13, 0.17, d)) * (1.0 - letter);
                    float4 c = float4(lerp(_EdgeColor.rgb, _BaseColor.rgb, letter / max(letter + edge, 1e-3)), max(letter, edge * 0.85) * fade);
                    colour = c.a > colour.a ? c : colour;
                }
                return half4(colour.rgb, colour.a);
            }
            ENDHLSL
        }
    }
}

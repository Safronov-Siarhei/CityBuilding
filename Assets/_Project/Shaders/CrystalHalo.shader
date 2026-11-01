// The halo round an upgrade crystal (Dungeon/DungeonCrystals): a soft glow that breathes, always
// facing the camera, with a few four-pointed sparkles rising and twinkling in it. Additive, on a
// quad with UVs 0..1 centred on the gem; the quad turns to the camera in the vertex stage, so no
// script touches it. _Phase staggers the crystals so they do not breathe in step.
Shader "CityBuilder/CrystalHalo"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.45, 0.85, 1, 1)
        _SparkColor ("Sparkles", Color) = (1, 1, 1, 1)
        _Size ("Size (cells)", Float) = 1.6
        _Phase ("Phase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "DisableBatching" = "True" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _SparkColor;
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

            float Spark(float2 p, float2 at, float size)
            {
                float2 d = abs(p - at) / size;
                float star = saturate(1.0 - (d.x * 6.0 + d.y)) + saturate(1.0 - (d.y * 6.0 + d.x));
                return star * saturate(1.0 - length(d) * 0.8);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y + _Phase;
                float2 p = input.uv - 0.5;
                float r = saturate(1.0 - length(p) * 2.0);
                float breathe = 0.75 + 0.25 * sin(t * 2.0);
                half3 colour = _BaseColor.rgb * (r * r * 0.9 + pow(r, 6.0) * 0.8) * breathe;
                // sparkles: each rises through the halo and twinkles, then starts again lower down
                float sparks = 0.0;
                [unroll] for (int i = 0; i < 6; i++)
                {
                    float k = i * 1.618 + _Phase;
                    float life = frac(t * 0.35 + i / 6.0);
                    float2 at = float2(sin(k * 3.1) * 0.28, -0.25 + life * 0.55);
                    float twinkle = sin(life * 3.14159) * (0.6 + 0.4 * sin(t * 9.0 + k * 5.0));
                    sparks += Spark(p, at, 0.07) * twinkle;
                }
                colour += _SparkColor.rgb * sparks;
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}

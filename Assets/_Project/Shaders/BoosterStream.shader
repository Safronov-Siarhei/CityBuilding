// The boosters' violet stream (the user, 2026-11-12: «фиолетовый поток воздуха, как будто магия»; look C
// of Design/store/Booster_Hud.png): ribbons spiralling round a boosted building (Shop/BoosterAuras builds
// the mesh once: uv.x runs along a ribbon, uv.y across it). Bright streaks run up the ribbon, soft at its
// edges and ends; additive, so it reads as light and costs one draw per building with no texture and no
// script touching it every frame. _Phase staggers the buildings.
Shader "CityBuilder/BoosterStream"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.75, 0.43, 1, 1)
        _Speed ("Streak speed", Float) = 0.6
        _Streaks ("Streaks along a ribbon", Float) = 5
        _Phase ("Phase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

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
                float _Speed;
                float _Streaks;
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
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float across = 1.0 - abs(input.uv.y * 2.0 - 1.0);
                across = across * across;
                float ends = smoothstep(0.0, 0.12, input.uv.x) * smoothstep(1.0, 0.8, input.uv.x);
                float s = frac(input.uv.x * _Streaks - _Time.y * _Speed + _Phase);
                // a streak: a sharp head, a long fading tail
                float streak = smoothstep(0.0, 0.08, s) * pow(1.0 - s, 2.5);
                float base = 0.18;
                float k = (base + streak * 1.4) * across * ends;
                return half4(_BaseColor.rgb * k, 1.0);
            }
            ENDHLSL
        }
    }
}

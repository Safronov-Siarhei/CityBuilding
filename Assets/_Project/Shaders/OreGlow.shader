// The glow under an ore vein (OreDepositSpawner): a soft round halo of the ore's colour lying on
// the ground, breathing slowly so a vein catches the eye across the map (the user, 2026-10-21:
// "на земле есть куски, где светится соответствующая руда"). Additive, drawn on a flat quad with
// UVs 0..1; the falloff and the pulse are both computed here, so it needs no texture and no
// script touching it every frame. _Phase staggers the veins so they do not pulse in step.
Shader "CityBuilder/OreGlow"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (1, 0.8, 0.3, 1)
        _Phase ("Pulse phase", Float) = 0
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
                float r = saturate(1.0 - length(input.uv - 0.5) * 2.0);
                float glow = r * r;
                float pulse = 0.75 + 0.25 * sin(_Time.y * 1.6 + _Phase); // always lit; the pulse only breathes
                return half4(_BaseColor.rgb * glow * pulse, 1.0);
            }
            ENDHLSL
        }
    }
}

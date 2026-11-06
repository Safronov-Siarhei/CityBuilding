// The low haze over the orcs' swamp island (the user, 2026-11-06: «мрачная дымка» instead of a storm
// cloud). A flat patch lying a little over the ground: soft round edges, slow drifting wisps from two
// sine fields -- no texture. Thin on purpose: the swamp must show through (the first try in the
// concept, thick and pale, hid the island). Mobile: a few patches only, alpha-blended, no depth write.
Shader "CityBuilder/SwampHaze"
{
    Properties
    {
        _BaseColor ("Haze", Color) = (0.40, 0.45, 0.37, 0.16)
        _Drift ("Drift speed", Float) = 0.06
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
                half4 _BaseColor;
                float _Drift;
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
                float2 world : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.uv = input.uv;
                output.world = world.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float edge = saturate(1.0 - dot(p, p));          // round, soft to nothing at the rim
                edge *= edge;
                float t = _Time.y * _Drift;
                float2 w = input.world * 0.11;
                float wisps = 0.55 + 0.25 * sin(w.x * 1.7 + t * 3.0 + sin(w.y * 1.3 - t * 2.0))
                                   + 0.20 * sin(w.y * 2.3 - t * 2.4 + sin(w.x * 0.9 + t));
                half4 c = _BaseColor;
                c.a *= edge * saturate(wisps);
                return c;
            }
            ENDHLSL
        }
    }
}

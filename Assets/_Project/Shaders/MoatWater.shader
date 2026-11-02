// The Moat's water (the user, 2026-11-02: "alive, like the sea", not still with a white stripe).
// Everything is in world space, so neighbouring cells run on as one stream: two sets of slow
// ripples crossing, the colour breathing between deep and shallow with them, sun glints where
// the crests meet, and a faint drift so the surface is never still. GPU only, one quad a cell.
Shader "CityBuilder/MoatWater"
{
    Properties
    {
        _Deep ("Deep", Color) = (0.11, 0.42, 0.66, 1)
        _Shallow ("Shallow", Color) = (0.3, 0.7, 0.9, 1)
        _Glint ("Glint", Color) = (0.95, 0.99, 1, 1)
        _Scale ("Ripples per metre", Float) = 2.2
        _Speed ("Ripple speed", Float) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep;
                half4 _Shallow;
                half4 _Glint;
                float _Scale;
                float _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 world : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world * _Scale;
                float t = _Time.y * _Speed;
                // two ripple trains at an angle, and a slow swell under them
                float a = sin(p.x * 1.7 + p.y * 0.6 + t * 2.3 + sin(p.y * 0.9 + t) * 0.8);
                float b = sin(p.y * 1.9 - p.x * 0.7 - t * 1.9 + sin(p.x * 1.1 - t * 0.7) * 0.8);
                float swell = sin(p.x * 0.35 + p.y * 0.27 + t * 0.6);
                float ripple = (a + b) * 0.25 + 0.5;
                half3 colour = lerp(_Deep.rgb, _Shallow.rgb, saturate(ripple * 0.8 + swell * 0.15 + 0.1));
                // glints where the crests cross, flickering as they pass
                float crest = saturate((a * b) - 0.72) / 0.28;
                float flicker = 0.6 + 0.4 * sin(t * 7.0 + p.x * 3.1 + p.y * 2.3);
                colour = lerp(colour, _Glint.rgb, crest * crest * flicker * 0.85);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}

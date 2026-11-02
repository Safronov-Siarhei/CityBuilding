// The surf round the archipelago's islands (Maps/Archipelago/ShoreSurf, the user 2026-11-02: «вода
// вокруг островов без какого-либо движения ... её края покачивались», options 1 + 2): a band laid
// along every coast whose uv.x is the distance from the shore in metres (below 0 over the sand).
// The water's edge runs up the beach and back -- a wet sheet behind a white crest -- each stretch
// of coast in its own time, and a broken line of foam breathes a little further out. No texture;
// the timing comes from the world position, so nothing runs on the CPU.
Shader "CityBuilder/ShoreSurf"
{
    Properties
    {
        _FoamColor ("Foam", Color) = (0.95, 0.98, 1, 1)
        _WetColor ("Wet sheet", Color) = (0.82, 0.94, 1, 1)
        _Reach ("How far the edge runs, metres", Float) = 0.9
        _Period ("Seconds a swing", Float) = 4.5
        _OuterLine ("The far foam line, metres out", Float) = 1.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }

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
                half4 _FoamColor;
                half4 _WetColor;
                float _Reach;
                float _Period;
                float _OuterLine;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 world : TEXCOORD0;
                float shore : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xz;
                output.shore = input.uv.x;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world;
                // each stretch of coast in its own time: slow, uneven phases along the shore
                float phase = sin(p.x * 0.11 + 1.3) * 2.1 + sin(p.y * 0.13 + 0.4) * 1.9 + sin((p.x + p.y) * 0.047) * 3.0;
                float t = _Time.y * 6.2831853 / _Period;
                float swing = 0.5 + 0.5 * sin(t + phase);
                // the edge: just off the sand at its lowest, _Reach further out at its highest... reversed,
                // the water climbing the beach (shore < 0) and running back
                float front = lerp(-0.35, _Reach - 0.35, 1.0 - swing);
                float d = input.shore;

                float alpha = 0;
                half3 colour = _WetColor.rgb;
                if (d <= front + 0.08)
                {
                    // the white crest at the edge, the wet sheet thinning behind it
                    float crest = 1.0 - saturate(abs(d - front) / 0.13);
                    float sheet = 0.32 * saturate(1.0 - (front - d) / 1.2) * saturate((d + 0.6) / 0.3);
                    alpha = max(crest * 0.9, sheet);
                    colour = lerp(_WetColor.rgb, _FoamColor.rgb, crest);
                }
                // the broken foam line out in the shallows, breathing in and out
                float line = _OuterLine + 0.25 * sin(t * 0.7 + phase * 1.3);
                float broken = saturate(sin(p.x * 0.83 + p.y * 0.61 + _Time.y * 0.35) * 2.2 + sin(p.x * 0.29 - p.y * 0.37) * 1.4 - 0.6);
                float far = (1.0 - saturate(abs(d - line) / 0.11)) * broken * 0.75;
                if (far > alpha)
                {
                    alpha = far;
                    colour = _FoamColor.rgb;
                }
                // fade out at the band's far edge so it never shows as a line of its own
                alpha *= saturate((2.3 - d) / 0.4);
                clip(alpha - 0.01);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}

// The opening film's marks on the ground round the portal (the user, 2026-11-10): glowing cracks
// running out from the platform when the shaking starts (_Mode 0), and the shock ring the opening
// throws over the square (_Mode 1). A flat additive quad, UV centre = the portal. _Grow is how far
// the cracks have run / the ring's radius (0..1 of the quad's half size); _Fade dims it; _T is the
// film's clock (set by the director). _Mode 2 is a flat glow (the runes lit on the portal stones).
Shader "CityBuilder/CineGroundFx"
{
    Properties
    {
        _Color ("Glow", Color) = (0.3, 0.85, 1, 1)
        _Mode ("0 cracks, 1 ring, 2 flat", Float) = 0
        _Grow ("Grow", Range(0, 1)) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _Inner ("Inner radius", Range(0, 1)) = 0.3
        _T ("Time", Float) = 0
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
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Mode;
                float _Grow;
                float _Fade;
                float _Inner;
                float _T;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float Hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float r = length(p);
                float a = atan2(p.y, p.x);
                if (_Mode > 1.5) return half4(_Color.rgb * _Fade, 1.0);
                if (_Mode > 0.5)
                {
                    // the shock ring: a soft bright band at _Grow, thinning as it runs out
                    float w = 0.03 + 0.05 * (1.0 - _Grow);
                    float band = exp(-pow((r - _Grow) / w, 2.0));
                    return half4(_Color.rgb * band * _Fade * 1.4, 1.0);
                }
                // the cracks: thirteen jagged lines from the platform's rim, each its own length
                float glow = 0.0;
                for (int k = 0; k < 13; k++)
                {
                    float base = (k + Hash(k) * 0.6) * 6.2832 / 13.0;
                    float len = _Inner + (1.0 - _Inner) * (0.45 + 0.55 * Hash(k + 17.0)) * _Grow;
                    float wob = 0.18 * sin(r * 23.0 + k * 3.1) * (r - _Inner) + 0.08 * sin(r * 61.0 + k);
                    float d = abs(sin(a - base - wob)) * r;
                    float crack = (1.0 - smoothstep(0.008, 0.026, d)) * step(_Inner, r) * (1.0 - smoothstep(len - 0.04, len, r));
                    glow = max(glow, crack);
                }
                float flicker = 0.8 + 0.2 * sin(_T * 9.0 + r * 12.0);
                return half4(_Color.rgb * glow * flicker * _Fade * 1.6, 1.0);
            }
            ENDHLSL
        }
    }
}

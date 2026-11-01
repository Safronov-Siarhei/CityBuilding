// The circle of runes glowing on the ground round an orc portal (Combat/OrcPortalFx): a ring of
// rune marks turning slowly and breathing, a soft glow inside it, and -- when a wave comes out
// (_Wave running 0..1) -- a shock ring rolling out over the grass and fading. Additive, on a flat
// quad with UVs 0..1; the ring sits at half the quad's radius so the shock ring has room to go.
Shader "CityBuilder/PortalRunes"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.62, 0.25, 1, 1)
        _WaveColor ("Shock ring", Color) = (0.5, 1, 0.35, 1)
        _Intensity ("Intensity", Float) = 1
        _Wave ("Shock ring 0..1 (0 = none)", Float) = 0
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
                half4 _WaveColor;
                float _Intensity;
                float _Wave;
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

            float Hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 p = (input.uv - 0.5) * 2.0;
                float r = length(p);
                float a = atan2(p.y, p.x) / 6.28318 + 0.5;
                // the band the runes are cut in, turning slowly
                const float Runes = 22.0;
                float u = frac(a + t * 0.015) * Runes;
                float index = floor(u);
                float x = frac(u);
                float y = saturate((r - 0.44) / 0.12);
                float band = step(0.44, r) * step(r, 0.56);
                float h = Hash(index);
                // a rune: an upright stroke and one or two slanted ones, picked by the hash
                float stroke = step(abs(x - 0.5), 0.07);
                stroke = max(stroke, step(abs(x - y), 0.08) * step(0.35, h));
                stroke = max(stroke, step(abs(x - (1.0 - y)), 0.08) * step(h, 0.7));
                stroke = max(stroke, step(abs(y - 0.5), 0.07) * step(0.8, h));
                float glyphs = stroke * band * step(0.12, x) * step(x, 0.88);
                // thin rings above and below the runes
                float rims = (1.0 - smoothstep(0.0, 0.015, abs(r - 0.42))) + (1.0 - smoothstep(0.0, 0.015, abs(r - 0.58)));
                float breathe = 0.65 + 0.35 * sin(t * 2.2 + index * 0.9);
                float inner = pow(saturate(1.0 - r / 0.5), 2.0) * 0.35;
                half3 colour = _BaseColor.rgb * ((glyphs * breathe + rims * 0.8) * 1.4 + inner) * _Intensity;
                // the shock ring of a wave coming out
                float waveR = 0.3 + _Wave * 0.7;
                float wave = (1.0 - smoothstep(0.0, 0.06, abs(r - waveR))) * (1.0 - _Wave) * step(0.001, _Wave);
                colour += _WaveColor.rgb * wave * 2.0;
                colour *= 1.0 - smoothstep(0.96, 1.0, r);
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}

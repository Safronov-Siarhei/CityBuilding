// The orc portal's whirlpool (Combat/OrcPortalFx, the user 2026-11-01: «сочнее, эффекты +
// движение»): spiral arms of violet and venom green turning into a white-hot eye, a bright rim,
// all inside the arch's opening (an ellipse over the quad's UVs 0..1). Additive, so the model's
// dark rim behind it is the night it burns in. _Intensity and _Speed tell the lit portal (the next
// wave's source) from a banked one; _Flash throws it white for a moment when a wave comes out.
Shader "CityBuilder/PortalVortex"
{
    Properties
    {
        _ColorA ("Arms", Color) = (0.62, 0.2, 1, 1)
        _ColorB ("Edge", Color) = (0.45, 1, 0.3, 1)
        _Intensity ("Intensity", Float) = 1
        _Speed ("Speed", Float) = 1
        _Flash ("Flash", Float) = 0
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
                half4 _ColorA;
                half4 _ColorB;
                float _Intensity;
                float _Speed;
                float _Flash;
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
                float t = _Time.y * _Speed;
                float2 p = (input.uv - 0.5) * 2.0;
                float r = length(p);
                float a = atan2(p.y, p.x);
                float inside = saturate((1.0 - r) * 6.0);
                // two spirals turning inwards, one fast and thin over one slow and broad
                float arms = pow(0.5 + 0.5 * sin(a * 3.0 + 1.6 / (r + 0.18) - t * 2.6), 3.0);
                float drift = 0.5 + 0.5 * sin(a * 5.0 - r * 11.0 + t * 3.7);
                float hue = saturate(r * 1.3 - 0.25 + 0.25 * sin(t * 0.7 + a));
                half3 colour = lerp(_ColorA.rgb, _ColorB.rgb, hue) * (arms * 0.95 + drift * 0.3 + 0.12);
                // the white-hot eye and the bright lip of the opening
                colour += half3(1.0, 0.9, 1.0) * pow(saturate(1.0 - r * 1.6), 3.0) * (0.9 + 0.3 * sin(t * 5.0));
                colour += _ColorB.rgb * smoothstep(0.72, 0.93, r) * (1.0 - smoothstep(0.93, 1.0, r)) * 1.3;
                colour *= inside * _Intensity * (1.0 + _Flash * 2.5);
                colour += half3(1.0, 0.95, 1.0) * _Flash * inside * 0.6;
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}

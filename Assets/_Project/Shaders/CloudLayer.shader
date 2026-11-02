// The clouds themselves (Maps/CloudLayer): a flat sheet at the field's height under the camera,
// drawing the same drifting field the ground's shadows come from (CloudField.hlsl), so every
// shadow has its cloud overhead. Soft white, a little grey-blue where thin; shown only near the
// farthest zoom (_Visible, faded by CloudLayer). Drawn after the fog of war (its blanket shares
// Transparent+50 and, sorted by its far-off centre, used to cover the clouds in a rectangle -- the
// user, 2026-11-02), and the sheet melts away towards its rim, so no edge of it is ever seen.
Shader "CityBuilder/CloudLayer"
{
    Properties
    {
        _Top ("Lit top", Color) = (1, 1, 1, 1)
        _Under ("Thin parts", Color) = (0.8, 0.86, 0.95, 1)
        _Opacity ("Opacity at full", Range(0, 1)) = 0.85
        _Visible ("Shown (0..1, set by CloudLayer)", Range(0, 1)) = 0
        _HalfSize ("The sheet's half size, metres", Float) = 70
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+80" "RenderPipeline" = "UniversalPipeline" }

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
            #include "CloudField.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Top;
                half4 _Under;
                half _Opacity;
                half _Visible;
                float _HalfSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 local : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.local = input.positionOS.xz / max(_HalfSize, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // a round sheet, thinning out over its outer third
                float rim = 1.0 - smoothstep(0.6, 0.98, length(input.local));
                float d = CloudDensity(input.positionWS.xz) * rim;
                clip(d * _Visible - 0.01);
                half3 colour = lerp(_Under.rgb, _Top.rgb, saturate(d * 1.4));
                return half4(colour, d * _Opacity * _Visible);
            }
            ENDHLSL
        }
    }
}

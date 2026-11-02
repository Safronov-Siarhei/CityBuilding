// The clouds themselves (Maps/CloudLayer): a flat sheet at the field's height under the camera,
// drawing the same drifting field the ground's shadows come from (CloudField.hlsl), so every
// shadow has its cloud overhead. Soft white, a little grey-blue where thin; shown only near the
// farthest zoom (_Visible, faded by CloudLayer).
Shader "CityBuilder/CloudLayer"
{
    Properties
    {
        _Top ("Lit top", Color) = (1, 1, 1, 1)
        _Under ("Thin parts", Color) = (0.8, 0.86, 0.95, 1)
        _Opacity ("Opacity at full", Range(0, 1)) = 0.85
        _Visible ("Shown (0..1, set by CloudLayer)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" }

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
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float d = CloudDensity(input.positionWS.xz);
                clip(d * _Visible - 0.01);
                half3 colour = lerp(_Under.rgb, _Top.rgb, saturate(d * 1.4));
                return half4(colour, d * _Opacity * _Visible);
            }
            ENDHLSL
        }
    }
}

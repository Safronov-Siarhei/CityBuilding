// A picture standing over something in the world, always turned to the camera -- the worker-shortage
// mark over a building (Citizens/WorkerShortageMarks, the user 2026-11-02). Turned in the vertex
// stage like SleepZ; bobs gently so the eye catches it. _Size is its width in world units.
Shader "CityBuilder/IconBillboard"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "white" {}
        _Size ("Size (world)", Float) = 1
        _Bob ("Bob (world)", Float) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" "DisableBatching" = "True" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Size;
                float _Bob;
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
                float3 centre = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                centre.y += sin(_Time.y * 3.0 + centre.x * 0.7) * _Bob;
                float3 centreVS = TransformWorldToView(centre);
                float2 corner = (input.uv - 0.5) * _Size;
                output.positionCS = TransformWViewToHClip(centreVS + float3(corner, 0.0));
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            }
            ENDHLSL
        }
    }
}

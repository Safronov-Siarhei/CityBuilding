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
        // a red ring round the picture, this much of it left (0..1); below 0 none (the sandbox
        // Лечебница's patient with no bed, Citizens/Infirmary, the user 2026-11-13)
        _Ring ("Ring left", Float) = -1
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
                float _Ring;
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
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (_Ring >= 0.0)
                {
                    float2 d = input.uv - 0.5;
                    float r = length(d);
                    if (r > 0.415 && r < 0.5)
                    {
                        // clockwise from the top: what is left is red, what is spent dark
                        float f = frac(atan2(d.x, d.y) / 6.2831853 + 1.0);
                        half4 ring = f <= _Ring ? half4(0.84, 0.13, 0.16, 1.0) : half4(0.18, 0.11, 0.07, 0.85);
                        c = lerp(c, ring, ring.a);
                    }
                }
                return c;
            }
            ENDHLSL
        }
    }
}

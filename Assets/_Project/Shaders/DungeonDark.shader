// The darkness a dungeon lies in (Dungeon/DungeonFog, the user 2026-11-01: «всё подземелье во
// тьме, тьма открывается по мере прохождения ... не за скалой»). Drawn over the whole screen
// last: every pixel's depth gives back the world point it shows, and that point's cell in the
// heroes' explored mask (_DarkMask, one texel a cell, eased) says how dark it lies -- the floor, the
// rock, a monster standing there alike. Beyond the map (and the empty void past its edge) is dark.
// Needs the camera's depth texture (DungeonFog turns it on for the main camera, in a dungeon only).
Shader "CityBuilder/DungeonDark"
{
    Properties
    {
        _DarkColor ("Darkness", Color) = (0.02, 0.02, 0.04, 1)
        // full: the blend is in linear light, where 3% let through still shows the map at a fifth of its brightness
        _Strength ("Strength", Float) = 1
        _DarkRect ("Mask rect: origin xz, 1/size xz", Vector) = (0, 0, 0.01, 0.01)
        _Soft ("Soft edge (mask texels)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_DarkMask);
            SAMPLER(sampler_DarkMask);
            float4 _DarkMask_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _DarkColor;
                float _Strength;
                float4 _DarkRect;
                float _Soft;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                // the quad's corners are the screen's (-1..1): no matrix, it is always the whole screen
                Varyings output;
                output.positionCS = float4(input.positionOS.xy, UNITY_NEAR_CLIP_VALUE, 1.0);
                return output;
            }

            float Open(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0)) return 0.0;
                return SAMPLE_TEXTURE2D(_DarkMask, sampler_DarkMask, uv).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screen = input.positionCS.xy / _ScaledScreenParams.xy;
                float depth = SampleSceneDepth(screen);
                #if UNITY_REVERSED_Z
                    bool sky = depth <= 0.0;
                #else
                    bool sky = depth >= 1.0;
                #endif
                float open = 0.0;
                if (!sky)
                {
                    float3 world = ComputeWorldSpacePosition(screen, depth, UNITY_MATRIX_I_VP);
                    float2 uv = (world.xz - _DarkRect.xy) * _DarkRect.zw;
                    float2 t = _DarkMask_TexelSize.xy * _Soft;
                    // a soft edge: the cell and its four neighbours half a cell out
                    open = Open(uv) * 0.4 + (Open(uv + float2(t.x, 0)) + Open(uv - float2(t.x, 0))
                        + Open(uv + float2(0, t.y)) + Open(uv - float2(0, t.y))) * 0.15;
                }
                return half4(_DarkColor.rgb, saturate(1.0 - open) * _Strength);
            }
            ENDHLSL
        }
    }
}

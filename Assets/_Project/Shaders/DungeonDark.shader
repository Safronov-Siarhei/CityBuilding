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
        _Soft ("Soft edge (mask texels)", Float) = 1.6
        _Wobble ("Edge wobble", Float) = 0.35
        // the camp lies ~7 cells from the south edge: the band (with its wobble) must stop short of it
        _MapEdge ("Darkness eating into the map's edge (cells)", Float) = 2
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
                float _Wobble;
                float _MapEdge;
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

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // value noise: smooth lumps a cell or two across, so the edge wanders like smoke, not in steps
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
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
                    // as the parchment's edge (the user, 2026-11-01: «темнота квадратами ... сделай
                    // такой же как пергамент»): the mask blurred over a few cells, then a wobbling,
                    // slowly drifting border cut through the blur, so it never shows the cell grid
                    float blurred = (Open(uv) * 2.0
                        + Open(uv + float2(t.x, t.y)) + Open(uv + float2(-t.x, t.y))
                        + Open(uv + float2(t.x, -t.y)) + Open(uv + float2(-t.x, -t.y))
                        + Open(uv + float2(t.x * 1.6, 0)) + Open(uv - float2(t.x * 1.6, 0))
                        + Open(uv + float2(0, t.y * 1.6)) + Open(uv - float2(0, t.y * 1.6))) / 10.0;
                    float drift = _Time.y * 0.05;
                    float lumps = Noise(world.xz * 0.45 + drift) * 0.65 + Noise(world.xz * 1.3 - drift) * 0.35;
                    float cover = 1.0 - blurred + (lumps - 0.5) * _Wobble;
                    open = 1.0 - smoothstep(0.3, 0.62, cover);
                    // the map's own edge: the darkness eats a wobbling band into it, so the end of
                    // the world is never a straight line (the user, 2026-11-01)
                    float2 cells = min(uv, 1.0 - uv) * _DarkMask_TexelSize.zw;
                    float inward = min(cells.x, cells.y) - _MapEdge + (lumps - 0.5) * _MapEdge * 1.2;
                    open *= smoothstep(0.0, _MapEdge * 0.5, inward);
                }
                return half4(_DarkColor.rgb, saturate(1.0 - open) * _Strength);
            }
            ENDHLSL
        }
    }
}

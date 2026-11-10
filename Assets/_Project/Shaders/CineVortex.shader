// The opening film's portal swirl (the user, 2026-11-10: «появление портала это просто пнг которое
// едет сверху вниз, надо добавить эффекты»). A quad filling the arch: a door shape (straight sides,
// a round top) with purple arms winding into a white-hot heart and pale dashes racing round. It opens
// from the heart outwards (_Open 0..1) behind a bright lip. _T is the film's own clock (set by the
// director every frame, so a film rendered frame by frame in the editor moves exactly as in play).
Shader "CityBuilder/CineVortex"
{
    Properties
    {
        _ColorA ("Arms", Color) = (0.42, 0.16, 0.85, 1)
        _ColorB ("Light arms", Color) = (0.86, 0.66, 1, 1)
        _Aspect ("Width / height", Float) = 0.6
        _Open ("Open", Range(0, 1)) = 1
        _T ("Time", Float) = 0
        _Flash ("Flash", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

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
                half4 _ColorA;
                half4 _ColorB;
                float _Aspect;
                float _Open;
                float _T;
                float _Flash;
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

            half4 Frag(Varyings input) : SV_Target
            {
                // in half-widths: x from -1 to 1, y from 0 (sill) to H (the top of the round head)
                float H = 2.0 / max(_Aspect, 0.05);
                float2 p = float2((input.uv.x - 0.5) * 2.0, input.uv.y * H);
                float headY = H - 1.0;
                float door = p.y < headY ? 1.0 - abs(p.x) : 1.0 - length(p - float2(0.0, headY));
                float inside = saturate(door * 14.0);
                float lip = smoothstep(0.0, 0.05, door) * (1.0 - smoothstep(0.05, 0.16, door));

                // the swirl round its heart, a little below the middle of the opening
                float2 c = p - float2(0.0, H * 0.46);
                float r = length(c) / (H * 0.5);
                float a = atan2(c.y, c.x);
                float t = _T;
                float arms = pow(0.5 + 0.5 * sin(a * 3.0 + 2.2 / (r + 0.16) - t * 2.4), 2.0);
                float fine = 0.5 + 0.5 * sin(a * 7.0 + 4.0 / (r + 0.25) - t * 4.1);
                half3 colour = lerp(_ColorA.rgb * 0.75, _ColorB.rgb, arms * 0.75 + fine * 0.2);
                // pale dashes racing round: short bright arcs on a few rings
                float ring = frac(r * 5.0 - t * 0.35);
                float dash = smoothstep(0.42, 0.5, ring) * (1.0 - smoothstep(0.5, 0.58, ring));
                dash *= step(0.62, frac(a / 6.2832 * 9.0 + r * 3.0 - t * 0.9));
                colour += half3(0.95, 0.88, 1.0) * dash * 0.85;
                // the white-hot heart, breathing
                colour = lerp(colour, half3(1.0, 0.97, 1.0), saturate(pow(saturate(1.0 - r * 2.6), 2.0) * (1.1 + 0.15 * sin(t * 5.0))));
                colour += half3(0.9, 0.75, 1.0) * lip * 0.9;

                // opening from the heart outwards behind a bright edge
                float reach = _Open * 1.35;
                float shown = 1.0 - smoothstep(reach - 0.06, reach, r);
                float edge = smoothstep(reach - 0.12, reach - 0.03, r) * shown * (1.0 - _Open * _Open);
                colour += half3(1.0, 0.9, 1.0) * edge * 1.5;
                colour += half3(1.0, 1.0, 1.0) * _Flash;

                float alpha = inside * shown * saturate(0.93 + lip + _Flash);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}

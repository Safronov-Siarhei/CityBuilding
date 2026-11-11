// The opening film's storm (the user, 2026-11-11: «с появлением портала небо затягивали тучи, оно
// становилось мрачным»). A big dome kept on the camera, drawn over the morning sky and under
// everything else: heavy grey-lilac clouds that roll in as _Cover goes 0..1 and drift on the film's
// own clock _T (set by OpeningFx, so the editor's frame-by-frame film moves as in play).
Shader "CityBuilder/CineStormSky"
{
    Properties
    {
        _Dark ("Cloud bellies", Color) = (0.22, 0.18, 0.25, 1)
        _Light ("Cloud tops", Color) = (0.5, 0.44, 0.52, 1)
        _Cover ("Cover", Range(0, 1)) = 0
        _T ("Time", Float) = 0
        _Scale ("Cloud scale", Float) = 1.6
        _Speed ("Drift", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-200" "RenderPipeline" = "UniversalPipeline" }

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
                half4 _Dark;
                half4 _Light;
                float _Cover;
                float _T;
                float _Scale;
                float _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.dir = ws - GetCameraPositionWS();
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int k = 0; k < 5; k++) { v += a * Noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return v;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                if (d.y < -0.02) return half4(0, 0, 0, 0);
                // the cloud deck as a ceiling: nearer overhead, squashed towards the horizon
                float2 uv = d.xz / (d.y + 0.12) * _Scale * 0.25;
                float2 drift = float2(_T, _T * 0.4) * _Speed;
                float n = Fbm(uv + drift);
                float fine = Fbm(uv * 2.7 - drift * 1.6);
                float edge = 1.0 - _Cover;
                float dens = smoothstep(edge - 0.12, edge + 0.2, n * 0.8 + fine * 0.25);
                // whatever is not cloud is a dim haze once the storm is in
                dens = max(dens, _Cover * _Cover * 0.75);
                half3 col = lerp(_Dark.rgb, _Light.rgb, saturate(fine * 1.3 - 0.2));
                float horizon = smoothstep(-0.03, 0.02, d.y);
                return half4(col, saturate(dens * horizon * min(1.0, _Cover * 1.5)));
            }
            ENDHLSL
        }
    }
}

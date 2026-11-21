// The studio splash's paper (Design/features/studio-splash.md), drawn instead of a stretched 1920x1080 picture
// (the user, 2026-11-21: on the tablet the splash looked short of resolution): the paper colour (vertex colour)
// under a warm vignette and a grain computed per SCREEN pixel, so it is crisp on any screen and costs no texture.
// The numbers are the preview's (gen_splash_ink.py), written into the material by SetupProjectSplash from
// UI/Splash/splash_ink.json.
Shader "CityBuilder/UI/SplashPaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _VignetteColor ("Vignette colour", Color) = (0.42, 0.31, 0.16, 1)
        _Vignette ("Vignette: centre xy, radius, inner stop", Vector) = (0.5, 0.46, 0.75, 0.55)
        _VignetteAlpha ("Vignette strength", Float) = 0.42
        _GrainColor ("Grain colour", Color) = (0.35, 0.27, 0.17, 1)
        _GrainAlpha ("Grain strength", Float) = 0.09
        _GrainFreq ("Grain frequency (per pixel)", Float) = 0.9
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            float4 _Vignette;
            fixed4 _VignetteColor, _GrainColor;
            float _VignetteAlpha, _GrainAlpha, _GrainFreq;
            float _UIVertexColorAlwaysGammaSpace;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace()) v.color.rgb = UIGammaToLinear(v.color.rgb);
                o.color = v.color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 c = i.color.rgb;
                // the vignette: an ellipse over the rect (the SVG gradient's objectBoundingBox), clear inside the inner stop
                float t = length((i.uv - _Vignette.xy) / _Vignette.z);
                float v = saturate((t - _Vignette.w) / (1.0 - _Vignette.w)) * _VignetteAlpha;
                c = lerp(c, _VignetteColor.rgb, v);
                // the grain: two octaves of value noise per screen pixel
                float2 px = i.pos.xy * _GrainFreq;
                float n = vnoise(px) * 0.65 + vnoise(px * 2.03 + 31.7) * 0.35;
                c = lerp(c, _GrainColor.rgb, n * _GrainAlpha);
                return fixed4(c, i.color.a);
            }
            ENDCG
        }
    }
}

// The studio splash's ink soaking into the four petals (Design/features/studio-splash.md): a UI shader for
// the RawImage that holds the filled petals (UI/Splash/fill.png, white + alpha, tinted ink by the vertex
// colour). The ink spreads from _Center out to _Radius (both in uv), its front made ragged by value noise
// the way the preview's displaced circle mask is. SplashInk drives _Radius every frame.
Shader "CityBuilder/UI/InkSoak"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Center ("Centre (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (uv)", Float) = 0
        _Edge ("Soft edge (uv)", Float) = 0.03
        _NoiseAmp ("Ragged front (uv)", Float) = 0.07
        _NoiseScale ("Noise scale", Float) = 9
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Center;
            float _Radius, _Edge, _NoiseAmp, _NoiseScale;
            float _UIVertexColorAlwaysGammaSpace;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // the same colour handling as UI/Default, so the fill matches the strokes' ink
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
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float n = vnoise(i.uv * _NoiseScale) * 0.65 + vnoise(i.uv * _NoiseScale * 2.3 + 17.0) * 0.35;
                float d = distance(i.uv, _Center.xy) + (n - 0.5) * 2.0 * _NoiseAmp;
                c.a *= 1.0 - smoothstep(_Radius - _Edge, _Radius, d);
                return c;
            }
            ENDCG
        }
    }
}

// The opening film's ground (the user, 2026-11-11: «трава монотонного зелёного цвета», «дороги --
// просто серая или коричневая плоскость»). Lit, world-space and procedural, so the village's flat
// ground pieces need no UVs:
//   _Mode 0 grass  -- the islands' colour zones (map3.py zone_colour): light flowery patches, dry
//                     yellowish meadow, darker long grass, small patches of a second shade, with
//                     soft edges; the islands' palette colours.
//   _Mode 1 cobble -- rounded setts in staggered rows, each its own shade, dark joints between.
//   _Mode 2 dirt   -- a packed earth lane: two wheel ruts along its length (_Dir), pebbles, grass
//                     creeping in from the edges.
//   _Mode 3 field  -- ploughed furrows along _Dir with a green tint of shoots.
//   _Mode 4 water  -- mission 1's river: deep and shallow blues, ripples along the flow (_Dir), lighter at the banks.
//   _Mode 5 wheat  -- the body of a standing wheat field (a slab under the ear tiles): golden, darker low down.
Shader "CityBuilder/CineGround"
{
    Properties
    {
        _Mode ("0 grass, 1 cobble, 2 dirt, 3 field, 4 water, 5 wheat", Float) = 0
        _Dir ("Lane direction (xz)", Vector) = (1, 0, 0, 0)
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float4 _Dir;
                float _Seed;
            CBUFFER_END

            // uv: a road ribbon's (metres along, metres across from its middle); zero on other pieces
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Hash2(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }

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
                for (int k = 0; k < 4; k++) { v += a * Noise(p); p = p * 2.07 + 13.7; a *= 0.5; }
                return v;
            }

            // the palette is written in sRGB; the project renders in linear space
            half3 Hex(float r, float g, float b) { return SRGBToLinear(half3(r, g, b) / 255.0); }

            // the islands' grass zones, edges softened over a few centimetres
            half3 Grass(float2 p)
            {
                half3 grass = Hex(0x86, 0xC7, 0x52);
                half3 light = Hex(0xA8, 0xD8, 0x67);
                half3 mid = Hex(0x71, 0xB9, 0x47);
                half3 dry = Hex(0xC4, 0xCE, 0x68);
                half3 alt = Hex(0x7C, 0xC1, 0x4D);
                float e = 0.035;
                half3 c = grass;
                c = lerp(c, mid, smoothstep(0.40 - e, 0.40 + e, Fbm(p / 9.0 + 3.1)));
                c = lerp(c, dry, smoothstep(0.64 - e, 0.64 + e, Fbm(p / 11.0 + 41.0)));
                c = lerp(c, light, smoothstep(0.62 - e, 0.62 + e, Fbm(p / 6.0 + 17.0)));
                c = lerp(c, alt, smoothstep(0.60 - e, 0.60 + e, Fbm(p / 1.6 + 7.0)) * 0.7);
                // tiny flecks of brighter blades so the grass is never a flat colour up close
                c *= 0.95 + 0.1 * Hash(floor(p * 9.0));
                return c;
            }

            half3 Cobble(float2 p)
            {
                float2 q = p / 0.1;
                float row = floor(q.y);
                q.x += Hash(float2(row, _Seed)) * 0.7 + row * 0.5;
                float2 cell = floor(q);
                float2 f = frac(q) - 0.5;
                f += (Hash2(cell) - 0.5) * 0.12;
                float d = max(abs(f.x) * 1.05, abs(f.y) * 1.25);
                float round_ = length(max(abs(f) - float2(0.28, 0.22), 0.0));
                float stone = 1.0 - smoothstep(0.40, 0.47, d + round_ * 0.6);
                float shade = Hash(cell + 3.7);
                half3 setts = lerp(Hex(0xA9, 0xA4, 0x99), Hex(0xC9, 0xC2, 0xB3), shade);
                setts = lerp(setts, Hex(0xB5, 0x9E, 0x83), step(0.86, Hash(cell + 9.1)) * 0.6);   // a warm one here and there
                setts *= 0.92 + 0.12 * (0.5 - f.y);                                            // lit from above, a soft dome
                half3 joint = Hex(0x6E, 0x69, 0x60);
                half3 c = lerp(joint, setts, stone);
                // moss in the joints and wear along the middle
                c = lerp(c, Hex(0x6A, 0x9A, 0x3C), (1.0 - stone) * smoothstep(0.55, 0.75, Fbm(p * 1.3 + 5.0)) * 0.7);
                return c;
            }

            half3 Dirt(float2 p, float2 uv)
            {
                // along and across the road itself (its ribbon's uv), so the ruts follow its bends
                float along = uv.x;
                float across = uv.y + (Fbm(float2(along * 0.15, _Seed)) - 0.5) * 0.25;
                half3 c = lerp(Hex(0xCD, 0xA8, 0x6D), Hex(0xB6, 0x8C, 0x57), Fbm(p * 1.7 + _Seed));
                // two wheel ruts, darker and smoother
                float rut = exp(-pow((abs(across) - 0.3) / 0.08, 2.0));
                c = lerp(c, Hex(0x8F, 0x6A, 0x43), rut * 0.55);
                // a ridge of grass down the middle where wheels don't run
                float middle = exp(-pow(across / 0.1, 2.0)) * smoothstep(0.45, 0.7, Fbm(float2(along * 0.6, across * 3.0) + 11.0));
                c = lerp(c, Hex(0x86, 0xC7, 0x52), middle * 0.6);
                // pebbles
                float2 g = p / 0.08;
                float2 cell = floor(g);
                float2 h = Hash2(cell);
                float peb = step(0.9, h.x) * (1.0 - smoothstep(0.12, 0.2, length(frac(g) - 0.3 - h * 0.4)));
                c = lerp(c, Hex(0xC4, 0xBB, 0xA2), peb);
                return c;
            }

            half3 Field(float2 p)
            {
                float2 dir = normalize(_Dir.xz + 1e-4);
                float2 side = float2(-dir.y, dir.x);
                float across = dot(p, side);
                float furrow = 0.5 + 0.5 * sin(across / 0.22 * 6.2832);
                half3 c = lerp(Hex(0x7A, 0x58, 0x38), Hex(0xA4, 0x7C, 0x50), furrow);
                // young shoots on the ridges, thicker in patches
                float shoots = furrow * smoothstep(0.35, 0.75, Fbm(p * 0.5 + _Seed)) * step(0.45, Hash(floor(p * 14.0)));
                c = lerp(c, Hex(0x8B, 0xB8, 0x4A), shoots * 0.8);
                return c;
            }

            half3 Water(float2 p, float2 uv)
            {
                float along = uv.x;
                float across = uv.y;
                half3 c = lerp(Hex(0x2F, 0x86, 0xC4), Hex(0x4D, 0xB6, 0xE6), Fbm(p * 0.35 + _Seed));
                // ripples running along the flow, broken up
                float rip = sin(along * 3.1 + Fbm(p * 0.8) * 6.0) * sin(across * 1.7 + along * 0.4);
                c = lerp(c, Hex(0xBF, 0xEA, 0xFA), smoothstep(0.82, 0.97, rip) * 0.6);
                // shallower and lighter along the banks
                c = lerp(c, Hex(0x7C, 0xCF, 0xEE), smoothstep(1.6, 2.4, abs(across)) * 0.7);
                return c;
            }

            half3 Wheat(float2 p, float y)
            {
                half3 c = lerp(Hex(0xE2, 0xB8, 0x4E), Hex(0xF4, 0xD4, 0x6C), Fbm(p * 0.9 + 3.0));
                c = lerp(c, Hex(0xF6, 0xD6, 0x72), step(0.8, Hash(floor(p * 18.0))) * 0.6);   // flecks of bright ears
                c *= lerp(0.6, 1.0, saturate(y / 0.27));                                       // shaded low down
                return c;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                half3 albedo = _Mode < 0.5 ? Grass(p) : _Mode < 1.5 ? Cobble(p) : _Mode < 2.5 ? Dirt(p, input.uv) : _Mode < 3.5 ? Field(p)
                    : _Mode < 4.5 ? Water(p, input.uv) : Wheat(p, input.positionWS.y);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                float3 n = normalize(input.normalWS);
                half3 lit = albedo * (light.color * saturate(dot(n, light.direction)) * light.shadowAttenuation + SampleSH(n));
                // fog per pixel: the grass is one quad 800 m across, its corners all deep in the haze
                lit = MixFog(lit, ComputeFogFactor(TransformWorldToHClip(input.positionWS).z));
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex V
            #pragma fragment F
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float4 _Dir;
                float _Seed;
            CBUFFER_END
            float4 V(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half F() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

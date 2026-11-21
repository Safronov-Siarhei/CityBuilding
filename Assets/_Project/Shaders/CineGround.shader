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

            // PerfProbe `cine N` (a measuring switch, 0 in the game): 1 flat colour per mode, 2 no shadow taps,
            // 3 both -- shows on a device what the patterns and the shadows cost
            float _CineProbe;

            // the palette's sRGB colours are written already converted to linear (no per-pixel pow; measured: no gain, kept)
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Hash2(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }

            // four octaves of value noise, baked once into a tiling texture by CineGroundNoise (period 64 cells): one
            // sample instead of ~16 sine hashes. Computed per pixel it was most of the frame on a Mali-G57 (2026-11-18)
            TEXTURE2D(_CineFbm);
            SAMPLER(sampler_CineFbm);
            float Fbm(float2 p)
            {
                return SAMPLE_TEXTURE2D(_CineFbm, sampler_CineFbm, p / 64.0).r;
            }

            // painted tiles (the user approved them 2026-11-21: Design/concepts/campaign_ground, made by
            // Tools/textures/cine_ground_textures.py, set as globals by CineGroundNoise): the setts with their
            // light baked in, and colour multipliers for grass and dirt. They replace the square-celled flecks
            // and setts the user called cheap («пиксели большими, как квадраты»)
            TEXTURE2D(_CineCobble);
            TEXTURE2D(_CineGrassDetail);
            TEXTURE2D(_CineDirtDetail);
            SAMPLER(sampler_CineCobble);
            half3 GrassDetail(float2 p) { return SAMPLE_TEXTURE2D(_CineGrassDetail, sampler_CineCobble, p / 4.0).rgb * 1.4; }

            // the palette is written in sRGB; the project renders in linear space
            half3 Hex(float r, float g, float b) { return SRGBToLinear(half3(r, g, b) / 255.0); }

            // the islands' grass zones, edges softened over a few centimetres
            half3 Grass(float2 p)
            {
                half3 grass = half3(0.23840, 0.57112, 0.08438);
                half3 light = half3(0.39157, 0.68669, 0.13563);
                half3 mid = half3(0.16513, 0.48515, 0.06301);
                half3 dry = half3(0.55201, 0.61721, 0.13843);
                half3 alt = half3(0.20156, 0.53328, 0.07421);
                float e = 0.035;
                half3 c = grass;
                c = lerp(c, mid, smoothstep(0.40 - e, 0.40 + e, Fbm(p / 9.0 + 3.1)));
                c = lerp(c, dry, smoothstep(0.64 - e, 0.64 + e, Fbm(p / 11.0 + 41.0)));
                c = lerp(c, light, smoothstep(0.62 - e, 0.62 + e, Fbm(p / 6.0 + 17.0)));
                c = lerp(c, alt, smoothstep(0.60 - e, 0.60 + e, Fbm(p / 1.6 + 7.0)) * 0.7);
                // tufts and blades from the painted tile (was: 11 cm square flecks)
                c *= GrassDetail(p);
                return c;
            }

            half3 Cobble(float2 p)
            {
                // the painted setts, 6 m a tile; a broad wash of light and dark so the tile never repeats visibly
                half3 c = SAMPLE_TEXTURE2D(_CineCobble, sampler_CineCobble, p / 6.0).rgb;
                return c * (0.92 + 0.16 * Fbm(p * 0.35 + 7.0));
            }

            half3 Dirt(float2 p, float2 uv)
            {
                // along and across the road itself (its ribbon's uv), so the ruts follow its bends
                float along = uv.x;
                float across = uv.y + (Fbm(float2(along * 0.15, _Seed)) - 0.5) * 0.25;
                half3 c = lerp(half3(0.61050, 0.39157, 0.15293), half3(0.46778, 0.26225, 0.09531), Fbm(p * 1.7 + _Seed));
                // two wheel ruts, darker and smoother
                float rut = exp(-pow((abs(across) - 0.3) / 0.08, 2.0));
                c = lerp(c, half3(0.27468, 0.14413, 0.05613), rut * 0.55);
                // a ridge of grass down the middle where wheels don't run
                float middle = exp(-pow(across / 0.1, 2.0)) * smoothstep(0.45, 0.7, Fbm(float2(along * 0.6, across * 3.0) + 11.0));
                c = lerp(c, half3(0.23840, 0.57112, 0.08438), middle * 0.6);
                // lumps and round pebbles from the painted tile (was: hashed 8 cm cells)
                c *= SAMPLE_TEXTURE2D(_CineDirtDetail, sampler_CineCobble, p / 4.0).rgb * 2.0;
                return c;
            }

            half3 Field(float2 p)
            {
                float2 dir = normalize(_Dir.xz + 1e-4);
                float2 side = float2(-dir.y, dir.x);
                float across = dot(p, side);
                float furrow = 0.5 + 0.5 * sin(across / 0.22 * 6.2832);
                half3 c = lerp(half3(0.19462, 0.09759, 0.03955), half3(0.37124, 0.20156, 0.08022), furrow);
                // young shoots on the ridges, thicker in patches
                float shoots = furrow * smoothstep(0.35, 0.75, Fbm(p * 0.5 + _Seed)) * smoothstep(0.98, 1.06, GrassDetail(p).g);
                c = lerp(c, half3(0.25818, 0.47932, 0.06848), shoots * 0.8);
                return c;
            }

            half3 Water(float2 p, float2 uv)
            {
                float along = uv.x;
                float across = uv.y;
                half3 c = lerp(half3(0.02843, 0.23840, 0.55201), half3(0.07421, 0.46778, 0.79130), Fbm(p * 0.35 + _Seed));
                // ripples running along the flow, broken up
                float rip = sin(along * 3.1 + Fbm(p * 0.8) * 6.0) * sin(across * 1.7 + along * 0.4);
                c = lerp(c, half3(0.52100, 0.82279, 0.95597), smoothstep(0.82, 0.97, rip) * 0.6);
                // shallower and lighter along the banks
                c = lerp(c, half3(0.20156, 0.62396, 0.85499), smoothstep(1.6, 2.4, abs(across)) * 0.7);
                return c;
            }

            half3 Wheat(float2 p, float y)
            {
                half3 c = lerp(half3(0.76052, 0.47932, 0.07619), half3(0.90466, 0.65837, 0.14996), Fbm(p * 0.9 + 3.0));
                c = lerp(c, half3(0.92158, 0.67244, 0.16827), smoothstep(1.02, 1.1, GrassDetail(p * 1.6).g) * 0.6);   // flecks of bright ears (round, from the grass tile)
                c *= lerp(0.6, 1.0, saturate(y / 0.27));                                       // shaded low down
                return c;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                half3 albedo;
                if (_CineProbe == 1.0 || _CineProbe == 3.0) albedo = _Mode < 0.5 ? half3(0.23840, 0.57112, 0.08438) : _Mode < 1.5 ? half3(0.39676, 0.37124, 0.31855) : half3(0.61050, 0.39157, 0.15293);
                // REAL branches on the material's mode: HLSL's ?: evaluates BOTH sides, so the old
                // chain of ?: ran all six patterns on every pixel -- 11 ms of the Redmi Pad's GPU in
                // mission 1 (2026-11-21). Never pick a pattern with ?: here.
                else [branch] if (_Mode < 0.5) albedo = Grass(p);
                else [branch] if (_Mode < 1.5) albedo = Cobble(p);
                else [branch] if (_Mode < 2.5) albedo = Dirt(p, input.uv);
                else [branch] if (_Mode < 3.5) albedo = Field(p);
                else [branch] if (_Mode < 4.5) albedo = Water(p, input.uv);
                else albedo = Wheat(p, input.positionWS.y);
                Light light;
                if (_CineProbe >= 2.0) light = GetMainLight();
                else light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
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

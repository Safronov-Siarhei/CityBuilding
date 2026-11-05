// A destroyed building of the player's, left standing as a burnt shell (the user, 2026-11-05: look A,
// «обгоревший остов»; card Design/features/ruins.md). The building's own model, drawn with this
// shader instead of BuildingDetail:
//   * everything above a broken line is gone -- clipped where the world height is over _RuinCut
//     plus a jagged wave (_RuinJag, varied per ruin by _RuinSeed), so the roof and the upper walls
//     are missing and the line is torn, not sawn;
//   * the colours are burnt: greyed and browned by _Char, blackened with soot towards the broken
//     edge and in blotches;
//   * both sides are drawn (Cull Off): with the top gone, the walls' insides show, darker.
// The building meshes are not readable on the device (ModelImportDefaults), so the cut lives here,
// on the GPU, and costs nothing at runtime. One material instance per ruin (BuildingRuin): ruins
// are few. The rubble inside uses an instance with a white _BaseMap and a stone _BaseColor, uncut.
Shader "CityBuilder/BuildingRuin"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _DetailAtlas ("Detail atlas (grey, 0.5 = none)", 2D) = "grey" {}
        _DetailStrength ("Detail strength", Range(0, 1)) = 1
        _ShadeStrength ("Ground darkening strength", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.05
        _SnowMask ("Takes snow in winter", Range(0, 1)) = 1
        _RuinCut ("World height of the break", Float) = 1000
        _RuinJag ("Height of the break's teeth, metres", Float) = 0.15
        _RuinSeed ("Seed of the break's shape", Float) = 0
        _Char ("Burnt", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define DETAIL_GRID 4.0

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _DetailStrength;
            half _ShadeStrength;
            half _Smoothness;
            half _SnowMask;
            float _RuinCut;
            float _RuinJag;
            float _RuinSeed;
            half _Char;
        CBUFFER_END

        half _SeasonSnow;

        TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
        TEXTURE2D(_DetailAtlas);  SAMPLER(sampler_DetailAtlas);

        // the height of the break over this point of the ground: a few waves of different length
        // added up, so the line has big gaps and small teeth
        float BreakHeight(float2 xz)
        {
            float s = _RuinSeed;
            float w = sin(xz.x * 2.3 + s) * 0.45 + sin(xz.y * 2.9 + s * 1.7) * 0.45
                    + sin((xz.x + xz.y) * 7.1 + s * 2.3) * 0.25 + sin((xz.x - xz.y) * 11.3 + s * 0.6) * 0.15;
            return _RuinCut + w * _RuinJag;
        }

        void ClipAboveBreak(float3 positionWS)
        {
            clip(BreakHeight(positionWS.xz) - positionWS.y);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float  pattern    : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                half3  normalWS   : TEXCOORD4;
                half   shade      : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv0 = TRANSFORM_TEX(input.uv0, _BaseMap);
                output.uv1 = input.uv1;
                output.pattern = input.uv2.x > 0.25 ? floor(input.uv2.x) : -1.0;
                output.shade = output.pattern < 0 ? 1.0h : lerp(1.0h, input.color.r, _ShadeStrength);
                return output;
            }

            half3 SampleDetail(float2 uv1, float pattern)
            {
                float col = fmod(pattern, DETAIL_GRID);
                float row = floor(pattern / DETAIL_GRID);
                float2 local = clamp(frac(uv1), 0.002, 0.998);
                float2 atlasUV = float2((col + local.x) / DETAIL_GRID, 1.0 - (row + 1.0 - local.y) / DETAIL_GRID);
                float2 dx = ddx(uv1) / DETAIL_GRID;
                float2 dy = ddy(uv1) / DETAIL_GRID;
                return SAMPLE_TEXTURE2D_GRAD(_DetailAtlas, sampler_DetailAtlas, atlasUV, dx, dy).rgb;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float breakY = BreakHeight(input.positionWS.xz);
                clip(breakY - input.positionWS.y);
                bool front = IS_FRONT_VFACE(face, true, false);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0).rgb * _BaseColor.rgb;
                if (input.pattern >= 0)
                {
                    half3 detail = SampleDetail(input.uv1, input.pattern) * 2.0h;
                    albedo *= lerp(half3(1, 1, 1), detail, _DetailStrength);
                }
                albedo *= input.shade;

                // burnt: the colour drained to a warm grey, darker
                half grey = dot(albedo, half3(0.3h, 0.59h, 0.11h));
                half3 burnt = grey * half3(0.62h, 0.56h, 0.5h);
                albedo = lerp(albedo, burnt, _Char);
                // soot: blotches over the walls, and black towards the broken edge
                float2 p = input.positionWS.xz * 1.7 + input.positionWS.y * 0.9;
                half blotch = (half)saturate(0.5 + 0.5 * sin(p.x * 3.1 + _RuinSeed) * sin(p.y * 2.7 - _RuinSeed * 0.5));
                half nearEdge = (half)saturate(1.0 - (breakY - input.positionWS.y) / 0.45);
                half soot = saturate(blotch * 0.45h + nearEdge * nearEdge * 0.85h) * _Char;
                albedo *= 1.0h - soot * 0.8h;

                half3 normalWS = normalize(input.normalWS);
                // the inside of a wall: turned to face the eye, in the dark of the shell
                if (!front)
                {
                    normalWS = -normalWS;
                    albedo *= 0.45h;
                }

                half up = saturate((normalWS.y - 0.35h) / 0.3h);
                albedo = lerp(albedo, half3(0.93h, 0.96h, 1.0h), up * _SeasonSnow * _SnowMask);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.alpha = 1;
                surface.specular = half3(0, 0, 0);
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;

                return UniversalFragmentBlinnPhong(inputData, surface);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                output.positionWS = positionWS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                ClipAboveBreak(input.positionWS);
                return 0;
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
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                ClipAboveBreak(input.positionWS);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}

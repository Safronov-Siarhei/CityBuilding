// The buildings' one material (Models/Palette/BuildingsPalette.mat): the palette's flat colour,
// times a soft painted pattern, times a darkening towards the ground -- lit exactly the way URP's
// Simple Lit lit the bare palette before, so nothing gets brighter or darker by the change.
//
// What each model carries (Tools/blender/detail.py writes it, export_fbx.py ships it):
//   uv0     -- the palette cell, as before;
//   uv1     -- where on the pattern, in pattern cells (repeats: only its fraction is used);
//   uv2.x   -- which pattern: the cell index in the detail atlas (GRID x GRID cells);
//   colour  -- the ground darkening, grey.
// The atlas is grey and centred on 0.5, so pattern x 2 leaves an untouched pixel untouched.
// A model exported before the detail existed has no uv1/uv2/colour: Unity hands those in as
// zeros, so _NoDetailFallback keeps such a model looking as it did (see the fragment).
//
// Winter (Maps.SeasonStyle): the global _SeasonSnow lays snow on every face that looks up --
// roofs, crowns, yards -- times the material's _SnowMask (0 on the figures' copy, so a soldier's
// helmet stays steel).
//
// [MainColor] _BaseColor is what BuildingPlacer's ghost tints (Material.color).
//
// Wind (the user, 2026-11-02: the map looked lifeless): with _WIND on, the vertices sway -- trees by
// their height over their foot (Trees-<season>.mat: _WindHeight), the map's grass, bushes and flowers by the
// weight MapDecorRenderer writes into uv3.x (DecorWind.mat, _WindByUV3). Gusts roll across the land
// in one direction, each plant flutters in its own time. Buildings never take the keyword.
Shader "CityBuilder/BuildingDetail"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _DetailAtlas ("Detail atlas (grey, 0.5 = none)", 2D) = "grey" {}
        _DetailStrength ("Detail strength", Range(0, 1)) = 1
        _ShadeStrength ("Ground darkening strength", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        // back faces culled in the game; the campaign film's close cameras draw both (CineTwoSided)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        // depth offset: 0 in the game; the film's back-face copies are pushed behind (CineTwoSided)
        _OffsetFactor ("Depth offset factor", Float) = 0
        _OffsetUnits ("Depth offset units", Float) = 0
        _SnowMask ("Takes snow in winter", Range(0, 1)) = 1
        [Toggle(_WIND)] _Wind ("Sways in the wind", Float) = 0
        _WindStrength ("Wind: sway at full weight, metres", Float) = 0.08
        _WindHeight ("Wind: model height of full weight (trees)", Float) = 2
        _WindByUV3 ("Wind: weight from uv3.x (the map's props)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

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
            float _WindStrength;
            float _WindHeight;
            float _WindByUV3;
        CBUFFER_END

        // set by SeasonStyle for the whole scene: 0 outside winter
        half _SeasonSnow;

        TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
        TEXTURE2D(_DetailAtlas);  SAMPLER(sampler_DetailAtlas);

        // How far the wind moves a vertex: gusts rolling across the map one way, a sway, a flutter.
        // `weight` 0 at the root, 1 at the top.
        float3 WindOffset(float3 positionWS, float weight)
        {
            const float2 dir = float2(0.94, 0.34);
            float t = _Time.y;
            float along = dot(positionWS.xz, dir);
            float gust = 0.5 + 0.5 * sin(t * 0.9 - along * 0.12);
            gust *= gust;
            float sway = sin(t * 2.1 - along * 0.35 + positionWS.x * 0.21);
            float flutter = sin(t * 5.3 + positionWS.x * 1.7 + positionWS.z * 1.3) * 0.25;
            float amount = _WindStrength * weight * (0.35 + 0.65 * gust) * (0.6 + 0.4 * sway + flutter);
            return float3(dir.x, 0.0, dir.y) * amount;
        }

        // the vertex's world position, swayed when the material has the wind
        float3 WindedWorld(float3 positionOS, float windUV)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            #if defined(_WIND)
                // the height over the model's own foot, in the world: a tree's root keeps the
                // import's corrective turn, so its model axes need not point up
                float foot = UNITY_MATRIX_M._m13;
                float byHeight = saturate((positionWS.y - foot) / max(_WindHeight, 0.01));
                float weight = _WindByUV3 > 0.5 ? windUV : byHeight * byHeight;
                positionWS += WindOffset(positionWS, weight);
            #endif
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing
            #pragma shader_feature_local_vertex _WIND

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CloudField.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
                float2 uv3        : TEXCOORD3;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = WindedWorld(input.positionOS.xyz, input.uv3.x);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv0 = TRANSFORM_TEX(input.uv0, _BaseMap);
                output.uv1 = input.uv1;
                // pattern index, -1 for "no detail data" (a model from before the detail: its
                // uv2 and colour arrive as zeros, and every real pattern sits at index + 0.5)
                output.pattern = input.uv2.x > 0.25 ? floor(input.uv2.x) : -1.0;
                output.shade = output.pattern < 0 ? 1.0h : lerp(1.0h, input.color.r, _ShadeStrength);
                return output;
            }

            half3 SampleDetail(float2 uv1, float pattern)
            {
                float col = fmod(pattern, DETAIL_GRID);
                float row = floor(pattern / DETAIL_GRID);
                // The pattern repeats inside its cell: frac() wraps it, and the derivatives of the
                // UNWRAPPED coordinate pick the mip, so the wrap does not flash a seam.
                // Kept half a texel inside the cell, so bilinear filtering never borrows the
                // neighbouring pattern's edge.
                float2 local = clamp(frac(uv1), 0.002, 0.998);
                float2 atlasUV = float2((col + local.x) / DETAIL_GRID, 1.0 - (row + 1.0 - local.y) / DETAIL_GRID);
                float2 dx = ddx(uv1) / DETAIL_GRID;
                float2 dy = ddy(uv1) / DETAIL_GRID;
                return SAMPLE_TEXTURE2D_GRAD(_DetailAtlas, sampler_DetailAtlas, atlasUV, dx, dy).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0).rgb * _BaseColor.rgb;
                if (input.pattern >= 0)
                {
                    half3 detail = SampleDetail(input.uv1, input.pattern) * 2.0h;
                    albedo *= lerp(half3(1, 1, 1), detail, _DetailStrength);
                }
                albedo *= input.shade;

                // snow on what faces the sky, the steeper the less; a roof at 45 degrees is white
                half up = saturate((normalize(input.normalWS).y - 0.35h) / 0.3h);
                albedo = lerp(albedo, half3(0.93h, 0.96h, 1.0h), up * _SeasonSnow * _SnowMask);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
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

                half4 lit = UniversalFragmentBlinnPhong(inputData, surface);
                // a passing cloud's shadow (CloudLayer; none below ground or in the menu)
                lit.rgb *= 1.0h - (half)CloudShadow(input.positionWS, _MainLightPosition.xyz);
                return lit;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Cull [_Cull]
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_vertex _WIND

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv3        : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = WindedWorld(input.positionOS.xyz, input.uv3.x);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_vertex _WIND

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv3        : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformWorldToHClip(WindedWorld(input.positionOS.xyz, input.uv3.x));
            }

            half DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}

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
        _SnowMask ("Takes snow in winter", Range(0, 1)) = 1
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
        CBUFFER_END

        // set by SeasonStyle for the whole scene: 0 outside winter
        half _SeasonSnow;

        TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
        TEXTURE2D(_DetailAtlas);  SAMPLER(sampler_DetailAtlas);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 uv2        : TEXCOORD2;
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

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
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
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
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
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}

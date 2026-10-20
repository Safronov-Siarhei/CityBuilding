// The building the player is about to place (BuildingPlacer's ghost, PlacementPreview). Two looks:
//   _Look 0 -- the building in its own colours, a little see-through, with a gold glow along its
//              silhouette; where it cannot stand it greys and darkens and the glow turns wine-red.
//   _Look 1 -- a drawing in ink on parchment: pale paper faces, a fine grid of ink lines over them
//              and an inked silhouette; indigo ink where it can stand, red-brown where it cannot.
// _Valid is 1 where the building can stand, 0 where it cannot (set per renderer by a property block).
//
// Two passes: the first writes only depth, so the second -- the see-through colour -- shows the
// front faces alone and the walls behind them never show through as a muddle of edges.
Shader "CityBuilder/PlacementGhost"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Look ("Look: 0 own colours, 1 drawing", Float) = 0
        _Valid ("1 can stand here, 0 cannot", Float) = 1
        [NoScaleOffset] _BlockMask ("Cells in the way (white), one texel per cell", 2D) = "black" {}
        _BlockRect ("Footprint: world x, z of its corner, 1 / width, 1 / depth", Vector) = (0, 0, 1, 1)
        _UseMask ("1: red only over the cells in the way", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Look;
            half _Valid;
            float4 _BlockRect;
            half _UseMask;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BlockMask); SAMPLER(sampler_point_clamp_BlockMask);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "GhostDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment FragDepth
            half4 FragDepth(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            // How close to a grid line this point is, 1 on the line: lines every half metre along
            // the two axes that run across this face (none along its normal).
            half GridLines(float3 positionWS, float3 normalWS)
            {
                float3 p = positionWS * 2.0;
                float3 gap = abs(frac(p + 0.5) - 0.5);
                float3 width = fwidth(p) * 1.2;
                float3 onLine = 1.0 - smoothstep(0.0, width, gap);
                float3 across = 1.0 - abs(normalWS);
                return max(max(onLine.x * across.x, onLine.y * across.y), onLine.z * across.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light light = GetMainLight();
                half lambert = saturate(dot(normal, light.direction));
                half rim = pow(1.0 - saturate(dot(normal, view)), 3.0);

                // where it can stand: everywhere or nowhere -- or, with the mask, cell by cell (a
                // roof hanging past the footprint takes the state of the cell it hangs from)
                half valid = _Valid;
                if (_UseMask > 0.5)
                {
                    float2 cell = saturate((input.positionWS.xz - _BlockRect.xy) * _BlockRect.zw);
                    valid = 1.0 - SAMPLE_TEXTURE2D(_BlockMask, sampler_point_clamp_BlockMask, cell).r;
                }

                // own colours
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half grey = dot(albedo, half3(0.3, 0.59, 0.11));
                albedo = lerp(lerp(albedo, grey.xxx, 0.7) * 0.7, albedo, valid);
                half3 glow = lerp(half3(0.75, 0.16, 0.12), half3(1.0, 0.78, 0.3), valid);
                half3 own = albedo * (0.6 + 0.6 * lambert * light.color) + glow * rim * 1.8;
                half ownAlpha = lerp(0.7, 0.9, valid) + rim * 0.1;

                // a drawing
                half3 ink = lerp(half3(0.62, 0.17, 0.1), half3(0.16, 0.27, 0.55), valid);
                half3 paper = lerp(half3(1.0, 0.8, 0.74), half3(0.98, 0.95, 0.86), valid) * (0.78 + 0.22 * lambert);
                half stroke = max(GridLines(input.positionWS, normal) * 0.85, saturate(rim * 2.2));
                half3 drawn = lerp(paper, ink, stroke);
                half drawnAlpha = 0.5 + 0.45 * stroke;

                return half4(lerp(own, drawn, _Look), saturate(lerp(ownAlpha, drawnAlpha, _Look)));
            }
            ENDHLSL
        }
    }
}

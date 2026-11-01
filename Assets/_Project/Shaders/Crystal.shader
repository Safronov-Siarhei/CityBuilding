// The upgrade crystal's body (Dungeon/DungeonCrystals; the user, 2026-11-01: «яркие, светящиеся,
// сочные ... особенная вещь, платный контент»). It lights itself -- bright in a dark passage, with
// no bloom and no real light: a violet-to-cyan gradient up the gem, the facets shaded by the main
// light, a white rim, a band of light running up it and a facet now and then flashing. Opaque, one
// pass, nothing per frame from a script. The height is taken in world space from the object's
// origin (the gem's foot), so the FBX's own axes and scale do not matter.
Shader "CityBuilder/Crystal"
{
    Properties
    {
        _BottomColor ("Bottom", Color) = (0.55, 0.2, 1, 1)
        _TopColor ("Top", Color) = (0.25, 0.95, 1, 1)
        _RimColor ("Rim", Color) = (0.85, 1, 1, 1)
        _Height ("Gem height (cells)", Float) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                half4 _TopColor;
                half4 _RimColor;
                float _Height;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float height : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                output.height = saturate((output.positionWS.y - origin.y) / max(0.01, _Height));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                float h = input.height;
                half3 colour = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.0, 1.0, h));
                // facets: lit by the sun, never dark -- it glows from inside
                float facet = saturate(dot(n, GetMainLight().direction));
                colour *= 0.85 + 0.55 * facet;
                // a white rim round the outline
                float rim = pow(1.0 - saturate(dot(n, v)), 2.5);
                colour += _RimColor.rgb * rim * 0.9;
                // a band of light running up the gem
                float band = frac(h * 1.2 - _Time.y * 0.45);
                colour += _RimColor.rgb * smoothstep(0.86, 0.97, band) * (1.0 - smoothstep(0.97, 1.0, band)) * 0.7;
                // now and then a facet flashes
                float seed = dot(round(n * 4.0), float3(12.9898, 78.233, 37.719));
                float flash = pow(saturate(sin(_Time.y * 2.3 + seed * 7.0)), 24.0);
                colour += _RimColor.rgb * flash * 0.8;
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}

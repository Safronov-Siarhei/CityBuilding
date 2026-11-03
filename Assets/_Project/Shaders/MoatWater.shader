// The Moat's water (the user, 2026-11-03: "just like the sea" -- by the bank a light band like the
// sea's shore, the rest plain dark still water, no white blobs). Each cell's quad carries in UV1.x
// which of its sides are banks (MoatAppearance: bit 0 N, 1 E, 2 S, 3 W); along those a white foam
// line at the stone lip, then the shallows' turquoise fading into the deep. The sea's own palette
// colours (TerrainPalette: Foam, WaterShallow, WaterDeep). GPU only, one quad a cell.
Shader "CityBuilder/MoatWater"
{
    Properties
    {
        _Deep ("Deep", Color) = (0.13, 0.42, 0.66, 1)
        _Shallow ("Shallow", Color) = (0.447, 0.824, 0.871, 1)
        _Foam ("Foam", Color) = (0.93, 0.98, 1, 1)
        _Lip ("Bank lip (cell)", Float) = 0.12
        _FoamWidth ("Foam width (cell)", Float) = 0.045
        _Band ("Shallow band (cell)", Float) = 0.2
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep;
                half4 _Shallow;
                half4 _Foam;
                float _Lip;
                float _FoamWidth;
                float _Band;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 mask : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation float mask : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.mask = input.mask.x;
                return output;
            }

            float Bit(float mask, float bit)
            {
                return fmod(floor((mask + 0.5) / bit), 2.0) > 0.5 ? 1.0 : 0.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // the distance to the nearest bank (cell units); a side with moat beside it is never near
                float far = 10.0;
                float d = far;
                d = min(d, lerp(far, 1.0 - input.uv.y, Bit(input.mask, 1.0)));
                d = min(d, lerp(far, 1.0 - input.uv.x, Bit(input.mask, 2.0)));
                d = min(d, lerp(far, input.uv.y, Bit(input.mask, 4.0)));
                d = min(d, lerp(far, input.uv.x, Bit(input.mask, 8.0)));

                float out_ = d - _Lip;
                half3 colour = lerp(_Shallow.rgb, _Deep.rgb, smoothstep(0.0, _Band, out_));
                float foam = 1.0 - smoothstep(_FoamWidth * 0.6, _FoamWidth, abs(out_));
                colour = lerp(colour, _Foam.rgb, foam);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}

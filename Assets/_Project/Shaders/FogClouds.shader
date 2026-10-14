// The fog of war as a layer of cloud lying over the land the player has not explored.
//
// Drawn on a blanket mesh that follows the land a few metres above it (FogOfWarManager builds it),
// transparent, unlit, no shadows. What it shows comes from two textures:
//
// - _MaskTex, one texel per grid cell: 1 where the land is revealed, 0 where it is not, eased in
//   between over a second by FogOfWarManager so new land clears gradually. It is sampled where the
//   view ray through this pixel meets the GROUND, not where it meets the blanket: the blanket hangs
//   _Clearance metres up, and sampling at its own position would slide every hole in the clouds
//   towards the camera by that much at this pitch -- the floating-island look the old flat lid had.
//   Five taps soften the 1 m cell staircase into a smooth edge.
//
// - _NoiseTex, seamless: red = broad billows, green = finer puffs. Two scrolling samples push the
//   cloud's edge in and out (so it is lumpy, not a circle) and shade its body.
Shader "CityBuilder/FogClouds"
{
    Properties
    {
        _MaskTex ("Reveal mask (R: 1 = revealed)", 2D) = "black" {}
        _NoiseTex ("Cloud noise (R billows, G puffs)", 2D) = "gray" {}
        _CloudColor ("Cloud", Color) = (0.98, 0.98, 1.0, 1)
        _ShadeColor ("Cloud shade", Color) = (0.74, 0.79, 0.89, 1)
        _EdgeColor ("Cloud edge", Color) = (0.78, 0.82, 0.92, 1)
        _MapRect ("Map rect (x0, z0, 1/width, 1/depth)", Vector) = (0, 0, 0.005, 0.005)
        _MaskStep ("Mask blur step (uv)", Vector) = (0.0075, 0.0075, 0, 0)
        _Clearance ("Blanket height above the land", Float) = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Clouds"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskTex_ST;
                float4 _NoiseTex_ST;
                half4 _CloudColor;
                half4 _ShadeColor;
                half4 _EdgeColor;
                float4 _MapRect;
                float4 _MaskStep;
                float _Clearance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half Revealed(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Down the view ray to the land under this pixel. An orthographic camera (the
                // minimap) looks straight along its forward axis everywhere.
                float3 view = IsPerspectiveProjection()
                    ? normalize(input.positionWS - _WorldSpaceCameraPos)
                    : GetViewForwardDir();
                float drop = _Clearance / max(-view.y, 0.25);
                float2 ground = input.positionWS.xz + view.xz * drop;

                float2 uv = (ground - _MapRect.xy) * _MapRect.zw;
                float2 s = _MaskStep.xy;
                half revealed = (Revealed(uv) * 2.0
                    + Revealed(uv + float2(s.x, s.y)) + Revealed(uv + float2(-s.x, s.y))
                    + Revealed(uv + float2(s.x, -s.y)) + Revealed(uv + float2(-s.x, -s.y))) / 6.0;

                // Beyond the map's edge there is nothing to reveal: cloud all the way out.
                float2 outside = max(-uv, uv - 1.0);
                revealed *= step(max(outside.x, outside.y), 0.0);

                // Noise in the cloud's own world position, so it stays put on the land as the
                // camera moves, and drifts slowly on its own.
                float2 w = input.positionWS.xz;
                float2 billowUv = w * 0.021 + _Time.y * float2(0.0030, 0.0012);
                half billow = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, billowUv).r;
                half puff = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, w * 0.055 + _Time.y * float2(-0.0050, 0.0035)).g;
                half n = billow * 0.7 + puff * 0.3;

                // Cover is where the fog is. The noise pushes the edge in and out by several
                // metres, and the ramp is wide enough that the cloud thins out rather than ending
                // on a cut line.
                half cover = 1.0 - revealed;
                half density = saturate((cover + (n - 0.5) * 1.1 - 0.30) * 2.6);

                // Volume: the billows are lit from one side. Where the noise rises towards the
                // light the cloud is bright, where it falls away it is in its own shade.
                half towardsLight = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, billowUv + float2(0.012, 0.018)).r;
                half lit = saturate(0.55 + (billow - towardsLight) * 3.5 + (puff - 0.5) * 0.2);
                half3 colour = lerp(_ShadeColor.rgb, _CloudColor.rgb, lit);
                // Thin cloud at the edge shows its blue-grey underside.
                colour = lerp(_EdgeColor.rgb, colour, saturate(density * 1.6 - 0.15));

                // The cloud's shadow on the explored land beside it: the mask read a few metres
                // towards the light, darkening ground the cloud is not itself covering.
                half shadowCover = 1.0 - Revealed(uv + _MaskStep.xy * float2(-1.6, -2.4));
                half shadow = saturate((shadowCover + (n - 0.5) * 0.8 - 0.35) * 2.0) * 0.22 * (1.0 - density);

                half alpha = saturate(density * 1.15);
                half total = alpha + shadow;
                half3 mixed = (colour * alpha + half3(0.10, 0.13, 0.22) * shadow) / max(total, 1e-3);
                return half4(mixed, saturate(total) * _CloudColor.a);
            }
            ENDHLSL
        }
    }
}


Shader "Custom/TrafficLight_Toon_URP"
{
    Properties
    {
        [MainTexture] _BaseMap ("Sample / Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)

        _EmissionMap ("Emit Texture", 2D) = "black" {}
        _EmissionTint ("Emit Tint", Color) = (1,1,1,1)
        _EmissionIntensity ("Emit Intensity", Range(0,10)) = 3

        [Enum(Off,0,Red,1,Yellow,2,Green,3,All,4)]
        _TrafficState ("Traffic Light State", Float) = 0

        _ToonThreshold1 ("Toon Shadow Threshold", Range(0,1)) = 0.25
        _ToonThreshold2 ("Toon Light Threshold", Range(0,1)) = 0.70

        _ShadowTint ("Shadow Tint", Color) = (0.45,0.45,0.5,1)
        _MidTint ("Middle Tint", Color) = (0.8,0.8,0.8,1)
        _LightTint ("Light Tint", Color) = (1,1,1,1)

        _MaskSoftness ("Emit Mask Softness", Range(0.01,0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;

                half4 _BaseColor;
                half4 _EmissionTint;

                half _EmissionIntensity;
                half _TrafficState;

                half _ToonThreshold1;
                half _ToonThreshold2;

                half4 _ShadowTint;
                half4 _MidTint;
                half4 _LightTint;

                half _MaskSoftness;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uvBase : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float2 uvEmit : TEXCOORD4;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;

                OUT.uvBase = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvEmit = TRANSFORM_TEX(IN.uv, _EmissionMap);

                OUT.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            // RED MASK
            // Loại bỏ màu vàng/cam khi nhận diện màu đỏ.
            half GetRedMask(half3 color)
            {
                half difference =
                    color.r - max(color.g, color.b);

                half greenRatio =
                    color.g / max(color.r, 0.001h);

                half redDominance =
                    smoothstep(
                        _MaskSoftness,
                        _MaskSoftness * 2.5h,
                        difference
                    );

                half notYellowOrOrange =
                    1.0h - smoothstep(
                        0.28h,
                        0.45h,
                        greenRatio
                    );

                half redBrightness =
                    smoothstep(0.15h, 0.40h, color.r);

                return redDominance *
                       notYellowOrOrange *
                       redBrightness;
            }

            // YELLOW MASK
            half GetYellowMask(half3 color)
            {
                half difference =
                    min(color.r, color.g) - color.b;

                half bothBright =
                    min(color.r, color.g);

                return
                    smoothstep(
                        _MaskSoftness,
                        _MaskSoftness * 2.5h,
                        difference
                    )
                    *
                    smoothstep(0.15h, 0.45h, bothBright);
            }

            // GREEN MASK
            half GetGreenMask(half3 color)
            {
                half difference =
                    color.g - max(color.r, color.b);

                return smoothstep(
                    _MaskSoftness,
                    _MaskSoftness * 2.5h,
                    difference
                );
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // BASE TEXTURE
                half4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uvBase
                    ) * _BaseColor;

                half3 normalWS = normalize(IN.normalWS);

                // TOON LIGHTING
                float4 shadowCoord =
                    TransformWorldToShadowCoord(IN.positionWS);

                Light mainLight = GetMainLight(shadowCoord);

                half NdotL =
                    saturate(dot(normalWS, mainLight.direction));

                half lightAmount =
                    NdotL * mainLight.shadowAttenuation;

                half3 toonTint = _ShadowTint.rgb;

                toonTint = lerp(
                    toonTint,
                    _MidTint.rgb,
                    step(_ToonThreshold1, lightAmount)
                );

                toonTint = lerp(
                    toonTint,
                    _LightTint.rgb,
                    step(_ToonThreshold2, lightAmount)
                );

                half3 ambient = SampleSH(normalWS);

                half3 directLight =
                    mainLight.color * toonTint;

                half3 surfaceColor =
                    baseSample.rgb * (ambient + directLight);

                // EMIT TEXTURE
                half3 emitSample =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        IN.uvEmit
                    ).rgb;

                half redMask = GetRedMask(emitSample);
                half yellowMask = GetYellowMask(emitSample);
                half greenMask = GetGreenMask(emitSample);

                // TRAFFIC LIGHT STATE
                half redEnabled =
                    step(0.5h, _TrafficState) *
                    (1.0h - step(1.5h, _TrafficState));

                half yellowEnabled =
                    step(1.5h, _TrafficState) *
                    (1.0h - step(2.5h, _TrafficState));

                half greenEnabled =
                    step(2.5h, _TrafficState) *
                    (1.0h - step(3.5h, _TrafficState));

                half allEnabled =
                    step(3.5h, _TrafficState);

                half emissionMask =
                    redMask * (redEnabled + allEnabled)
                    +
                    yellowMask * (yellowEnabled + allEnabled)
                    +
                    greenMask * (greenEnabled + allEnabled);

                // EMISSION
                half3 emission =
                    emitSample *
                    emissionMask *
                    _EmissionTint.rgb *
                    _EmissionIntensity;

                half3 finalColor =
                    surfaceColor + emission;

                finalColor =
                    MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, baseSample.a);
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack Off
}

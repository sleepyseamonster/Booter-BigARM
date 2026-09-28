Shader "BooterBigArm/TopDown3D/Formation Contact Sand"
{
    Properties
    {
        [MainTexture] _BaseMap("Sand Surface", 2D) = "white" {}
        _SandBorderColor("Sand Border Tint", Color) = (0.88, 0.72, 0.62, 1)
        _MetersPerTile("Meters Per Tile", Float) = 3.5
        _Waviness("Waviness", Range(0, 1)) = 0.38
        _NoiseScale("Noise Scale", Range(0.2, 8)) = 2.2
        _DirectionalBuildup("Directional Buildup", Range(0, 1)) = 0.22
        _WindDirection("Wind Direction", Vector) = (0.85, 0.53, 0, 0)
        _BorderWidth("Border Width", Float) = 0.42
        _BaseFeatherDistance("Base Feather Distance", Range(0, 0.5)) = 0.08
        _TopFeatherDistance("Top Feather Distance", Range(0, 0.5)) = 0.06
        _Smoothness("Smoothness", Range(0, 1)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
        }
        LOD 200

        Pass
        {
            Name "VisualContactSand"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ContactVertex
            #pragma fragment ContactFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _SandBorderColor;
                float _MetersPerTile;
                float _Waviness;
                float _NoiseScale;
                float _DirectionalBuildup;
                float4 _WindDirection;
                float _BorderWidth;
                float _BaseFeatherDistance;
                float _TopFeatherDistance;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 localCoordinate : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 contactData : TEXCOORD2;
                float2 localCoordinate : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 local = frac(position);
                local = local * local * (3.0 - 2.0 * local);
                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), local.x);
                float top = lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), local.x);
                return lerp(bottom, top, local.y);
            }

            float FractalNoise(float2 position)
            {
                float value = ValueNoise(position) * 0.62;
                value += ValueNoise(position * 2.07 + 19.1) * 0.27;
                value += ValueNoise(position * 4.13 - 7.4) * 0.11;
                return value;
            }

            Varyings ContactVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.contactData = input.color;
                output.localCoordinate = input.localCoordinate;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 ContactFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                float phase = input.contactData.y * 37.0;
                float noise = FractalNoise(absolutePositionWS.xz * _NoiseScale + phase) - 0.5;
                float2 outward = normalize(input.localCoordinate + float2(0.0001, 0.0001));
                float2 wind = normalize(_WindDirection.xy + float2(0.0001, 0.0001));
                float directional = dot(outward, wind);
                float displacedDistance = input.contactData.x
                    - noise * _Waviness * 0.48
                    - directional * _DirectionalBuildup * 0.22;
                half band = 1.0h - smoothstep(0.04h, 1.0h, (half)displacedDistance);
                half shapedLip = saturate(input.contactData.z * 1.35h);
                half coverage = band * shapedLip;
                float featherNormalized = saturate(
                    _BaseFeatherDistance / max(_BorderWidth, 0.01));
                half baseFade = _BaseFeatherDistance <= 0.0001
                    ? 1.0h
                    : (half)(1.0 - smoothstep(
                        1.0 - featherNormalized,
                        1.0,
                        saturate(displacedDistance)));
                float topFeatherNormalized = saturate(
                    _TopFeatherDistance / max(_BorderWidth, 0.01));
                half topFade = _TopFeatherDistance <= 0.0001
                    ? (half)smoothstep(-1.0h, -0.82h, input.contactData.a)
                    : (half)smoothstep(
                        -max(topFeatherNormalized, 0.0001),
                        0.0,
                        displacedDistance);
                half alpha = saturate(coverage * baseFade * topFade);
                clip(alpha - 0.008h);

                float meters = max(_MetersPerTile, 0.01);
                float2 primaryUv = absolutePositionWS.xz / meters;
                const float2x2 rotation = float2x2(0.8660254, -0.5, 0.5, 0.8660254);
                float2 alternateUv = mul(rotation, primaryUv * 0.91) + float2(17.3, 29.1);
                half textureBlend = (half)ValueNoise(absolutePositionWS.xz * 0.04 + phase);
                half3 primary = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, primaryUv).rgb;
                half3 alternate = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, alternateUv).rgb;
                half3 sandAlbedo = lerp(primary, alternate, textureBlend) * _SandBorderColor.rgb;
                half innerBank = 1.0h - smoothstep(0.0h, 0.38h, (half)displacedDistance);
                sandAlbedo *= lerp(0.92h, 1.06h, innerBank);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = sandAlbedo;
                surfaceData.specular = half3(0.08, 0.08, 0.08);
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = alpha;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = 0.0;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.a = alpha;
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

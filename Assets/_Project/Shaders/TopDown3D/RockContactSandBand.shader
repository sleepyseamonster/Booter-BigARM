Shader "BooterBigArm/TopDown3D/Rock Contact Sand Band"
{
    Properties
    {
        [MainTexture] _BaseMap("Rock Base Color", 2D) = "white" {}
        [Normal] _NormalMap("Rock Normal", 2D) = "bump" {}
        _BandColor("Sand Tint", Color) = (0.82, 0.54, 0.34, 1)
        _GroundHeight("Ground Height", Float) = 0
        _BandHeight("Band Height", Float) = 0.48
        _Opacity("Opacity", Range(0, 1)) = 0.72
        _FeatherHeight("Feather Height", Float) = 0.14
        _Waviness("Waviness", Range(0, 1)) = 0.38
        _NoiseScale("Noise Scale", Float) = 2.2
        _DirectionalBuildup("Directional Buildup", Range(0, 1)) = 0.22
        _WindDirection("Wind Direction", Vector) = (0.85, 0.53, 0, 0)
        _RockCenter("Rock Center", Vector) = (0, 0, 0, 0)
        _Phase("Stable Phase", Float) = 0
        _RockMetersPerTile("Rock Meters Per Tile", Float) = 1.1
        _TriplanarSharpness("Triplanar Sharpness", Range(1, 12)) = 4
        _NormalStrength("Normal Strength", Range(0, 2)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
        }

        Pass
        {
            Name "RockContactSandBand"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BandVertex
            #pragma fragment BandFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BandColor;
                float _GroundHeight;
                float _BandHeight;
                float _Opacity;
                float _FeatherHeight;
                float _Waviness;
                float _NoiseScale;
                float _DirectionalBuildup;
                float4 _WindDirection;
                float4 _RockCenter;
                float _Phase;
                float _RockMetersPerTile;
                float _TriplanarSharpness;
                float _NormalStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32 + _Phase);
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

            void SampleRockSurface(
                float3 positionWS,
                half3 geometricNormalWS,
                out half3 albedo,
                out half3 normalWS)
            {
                float meters = max(_RockMetersPerTile, 0.01);
                float3 projectionPosition = positionWS / meters;
                half2 weights = pow(abs(geometricNormalWS.xz), max((half)_TriplanarSharpness, 1.0h));
                weights = max(weights, half2(0.0001h, 0.0001h));
                weights /= max(weights.x + weights.y, 0.001h);
                float2 uvX = projectionPosition.zy;
                float2 uvZ = projectionPosition.xy;

                half3 albedoX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 albedoZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                albedo = albedoX * weights.x + albedoZ * weights.y;

                half3 normalX = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX), _NormalStrength);
                half3 normalZ = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ), _NormalStrength);
                normalX = half3(normalX.xy + geometricNormalWS.zy, abs(normalX.z) * geometricNormalWS.x);
                normalZ = half3(normalZ.xy + geometricNormalWS.xy, abs(normalZ.z) * geometricNormalWS.z);
                normalWS = normalize(normalX.zyx * weights.x + normalZ.xyz * weights.y);
            }

            Varyings BandVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 BandFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                float heightAboveGround = absolutePositionWS.y - _GroundHeight;
                float2 outward = normalize(
                    absolutePositionWS.xz - _RockCenter.xy + float2(0.0001, 0.0001));
                float2 wind = normalize(_WindDirection.xy + float2(0.0001, 0.0001));
                float noise = ValueNoise(absolutePositionWS.xz * max(_NoiseScale, 0.01)) - 0.5;
                float directional = dot(outward, wind) * _DirectionalBuildup;
                float effectiveHeight = max(_BandHeight, 0.01)
                    * (1.0 + noise * _Waviness * 0.72 + directional * 0.38);
                float featherHeight = clamp(_FeatherHeight, 0.005, effectiveHeight);
                half band = (half)(1.0 - smoothstep(
                    effectiveHeight - featherHeight,
                    effectiveHeight,
                    heightAboveGround));
                half alpha = band * saturate((half)_Opacity);
                clip(alpha - 0.008h);

                half3 rockAlbedo;
                half3 sampledNormalWS;
                SampleRockSurface(absolutePositionWS, normalize(input.normalWS), rockAlbedo, sampledNormalWS);
                half3 normalWS = NormalizeNormalPerPixel(sampledNormalWS);
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = rockAlbedo * _BandColor.rgb;
                surfaceData.specular = half3(0.03h, 0.03h, 0.03h);
                surfaceData.metallic = 0.0h;
                surfaceData.smoothness = 0.035h;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.emission = 0.0h;
                surfaceData.occlusion = 0.92h;
                surfaceData.alpha = alpha;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = 0.0h;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

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

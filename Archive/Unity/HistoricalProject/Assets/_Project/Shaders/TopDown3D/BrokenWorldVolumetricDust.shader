Shader "BooterBigArm/TopDown3D/Broken World Volumetric Dust"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #pragma target 4.5
        #define USE_FULL_PRECISION_BLIT_TEXTURE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        TEXTURE2D(_DustDensityMap);
        SAMPLER(sampler_DustDensityMap);
        TEXTURE2D_X(_DustScatteringTexture);

        float4 _DustDensityMapParams;
        half4 _DustTint;
        float4 _DustOptics;
        float _DustMaximumForwardPhase;
        float4 _DustMarch;
        float4 _DustScatteringTexelSize;

        float SampleDustDensity(float3 positionWS)
        {
            float2 densityUv =
                (positionWS.xz - _DustDensityMapParams.xy) * _DustDensityMapParams.zw;
            return SAMPLE_TEXTURE2D_LOD(
                _DustDensityMap,
                sampler_DustDensityMap,
                saturate(densityUv),
                0).r;
        }

        float NormalizedHenyeyGreenstein(float cosine, float anisotropy)
        {
            float g = clamp(anisotropy, -0.9, 0.9);
            float gSquared = g * g;
            float denominator = pow(max(0.0001, 1.0 + gSquared - (2.0 * g * cosine)), 1.5);
            return (1.0 - gSquared) / denominator;
        }

        float VisibilitySafePhase(float cosine, float anisotropy, float maximumForwardPhase)
        {
            float rawPhase = NormalizedHenyeyGreenstein(cosine, anisotropy);
            if (rawPhase <= 1.0)
            {
                return rawPhase;
            }

            float forwardRange = max(0.0, maximumForwardPhase - 1.0);
            if (forwardRange <= 0.0)
            {
                return 1.0;
            }

            float forwardExcess = rawPhase - 1.0;
            return 1.0 + ((forwardExcess * forwardRange) / (forwardExcess + forwardRange));
        }

        float DeviceDepthFromRaw(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth;
        #else
            return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
        #endif
        }

        bool IsSkyDepth(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth <= 0.00001;
        #else
            return rawDepth >= 0.99999;
        #endif
        }

        half4 RaymarchDust(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float rawDepth = SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture,
                sampler_PointClamp,
                uv,
                0).r;
            float3 endpointWS = ComputeWorldSpacePosition(
                uv,
                DeviceDepthFromRaw(rawDepth),
                UNITY_MATRIX_I_VP);
            float3 cameraToEndpoint = endpointWS - _WorldSpaceCameraPos;
            float endpointDistance = max(0.001, length(cameraToEndpoint));
            float3 rayDirection = cameraToEndpoint / endpointDistance;
            float marchDistance = IsSkyDepth(rawDepth)
                ? _DustMarch.x
                : min(endpointDistance, _DustMarch.x);

            int stepCount = clamp((int)_DustMarch.y, 4, 32);
            int shadowStride = max(1, (int)_DustMarch.z);
            float stepLength = marchDistance / stepCount;
            float jitter = InterleavedGradientNoise(input.positionCS.xy, 0);
            float sampleDistance = (0.35 + (jitter * 0.65)) * stepLength;
            float transmittance = 1.0;
            float3 scattering = 0.0;
            half shadowAttenuation = 1.0h;
            Light mainLight = GetMainLight();
            half3 ambientLight = SampleSH(half3(0.0h, 1.0h, 0.0h));
            float phase = VisibilitySafePhase(
                clamp(dot(rayDirection, mainLight.direction), -1.0, 1.0),
                _DustOptics.z,
                _DustMaximumForwardPhase);

            [loop]
            for (int stepIndex = 0; stepIndex < 32; stepIndex++)
            {
                if (stepIndex >= stepCount || transmittance <= 0.01)
                {
                    break;
                }

                float3 samplePositionWS = _WorldSpaceCameraPos + (rayDirection * sampleDistance);
                if ((stepIndex % shadowStride) == 0)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(samplePositionWS);
                    shadowAttenuation = GetMainLight(shadowCoord).shadowAttenuation;
                }

                float density = max(0.0, SampleDustDensity(samplePositionWS));
                float stepTransmittance = exp(-_DustOptics.x * density * stepLength);
                half3 directScattering =
                    mainLight.color * shadowAttenuation * phase;
                half3 ambientScattering = ambientLight * _DustOptics.w;
                half3 lighting = _DustTint.rgb * (directScattering + ambientScattering);
                scattering +=
                    transmittance
                    * (1.0 - stepTransmittance)
                    * _DustOptics.y
                    * lighting;
                transmittance *= stepTransmittance;
                sampleDistance += stepLength;
            }

            return half4(scattering, transmittance);
        }

        float LinearDepthAt(float2 uv)
        {
            return LinearEyeDepth(SampleSceneDepth(saturate(uv)), _ZBufferParams);
        }

        void AccumulateBilateralSample(
            float2 sampleUv,
            float spatialWeight,
            float centerDepth,
            inout float4 weightedScattering,
            inout float totalWeight)
        {
            sampleUv = saturate(sampleUv);
            float sampleDepth = LinearDepthAt(sampleUv);
            float relativeDepthDifference =
                abs(sampleDepth - centerDepth) / max(1.0, centerDepth);
            float depthWeight = exp2(-relativeDepthDifference * _DustMarch.w);
            float weight = max(0.00001, spatialWeight * depthWeight);
            weightedScattering += SAMPLE_TEXTURE2D_X_LOD(
                _DustScatteringTexture,
                sampler_LinearClamp,
                sampleUv,
                0) * weight;
            totalWeight += weight;
        }

        half4 CompositeDust(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            half4 sceneColor = SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture,
                sampler_LinearClamp,
                uv,
                0);

            float2 lowResolutionPosition =
                (uv * _DustScatteringTexelSize.zw) - 0.5;
            float2 fractionWithinCell = frac(lowResolutionPosition);
            float2 baseUv =
                (floor(lowResolutionPosition) + 0.5) * _DustScatteringTexelSize.xy;
            float centerDepth = LinearDepthAt(uv);
            float4 weightedScattering = 0.0;
            float totalWeight = 0.0;

            AccumulateBilateralSample(
                baseUv,
                (1.0 - fractionWithinCell.x) * (1.0 - fractionWithinCell.y),
                centerDepth,
                weightedScattering,
                totalWeight);
            AccumulateBilateralSample(
                baseUv + float2(_DustScatteringTexelSize.x, 0.0),
                fractionWithinCell.x * (1.0 - fractionWithinCell.y),
                centerDepth,
                weightedScattering,
                totalWeight);
            AccumulateBilateralSample(
                baseUv + float2(0.0, _DustScatteringTexelSize.y),
                (1.0 - fractionWithinCell.x) * fractionWithinCell.y,
                centerDepth,
                weightedScattering,
                totalWeight);
            AccumulateBilateralSample(
                baseUv + _DustScatteringTexelSize.xy,
                fractionWithinCell.x * fractionWithinCell.y,
                centerDepth,
                weightedScattering,
                totalWeight);

            float4 dust = weightedScattering / max(0.00001, totalWeight);
            return half4((sceneColor.rgb * dust.a) + dust.rgb, sceneColor.a);
        }
        ENDHLSL

        Pass
        {
            Name "VolumetricDustRaymarch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment RaymarchDust
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }

        Pass
        {
            Name "VolumetricDustComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeDust
            ENDHLSL
        }
    }
}

Shader "BooterBigArm/TopDown3D/Broken World Round Lens Flare"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_RoundFlarePrimarySprite);
            SAMPLER(sampler_RoundFlarePrimarySprite);
            TEXTURE2D(_RoundFlareGhostRingSprite);
            SAMPLER(sampler_RoundFlareGhostRingSprite);
            TEXTURE2D(_RoundFlareApertureGhostSprite);
            SAMPLER(sampler_RoundFlareApertureGhostSprite);
            TEXTURE2D_X(_RoundFlareOcclusionHistory);

            float4 _RoundFlareSource;
            float4 _RoundFlareParams;
            float4 _RoundFlareOptics;
            float4 _RoundFlareEnergy;
            float4 _RoundFlareOcclusion;
            float4 _RoundFlareTemporal;
            half4 _RoundFlareTint;

            half4 SampleSource(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    saturate(uv),
                    0);
            }

            float EllipseDistance(float2 uv, float2 center, float radius, float anisotropy, float aspect)
            {
                float2 delta = uv - center;
                delta.x *= aspect / max(0.001, anisotropy);
                return length(delta) / max(0.0001, radius);
            }

            half SoftDisc(float normalizedRadius, float falloff)
            {
                return exp2(-normalizedRadius * normalizedRadius * falloff);
            }

            half SoftRing(float normalizedRadius, float ringRadius, float thickness)
            {
                float ringDistance = abs(normalizedRadius - ringRadius);
                return 1.0h - smoothstep(0.0, max(0.001, thickness), ringDistance);
            }

            half OffscreenVisibility(float2 source, float edgeReach)
            {
                float2 outside = max(max(-source, source - 1.0), 0.0);
                float outsideDistance = length(outside);
                return 1.0h - smoothstep(0.0, max(0.0001, edgeReach), outsideDistance);
            }

            half DepthVisibility(float2 source)
            {
                if (any(source < 0.0) || any(source > 1.0))
                {
                    return 1.0h;
                }

                float rawDepth = SampleSceneDepth(saturate(source));
                float linearDepth = Linear01Depth(rawDepth, _ZBufferParams);
                return smoothstep(0.985, 0.9995, linearDepth);
            }

            half SoftSourceOcclusion(float2 source, float radius, float aspect)
            {
                float2 horizontal = float2(radius / max(0.01, aspect), 0.0);
                float2 vertical = float2(0.0, radius);
                float2 diagonalA = float2(horizontal.x, vertical.y) * 0.70710678;
                float2 diagonalB = float2(horizontal.x, -vertical.y) * 0.70710678;
                float2 innerHorizontal = horizontal * 0.45;
                float2 innerVertical = vertical * 0.45;

                half visibility = DepthVisibility(source) * 0.16h;
                visibility += DepthVisibility(source + innerHorizontal) * 0.08h;
                visibility += DepthVisibility(source - innerHorizontal) * 0.08h;
                visibility += DepthVisibility(source + innerVertical) * 0.08h;
                visibility += DepthVisibility(source - innerVertical) * 0.08h;
                visibility += DepthVisibility(source + horizontal) * 0.065h;
                visibility += DepthVisibility(source - horizontal) * 0.065h;
                visibility += DepthVisibility(source + vertical) * 0.065h;
                visibility += DepthVisibility(source - vertical) * 0.065h;
                visibility += DepthVisibility(source + diagonalA) * 0.065h;
                visibility += DepthVisibility(source - diagonalA) * 0.065h;
                visibility += DepthVisibility(source + diagonalB) * 0.065h;
                visibility += DepthVisibility(source - diagonalB) * 0.065h;
                return saturate(visibility);
            }

            half OcclusionResponse(half sampledVisibility, half response)
            {
                return lerp(1.0h, sampledVisibility, saturate(response));
            }

            half4 ResolveOcclusionHistory(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half currentVisibility = SoftSourceOcclusion(
                    _RoundFlareSource.xy,
                    _RoundFlareOcclusion.x,
                    max(0.01, _RoundFlareSource.z));
                if (_RoundFlareTemporal.y < 0.5)
                {
                    return half4(currentVisibility, 0.0h, 0.0h, 1.0h);
                }

                half previousVisibility = SAMPLE_TEXTURE2D_X_LOD(
                    _RoundFlareOcclusionHistory,
                    sampler_LinearClamp,
                    float2(0.5, 0.5),
                    0).r;
                half response = 1.0h - saturate(_RoundFlareTemporal.x);
                half stableVisibility = lerp(previousVisibility, currentVisibility, response);
                return half4(stableVisibility, 0.0h, 0.0h, 1.0h);
            }

            half4 CompositeRoundFlare(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 sourcePosition = _RoundFlareSource.xy;
                float aspect = max(0.01, _RoundFlareSource.z);
                float radius = _RoundFlareParams.y;
                float anisotropy = _RoundFlareParams.z;
                float haloThickness = _RoundFlareOptics.x;

                half frameVisibility = OffscreenVisibility(sourcePosition, _RoundFlareSource.w);
                half apertureVisibility = SAMPLE_TEXTURE2D_X_LOD(
                    _RoundFlareOcclusionHistory,
                    sampler_LinearClamp,
                    float2(0.5, 0.5),
                    0).r;
                half sampledVisibility = apertureVisibility;
                half coreVisibility = frameVisibility
                    * OcclusionResponse(sampledVisibility, _RoundFlareOcclusion.y);
                half aureoleVisibility = frameVisibility
                    * OcclusionResponse(apertureVisibility, _RoundFlareOcclusion.z);
                half ghostVisibility = frameVisibility
                    * OcclusionResponse(apertureVisibility, _RoundFlareOcclusion.w);
                float sourceDistance = EllipseDistance(
                    uv,
                    sourcePosition,
                    radius,
                    anisotropy,
                    aspect);
                half sourceDisc = SoftDisc(sourceDistance, 4.5);
                half sourceHalo = SoftRing(sourceDistance, 1.0, haloThickness) * 0.55h;
                half broadWash = SoftDisc(sourceDistance, 0.42) * 0.12h;

                float2 spriteDelta = uv - sourcePosition;
                spriteDelta.x *= aspect / max(0.001, anisotropy);
                float2 spriteUV = spriteDelta / max(0.0001, radius * 2.0) + 0.5;
                half4 primarySprite = SAMPLE_TEXTURE2D(
                    _RoundFlarePrimarySprite,
                    sampler_RoundFlarePrimarySprite,
                    saturate(spriteUV));
                half insideSprite = step(0.0, spriteUV.x)
                    * step(spriteUV.x, 1.0)
                    * step(0.0, spriteUV.y)
                    * step(spriteUV.y, 1.0);
                half3 proceduralPrimary = sourceDisc * _RoundFlareTint.rgb;
                half3 texturedPrimary = primarySprite.rgb * primarySprite.a * insideSprite;
                half3 primaryFlare = lerp(proceduralPrimary, texturedPrimary, _RoundFlareOptics.y);

                float2 opticalAxis = float2(0.5, 0.5) - sourcePosition;
                float2 ghostPositionA = sourcePosition + opticalAxis * (0.72 * _RoundFlareParams.w);
                float2 ghostPositionB = sourcePosition + opticalAxis * (1.18 * _RoundFlareParams.w);
                float2 ghostPositionC = sourcePosition + opticalAxis * (1.72 * _RoundFlareParams.w);
                half ghostA = SoftRing(
                    EllipseDistance(uv, ghostPositionA, radius * 0.42, 1.0, aspect),
                    0.76,
                    haloThickness * 1.2) * 0.22h;
                half ghostB = SoftDisc(
                    EllipseDistance(uv, ghostPositionB, radius * 0.3, 1.0, aspect),
                    5.0) * 0.16h;
                half ghostC = SoftRing(
                    EllipseDistance(uv, ghostPositionC, radius * 0.6, 1.0, aspect),
                    0.82,
                    haloThickness * 1.4) * 0.1h;

                float2 ghostRingDelta = uv - ghostPositionA;
                ghostRingDelta.x *= aspect;
                float2 ghostRingUV = ghostRingDelta / max(0.0001, radius * 0.84) + 0.5;
                half ringInside = step(0.0, ghostRingUV.x)
                    * step(ghostRingUV.x, 1.0)
                    * step(0.0, ghostRingUV.y)
                    * step(ghostRingUV.y, 1.0);
                half4 ringSprite = SAMPLE_TEXTURE2D(
                    _RoundFlareGhostRingSprite,
                    sampler_RoundFlareGhostRingSprite,
                    saturate(ghostRingUV));
                half3 texturedRing = ringSprite.rgb * ringSprite.a * ringInside * 0.34h;

                float2 apertureDelta = uv - ghostPositionB;
                apertureDelta.x *= aspect;
                float2 apertureUV = apertureDelta / max(0.0001, radius * 0.62) + 0.5;
                half apertureInside = step(0.0, apertureUV.x)
                    * step(apertureUV.x, 1.0)
                    * step(0.0, apertureUV.y)
                    * step(apertureUV.y, 1.0);
                half4 apertureSprite = SAMPLE_TEXTURE2D(
                    _RoundFlareApertureGhostSprite,
                    sampler_RoundFlareApertureGhostSprite,
                    saturate(apertureUV));
                half3 texturedAperture = apertureSprite.rgb * apertureSprite.a * apertureInside * 0.26h;

                half3 proceduralGhosts = (ghostA + ghostC) * half3(0.16h, 0.32h, 0.42h)
                    + ghostB * _RoundFlareTint.rgb;
                half3 ringGhost = lerp(proceduralGhosts, texturedRing, _RoundFlareOptics.z);
                half3 apertureGhost = lerp(0.0h, texturedAperture, _RoundFlareOptics.w);
                half3 coreFlare = primaryFlare * coreVisibility;
                half3 opticalGlow = (sourceHalo + broadWash)
                    * _RoundFlareTint.rgb
                    * aureoleVisibility;
                half3 opticalGhosts = (ringGhost + apertureGhost) * ghostVisibility;
                half3 flare = (coreFlare + opticalGlow + opticalGhosts)
                    * _RoundFlareParams.x
                    * max(0.5, _RoundFlareEnergy.x * _RoundFlareEnergy.y);

                half4 source = SampleSource(uv);
                source.rgb += flare;
                return source;
            }
        ENDHLSL

        Pass
        {
            Name "Sun Occlusion History"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ResolveOcclusionHistory
            ENDHLSL
        }

        Pass
        {
            Name "Round Lens Flare Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeRoundFlare
            ENDHLSL
        }
    }
}

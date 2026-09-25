Shader "BooterBigArm/TopDown3D/Broken World Sun Bloom"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #pragma target 4.5
        #define USE_FULL_PRECISION_BLIT_TEXTURE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_SunBloomTexture);
        TEXTURE2D_X(_SunBloomLowTexture);
        float4 _SunBloomParams;
        float4 _SunBloomOptics;
        float4 _SunBloomTexelSize;
        float4 _SunBloomLowTexelSize;
        float2 _SunBloomDirection;
        float _SunBloomLevelWeight;
        float4 _SunBloomSource;
        float4 _SunBloomSunOptics;
        half4 _SunBloomTint;

        half3 SampleSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, saturate(uv), 0).rgb;
        }

        half SunFrameVisibility(float2 source, float edgeReach)
        {
            float2 outside = max(max(-source, source - 1.0), 0.0);
            return 1.0h - smoothstep(0.0, max(0.0001, edgeReach), length(outside));
        }

        half PersistentSunGlow(float2 uv)
        {
            float2 delta = uv - _SunBloomSource.xy;
            delta.x *= max(0.01, _SunBloomSource.z);
            float normalizedRadius = length(delta) / max(0.001, _SunBloomSunOptics.y);
            half profile = exp2(-normalizedRadius * normalizedRadius * 2.4);
            return profile
                * _SunBloomSource.w
                * _SunBloomSunOptics.z
                * SunFrameVisibility(_SunBloomSource.xy, _SunBloomSunOptics.x);
        }

        half4 ExtractBloom(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 color = min(SampleSource(input.texcoord), _SunBloomParams.w);
            half brightness = max(color.r, max(color.g, color.b));
            half knee = max(0.0001h, _SunBloomParams.x * _SunBloomParams.y);
            half soft = saturate((brightness - _SunBloomParams.x + knee) / (2.0h * knee));
            soft = soft * soft * knee;
            half contribution = max(brightness - _SunBloomParams.x, soft);
            half3 extracted = color * (contribution / max(0.0001h, brightness));
            half sunGlow = PersistentSunGlow(input.texcoord);
            return half4(extracted + sunGlow.xxx, 1.0h);
        }

        half4 DownsampleBloom(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 stepUV = _SunBloomTexelSize.xy * lerp(0.75, 1.65, _SunBloomOptics.x);
            half3 center = SampleSource(input.texcoord) * 0.25h;
            half3 corners = SampleSource(input.texcoord + stepUV * float2(-1.0, -1.0));
            corners += SampleSource(input.texcoord + stepUV * float2(1.0, -1.0));
            corners += SampleSource(input.texcoord + stepUV * float2(-1.0, 1.0));
            corners += SampleSource(input.texcoord + stepUV * float2(1.0, 1.0));
            half3 axes = SampleSource(input.texcoord + stepUV * float2(-1.0, 0.0));
            axes += SampleSource(input.texcoord + stepUV * float2(1.0, 0.0));
            axes += SampleSource(input.texcoord + stepUV * float2(0.0, -1.0));
            axes += SampleSource(input.texcoord + stepUV * float2(0.0, 1.0));
            half3 result = center + corners * 0.0625h + axes * 0.125h;
            return half4(result, 1.0h);
        }

        half4 UpsampleBloom(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = saturate(input.texcoord);
            half3 high = SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture, sampler_LinearClamp, uv, 0).rgb;
            float2 stepUV = _SunBloomLowTexelSize.xy;
            half3 low = SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv, 0).rgb * 4.0h;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(-1.0, 0.0), 0).rgb * 2.0h;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(1.0, 0.0), 0).rgb * 2.0h;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(0.0, -1.0), 0).rgb * 2.0h;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(0.0, 1.0), 0).rgb * 2.0h;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(-1.0, -1.0), 0).rgb;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(1.0, -1.0), 0).rgb;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(-1.0, 1.0), 0).rgb;
            low += SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomLowTexture, sampler_LinearClamp, uv + stepUV * float2(1.0, 1.0), 0).rgb;
            low *= 0.0625h;
            return half4(high + low * _SunBloomLevelWeight, 1.0h);
        }

        half4 CompositeBloom(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 source = SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture, sampler_LinearClamp, saturate(input.texcoord), 0);
            half3 bloom = SAMPLE_TEXTURE2D_X_LOD(
                _SunBloomTexture, sampler_LinearClamp, saturate(input.texcoord), 0).rgb;
            source.rgb += bloom * _SunBloomTint.rgb * _SunBloomParams.z;
            return source;
        }
        ENDHLSL

        Pass
        {
            Name "Sun Bloom Extract"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ExtractBloom
            ENDHLSL
        }

        Pass
        {
            Name "Sun Bloom Downsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DownsampleBloom
            ENDHLSL
        }

        Pass
        {
            Name "Sun Bloom Upsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment UpsampleBloom
            ENDHLSL
        }

        Pass
        {
            Name "Sun Bloom Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeBloom
            ENDHLSL
        }
    }
}

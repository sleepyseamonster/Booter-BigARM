Shader "BooterBigArm/TopDown3D/Broken World Tilt Shift"
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
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_TiltShiftBlurTexture);

        float4 _TiltShiftBlurTexelSize;
        float4 _TiltShiftParams;

        float BlurWeight(float screenY)
        {
            float halfBand = _TiltShiftParams.y * 0.5;
            float distanceOutsideBand = max(0.0, abs(screenY - _TiltShiftParams.x) - halfBand);
            return smoothstep(0.0, max(0.0001, _TiltShiftParams.z), distanceOutsideBand);
        }

        float4 SampleSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture,
                sampler_LinearClamp,
                saturate(uv),
                0);
        }

        float4 HorizontalBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float blurWeight = BlurWeight(uv.y);
            float2 offset = float2(_BlitTexture_TexelSize.x * _TiltShiftParams.w * blurWeight, 0.0);
            float4 color = SampleSource(uv) * 0.22702703;
            color += SampleSource(uv + offset * 1.0) * 0.19459459;
            color += SampleSource(uv - offset * 1.0) * 0.19459459;
            color += SampleSource(uv + offset * 2.0) * 0.12162162;
            color += SampleSource(uv - offset * 2.0) * 0.12162162;
            color += SampleSource(uv + offset * 3.0) * 0.05405405;
            color += SampleSource(uv - offset * 3.0) * 0.05405405;
            color += SampleSource(uv + offset * 4.0) * 0.01621622;
            color += SampleSource(uv - offset * 4.0) * 0.01621622;
            return color;
        }

        float4 SampleHorizontalBlur(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(
                _TiltShiftBlurTexture,
                sampler_LinearClamp,
                saturate(uv),
                0);
        }

        float4 CompositeTiltShift(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float blurWeight = BlurWeight(uv.y);
            float2 offset = float2(0.0, _TiltShiftBlurTexelSize.y * _TiltShiftParams.w * blurWeight);
            float4 blurred = SampleHorizontalBlur(uv) * 0.22702703;
            blurred += SampleHorizontalBlur(uv + offset * 1.0) * 0.19459459;
            blurred += SampleHorizontalBlur(uv - offset * 1.0) * 0.19459459;
            blurred += SampleHorizontalBlur(uv + offset * 2.0) * 0.12162162;
            blurred += SampleHorizontalBlur(uv - offset * 2.0) * 0.12162162;
            blurred += SampleHorizontalBlur(uv + offset * 3.0) * 0.05405405;
            blurred += SampleHorizontalBlur(uv - offset * 3.0) * 0.05405405;
            blurred += SampleHorizontalBlur(uv + offset * 4.0) * 0.01621622;
            blurred += SampleHorizontalBlur(uv - offset * 4.0) * 0.01621622;

            return lerp(SampleSource(uv), blurred, blurWeight);
        }
        ENDHLSL

        Pass
        {
            Name "Horizontal Blur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HorizontalBlur
            ENDHLSL
        }

        Pass
        {
            Name "Vertical Blur And Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeTiltShift
            ENDHLSL
        }
    }
}

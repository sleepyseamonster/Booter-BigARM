Shader "BooterBigArm/TopDown3D/Broken World Anamorphic Streak"
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

        TEXTURE2D_X(_AnamorphicStreakTexture);

        float4 _AnamorphicStreakTexelSize;
        float4 _AnamorphicStreakParams;
        float4 _AnamorphicStreakDirection;
        half4 _AnamorphicStreakTint;

        half4 SampleSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(
                _BlitTexture,
                sampler_LinearClamp,
                saturate(uv),
                0);
        }

        half3 ExtractHighlight(float2 uv)
        {
            half3 color = SampleSource(uv).rgb;
            half brightness = max(color.r, max(color.g, color.b));
            half highlightWeight = smoothstep(
                _AnamorphicStreakParams.x,
                _AnamorphicStreakParams.x + 0.12,
                brightness);
            return color * highlightWeight;
        }

        half4 ExtractStreak(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float2 direction = normalize(_AnamorphicStreakDirection.xy + float2(0.00001, 0.0));
            float2 texelDirection = direction * _AnamorphicStreakTexelSize.xy;
            float reach = lerp(2.0, 30.0, _AnamorphicStreakParams.z);
            float2 offset = texelDirection * reach;

            half3 streak = ExtractHighlight(uv) * 0.22h;
            streak += ExtractHighlight(uv + offset * 0.20) * 0.16h;
            streak += ExtractHighlight(uv - offset * 0.20) * 0.16h;
            streak += ExtractHighlight(uv + offset * 0.45) * 0.11h;
            streak += ExtractHighlight(uv - offset * 0.45) * 0.11h;
            streak += ExtractHighlight(uv + offset * 0.72) * 0.07h;
            streak += ExtractHighlight(uv - offset * 0.72) * 0.07h;
            streak += ExtractHighlight(uv + offset) * 0.05h;
            streak += ExtractHighlight(uv - offset) * 0.05h;
            return half4(streak, 1.0h);
        }

        half3 SampleStreak(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(
                _AnamorphicStreakTexture,
                sampler_LinearClamp,
                saturate(uv),
                0).rgb;
        }

        half4 CompositeStreak(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float2 direction = normalize(_AnamorphicStreakDirection.xy + float2(0.00001, 0.0));
            float2 colorOffset = direction
                * _AnamorphicStreakTexelSize.xy
                * (_AnamorphicStreakParams.w * 36.0);
            half3 centered = SampleStreak(uv);
            half3 streak = half3(
                SampleStreak(uv + colorOffset).r,
                centered.g,
                SampleStreak(uv - colorOffset).b);
            half4 source = SampleSource(uv);
            source.rgb += streak * _AnamorphicStreakTint.rgb * _AnamorphicStreakParams.y;
            return source;
        }
        ENDHLSL

        Pass
        {
            Name "Highlight Streak Extraction"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ExtractStreak
            ENDHLSL
        }

        Pass
        {
            Name "Anamorphic Streak Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeStreak
            ENDHLSL
        }
    }
}

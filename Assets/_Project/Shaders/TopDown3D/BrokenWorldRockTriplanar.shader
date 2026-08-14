Shader "BooterBigArm/TopDown3D/Broken World Rock Triplanar"
{
    Properties
    {
        [MainTexture] _BaseMap("Rock Surface", 2D) = "white" {}
        [NoScaleOffset] _LusterMask("Mineral Luster Mask", 2D) = "black" {}
        [MainColor] _BaseColor("Rock Tint", Color) = (1, 1, 1, 1)
        _RockMetersPerTile("Rock Meters Per Tile", Float) = 0.85
        _TriplanarSharpness("Triplanar Sharpness", Range(1, 12)) = 4
        _Smoothness("Smoothness", Range(0, 1)) = 0.14
        _LusterStrength("Mineral Luster Strength", Range(0, 1)) = 0
        _LusterSmoothness("Mineral Luster Smoothness", Range(0, 1)) = 0.94
        _LusterMetallic("Mineral Luster Metallic", Range(0, 1)) = 0.35
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RockVertex
            #pragma fragment RockFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_LusterMask);
            SAMPLER(sampler_LusterMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _RockMetersPerTile;
                float _TriplanarSharpness;
                float _Smoothness;
                float _LusterStrength;
                float _LusterSmoothness;
                float _LusterMetallic;
                float _Cutoff;
                float _Surface;
                float _Cull;
                float _ZWrite;
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

            Varyings RockVertex(Attributes input)
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
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half3 SampleRockTriplanar(float3 positionWS, half3 normalWS)
            {
                float meters = max(_RockMetersPerTile, 0.01);
                float3 projectionPosition = positionWS / meters;
                half sharpness = max((half)_TriplanarSharpness, 1.0h);
                half3 weights = pow(abs(normalWS), sharpness);
                weights /= max(weights.x + weights.y + weights.z, 0.001h);

                float2 uvX = projectionPosition.zy;
                float2 uvY = projectionPosition.xz;
                float2 uvZ = projectionPosition.xy;
                uvX.x *= normalWS.x < 0.0h ? -1.0 : 1.0;
                uvY.x *= normalWS.y < 0.0h ? -1.0 : 1.0;
                uvZ.x *= normalWS.z >= 0.0h ? -1.0 : 1.0;

                half3 xProjection = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 yProjection = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                half3 zProjection = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                return xProjection * weights.x + yProjection * weights.y + zProjection * weights.z;
            }

            half SampleRockLusterTriplanar(float3 positionWS, half3 normalWS)
            {
                float meters = max(_RockMetersPerTile, 0.01);
                float3 projectionPosition = positionWS / meters;
                half sharpness = max((half)_TriplanarSharpness, 1.0h);
                half3 weights = pow(abs(normalWS), sharpness);
                weights /= max(weights.x + weights.y + weights.z, 0.001h);

                float2 uvX = projectionPosition.zy;
                float2 uvY = projectionPosition.xz;
                float2 uvZ = projectionPosition.xy;
                uvX.x *= normalWS.x < 0.0h ? -1.0 : 1.0;
                uvY.x *= normalWS.y < 0.0h ? -1.0 : 1.0;
                uvZ.x *= normalWS.z >= 0.0h ? -1.0 : 1.0;

                half xProjection = SAMPLE_TEXTURE2D(_LusterMask, sampler_LusterMask, uvX).r;
                half yProjection = SAMPLE_TEXTURE2D(_LusterMask, sampler_LusterMask, uvY).r;
                half zProjection = SAMPLE_TEXTURE2D(_LusterMask, sampler_LusterMask, uvZ).r;
                return xProjection * weights.x + yProjection * weights.y + zProjection * weights.z;
            }

            half4 RockFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                half3 albedo = SampleRockTriplanar(absolutePositionWS, normalWS) * _BaseColor.rgb;
                half luster = saturate(
                    SampleRockLusterTriplanar(absolutePositionWS, normalWS) * _LusterStrength);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = lerp(half3(0.16, 0.16, 0.16), half3(0.72, 0.78, 0.80), luster);
                surfaceData.metallic = luster * _LusterMetallic;
                surfaceData.smoothness = lerp(_Smoothness, _LusterSmoothness, luster);
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, normalWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

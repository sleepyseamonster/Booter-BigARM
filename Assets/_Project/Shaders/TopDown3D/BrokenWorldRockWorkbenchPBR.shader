Shader "BooterBigArm/TopDown3D/Broken World Rock Workbench PBR"
{
    Properties
    {
        [MainTexture] _BaseMap("Rock Base Color", 2D) = "white" {}
        [Normal] _NormalMap("Rock Normal", 2D) = "bump" {}
        [NoScaleOffset] _SurfaceMap("Surface (R AO, G Roughness, B Height)", 2D) = "white" {}
        [NoScaleOffset] _TopBaseMap("Top Base Color", 2D) = "white" {}
        [NoScaleOffset][Normal] _TopNormalMap("Top Normal", 2D) = "bump" {}
        [NoScaleOffset] _TopSurfaceMap("Top Surface (R AO, G Roughness, B Height)", 2D) = "white" {}
        [NoScaleOffset] _CrackMap("Cracks (R Crack, G Halo, B Mineral)", 2D) = "black" {}
        [MainColor] _BaseColor("Rock Tint", Color) = (1, 1, 1, 1)
        _RockMetersPerTile("Rock Meters Per Tile", Float) = 1.1
        _TriplanarSharpness("Triplanar Sharpness", Range(1, 12)) = 4
        _TopBlendStart("Top Blend Start", Range(0, 1)) = 0.42
        _TopBlendEnd("Top Blend End", Range(0, 1)) = 0.72
        _NormalStrength("Normal Strength", Range(0, 2)) = 0.75
        _SmoothnessMin("Rough Surface Smoothness", Range(0, 1)) = 0.04
        _SmoothnessMax("Smooth Surface Smoothness", Range(0, 1)) = 0.3
        _SurfacePatchStrength("Smooth And Grainy Patches", Range(0, 1)) = 0.72
        _WornSmoothnessBoost("Worn Patch Shine", Range(0, 0.5)) = 0.2
        _MineralShine("Mineral Shine", Range(0, 1)) = 0.32
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 0.75
        _HeightColorStrength("Height Color Strength", Range(0, 0.35)) = 0.1
        _MacroScale("Macro Variation Scale", Float) = 7.5
        _MacroStrength("Macro Variation Strength", Range(0, 0.3)) = 0.1
        _CrackMetersPerTile("Crack Meters Per Tile", Float) = 3.2
        _CrackAmount("Crack Amount", Range(0, 1)) = 0.42
        _CrackColor("Crack Color", Color) = (0.12, 0.085, 0.06, 1)
        _MineralColor("Mineral Patch Color", Color) = (0.48, 0.42, 0.34, 1)
        _DustColor("Upward Dust Color", Color) = (0.48, 0.34, 0.24, 1)
        _DustAmount("Upward Dust Amount", Range(0, 1)) = 0.15
        _DustSharpness("Upward Dust Sharpness", Range(1, 16)) = 5
        [HideInInspector] _RockSeed01("Rock Seed", Float) = 0
        [HideInInspector] _RockSize("Rock Size", Vector) = (4, 3, 3.5, 0)
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
        LOD 350

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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SurfaceMap);
            SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_TopBaseMap);
            SAMPLER(sampler_TopBaseMap);
            TEXTURE2D(_TopNormalMap);
            SAMPLER(sampler_TopNormalMap);
            TEXTURE2D(_TopSurfaceMap);
            SAMPLER(sampler_TopSurfaceMap);
            TEXTURE2D(_CrackMap);
            SAMPLER(sampler_CrackMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _DustColor;
                half4 _CrackColor;
                half4 _MineralColor;
                float _RockMetersPerTile;
                float _TriplanarSharpness;
                float _TopBlendStart;
                float _TopBlendEnd;
                float _NormalStrength;
                float _SmoothnessMin;
                float _SmoothnessMax;
                float _SurfacePatchStrength;
                float _WornSmoothnessBoost;
                float _MineralShine;
                float _OcclusionStrength;
                float _HeightColorStrength;
                float _MacroScale;
                float _MacroStrength;
                float _CrackMetersPerTile;
                float _CrackAmount;
                float _DustAmount;
                float _DustSharpness;
                float _RockSeed01;
                float4 _RockSize;
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

            struct TriplanarSample
            {
                half3 albedo;
                half3 normalWS;
                half3 surface;
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

            TriplanarSample SampleSideRock(float3 samplePositionWS, half3 geometricNormalWS)
            {
                TriplanarSample result;
                float meters = max(_RockMetersPerTile, 0.01);
                float3 projectionPosition = samplePositionWS / meters;
                half2 weights = pow(abs(geometricNormalWS.xz), max((half)_TriplanarSharpness, 1.0h));
                weights = max(weights, half2(0.0001h, 0.0001h));
                weights /= max(weights.x + weights.y, 0.001h);
                float2 uvX = projectionPosition.zy;
                float2 uvZ = projectionPosition.xy;

                half3 albedoX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 albedoZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                result.albedo = albedoX * weights.x + albedoZ * weights.y;

                half3 surfaceX = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, uvX).rgb;
                half3 surfaceZ = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, uvZ).rgb;
                result.surface = surfaceX * weights.x + surfaceZ * weights.y;

                half3 normalX = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX), _NormalStrength);
                half3 normalZ = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ), _NormalStrength);

                normalX = half3(normalX.xy + geometricNormalWS.zy, abs(normalX.z) * geometricNormalWS.x);
                normalZ = half3(normalZ.xy + geometricNormalWS.xy, abs(normalZ.z) * geometricNormalWS.z);
                result.normalWS = normalize(
                    normalX.zyx * weights.x + normalZ.xyz * weights.y);
                return result;
            }

            TriplanarSample SampleTopRock(float3 samplePositionWS, half3 geometricNormalWS)
            {
                TriplanarSample result;
                float meters = max(_RockMetersPerTile, 0.01);
                float2 uv = samplePositionWS.xz / meters;
                result.albedo = SAMPLE_TEXTURE2D(_TopBaseMap, sampler_TopBaseMap, uv).rgb;
                result.surface = SAMPLE_TEXTURE2D(_TopSurfaceMap, sampler_TopSurfaceMap, uv).rgb;
                half3 normal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_TopNormalMap, sampler_TopNormalMap, uv), _NormalStrength);
                normal = half3(normal.xy + geometricNormalWS.xz, abs(normal.z) * geometricNormalWS.y);
                result.normalWS = normalize(normal.xzy);
                return result;
            }

            TriplanarSample SampleLayeredRock(
                float3 samplePositionWS,
                half3 geometricNormalWS,
                out half topBlend)
            {
                TriplanarSample side = SampleSideRock(samplePositionWS, geometricNormalWS);
                TriplanarSample top = SampleTopRock(samplePositionWS, geometricNormalWS);
                topBlend = smoothstep(
                    (half)_TopBlendStart,
                    max((half)_TopBlendEnd, (half)_TopBlendStart + 0.001h),
                    saturate(geometricNormalWS.y));

                TriplanarSample result;
                result.albedo = lerp(side.albedo, top.albedo, topBlend);
                result.surface = lerp(side.surface, top.surface, topBlend);
                result.normalWS = normalize(lerp(side.normalWS, top.normalWS, topBlend));
                return result;
            }

            half3 SampleCrackLayer(float3 samplePositionWS, half3 geometricNormalWS, half topBlend)
            {
                float meters = max(_CrackMetersPerTile, 0.01);
                float3 projectionPosition = samplePositionWS / meters;
                half2 weights = pow(abs(geometricNormalWS.xz), max((half)_TriplanarSharpness, 1.0h));
                weights = max(weights, half2(0.0001h, 0.0001h));
                weights /= max(weights.x + weights.y, 0.001h);
                half3 sideX = SAMPLE_TEXTURE2D(_CrackMap, sampler_CrackMap, projectionPosition.zy).rgb;
                half3 sideZ = SAMPLE_TEXTURE2D(_CrackMap, sampler_CrackMap, projectionPosition.xy).rgb;
                half3 side = sideX * weights.x + sideZ * weights.y;
                half3 top = SAMPLE_TEXTURE2D(_CrackMap, sampler_CrackMap, projectionPosition.xz).rgb;
                return lerp(side, top, topBlend);
            }

            float Hash31(float3 samplePosition)
            {
                samplePosition = frac(samplePosition * 0.1031);
                samplePosition += dot(samplePosition, samplePosition.yzx + 33.33);
                return frac((samplePosition.x + samplePosition.y) * samplePosition.z);
            }

            float ValueNoise3D(float3 samplePosition)
            {
                float3 cell = floor(samplePosition);
                float3 local = frac(samplePosition);
                local = local * local * (3.0 - 2.0 * local);
                float lowerX0 = lerp(Hash31(cell + float3(0, 0, 0)), Hash31(cell + float3(1, 0, 0)), local.x);
                float lowerX1 = lerp(Hash31(cell + float3(0, 1, 0)), Hash31(cell + float3(1, 1, 0)), local.x);
                float upperX0 = lerp(Hash31(cell + float3(0, 0, 1)), Hash31(cell + float3(1, 0, 1)), local.x);
                float upperX1 = lerp(Hash31(cell + float3(0, 1, 1)), Hash31(cell + float3(1, 1, 1)), local.x);
                return lerp(lerp(lowerX0, lowerX1, local.y), lerp(upperX0, upperX1, local.y), local.z);
            }

            half4 RockFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 geometricNormalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                float rockScale = max(_RockSize.x, max(_RockSize.y, _RockSize.z));
                float3 seedOffset = float3(
                    Hash31(float3(_RockSeed01, 1.7, 4.1)),
                    Hash31(float3(_RockSeed01, 7.3, 2.9)),
                    Hash31(float3(_RockSeed01, 5.1, 9.7))) * 37.0;
                float3 samplePositionWS = absolutePositionWS + seedOffset;
                half topBlend;
                TriplanarSample rock = SampleLayeredRock(
                    samplePositionWS,
                    geometricNormalWS,
                    topBlend);

                float patchMeters = max(0.8, rockScale * 0.55);
                half patchNoise = (half)ValueNoise3D(samplePositionWS / patchMeters);
                half secondaryPatch = (half)ValueNoise3D(samplePositionWS / (patchMeters * 0.43) + 19.37);
                half smoothPatch = smoothstep(0.54h, 0.82h, patchNoise)
                    * (half)_SurfacePatchStrength;
                half grainPatch = smoothstep(0.58h, 0.86h, secondaryPatch)
                    * (1.0h - smoothPatch * 0.65h)
                    * (half)_SurfacePatchStrength;
                rock.normalWS = normalize(lerp(
                    rock.normalWS,
                    geometricNormalWS,
                    smoothPatch * 0.62h));

                half macro = (half)ValueNoise3D(absolutePositionWS / max(_MacroScale, 0.01));
                half macroMultiplier = lerp(1.0h - (half)_MacroStrength, 1.0h + (half)_MacroStrength, macro);
                half heightMultiplier = lerp(
                    1.0h - (half)_HeightColorStrength,
                    1.0h + (half)_HeightColorStrength,
                    rock.surface.b);
                half3 albedo = rock.albedo * _BaseColor.rgb * macroMultiplier * heightMultiplier;

                half3 crackLayer = SampleCrackLayer(samplePositionWS, geometricNormalWS, topBlend);
                half crackCoverage = lerp(0.45h, 1.0h, smoothstep(0.2h, 0.8h, macro));
                half crack = saturate(pow(saturate(crackLayer.r), 1.25h)
                    * (half)_CrackAmount
                    * crackCoverage);
                half crackHalo = saturate(crackLayer.g * (half)_CrackAmount * 0.38h);
                half mineral = saturate(crackLayer.b * smoothstep(0.46h, 0.78h, secondaryPatch));
                albedo = lerp(albedo, _MineralColor.rgb, mineral * 0.16h);
                albedo = lerp(albedo, _CrackColor.rgb, crack);
                albedo *= 1.0h - crackHalo * 0.12h;

                half upward = pow(saturate(geometricNormalWS.y), max((half)_DustSharpness, 1.0h));
                half dust = saturate(upward * (half)_DustAmount * lerp(0.65h, 1.25h, rock.surface.b));
                albedo = lerp(albedo, _DustColor.rgb, dust);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0.04h, 0.04h, 0.04h);
                surfaceData.metallic = 0.0h;
                surfaceData.smoothness = saturate(
                    lerp(_SmoothnessMax, _SmoothnessMin, rock.surface.g)
                    + smoothPatch * (half)_WornSmoothnessBoost
                    + mineral * (half)_MineralShine * 0.32h
                    - grainPatch * 0.035h
                    - crack * 0.08h);
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.emission = 0.0h;
                surfaceData.occlusion = lerp(1.0h, rock.surface.r, _OcclusionStrength)
                    * lerp(1.0h, 0.58h, crack);
                surfaceData.alpha = 1.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(rock.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

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

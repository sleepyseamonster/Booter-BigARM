Shader "BooterBigArm/TopDown3D/Broken World Terrain Blend"
{
    Properties
    {
        [MainTexture] _BaseMap("Rust Sand Dirt", 2D) = "white" {}
        _SweptSandMap("Swept Beige Sand", 2D) = "white" {}
        _SweptSandTransitionMap("Swept Sand Transition", 2D) = "white" {}
        _GravelMap("Iron Gravel", 2D) = "white" {}
        _GravelTransitionMap("Gravel Transition", 2D) = "white" {}
        _RockyMap("Mixed Gray Earth Rock", 2D) = "white" {}
        _RockyTransitionMap("Mixed Rock Transition", 2D) = "white" {}
        [NoScaleOffset] _RockyHeightMap("Mixed Rock Height", 2D) = "black" {}
        [NoScaleOffset] _RockyNormalMap("Mixed Rock Normal", 2D) = "bump" {}
        [NoScaleOffset] _RockyTransitionHeightMap("Mixed Rock Transition Height", 2D) = "black" {}
        [NoScaleOffset] _RockyTransitionNormalMap("Mixed Rock Transition Normal", 2D) = "bump" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMetersPerTile("Base Meters Per Tile", Float) = 3
        _SweptSandMetersPerTile("Swept Sand Meters Per Tile", Float) = 4
        _GravelMetersPerTile("Gravel Meters Per Tile", Float) = 2.25
        _RockyMetersPerTile("Rocky Meters Per Tile", Float) = 3
        _PatchFrequency("Patch Frequency", Float) = 0.035
        _SweptSandThreshold("Swept Sand Threshold", Range(0, 1)) = 0.64
        _GravelThreshold("Gravel Threshold", Range(0, 1)) = 0.66
        _RockyThreshold("Rocky Threshold", Range(0, 1)) = 0.62
        _BlendWidth("Patch Edge Softness", Range(0.01, 0.3)) = 0.11
        _TransitionWidth("Transition Material Band", Range(0.02, 0.3)) = 0.13
        _SweptSandStrength("Swept Sand Strength", Range(0, 1)) = 0.9
        _GravelStrength("Gravel Strength", Range(0, 1)) = 0.92
        _RockyStrength("Rocky Strength", Range(0, 1)) = 0.96
        _RockyHeightScale("Rocky Raised Relief", Range(0, 0.12)) = 0.055
        _RockyNormalStrength("Rocky Normal Strength", Range(0, 2)) = 0.9
        _RockyReliefOcclusion("Rocky Crevice Occlusion", Range(0, 0.5)) = 0.16
        _Smoothness("Smoothness", Range(0, 1)) = 0.18
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
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment
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
            TEXTURE2D(_SweptSandMap);
            SAMPLER(sampler_SweptSandMap);
            TEXTURE2D(_SweptSandTransitionMap);
            SAMPLER(sampler_SweptSandTransitionMap);
            TEXTURE2D(_GravelMap);
            SAMPLER(sampler_GravelMap);
            TEXTURE2D(_GravelTransitionMap);
            SAMPLER(sampler_GravelTransitionMap);
            TEXTURE2D(_RockyMap);
            SAMPLER(sampler_RockyMap);
            TEXTURE2D(_RockyTransitionMap);
            SAMPLER(sampler_RockyTransitionMap);
            TEXTURE2D(_RockyHeightMap);
            SAMPLER(sampler_RockyHeightMap);
            TEXTURE2D(_RockyNormalMap);
            SAMPLER(sampler_RockyNormalMap);
            TEXTURE2D(_RockyTransitionHeightMap);
            SAMPLER(sampler_RockyTransitionHeightMap);
            TEXTURE2D(_RockyTransitionNormalMap);
            SAMPLER(sampler_RockyTransitionNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BaseMetersPerTile;
                float _SweptSandMetersPerTile;
                float _GravelMetersPerTile;
                float _RockyMetersPerTile;
                float _PatchFrequency;
                float _SweptSandThreshold;
                float _GravelThreshold;
                float _RockyThreshold;
                float _BlendWidth;
                float _TransitionWidth;
                float _SweptSandStrength;
                float _GravelStrength;
                float _RockyStrength;
                float _RockyHeightScale;
                float _RockyNormalStrength;
                float _RockyReliefOcclusion;
                float _Smoothness;
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
                float value = 0.0;
                float amplitude = 0.5;
                const float2x2 rotation = float2x2(0.80, -0.60, 0.60, 0.80);
                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += ValueNoise(position) * amplitude;
                    position = mul(rotation, position) * 2.03 + float2(11.7, 7.3);
                    amplitude *= 0.5;
                }

                return value;
            }

            half3 SampleAntiTiled(
                float2 uv,
                float2 groundPosition,
                float seed,
                TEXTURE2D_PARAM(textureMap, sampler_textureMap))
            {
                const float2x2 rotation = float2x2(0.8660254, -0.5, 0.5, 0.8660254);
                float blend = smoothstep(
                    0.25,
                    0.75,
                    ValueNoise(groundPosition * 0.075 + seed * 9.17));
                float2 alternateUv = mul(rotation, uv * 0.91) + float2(17.3, 29.1) * seed;
                half3 primary = SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, uv).rgb;
                half3 alternate = SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, alternateUv).rgb;
                return lerp(primary, alternate, blend);
            }

            float SampleAntiTiledHeight(
                float2 uv,
                float2 groundPosition,
                float seed,
                TEXTURE2D_PARAM(textureMap, sampler_textureMap))
            {
                const float2x2 rotation = float2x2(0.8660254, -0.5, 0.5, 0.8660254);
                float blend = smoothstep(
                    0.25,
                    0.75,
                    ValueNoise(groundPosition * 0.075 + seed * 9.17));
                float2 alternateUv = mul(rotation, uv * 0.91) + float2(17.3, 29.1) * seed;
                float primary = SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, uv).r;
                float alternate = SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, alternateUv).r;
                return lerp(primary, alternate, blend);
            }

            half3 SampleAntiTiledNormal(
                float2 uv,
                float2 groundPosition,
                float seed,
                TEXTURE2D_PARAM(textureMap, sampler_textureMap))
            {
                const float2x2 rotation = float2x2(0.8660254, -0.5, 0.5, 0.8660254);
                float blend = smoothstep(
                    0.25,
                    0.75,
                    ValueNoise(groundPosition * 0.075 + seed * 9.17));
                float2 alternateUv = mul(rotation, uv * 0.91) + float2(17.3, 29.1) * seed;
                half3 primary = UnpackNormal(SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, uv));
                half3 alternate = UnpackNormal(SAMPLE_TEXTURE2D(textureMap, sampler_textureMap, alternateUv));
                alternate.xy = mul(transpose(rotation), alternate.xy);
                return normalize(lerp(primary, alternate, blend));
            }

            void BuildPatchMasks(
                float signal,
                float threshold,
                float strength,
                out float transitionMask,
                out float centerMask)
            {
                float transitionCoverage = smoothstep(
                    threshold - _TransitionWidth,
                    threshold,
                    signal) * strength;
                centerMask = smoothstep(
                    threshold,
                    threshold + _BlendWidth,
                    signal) * strength;
                transitionMask = saturate(transitionCoverage - centerMask);
            }

            Varyings TerrainVertex(Attributes input)
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

            half4 TerrainFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                float2 groundPosition = absolutePositionWS.xz;
                half3 geometricNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 normalWS = geometricNormalWS;
                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float baseMeters = max(_BaseMetersPerTile, 0.01);
                float sweptMeters = max(_SweptSandMetersPerTile, 0.01);
                float gravelMeters = max(_GravelMetersPerTile, 0.01);
                float rockyMeters = max(_RockyMetersPerTile, 0.01);
                half3 baseAlbedo = SampleAntiTiled(
                    groundPosition / baseMeters,
                    groundPosition,
                    1.0,
                    TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half3 sweptAlbedo = SampleAntiTiled(
                    groundPosition / sweptMeters,
                    groundPosition,
                    2.0,
                    TEXTURE2D_ARGS(_SweptSandMap, sampler_SweptSandMap));
                half3 sweptTransitionAlbedo = SampleAntiTiled(
                    groundPosition / sweptMeters,
                    groundPosition,
                    2.4,
                    TEXTURE2D_ARGS(_SweptSandTransitionMap, sampler_SweptSandTransitionMap));
                half3 gravelAlbedo = SampleAntiTiled(
                    groundPosition / gravelMeters,
                    groundPosition,
                    3.0,
                    TEXTURE2D_ARGS(_GravelMap, sampler_GravelMap));
                half3 gravelTransitionAlbedo = SampleAntiTiled(
                    groundPosition / gravelMeters,
                    groundPosition,
                    3.4,
                    TEXTURE2D_ARGS(_GravelTransitionMap, sampler_GravelTransitionMap));
                float rockyHeight = SampleAntiTiledHeight(
                    groundPosition / rockyMeters,
                    groundPosition,
                    4.0,
                    TEXTURE2D_ARGS(_RockyHeightMap, sampler_RockyHeightMap));
                float rockyViewDenominator = max(abs(viewDirectionWS.y), 0.28);
                float2 rockyParallax = viewDirectionWS.xz / rockyViewDenominator
                    * rockyHeight * _RockyHeightScale;
                float2 rockyGroundPosition = groundPosition + rockyParallax;
                rockyHeight = SampleAntiTiledHeight(
                    rockyGroundPosition / rockyMeters,
                    rockyGroundPosition,
                    4.0,
                    TEXTURE2D_ARGS(_RockyHeightMap, sampler_RockyHeightMap));
                half3 rockyAlbedo = SampleAntiTiled(
                    rockyGroundPosition / rockyMeters,
                    rockyGroundPosition,
                    4.0,
                    TEXTURE2D_ARGS(_RockyMap, sampler_RockyMap));
                half3 rockyNormalTS = SampleAntiTiledNormal(
                    rockyGroundPosition / rockyMeters,
                    rockyGroundPosition,
                    4.0,
                    TEXTURE2D_ARGS(_RockyNormalMap, sampler_RockyNormalMap));
                float rockyTransitionHeight = SampleAntiTiledHeight(
                    groundPosition / gravelMeters,
                    groundPosition,
                    4.4,
                    TEXTURE2D_ARGS(_RockyTransitionHeightMap, sampler_RockyTransitionHeightMap));
                float2 rockyTransitionParallax = viewDirectionWS.xz / rockyViewDenominator
                    * rockyTransitionHeight * (_RockyHeightScale * 0.7);
                float2 rockyTransitionGroundPosition = groundPosition + rockyTransitionParallax;
                rockyTransitionHeight = SampleAntiTiledHeight(
                    rockyTransitionGroundPosition / gravelMeters,
                    rockyTransitionGroundPosition,
                    4.4,
                    TEXTURE2D_ARGS(_RockyTransitionHeightMap, sampler_RockyTransitionHeightMap));
                half3 rockyTransitionAlbedo = SampleAntiTiled(
                    rockyTransitionGroundPosition / gravelMeters,
                    rockyTransitionGroundPosition,
                    4.4,
                    TEXTURE2D_ARGS(_RockyTransitionMap, sampler_RockyTransitionMap));
                half3 rockyTransitionNormalTS = SampleAntiTiledNormal(
                    rockyTransitionGroundPosition / gravelMeters,
                    rockyTransitionGroundPosition,
                    4.4,
                    TEXTURE2D_ARGS(_RockyTransitionNormalMap, sampler_RockyTransitionNormalMap));

                float2 patchPosition = groundPosition * _PatchFrequency;
                float sweptNoise = FractalNoise(patchPosition + float2(13.1, -7.9));
                float gravelNoise = FractalNoise(patchPosition * 0.87 + float2(-31.7, 19.4));
                float rockyNoise = FractalNoise(patchPosition * 0.72 + float2(47.3, 28.6));
                sweptNoise += (FractalNoise(patchPosition * 4.7 + float2(-8.2, 41.6)) - 0.5) * 0.10;
                gravelNoise += (FractalNoise(patchPosition * 5.1 + float2(24.8, -15.3)) - 0.5) * 0.11;
                rockyNoise += (FractalNoise(patchPosition * 5.3 + float2(8.4, -42.7)) - 0.5) * 0.12;
                float slope = saturate(1.0 - geometricNormalWS.y);
                gravelNoise += slope * 0.18;
                rockyNoise += slope * 0.10;

                float sweptTransitionMask;
                float sweptMask;
                BuildPatchMasks(
                    sweptNoise,
                    _SweptSandThreshold,
                    _SweptSandStrength,
                    sweptTransitionMask,
                    sweptMask);

                float gravelTransitionMask;
                float gravelMask;
                BuildPatchMasks(
                    gravelNoise,
                    _GravelThreshold,
                    _GravelStrength,
                    gravelTransitionMask,
                    gravelMask);
                float gravelAvailability = 1.0 - saturate(sweptTransitionMask + sweptMask);
                gravelTransitionMask *= gravelAvailability;
                gravelMask *= gravelAvailability;

                float rockyTransitionMask;
                float rockyMask;
                BuildPatchMasks(
                    rockyNoise,
                    _RockyThreshold,
                    _RockyStrength,
                    rockyTransitionMask,
                    rockyMask);
                float rockyAvailability = 1.0 - saturate(
                    sweptTransitionMask + sweptMask + gravelTransitionMask + gravelMask);
                rockyTransitionMask *= rockyAvailability;
                rockyMask *= rockyAvailability;

                half3 albedo = lerp(baseAlbedo, sweptTransitionAlbedo, sweptTransitionMask);
                albedo = lerp(albedo, sweptAlbedo, sweptMask);
                albedo = lerp(albedo, gravelTransitionAlbedo, gravelTransitionMask);
                albedo = lerp(albedo, gravelAlbedo, gravelMask);
                albedo = lerp(albedo, rockyTransitionAlbedo, rockyTransitionMask);
                albedo = lerp(albedo, rockyAlbedo, rockyMask);
                float macroVariation = lerp(0.94, 1.04, FractalNoise(groundPosition * 0.008 + 4.0));
                albedo *= _BaseColor.rgb * macroVariation;

                half3 rockyDetailNormalTS = half3(0.0, 0.0, 1.0);
                rockyDetailNormalTS = normalize(lerp(
                    rockyDetailNormalTS,
                    rockyTransitionNormalTS,
                    saturate(rockyTransitionMask * _RockyNormalStrength)));
                rockyDetailNormalTS = normalize(lerp(
                    rockyDetailNormalTS,
                    rockyNormalTS,
                    saturate(rockyMask * _RockyNormalStrength)));
                half3 tangentReference = abs(geometricNormalWS.z) < 0.999h
                    ? half3(0.0, 0.0, 1.0)
                    : half3(0.0, 1.0, 0.0);
                half3 tangentWS = normalize(cross(geometricNormalWS, tangentReference));
                half3 bitangentWS = normalize(cross(tangentWS, geometricNormalWS));
                normalWS = normalize(
                    tangentWS * rockyDetailNormalTS.x
                    + bitangentWS * rockyDetailNormalTS.y
                    + geometricNormalWS * rockyDetailNormalTS.z);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0.2, 0.2, 0.2);
                surfaceData.metallic = 0.0;
                float stonyMask = max(
                    gravelTransitionMask,
                    max(gravelMask, max(rockyTransitionMask, rockyMask)));
                surfaceData.smoothness = lerp(_Smoothness, 0.10, stonyMask);
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission = 0.0;
                float rockyCrevice = max(
                    rockyTransitionMask * (1.0 - rockyTransitionHeight),
                    rockyMask * (1.0 - rockyHeight));
                surfaceData.occlusion = 1.0 - rockyCrevice * _RockyReliefOcclusion;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
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

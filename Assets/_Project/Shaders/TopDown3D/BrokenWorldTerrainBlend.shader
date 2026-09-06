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
        _RockyMidTransitionMap("Medium Shale Transition", 2D) = "white" {}
        _RockyTransitionMap("Mixed Rock Transition", 2D) = "white" {}
        [NoScaleOffset] _RockyHeightMap("Mixed Rock Height", 2D) = "black" {}
        [NoScaleOffset] _RockyNormalMap("Mixed Rock Normal", 2D) = "bump" {}
        [NoScaleOffset] _RockyMidTransitionHeightMap("Medium Shale Transition Height", 2D) = "black" {}
        [NoScaleOffset] _RockyMidTransitionNormalMap("Medium Shale Transition Normal", 2D) = "bump" {}
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
        _DetailFadeStart("Geological Detail Fade Start", Float) = 55
        _DetailFadeEnd("Geological Detail Fade End", Float) = 150
        _PebbleDetail("Pebble Detail", Range(0, 1)) = 0
        [NoScaleOffset] _PebbleAlbedoMap("Pebble Gravel Color", 2D) = "gray" {}
        [NoScaleOffset] _PebbleHeightMap("Pebble Gravel Height", 2D) = "black" {}
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
            #pragma multi_compile_fragment _ TOPDOWN3D_PLAYTEST_FAST_TERRAIN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            // Share compatible repeat/filter states. Independent texture resources do not
            // need independent samplers; reserve headroom for URP lights and shadow maps.
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SweptSandMap);
            #define sampler_SweptSandMap sampler_BaseMap
            TEXTURE2D(_SweptSandTransitionMap);
            #define sampler_SweptSandTransitionMap sampler_BaseMap
            TEXTURE2D(_GravelMap);
            #define sampler_GravelMap sampler_BaseMap
            TEXTURE2D(_GravelTransitionMap);
            #define sampler_GravelTransitionMap sampler_BaseMap
            TEXTURE2D(_RockyMap);
            #define sampler_RockyMap sampler_BaseMap
            TEXTURE2D(_RockyMidTransitionMap);
            #define sampler_RockyMidTransitionMap sampler_BaseMap
            TEXTURE2D(_RockyTransitionMap);
            #define sampler_RockyTransitionMap sampler_BaseMap
            TEXTURE2D(_RockyHeightMap);
            SAMPLER(sampler_RockyHeightMap);
            TEXTURE2D(_RockyNormalMap);
            SAMPLER(sampler_RockyNormalMap);
            TEXTURE2D(_RockyMidTransitionHeightMap);
            #define sampler_RockyMidTransitionHeightMap sampler_RockyHeightMap
            TEXTURE2D(_RockyMidTransitionNormalMap);
            #define sampler_RockyMidTransitionNormalMap sampler_RockyNormalMap
            TEXTURE2D(_RockyTransitionHeightMap);
            #define sampler_RockyTransitionHeightMap sampler_RockyHeightMap
            TEXTURE2D(_RockyTransitionNormalMap);
            #define sampler_RockyTransitionNormalMap sampler_RockyNormalMap

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
                float _DetailFadeStart;
                float _DetailFadeEnd;
                float _PebbleDetail;
                float _Smoothness;
                float _Cutoff;
                float _Surface;
                float _Cull;
                float _ZWrite;
            CBUFFER_END

            TEXTURE2D(_PebbleAlbedoMap);
            SAMPLER(sampler_PebbleAlbedoMap);
            TEXTURE2D(_PebbleHeightMap);
            #define sampler_PebbleHeightMap sampler_PebbleAlbedoMap

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 clutter : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                half4 geologyWeights : TEXCOORD3;
                half clutter : TEXCOORD4;
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

            half3 SampleFarAlbedo(
                float2 uv,
                float mipLevel,
                TEXTURE2D_PARAM(textureMap, sampler_textureMap))
            {
                return SAMPLE_TEXTURE2D_LOD(
                    textureMap,
                    sampler_textureMap,
                    uv,
                    mipLevel).rgb;
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
                output.geologyWeights = input.color;
                output.clutter = input.clutter.x;
                return output;
            }

            // Matched image textures replace the rejected cell/dot pattern. Colour and height
            // share the same world-space anti-tiling; normals are derivatives of that height.
            float PebbleHeight(float2 position)
            {
                return SampleAntiTiledHeight(position / 1.25, position, 7.0,
                    TEXTURE2D_ARGS(_PebbleHeightMap, sampler_PebbleHeightMap));
            }

            void ApplyPebbles(float2 position, half3 view, float admission, float pixelFootprint,
                inout half3 normal, inout SurfaceData surface)
            {
                float patch = smoothstep(0.22, 0.62, ValueNoise(position * 1.4 + 17.3));
                float amount = admission * patch;
                [branch] if (amount <= 0.001) return;
                float detail = 1.0 - smoothstep(0.02, 0.08, pixelFootprint);
                float height = PebbleHeight(position);
                position += view.xz / max(abs(view.y), 0.3) * max(0.0, height - 0.08) * 0.025 * detail;
                height = PebbleHeight(position);
                half3 albedo = SampleAntiTiled(position / 1.25, position, 7.0,
                    TEXTURE2D_ARGS(_PebbleAlbedoMap, sampler_PebbleAlbedoMap));
                float cover = smoothstep(0.075, 0.22, height) * amount;
                surface.albedo = lerp(surface.albedo, albedo * _BaseColor.rgb, cover);
                surface.smoothness = lerp(surface.smoothness, lerp(0.07, 0.15, saturate(height)), cover);
                surface.occlusion *= 1.0 - cover * 0.08;
                [branch] if (detail > 0.001)
                {
                    float step = max(0.0025, pixelFootprint * 0.5);
                    float dx = (PebbleHeight(position + float2(step, 0))
                        - PebbleHeight(position - float2(step, 0))) * 0.025 / (2.0 * step);
                    float dz = (PebbleHeight(position + float2(0, step))
                        - PebbleHeight(position - float2(0, step))) * 0.025 / (2.0 * step);
                    normal = normalize(normal - half3(dx, 0, dz) * amount * detail * normal.y);
                }
            }

            half4 TerrainFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 absolutePositionWS = GetAbsolutePositionWS(input.positionWS);
                float2 groundPosition = absolutePositionWS.xz;
                float pixelFootprint = max(length(ddx(groundPosition)), length(ddy(groundPosition)));
                half3 geometricNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 normalWS = geometricNormalWS;
                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float cameraDistance = distance(GetCameraPositionWS(), input.positionWS);
                float detailFade = 1.0 - smoothstep(
                    _DetailFadeStart,
                    max(_DetailFadeStart + 1.0, _DetailFadeEnd),
                    cameraDistance);

                // Once the geological detail contract has fully faded, the distant
                // landscape no longer needs the near-field anti-tiling, parallax,
                // height, or normal samples. Explicit mip sampling keeps this branch
                // derivative-safe while retaining the same generated geology weights.
#if defined(TOPDOWN3D_PLAYTEST_FAST_TERRAIN)
                if (true)
#else
                [branch]
                if (detailFade <= 0.0001)
#endif
                {
                    float sandSignal = saturate(input.geologyWeights.r);
                    float gravelSignal = saturate(input.geologyWeights.g);
                    float rockySignal = saturate(input.geologyWeights.b);
                    float weathering = saturate(input.geologyWeights.a);
                    float sweptMask = smoothstep(0.22, 0.78, sandSignal) * _SweptSandStrength;
                    float gravelMask = smoothstep(0.14, 0.62, gravelSignal) * _GravelStrength;
                    float rockyMask = smoothstep(0.16, 0.72, rockySignal) * _RockyStrength;
                    float totalGeology = max(1.0, sweptMask + gravelMask + rockyMask);
                    sweptMask /= totalGeology;
                    gravelMask /= totalGeology;
                    rockyMask /= totalGeology;

#if defined(TOPDOWN3D_PLAYTEST_FAST_TERRAIN)
                    const float farTileScale = 1.0;
                    const float farMipLevel = 0.0;
#else
                    const float farTileScale = 8.0;
                    const float farMipLevel = 3.0;
#endif
                    half3 farBaseAlbedo = SampleFarAlbedo(
                        groundPosition / max(_BaseMetersPerTile * farTileScale, 0.01),
                        farMipLevel,
                        TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                    half3 farSweptAlbedo = SampleFarAlbedo(
                        groundPosition / max(_SweptSandMetersPerTile * farTileScale, 0.01),
                        farMipLevel,
                        TEXTURE2D_ARGS(_SweptSandMap, sampler_SweptSandMap));
                    half3 farGravelAlbedo = SampleFarAlbedo(
                        groundPosition / max(_GravelMetersPerTile * farTileScale, 0.01),
                        farMipLevel,
                        TEXTURE2D_ARGS(_GravelMap, sampler_GravelMap));
                    half3 farRockyAlbedo = SampleFarAlbedo(
                        groundPosition / max(_RockyMetersPerTile * farTileScale, 0.01),
                        farMipLevel,
                        TEXTURE2D_ARGS(_RockyMap, sampler_RockyMap));

                    half3 farAlbedo = lerp(farBaseAlbedo, farSweptAlbedo, sweptMask);
                    farAlbedo = lerp(farAlbedo, farGravelAlbedo, gravelMask);
                    farAlbedo = lerp(farAlbedo, farRockyAlbedo, rockyMask);
                    farAlbedo *= _BaseColor.rgb * lerp(0.92, 1.05, weathering);

                    SurfaceData farSurfaceData = (SurfaceData)0;
                    farSurfaceData.albedo = farAlbedo;
                    farSurfaceData.specular = half3(0.2, 0.2, 0.2);
                    farSurfaceData.metallic = 0.0;
                    farSurfaceData.smoothness = lerp(
                        _Smoothness,
                        0.10,
                        max(gravelMask, rockyMask));
                    farSurfaceData.normalTS = half3(0.0, 0.0, 1.0);
                    farSurfaceData.emission = 0.0;
                    farSurfaceData.occlusion = 1.0;
                    farSurfaceData.alpha = 1.0;
                    farSurfaceData.clearCoatMask = 0.0;
                    farSurfaceData.clearCoatSmoothness = 0.0;

                    InputData farInputData = (InputData)0;
                    farInputData.positionWS = input.positionWS;
                    farInputData.positionCS = input.positionCS;
                    farInputData.normalWS = geometricNormalWS;
                    farInputData.viewDirectionWS = viewDirectionWS;
                    farInputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    farInputData.fogCoord = input.fogFactor;
                    farInputData.vertexLighting = VertexLighting(input.positionWS, geometricNormalWS);
                    farInputData.bakedGI = SampleSH(geometricNormalWS);
                    farInputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    farInputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                    ApplyPebbles(groundPosition, viewDirectionWS,
                        input.clutter * _PebbleDetail * (1.0 - smoothstep(0.55, 0.9, sandSignal)), pixelFootprint,
                        farInputData.normalWS, farSurfaceData);
                    half4 farColor = UniversalFragmentPBR(farInputData, farSurfaceData);
                    farColor.rgb = MixFog(farColor.rgb, input.fogFactor);
                    return farColor;
                }

                float baseMeters = max(_BaseMetersPerTile, 0.01);
                float sweptMeters = max(_SweptSandMetersPerTile, 0.01);
                float gravelMeters = max(_GravelMetersPerTile, 0.01);
                float rockyMeters = max(_RockyMetersPerTile, 0.01);
                float rockyMidMeters = max(lerp(gravelMeters, rockyMeters, 0.55), 0.01);
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
                    * rockyHeight * _RockyHeightScale * detailFade;
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
                float rockyMidTransitionHeight = SampleAntiTiledHeight(
                    groundPosition / rockyMidMeters,
                    groundPosition,
                    4.2,
                    TEXTURE2D_ARGS(_RockyMidTransitionHeightMap, sampler_RockyMidTransitionHeightMap));
                float2 rockyMidTransitionParallax = viewDirectionWS.xz / rockyViewDenominator
                    * rockyMidTransitionHeight * (_RockyHeightScale * 0.82) * detailFade;
                float2 rockyMidTransitionGroundPosition = groundPosition + rockyMidTransitionParallax;
                rockyMidTransitionHeight = SampleAntiTiledHeight(
                    rockyMidTransitionGroundPosition / rockyMidMeters,
                    rockyMidTransitionGroundPosition,
                    4.2,
                    TEXTURE2D_ARGS(_RockyMidTransitionHeightMap, sampler_RockyMidTransitionHeightMap));
                half3 rockyMidTransitionAlbedo = SampleAntiTiled(
                    rockyMidTransitionGroundPosition / rockyMidMeters,
                    rockyMidTransitionGroundPosition,
                    4.2,
                    TEXTURE2D_ARGS(_RockyMidTransitionMap, sampler_RockyMidTransitionMap));
                half3 rockyMidTransitionNormalTS = SampleAntiTiledNormal(
                    rockyMidTransitionGroundPosition / rockyMidMeters,
                    rockyMidTransitionGroundPosition,
                    4.2,
                    TEXTURE2D_ARGS(_RockyMidTransitionNormalMap, sampler_RockyMidTransitionNormalMap));
                float rockyTransitionHeight = SampleAntiTiledHeight(
                    groundPosition / gravelMeters,
                    groundPosition,
                    4.4,
                    TEXTURE2D_ARGS(_RockyTransitionHeightMap, sampler_RockyTransitionHeightMap));
                float2 rockyTransitionParallax = viewDirectionWS.xz / rockyViewDenominator
                    * rockyTransitionHeight * (_RockyHeightScale * 0.7) * detailFade;
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

                // Vertex colour is the rollback packing emitted by WorldTerrainMaterialPackingAdapter:
                // R semantic deposit, G erosion/sediment, B strata exposure, A weathering.
                // Canonical world semantics remain independent of this shader-channel contract.
                float sandSignal = saturate(input.geologyWeights.r);
                float gravelSignal = saturate(input.geologyWeights.g);
                float rockySignal = saturate(input.geologyWeights.b);
                float weathering = saturate(input.geologyWeights.a);
                float sweptMask = smoothstep(0.48, 0.78, sandSignal) * _SweptSandStrength;
                float sweptTransitionMask = saturate(
                    smoothstep(0.22, 0.52, sandSignal) - sweptMask);
                float gravelMask = smoothstep(0.30, 0.62, gravelSignal) * _GravelStrength;
                float gravelTransitionMask = saturate(
                    smoothstep(0.14, 0.36, gravelSignal) - gravelMask);
                float rockyMask = smoothstep(0.34, 0.72, rockySignal) * _RockyStrength;
                float rockyTransitionMask = saturate(
                    smoothstep(0.16, 0.40, rockySignal) - rockyMask);

                float totalGeology = max(
                    1.0,
                    sweptMask + sweptTransitionMask
                    + gravelMask + gravelTransitionMask
                    + rockyMask + rockyTransitionMask);
                sweptMask /= totalGeology;
                sweptTransitionMask /= totalGeology;
                gravelMask /= totalGeology;
                gravelTransitionMask /= totalGeology;
                rockyMask /= totalGeology;
                rockyTransitionMask /= totalGeology;

                float rockyMidTransitionBlend = smoothstep(
                    _RockyThreshold - _TransitionWidth * 0.50,
                    _RockyThreshold + _BlendWidth * 0.20,
                    lerp(rockySignal, weathering, 0.35));
                half3 rockyTransitionBandAlbedo = lerp(
                    rockyTransitionAlbedo,
                    rockyMidTransitionAlbedo,
                    rockyMidTransitionBlend);
                half3 rockyTransitionBandNormalTS = normalize(lerp(
                    rockyTransitionNormalTS,
                    rockyMidTransitionNormalTS,
                    rockyMidTransitionBlend));
                float rockyTransitionBandHeight = lerp(
                    rockyTransitionHeight,
                    rockyMidTransitionHeight,
                    rockyMidTransitionBlend);
                // Treat the rocky surfaces as one covered patch so overlapping masks cannot reveal base sand.
                float rockyCoverage = saturate(rockyTransitionMask + rockyMask);
                float rockyCenterBlend = saturate(rockyMask / max(rockyCoverage, 0.0001));
                half3 rockyPatchAlbedo = lerp(
                    rockyTransitionBandAlbedo,
                    rockyAlbedo,
                    rockyCenterBlend);
                half3 rockyPatchNormalTS = normalize(lerp(
                    rockyTransitionBandNormalTS,
                    rockyNormalTS,
                    rockyCenterBlend));
                float rockyPatchHeight = lerp(
                    rockyTransitionBandHeight,
                    rockyHeight,
                    rockyCenterBlend);

                half3 albedo = lerp(baseAlbedo, sweptTransitionAlbedo, sweptTransitionMask);
                albedo = lerp(albedo, sweptAlbedo, sweptMask);
                albedo = lerp(albedo, gravelTransitionAlbedo, gravelTransitionMask);
                albedo = lerp(albedo, gravelAlbedo, gravelMask);
                albedo = lerp(albedo, rockyPatchAlbedo, rockyCoverage);
                float macroVariation = lerp(0.92, 1.05, weathering);
                macroVariation *= lerp(0.97, 1.03, FractalNoise(groundPosition * 0.008 + 4.0));
                albedo *= _BaseColor.rgb * macroVariation;

                half3 rockyDetailNormalTS = half3(0.0, 0.0, 1.0);
                rockyDetailNormalTS = normalize(lerp(
                    rockyDetailNormalTS,
                    rockyPatchNormalTS,
                    saturate(rockyCoverage * _RockyNormalStrength * detailFade)));
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
                    max(gravelMask, rockyCoverage));
                surfaceData.smoothness = lerp(_Smoothness, 0.10, stonyMask);
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission = 0.0;
                float rockyCrevice = rockyCoverage * (1.0 - rockyPatchHeight);
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

                ApplyPebbles(groundPosition, viewDirectionWS,
                    input.clutter * _PebbleDetail * (1.0 - smoothstep(0.55, 0.9, sandSignal)), pixelFootprint,
                    inputData.normalWS, surfaceData);
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

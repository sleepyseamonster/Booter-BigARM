Shader "BooterBigArm/Studies/Terrain Anti Tiling"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        [Normal] _NormalMap("Normal", 2D) = "bump" {}
        _HeightMap("Height (R; diagnostic only)", 2D) = "black" {}
        _MetersPerTile("Metres Per Texture Tile", Float) = 3
        _CellMeters("Variation Cell Metres", Float) = 5
        _Seed("Fixed Pattern Seed", Float) = 17
        _Rotation("Rotation Radians", Range(0,3.14159265)) = 3.14159265
        _ScaleVariation("Scale Variation", Range(0,.5)) = .15
        _Sharpness("Blend Sharpness", Range(1,8)) = 3
        _NormalStrength("Normal Strength", Range(0,2)) = 1
        _Smoothness("Smoothness", Range(0,1)) = .15
        [Toggle(_ANTI_TILING)] _AntiTiling("Anti Tiling", Float) = 1
        [Enum(Lit,0,Albedo,1,Normal,2,Height,3)] _DebugView("Diagnostic View", Float) = 0
        [HideInInspector] _TerrainHolesTexture("Holes", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-100" "TerrainCompatible"="True" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature_local_fragment _ANTI_TILING
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Includes/WorldAntiTiling.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _MetersPerTile, _CellMeters, _Seed, _Rotation;
            float _ScaleVariation, _Sharpness, _NormalStrength, _Smoothness;
            float _AntiTiling, _DebugView;
            float4 _TerrainHeightmapScale, _TerrainHeightmapRecipSize;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            TEXTURE2D(_TerrainHolesTexture); SAMPLER(sampler_TerrainHolesTexture);
            #ifdef UNITY_INSTANCING_ENABLED
            TEXTURE2D(_TerrainHeightmapTexture);
            TEXTURE2D(_TerrainNormalmapTexture);
            #endif
            UNITY_INSTANCING_BUFFER_START(Terrain)
            UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)
            UNITY_INSTANCING_BUFFER_END(Terrain)
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #ifdef UNITY_INSTANCING_ENABLED
                float4 patch=UNITY_ACCESS_INSTANCED_PROP(Terrain,_TerrainPatchInstanceData);
                int2 coord=int2((v.positionOS.xy+patch.xy)*patch.z);
                v.positionOS.xyz=float3(coord.x*_TerrainHeightmapScale.x,
                    UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(coord,0)))*_TerrainHeightmapScale.y,
                    coord.y*_TerrainHeightmapScale.z);
                v.normalOS=_TerrainNormalmapTexture.Load(int3(coord,0)).rgb*2-1;
                v.uv=coord*_TerrainHeightmapRecipSize.zw;
                #endif
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=v.uv; o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 world=GetAbsolutePositionWS(i.positionWS).xz;
                float2 uv=world/max(_MetersPerTile,.01);
                float2 dx=ddx(uv),dy=ddy(uv);
                #if defined(_ANTI_TILING)
                BAAntiTileContext ctx=BABuildAntiTileContext(world,_MetersPerTile,_CellMeters,
                    _Seed,_Rotation,_ScaleVariation,_Sharpness);
                #endif
                // Capture derivatives before any holes discard. Height is a
                // diagnostic only: do not pay its three fetches in lit views.
                clip(SAMPLE_TEXTURE2D(_TerrainHolesTexture,sampler_TerrainHolesTexture,i.uv).r-.0005);
                [branch] if(_DebugView>2.5)
                {
                    #if defined(_ANTI_TILING)
                    float height=BASampleAntiTiled(TEXTURE2D_ARGS(_HeightMap,sampler_HeightMap),ctx).r;
                    #else
                    float height=SAMPLE_TEXTURE2D_GRAD(_HeightMap,sampler_HeightMap,uv,dx,dy).r;
                    #endif
                    return half4(height.xxx,1);
                }
                float3 color,normalTS;
                #if defined(_ANTI_TILING)
                color=BASampleAntiTiled(TEXTURE2D_ARGS(_BaseMap,sampler_BaseMap),ctx).rgb;
                normalTS=BASampleAntiTiledNormal(TEXTURE2D_ARGS(_NormalMap,sampler_NormalMap),ctx,_NormalStrength);
                #else
                color=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv,dx,dy).rgb;
                normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_NormalMap,sampler_NormalMap,uv,dx,dy),_NormalStrength);
                #endif
                if(_DebugView>.5 && _DebugView<1.5) return half4(color,1);
                if(_DebugView>1.5 && _DebugView<2.5) return half4(normalTS*.5+.5,1);
                InputData data=(InputData)0;
                data.positionWS=i.positionWS;
                data.normalWS=BAPlanarNormalToWorld(normalTS,i.normalWS);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                data.bakedGI=SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                data.shadowMask=1;
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=color; surface.alpha=1; surface.occlusion=1;
                surface.normalTS=normalTS; surface.smoothness=_Smoothness;
                half4 result=UniversalFragmentPBR(data,surface);
                result.rgb=MixFog(result.rgb,i.fog);
                return result;
            }
            ENDHLSL
        }
    }
    Fallback Off
}

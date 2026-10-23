Shader "Hidden/BooterBigArm/AntiTilingContractProbe"
{
    Properties
    {
        _SourceNormal("Source normal", Vector) = (.6,0,.8,0)
        _GeometricNormal("Geometric normal", Vector) = (0,1,0,0)
        _Angle("Angle", Float) = 0
        _Mode("Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Assets/_Project/Shaders/TopDown3D/Includes/WorldAntiTiling.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _SourceNormal, _GeometricNormal;
            float _Angle, _Mode;
            CBUFFER_END
            float4 Vert(float4 positionOS:POSITION):SV_POSITION
            { return TransformObjectToHClip(positionOS.xyz); }
            float4 Frag():SV_Target
            {
                float2 rotation; sincos(_Angle,rotation.y,rotation.x);
                float3 result = _Mode < .5
                    ? BAReorientTileNormal(_SourceNormal.xyz,rotation)
                    : BAPlanarNormalToWorld(_SourceNormal.xyz,_GeometricNormal.xyz);
                return float4(result*.5+.5,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

Shader "BooterBigArm/TopDown3D/Ironstone Seam Experiment"
{
    Properties
    {
        _BaseColor("Neutral Rock", Color) = (0.27, 0.26, 0.25, 1)
        _IronstoneVeinColor("Ironstone Vein", Color) = (0.42, 0.13, 0.075, 1)
        _IronstoneVeinAmount("Vein Coverage", Range(0, 1)) = 0
        _IronstoneBranchAmount("Contact Branches", Range(0, 1)) = 0
        _IronstoneDepleted("Depleted Stain", Range(0, 1)) = 0
        _IronstoneDebugMask("Mask Debug", Range(0, 1)) = 0
        _FormationFractureSpacing("Branch Spacing", Float) = 6
        _RockOriginWS("Formation Origin", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _IronstoneVeinColor;
                float _IronstoneVeinAmount;
                float _IronstoneBranchAmount;
                float _IronstoneDepleted;
                float _IronstoneDebugMask;
                float _FormationFractureSpacing;
                float4 _RockOriginWS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half seam : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.seam = input.color.r;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half contact = saturate(input.seam);
                float3 localPosition = input.positionWS - _RockOriginWS.xyz;
                float spacing = max(_FormationFractureSpacing, 0.25);
                half fracture = 1.0h - smoothstep(0.02h, 0.1h,
                    abs(sin((localPosition.x + localPosition.z * 0.37) * 6.2831853 / spacing)));
                // Branches are restricted to the vicinity of actual baked contacts.
                half branch = fracture * smoothstep(0.03h, 0.4h, contact)
                    * saturate((half)_IronstoneBranchAmount);
                half mask = saturate(max(contact, branch) * (half)_IronstoneVeinAmount);
                half3 rock = _BaseColor.rgb;
                half3 stain = _IronstoneVeinColor.rgb * 0.34h;
                half3 mineral = lerp(_IronstoneVeinColor.rgb, stain,
                    saturate((half)_IronstoneDepleted));
                half3 albedo = lerp(rock, mineral, mask);
                half3 normalWS = normalize(input.normalWS);
                Light sun = GetMainLight();
                half diffuse = saturate(dot(normalWS, sun.direction));
                half3 lit = albedo * (SampleSH(normalWS) + sun.color * diffuse);
                lit = lerp(lit, half3(mask, branch, 0.0h), saturate((half)_IronstoneDebugMask));
                return half4(MixFog(lit, input.fog), 1.0h);
            }
            ENDHLSL
        }
    }
}

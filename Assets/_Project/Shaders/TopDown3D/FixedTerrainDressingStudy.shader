Shader "BooterBigArm/Studies/Fixed Terrain Dressing"
{
    Properties
    {
        _SandMap("Sand", 2D) = "white" {}
        _StoneMap("Stone", 2D) = "white" {}
        _StoneNormal("Stone normal", 2D) = "bump" {}
        _StoneSurface("Stone AO / roughness / height", 2D) = "white" {}
        _MetersPerTile("Meters per tile", Float) = 2
        _Contact("Contact treatment", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_SandMap); SAMPLER(sampler_SandMap);
            TEXTURE2D(_StoneMap); TEXTURE2D(_StoneNormal); TEXTURE2D(_StoneSurface);
            SAMPLER(sampler_StoneMap);
            CBUFFER_START(UnityPerMaterial)
            float _MetersPerTile, _Contact;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; half4 mask:TEXCOORD4; half fog:TEXCOORD5; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=n.normalWS; o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=v.uv/max(_MetersPerTile,.01); o.mask=v.color; o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half4 stone=SAMPLE_TEXTURE2D(_StoneMap,sampler_StoneMap,i.uv);
                half4 packed=SAMPLE_TEXTURE2D(_StoneSurface,sampler_StoneMap,i.uv);
                half3 sand=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.uv).rgb;
                // One sampled coordinate for all stone channels. Mask and height cooperate at transitions.
                half gravel=saturate(i.mask.g*_Contact*1.4-(1-packed.b)*.25);
                half sandContact=saturate(i.mask.r*_Contact);
                gravel*=1-sandContact;
                half3 tangentNormal=UnpackNormal(SAMPLE_TEXTURE2D(_StoneNormal,sampler_StoneMap,i.uv));
                tangentNormal=normalize(lerp(half3(0,0,1),tangentNormal,gravel));
                half3 n=normalize(i.normalWS), t=normalize(i.tangentWS.xyz);
                half3 b=cross(n,t)*i.tangentWS.w;
                InputData input=(InputData)0;
                input.positionWS=i.positionWS;
                input.normalWS=normalize(t*tangentNormal.x+b*tangentNormal.y+n*tangentNormal.z);
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI=SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=lerp(sand,stone.rgb,gravel);
                surface.alpha=1; surface.normalTS=tangentNormal;
                surface.occlusion=lerp(1,packed.r,gravel);
                surface.smoothness=lerp(.08,1-packed.g,gravel)*.4;
                half4 result=UniversalFragmentPBR(input,surface);
                result.rgb=MixFog(result.rgb,i.fog);
                return result;
            }
            ENDHLSL
        }
        // Ground geometry is physically displaced on the CPU, so ordinary depth/shadow passes see the same vertices.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}

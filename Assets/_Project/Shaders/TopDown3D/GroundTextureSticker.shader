Shader "BooterBigArm/Ground Texture Sticker"
{
    Properties
    {
        _BaseMap("Texture",2D)="white" {}
        _NormalMap("Normal",2D)="bump" {}
        _Tint("Tint",Color)=(1,1,1,1)
        _Opacity("Opacity",Range(0,1))=1
        _Feather("Edge softness",Range(.01,.49))=.2
        _Round("Round shape",Float)=1
        _Repeats("Texture repeats",Vector)=(4,4,0,0)
        _NormalStrength("Normal strength",Range(0,2))=1
        _Smoothness("Smoothness",Range(0,1))=.08
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" }
        Pass
        {
            Name "GroundSticker"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1,-1
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint; float4 _Repeats;
            float _Opacity,_Feather,_Round,_NormalStrength,_Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; half fog:TEXCOORD4; };
            Varyings Vert(Attributes v)
            {
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=v.uv; o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float rectangle=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                float circle=.5-length(i.uv-.5);
                float edge=lerp(rectangle,circle,_Round);
                float feather=smoothstep(0,max(.01,_Feather),edge);
                float2 uv=i.uv*_Repeats.xy;
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
                half alpha=tex.a*_Tint.a*_Opacity*feather;
                clip(alpha-.001);
                half3 mapped=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uv));
                mapped=normalize(half3(mapped.xy*_NormalStrength,lerp(1,mapped.z,saturate(_NormalStrength))));
                half3 n=normalize(i.normalWS),t=normalize(i.tangentWS.xyz),b=cross(n,t)*i.tangentWS.w;
                InputData input=(InputData)0; input.positionWS=i.positionWS;
                input.normalWS=normalize(t*mapped.x+b*mapped.y+n*mapped.z);
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS); input.bakedGI=SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS); input.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0; surface.albedo=tex.rgb*_Tint.rgb;
                surface.alpha=alpha; surface.normalTS=mapped; surface.occlusion=1; surface.smoothness=_Smoothness;
                half4 result=UniversalFragmentPBR(input,surface);
                result.rgb=MixFog(result.rgb,i.fog); result.a=alpha; return result;
            }
            ENDHLSL
        }
    }
}

Shader "Hidden/BooterBigArm/DeathValleyMapPreview"
{
    Properties
    {
        _MainTex ("Blender regional Landsat", 2D) = "white" {}
        _CorridorTex ("Blender corridor NAIP/Landsat", 2D) = "white" {}
        _PilotTex ("Blender pilot NAIP/Landsat", 2D) = "white" {}
        _PatchTex ("Blender canyon matched imagery", 2D) = "white" {}
        _UseImagery ("Aerial imagery", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _CorridorTex, _PilotTex, _PatchTex;
            float4 _CorridorBounds, _PilotBounds, _PatchBounds;
            float2 _MapSize;
            float _UseImagery;
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 position:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Output vert(Input input)
            {
                Output output; output.position=UnityObjectToClipPos(input.vertex);
                output.color=input.color; output.uv=input.uv; return output;
            }
            float blendWeight(float2 uv,float4 bounds,float band)
            {
                float2 inside=min(uv-bounds.xy,bounds.zw-uv)*_MapSize;
                return saturate(min(inside.x,inside.y)/band);
            }
            float2 detailUV(float2 uv,float4 bounds) { return (uv-bounds.xy)/(bounds.zw-bounds.xy); }
            fixed4 frag(Output input):SV_Target
            {
                if(_UseImagery<.5)return input.color;
                fixed3 color=tex2D(_MainTex,input.uv).rgb;
                color=lerp(color,tex2D(_CorridorTex,detailUV(input.uv,_CorridorBounds)).rgb,blendWeight(input.uv,_CorridorBounds,1000));
                color=lerp(color,tex2D(_PilotTex,detailUV(input.uv,_PilotBounds)).rgb,blendWeight(input.uv,_PilotBounds,300));
                color=lerp(color,tex2D(_PatchTex,detailUV(input.uv,_PatchBounds)).rgb,blendWeight(input.uv,_PatchBounds,300));
                return fixed4(color*input.color.rgb,1);
            }
            ENDCG
        }
    }
}

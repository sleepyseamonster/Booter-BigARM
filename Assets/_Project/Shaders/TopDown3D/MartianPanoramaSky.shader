Shader "BooterBigArm/TopDown3D/Martian Panorama Sky"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0, 2)) = 1
        _SourceSunU ("Painted Sun Longitude", Range(0, 1)) = 0.744
        _SourceSunV ("Painted Sun Latitude", Range(0.5, 1)) = 0.72
        _HorizonV ("Skyline Latitude", Range(0.5, 0.7)) = 0.545
        _SeamBlendWidth ("Seam Blend Width", Range(0, 0.1)) = 0.025
        [HideInInspector] _SunDirection ("Sun Direction", Vector) = (0, 0.5, 0.866, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half4 _Tint;
            half _Exposure;
            float _SourceSunU;
            float _SourceSunV;
            float _HorizonV;
            float _SeamBlendWidth;
            float4 _SunDirection;

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float3 sunDirection = normalize(_SunDirection.xyz);
                float viewAzimuth = atan2(direction.x, direction.z);
                float sunAzimuth = atan2(sunDirection.x, sunDirection.z);
                float u = frac(_SourceSunU + (viewAzimuth - sunAzimuth) / 6.28318530718);

                // Retain the image's distant ridge at the horizon while excluding its painted foreground.
                float elevation = degrees(asin(clamp(direction.y, -1.0, 1.0)));
                float sunElevation = max(1.0, degrees(asin(clamp(sunDirection.y, -1.0, 1.0))));
                float lowerSky = saturate(elevation / sunElevation);
                float upperSky = saturate((elevation - sunElevation) / (90.0 - sunElevation));
                float v = elevation <= sunElevation
                    ? lerp(_HorizonV, _SourceSunV, lowerSky)
                    : lerp(_SourceSunV, 1.0, upperSky);

                half3 color = tex2D(_MainTex, float2(u, v)).rgb;
                float seamDistance = min(u, 1.0 - u);
                if (_SeamBlendWidth > 0.0 && seamDistance < _SeamBlendWidth)
                {
                    half3 seamColor = 0.5h * (
                        tex2D(_MainTex, float2(0.0, v)).rgb
                        + tex2D(_MainTex, float2(1.0, v)).rgb);
                    color = lerp(seamColor, color,
                        smoothstep(0.0, _SeamBlendWidth, seamDistance));
                }

                return half4(color * _Tint.rgb * _Exposure, 1.0h);
            }
            ENDCG
        }
    }

    Fallback Off
}

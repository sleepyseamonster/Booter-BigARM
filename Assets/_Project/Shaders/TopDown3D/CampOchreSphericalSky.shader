Shader "BooterBigArm/TopDown3D/Camp Ochre Spherical Sky"
{
    Properties
    {
        _MainTex ("Sunless spherical artwork", 2D) = "white" {}
        _Exposure ("Exposure", Range(0, 2)) = 1
        _ArtworkRotation ("Fixed artwork longitude offset", Range(0, 360)) = 250
        _SunDirection ("Physical sun direction", Vector) = (0, .32, -1, 0)
        [HDR] _SunColor ("Deep yellow disc", Color) = (1.4, .85, .12, 1)
        _SunAngularRadius ("Sun angular radius in degrees", Range(.1, 2)) = .35
        _GroundHazeColor ("Below horizon dust", Color) = (.36, .16, .055, 1)
        _ZenithColor ("Pole continuity color", Color) = (.28, .09, .035, 1)
        _TwilightBrightness ("Cycle brightness", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Exposure, _SunAngularRadius, _TwilightBrightness, _ArtworkRotation;
            float4 _SunDirection;
            half4 _SunColor, _GroundHazeColor, _ZenithColor;
            struct Attributes { float4 vertex : POSITION; };
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                #if UNITY_REVERSED_Z
                    output.position.z = 0;
                #else
                    output.position.z = output.position.w;
                #endif
                output.direction = input.vertex.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float elevation = asin(clamp(direction.y, -1, 1));
                float u = frac(atan2(direction.x, direction.z) / 6.28318530718 + .5 + _ArtworkRotation / 360);
                float v = elevation / 3.14159265359 + .5;
                half3 color = tex2D(_MainTex, float2(u, v)).rgb;
                // Generation is concept artwork, not guaranteed spherical stitching.
                // Blend only the seam and pole regions; never rotate geography with the sun.
                float seamWeight = 1 - smoothstep(0, .025, min(u, 1 - u));
                half3 seam = .5 * (tex2D(_MainTex, float2(0, v)).rgb + tex2D(_MainTex, float2(1, v)).rgb);
                color = lerp(color, seam, seamWeight);
                color = lerp(color, _ZenithColor.rgb, smoothstep(.92, 1, direction.y));
                // No detailed photographic ground: it would slide against resident terrain.
                color = lerp(color, _GroundHazeColor.rgb, smoothstep(0, .16, -direction.y));
                float alignment = dot(direction, normalize(_SunDirection.xyz));
                float radius = radians(_SunAngularRadius);
                float disc = smoothstep(cos(radius * 1.18), cos(radius * .82), alignment);
                float aureole = pow(saturate(alignment), 220) * .16;
                half3 sun = _SunColor.rgb * (disc + aureole) * lerp(.65, 1, _TwilightBrightness);
                return half4((color + sun) * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}

$input v_texcoord0
#include <bgfx_shader.sh>
#include "pbr_neutral.sh"
SAMPLER2D(s_scene, 0);
SAMPLER2D(s_sceneNormal, 1);
SAMPLER2D(s_sceneDepth, 2);
uniform vec4 u_display;
uniform vec4 u_ao; // enabled, strength, normalized radius, reserved
vec3 encodeSRGB(vec3 v)
{
    v = max(v, vec3(0.0));
    return mix(12.92*v, 1.055*pow(v, vec3(1.0/2.4))-0.055, step(vec3(0.0031308),v));
}
void main()
{
    vec3 sceneColor = texture2D(s_scene, v_texcoord0).rgb;
    float aoFactor = 1.0;
    vec4 centerNormalSample = texture2D(s_sceneNormal, v_texcoord0);
    float centerDepth = texture2D(s_sceneDepth, v_texcoord0).r;
    if (u_ao.x > 0.5 && centerNormalSample.a > 0.5 && centerDepth < 0.999)
    {
        vec3 centerNormal = normalize(centerNormalSample.rgb * 2.0 - 1.0);
        vec2 offsets[4];
        offsets[0] = vec2(1.0, 0.0);
        offsets[1] = vec2(-1.0, 0.0);
        offsets[2] = vec2(0.0, 1.0);
        offsets[3] = vec2(0.0, -1.0);
        float occlusion = 0.0;
        for (int i = 0; i < 4; ++i)
        {
            vec2 sampleUv = v_texcoord0 + offsets[i] * u_ao.z;
            vec4 sampleNormal = texture2D(s_sceneNormal, sampleUv);
            float sampleDepth = texture2D(s_sceneDepth, sampleUv).r;
            float closer = step(sampleDepth + 0.002, centerDepth);
            vec3 decodedSampleNormal = normalize(sampleNormal.rgb * 2.0 - 1.0);
            float normalAgreement = 0.5 + 0.5 * max(dot(centerNormal, decodedSampleNormal), 0.0);
            occlusion += closer * normalAgreement * step(0.5, sampleNormal.a);
        }
        aoFactor = 1.0 - clamp(occlusion * 0.25 * u_ao.y, 0.0, 0.8);
    }
    sceneColor *= aoFactor;
    // Normal diagnostic colors are already display values; exposure must not alter them.
    vec3 exposed = max(sceneColor*exp2(u_display.x),vec3(0.0));
    vec3 mapped = u_display.w > 0.5 ? PBRNeutralToneMapping(exposed) : exposed;
    vec3 display = u_display.y > 0.5 ? sceneColor : encodeSRGB(mapped);
    gl_FragColor = vec4(clamp(display,0.0,1.0),1.0);
}

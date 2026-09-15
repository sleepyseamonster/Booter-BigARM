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
        vec2 offsets[12];
        offsets[0] = vec2(1.0, 0.0);
        offsets[1] = vec2(-1.0, 0.0);
        offsets[2] = vec2(0.0, 1.0);
        offsets[3] = vec2(0.0, -1.0);
        offsets[4] = vec2(0.7071, 0.7071);
        offsets[5] = vec2(-0.7071, 0.7071);
        offsets[6] = vec2(0.7071, -0.7071);
        offsets[7] = vec2(-0.7071, -0.7071);
        offsets[8] = vec2(0.9239, 0.3827);
        offsets[9] = vec2(-0.3827, 0.9239);
        offsets[10] = vec2(-0.9239, -0.3827);
        offsets[11] = vec2(0.3827, -0.9239);
        float occlusion = 0.0;
        float weightSum = 0.0;
        for (int i = 0; i < 12; ++i)
        {
            float ring = i < 4 ? 0.45 : (i < 8 ? 0.72 : 1.0);
            float weight = i < 8 ? 1.0 : 0.75;
            vec2 sampleUv = v_texcoord0 + offsets[i] * u_ao.z * ring;
            vec4 sampleNormal = texture2D(s_sceneNormal, sampleUv);
            float sampleDepth = texture2D(s_sceneDepth, sampleUv).r;
            float depthDelta = centerDepth - sampleDepth;
            float closer = smoothstep(0.0008, 0.012, depthDelta);
            float rangeWeight = 1.0 - smoothstep(0.015, 0.12, abs(depthDelta));
            vec3 decodedSampleNormal = normalize(sampleNormal.rgb * 2.0 - 1.0);
            float normalAgreement = 0.35 + 0.65 * max(dot(centerNormal, decodedSampleNormal), 0.0);
            occlusion += closer * rangeWeight * normalAgreement * step(0.5, sampleNormal.a) * weight;
            weightSum += weight;
        }
        aoFactor = 1.0 - clamp((occlusion / max(weightSum, 0.001)) * 2.2 * u_ao.y, 0.0, 0.85);
    }
    sceneColor *= aoFactor;
    // Normal diagnostic colors are already display values; exposure must not alter them.
    vec3 exposed = max(sceneColor*exp2(u_display.x),vec3(0.0));
    vec3 mapped = u_display.w > 0.5 ? PBRNeutralToneMapping(exposed) : exposed;
    vec3 display = u_display.y > 0.5 ? sceneColor : encodeSRGB(mapped);
    gl_FragColor = vec4(clamp(display,0.0,1.0),1.0);
}

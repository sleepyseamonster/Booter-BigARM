$input v_normal, v_world, v_shadow
#include <bgfx_shader.sh>
SAMPLER2D(s_shadow, 0);
SAMPLER2D(s_albedo, 1);
SAMPLER2D(s_normal, 2);
SAMPLER2D(s_surface, 3);
uniform vec4 u_material;
uniform vec4 u_light;
uniform vec4 u_sceneOptions; // normal diagnostic, textures, world repeats/m, ambient
uniform vec4 u_surfaceParams; // roughness multiplier, metallic, normal strength, reserved
uniform vec4 u_eye;
uniform vec4 u_shadowOptions; // enabled, depth bias, texel size, reserved

float visibility(vec4 shadowPosition, vec3 geometricNormal, vec3 light)
{
    if (u_shadowOptions.x < 0.5) return 1.0;
    vec3 p = shadowPosition.xyz / shadowPosition.w;
    // Outside the bounded first-pass sun volume is unshadowed.
    if (any(lessThan(p, vec3(0.0))) || any(greaterThan(p, vec3(1.0)))) return 1.0;
    float bias = u_shadowOptions.y * (1.0 + 2.0 * (1.0 - max(dot(geometricNormal, light), 0.0)));
    float result = 0.0;
    for (int y = -1; y <= 1; ++y)
        for (int x = -1; x <= 1; ++x)
            result += step(p.z - bias, texture2D(s_shadow, p.xy + vec2(float(x),float(y)) * u_shadowOptions.z).r);
    return result / 9.0;
}
void main()
{
    vec3 geometricNormal = normalize(v_normal);
    if (u_sceneOptions.x > 0.5)
    {
        gl_FragColor = vec4(geometricNormal * 0.5 + 0.5, 1.0);
        return;
    }
    vec3 normal = geometricNormal;
    vec3 albedo = u_material.rgb;
    float roughness = u_surfaceParams.x;
    float ao = 1.0;
    if (u_sceneOptions.y > 0.5)
    {
        // World projection needs no author-supplied UVs. Signed right-handed bases
        // keep +Y tangent normals aligned on both sides of every projection.
        vec3 signN = mix(vec3(-1.0),vec3(1.0),step(vec3(0.0),geometricNormal));
        vec3 weights = pow(abs(geometricNormal),vec3(4.0));
        weights /= max(dot(weights,vec3(1.0)),0.0001);
        vec3 p = v_world * u_sceneOptions.z;
        vec2 uvX = vec2(-p.z * signN.x,p.y);
        vec2 uvY = vec2(p.x,-p.z * signN.y);
        vec2 uvZ = vec2(p.x * signN.z,p.y);
        albedo *= texture2D(s_albedo,uvX).rgb * weights.x + texture2D(s_albedo,uvY).rgb * weights.y + texture2D(s_albedo,uvZ).rgb * weights.z;
        vec3 surface = texture2D(s_surface,uvX).rgb * weights.x + texture2D(s_surface,uvY).rgb * weights.y + texture2D(s_surface,uvZ).rgb * weights.z;
        ao = surface.r;
        roughness *= mix(1.0,surface.g,u_surfaceParams.w);
        // Surface B is preserved height, never metallic. No displacement in this pass.
        vec3 nx = texture2D(s_normal,uvX).xyz * 2.0 - 1.0;
        vec3 ny = texture2D(s_normal,uvY).xyz * 2.0 - 1.0;
        vec3 nz = texture2D(s_normal,uvZ).xyz * 2.0 - 1.0;
        vec3 detail = vec3(0.0,nx.y,-nx.x * signN.x) * weights.x
                    + vec3(ny.x,0.0,-ny.y * signN.y) * weights.y
                    + vec3(nz.x * signN.z,nz.y,0.0) * weights.z;
        // Remove the component into the surface; neutral normals preserve geometry.
        detail -= geometricNormal * dot(detail,geometricNormal);
        float z = max(nx.z * weights.x + ny.z * weights.y + nz.z * weights.z,0.05);
        normal = normalize(geometricNormal * z + detail * u_surfaceParams.z);
    }
    roughness = clamp(roughness,0.045,1.0);
    float metal = u_surfaceParams.y;
    vec3 light = normalize(u_light.xyz);
    vec3 view = normalize(u_eye.xyz - v_world);
    vec3 halfVector = normalize(light + view);
    float NoL = max(dot(normal,light),0.0);
    float NoV = max(dot(normal,view),0.0001);
    float NoH = max(dot(normal,halfVector),0.0);
    float VoH = max(dot(view,halfVector),0.0);
    float alpha = roughness * roughness;
    float a2 = alpha * alpha;
    float d = NoH * NoH * (a2 - 1.0) + 1.0;
    float distribution = a2 / max(3.14159265 * d * d,0.000001);
    // Height-correlated Smith GGX visibility; Fresnel uses Schlick's approximation.
    float gv = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float gl = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    float smith = 0.5 / max(gv + gl,0.0001);
    vec3 f0 = mix(vec3(0.04),albedo,metal);
    vec3 fresnel = f0 + (1.0 - f0) * pow(1.0 - VoH,5.0);
    vec3 diffuse = (1.0 - fresnel) * (1.0 - metal) * albedo / 3.14159265;
    vec3 direct = (diffuse + distribution * smith * fresnel) * NoL * u_light.w * visibility(v_shadow,geometricNormal,light);
    // Bounded hemispheric fill. This is not an environment-map/IBL solution.
    vec3 hemisphere = mix(vec3(0.16,0.13,0.10),vec3(0.50,0.60,0.75),normal.y * 0.5 + 0.5);
    vec3 ambient = ((1.0 - metal) * albedo + f0 * (1.0 - 0.5 * roughness)) * hemisphere * u_sceneOptions.w * ao;
    gl_FragColor = vec4(direct + ambient,1.0);
}

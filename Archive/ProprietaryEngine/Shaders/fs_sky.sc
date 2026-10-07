$input v_texcoord0
#include <bgfx_shader.sh>
#include "environment.sh"
uniform mat4 u_inverseViewProjection;
uniform vec4 u_eye;
uniform vec4 u_light;
void main()
{
    vec4 world = mul(u_inverseViewProjection,vec4(v_texcoord0.x*2.0-1.0,1.0-v_texcoord0.y*2.0,1.0,1.0));
    vec3 ray = normalize(world.xyz/world.w-u_eye.xyz);
    float towardSun = max(dot(ray,normalize(u_light.xyz)),0.0);
    // Broad glow and a softened small disk, sharing the surface sun direction.
    vec3 glow = u_environment[0].rgb*u_light.w*(0.12*pow(towardSun,32.0)+4.0*smoothstep(0.9997,0.99995,towardSun));
    gl_FragData[0]=vec4(skyRadiance(ray)+glow,1.0);
    gl_FragData[1]=vec4(0.0);
}

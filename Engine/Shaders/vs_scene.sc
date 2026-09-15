$input a_position, a_normal, a_texcoord0, a_tangent
$output v_normal, v_world, v_shadow, v_uv, v_tangent, v_screen
#include <bgfx_shader.sh>
uniform mat4 u_normalMatrix;
uniform mat4 u_linearMatrix;
uniform vec4 u_orientationSign;
uniform mat4 u_shadowMatrix;
void main()
{
    gl_Position = mul(u_modelViewProj, vec4(a_position, 1.0));
    v_screen = gl_Position;
    v_normal = normalize(mul(u_normalMatrix, vec4(a_normal, 0.0)).xyz);
    vec3 tangent = mul(u_linearMatrix, vec4(a_tangent.xyz, 0.0)).xyz;
    tangent = normalize(tangent-v_normal*dot(tangent,v_normal));
    v_tangent = vec4(tangent, a_tangent.w*u_orientationSign.x);
    v_uv = a_texcoord0;
    vec4 world = mul(u_model[0], vec4(a_position, 1.0));
    v_world = world.xyz;
    v_shadow = mul(u_shadowMatrix, world);
}

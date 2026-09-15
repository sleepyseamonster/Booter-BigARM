$input a_position, a_normal, a_texcoord0, a_tangent
$output v_normal, v_world, v_shadow, v_uv, v_tangent
#include <bgfx_shader.sh>
uniform mat4 u_normalMatrix;
uniform mat4 u_shadowMatrix;
void main()
{
    gl_Position = mul(u_modelViewProj, vec4(a_position, 1.0));
    v_normal = mul(u_normalMatrix, vec4(a_normal, 0.0)).xyz;
    v_tangent = vec4(normalize(mul(u_normalMatrix, vec4(a_tangent.xyz, 0.0)).xyz), a_tangent.w);
    v_uv = a_texcoord0;
    vec4 world = mul(u_model[0], vec4(a_position, 1.0));
    v_world = world.xyz;
    v_shadow = mul(u_shadowMatrix, world);
}

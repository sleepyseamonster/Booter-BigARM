$input a_position, a_normal
$output v_normal, v_world, v_shadow
#include <bgfx_shader.sh>
uniform mat4 u_normalMatrix;
uniform mat4 u_shadowMatrix;
void main()
{
    gl_Position = mul(u_modelViewProj, vec4(a_position, 1.0));
    v_normal = mul(u_normalMatrix, vec4(a_normal, 0.0)).xyz;
    vec4 world = mul(u_model[0], vec4(a_position, 1.0));
    v_world = world.xyz;
    v_shadow = mul(u_shadowMatrix, world);
}

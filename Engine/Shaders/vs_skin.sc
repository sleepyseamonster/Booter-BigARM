$input a_position, a_normal, a_indices, a_weight, a_texcoord0, a_tangent
$output v_normal, v_world, v_shadow, v_uv, v_tangent, v_screen
#include <bgfx_shader.sh>
uniform mat4 u_joints[64];
uniform mat4 u_normalMatrix;
uniform mat4 u_linearMatrix;
uniform vec4 u_orientationSign;
uniform mat4 u_shadowMatrix;
void main()
{
    mat4 skin = u_joints[int(a_indices.x)] * a_weight.x
        + u_joints[int(a_indices.y)] * a_weight.y
        + u_joints[int(a_indices.z)] * a_weight.z
        + u_joints[int(a_indices.w)] * a_weight.w;
    vec4 position = mul(skin, vec4(a_position, 1.0));
    gl_Position = mul(u_modelViewProj, position);
    v_screen = gl_Position;
    v_normal = normalize(mul(u_normalMatrix, vec4(normalize(mul(skin, vec4(a_normal, 0.0)).xyz), 0.0)).xyz);
    vec3 tangent = mul(u_linearMatrix, vec4(normalize(mul(skin, vec4(a_tangent.xyz, 0.0)).xyz), 0.0)).xyz;
    tangent = normalize(tangent-v_normal*dot(tangent,v_normal));
    v_tangent = vec4(tangent, a_tangent.w*u_orientationSign.x);
    v_uv = a_texcoord0;
    vec4 world = mul(u_model[0], position);
    v_world = world.xyz;
    v_shadow = mul(u_shadowMatrix, world);
}

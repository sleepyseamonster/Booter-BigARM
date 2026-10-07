$input a_position, a_indices, a_weight
#include <bgfx_shader.sh>
uniform mat4 u_joints[64];
void main()
{
    mat4 skin = u_joints[int(a_indices.x)] * a_weight.x
        + u_joints[int(a_indices.y)] * a_weight.y
        + u_joints[int(a_indices.z)] * a_weight.z
        + u_joints[int(a_indices.w)] * a_weight.w;
    gl_Position = mul(u_modelViewProj, mul(skin, vec4(a_position, 1.0)));
}

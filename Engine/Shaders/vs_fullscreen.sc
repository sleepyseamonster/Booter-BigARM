$input a_position, a_texcoord0
$output v_texcoord0
#include <bgfx_shader.sh>
uniform vec4 u_display;
void main()
{
    gl_Position = vec4(a_position, 1.0);
    v_texcoord0 = a_texcoord0;
    if (u_display.z > 0.5) v_texcoord0.y = 1.0 - v_texcoord0.y;
}

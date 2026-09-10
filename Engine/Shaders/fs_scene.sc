$input v_normal
#include <bgfx_shader.sh>
uniform vec4 u_material;
uniform vec4 u_light;
void main()
{
    float diffuse = max(dot(normalize(v_normal), normalize(u_light.xyz)), 0.0);
    gl_FragColor = vec4(u_material.rgb * (0.22 + diffuse * u_light.w), 1.0);
}

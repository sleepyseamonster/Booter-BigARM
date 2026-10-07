$input v_normal, v_world, v_shadow, v_uv, v_tangent, v_screen
#include <bgfx_shader.sh>
void main()
{
    vec3 normal = normalize(mul(u_view,vec4(normalize(v_normal),0.0)).xyz);
    float viewDepth = -mul(u_view,vec4(v_world,1.0)).z;
    gl_FragData[0] = vec4(normal * 0.5 + 0.5, 1.0);
    gl_FragData[1] = vec4(max(viewDepth,0.0), 0.0, 0.0, 1.0);
}

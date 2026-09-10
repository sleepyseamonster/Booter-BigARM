$input v_texcoord0
#include <bgfx_shader.sh>
SAMPLER2D(s_scene, 0);
uniform vec4 u_display;
vec3 encodeSRGB(vec3 v)
{
    v = max(v, vec3(0.0));
    return mix(12.92*v, 1.055*pow(v, vec3(1.0/2.4))-0.055, step(vec3(0.0031308),v));
}
void main()
{
    vec3 sceneColor = texture2D(s_scene, v_texcoord0).rgb;
    // Normal diagnostic colors are already display values; exposure must not alter them.
    vec3 display = u_display.y > 0.5 ? sceneColor : encodeSRGB(sceneColor*exp2(u_display.x));
    gl_FragColor = vec4(clamp(display,0.0,1.0),1.0);
}

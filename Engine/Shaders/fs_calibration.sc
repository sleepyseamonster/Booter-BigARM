$input v_texcoord0
#include <bgfx_shader.sh>
void main()
{
    // Linear reference values pass through the same RGBA16F scene target as geometry.
    float band = floor(clamp(v_texcoord0.x,0.0,0.9999)*8.0);
    float value = 0.0;
    if (band > 0.5) value=0.0031308;
    if (band > 1.5) value=0.2158605;
    if (band > 2.5) value=0.5;
    if (band > 3.5) value=1.0;
    if (band > 4.5) value=2.0;
    if (band > 5.5) value=0.1;
    if (band > 6.5) value=0.75;
    gl_FragColor=vec4(value,value,value,1.0);
}

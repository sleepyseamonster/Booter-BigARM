#include <bgfx_shader.sh>
void main()
{
    // Window depth is [0,1] on both selected backends. R32F is sampled manually.
    gl_FragColor = vec4(gl_FragCoord.z, 0.0, 0.0, 1.0);
}

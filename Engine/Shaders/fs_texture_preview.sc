$input v_texcoord0
#include <bgfx_shader.sh>
SAMPLER2D(s_preview, 0);
uniform vec4 u_textureOptions;
vec3 decodeSRGB(vec3 v)
{
    return mix(v/12.92,pow((v+0.055)/1.055,vec3(2.4)),step(vec3(0.04045),v));
}
void main()
{
    vec4 sampleValue=texture2DLod(s_preview,v_texcoord0*u_textureOptions.w,u_textureOptions.x);
    vec3 result=sampleValue.rgb;
    if (u_textureOptions.y>0.5 && u_textureOptions.y<1.5) result=vec3(sampleValue.r);
    if (u_textureOptions.y>1.5 && u_textureOptions.y<2.5) result=vec3(sampleValue.g);
    if (u_textureOptions.y>2.5 && u_textureOptions.y<3.5) result=vec3(sampleValue.b);
    if (u_textureOptions.y>3.5) result=vec3(sampleValue.a);
    // Numerical diagnostics are displayed as encoded bytes, not interpreted as light.
    if (u_textureOptions.z<0.5 || u_textureOptions.y>3.5) result=decodeSRGB(result);
    gl_FragColor=vec4(result,1.0);
}

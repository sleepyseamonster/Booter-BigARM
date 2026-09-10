$input v_normal
#include <bgfx_shader.sh>
uniform vec4 u_material;
uniform vec4 u_light;
uniform vec4 u_sceneOptions;
void main()
{
    vec3 normal = normalize(v_normal);
    if (u_sceneOptions.x > 0.5)
    {
        // Diagnostic RGB encodes world XYZ normals directly; this is not lit color.
        gl_FragColor = vec4(normal * 0.5 + 0.5, 1.0);
        return;
    }
    float diffuse = max(dot(normal, normalize(u_light.xyz)), 0.0);
    gl_FragColor = vec4(u_material.rgb * (0.22 + diffuse * u_light.w), 1.0);
}

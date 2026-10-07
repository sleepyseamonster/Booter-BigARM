// Shared linear-RGB environment; the ambient approximation is not GI or IBL.
uniform vec4 u_environment[4]; // sun, zenith, horizon, ground
vec3 skyRadiance(vec3 direction)
{
    return direction.y >= 0.0
        ? mix(u_environment[2].rgb,u_environment[1].rgb,sqrt(max(direction.y,0.0)))
        : mix(u_environment[2].rgb,u_environment[3].rgb,sqrt(max(-direction.y,0.0)));
}
vec3 environmentAmbient(vec3 normal)
{
    return mix(u_environment[3].rgb,mix(u_environment[2].rgb,u_environment[1].rgb,0.5),normal.y*0.5+0.5);
}

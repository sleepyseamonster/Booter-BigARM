$input v_normal, v_world, v_shadow, v_uv, v_tangent
#include <bgfx_shader.sh>
#include "environment.sh"
SAMPLER2D(s_shadow, 0);
SAMPLER2D(s_albedo, 1);
SAMPLER2D(s_normal, 2);
SAMPLER2D(s_surface, 3);
SAMPLER2D(s_topColor, 4);
SAMPLER2D(s_topNormal, 5);
SAMPLER2D(s_topSurface, 6);
SAMPLER2D(s_bottomColor, 7);
SAMPLER2D(s_bottomNormal, 8);
SAMPLER2D(s_bottomSurface, 9);
SAMPLER2D(s_gritColor, 10);
SAMPLER2D(s_gritNormal, 11);
SAMPLER2D(s_gritSurface, 12);
SAMPLER2D(s_cracks, 13);
uniform vec4 u_rockLayers[5]; // enabled/grit/shale/cracks, dust/variation/worn/seed, local origin
uniform vec4 u_material;
uniform vec4 u_light;
uniform vec4 u_sceneOptions; // normal diagnostic, textures, world repeats/m, ambient
uniform vec4 u_surfaceParams; // roughness multiplier, metallic, normal strength, reserved
uniform vec4 u_materialMapping; // UV mode, scale.xy
uniform vec4 u_materialUvOffset; // UV offset.xy
uniform vec4 u_fog; // enabled, density, height falloff
uniform vec4 u_fogColor; // linear RGB
uniform vec4 u_eye;
uniform vec4 u_shadowOptions; // enabled, depth bias, texel size, reserved

uniform mat4 u_shadowFarMatrix;
uniform vec4 u_shadowRange;
uniform vec4 u_shadowCamera;
vec2 receiverGradient(vec3 p)
{
    vec3 dx=dFdx(p),dy=dFdy(p);
    float determinant=dx.x*dy.y-dx.y*dy.x;
    if(abs(determinant)<1e-12)return vec2(0.0);
    return vec2(dx.z*dy.y-dy.z*dx.y,dy.z*dx.x-dx.z*dy.x)/determinant;
}
float cascadeVisibility(vec3 p, float tile, float bias, vec2 gradient)
{
    if(any(lessThan(p,vec3(0.0)))||any(greaterThan(p,vec3(1.0))))return 1.0;
    float result=0.0;
    float texel=u_shadowOptions.z;
    for(int y=-1;y<=1;++y)for(int x=-1;x<=1;++x) {
        vec2 local=clamp(p.xy+vec2(float(x),float(y))*texel,vec2(texel*.5),vec2(1.0-texel*.5));
        local=(floor(local/texel)+.5)*texel;
        float receiverDepth=p.z+dot(gradient,local-p.xy)-bias;
        vec2 uv=vec2((local.x+tile)*.5,local.y);
        result+=step(receiverDepth,texture2D(s_shadow,uv).r);
    }
    return result/9.0;
}
float visibility(vec4 shadowPosition, vec3 geometricNormal, vec3 light, vec3 world)
{
    if(u_shadowOptions.x<.5)return 1.0;
    vec3 nearPosition=shadowPosition.xyz/shadowPosition.w;
    vec4 farProjected=mul(u_shadowFarMatrix,vec4(world,1.0));
    vec3 farPosition=farProjected.xyz/farProjected.w;
    // Derivatives precede pixel-dependent range branches. Compare against the
    // receiver plane at each sampled texel center, avoiding grazing-angle acne.
    vec2 nearGradient=receiverGradient(nearPosition),farGradient=receiverGradient(farPosition);
    float distance=dot(world-u_eye.xyz,u_shadowCamera.xyz);
    if(distance>=u_shadowRange.w)return 1.0;
    float slope=1.0+2.0*(1.0-max(dot(geometricNormal,light),0.0));
    if(distance<u_shadowRange.x)return cascadeVisibility(nearPosition,0.0,u_shadowOptions.y*slope,nearGradient);
    float farShadow=cascadeVisibility(farPosition,1.0,u_shadowOptions.w*slope,farGradient);
    if(distance<u_shadowRange.y) {
        float nearShadow=cascadeVisibility(nearPosition,0.0,u_shadowOptions.y*slope,nearGradient);
        return mix(nearShadow,farShadow,smoothstep(u_shadowRange.x,u_shadowRange.y,distance));
    }
    return mix(farShadow,1.0,smoothstep(u_shadowRange.z,u_shadowRange.w,distance));
}
vec3 layerNormal(vec3 nx, vec3 ny, vec3 nz, vec3 weights, vec3 signN, vec3 geometricNormal, float strength)
{
    nx=nx*2.0-1.0; ny=ny*2.0-1.0; nz=nz*2.0-1.0;
    vec3 d=vec3(0.0,nx.y,-nx.x*signN.x)*weights.x+vec3(ny.x,0.0,-ny.y*signN.y)*weights.y+vec3(nz.x*signN.z,nz.y,0.0)*weights.z;
    d-=geometricNormal*dot(d,geometricNormal);
    return normalize(geometricNormal*max(dot(vec3(nx.z,ny.z,nz.z),weights),0.05)+d*strength);
}
float rockPatch(vec3 p)
{
    // Coherent broad variation; a material seed changes patches without changing world IDs.
    return 0.5+0.5*sin(p.x*1.7+p.y*.8)*sin(p.z*1.3-p.y*1.1);
}
float terrainNoise(vec2 p)
{
    vec2 cell=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);
    float a=fract(sin(dot(cell,vec2(127.1,311.7)))*43758.5453);
    float b=fract(sin(dot(cell+vec2(1.0,0.0),vec2(127.1,311.7)))*43758.5453);
    float c=fract(sin(dot(cell+vec2(0.0,1.0),vec2(127.1,311.7)))*43758.5453);
    float d=fract(sin(dot(cell+vec2(1.0,1.0),vec2(127.1,311.7)))*43758.5453);
    return mix(mix(a,b,f.x),mix(c,d,f.x),f.y);
}
void main()
{
    vec3 geometricNormal = normalize(v_normal);
    if (u_sceneOptions.x > 0.5)
    {
        gl_FragColor = vec4(geometricNormal * 0.5 + 0.5, 1.0);
        return;
    }
    vec3 normal = geometricNormal;
    vec3 albedo = u_material.rgb;
    float roughness = u_surfaceParams.x;
    float ao = 1.0;
    if (u_sceneOptions.y > 0.5 && u_materialMapping.x > 0.5)
    {
        vec2 uv=v_uv*u_materialMapping.yz+u_materialUvOffset.xy;
        albedo*=texture2D(s_albedo,uv).rgb;
        vec3 surfaceSample=texture2D(s_surface,uv).rgb;
        ao=surfaceSample.r;
        roughness*=mix(1.0,surfaceSample.g,u_surfaceParams.w);
        vec3 tangent=normalize(v_tangent.xyz-geometricNormal*dot(v_tangent.xyz,geometricNormal));
        vec3 bitangent=normalize(cross(geometricNormal,tangent))*v_tangent.w;
        vec3 tangentNormal=texture2D(s_normal,uv).xyz*2.0-1.0;
        vec3 mapped=tangent*tangentNormal.x+bitangent*tangentNormal.y+geometricNormal*tangentNormal.z;
        normal=normalize(mix(geometricNormal,mapped,clamp(u_surfaceParams.z,0.0,1.0)));
    }
    else if (u_sceneOptions.y > 0.5)
    {
        // World projection needs no author-supplied UVs. Signed right-handed bases
        // keep +Y tangent normals aligned on both sides of every projection.
        vec3 signN = mix(vec3(-1.0),vec3(1.0),step(vec3(0.0),geometricNormal));
        vec3 weights = pow(abs(geometricNormal),vec3(4.0));
        weights /= max(dot(weights,vec3(1.0)),0.0001);
        vec3 p = v_world * u_sceneOptions.z;
        vec2 uvX = vec2(-p.z * signN.x,p.y);
        vec2 uvY = vec2(p.x,-p.z * signN.y);
        vec2 uvZ = vec2(p.x * signN.z,p.y);
        albedo *= texture2D(s_albedo,uvX).rgb * weights.x + texture2D(s_albedo,uvY).rgb * weights.y + texture2D(s_albedo,uvZ).rgb * weights.z;
        vec3 surface = texture2D(s_surface,uvX).rgb * weights.x + texture2D(s_surface,uvY).rgb * weights.y + texture2D(s_surface,uvZ).rgb * weights.z;
        ao = surface.r;
        roughness *= mix(1.0,surface.g,u_surfaceParams.w);
        // Surface B is preserved height, never metallic. No displacement in this pass.
        vec3 nx = texture2D(s_normal,uvX).xyz * 2.0 - 1.0;
        vec3 ny = texture2D(s_normal,uvY).xyz * 2.0 - 1.0;
        vec3 nz = texture2D(s_normal,uvZ).xyz * 2.0 - 1.0;
        vec3 detail = vec3(0.0,nx.y,-nx.x * signN.x) * weights.x
                    + vec3(ny.x,0.0,-ny.y * signN.y) * weights.y
                    + vec3(nz.x * signN.z,nz.y,0.0) * weights.z;
        // Remove the component into the surface; neutral normals preserve geometry.
        detail -= geometricNormal * dot(detail,geometricNormal);
        float z = max(nx.z * weights.x + ny.z * weights.y + nz.z * weights.z,0.05);
        normal = normalize(geometricNormal * z + detail * u_surfaceParams.z);
        if(u_rockLayers[0].x>0.5 && u_rockLayers[0].x<1.5)
        {
            vec3 local=v_world-u_rockLayers[2].xyz;
            float patch=rockPatch(local+vec3(u_rockLayers[1].w*19.0));
            float top=smoothstep(0.42,0.72,geometricNormal.y);
            float bottom=smoothstep(0.08,0.5,-geometricNormal.y)*u_rockLayers[0].z;
            float side=1.0-top;
            float shalePatch=smoothstep(0.60,0.82,rockPatch(local*1.9+3.7))*u_rockLayers[0].z*(u_rockLayers[3].x*side+u_rockLayers[3].y*top);
            bottom=clamp(bottom+shalePatch,0.0,1.0);
            float grit=smoothstep(0.58,0.80,patch)*side*(1.0-bottom)*u_rockLayers[0].y;
            vec2 topX=uvX*1.0, topY=uvY*1.0, topZ=uvZ*1.0;
            vec3 topColor=texture2D(s_topColor,topX).rgb*weights.x+texture2D(s_topColor,topY).rgb*weights.y+texture2D(s_topColor,topZ).rgb*weights.z;
            vec3 topSurface=texture2D(s_topSurface,topX).rgb*weights.x+texture2D(s_topSurface,topY).rgb*weights.y+texture2D(s_topSurface,topZ).rgb*weights.z;
            vec3 topNormal=layerNormal(texture2D(s_topNormal,topX).rgb,texture2D(s_topNormal,topY).rgb,texture2D(s_topNormal,topZ).rgb,weights,signN,geometricNormal,u_surfaceParams.z*1.0);
            albedo=mix(albedo,topColor*mix(0.90,1.06,topSurface.b),top);
            surface=mix(surface,topSurface,top);
            normal=normalize(mix(normal,topNormal,top));
            vec2 bottomX=uvX*1.22, bottomY=uvY*1.22, bottomZ=uvZ*1.22;
            vec3 bottomColor=texture2D(s_bottomColor,bottomX).rgb*weights.x+texture2D(s_bottomColor,bottomY).rgb*weights.y+texture2D(s_bottomColor,bottomZ).rgb*weights.z;
            vec3 bottomSurface=texture2D(s_bottomSurface,bottomX).rgb*weights.x+texture2D(s_bottomSurface,bottomY).rgb*weights.y+texture2D(s_bottomSurface,bottomZ).rgb*weights.z;
            vec3 bottomNormal=layerNormal(texture2D(s_bottomNormal,bottomX).rgb,texture2D(s_bottomNormal,bottomY).rgb,texture2D(s_bottomNormal,bottomZ).rgb,weights,signN,geometricNormal,u_surfaceParams.z*1.5);
            albedo=mix(albedo,bottomColor*mix(0.90,1.06,bottomSurface.b),bottom);
            surface=mix(surface,bottomSurface,bottom);
            normal=normalize(mix(normal,bottomNormal,bottom));
            vec2 gritX=uvX*1.57, gritY=uvY*1.57, gritZ=uvZ*1.57;
            vec3 gritColor=texture2D(s_gritColor,gritX).rgb*weights.x+texture2D(s_gritColor,gritY).rgb*weights.y+texture2D(s_gritColor,gritZ).rgb*weights.z;
            vec3 gritSurface=texture2D(s_gritSurface,gritX).rgb*weights.x+texture2D(s_gritSurface,gritY).rgb*weights.y+texture2D(s_gritSurface,gritZ).rgb*weights.z;
            vec3 gritNormal=layerNormal(texture2D(s_gritNormal,gritX).rgb,texture2D(s_gritNormal,gritY).rgb,texture2D(s_gritNormal,gritZ).rgb,weights,signN,geometricNormal,u_surfaceParams.z*1.35);
            albedo=mix(albedo,gritColor*mix(0.90,1.06,gritSurface.b),grit);
            surface=mix(surface,gritSurface,grit);
            normal=normalize(mix(normal,gritNormal,grit));
            vec3 mask=texture2D(s_cracks,uvX*.344).rgb*weights.x+texture2D(s_cracks,uvY*.344).rgb*weights.y+texture2D(s_cracks,uvZ*.344).rgb*weights.z;
            float crack=clamp(pow(clamp(mask.r,0.0,1.0),1.25)*u_rockLayers[0].w*mix(.45,1.0,patch),0.0,1.0);
            float halo=mask.g*u_rockLayers[0].w*.38;
            float mineral=mask.b*smoothstep(.46,.78,rockPatch(local*2.3));
            // Colors below are linear equivalents of the preserved Unity material tints.
            albedo=mix(albedo,vec3(.0134,.0078,.0049),crack)*(1.0-halo*.12);
            albedo=mix(albedo,vec3(.196,.147,.095),mineral*.22);
            float dust=clamp(pow(max(geometricNormal.y,0.0),5.0)*u_rockLayers[1].x*mix(.65,1.25,surface.b),0.0,1.0);
            albedo=mix(albedo,u_rockLayers[4].rgb,dust);
            albedo*=mix(1.0,mix(.87,1.07,patch),u_rockLayers[1].y);
            ao=surface.r*mix(1.0,.58,crack);
            roughness=clamp(surface.g*u_surfaceParams.x-u_rockLayers[1].z*smoothstep(.55,.85,patch)*.25+crack*.08,0.045,1.0);
            roughness=mix(roughness,.95,dust);
        }
        if(u_rockLayers[0].x>1.5 && u_rockLayers[0].x<2.5)
        {
            // Shared world-space weights cross region boundaries without restarting.
            float broad=rockPatch(v_world*.065);
            float fine=rockPatch(v_world*.23+7.1);
            float slope=1.0-clamp(geometricNormal.y,0.0,1.0);
            float sand=1.0-smoothstep(.30,.62,broad+slope*1.8);
            float rocky=smoothstep(.025,.18,slope)*.8+smoothstep(.61,.84,broad)*.65;
            rocky=clamp(rocky,0.0,1.0)*(1.0-sand);
            float gravel=(1.0-sand-rocky)*smoothstep(.30,.70,fine);
            vec3 sandColor=texture2D(s_topColor,uvY*.65).rgb;
            vec3 gravelColor=texture2D(s_bottomColor,uvY*1.3).rgb;
            vec3 rockyColor=texture2D(s_gritColor,uvX).rgb*weights.x+texture2D(s_gritColor,uvY).rgb*weights.y+texture2D(s_gritColor,uvZ).rgb*weights.z;
            albedo=albedo*(1.0-sand-rocky-gravel)+sandColor*sand+gravelColor*gravel+rockyColor*rocky;
            vec3 rockyNormal=layerNormal(texture2D(s_gritNormal,uvX).rgb,texture2D(s_gritNormal,uvY).rgb,texture2D(s_gritNormal,uvZ).rgb,weights,signN,geometricNormal,u_surfaceParams.z);
            normal=normalize(mix(geometricNormal,rockyNormal,rocky));
            albedo*=mix(.90,1.05,broad);
            roughness=mix(.96,.85,rocky);ao=1.0;
        }
        if(u_rockLayers[0].x>2.5)
        {
            vec2 world=v_world.xz;
            vec2 warp=vec2(terrainNoise(world*.011+4.0),terrainNoise(world*.011+29.0))*24.0;
            float slope=1.0-clamp(geometricNormal.y,0.0,1.0);
            float deposit=clamp(terrainNoise((world+warp)*vec2(.006,.017))*1.1-slope*2.0,0.0,1.0);
            float erosion=clamp(terrainNoise(world*.034+17.0)*.7+slope,0.0,1.0);
            float exposure=clamp(terrainNoise((world-warp)*.024+71.0)*.6+slope*2.0,0.0,1.0);
            float sand=smoothstep(.48,.78,deposit),sandEdge=clamp(smoothstep(.22,.52,deposit)-sand,0.0,1.0);
            float gravel=smoothstep(.30,.62,erosion)*.92,gravelEdge=clamp(smoothstep(.14,.36,erosion)-gravel,0.0,1.0);
            float rocky=smoothstep(.34,.72,exposure)*.96,rockyEdge=clamp(smoothstep(.16,.40,exposure)-rocky,0.0,1.0);
            float sum=max(1.0,sand+sandEdge+gravel+gravelEdge+rocky+rockyEdge);
            sand/=sum;sandEdge/=sum;gravel/=sum;gravelEdge/=sum;rocky/=sum;rockyEdge/=sum;
            // Explicit source transitions bridge materials instead of blurring unrelated colors.
            vec2 soilUV=world/3.0,sandUV=world/4.0,gravelUV=world/2.25;
            albedo=texture2D(s_albedo,soilUV).rgb;
            albedo=mix(albedo,texture2D(s_topNormal,sandUV).rgb,sandEdge);
            albedo=mix(albedo,texture2D(s_topColor,sandUV).rgb,sand);
            albedo=mix(albedo,texture2D(s_bottomNormal,gravelUV).rgb,gravelEdge);
            albedo=mix(albedo,texture2D(s_bottomColor,gravelUV).rgb,gravel);
            float rockyCoverage=clamp(rocky+rockyEdge,0.0,1.0);
            vec3 rockColor=mix(texture2D(s_topSurface,soilUV).rgb,texture2D(s_gritColor,soilUV).rgb,rocky/max(rockyCoverage,.0001));
            albedo=mix(albedo,rockColor,rockyCoverage);
            vec3 n=texture2D(s_gritNormal,soilUV).xyz*2.0-1.0;
            vec3 detail=vec3(n.x,0.0,n.y);detail-=geometricNormal*dot(detail,geometricNormal);
            normal=normalize(geometricNormal+detail*rockyCoverage*u_surfaceParams.z*.9);
            albedo*=mix(.92,1.05,terrainNoise(world*.008+4.0));roughness=.92;ao=1.0;
        }

    }
    roughness = clamp(roughness,0.045,1.0);
    float metal = u_surfaceParams.y;
    vec3 light = normalize(u_light.xyz);
    vec3 view = normalize(u_eye.xyz - v_world);
    vec3 halfVector = normalize(light + view);
    float NoL = max(dot(normal,light),0.0);
    float NoV = max(dot(normal,view),0.0001);
    float NoH = max(dot(normal,halfVector),0.0);
    float VoH = max(dot(view,halfVector),0.0);
    float alpha = roughness * roughness;
    float a2 = alpha * alpha;
    float d = NoH * NoH * (a2 - 1.0) + 1.0;
    float distribution = a2 / max(3.14159265 * d * d,0.000001);
    // Height-correlated Smith GGX visibility; Fresnel uses Schlick's approximation.
    float gv = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float gl = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    float smith = 0.5 / max(gv + gl,0.0001);
    vec3 f0 = mix(vec3(0.04),albedo,metal);
    vec3 fresnel = f0 + (1.0 - f0) * pow(1.0 - VoH,5.0);
    vec3 diffuse = (1.0 - fresnel) * (1.0 - metal) * albedo / 3.14159265;
    vec3 direct = (diffuse + distribution * smith * fresnel) * NoL * u_light.w * u_environment[0].rgb * visibility(v_shadow,geometricNormal,light,v_world);
    // Bounded hemispheric fill. This is not an environment-map/IBL solution.
    vec3 hemisphere = environmentAmbient(normal);
    vec3 ambient = ((1.0 - metal) * albedo + f0 * (1.0 - 0.5 * roughness)) * hemisphere * u_sceneOptions.w * ao;
    vec3 lit=direct+ambient;
    float distanceToEye=length(v_world-u_eye.xyz);
    float heightAttenuation=exp(-max(v_world.y,0.0)*u_fog.z);
    float fogAmount=clamp(1.0-exp(-u_fog.y*distanceToEye*heightAttenuation),0.0,1.0)*u_fog.x;
    gl_FragColor = vec4(mix(lit,u_fogColor.rgb,fogAmount),1.0);
}

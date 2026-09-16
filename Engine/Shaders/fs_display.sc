$input v_texcoord0
#include <bgfx_shader.sh>
#include "pbr_neutral.sh"
SAMPLER2D(s_scene, 0);
SAMPLER2D(s_sceneIndirect, 1);
SAMPLER2D(s_sceneNormal, 2);
SAMPLER2D(s_sceneDepth, 3);
uniform vec4 u_display;
uniform vec4 u_ao[2]; // enabled/strength/radius/bias meters; thickness/tanHalfFov/aspect/reserved
uniform vec4 u_fogColor;
vec3 encodeSRGB(vec3 v)
{
    v = max(v, vec3(0.0));
    return mix(12.92*v, 1.055*pow(v, vec3(1.0/2.4))-0.055, step(vec3(0.0031308),v));
}
vec3 viewPosition(vec2 uv,float depth)
{
    vec2 ndc=vec2(uv.x*2.0-1.0,1.0-uv.y*2.0);
    return vec3(ndc.x*depth*u_ao[1].y*u_ao[1].z,ndc.y*depth*u_ao[1].y,-depth);
}
void main()
{
    vec3 direct=texture2D(s_scene,v_texcoord0).rgb;
    vec4 indirectFog=texture2D(s_sceneIndirect,v_texcoord0);
    float aoFactor=1.0;
    vec4 centerNormalSample=texture2D(s_sceneNormal,v_texcoord0);
    float centerDepth=texture2D(s_sceneDepth,v_texcoord0).r;
    if(u_ao[0].x>0.5&&centerNormalSample.a>0.5&&centerDepth>0.0)
    {
        vec3 centerNormal=normalize(centerNormalSample.rgb*2.0-1.0);
        vec3 centerPosition=viewPosition(v_texcoord0,centerDepth);
        vec2 radiusUv=vec2(u_ao[0].z/(2.0*centerDepth*u_ao[1].y*u_ao[1].z),u_ao[0].z/(2.0*centerDepth*u_ao[1].y));
        vec2 offsets[12];
        offsets[0]=vec2(1.0,0.0);offsets[1]=vec2(-1.0,0.0);offsets[2]=vec2(0.0,1.0);offsets[3]=vec2(0.0,-1.0);
        offsets[4]=vec2(0.7071,0.7071);offsets[5]=vec2(-0.7071,0.7071);offsets[6]=vec2(0.7071,-0.7071);offsets[7]=vec2(-0.7071,-0.7071);
        offsets[8]=vec2(0.9239,0.3827);offsets[9]=vec2(-0.3827,0.9239);offsets[10]=vec2(-0.9239,-0.3827);offsets[11]=vec2(0.3827,-0.9239);
        float occlusion=0.0,weightSum=0.0;
        for(int i=0;i<12;++i)
        {
            float ring=i<4?0.45:(i<8?0.72:1.0),weight=i<8?1.0:0.75;
            vec2 sampleUv=v_texcoord0+offsets[i]*radiusUv*ring;
            vec4 sampleNormal=texture2D(s_sceneNormal,sampleUv);float sampleDepth=texture2D(s_sceneDepth,sampleUv).r;
            if(sampleNormal.a>0.5&&sampleDepth>0.0)
            {
                vec3 delta=viewPosition(sampleUv,sampleDepth)-centerPosition;float distanceToSample=length(delta);
                float rangeWeight=1.0-smoothstep(u_ao[0].z*.35,u_ao[0].z,distanceToSample);
                float closer=smoothstep(u_ao[0].w,u_ao[1].x,centerDepth-sampleDepth);
                float horizon=max(dot(centerNormal,normalize(delta))-u_ao[0].w/max(distanceToSample,0.001),0.0);
                occlusion+=closer*rangeWeight*(0.35+0.65*horizon)*weight;
            }
            weightSum+=weight;
        }
        aoFactor=1.0-clamp((occlusion/max(weightSum,0.001))*2.2*u_ao[0].y,0.0,0.85);
    }
    vec3 sceneColor=mix(direct+indirectFog.rgb*aoFactor,u_fogColor.rgb,indirectFog.a);
    vec3 exposed=max(sceneColor*exp2(u_display.x),vec3(0.0));
    vec3 mapped=u_display.w>0.5?PBRNeutralToneMapping(exposed):exposed;
    vec3 display=u_display.y>0.5?sceneColor:encodeSRGB(mapped);
    gl_FragColor=vec4(clamp(display,0.0,1.0),1.0);
}

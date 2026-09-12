#include "World/Rocks/CalibratedRock.h"
#include <algorithm>
#include <cmath>
namespace engine {
namespace {
using V=std::array<float,3>;
V add(V a,V b){for(size_t i=0;i<3;++i)a[i]+=b[i];return a;}
V sub(V a,V b){for(size_t i=0;i<3;++i)a[i]-=b[i];return a;}
V mul(V a,float n){for(float& v:a)v*=n;return a;}
float dot(V a,V b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
V cross(V a,V b){return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
float lerp(float a,float b,float t){return a+(b-a)*t;}
uint32_t next(uint32_t& state){state+=0x9E3779B9u;auto v=state;v=(v^(v>>16))*0x7FEB352Du;v=(v^(v>>15))*0x846CA68Bu;return v^(v>>16);}
float random(uint32_t& state){return float(next(state)&0x00ffffffu)/16777215.f;}
V rotate(const RockVolume& v,V p){
    const V q{v.orientation[0],v.orientation[1],v.orientation[2]};const auto t=mul(cross(q,p),2);p=add(p,add(mul(t,v.orientation[3]),cross(q,t)));
    const float cr=std::cos(v.roll),sr=std::sin(v.roll);p={cr*p[0]-sr*p[1],sr*p[0]+cr*p[1],p[2]};
    const float cp=std::cos(v.pitch),sp=std::sin(v.pitch);p={p[0],cp*p[1]-sp*p[2],sp*p[1]+cp*p[2]};
    const float cy=std::cos(v.yaw),sy=std::sin(v.yaw);return {cy*p[0]+sy*p[2],p[1],-sy*p[0]+cy*p[2]};
}
}
CalibratedRockVolume::CalibratedRockVolume(const RockVolume& v,float edgeDamage,uint32_t seedDelta):volume_(v){
    // Serialized Unity rotations carry small decimal rounding; normalize once.
    float norm=0;for(float q:volume_.orientation)norm+=q*q;for(float& q:volume_.orientation)q/=std::sqrt(norm);
    axes_={rotate(volume_,{1,0,0}),rotate(volume_,{0,1,0}),rotate(volume_,{0,0,1})};
    V extent{};for(size_t a=0;a<3;++a)for(size_t j=0;j<3;++j)extent[a]+=std::abs(axes_[j][a])*v.halfSize[j];minimum=sub(v.center,extent);maximum=add(v.center,extent);
    distanceScale_=2.f * *std::min_element(v.halfSize.begin(),v.halfSize.end());
    uint32_t state=(v.shapeSeed^seedDelta)^0xA511E9B3u;
    round_=lerp(.045f,.105f,random(state));taperX_=lerp(.035f,.09f,random(state));taperZ_=lerp(.035f,.09f,random(state));
    const uint32_t start=next(state)&7u,stride=(next(state)&3u)*2+1;
    for(uint32_t i=0;i<4;++i){const auto corner=(start+stride*i)&7u;V n{};for(uint32_t a=0;a<3;++a)n[a]=((corner&(1u<<a))?1.f:-1.f)*lerp(.72f,1,random(state));n=mul(n,1/std::sqrt(dot(n,n)));
        const float damaged=lerp(.56f,.5f,edgeDamage)+random(state)*lerp(.1f,.12f,edgeDamage);cuts_[i]={n[0],n[1],n[2],lerp(.74f,damaged,edgeDamage)};
    }
    state=(v.shapeSeed^seedDelta)^0xC13FA9A9u;wedgeSlope_=lerp(.85f,1.25f,random(state));wedgeOffset_=lerp(.18f,.32f,random(state));topX_=lerp(.12f,.26f,random(state));topZ_=lerp(.14f,.3f,random(state));
}
float CalibratedRockVolume::evaluate(V p)const{
    const auto relative=sub(p,volume_.center);for(size_t a=0;a<3;++a)p[a]=dot(relative,axes_[a])/(2*volume_.halfSize[a]);
    float upper=std::clamp((p[1]+.5f-.52f)/.48f,0.f,1.f);upper=upper*upper*(3-2*upper);
    const V half{.5f-taperX_*upper,.5f,.5f-taperZ_*upper};V q{},outside{};for(size_t a=0;a<3;++a){q[a]=std::abs(p[a])-half[a]+round_;outside[a]=std::max(q[a],0.f);}
    float value=std::sqrt(dot(outside,outside))+std::min(std::max({q[0],q[1],q[2]}),0.f)-round_;
    if(volume_.primitive==2)value=std::max(value,(p[0]+wedgeSlope_*p[1]-wedgeOffset_)/std::sqrt(1+wedgeSlope_*wedgeSlope_));
    if(volume_.primitive==1)for(size_t a:{0u,2u}){const float taper=.5f-(a==0?topX_:topZ_);value=std::max(value,(std::abs(p[a])+taper*p[1]-.5f*(1-taper))/std::sqrt(1+taper*taper));}
    for(const auto& c:cuts_)value=std::max(value,c[0]*p[0]+c[1]*p[1]+c[2]*p[2]-c[3]);
    return value*distanceScale_;
}
void relaxCalibratedRock(ModelData& mesh,float amount,float cellSize){
    if(amount<=.0001f||mesh.vertices.empty())return;
    std::vector<V> working,scratch(mesh.vertices.size());std::vector<std::vector<uint32_t>> neighbors(mesh.vertices.size());
    for(const auto& v:mesh.vertices)working.push_back(v.position);
    for(size_t i=0;i<mesh.indices.size();i+=3)for(size_t a=0;a<3;++a){auto& n=neighbors[mesh.indices[i+a]];n.push_back(mesh.indices[i+(a+1)%3]);n.push_back(mesh.indices[i+(a+2)%3]);}
    auto minimumY=[](const auto& points){float y=1e6f;for(auto p:points)y=std::min(y,p[1]);return y;};
    auto volume=[&](const auto& points){double v=0;for(size_t i=0;i<mesh.indices.size();i+=3)v+=dot(points[mesh.indices[i]],cross(points[mesh.indices[i+1]],points[mesh.indices[i+2]]))/6.;return std::abs(v);};
    const auto originalVolume=volume(working);const float originalY=minimumY(working),maxStep=std::max(.0001f,cellSize*.72f*amount);
    for(int i=0;i<1+int(std::round(amount*4));++i)for(float strength:{.42f*amount,-.44f*amount}){
        for(size_t j=0;j<working.size();++j){const auto& n=neighbors[j];if(n.empty()){scratch[j]=working[j];continue;}V average{};for(auto k:n)average=add(average,working[k]);
            V delta=mul(sub(mul(average,1.f/n.size()),working[j]),strength);const float length=std::sqrt(dot(delta,delta));if(length>maxStep)delta=mul(delta,maxStep/length);scratch[j]=add(working[j],delta);
        }working.swap(scratch);
    }
    const auto relaxedVolume=volume(working);if(originalVolume>1e-7&&relaxedVolume>1e-7){const float scale=std::clamp(float(std::cbrt(originalVolume/relaxedVolume)),.95f,1.05f);V center{};for(auto p:working)center=add(center,p);center=mul(center,1.f/working.size());for(auto& p:working)p=add(center,mul(sub(p,center),scale));}
    const float correction=originalY-minimumY(working);for(size_t i=0;i<working.size();++i){working[i][1]+=correction;mesh.vertices[i].position=working[i];}
}
}

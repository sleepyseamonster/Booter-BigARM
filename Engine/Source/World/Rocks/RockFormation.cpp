#include "World/Rocks/RockGenerator.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace engine {
std::vector<RockFormationMember> planComposedFormation(const RockRecipe&,const GeneratedId&);
RockResult generateComposedFormation(const RockRecipe&,const GeneratedId&,const std::function<float(float,float)>&);
namespace {
uint64_t mix(uint64_t x){x+=0x9e3779b97f4a7c15ULL;x=(x^(x>>30))*0xbf58476d1ce4e5b9ULL;x=(x^(x>>27))*0x94d049bb133111ebULL;return x^(x>>31);}
}
std::vector<RockFormationMember> planRockFormation(const RockRecipe& r,const GeneratedId& id,const std::function<float(float,float)>& ground){
    if(r.version==4)return planComposedFormation(r,id);
    validateRecipe(r);std::vector<RockFormationMember> plan;
    const float space=r.spacingMm*.001f;
    for(uint32_t i=0;i<r.members;++i){
        const auto random=mix(r.seed^mix(id.seed)^mix(id.member)^mix(i));const float angle=float(random%65536)/65536.f*6.2831853f;
        RockFormationMember p;p.id=id;p.id.generator="rock-member-"+std::to_string(id.member);p.id.member=i;p.scale=i==0?1.f:(.55f+float((random>>16)%1000)*.0004f);p.yaw=angle;
        if(r.formation==1)p.offset={float(i)*space*.65f,0,std::sin(float(i)*1.7f)*space*.3f};
        else if(r.formation==2){const float radius=std::sqrt(float(i))*space;p.offset={std::cos(angle)*radius,0,std::sin(angle)*radius};}
        else {const float radius=i==0?0:space*.4f;p.offset={std::cos(angle)*radius,0,std::sin(angle)*radius};}
        plan.push_back(p);
    }
    float cx=0,cz=0;for(const auto& p:plan){cx+=p.offset[0];cz+=p.offset[2];}cx/=plan.size();cz/=plan.size();
    for(auto& p:plan){p.offset[0]-=cx;p.offset[2]-=cz;const float h=ground(p.offset[0],p.offset[2]);if(!std::isfinite(h))throw std::runtime_error("Invalid formation terrain sample");p.offset[1]+=h;}
    return plan;
}
RockResult generateRockFormation(const RockRecipe& r,const GeneratedId& id,const std::function<float(float,float)>& ground){
    if(r.version==4)return generateComposedFormation(r,id,ground);
    auto single=r;single.formation=0;RockResult result;result.id=id.text();result.minimum={1e6f,1e6f,1e6f};result.maximum={-1e6f,-1e6f,-1e6f};
    for(auto member:planRockFormation(r,id,ground)){
        auto rock=generateVolumeRock(single,member.id);const float c=std::cos(member.yaw),s=std::sin(member.yaw);const uint32_t first=uint32_t(result.mesh.vertices.size());
        if(first==0){result.mesh=rock.mesh;result.mesh.vertices.clear();result.mesh.indices.clear();}
        // Seat on sampled support beneath the actual underside, rather than center height alone.
        float support=-1e6f;const float lift=member.offset[1]-ground(member.offset[0],member.offset[2]);
        for(const auto& v:rock.mesh.vertices){const auto& p=v.position;const float x=(c*p[0]+s*p[2])*member.scale+member.offset[0],z=(-s*p[0]+c*p[2])*member.scale+member.offset[2];const float h=ground(x,z);if(!std::isfinite(h))throw std::runtime_error("Invalid formation support sample");support=std::max(support,h-p[1]*member.scale);}
        member.offset[1]=support+lift;
        for(auto v:rock.mesh.vertices){const auto p=v.position,n=v.normal;v.position={(c*p[0]+s*p[2])*member.scale+member.offset[0],p[1]*member.scale+member.offset[1],(-s*p[0]+c*p[2])*member.scale+member.offset[2]};v.normal={c*n[0]+s*n[2],n[1],-s*n[0]+c*n[2]};const auto t=v.tangent;v.tangent={c*t[0]+s*t[2],t[1],-s*t[0]+c*t[2],t[3]};result.mesh.vertices.push_back(v);for(int a=0;a<3;++a){result.minimum[a]=std::min(result.minimum[a],v.position[a]);result.maximum[a]=std::max(result.maximum[a],v.position[a]);}result.footprintRadius=std::max(result.footprintRadius,std::hypot(v.position[0],v.position[2]));}
        for(auto index:rock.mesh.indices)result.mesh.indices.push_back(first+index);result.surfaces.insert(result.surfaces.end(),rock.surfaces.begin(),rock.surfaces.end());result.memberIds.push_back(member.id.text());
    }
    if(result.mesh.indices.size()>300000)throw std::runtime_error("Formation exceeds collision budget; reduce members or detail");
    return result;
}
}

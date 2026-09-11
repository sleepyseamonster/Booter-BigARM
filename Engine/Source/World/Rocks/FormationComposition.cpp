#include "World/Rocks/RockGenerator.h"
#include <algorithm>
#include <cmath>
#include <optional>
namespace engine {
namespace {
uint64_t mix(uint64_t x){x+=0x9e3779b97f4a7c15ULL;x=(x^(x>>30))*0xbf58476d1ce4e5b9ULL;x=(x^(x>>27))*0x94d049bb133111ebULL;return x^(x>>31);}
float random(uint64_t seed,uint32_t slot){return float(mix(seed+slot)>>40)/float(0xffffff);}
std::array<float,3> unit(std::array<float,3> p){const float n=std::hypot(std::hypot(p[0],p[1]),p[2]);for(auto& v:p)v/=std::max(n,1e-12f);return p;}
std::optional<float> topAt(const RockFormationMember& member,const RockResult& rock,float x,float z){
    std::optional<float> top;
    for(size_t i=0;i<rock.mesh.indices.size();i+=3){const auto a=formationPoint(member,rock.mesh.vertices[rock.mesh.indices[i]].position),b=formationPoint(member,rock.mesh.vertices[rock.mesh.indices[i+1]].position),c=formationPoint(member,rock.mesh.vertices[rock.mesh.indices[i+2]].position);
        const float den=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2]);if(std::abs(den)<1e-8f)continue;
        const float u=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den,v=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den;
        if(u>=-1e-5f&&v>=-1e-5f&&u+v<=1.00001f){const float y=u*a[1]+v*b[1]+(1-u-v)*c[1];if(!top||y>*top)top=y;}
    }return top;
}
}
std::array<float,3> formationPoint(const RockFormationMember& m,std::array<float,3> p){
    for(size_t i=0;i<3;++i)p[i]*=m.axes[i]*m.scale;const float c=std::cos(m.yaw),s=std::sin(m.yaw);
    return {m.offset[0]+c*p[0]+s*p[2],m.offset[1]+p[1],m.offset[2]-s*p[0]+c*p[2]};
}
std::vector<RockFormationMember> planComposedFormation(const RockRecipe& r,const GeneratedId& id){
    validateRecipe(r);const auto seed=mix(r.seed^mix(id.seed)^mix(uint64_t(id.region.x))^mix(uint64_t(id.region.z)+17)^mix(id.member));
    const auto kind=r.formation==5?uint32_t(id.member%4)+1:r.formation;
    const float radius=std::max(r.radiiMm[0],r.radiiMm[2])*.001f,heading=random(seed,0)*6.2831853f,space=r.spacingMm*.001f;
    std::vector<RockFormationMember> plan;
    const uint32_t baseCount=std::max(1u,(r.members+1)/2);
    for(uint32_t i=0;i<r.members;++i){RockFormationMember p;p.id=id;p.id.generator="rock-member-"+std::to_string(id.member);p.id.member=i;p.variant=uint32_t(mix(seed+i)%4);p.yaw=heading+(random(seed,i*7+1)-.5f)*1.5f;
        const float angle=heading+i*2.39996323f,jitter=.85f+random(seed,i*7+2)*.3f;
        if(kind==2){
            p.role=i<2?FormationRole::Core:(i<r.members/2+1?FormationRole::Slab:FormationRole::Fragment);
            const float size=i==0?1.05f:(i==1?.8f:(i<r.members/2+1?.55f:.25f));
            p.axes={size*jitter,size*(p.role==FormationRole::Slab?.48f:.8f),size/jitter};
            // Elliptical rings have deliberate open ground and never a straight cell row.
            const float distance=i==0?0:space*(1.0f+std::sqrt(float(i))*.65f);p.offset={std::cos(angle)*distance,0,std::sin(angle)*distance*.72f};
        }else if(kind==3){
            if(i<baseCount){p.role=FormationRole::Base;p.axes={1.12f*jitter,.42f,.94f};const float d=i==0?0:radius*1.15f;p.offset={std::cos(angle)*d,0,std::sin(angle)*d};}
            else {p.role=i==r.members-1?FormationRole::Cap:FormationRole::Middle;p.host=p.role==FormationRole::Cap&&i>baseCount?i-1:(i-baseCount)%baseCount;
                p.axes={.85f*jitter,p.role==FormationRole::Cap?.38f:.64f,.78f};p.offset=plan[p.host].offset;p.offset[0]+=radius*.12f*std::cos(angle);p.offset[2]+=radius*.12f*std::sin(angle);}
        }else if(kind==4){
            if(i<baseCount){p.role=FormationRole::Base;p.axes={1.25f,.44f,.74f};p.offset={(float(i)-float(baseCount-1)*.5f)*radius*1.55f,0,std::sin(float(i)*1.3f)*radius*.32f};}
            else {p.host=(i-baseCount)%baseCount;p.role=FormationRole::Pillar;p.axes={.40f,.86f,.42f};p.offset=plan[p.host].offset;}
        }else {
            if(i==0){p.role=FormationRole::Core;p.axes={1.05f,1.25f,.95f};}
            else if(i<3){p.role=FormationRole::Buttress;p.axes={.8f,.62f,.85f};const float a=heading+(i==1?1.05f:-1.05f);p.offset={std::cos(a)*radius*1.4f,0,std::sin(a)*radius*1.4f};}
            else if(i==3){p.role=FormationRole::Pillar;p.axes={.5f,1.05f,.55f};p.offset={-std::cos(heading)*radius,0,-std::sin(heading)*radius};}
            else {p.role=FormationRole::Talus;p.axes={.34f,.23f,.40f};p.offset={std::cos(angle)*radius*1.9f,0,std::sin(angle)*radius*1.9f};}
        }
        if(kind==4){const float c=std::cos(heading),s=std::sin(heading),x=p.offset[0],z=p.offset[2];p.offset[0]=c*x+s*z;p.offset[2]=-s*x+c*z;}
        plan.push_back(p);
    }
    // Layout 4 supported members inherit already rotated hosts; do not rotate twice.
    if(kind==4)for(auto& p:plan)if(p.host!=UINT32_MAX)p.offset=plan[p.host].offset;
    return plan;
}
void seatRockFormation(std::vector<RockFormationMember>& plan,const std::array<const RockResult*,4>& rocks,const std::function<float(float,float)>& ground){
    for(size_t i=0;i<plan.size();++i){auto& p=plan[i];if(p.variant>=rocks.size()||!rocks[p.variant])throw std::invalid_argument("Formation mesh variant unavailable");
        const auto& mesh=*rocks[p.variant];p.offset[1]=0;float support=-1e6f;
        for(const auto& v:mesh.mesh.vertices){const auto q=formationPoint(p,v.position);const float h=ground(q[0],q[2]);if(!std::isfinite(h))throw std::runtime_error("Formation terrain sample is nonfinite");support=std::max(support,h-q[1]);}
        const float burial=std::min(.12f,(mesh.maximum[1]-mesh.minimum[1])*p.axes[1]*p.scale*.08f);
        p.offset[1]=support-burial;
        if(p.host!=UINT32_MAX){if(p.host>=i)throw std::invalid_argument("Formation support must precede its member");const auto& host=plan[p.host];auto top=topAt(host,*rocks[host.variant],p.offset[0],p.offset[2]);
            if(!top){p.offset[0]=host.offset[0];p.offset[2]=host.offset[2];top=topAt(host,*rocks[host.variant],p.offset[0],p.offset[2]);}
            if(!top)throw std::runtime_error("Formation host has no support at contact");
            // The lower central surface meets the actual upper host triangle.
            auto inverted=p;inverted.offset[1]=0;inverted.axes[1]*=-1;
            auto bottom=topAt(inverted,mesh,p.offset[0],p.offset[2]);if(!bottom)throw std::runtime_error("Formation member has no underside support");
            p.offset[1]=std::max(p.offset[1],*top+*bottom-burial);
        }
    }
}
RockResult generateComposedFormation(const RockRecipe& r,const GeneratedId& id,const std::function<float(float,float)>& ground){
    auto single=r;single.formation=0;std::array<RockResult,4> rocks;std::array<const RockResult*,4> meshes;
    for(uint32_t i=0;i<4;++i){auto shapeId=id;shapeId.member=i;rocks[i]=generateVolumeRock(single,shapeId);meshes[i]=&rocks[i];}
    auto plan=planComposedFormation(r,id);seatRockFormation(plan,meshes,ground);
    RockResult result;result.id=id.text();result.mesh=rocks[0].mesh;result.mesh.vertices.clear();result.mesh.indices.clear();result.minimum={1e6f,1e6f,1e6f};result.maximum={-1e6f,-1e6f,-1e6f};
    for(const auto& p:plan){const auto& rock=rocks[p.variant];const uint32_t first=uint32_t(result.mesh.vertices.size());const float c=std::cos(p.yaw),s=std::sin(p.yaw);
        for(auto v:rock.mesh.vertices){v.position=formationPoint(p,v.position);auto n=v.normal;for(size_t a=0;a<3;++a)n[a]/=p.axes[a];v.normal=unit({c*n[0]+s*n[2],n[1],-s*n[0]+c*n[2]});
            const auto t=unit({c*v.tangent[0]*p.axes[0]+s*v.tangent[2]*p.axes[2],v.tangent[1]*p.axes[1],-s*v.tangent[0]*p.axes[0]+c*v.tangent[2]*p.axes[2]});v.tangent={t[0],t[1],t[2],1};result.mesh.vertices.push_back(v);
            for(size_t a=0;a<3;++a){result.minimum[a]=std::min(result.minimum[a],v.position[a]);result.maximum[a]=std::max(result.maximum[a],v.position[a]);}result.footprintRadius=std::max(result.footprintRadius,std::hypot(v.position[0],v.position[2]));}
        for(auto index:rock.mesh.indices)result.mesh.indices.push_back(first+index);
        for(size_t i=0;i<rock.mesh.indices.size();i+=3){const auto a=result.mesh.vertices[first+rock.mesh.indices[i]].position,b=result.mesh.vertices[first+rock.mesh.indices[i+1]].position,c0=result.mesh.vertices[first+rock.mesh.indices[i+2]].position;
            const std::array<float,3> u{b[0]-a[0],b[1]-a[1],b[2]-a[2]},v{c0[0]-a[0],c0[1]-a[1],c0[2]-a[2]};const auto n=unit({u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]});result.surfaces.push_back(n[1]>.65f?1:(n[1]<-.5f?2:0));}
        result.memberIds.push_back(p.id.text());
    }
    if(result.mesh.indices.size()>300000)throw std::runtime_error("Formation exceeds mesh budget; reduce detail or members");validateModel(result.mesh);return result;
}
}

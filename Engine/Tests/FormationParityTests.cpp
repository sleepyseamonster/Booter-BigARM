#include "World/Streaming/RegionStream.h"
#include "Game/WorldSession.h"
#include <chrono>
#include <cmath>
#include <iostream>
#include <set>
using namespace engine;
namespace {
void require(bool v,const char* message){if(!v)throw std::runtime_error(message);}
void ready(RegionStream& stream){const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(30);
    for(;;){stream.update({{{{},{32,0,32}},0}});bool done=stream.slots().size()==9;for(const auto& [_,slot]:stream.slots()){if(!slot.error.empty())throw std::runtime_error(slot.error);done=done&&slot.ready.collision;}
        if(done&&stream.jobStats().outstanding==0)return;if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Formation streaming timed out");std::this_thread::sleep_for(std::chrono::milliseconds(1));}}
}
int main(int argc,char** argv)try{
    if(argc!=4)throw std::runtime_error("Need terrain, formation recipe and new output directory");
    const auto terrain=loadTerrainRecipe(argv[1]);const auto recipe=loadRockRecipe(argv[2]);const std::filesystem::path out=argv[3];if(std::filesystem::exists(out))throw std::runtime_error("Output exists");std::filesystem::create_directories(out);
    require(terrain.version==3&&recipe.version==4,"Parity recipe versions");
    size_t pileSupports=0;
    for(uint32_t kind=1;kind<=4;++kind){auto r=recipe;r.formation=kind;const GeneratedId id{1049,4,{},7,"rock"};auto plan=planRockFormation(r,id,[](float,float){return 0.f;});
        require(plan.size()==r.members,"Member count lost");std::set<std::string> ids;for(size_t i=0;i<plan.size();++i){require(ids.insert(plan[i].id.text()).second&&plan[i].id!=id,"Child identity collision");if(plan[i].host!=UINT32_MAX){require(plan[i].host<i,"Cyclic support");if(kind==3)++pileSupports;}}
        const auto a=generateRock(r,id),b=generateRock(r,id);require(a.mesh.indices==b.mesh.indices&&a.mesh.vertices.size()==b.mesh.vertices.size(),"Formation determinism");
        for(size_t i=0;i<a.mesh.vertices.size();++i){require(a.mesh.vertices[i].position==b.mesh.vertices[i].position,"Reroll changed fixed seed");const auto n=a.mesh.vertices[i].normal;require(std::abs(std::hypot(std::hypot(n[0],n[1]),n[2])-1)<1e-4,"Scaled normal is not unit");}
        require(a.memberIds.size()==r.members&&a.surfaces.size()*3==a.mesh.indices.size(),"Export member/classification contract");
        saveRockRecipe(out/("kind-"+std::to_string(kind)+".json"),r);require(loadRockRecipe(out/("kind-"+std::to_string(kind)+".json"))==r,"V4 recipe roundtrip");
        if(kind==3)require(a.maximum[1]>recipe.radiiMm[1]*.001f,"Pile has no vertical tiers");
    }
    require(pileSupports>=2,"Pile has no support hierarchy");
    WorldConfiguration configuration{terrain,recipe,{}};WorldSession session(out/"profile",configuration);CalibrationRuntime runtime({},session.initial().player);std::map<RegionKey,BodyToken> bodies;
    RegionStream stream(terrain,recipe,{},[&](const RegionContent& c){const auto r=c.terrain.region;const RegionKey key{r.x,r.z};
        if(bodies.contains(key))runtime.physics().replaceMesh(bodies.at(key),c.collision);else bodies.emplace(key,runtime.physics().mesh(c.terrain.id.text(),WorldPosition{r,{}}.relativeTo({},256,4096),c.collision));return RegionReadiness{true,true};},[&](Region r){auto it=bodies.find({r.x,r.z});if(it!=bodies.end()){runtime.physics().remove(it->second);bodies.erase(it);}});
    ready(stream);const auto initial=stream.slots().at({0,0}).content;require(!initial->terrain.rocks.empty(),"No groups streamed");size_t members=0,index=initial->terrain.collision.size();std::set<std::string> childIds;
    for(const auto& group:initial->terrain.rocks){require(group.members.size()==recipe.members,"Streaming flattened or dropped formation");
        for(const auto& member:group.members){++members;require(childIds.insert(member.id.text()).second,"Groups share child identities");
            for(const auto& vertex:stream.rocks()[member.variant].collision){auto q=formationPoint(member,vertex);q[0]+=float(group.position.local[0]);q[2]+=float(group.position.local[2]);require(q==initial->collision.at(index++),"Render/member plan and collider differ");}
        }
    }require(index==initial->collision.size(),"Collider contains unplanned triangles");
    auto constraints=PlacementConstraints{};const auto group=initial->terrain.rocks.front();constraints.exclusions.push_back({"reserved-site",group.position,group.footprint});
    const auto excluded=generateTerrain(terrain,{},recipe,constraints);for(const auto& p:excluded.rocks)require(p.id!=group.id&&placementAllowed(constraints,p.position,p.footprint),"Whole-group exclusion ignored");
    require(stream.removeRock(group.id),"Formation removal rejected");ready(stream);session.save(runtime,.3f,.25f,7,stream.deltas());WorldSession restored(out/"profile",configuration);require(restored.initial().deltas.removed({},group.id.member),"Saved formation delta lost");
    stream.update({});require(bodies.empty(),"Retirement leaked colliders");ready(stream);
    for(const auto& p:stream.slots().at({0,0}).content->terrain.rocks){require(p.id!=group.id,"Removed formation returned");const auto before=std::find_if(initial->terrain.rocks.begin(),initial->terrain.rocks.end(),[&](const auto& old){return old.id==p.id;});require(before!=initial->terrain.rocks.end(),"Reload changed group ID");
        for(size_t i=0;i<p.members.size();++i)require(before->members[i].id==p.members[i].id&&before->members[i].offset==p.members[i].offset&&before->members[i].axes==p.members[i].axes,"Reload changed member plan");}
    writeDocument(out/"result.json","engine.formation-parity-tests",{{"passed",true},{"regions",bodies.size()},{"origin_members",members},{"pile_supports",pileSupports},{"resident_bytes",stream.residentBytes()},{"native_windows",false}});
    std::cout<<"PASS: four composition roles, supported tiers, deterministic v4 export/roundtrip, unit scaled normals, streamed member/collider agreement, whole-group exclusions and persisted removal/reload\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

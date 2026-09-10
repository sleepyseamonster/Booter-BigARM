#include "Persistence/WorldSave.h"
#include "World/Streaming/RegionStream.h"
#include <chrono>
#include <fstream>
#include <iostream>
using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F f){bool failed=false;try{f();}catch(const std::exception&){failed=true;}require(failed,"Expected save rejection");}
void awaitRegion(RegionStream& stream,size_t expectedCount){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(10);
    for(;;){stream.update({{{{},{32,0,32}},0}});const auto it=stream.slots().find({0,0});if(it!=stream.slots().end()&&it->second.content&&it->second.content->terrain.rocks.size()==expectedCount)return;
        for(const auto& [_,slot]:stream.slots())if(!slot.error.empty())throw std::runtime_error(slot.error);
        if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Delta regeneration timed out");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
}
}
int main(int argc,char** argv)try{
    if(argc!=2)throw std::runtime_error("Need new output directory");const std::filesystem::path root=argv[1];if(std::filesystem::exists(root))throw std::runtime_error("Choose a new test directory");std::filesystem::create_directories(root);
    WorldSave state;state.player.feet={2,0,3};state.player.markerActive=true;state.deltas.removedRocks[{-1,0}]={4,9};state.origin={12,-13};
    const auto profile=root/"world";auto generation=saveWorld(profile,state,0);require(generation==1,"Initial generation");auto loaded=loadWorldSave(profile,state.configuration);require(loaded.value&&loaded.value->deltas==state.deltas&&loaded.value->player.feet==state.player.feet&&loaded.value->player.markerActive&&loaded.value->origin==state.origin,"World/player/config/delta roundtrip");
    auto changed=state;changed.player.feet={7,0,8};changed.player.markerActive=false;changed.deltas.removedRocks[{0,0}]={16};
    // Fail after player write: no generation may mix the new player with old deltas.
    rejects([&]{saveWorld(profile,changed,generation,[](unsigned step){if(step==4)throw std::runtime_error("Injected interrupted save");});});
    loaded=loadWorldSave(profile,state.configuration);require(loaded.generation==1&&loaded.recovered&&loaded.value->player.feet==state.player.feet&&loaded.value->deltas==state.deltas,"Interrupted cohort became visible");
    generation=saveWorld(profile,changed,1);require(generation==3,"Recovery did not skip interrupted generation");loaded=loadWorldSave(profile,state.configuration);require(loaded.value->player.feet==changed.player.feet&&loaded.value->deltas==changed.deltas,"New committed cohort mixed states");
    rejects([&]{saveWorld(profile,state,1);});
    // OS writer lease is released after a failed save; an overlapping writer is rejected.
    bool leaseRejected=false;generation=saveWorld(profile,changed,generation,[&](unsigned step){if(step==1){try{saveWorld(profile,state,generation);}catch(const std::exception&){leaseRejected=true;}}});require(leaseRejected,"Overlapping save writer accepted");
    std::ofstream(profile/("world-gen-"+std::to_string(generation))/"deltas.json",std::ios::trunc)<<"{}";
    loaded=loadWorldSave(profile,state.configuration);require(loaded.recovered&&loaded.generation==3&&loaded.value->deltas==changed.deltas,"Corrupt committed member did not recover complete older generation");
    auto incompatible=state.configuration;++incompatible.terrain.seed;rejects([&]{loadWorldSave(profile,incompatible);});
    generation=saveWorld(profile,changed,loaded.generation);const auto dir=profile/("world-gen-"+std::to_string(generation));auto manifest=readDocument(dir/"commit.json","engine.world-generation");manifest["content_version"]=2;writeDocument(dir/"commit.json","engine.world-generation",manifest);
    bool compatibility=false;try{loadWorldSave(profile,state.configuration);}catch(const DocumentCompatibilityError&){compatibility=true;}require(compatibility,"Future content version silently fell back to older save");
    rejects([&]{saveWorld(profile,state,3);});
    const auto invalid=root/"invalid";saveWorld(invalid,state,0);std::ofstream(invalid/"world-gen-1/player.json",std::ios::trunc)<<"{}";rejects([&]{loadWorldSave(invalid,state.configuration);});rejects([&]{saveWorld(invalid,state,0);});
    const auto legacy=root/"legacy";saveSnapshot(legacy,state.player);rejects([&]{loadWorldSave(legacy,state.configuration);});
    // Shared generation path applies stable tombstones to geometry and aggregate collision.
    PhysicsWorld physics;std::map<DeltaRegion,BodyToken> bodies;
    auto attach=[&](const RegionContent& c){const auto r=c.terrain.region;const auto key=DeltaRegion{r.x,r.z};if(bodies.contains(key))physics.replaceMesh(bodies.at(key),c.collision);else bodies[key]=physics.mesh(c.terrain.id.text(),WorldPosition{r,{}}.relativeTo({},256,4096),c.collision);return RegionReadiness{true,true};};
    auto detach=[&](Region r){auto it=bodies.find({r.x,r.z});if(it!=bodies.end()){physics.remove(it->second);bodies.erase(it);}};
    const auto count=generateTerrain({}, {}, {}, {}).rocks.size();WorldDeltas delta;
    {
        RegionStream stream({}, {}, {},attach,detach);awaitRegion(stream,count);const auto rock=stream.slots().at({0,0}).content->terrain.rocks.front();const auto before=stream.slots().at({0,0}).content->collision.size();const auto token=bodies.at({0,0});
        const auto x=float(rock.position.local[0]),z=float(rock.position.local[2]);const auto hit=physics.ray({x,30,z},{0,-60,0});require(hit&&hit->position[1]>rock.position.local[1]+1,"Rock collision oracle missing");
        require(stream.removeRock(rock.id)&&!stream.removeRock(rock.id),"Tombstone not idempotent");awaitRegion(stream,count-1);
        require(bodies.at({0,0})==token&&stream.slots().at({0,0}).content->collision.size()==before-stream.rocks()[rock.id.member%4].collision.size(),"Delta rebuild changed body identity or retained rock collision");
        const auto after=physics.ray({x,30,z},{0,-60,0});require(after&&std::abs(after->position[1]-rock.position.local[1])<.001,"Removed rock still collides");delta=stream.deltas();stream.update({});awaitRegion(stream,count-1);require(stream.deltas()==delta,"Unload/reload lost tombstone");
    }
    require(physics.size()==0,"Delta stream teardown leaked colliders");state.origin={};state.deltas=delta;const auto restart=root/"restart";saveWorld(restart,state,0);loaded=loadWorldSave(restart,state.configuration);
    {RegionStream stream({}, {}, {},attach,detach,loaded.value->deltas);awaitRegion(stream,count-1);require(stream.deltas()==delta,"Saved tombstone lost after restart");}
    require(physics.size()==0,"Restart teardown leaked colliders");
    std::cout<<"PASS: atomic world/player/delta cohorts, interruption and corruption recovery, writer/stale-session rejection, compatibility preservation; rock removal updates stable collision and survives unload/restart\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

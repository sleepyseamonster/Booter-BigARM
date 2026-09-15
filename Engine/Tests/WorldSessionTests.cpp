#include "Game/WorldSession.h"
#include "World/Streaming/RegionStream.h"
#include <chrono>
#include <cmath>
#include <iostream>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected incompatible or stale session rejection");}
void ready(RegionStream& stream,WorldPosition anchor,size_t expected){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(10);
    for(;;){
        stream.update({{anchor,0}});
        const auto it=stream.slots().find({0,0});
        if(it!=stream.slots().end()&&it->second.ready.collision&&it->second.content->terrain.rocks.size()==expected)return;
        for(const auto& [_,slot]:stream.slots())if(!slot.error.empty())throw std::runtime_error(slot.error);
        if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Session region timed out");
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Usage: engine_world_session_tests create|restore new-output-directory");
    const std::string mode=argv[1];const std::filesystem::path root=argv[2];
    if(mode!="create"&&mode!="restore")throw std::runtime_error("Unknown check mode");
    if(mode=="create"){std::error_code ignored;std::filesystem::remove_all(root,ignored);}
    WorldConfiguration config;config.terrain.amplitudeMm=0;
    const auto patch=generateTerrain(config.terrain,{},config.rock,config.constraints);
    require(!patch.rocks.empty(),"Missing rock fixture");const auto rock=patch.rocks.front();
    const auto position=rock.position.relativeTo({},256,4096);const auto profile=root/"world";
    if(mode=="create"){
        require(!std::filesystem::exists(root),"Create needs a new output directory");
        WorldSession fresh(profile,config);require(!fresh.restored()&&fresh.generation()==0&&fresh.initial().player.feet[1]>.0f,"Fresh terrain spawn/session incorrect");
        auto seed=fresh.initial();seed.player.feet={position[0]+1,4,position[2]};seed.player.markerActive=true;seed.player.ticks=42;
        saveWorld(profile,seed,0);
    }
    WorldSession session(profile,config),stale(profile,config);
    require(session.restored()&&session.generation()==(mode=="create"?1:2),"Wrong restored generation");
    CalibrationRuntime runtime({},session.initial().player);
    require(runtime.feet()==PhysicsVector{position[0]+1,4,position[2]}&&runtime.targetActive()&&runtime.clock().ticks()==42,"Application runtime did not restore player state");
    std::map<RegionKey,BodyToken> bodies;
    auto attach=[&](const RegionContent& c){
        const auto key=RegionKey{c.terrain.region.x,c.terrain.region.z};
        if(bodies.contains(key))runtime.physics().replaceMesh(bodies.at(key),c.collision);
        else bodies[key]=runtime.physics().mesh(c.terrain.id.text(),WorldPosition{c.terrain.region,{}}.relativeTo({},256,4096),c.collision);
        return RegionReadiness{true,true};
    };
    auto detach=[&](Region r){const auto key=RegionKey{r.x,r.z};if(bodies.contains(key)){runtime.physics().remove(bodies.at(key));bodies.erase(key);}};
    {
        const auto& initial=session.initial();
        RegionStream stream(initial.configuration.terrain,initial.configuration.rock,initial.configuration.constraints,attach,detach,initial.deltas);
        const auto expected=patch.rocks.size()-(mode=="restore"?1:0);ready(stream,rock.position,expected);
        if(mode=="create"){
            require(stream.removeRock(rock.id),"Application edit failed");
            // Save immediately, before the asynchronous visual/collision replacement completes.
            session.save(runtime,1.1f,.4f,6,stream.deltas());
            require(session.generation()==2,"Session did not track successful save generation");
            rejects([&]{stale.save(runtime,0,.3f,5,stream.deltas());});
            require(stale.generation()==1,"Rejected save advanced stale session generation");
            ready(stream,rock.position,patch.rocks.size()-1);
        } else {
            require(initial.deltas.removed(rock.id.region,rock.id.member)&&initial.player.cameraYaw==1.1f&&initial.player.cameraPitch==.4f&&initial.player.cameraDistance==6,"Restart lost world edit/camera");
            stream.update({});ready(stream,rock.position,patch.rocks.size()-1);
        }
        const auto hit=runtime.physics().ray({position[0],30,position[2]},{0,-60,0});
        require(hit&&std::abs(hit->position[1])<.001f,"Saved tombstone retained rock collision");
    }
    require(bodies.empty(),"Session teardown leaked region colliders");
    if(mode=="restore"){
        auto mismatch=config;++mismatch.terrain.seed;rejects([&]{WorldSession bad(profile,mismatch);});
        auto shifted=session.initial();shifted.origin={1,0};saveWorld(root/"shifted",shifted,0);rejects([&]{WorldSession bad(root/"shifted",config);});
        saveSnapshot(root/"legacy",{});rejects([&]{WorldSession bad(root/"legacy",config);});
        session.save(runtime,1.1f,.4f,6,session.initial().deltas);
    }
    writeDocument(root/(mode+".json"),"engine.world-session-check",{{"passed",true},{"mode",mode},{"generation",session.generation()},{"removed_id",rock.id.text()},{"feet",runtime.feet()},{"ticks",runtime.clock().ticks()}});
    std::cout<<"PASS: "<<mode<<" shared application world session, player/camera/delta cohort, real collision and profile guards\n";
    return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

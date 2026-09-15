#include "Game/CalibrationRuntime.h"
#include "Persistence/PlayerSnapshot.h"
#include <cmath>
#include <fstream>
#include <iostream>
using namespace engine;

// Observations, not regression gates: reproduced_issue=true records broken behavior.
namespace {
std::vector<PhysicsVector> plane(float y) {
    return {{-5,y,-5},{-5,y,5},{5,y,-5},{5,y,-5},{-5,y,5},{5,y,5}};
}
void steps(PhysicsWorld& world, unsigned count) {
    for(unsigned i=0;i<count;++i){world.step(float(FixedClock::step));world.takeContacts();}
}
Json sleepingSupport(bool remove) {
    PhysicsWorld world;
    const auto support=world.mesh("authored:audit:support",{},plane(0));
    const auto body=world.capsule("authored:audit:dynamic",{0,3,0},.25f,.5f,true);
    steps(world,360);
    const float settled=world.position(body)->at(1);
    if(std::abs(settled-.75f)>.05f)throw std::runtime_error("Support fixture failed to settle");
    if(remove)world.remove(support);else world.replaceMesh(support,plane(-3));
    const auto surface=world.ray({3,1,0},{0,-8,0});
    if(remove?bool(surface):(!surface||std::abs(surface->position[1]+3)>.01f))
        throw std::runtime_error("Support fixture was not changed");
    steps(world,120);
    const float after=world.position(body)->at(1);
    world.velocity(body,{0,0,0}); // Existing public API explicitly activates this body.
    steps(world,120);
    const float awakened=world.position(body)->at(1);
    return {{"settled_y",settled},{"after_support_edit_y",after},{"after_explicit_wake_y",awakened},
        {"reproduced_issue",std::abs(after-settled)<.001f&&awakened<settled-1}};
}
Json futureSave(const std::filesystem::path& root) {
    PlayerSnapshot first;first.ticks=1;saveSnapshot(root,first);
    auto second=first;second.ticks=2;saveSnapshot(root,second);
    const auto file=root/"snapshot-b.json";
    auto payload=readDocument(file,"engine.player-snapshot");
    payload["future_only_state"]="must survive older engine";
    writeDocument(file,"engine.player-snapshot",payload,2);
    const auto recovered=loadSnapshot(root);
    saveSnapshot(root,first);
    Json overwritten;std::ifstream(file)>>overwritten;
    return {{"loaded_generation",recovered.generation},{"reported_recovered",recovered.recovered},
        {"version_after_save",overwritten.at("version")},
        {"future_state_retained",overwritten.at("payload").contains("future_only_state")},
        {"reproduced_issue",overwritten.at("version")==1&&!overwritten.at("payload").contains("future_only_state")}};
}
Json pausePresentation() {
    CalibrationRuntime runtime;Actions actions;float yaw=0,pitch=.3f;
    actions.set(Control::D,1);
    runtime.advance(FixedClock::step*1.5,false,actions,yaw,pitch);
    const auto before=runtime.present(yaw,pitch,5,0).feet;
    const auto physicalBefore=runtime.feet();const auto ticks=runtime.clock().ticks();
    runtime.advance(FixedClock::step,true,actions,yaw,pitch);
    const auto paused=runtime.present(yaw,pitch,5,0).feet;
    const auto physicalAfter=runtime.feet();
    return {{"before_pause",before},{"paused",paused},{"physics_before",physicalBefore},
        {"physics_after",physicalAfter},{"ticks_before",ticks},{"ticks_after",runtime.clock().ticks()},
        {"reproduced_issue",std::abs(before[0]-paused[0])>.001f&&physicalBefore==physicalAfter&&ticks==runtime.clock().ticks()}};
}
Json guardRemoval() {
    CalibrationRuntime runtime;
    runtime.streamingGuard([](PhysicsVector,PhysicsVector){return true;});
    BodyToken last;
    while(runtime.physics().size()<2048) {
        const auto index=runtime.physics().size();
        last=runtime.physics().box("authored:audit:capacity:"+std::to_string(index),{100,100,100},{.1f,.1f,.1f});
    }
    std::string error;
    try{runtime.streamingGuard({});}catch(const std::exception& e){error=e.what();}
    const auto atFailure=runtime.physics().size();
    runtime.physics().remove(last);
    runtime.streamingGuard({});
    return {{"body_count_at_failure",atFailure},{"error",error},{"succeeds_after_freeing_one_body",true},
        {"reproduced_issue",error=="Physics body budget exhausted"}};
}
}
int main(int argc,char** argv)try {
    if(argc!=2)throw std::invalid_argument("Expected a new scratch profile path");
    const std::filesystem::path root=argv[1];
    if(std::filesystem::exists(root))throw std::invalid_argument("Scratch profile already exists");
    Json result;
    result["future_snapshot_overwrite"]=futureSave(root);
    result["sleeping_support_replaced"]=sleepingSupport(false);
    result["sleeping_support_removed"]=sleepingSupport(true);
    result["pause_presentation_rewind"]=pausePresentation();
    result["guard_removal_at_capacity"]=guardRemoval();
    std::cout<<result.dump(2)<<'\n';
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

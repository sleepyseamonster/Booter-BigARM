#include "Physics/PhysicsWorld.h"
#include "Physics/CharacterController.h"
#include "Game/ThirdPersonCamera.h"
#include "Game/Locomotion.h"
#include "Simulation/FixedClock.h"
#include <algorithm>
#include <cmath>
#include <iostream>
#include <stdexcept>
using namespace engine;
namespace {
void require(bool value,const char* message) {if(!value) throw std::runtime_error(message);}
template<class F> void rejects(F&& f) {bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected invalid physics candidate to fail");}
void tick(PhysicsWorld& w,CharacterController& c,PhysicsVector v={0,0,0},bool jump=false) {c.step(float(FixedClock::step),v,jump);w.step(float(FixedClock::step));w.takeContacts();}
void floor(PhysicsWorld& w,float half=20) {w.box("authored:floor",{0,-.5f,0},{half,.5f,half});}
}
int main() try {
    {
        PhysicsWorld world;floor(world);
        const auto ray=world.ray({0,5,0},{0,-10,0});
        require(ray && ray->stableId=="authored:floor" && std::abs(ray->fraction-.5f)<.001f && ray->normal[1]>.99f,"Ray hit fraction and normal");
        require(world.capsuleOverlap({0,.5f,0},.35f,.55f) && !world.capsuleOverlap({0,4,0},.35f,.55f),"Capsule overlap/clear cases");
        const auto wall=world.box("authored:wall",{0,1,0},{.1f,1,2});
        const auto sweep=world.sphereSweep({-3,1,0},.2f,{6,0,0});
        require(sweep && sweep->body==wall && sweep->fraction>.44f && sweep->fraction<.46f,"Sphere sweep stops at radius clearance");
        world.mesh("authored:mesh",{25,0,0},{{-2,0,-2},{0,1,2},{2,0,-2}});
        const auto meshHit=world.ray({25,3,0},{0,-6,0});require(meshHit && meshHit->stableId=="authored:mesh" && std::abs(meshHit->position[1]-.5f)<.01f,"Triangle mesh query");
        world.heightfield("authored:heightfield",{30,0,0},8,1,std::vector<float>(64,1));
        const auto terrain=world.ray({32,5,2},{0,-10,0});require(terrain && terrain->stableId=="authored:heightfield" && std::abs(terrain->position[1]-1)<.01f,"Heightfield query");
        auto capsule=world.capsule("authored:falling",{-4,3,0},.25f,.5f,true);bool added=false,removed=false;
        for(int i=0;i<180;++i) {world.step(float(FixedClock::step));for(const auto& event:world.takeContacts()) {
            require(event.idA<=event.idB,"Stable canonical contact identity order");
            added|=event.kind==ContactEvent::Kind::Added && (event.idA=="authored:falling" || event.idB=="authored:falling");
        }}
        require(added && std::abs(world.position(capsule)->at(1)-.75f)<.04f,"Dynamic capsule settles and emits contact");
        world.velocity(capsule,{0,0,0});world.step(float(FixedClock::step));world.takeContacts();
        world.remove(capsule);world.step(float(FixedClock::step));
        for(const auto& event:world.takeContacts()) removed|=event.kind==ContactEvent::Kind::Removed && (event.idA=="authored:falling"||event.idB=="authored:falling");
        require(removed,"Removal callback retains stable identity after body destruction");
        require(!world.position(capsule),"Removed body token expires");
        auto replacement=world.capsule("authored:falling",{-4,3,0},.25f,.5f,true);
        require(replacement!=capsule && !world.remove(capsule),"Reused stable identity has new lifetime");
        rejects([&]{world.velocity(capsule,{1,0,0});});rejects([&]{world.velocity(wall,{1,0,0});});
        rejects([&]{world.ray({0,0,0},{0,0,0});});rejects([&]{world.box("authored:nan",{NAN,0,0},{1,1,1});});
        rejects([&]{world.mesh("authored:bad",{0,0,0},{{0,0,0},{0,0,0},{1,1,1}});});
        const auto count=world.size();rejects([&]{world.heightfield("authored:bad",{0,0,0},4,1,std::vector<float>(16,0));});require(world.size()==count,"Invalid geometry retains body set");
    }
    {
        PhysicsWorld world;floor(world);world.box("authored:step",{2,.125f,0},{.6f,.125f,2});world.box("authored:blocker",{5,1,0},{.25f,1,2});
        CharacterController actor(world,"authored:actor",{0,0,0});float peak=0;
        for(int i=0;i<140;++i) {tick(world,actor,{3,0,0});peak=std::max(peak,actor.position()[1]);}
        std::cout<<"step peak="<<peak<<" stop x="<<actor.position()[0]<<'\n';
        require(peak>.2f && actor.position()[0]>3.5f && actor.position()[0]<4.5f,"Character traverses low step and stops at tall blocker");
        require(actor.grounded(),"Character ground support");
        tick(world,actor,{0,0,0},true);peak=0;
        for(int i=0;i<90;++i) {tick(world,actor);peak=std::max(peak,actor.position()[1]);}
        require(peak>1 && actor.grounded() && std::abs(actor.position()[1])<.1f,"Jump arc and landing");
        require(!world.remove(actor.body()),"Character owns query body removal");
    }
    for(bool steep:{false,true}) {
        PhysicsWorld world;floor(world);const float end=steep?2.0f:4.0f,height=steep?4.0f:2.0f;
        world.mesh("authored:ramp",{0,0,0},{{0,0,-2},{0,0,2},{end,height,-2},{end,height,-2},{0,0,2},{end,height,2}});
        CharacterController actor(world,"authored:actor",{-1,0,0});
        for(int i=0;i<80;++i) tick(world,actor,{3,0,0});
        std::cout<<"slope steep="<<steep<<" x="<<actor.position()[0]<<" y="<<actor.position()[1]<<'\n';
        require(steep?(actor.position()[0]<.7f && actor.position()[1]<.6f):(actor.position()[0]>2 && actor.position()[1]>1),"Walkable and rejected slope cases");
    }
    {
        PhysicsWorld world;floor(world,1);CharacterController actor(world,"authored:actor",{0,0,0});
        for(int i=0;i<100;++i) tick(world,actor,{3,0,0});
        require(!actor.grounded() && actor.position()[1]<-2,"Ground loss falls rather than hovering");
    }
    {
        PhysicsWorld world;floor(world);CharacterController actor(world,"authored:actor",{0,0,0});
        const auto query=world.ray({0,1,-3},{0,0,6});require(query && query->body==actor.body(),"Character inner body visible to ordinary queries");
        require(world.capsuleOverlap({0,1,0},.1f,.1f) && !world.capsuleOverlap({0,1,0},.1f,.1f,actor.body()),"Self exclusion query");
        const auto wall=world.box("authored:camera-wall",{0,1.5f,2},{2,2,.2f});ThirdPersonCamera camera;
        const auto blocked=camera.update(world,{0,0,0},0,0,5,1.0f/60,actor.body());
        require(blocked.obstructed && blocked.eye[2]>1.4f && blocked.eye[2]<1.61f,"Camera keeps sphere clearance before wall");
        world.remove(wall);const auto clear=camera.update(world,{0,0,0},0,0,5,.1f,actor.body());
        require(!clear.obstructed && clear.distance>blocked.distance && clear.distance<5,"Camera recovers outward without snapping");
    }
    float reference=0;
    for(unsigned rate:{30,60,144}) {
        PhysicsWorld world;floor(world);CharacterController actor(world,"authored:actor",{0,0,0});FixedClock clock;
        for(unsigned i=0;i<rate*2;++i) clock.advance(1.0/rate,false,[&](double,uint64_t){tick(world,actor,{2,0,0});});
        if(!reference) reference=actor.position()[0];require(std::abs(actor.position()[0]-reference)<.0001f,"Character results independent of render rate");
    }
    {
        PhysicsWorld small(1);floor(small);rejects([&]{small.capsule("authored:excess",{0,3,0},.2f,.4f);});
        ActionFrame actions{};actions[size_t(Action::MoveX)].value=1;actions[size_t(Action::MoveZ)].value=-1;
        const auto intent=locomotionIntent(actions,0);require(std::abs(std::hypot(intent.velocity[0],intent.velocity[2])-3)<.001f,"Diagonal locomotion speed normalized");
    }
    std::cout<<"PASS: Jolt bodies/queries/contact lifetime, capsule slopes/steps/jump/ground loss, camera obstruction and fixed-rate motion\n";
    return 0;
} catch(const std::exception& error) {std::cerr<<error.what()<<'\n';return 1;}

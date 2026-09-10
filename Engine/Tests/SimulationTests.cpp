#include "Simulation/Actions.h"
#include "Simulation/FixedClock.h"
#include "Simulation/World.h"
#include "Persistence/Document.h"
#include <cmath>
#include <filesystem>
#include <iostream>
#include <stdexcept>
using namespace engine;
namespace {
void require(bool condition,const char* message) {if(!condition) throw std::runtime_error(message);}
template<class F> void rejects(F&& f) {bool rejected=false;try {f();}catch(const std::exception&) {rejected=true;}require(rejected,"Expected rejection");}
WorldCommand velocity(uint64_t sequence,EntityToken target,double x) {WorldCommand c;c.sequence=sequence;c.target=target;c.velocity={x,0,0};return c;}
}
int main(int argc,char** argv) try {
    if(argc!=2) throw std::runtime_error("Expected temporary output path");
    const std::filesystem::path root=argv[1];std::filesystem::create_directories(root);
    for(unsigned rate:{30,60,144}) {
        FixedClock clock;World world;const auto actor=world.create("authored:rate:actor");world.enqueue(velocity(1,actor,3));
        for(unsigned frame=0;frame<rate*2;++frame) clock.advance(1.0/rate,false,[&](double dt,uint64_t){world.step(dt);});
        require(clock.ticks()==120 && std::abs(world.snapshot(actor)->current.position.local[0]-6)<1e-9,"Fixed results across render rates");
    }
    FixedClock clock;unsigned calls=0;
    clock.advance(.01,false,[&](double,uint64_t){++calls;});
    clock.advance(5,true,[&](double,uint64_t){++calls;});
    require(calls==0 && clock.alpha()==0,"Pause clears residual time");
    clock.advance(10,false,[&](double,uint64_t){++calls;});
    require(calls==4 && clock.overloadedFrames()==1 && clock.droppedSeconds()>9.9,"Bounded catch-up accounts for lost time");
    rejects([&]{clock.advance(NAN,false,[](double,uint64_t){});});
    Actions input;
    input.set(Control::Space,1);input.set(Control::Space,0);
    auto tick=input.takeTick();require(tick[size_t(Action::Jump)].pressed && tick[size_t(Action::Jump)].released,"Short tap survives between ticks");
    require(!input.takeTick()[size_t(Action::Jump)].pressed,"Tap delivered once across catch-up ticks");
    input.set(Control::W,1);require(input.peek()[size_t(Action::MoveZ)].value==-1,"Default movement action");
    input.focus(false);input.focus(true);require(input.takeTick()[size_t(Action::MoveZ)].value==0,"Focus transition inhibits held movement");
    input.set(Control::W,0);input.set(Control::W,1);require(input.takeTick()[size_t(Action::MoveZ)].value==-1,"Neutral then press rearms movement");
    input.gameplay(false);input.set(Control::Space,1);require(!input.takeTick()[size_t(Action::Jump)].pressed,"UI context blocks gameplay");
    input.set(Control::Enter,1);require(input.takeTick()[size_t(Action::Confirm)].pressed,"UI context gets confirm");
    input.set(Control::P,1);require(input.consume(Action::Pause).pressed,"System pause works in UI context");
    input.gameplay(true);require(input.takeTick()[size_t(Action::Jump)].value==0,"UI-held button cannot become jump");
    input.set(Control::Space,0);input.set(Control::PadSouth,1);input.disconnectGamepad();
    require(!input.takeTick()[size_t(Action::Jump)].pressed && input.peek()[size_t(Action::Jump)].value==0,"Disconnect cancels pending and held input");
    input.connectGamepad();input.set(Control::PadSouth,1);
    require(!input.takeTick()[size_t(Action::Jump)].pressed,"Hotplug-held button requires neutral");
    input.set(Control::PadSouth,0);input.set(Control::PadSouth,1);
    require(input.takeTick()[size_t(Action::Jump)].pressed,"Neutral arms newly connected controller");
    input.set(Control::W,1);input.replaceBindings({{Action::Jump,Control::W,1}});
    require(!input.takeTick()[size_t(Action::Jump)].pressed,"Remapping cannot synthesize held press");
    input.set(Control::W,0);input.set(Control::W,1);require(input.takeTick()[size_t(Action::Jump)].pressed,"Remapped press works");
    saveBindings(root/"bindings.json",input);Actions loaded;loadBindings(root/"bindings.json",loaded);
    loaded.set(Control::W,1);require(loaded.takeTick()[size_t(Action::Jump)].pressed,"Bindings persist by named actions/controls");
    auto bad=readDocument(root/"bindings.json","engine.input-bindings");bad["bindings"][0]["control"]="unknown";
    writeDocument(root/"bindings.json","engine.input-bindings",bad);rejects([&]{loadBindings(root/"bindings.json",loaded);});
    require(loaded.bindings().size()==1,"Bad binding document retains previous bindings");
    World world(16,4);const auto id=GeneratedId{7,1,{-1,0},2,"test"}.text();
    const auto actor=world.create(id,{{{-1,0},{15.99,0,0}},0});
    rejects([&]{world.create(id);});
    world.enqueue(velocity(2,actor,3));world.enqueue(velocity(1,actor,1));world.step(FixedClock::step);
    auto pose=world.snapshot(actor);
    require(pose->current.position.region.x==0 && std::abs(pose->current.position.local[0]-.04)<1e-9,"Ordered commands and normalized region crossing");
    require(pose->previous.position.region.x==-1,"Interpolation retains previous region");
    rejects([&]{world.enqueue(velocity(2,actor,0));});
    const auto foreign=World{}.snapshot(actor);require(!foreign,"Foreign world rejects token");
    world.remove(actor);const auto replacement=world.create(id);
    world.enqueue(velocity(3,actor,99));world.step(FixedClock::step);
    require(!world.snapshot(actor) && replacement!=actor && world.snapshot(replacement)->velocity[0]==0 && world.rejectedCommands()==1,"Stale commands cannot affect reloaded identity");
    world.create("authored:z");world.create("authored:a");
    const auto ordered=world.ordered();require(ordered[0].id=="authored:a" && ordered[1].id=="authored:z","Stable identity iteration independent of creation order");
    require(world.unloadRegion({0,0})==3 && !world.find(id),"Region unload removes live lookup and entities");
    const auto reloaded=world.create(id);require(reloaded!=replacement,"Reload preserves stable ID with fresh lifetime");
    for(uint64_t i=4;i<1028;++i) world.enqueue(velocity(i,reloaded,0));
    rejects([&]{world.enqueue(velocity(1028,reloaded,0));});
    std::cout<<"PASS: fixed clock, action lifecycle, named bindings, ordered ECS commands, region unload and stale lifetimes\n";
    return 0;
} catch(const std::exception& error) {std::cerr<<error.what()<<'\n';return 1;}

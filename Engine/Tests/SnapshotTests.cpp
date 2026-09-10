#include "Persistence/PlayerSnapshot.h"
#include "Game/CalibrationRuntime.h"
#include <fstream>
#include <cmath>
#include <iostream>
using namespace engine;
void require(bool c,const char* m){if(!c)throw std::runtime_error(m);}
template<class F>void rejects(F&& f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected snapshot rejection");}
int main(int argc,char** argv)try {
    if(argc!=2)throw std::runtime_error("Expected new test directory");const std::filesystem::path root=argv[1];
    if(std::filesystem::exists(root))throw std::runtime_error("Choose a new snapshot test directory");
    require(!loadSnapshot(root).value,"Missing profile starts fresh");
    PlayerSnapshot first;first.feet={1,2,-1};first.velocity={0,-1,0};first.yaw=.7f;first.markerActive=true;first.ticks=120;
    saveSnapshot(root,first);const auto loaded=loadSnapshot(root);require(loaded.value&&loaded.generation==1&&!loaded.recovered,"First generation loads");
    CalibrationRuntime restarted({},*loaded.value);const auto restored=restarted.snapshot(first.cameraYaw,first.cameraPitch,first.cameraDistance);
    require(restored.feet==first.feet&&restored.velocity==first.velocity&&restored.markerActive&&restored.ticks==first.ticks,"Runtime restart restores authoritative pose, velocity, state and time");
    require(restarted.world().find("authored:calibration:proxy").has_value()&&restarted.takeCues().empty(),"Restart preserves identity without replaying cues");
    PlayerSnapshot second=first;second.markerActive=false;second.feet={0,0,0};saveSnapshot(root,second);require(loadSnapshot(root).generation==2,"Alternating committed generation");
    {std::ofstream f(root/"snapshot-a.json.writing");f<<"interrupted";}
    require(loadSnapshot(root).recovered&&loadSnapshot(root).generation==2,"Incomplete write retains committed generation");
    rejects([&]{saveSnapshot(root,first);});require(loadSnapshot(root).generation==2,"Blocked writer retains last valid state");
    std::filesystem::remove(root/"snapshot-a.json.writing");
    {std::ofstream f(root/"snapshot-b.json");f<<"corrupt";}
    const auto recovered=loadSnapshot(root);require(recovered.recovered&&recovered.generation==1&&recovered.value->markerActive,"Corrupt newer snapshot recovers older valid slot");
    saveSnapshot(root,first);require(loadSnapshot(root).generation==2&&!loadSnapshot(root).recovered,"Next write repairs invalid slot");
    auto invalid=first;invalid.feet[0]=INFINITY;rejects([&]{saveSnapshot(root,invalid);});require(loadSnapshot(root).generation==2,"Invalid input cannot overwrite saves");
    const auto broken=root/"broken";std::filesystem::create_directory(broken);{std::ofstream f(broken/"snapshot-a.json");f<<"{}";}
    rejects([&]{loadSnapshot(broken);});rejects([&]{saveSnapshot(broken,first);});
    // Retain a valid profile for the separate package read/restart check.
    const auto package=root/"package-profile";saveSnapshot(package,first);
    std::cout<<"PASS: runtime restart, stable identity, alternating slots, interrupted/corrupt recovery and preservation\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

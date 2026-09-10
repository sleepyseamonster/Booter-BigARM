#include "Persistence/PlayerSnapshot.h"
#include <cmath>
namespace engine {
namespace {
constexpr const char* names[]={"snapshot-a.json","snapshot-b.json"};
Json encode(const PlayerSnapshot& s,uint64_t generation) {
    return {{"world","calibration:v1"},{"player","authored:calibration:proxy"},{"marker","authored:calibration:marker"},{"generation",generation},
        {"feet",s.feet},{"velocity",s.velocity},{"yaw",s.yaw},{"camera_yaw",s.cameraYaw},{"camera_pitch",s.cameraPitch},{"camera_distance",s.cameraDistance},{"marker_active",s.markerActive},{"ticks",s.ticks}};
}
PlayerSnapshot decode(const Json& p) {
    if(!p.is_object()||p.size()!=12||p.at("world")!="calibration:v1"||p.at("player")!="authored:calibration:proxy"||p.at("marker")!="authored:calibration:marker"||
        !p.at("ticks").is_number_unsigned()||!p.at("marker_active").is_boolean())throw std::runtime_error("Unsupported player/world snapshot");
    PlayerSnapshot s;s.feet=p.at("feet").get<PhysicsVector>();s.velocity=p.at("velocity").get<PhysicsVector>();s.yaw=p.at("yaw").get<float>();
    s.cameraYaw=p.at("camera_yaw").get<float>();s.cameraPitch=p.at("camera_pitch").get<float>();s.cameraDistance=p.at("camera_distance").get<float>();s.markerActive=p.at("marker_active").get<bool>();s.ticks=p.at("ticks").get<uint64_t>();validateSnapshot(s);return s;
}
struct Slots {SnapshotRead result;int newest=-1;};
Slots readSlots(const std::filesystem::path& profile) {
    Slots slots;bool invalid=false,staging=false;
    for(int i=0;i<2;++i) {
        const auto file=profile/names[i];staging|=std::filesystem::exists(file.string()+".writing");
        if(!std::filesystem::exists(file))continue;
        try {
            const auto p=readDocument(file,"engine.player-snapshot");const auto value=decode(p);
            if(!p.at("generation").is_number_unsigned()||p.at("generation")==0)throw std::runtime_error("Invalid snapshot generation");
            const auto generation=p.at("generation").get<uint64_t>();
            if(slots.result.value && generation==slots.result.generation)throw std::runtime_error("Duplicate snapshot generation");
            if(generation>slots.result.generation){slots.result.value=value;slots.result.generation=generation;slots.newest=i;}
        }catch(const std::exception&){invalid=true;}
    }
    if(!slots.result.value&&(invalid||staging))throw std::runtime_error("No valid snapshot; preserve this profile and choose a new --profile to start fresh");
    slots.result.recovered=invalid||staging;return slots;
}
}
void validateSnapshot(const PlayerSnapshot& s) {
    for(float x:s.feet)if(!std::isfinite(x)||std::abs(x)>4096)throw std::invalid_argument("Invalid saved player position");
    for(float x:s.velocity)if(!std::isfinite(x)||std::abs(x)>100)throw std::invalid_argument("Invalid saved velocity");
    if(!std::isfinite(s.yaw)||std::abs(s.yaw)>100000||!std::isfinite(s.cameraYaw)||std::abs(s.cameraYaw)>100000||!std::isfinite(s.cameraPitch)||s.cameraPitch<-.15f||s.cameraPitch>1.4f||!std::isfinite(s.cameraDistance)||s.cameraDistance<2.5f||s.cameraDistance>8||s.ticks>UINT64_MAX-1024)throw std::invalid_argument("Invalid saved camera/time");
}
SnapshotRead loadSnapshot(const std::filesystem::path& profile){return readSlots(profile).result;}
void saveSnapshot(const std::filesystem::path& profile,const PlayerSnapshot& value) {
    validateSnapshot(value);std::filesystem::create_directories(profile);
    const auto slots=readSlots(profile);if(slots.result.generation==UINT64_MAX)throw std::runtime_error("Snapshot generation exhausted");
    // Always overwrite the older/invalid slot, retaining the newest valid snapshot.
    writeDocument(profile/names[slots.newest==0?1:0],"engine.player-snapshot",encode(value,slots.result.generation+1));
}
}

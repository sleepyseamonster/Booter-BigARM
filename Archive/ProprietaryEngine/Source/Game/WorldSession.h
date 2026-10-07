#pragma once
#include "Game/CalibrationRuntime.h"
#include "Persistence/WorldSave.h"
#include "Persistence/DurabilityService.h"

namespace engine {
// Shared application persistence owner. All calls occur on the runtime's main thread.
class WorldSession {
public:
    WorldSession(std::filesystem::path profile, WorldConfiguration configuration);
    const WorldSave& initial() const { return initial_; }
    uint64_t generation() const { return generation_; }
    bool restored() const { return restored_; }
    bool recovered() const { return recovered_; }
    void save(const CalibrationRuntime&, float yaw, float pitch, float distance, const WorldDeltas&);
    uint64_t requestSave(DurabilityService&,const CalibrationRuntime&,float yaw,float pitch,float distance,const WorldDeltas&,
                         uint64_t sessionEpoch,uint64_t sourceRevision,bool explicitSave=true);
    DurableReceipt pollSave(DurabilityService&);
    bool savePending()const{return pendingSave_!=0;}
private:
    std::filesystem::path profile_;
    WorldSave initial_;
    uint64_t generation_=0;
    bool restored_=false, recovered_=false;
    uint64_t pendingSave_=0;
};
}

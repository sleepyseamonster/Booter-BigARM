#pragma once
#include "Game/CalibrationRuntime.h"
#include "Persistence/WorldSave.h"

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
private:
    std::filesystem::path profile_;
    WorldSave initial_;
    uint64_t generation_=0;
    bool restored_=false, recovered_=false;
};
}

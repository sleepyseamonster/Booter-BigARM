#include "Game/WorldSession.h"
#include <cmath>

namespace engine {
WorldSession::WorldSession(std::filesystem::path profile, WorldConfiguration configuration)
    :profile_(std::move(profile)) {
    validateTerrainRecipe(configuration.terrain);
    validateRecipe(configuration.rock);
    validateConstraints(configuration.constraints);
    initial_.configuration=std::move(configuration);
    // Start above the generated surface while collision detail is being prepared.
    initial_.player.feet[1]=terrainSample(initial_.configuration.terrain,{}).height+.1f;
    if(profile_.empty())return; // Technical render sessions may be transient.
    auto loaded=loadWorldSave(profile_,initial_.configuration);
    generation_=loaded.generation;recovered_=loaded.recovered;restored_=loaded.value.has_value();
    if(loaded.value)initial_=std::move(*loaded.value);
    // The current rendering/physics adapters use origin zero. Never misplace a future save.
    if(initial_.origin!=Region{})throw DocumentCompatibilityError("This engine pass cannot restore a shifted world origin; profile preserved");
    if(std::abs(initial_.player.feet[0])>3800||std::abs(initial_.player.feet[2])>3800)
        throw DocumentCompatibilityError("Saved player is outside this engine pass's traversal range; profile preserved");
}
void WorldSession::save(const CalibrationRuntime& runtime,float yaw,float pitch,float distance,const WorldDeltas& deltas) {
    if(profile_.empty())throw std::runtime_error("No world profile selected");
    // Snapshot and deltas belong to the same main-thread boundary, before another tick/job adoption.
    WorldSave state{initial_.configuration,runtime.snapshot(yaw,pitch,distance),initial_.origin,deltas};
    generation_=saveWorld(profile_,state,generation_);
}
}

#pragma once
#include "Persistence/PlayerSnapshot.h"
#include "World/Terrain.h"
#include "World/WorldDeltas.h"
#include <functional>
namespace engine {
struct WorldSave {
    WorldConfiguration configuration;
    PlayerSnapshot player;
    Region origin;
    WorldDeltas deltas;
};
struct WorldSaveRead {std::optional<WorldSave> value;uint64_t generation=0;bool recovered=false;};
WorldSaveRead loadWorldSave(const std::filesystem::path&,const WorldConfiguration& expected);
// Expected generation prevents stale application sessions from overwriting another writer.
// The optional checkpoint supports bounded interruption tests; applications omit it.
uint64_t saveWorld(const std::filesystem::path&,const WorldSave&,uint64_t expectedGeneration,const std::function<void(unsigned)>& checkpoint={});
}

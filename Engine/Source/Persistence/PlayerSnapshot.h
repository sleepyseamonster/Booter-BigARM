#pragma once
#include "Persistence/Document.h"
#include "Physics/PhysicsWorld.h"
#include <optional>
namespace engine {
struct PlayerSnapshot {
    PhysicsVector feet{},velocity{};
    float yaw=0,cameraYaw=.65f,cameraPitch=.3f,cameraDistance=5;
    bool markerActive=false;
    uint64_t ticks=0;
};
struct SnapshotRead {std::optional<PlayerSnapshot> value;uint64_t generation=0;bool recovered=false;};
void validateSnapshot(const PlayerSnapshot&);
// One writer per profile. Two committed slots; incomplete staging files never load.
// Single snapshot member of an enclosing atomic world generation.
void saveSnapshotFile(const std::filesystem::path&,const PlayerSnapshot&);
PlayerSnapshot loadSnapshotFile(const std::filesystem::path&);
SnapshotRead loadSnapshot(const std::filesystem::path& profile);
void saveSnapshot(const std::filesystem::path& profile,const PlayerSnapshot&);
}

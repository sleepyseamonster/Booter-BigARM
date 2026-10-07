#pragma once
#include "Core/WorldIdentity.h"
#include <memory>
#include <optional>
#include <vector>
namespace engine {
struct EntityToken {
    uint64_t world=0,incarnation=0;
    uint32_t entity=UINT32_MAX;
    bool operator==(const EntityToken&) const=default;
};
struct Pose {WorldPosition position;float yaw=0;};
struct EntitySnapshot {std::string id;EntityToken token;Pose previous,current;std::array<double,3> velocity{};};
struct WorldCommand {
    enum class Kind { Velocity,Teleport,Remove };
    uint64_t sequence=0;
    Kind kind=Kind::Velocity;
    EntityToken target;
    Pose pose;
    std::array<double,3> velocity{};
};
// Single simulation-thread owner. Immediate lifecycle calls are boundary operations;
// runtime edits enqueue commands and are adopted in sequence order at a fixed tick.
class World {
public:
    explicit World(double regionSpan=256,size_t maxEntities=8192);
    ~World();
    World(const World&)=delete;
    World& operator=(const World&)=delete;
    EntityToken create(std::string stableId,Pose pose={});
    bool remove(EntityToken);
    size_t unloadRegion(Region);
    std::optional<EntityToken> find(const std::string& stableId) const;
    std::optional<EntitySnapshot> snapshot(EntityToken) const;
    std::vector<EntitySnapshot> ordered() const;
    void enqueue(WorldCommand);
    void step(double seconds);
    // Adopt an authoritative physics result after step; preserve prior pose for presentation.
    void setSimulatedPose(EntityToken,Pose);
    size_t size() const;
    uint64_t appliedCommands() const;
    uint64_t rejectedCommands() const;
    double regionSpan() const;
private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};
}

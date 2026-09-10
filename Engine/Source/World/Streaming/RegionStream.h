#pragma once
#include "World/Terrain.h"
#include "World/Rocks/RockAsset.h"
#include "Core/BoundedJobs.h"
#include <map>
namespace engine {
using RegionKey=std::pair<int64_t,int64_t>;
struct StreamAnchor {WorldPosition position;unsigned priority=0;};
struct RegionContent {
    TerrainPatch terrain;
    std::vector<std::array<float,3>> collision;
    size_t bytes()const{return terrain.bytes()+collision.capacity()*sizeof(std::array<float,3>);}
};
struct RegionReadiness {
    bool render=false,collision=false;
    // No navigation adapter exists yet. Zero cannot be consumed as a generated tile version.
    static constexpr uint32_t navigationVersion=0;
};
static_assert(RegionReadiness::navigationVersion==0);
struct RegionSlot {
    Region region;uint64_t ticket=0;
    bool desired=false;RegionReadiness ready;
    std::shared_ptr<const RegionContent> content;
    std::string error;
};
// Single owner-thread region lifecycle. Callbacks attach/retire physical/GPU representations on that thread.
class RegionStream {
public:
    using Attach=std::function<RegionReadiness(const RegionContent&)>;
    using Detach=std::function<void(Region)>;
    RegionStream(TerrainRecipe,RockRecipe,PlacementConstraints,Attach,Detach);
    ~RegionStream();
    void update(const std::vector<StreamAnchor>&);
    bool collisionReady(WorldPosition from,WorldPosition to,float radius=.5f)const;
    const std::map<RegionKey,RegionSlot>& slots()const{return slots_;}
    const std::array<RockAsset,4>& rocks()const{return *rocks_;}
    size_t residentBytes()const{return residentBytes_;}
    uint64_t retired()const{return retired_;}
    BoundedJobs<RegionContent>::Stats jobStats(){return jobs_.stats();}
private:
    TerrainRecipe terrain_;RockRecipe recipe_;PlacementConstraints constraints_;
    std::shared_ptr<std::array<RockAsset,4>> rocks_;
    Attach attach_;Detach detach_;
    BoundedJobs<RegionContent> jobs_;
    std::map<RegionKey,RegionSlot> slots_;
    size_t reservation_=0,residentBytes_=0;
    uint64_t epoch_=0,retired_=0;
};
}

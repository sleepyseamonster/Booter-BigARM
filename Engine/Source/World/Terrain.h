#pragma once
#include "World/Placement.h"
#include "World/Rocks/RockGenerator.h"
#include <functional>
namespace engine {
struct TerrainRecipe {
    uint64_t seed=1049;
    uint32_t version=1,amplitudeMm=6000;
    bool operator==(const TerrainRecipe&)const=default;
};
struct TerrainSample {float height=0;std::array<float,3> normal{0,1,0};};
struct RockPlacement {GeneratedId id;WorldPosition position;float yaw=0,footprint=0;};
struct LandmarkAnchor {std::string id;WorldPosition position;float radius=0;};
struct TerrainPatch {
    Region region;
    GeneratedId id;
    ModelData mesh;
    std::vector<std::array<float,3>> collision;
    std::vector<RockPlacement> rocks;
    std::vector<LandmarkAnchor> landmarks;
    std::vector<ReservedRoute> routes;
    size_t bytes()const;
};
void validateTerrainRecipe(const TerrainRecipe&);
void saveTerrainRecipe(const std::filesystem::path&,const TerrainRecipe&);
TerrainRecipe loadTerrainRecipe(const std::filesystem::path&);
// Query contract is independent of patch representation; later surfaces need not be heightfields.
TerrainSample terrainSample(const TerrainRecipe&,WorldPosition);
TerrainPatch generateTerrain(const TerrainRecipe&,Region,const RockRecipe&,const PlacementConstraints&,
                             const std::function<bool()>& cancelled=[] {return false;});
}

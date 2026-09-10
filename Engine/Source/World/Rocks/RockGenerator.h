#pragma once
#include "Core/WorldIdentity.h"
#include "Animation/Model.h"
namespace engine {
struct RockRecipe {
    uint64_t seed=1;
    uint32_t version=1,subdivisions=2;
    std::array<uint32_t,3> radiiMm{1400,1000,1100};
    uint32_t distortionPermille=180,bandPermille=80,bands=5;
    bool operator==(const RockRecipe&)const=default;
};
struct RockResult {
    std::string id;
    ModelData mesh;
    std::array<float,3> minimum{},maximum{};
    float footprintRadius=0;
    // One classification per triangle: 0 side, 1 upward surface, 2 underside.
    std::vector<uint8_t> surfaces;
};
void validateRecipe(const RockRecipe&);
void saveRockRecipe(const std::filesystem::path&,const RockRecipe&);
RockRecipe loadRockRecipe(const std::filesystem::path&);
void saveRockResult(const std::filesystem::path& newDirectory,const RockResult&,const RockRecipe&);
RockResult generateRock(const RockRecipe&,const GeneratedId&);
}

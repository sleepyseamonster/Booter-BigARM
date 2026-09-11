#pragma once
#include "Core/WorldIdentity.h"
#include "Core/RockMaterial.h"
#include <functional>
#include "Animation/Model.h"
namespace engine {
struct RockVolume {
    uint32_t id=0;
    std::array<float,3> center{},halfSize{.5f,.5f,.5f};
    float yaw=0;
    bool subtractive=false;
    bool operator==(const RockVolume&)const=default;
};
struct RockRecipe {
    uint64_t seed=1;
    uint32_t version=1,subdivisions=2;
    std::array<uint32_t,3> radiiMm{1400,1000,1100};
    uint32_t distortionPermille=180,bandPermille=80,bands=5;
    // V3 normalized authoring volumes; empty means generate the seeded source plan.
    uint32_t massCount=6,compaction=620,asymmetry=650,fractures=500,edgeDamage=450;
    uint32_t formation=0,members=4,spacingMm=2500; // single / outcrop / scattered / pile
    std::vector<RockVolume> volumes;
    RockMaterial material;
    bool operator==(const RockRecipe&)const=default;
};
struct RockResult {
    std::string id;
    ModelData mesh;
    std::array<float,3> minimum{},maximum{};
    float footprintRadius=0;
    // One classification per triangle: 0 side, 1 upward surface, 2 underside.
    std::vector<uint8_t> surfaces;
    std::vector<std::string> memberIds;
};
std::vector<RockVolume> planRockVolumes(const RockRecipe&,const GeneratedId&);
RockResult generateVolumeRock(const RockRecipe&,const GeneratedId&);
struct RockFormationMember {GeneratedId id;std::array<float,3> offset{};float yaw=0,scale=1;};
std::vector<RockFormationMember> planRockFormation(const RockRecipe&,const GeneratedId&,const std::function<float(float,float)>& ground);
RockResult generateRockFormation(const RockRecipe&,const GeneratedId&,const std::function<float(float,float)>& ground);
void validateRecipe(const RockRecipe&);
void saveRockRecipe(const std::filesystem::path&,const RockRecipe&);
RockRecipe loadRockRecipe(const std::filesystem::path&);
void saveRockResult(const std::filesystem::path& newDirectory,const RockResult&,const RockRecipe&);
RockResult generateRock(const RockRecipe&,const GeneratedId&);
}

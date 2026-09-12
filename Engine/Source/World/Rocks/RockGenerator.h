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
    // V5 local primitive, rotated yaw then pitch then roll in the field inverse.
    uint32_t primitive=0; // rounded block / tapered stone / wedge
    float pitch=0,roll=0,taper=0;
    bool operator==(const RockVolume&)const=default;
};
struct RockMemberEdit {
    uint32_t slot=0,variant=0;
    std::array<float,3> translation{},axes{1,1,1};
    float yaw=0;
    bool groundOnly=false;
    bool operator==(const RockMemberEdit&)const=default;
};
struct RockRecipe {
    uint64_t seed=1;
    uint32_t version=1,subdivisions=2;
    std::array<uint32_t,3> radiiMm{1400,1000,1100};
    uint32_t distortionPermille=180,bandPermille=80,bands=5;
    // V3 normalized authoring volumes; empty means generate the seeded source plan.
    uint32_t massCount=6,compaction=620,asymmetry=650,fractures=500,edgeDamage=450;
    uint32_t formation=0,members=4,spacingMm=2500; // single / outcrop / scattered / pile; v4 also ridge / mixed
    std::vector<RockVolume> volumes;
    RockMaterial material;
    uint32_t profile=0; // V5: auto / boulder / broken slab / angular chunk / shard
    std::vector<RockMemberEdit> memberEdits;
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
enum class FormationRole { Core, Buttress, Pillar, Talus, Slab, Fragment, Base, Middle, Cap };
struct RockFormationMember {
    GeneratedId id;std::array<float,3> offset{};float yaw=0,scale=1;
    std::array<float,3> axes{1,1,1};uint32_t variant=0,host=UINT32_MAX;FormationRole role=FormationRole::Core;
    float authoredLift=0;
    bool preservePlacement=false;
};
std::array<float,3> formationPoint(const RockFormationMember&,std::array<float,3>);
void seatRockFormation(std::vector<RockFormationMember>&,const std::array<const RockResult*,4>&,const std::function<float(float,float)>&);
std::vector<RockFormationMember> planRockFormation(const RockRecipe&,const GeneratedId&,const std::function<float(float,float)>& ground);
RockResult generateRockFormation(const RockRecipe&,const GeneratedId&,const std::function<float(float,float)>& ground);
void validateRecipe(const RockRecipe&);
void saveRockRecipe(const std::filesystem::path&,const RockRecipe&);
RockRecipe loadRockRecipe(const std::filesystem::path&);
void saveRockResult(const std::filesystem::path& newDirectory,const RockResult&,const RockRecipe&);
RockResult generateRock(const RockRecipe&,const GeneratedId&);
}

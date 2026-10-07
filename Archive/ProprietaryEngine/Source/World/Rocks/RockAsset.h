#pragma once
#include "World/Rocks/RockGenerator.h"
namespace engine {
struct RockAsset {std::vector<RockResult> lods;std::vector<std::array<float,3>> collision;size_t bytes=0;};
RockAsset buildRockAsset(const RockRecipe&,const GeneratedId&);
size_t rockLod(const RockAsset&,float distance);
}

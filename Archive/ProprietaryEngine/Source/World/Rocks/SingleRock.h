#pragma once
#include "World/Rocks/RockGenerator.h"
namespace engine {
// Empty volumes regenerate Unity's original single-rock plan. Explicit volumes
// override the plan for authored edits. No renderer/UI/Unity runtime dependency.
std::vector<RockVolume> planSingleRock(const RockRecipe&);
std::array<float,2> singleRockDimensions(uint32_t seed);
}

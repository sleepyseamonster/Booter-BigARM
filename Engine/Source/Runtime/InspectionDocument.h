#pragma once
#include "Core/FixtureState.h"
#include <filesystem>
namespace engine {
void saveInspection(const std::filesystem::path& path,const FixtureState& state);
// Parse and validate a candidate before changing live state.
void loadInspection(const std::filesystem::path& path,FixtureState& state);
}

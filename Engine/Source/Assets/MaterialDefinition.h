#pragma once
#include "Assets/TextureCatalog.h"
#include "Core/RockMaterial.h"
#include <array>
#include <string>
#include <vector>

namespace engine {
struct MaterialSlot {
    std::string id;
    TextureRole role=TextureRole::Color;
};
struct MaterialDefinition {
    std::string id;
    std::array<MaterialSlot,3> base{};
    std::array<MaterialSlot,10> layers{};
    bool layered=false;
};
struct MaterialValidation {
    bool valid=false;
    std::vector<std::string> errors;
};

MaterialDefinition layeredRockMaterial();
MaterialValidation validateMaterial(const MaterialDefinition&,const std::vector<TextureRecord>&);
}

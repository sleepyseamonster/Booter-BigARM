#pragma once
#include "Assets/TextureCatalog.h"
#include "Core/RockMaterial.h"
#include <array>
#include <string>
#include <vector>

namespace engine {
enum class MaterialProjection : uint8_t { WorldTriplanar,UV0 };
struct MaterialSlot {
    std::string id;
    TextureRole role=TextureRole::Color;
};
struct MaterialDefinition {
    std::string id;
    std::array<MaterialSlot,3> base{};
    std::array<MaterialSlot,10> layers{};
    bool layered=false;
    MaterialProjection projection=MaterialProjection::UV0;
    uint8_t uvSet=0;
    std::array<float,4> uvTransform{1,1,0,0};
};
struct MaterialValidation {
    bool valid=false;
    std::vector<std::string> errors;
};

MaterialDefinition layeredRockMaterial();
MaterialDefinition uvMaterial(std::string id,std::string color,std::string normal,std::string surface,
                              std::array<float,4> uvTransform={1,1,0,0});
MaterialValidation validateMaterial(const MaterialDefinition&,const std::vector<TextureRecord>&);
}

#pragma once
#include <array>
#include <cstdint>
namespace engine {
// Logical family identity is persisted; GPU resources are borrowed by the renderer.
inline constexpr const char* rockMaterialFamily="wasteland-layered-v1";
struct RockMaterial {
    uint32_t grit=300,shale=700,cracks=420,dust=150,variation=620,worn=200;
    bool operator==(const RockMaterial&)const=default;
};
// Exact preserved Unity bindings: the four color slots intentionally share side albedo.
inline constexpr std::array<const char*,10> rockLayerTextureIds={
 "surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",
 "surface/textures/rocks/workbench/layered/rockworkbenchtop_normal",
 "surface/textures/rocks/workbench/layered/rockworkbenchtop_surface",
 "surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",
 "surface/textures/rocks/workbench/layered/rockworkbenchunderside_normal",
 "surface/textures/rocks/workbench/layered/rockworkbenchunderside_surface",
 "surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",
 "surface/textures/rocks/workbench/layered/rockworkbenchgrit_normal",
 "surface/textures/rocks/workbench/layered/rockworkbenchgrit_surface",
 "surface/textures/rocks/workbench/layered/rockworkbenchcrack_mask"};
}

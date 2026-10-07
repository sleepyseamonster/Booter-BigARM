#pragma once
#include "Assets/MaterialDefinition.h"
#include "Core/RockMaterial.h"
#include "Rendering/TextureStore.h"
#include <array>
#include <memory>

namespace engine {

struct SurfaceTextures {
    bool packedSurface=true;
    bgfx::TextureHandle albedo=BGFX_INVALID_HANDLE,normal=BGFX_INVALID_HANDLE,surface=BGFX_INVALID_HANDLE;
    std::array<bgfx::TextureHandle,10> layers=[] {std::array<bgfx::TextureHandle,10> a;for(auto& h:a)h=BGFX_INVALID_HANDLE;return a;}();
    bool terrainBlend=false,terrainNatural=false,layered=false;
    RockMaterial material;
    float seed=0;
    MaterialProjection projection=MaterialProjection::WorldTriplanar;
    std::array<float,4> uvTransform{1,1,0,0};
};

// Owns texture leases and exposes borrowed GPU handles for a frame. Logical
// material/texture IDs remain authoritative; handles never enter scene data.
class SurfaceMaterialBinding {
public:
    const SurfaceTextures& surface()const{return surface_;}
private:
    friend std::shared_ptr<const SurfaceMaterialBinding> resolveSurfaceMaterial(const MaterialDefinition&,const std::vector<TextureRecord>&,TextureStore&);
    SurfaceTextures surface_;
    std::array<TextureLease,13> leases_;
};

std::shared_ptr<const SurfaceMaterialBinding> resolveSurfaceMaterial(const MaterialDefinition&,const std::vector<TextureRecord>&,TextureStore&);

} // namespace engine

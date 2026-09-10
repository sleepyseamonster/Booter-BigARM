#pragma once
#include "Animation/Model.h"
#include <bgfx/bgfx.h>
namespace engine {
// Render-thread-owned mesh buffers. Destroy before renderer shutdown.
class RenderModel {
public:
    explicit RenderModel(const ModelData&);
    ~RenderModel();
    RenderModel(const RenderModel&)=delete;
    RenderModel& operator=(const RenderModel&)=delete;
    void bakeReference(const ModelData&,const std::vector<SkinMatrix>&);
    void bind(bool cpuReference=false) const;
    std::array<float,4> color;
    float roughness,metallic;
    size_t jointCount;
private:
    bgfx::VertexBufferHandle vertices_=BGFX_INVALID_HANDLE,reference_=BGFX_INVALID_HANDLE;
    bgfx::IndexBufferHandle indices_=BGFX_INVALID_HANDLE;
};
}

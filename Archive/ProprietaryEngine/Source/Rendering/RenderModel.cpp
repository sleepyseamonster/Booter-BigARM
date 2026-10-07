#include "Rendering/RenderModel.h"
#include "Animation/AnimationPlayer.h"
#include <stdexcept>
namespace engine {
namespace {
bgfx::VertexLayout layout() {
    bgfx::VertexLayout result;result.begin().add(bgfx::Attrib::Position,3,bgfx::AttribType::Float)
        .add(bgfx::Attrib::Normal,3,bgfx::AttribType::Float).add(bgfx::Attrib::TexCoord0,2,bgfx::AttribType::Float)
        .add(bgfx::Attrib::Tangent,4,bgfx::AttribType::Float).add(bgfx::Attrib::Weight,4,bgfx::AttribType::Float)
        .add(bgfx::Attrib::Indices,4,bgfx::AttribType::Uint8,false,false).end();
    if(result.getStride()!=sizeof(SkinVertex))throw std::runtime_error("Skin vertex layout mismatch");return result;
}
bgfx::VertexBufferHandle upload(const std::vector<SkinVertex>& vertices) {
    const auto handle=bgfx::createVertexBuffer(bgfx::copy(vertices.data(),uint32_t(vertices.size()*sizeof(SkinVertex))),layout());
    if(!bgfx::isValid(handle))throw std::runtime_error("Model vertex allocation failed");return handle;
}
}
RenderModel::RenderModel(const ModelData& model):color(model.baseColor),roughness(model.roughness),metallic(model.metallic),jointCount(model.joints.size()) {
    validateModel(model);vertices_=upload(model.vertices);
    indices_=bgfx::createIndexBuffer(bgfx::copy(model.indices.data(),uint32_t(model.indices.size()*sizeof(uint32_t))),BGFX_BUFFER_INDEX32);
    if(!bgfx::isValid(indices_)) {bgfx::destroy(vertices_);throw std::runtime_error("Model index allocation failed");}
}
RenderModel::~RenderModel() {
    for(auto handle:{vertices_,reference_})if(bgfx::isValid(handle))bgfx::destroy(handle);
    if(bgfx::isValid(indices_))bgfx::destroy(indices_);
}
void RenderModel::bakeReference(const ModelData& model,const std::vector<SkinMatrix>& palette) {
    const auto next=upload(skinVertices(model,palette));if(bgfx::isValid(reference_))bgfx::destroy(reference_);reference_=next;
}
void RenderModel::bind(bool reference) const {
    const auto handle=reference?reference_:vertices_;if(!bgfx::isValid(handle))throw std::runtime_error("Model reference has not been baked");
    bgfx::setVertexBuffer(0,handle);bgfx::setIndexBuffer(indices_);
}
}

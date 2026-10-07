#include "Assets/MaterialDefinition.h"
#include <algorithm>
#include <cmath>

namespace engine {
MaterialDefinition layeredRockMaterial() {
    MaterialDefinition definition;
    definition.id=rockMaterialFamily;
    definition.layered=true;
    definition.projection=MaterialProjection::WorldTriplanar;
    definition.base={MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",TextureRole::Color},
                     MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_normal",TextureRole::Normal},
                     MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_surface",TextureRole::Surface}};
    for(size_t i=0;i<definition.layers.size();++i) {
        const auto role=i==9?TextureRole::Mask:(i%3==0?TextureRole::Color:(i%3==1?TextureRole::Normal:TextureRole::Surface));
        definition.layers[i]={rockLayerTextureIds[i],role};
    }
    return definition;
}

MaterialDefinition uvMaterial(std::string id,std::string color,std::string normal,std::string surface,std::array<float,4> transform) {
    MaterialDefinition definition;definition.id=std::move(id);definition.projection=MaterialProjection::UV0;definition.uvTransform=transform;
    definition.base={MaterialSlot{std::move(color),TextureRole::Color},MaterialSlot{std::move(normal),TextureRole::Normal},MaterialSlot{std::move(surface),TextureRole::Surface}};
    return definition;
}

MaterialValidation validateMaterial(const MaterialDefinition& definition,const std::vector<TextureRecord>& records) {
    MaterialValidation result;
    if(definition.id.empty()) result.errors.emplace_back("Material definition requires a stable id");
    if(definition.uvSet!=0)result.errors.emplace_back("Only UV0 is supported");
    for(float value:definition.uvTransform)if(!std::isfinite(value))result.errors.emplace_back("Material UV transform must be finite");
    if(definition.projection==MaterialProjection::UV0&&(definition.uvTransform[0]==0||definition.uvTransform[1]==0))result.errors.emplace_back("Material UV scale cannot be zero");
    if(definition.layered&&definition.projection!=MaterialProjection::WorldTriplanar)result.errors.emplace_back("Layered materials require world triplanar projection");
    std::vector<MaterialSlot> slots;
    slots.insert(slots.end(),definition.base.begin(),definition.base.end());
    if(definition.layered) slots.insert(slots.end(),definition.layers.begin(),definition.layers.end());
    for(const auto& slot:slots) {
        if(slot.id.empty()) {result.errors.emplace_back("Material slot id is empty");continue;}
        const auto found=std::find_if(records.begin(),records.end(),[&](const auto& record){return record.id==slot.id;});
        if(found==records.end()) {result.errors.emplace_back("Missing material texture: "+slot.id);continue;}
        if(found->role!=slot.role) result.errors.emplace_back("Material texture role mismatch: "+slot.id);
        if(found->srgb!=(slot.role==TextureRole::Color)) result.errors.emplace_back("Material transfer function mismatch: "+slot.id);
    }
    result.valid=result.errors.empty();
    return result;
}
}

#include "Assets/MaterialDefinition.h"
#include <algorithm>

namespace engine {
MaterialDefinition layeredRockMaterial() {
    MaterialDefinition definition;
    definition.id=rockMaterialFamily;
    definition.layered=true;
    definition.base={MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",TextureRole::Color},
                     MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_normal",TextureRole::Normal},
                     MaterialSlot{"surface/textures/rocks/workbench/layered/rockworkbenchside_surface",TextureRole::Surface}};
    for(size_t i=0;i<definition.layers.size();++i) {
        const auto role=i==9?TextureRole::Mask:(i%3==0?TextureRole::Color:(i%3==1?TextureRole::Normal:TextureRole::Surface));
        definition.layers[i]={rockLayerTextureIds[i],role};
    }
    return definition;
}

MaterialValidation validateMaterial(const MaterialDefinition& definition,const std::vector<TextureRecord>& records) {
    MaterialValidation result;
    if(definition.id.empty()) result.errors.emplace_back("Material definition requires a stable id");
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

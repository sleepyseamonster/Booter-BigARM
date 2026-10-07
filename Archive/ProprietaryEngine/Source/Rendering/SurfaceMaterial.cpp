#include "Rendering/SurfaceMaterial.h"
#include <algorithm>
#include <stdexcept>

namespace engine {

std::shared_ptr<const SurfaceMaterialBinding> resolveSurfaceMaterial(const MaterialDefinition& definition,const std::vector<TextureRecord>& records,TextureStore& store){
    const auto validation=validateMaterial(definition,records);
    if(!validation.valid){
        std::string error="Material validation failed";for(const auto& item:validation.errors)error+="; "+item;throw std::invalid_argument(error);
    }
    auto result=std::make_shared<SurfaceMaterialBinding>();result->surface_.projection=definition.projection;result->surface_.uvTransform=definition.uvTransform;result->surface_.layered=definition.layered;
    auto acquire=[&](const MaterialSlot& slot,size_t lease) {
        const auto found=std::find_if(records.begin(),records.end(),[&](const auto& record){return record.id==slot.id;});
        result->leases_[lease]=store.acquire(*found);return store.resolve(result->leases_[lease].token());
    };
    result->surface_.albedo=acquire(definition.base[0],0);result->surface_.normal=acquire(definition.base[1],1);result->surface_.surface=acquire(definition.base[2],2);
    if(definition.layered)for(size_t i=0;i<definition.layers.size();++i)result->surface_.layers[i]=acquire(definition.layers[i],i+3);
    return result;
}

} // namespace engine

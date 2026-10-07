#include "Assets/MaterialDefinition.h"
#include <iostream>
#include <stdexcept>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
TextureRecord record(std::string id,TextureRole role){TextureRecord r;r.id=std::move(id);r.role=role;r.srgb=role==TextureRole::Color;return r;}
}
int main(){try{
    const auto definition=layeredRockMaterial();
    std::vector<TextureRecord> records;
    for(const auto& slot:definition.base)records.push_back(record(slot.id,slot.role));
    for(const auto& slot:definition.layers)records.push_back(record(slot.id,slot.role));
    auto valid=validateMaterial(definition,records);require(valid.valid,"Complete layered material should validate");
    records.back().role=TextureRole::Normal;require(!validateMaterial(definition,records).valid,"Role mismatch must reject material");
    records.back().role=TextureRole::Mask;records.back().srgb=true;require(!validateMaterial(definition,records).valid,"Transfer mismatch must reject material");
    records.pop_back();require(!validateMaterial(definition,records).valid,"Missing material texture must reject material");
    std::vector<TextureRecord> uvRecords{record("generic/color",TextureRole::Color),record("generic/normal",TextureRole::Normal),record("generic/surface",TextureRole::Surface)};
    auto uv=uvMaterial("generic/uv","generic/color","generic/normal","generic/surface",{2,3,.25f,.5f});
    require(validateMaterial(uv,uvRecords).valid,"Generic UV material should validate");
    uv.uvSet=1;require(!validateMaterial(uv,uvRecords).valid,"Unsupported UV set must reject");
    uv.uvSet=0;uv.uvTransform[0]=0;require(!validateMaterial(uv,uvRecords).valid,"Degenerate UV scale must reject");
    std::cout<<"PASS: material family slots, UV0 projection, semantic roles, transfer functions and explicit unsupported-profile rejection\n";
    return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}}

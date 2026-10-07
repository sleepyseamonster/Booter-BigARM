#include "World/Rocks/RockAsset.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace engine {
RockAsset buildRockAsset(const RockRecipe& recipe,const GeneratedId& id) {
    RockAsset asset;auto next=recipe;
    for(unsigned i=0;i<3;++i){asset.lods.push_back(generateRock(next,id));const auto& mesh=asset.lods.back().mesh;asset.bytes+=mesh.vertices.size()*sizeof(SkinVertex)+mesh.indices.size()*sizeof(uint32_t);if(next.subdivisions==0)break;--next.subdivisions;}
    const auto& mesh=asset.lods.front().mesh;asset.collision.reserve(mesh.indices.size());for(auto i:mesh.indices)asset.collision.push_back(mesh.vertices.at(i).position);
    asset.bytes+=asset.collision.size()*sizeof(std::array<float,3>);return asset;
}
size_t rockLod(const RockAsset& asset,float distance){if(asset.lods.empty()||!std::isfinite(distance)||distance<0)throw std::invalid_argument("Invalid rock LOD selection");return std::min(asset.lods.size()-1,size_t(distance<12?0:(distance<24?1:2)));}
}

#pragma once
#include "Core/WorldIdentity.h"
#include <map>
#include <set>
#include <stdexcept>
namespace engine {
using DeltaRegion=std::pair<int64_t,int64_t>;
struct WorldDeltas {
    std::map<DeltaRegion,std::set<uint64_t>> removedRocks;
    bool removed(Region region,uint64_t member)const{const auto it=removedRocks.find({region.x,region.z});return it!=removedRocks.end()&&it->second.contains(member);}
    bool operator==(const WorldDeltas&)const=default;
};
inline void validateDeltas(const WorldDeltas& d){
    if(d.removedRocks.size()>256)throw std::invalid_argument("World delta region limit exceeded");
    for(const auto& [_,members]:d.removedRocks)if(members.empty()||members.size()>256||*members.rbegin()>255)throw std::invalid_argument("Invalid rock delta members");
}
}

#pragma once
#include "Persistence/Document.h"
namespace engine {
inline bool boundedJsonDepth(const Json& value,size_t depth=0)noexcept{
    if(!value.is_structured())return true;
    if(depth>=32)return false;
    for(const auto& entry:value)if(!boundedJsonDepth(entry,depth+1))return false;
    return true;
}
// nlohmann 3.12's normal container destruction allocates a flattening stack.
// Engine documents have bounded depth. Clear their children first so cleanup of
// prepared receipts/candidates cannot allocate, including during exception unwind.
inline void clearBoundedJson(Json& value)noexcept{
    if(auto* object=value.get_ptr<Json::object_t*>()){
        for(auto& entry:*object)clearBoundedJson(entry.second);
        object->clear();
    }else if(auto* array=value.get_ptr<Json::array_t*>()){
        for(auto& entry:*array)clearBoundedJson(entry);
        array->clear();
    }
}
struct JsonReleaseGuard {
    Json& value;
    ~JsonReleaseGuard(){clearBoundedJson(value);}
};
struct PreparedJson {
    Json value=Json::object();
    ~PreparedJson(){clearBoundedJson(value);}
    PreparedJson()=default;
    PreparedJson(const PreparedJson&)=delete;
    PreparedJson& operator=(const PreparedJson&)=delete;
};
}

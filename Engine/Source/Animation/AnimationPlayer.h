#pragma once
#include "Animation/Model.h"
#include <memory>
#include <limits>
namespace engine {
// Each instance owns sampling cursors and pose buffers. Source data can be shared.
class AnimationPlayer {
public:
    explicit AnimationPlayer(std::shared_ptr<const ModelData>);
    ~AnimationPlayer();
    AnimationPlayer(const AnimationPlayer&)=delete;
    AnimationPlayer& operator=(const AnimationPlayer&)=delete;
    const std::vector<SkinMatrix>& sample(size_t clip,double seconds,size_t blendClip=std::numeric_limits<size_t>::max(),float blend=0);
    const std::vector<SkinMatrix>& rest();
private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};
std::vector<SkinVertex> skinVertices(const ModelData&,const std::vector<SkinMatrix>&);
}

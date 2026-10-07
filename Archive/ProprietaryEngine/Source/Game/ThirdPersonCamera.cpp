#include "Game/ThirdPersonCamera.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace engine {
FollowCameraFrame ThirdPersonCamera::update(const PhysicsWorld& world,PhysicsVector feet,float yaw,float pitch,float distance,float seconds,BodyToken ignore) {
    if(!std::isfinite(yaw)||!std::isfinite(pitch)||!std::isfinite(distance)||!std::isfinite(seconds)||seconds<0) throw std::invalid_argument("Invalid follow camera state");
    distance=std::clamp(distance,1.0f,8.0f);pitch=std::clamp(pitch,-.15f,1.2f);
    PhysicsVector target={feet[0],feet[1]+1.35f,feet[2]};
    const PhysicsVector direction={std::cos(pitch)*std::sin(yaw),std::sin(pitch),std::cos(pitch)*std::cos(yaw)};
    PhysicsVector travel;for(size_t i=0;i<3;++i) travel[i]=direction[i]*distance;
    const auto hit=world.sphereSweep(target,.2f,travel,ignore);
    const float safe=hit?std::max(.05f,distance*hit->fraction-.05f):distance;
    // Obstructions contract immediately. Only outward recovery is smoothed,
    // so interpolation cannot carry the camera through a newly detected wall.
    if(distance_<=0 || safe<distance_) distance_=safe;
    else distance_+= (safe-distance_)*(1-std::exp(-12*std::min(seconds,.25f)));
    PhysicsVector eye;for(size_t i=0;i<3;++i) eye[i]=target[i]+direction[i]*distance_;
    return {eye,target,distance_,hit.has_value()};
}
}

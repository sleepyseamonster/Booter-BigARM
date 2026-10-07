#pragma once
#include "Physics/PhysicsWorld.h"
namespace engine {
struct FollowCameraFrame {PhysicsVector eye{},target{};float distance=0;bool obstructed=false;};
class ThirdPersonCamera {
public:
    FollowCameraFrame update(const PhysicsWorld&,PhysicsVector feet,float yaw,float pitch,float distance,float seconds,BodyToken ignore={});
private:
    float distance_=0;
};
}

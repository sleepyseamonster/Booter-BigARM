#pragma once
#include "Simulation/Actions.h"
#include "Physics/PhysicsWorld.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace engine {
struct LocomotionIntent {PhysicsVector velocity{};bool jump=false;};
inline LocomotionIntent locomotionIntent(const ActionFrame& input,float cameraYaw) {
    if(!std::isfinite(cameraYaw)) throw std::invalid_argument("Invalid locomotion heading");
    const float x=input[size_t(Action::MoveX)].value,z=input[size_t(Action::MoveZ)].value;
    const float scale=(input[size_t(Action::Sprint)].value>0?5.0f:3.0f)/std::max(1.0f,std::sqrt(x*x+z*z));
    return {{{(x*std::cos(cameraYaw)+z*std::sin(cameraYaw))*scale,0,(-x*std::sin(cameraYaw)+z*std::cos(cameraYaw))*scale}},input[size_t(Action::Jump)].pressed};
}
}

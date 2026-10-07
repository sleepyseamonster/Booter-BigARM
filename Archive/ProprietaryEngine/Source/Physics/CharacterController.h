#pragma once
#include "Physics/PhysicsWorld.h"
namespace engine {
struct CharacterConfig {float radius=.35f,height=1.8f,maxSlopeDegrees=45,stepHeight=.35f,jumpSpeed=5;};
// Foot-position capsule; inner kinematic body makes the character visible to
// ordinary queries. The controller and PhysicsWorld share the physics lifetime.
class CharacterController {
public:
    CharacterController(PhysicsWorld&,std::string stableId,PhysicsVector feet,CharacterConfig config={});
    ~CharacterController();
    CharacterController(const CharacterController&)=delete;
    CharacterController& operator=(const CharacterController&)=delete;
    void step(float seconds,PhysicsVector horizontalVelocity,bool jump);
    void restore(PhysicsVector feet,PhysicsVector velocity={});
    PhysicsVector position() const;
    PhysicsVector velocity() const;
    bool grounded() const;
    BodyToken body() const;
private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};
}

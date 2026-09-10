#pragma once
#include <array>
#include <cstdint>
#include <memory>
#include <optional>
#include <string>
#include <vector>
namespace engine {
using PhysicsVector=std::array<float,3>;
struct BodyToken {
    uint64_t owner=0,incarnation=0;
    uint32_t body=UINT32_MAX;
    bool operator==(const BodyToken&) const=default;
};
struct PhysicsHit {BodyToken body;std::string stableId;float fraction=0;PhysicsVector position{},normal{};};
struct ContactEvent {
    enum class Kind { Added,Removed };
    Kind kind=Kind::Added;
    BodyToken a,b;std::string idA,idB;
    uint32_t subshapeA=0,subshapeB=0;
    PhysicsVector normal{};
};
class PhysicsWorld {
public:
    explicit PhysicsWorld(uint32_t maxBodies=2048);
    ~PhysicsWorld();
    PhysicsWorld(const PhysicsWorld&)=delete;
    PhysicsWorld& operator=(const PhysicsWorld&)=delete;
    BodyToken box(std::string id,PhysicsVector center,PhysicsVector halfExtent,std::array<float,4> rotation={0,0,0,1});
    BodyToken capsule(std::string id,PhysicsVector center,float radius,float halfCylinder,bool dynamic=false);
    BodyToken mesh(std::string id,PhysicsVector offset,const std::vector<PhysicsVector>& triangles);
    BodyToken heightfield(std::string id,PhysicsVector offset,uint32_t side,float spacing,const std::vector<float>& heights);
    bool remove(BodyToken);
    std::optional<PhysicsVector> position(BodyToken) const;
    void velocity(BodyToken,PhysicsVector);
    void step(float seconds);
    std::optional<PhysicsHit> ray(PhysicsVector origin,PhysicsVector direction,BodyToken ignore={}) const;
    std::optional<PhysicsHit> sphereSweep(PhysicsVector origin,float radius,PhysicsVector direction,BodyToken ignore={}) const;
    bool capsuleOverlap(PhysicsVector center,float radius,float halfCylinder,BodyToken ignore={}) const;
    std::vector<ContactEvent> takeContacts();
    size_t size() const;
private:
    friend class CharacterController;
    struct Impl;
    std::shared_ptr<Impl> impl_;
};
}

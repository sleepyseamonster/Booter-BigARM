#pragma once
#include "Simulation/World.h"
#include "Simulation/Actions.h"
#include "Simulation/FixedClock.h"
#include "Physics/CharacterController.h"
#include "Game/ThirdPersonCamera.h"
#include "Animation/AnimationPlayer.h"
namespace engine {
struct CalibrationFrame {
    PhysicsVector feet{},eye{},target{};
    float yaw=0;
    const std::vector<SkinMatrix>* palette=nullptr;
};
struct CalibrationCue {uint64_t sequence=0;std::string source;};
// Shared by player and workbench. UI and audio consume state/events, never own it.
class CalibrationRuntime {
public:
    explicit CalibrationRuntime(std::shared_ptr<const ModelData> model={});
    void advance(double seconds,bool paused,Actions&,float& cameraYaw,float& cameraPitch);
    CalibrationFrame present(float cameraYaw,float cameraPitch,float distance,float seconds);
    std::vector<CalibrationCue> takeCues();
    bool canInteract() const;
    bool targetActive() const {return targetActive_;}
    bool grounded() const {return character_.grounded();}
    const FixedClock& clock() const {return clock_;}
    const World& world() const {return world_;}
private:
    void tick(double,const ActionFrame&,float cameraYaw);
    World world_;
    PhysicsWorld physics_;
    CharacterController character_;
    ThirdPersonCamera camera_;
    EntityToken player_,target_;
    FixedClock clock_;
    std::shared_ptr<const ModelData> model_;
    std::unique_ptr<AnimationPlayer> animation_;
    float yaw_=0,walkBlend_=0;
    bool targetActive_=false;
    uint64_t cueSequence_=0;
    std::vector<CalibrationCue> cues_;
};
}

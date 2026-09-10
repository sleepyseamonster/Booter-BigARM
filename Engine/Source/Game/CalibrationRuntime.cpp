#include "Game/CalibrationRuntime.h"
#include "Game/Locomotion.h"
#include <algorithm>
#include <cmath>
#include <utility>
namespace engine {
CalibrationRuntime::CalibrationRuntime(std::shared_ptr<const ModelData> model,const PlayerSnapshot& initial)
    :character_(physics_,"authored:calibration:proxy",initial.feet),clock_(initial.ticks),model_(std::move(model)) {
    validateSnapshot(initial);yaw_=initial.yaw;targetActive_=initial.markerActive;
    player_=world_.create("authored:calibration:proxy",{{{0,0},{initial.feet[0],initial.feet[1],initial.feet[2]}},initial.yaw});
    target_=world_.create("authored:calibration:marker",{{{0,0},{2.1,.9,0}},0});
    groundBody_=physics_.box("authored:calibration:ground",{0,-.05f,0},{10,.05f,10});
    physics_.box("authored:calibration:marker",{2.1f,.9f,0},{.175f,.9f,.175f});
    character_.restore(initial.feet,initial.velocity);
    if(model_)animation_=std::make_unique<AnimationPlayer>(model_);
}
bool CalibrationRuntime::canInteract() const {
    const auto target=world_.snapshot(target_);if(!target)return false;
    const auto p=target->current.position.relativeTo({},world_.regionSpan(),4096);
    auto origin=character_.position();origin[1]+=1;
    PhysicsVector direction{};float squared=0;
    for(size_t i=0;i<3;++i){direction[i]=float(p[i])-origin[i];squared+=direction[i]*direction[i];}
    if(squared>2.5f*2.5f)return false;
    const auto hit=physics_.ray(origin,direction,character_.body());
    return hit && hit->stableId==target->id;
}
void CalibrationRuntime::tick(double seconds,const ActionFrame& input,float cameraYaw) {
    const auto intent=locomotionIntent(input,cameraYaw);
    const auto before=character_.position();
    auto proposed=before;for(size_t i=0;i<3;++i)proposed[i]+=intent.velocity[i]*float(seconds);
    world_.step(seconds);
    waitingForWorld_=streamingGuard_&&!streamingGuard_(before,proposed);
    if(waitingForWorld_){walkBlend_=0;return;}
    character_.step(float(seconds),intent.velocity,intent.jump);physics_.step(float(seconds));
    if(!streamingGuard_&&character_.position()[1]<-20)character_.restore({0,0,0});
    const auto feet=character_.position();
    const float moved=std::hypot(feet[0]-before[0],feet[2]-before[2]);
    walkBlend_+=std::clamp((moved>.01f*float(seconds)?1.f:0.f)-walkBlend_,-float(seconds)*5,float(seconds)*5);
    if(std::hypot(intent.velocity[0],intent.velocity[2])>.01f)yaw_=std::atan2(-intent.velocity[0],-intent.velocity[2]);
    world_.setSimulatedPose(player_,{{{0,0},{feet[0],feet[1],feet[2]}},yaw_});
    physics_.takeContacts();
    if(input[size_t(Action::Interact)].pressed && canInteract()) {
        if(cues_.size()>=64||cueSequence_==UINT64_MAX)throw std::runtime_error("Calibration event capacity exhausted");
        targetActive_=!targetActive_;cues_.push_back({++cueSequence_,"authored:calibration:marker"});
    }
}
void CalibrationRuntime::advance(double seconds,bool paused,Actions& actions,float& yaw,float& pitch) {
    if(!std::isfinite(yaw)||!std::isfinite(pitch))throw std::invalid_argument("Invalid calibration camera");
    clock_.advance(seconds,paused,[&](double dt,uint64_t) {
        const auto input=actions.takeTick();yaw-=input[size_t(Action::LookX)].value*float(dt)*2;
        pitch=std::clamp(pitch+input[size_t(Action::LookY)].value*float(dt)*1.5f,-.15f,1.2f);
        tick(dt,input,yaw);
    });
}
CalibrationFrame CalibrationRuntime::present(float yaw,float pitch,float distance,float seconds) {
    const auto entity=*world_.snapshot(player_);const auto a=entity.previous.position.relativeTo({},world_.regionSpan(),4096),b=entity.current.position.relativeTo({},world_.regionSpan(),4096);
    CalibrationFrame result;for(size_t i=0;i<3;++i)result.feet[i]=float(a[i]+clock_.alpha()*(b[i]-a[i]));
    const auto camera=camera_.update(physics_,result.feet,yaw,pitch,std::clamp(distance,2.5f,8.f),seconds,character_.body());
    result.eye=camera.eye;result.target=camera.target;result.yaw=yaw_;
    if(animation_)result.palette=model_->clips.empty()?&animation_->rest():&animation_->sample(0,clock_.seconds(),model_->clips.size()>1?1:SIZE_MAX,walkBlend_);
    return result;
}
void CalibrationRuntime::streamingGuard(std::function<bool(PhysicsVector,PhysicsVector)> guard){
    if(guard&&!streamingGuard_)physics_.remove(groundBody_);
    else if(!guard&&streamingGuard_)groundBody_=physics_.box("authored:calibration:ground",{0,-.05f,0},{10,.05f,10});
    streamingGuard_=std::move(guard);waitingForWorld_=bool(streamingGuard_);
}
void CalibrationRuntime::setRock(const std::string& id,const std::vector<PhysicsVector>& triangles,PhysicsVector offset) {
    if(rockId_.empty()){rockBody_=physics_.mesh(id,offset,triangles);rockId_=id;rockOffset_=offset;}
    else {if(id!=rockId_||offset!=rockOffset_)throw std::invalid_argument("Rock replacement must preserve placement identity");physics_.replaceMesh(rockBody_,triangles);}
}
PlayerSnapshot CalibrationRuntime::snapshot(float cameraYaw,float cameraPitch,float cameraDistance) const {
    PlayerSnapshot s;s.feet=character_.position();s.velocity=character_.velocity();s.yaw=yaw_;s.cameraYaw=cameraYaw;s.cameraPitch=cameraPitch;s.cameraDistance=std::clamp(cameraDistance,2.5f,8.f);s.markerActive=targetActive_;s.ticks=clock_.ticks();validateSnapshot(s);return s;
}
std::vector<CalibrationCue> CalibrationRuntime::takeCues(){return std::exchange(cues_,{});}
}

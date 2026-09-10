#include "Simulation/World.h"
#include <entt/entity/registry.hpp>
#include <algorithm>
#include <atomic>
#include <cmath>
#include <map>
#include <stdexcept>
namespace engine {
namespace {
struct Identity {std::string id;uint64_t incarnation;};
struct Transform {Pose previous,current;};
struct Velocity {std::array<double,3> value{};};
std::atomic<uint64_t> nextWorld{1};
Pose checkedPose(Pose pose,double span) {
    if(!std::isfinite(pose.yaw)) throw std::invalid_argument("Invalid entity rotation");
    pose.position=pose.position.normalized(span);return pose;
}
void checkedVelocity(const std::array<double,3>& velocity) {
    for(double v:velocity) if(!std::isfinite(v) || std::abs(v)>1000) throw std::invalid_argument("Velocity exceeds simulation bound");
}
}
struct World::Impl {
    entt::registry registry;
    std::map<std::string,entt::entity> identities;
    std::vector<WorldCommand> commands;
    double span;size_t limit;
    uint64_t owner=nextWorld.fetch_add(1),incarnation=0,lastSequence=0,applied=0,rejected=0;
    Impl(double regionSpan,size_t maxEntities):span(regionSpan),limit(maxEntities) {}
    EntityToken token(entt::entity e) const {return {owner,registry.get<Identity>(e).incarnation,uint32_t(entt::to_integral(e))};}
    bool valid(EntityToken t) const {
        const auto e=entt::entity(t.entity);
        return t.world==owner && registry.valid(e) && registry.get<Identity>(e).incarnation==t.incarnation;
    }
};
World::World(double regionSpan,size_t maxEntities):impl_(std::make_unique<Impl>(regionSpan,maxEntities)) {
    if(!std::isfinite(regionSpan) || regionSpan<=0 || regionSpan>1e6 || !maxEntities || maxEntities>1000000) throw std::invalid_argument("Invalid world bounds");
}
World::~World()=default;
EntityToken World::create(std::string id,Pose pose) {
    auto& w=*impl_;
    if(id.empty() || id.size()>256 || (!id.starts_with("generated:")&&!id.starts_with("authored:"))) throw std::invalid_argument("Expected stable generated/authored identity");
    if(std::any_of(id.begin(),id.end(),[](unsigned char c){return c<33 || c>126;})) throw std::invalid_argument("Invalid identity characters");
    if(w.identities.contains(id)) throw std::invalid_argument("Duplicate live identity");
    if(w.identities.size()>=w.limit || w.incarnation==UINT64_MAX) throw std::runtime_error("World entity budget exhausted");
    pose=checkedPose(pose,w.span);
    const auto entity=w.registry.create();
    try {
        w.registry.emplace<Identity>(entity,id,++w.incarnation);
        w.registry.emplace<Transform>(entity,pose,pose);
        w.registry.emplace<Velocity>(entity);
        w.identities.emplace(std::move(id),entity);
    } catch(...) {w.registry.destroy(entity);throw;}
    return w.token(entity);
}
bool World::remove(EntityToken token) {
    auto& w=*impl_;if(!w.valid(token)) return false;
    const auto e=entt::entity(token.entity);
    w.identities.erase(w.registry.get<Identity>(e).id);w.registry.destroy(e);return true;
}
size_t World::unloadRegion(Region region) {
    size_t removed=0;
    for(const auto& entity:ordered()) if(entity.current.position.region==region) removed+=remove(entity.token);
    return removed;
}
std::optional<EntityToken> World::find(const std::string& id) const {
    const auto& w=*impl_;const auto found=w.identities.find(id);
    if(found==w.identities.end()) return {};return w.token(found->second);
}
std::optional<EntitySnapshot> World::snapshot(EntityToken token) const {
    const auto& w=*impl_;if(!w.valid(token)) return {};
    const auto e=entt::entity(token.entity);const auto& transform=w.registry.get<Transform>(e);
    return EntitySnapshot{w.registry.get<Identity>(e).id,token,transform.previous,transform.current,w.registry.get<Velocity>(e).value};
}
std::vector<EntitySnapshot> World::ordered() const {
    std::vector<EntitySnapshot> result;result.reserve(size());
    for(const auto& [id,e]:impl_->identities) result.push_back(*snapshot(impl_->token(e)));
    return result;
}
void World::enqueue(WorldCommand command) {
    auto& w=*impl_;
    if(w.commands.size()>=1024) throw std::runtime_error("Simulation command queue full");
    if(command.sequence<=w.lastSequence || command.sequence==0 || std::any_of(w.commands.begin(),w.commands.end(),[&](const auto& c){return c.sequence==command.sequence;})) throw std::invalid_argument("Duplicate or expired command sequence");
    switch(command.kind) {
    case WorldCommand::Kind::Velocity:checkedVelocity(command.velocity);break;
    case WorldCommand::Kind::Teleport:command.pose=checkedPose(command.pose,w.span);break;
    case WorldCommand::Kind::Remove:break;
    default:throw std::invalid_argument("Unknown world command");
    }
    w.commands.push_back(std::move(command));
}
void World::step(double seconds) {
    if(!std::isfinite(seconds) || seconds<=0 || seconds>.1) throw std::invalid_argument("Invalid simulation step");
    auto& w=*impl_;
    for(const auto& [id,e]:w.identities) {auto& t=w.registry.get<Transform>(e);t.previous=t.current;}
    std::sort(w.commands.begin(),w.commands.end(),[](const auto& a,const auto& b){return a.sequence<b.sequence;});
    for(const auto& command:w.commands) {
        w.lastSequence=command.sequence;
        if(!w.valid(command.target)) {++w.rejected;continue;}
        const auto e=entt::entity(command.target.entity);
        switch(command.kind) {
        case WorldCommand::Kind::Remove:remove(command.target);break;
        case WorldCommand::Kind::Velocity:w.registry.get<Velocity>(e).value=command.velocity;break;
        case WorldCommand::Kind::Teleport:w.registry.get<Transform>(e)={command.pose,command.pose};break;
        }
        ++w.applied;
    }
    w.commands.clear();
    // Free kinematic integration is the initial physics-independent component
    // consumer. The Jolt motor will own physical poses when introduced in P09/P10.
    for(const auto& [id,e]:w.identities) {
        auto& t=w.registry.get<Transform>(e);const auto& v=w.registry.get<Velocity>(e).value;
        auto next=t.current;
        for(size_t i=0;i<3;++i) next.position.local[i]+=v[i]*seconds;
        next.position=next.position.normalized(w.span);t.current=next;
    }
}
size_t World::size() const {return impl_->identities.size();}
uint64_t World::appliedCommands() const {return impl_->applied;}
uint64_t World::rejectedCommands() const {return impl_->rejected;}
double World::regionSpan() const {return impl_->span;}
}

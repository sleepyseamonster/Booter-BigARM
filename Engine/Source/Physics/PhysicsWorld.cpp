#include "Physics/PhysicsWorld.h"
#include "Physics/CharacterController.h"
#include <Jolt/Jolt.h>
#include <Jolt/RegisterTypes.h>
#include <Jolt/Physics/Character/CharacterVirtual.h>
#include <Jolt/Physics/Collision/Shape/RotatedTranslatedShape.h>
#include <Jolt/Core/Factory.h>
#include <Jolt/Core/TempAllocator.h>
#include <Jolt/Core/JobSystemSingleThreaded.h>
#include <Jolt/Physics/PhysicsSystem.h>
#include <Jolt/Physics/Body/BodyCreationSettings.h>
#include <Jolt/Physics/Body/BodyLock.h>
#include <Jolt/Physics/Collision/ContactListener.h>
#include <Jolt/Physics/Collision/CollisionCollectorImpl.h>
#include <Jolt/Physics/Collision/RayCast.h>
#include <Jolt/Physics/Collision/CastResult.h>
#include <Jolt/Physics/Collision/ShapeCast.h>
#include <Jolt/Physics/Collision/Shape/BoxShape.h>
#include <Jolt/Physics/Collision/Shape/CapsuleShape.h>
#include <Jolt/Physics/Collision/Shape/SphereShape.h>
#include <Jolt/Physics/Collision/Shape/MeshShape.h>
#include <Jolt/Physics/Collision/Shape/HeightFieldShape.h>
#include <algorithm>
#include <atomic>
#include <cmath>
#include <map>
#include <mutex>
#include <stdexcept>
#include <tuple>
namespace engine {
namespace {
using namespace JPH;
struct JoltContext {
    JoltContext() {RegisterDefaultAllocator();Factory::sInstance=new Factory;RegisterTypes();}
    ~JoltContext() {UnregisterTypes();delete Factory::sInstance;Factory::sInstance=nullptr;}
};
std::shared_ptr<JoltContext> context() {
    static std::mutex mutex;static std::weak_ptr<JoltContext> weak;
    const std::lock_guard lock(mutex);
    auto result=weak.lock();if(!result) {result=std::make_shared<JoltContext>();weak=result;}return result;
}
std::atomic<uint64_t> nextOwner{1};
constexpr ObjectLayer staticLayer=0,dynamicLayer=1;
class BroadLayers final:public BroadPhaseLayerInterface {
public:
    uint GetNumBroadPhaseLayers() const override {return 2;}
    BroadPhaseLayer GetBroadPhaseLayer(ObjectLayer layer) const override {return BroadPhaseLayer(uint8_t(layer));}
#if defined(JPH_EXTERNAL_PROFILE) || defined(JPH_PROFILE_ENABLED)
    const char* GetBroadPhaseLayerName(BroadPhaseLayer layer) const override {return layer.GetValue()==0?"Static":"Dynamic";}
#endif
};
class BroadFilter final:public ObjectVsBroadPhaseLayerFilter {
public:bool ShouldCollide(ObjectLayer a,BroadPhaseLayer b) const override {return a==dynamicLayer || b.GetValue()==dynamicLayer;}
};
class PairFilter final:public ObjectLayerPairFilter {
public:bool ShouldCollide(ObjectLayer a,ObjectLayer b) const override {return a==dynamicLayer || b==dynamicLayer;}
};
class IgnoreBody final:public BodyFilter {
public:
    explicit IgnoreBody(BodyID id):id_(id) {}
    bool ShouldCollide(const BodyID& id) const override {return id!=id_;}
private:BodyID id_;
};
Vec3 vec(PhysicsVector p) {return {p[0],p[1],p[2]};}
PhysicsVector array(Vec3Arg p) {return {p.GetX(),p.GetY(),p.GetZ()};}
void bounded(PhysicsVector v,float limit=8192) {for(float x:v) if(!std::isfinite(x) || std::abs(x)>limit) throw std::invalid_argument("Physics vector outside local bounds");}
void dimension(float v) {if(!std::isfinite(v) || v<.01f || v>1024) throw std::invalid_argument("Invalid collision dimension");}
void capsuleDimensions(float radius,float half) {dimension(radius);if(!std::isfinite(half) || half<0 || half>1024) throw std::invalid_argument("Invalid capsule length");}
using ContactKey=std::array<uint32_t,4>;
ContactKey key(const BodyID& a,uint32_t subA,const BodyID& b,uint32_t subB) {return {a.GetIndexAndSequenceNumber(),subA,b.GetIndexAndSequenceNumber(),subB};}
}
struct PhysicsWorld::Impl:public JPH::ContactListener {
    std::shared_ptr<JoltContext> runtime=context();
    BroadLayers broad;BroadFilter broadFilter;PairFilter pairFilter;
    JPH::TempAllocatorImpl allocator{16*1024*1024};
    JPH::JobSystemSingleThreaded jobs{JPH::cMaxPhysicsJobs};
    JPH::PhysicsSystem system;
    struct Record {BodyToken token;std::string id;bool characterOwned=false;};
    std::map<uint32_t,Record> bodies;
    std::map<std::string,uint32_t> identities;
    std::map<ContactKey,ContactEvent> activeContacts;
    std::vector<ContactEvent> events;
    uint64_t owner=nextOwner.fetch_add(1),incarnation=0;
    uint32_t limit;bool eventOverflow=false;
    explicit Impl(uint32_t maxBodies):limit(maxBodies) {
        if(maxBodies<1 || maxBodies>16384) throw std::invalid_argument("Physics body limit must be 1..16384");
        system.Init(maxBodies,0,8192,16384,broad,broadFilter,pairFilter);
        system.SetContactListener(this);events.reserve(8192);
    }
    ~Impl() {
        system.SetContactListener(nullptr);
        auto& interface=system.GetBodyInterface();
        for(const auto& [raw,record]:bodies) {const JPH::BodyID id(raw);interface.RemoveBody(id);interface.DestroyBody(id);}
    }
    bool valid(BodyToken token) const {
        const auto found=bodies.find(token.body);return token.owner==owner && found!=bodies.end() && found->second.token==token;
    }
    void checkId(const std::string& id) const {
        if(id.empty() || id.size()>256 || (!id.starts_with("generated:")&&!id.starts_with("authored:"))) throw std::invalid_argument("Expected stable collision identity");
        if(identities.contains(id)) throw std::invalid_argument("Duplicate collision identity");
        if(bodies.size()>=limit || incarnation==UINT64_MAX) throw std::runtime_error("Physics body budget exhausted");
    }
    BodyToken adopt(std::string id,const JPH::Shape* shape,PhysicsVector center,JPH::Quat rotation,bool dynamic) {
        checkId(id);bounded(center);
        JPH::BodyCreationSettings settings(shape,vec(center),rotation,dynamic?JPH::EMotionType::Dynamic:JPH::EMotionType::Static,dynamic?dynamicLayer:staticLayer);
        auto& interface=system.GetBodyInterface();auto* body=interface.CreateBody(settings);
        if(!body) throw std::runtime_error("Jolt body allocation failed");
        const auto raw=body->GetID().GetIndexAndSequenceNumber();const BodyToken token{owner,++incarnation,raw};
        try {bodies.emplace(raw,Record{token,id});identities.emplace(id,raw);}
        catch(...) {bodies.erase(raw);identities.erase(id);interface.DestroyBody(body->GetID());throw;}
        interface.AddBody(body->GetID(),dynamic?JPH::EActivation::Activate:JPH::EActivation::DontActivate);
        return token;
    }
    void emit(ContactEvent event) {if(events.size()<8192) events.push_back(std::move(event));else eventOverflow=true;}
    void OnContactAdded(const JPH::Body& a,const JPH::Body& b,const JPH::ContactManifold& manifold,JPH::ContactSettings&) override {
        const auto ia=bodies.find(a.GetID().GetIndexAndSequenceNumber()),ib=bodies.find(b.GetID().GetIndexAndSequenceNumber());
        if(ia==bodies.end() || ib==bodies.end()) return;
        ContactEvent event{ContactEvent::Kind::Added,ia->second.token,ib->second.token,ia->second.id,ib->second.id,
            manifold.mSubShapeID1.GetValue(),manifold.mSubShapeID2.GetValue(),array(manifold.mWorldSpaceNormal)};
        const auto pair=key(a.GetID(),event.subshapeA,b.GetID(),event.subshapeB);
        if(event.idB<event.idA) {std::swap(event.a,event.b);std::swap(event.idA,event.idB);std::swap(event.subshapeA,event.subshapeB);for(auto& n:event.normal)n=-n;}
        if(activeContacts.size()>=8192) {eventOverflow=true;return;}
        activeContacts[pair]=event;emit(std::move(event));
    }
    void OnContactRemoved(const JPH::SubShapeIDPair& pair) override {
        const auto found=activeContacts.find(key(pair.GetBody1ID(),pair.GetSubShapeID1().GetValue(),pair.GetBody2ID(),pair.GetSubShapeID2().GetValue()));
        if(found!=activeContacts.end()) {auto event=found->second;event.kind=ContactEvent::Kind::Removed;emit(std::move(event));activeContacts.erase(found);}
    }
    BodyID ignored(BodyToken token) const {return valid(token)?BodyID(token.body):BodyID();}
    PhysicsHit hit(BodyID body,float fraction,PhysicsVector position,PhysicsVector normal) const {
        const auto& record=bodies.at(body.GetIndexAndSequenceNumber());return {record.token,record.id,fraction,position,normal};
    }
};
PhysicsWorld::PhysicsWorld(uint32_t maxBodies):impl_(std::make_shared<Impl>(maxBodies)) {}
PhysicsWorld::~PhysicsWorld()=default;
BodyToken PhysicsWorld::box(std::string id,PhysicsVector center,PhysicsVector half,std::array<float,4> rotation) {
    impl_->checkId(id);for(float v:half) dimension(v);float norm=0;
    for(float v:rotation) {if(!std::isfinite(v)) throw std::invalid_argument("Invalid collision rotation");norm+=v*v;}
    if(std::abs(norm-1)>.001f) throw std::invalid_argument("Collision quaternion must be normalized");
    const JPH::RefConst<JPH::Shape> shape=new JPH::BoxShape(vec(half),std::min(.05f,*std::min_element(half.begin(),half.end())*.5f));
    return impl_->adopt(std::move(id),shape,center,{rotation[0],rotation[1],rotation[2],rotation[3]},false);
}
BodyToken PhysicsWorld::capsule(std::string id,PhysicsVector center,float radius,float half,bool dynamic) {
    impl_->checkId(id);capsuleDimensions(radius,half);
    const JPH::RefConst<JPH::Shape> shape=half>0?static_cast<JPH::Shape*>(new JPH::CapsuleShape(half,radius)):static_cast<JPH::Shape*>(new JPH::SphereShape(radius));
    return impl_->adopt(std::move(id),shape,center,JPH::Quat::sIdentity(),dynamic);
}
namespace {
JPH::RefConst<JPH::Shape> triangleShape(const std::vector<PhysicsVector>& vertices) {
    if(vertices.empty() || vertices.size()%3 || vertices.size()>300000) throw std::invalid_argument("Expected bounded triangle triples");
    JPH::TriangleList triangles;triangles.reserve(vertices.size()/3);
    for(size_t i=0;i<vertices.size();i+=3) {
        for(size_t j=0;j<3;++j) bounded(vertices[i+j]);
        if((vec(vertices[i+1])-vec(vertices[i])).Cross(vec(vertices[i+2])-vec(vertices[i])).LengthSq()<1e-12f) throw std::invalid_argument("Degenerate collision triangle");
        triangles.emplace_back(vec(vertices[i]),vec(vertices[i+1]),vec(vertices[i+2]));
    }
    const auto result=JPH::MeshShapeSettings(triangles).Create();
    if(result.HasError()) throw std::runtime_error(result.GetError().c_str());
    return result.Get();
}
}
BodyToken PhysicsWorld::mesh(std::string id,PhysicsVector offset,const std::vector<PhysicsVector>& vertices) {
    impl_->checkId(id);bounded(offset);return impl_->adopt(std::move(id),triangleShape(vertices),offset,JPH::Quat::sIdentity(),false);
}
void PhysicsWorld::replaceMesh(BodyToken token,const std::vector<PhysicsVector>& vertices) {
    if(!impl_->valid(token)||impl_->bodies.at(token.body).characterOwned)throw std::invalid_argument("Invalid mesh replacement owner");
    auto& bodies=impl_->system.GetBodyInterface();const JPH::BodyID id(token.body);
    if(bodies.GetMotionType(id)!=JPH::EMotionType::Static)throw std::invalid_argument("Mesh replacement requires a static body");
    const auto shape=triangleShape(vertices); // Preserve old body/identity if preparation fails.
    bodies.SetShape(id,shape,false,JPH::EActivation::DontActivate);
}
BodyToken PhysicsWorld::heightfield(std::string id,PhysicsVector offset,uint32_t side,float spacing,const std::vector<float>& heights) {
    impl_->checkId(id);bounded(offset);dimension(spacing);
    if(side<8 || side>512 || (side&(side-1)) || heights.size()!=size_t(side)*side || spacing*side>8192) throw std::invalid_argument("Heightfield requires an 8..512 power-of-two square grid");
    for(float v:heights) if(!std::isfinite(v) || std::abs(v)>4096) throw std::invalid_argument("Invalid height sample");
    const auto result=JPH::HeightFieldShapeSettings(heights.data(),JPH::Vec3::sZero(),{spacing,1,spacing},side).Create();
    if(result.HasError()) throw std::runtime_error(result.GetError().c_str());
    return impl_->adopt(std::move(id),result.Get(),offset,JPH::Quat::sIdentity(),false);
}
bool PhysicsWorld::remove(BodyToken token) {
    auto& w=*impl_;if(!w.valid(token) || w.bodies.at(token.body).characterOwned) return false;
    auto& interface=w.system.GetBodyInterface();interface.RemoveBody(JPH::BodyID(token.body));interface.DestroyBody(JPH::BodyID(token.body));
    w.identities.erase(w.bodies.at(token.body).id);w.bodies.erase(token.body);return true;
}
std::optional<PhysicsVector> PhysicsWorld::position(BodyToken token) const {
    if(!impl_->valid(token)) return {};return array(impl_->system.GetBodyInterface().GetPosition(JPH::BodyID(token.body)));
}
void PhysicsWorld::velocity(BodyToken token,PhysicsVector value) {
    bounded(value,1000);if(!impl_->valid(token)) throw std::invalid_argument("Expired physics body");
    auto& interface=impl_->system.GetBodyInterface();const JPH::BodyID id(token.body);
    if(interface.GetMotionType(id)!=JPH::EMotionType::Dynamic) throw std::invalid_argument("Velocity requires dynamic body");
    interface.SetLinearVelocity(id,vec(value));interface.ActivateBody(id);
}
void PhysicsWorld::step(float seconds) {
    if(!std::isfinite(seconds) || seconds<=0 || seconds>.1f) throw std::invalid_argument("Invalid physics step");
    const auto error=impl_->system.Update(seconds,1,&impl_->allocator,&impl_->jobs);
    if(error!=JPH::EPhysicsUpdateError::None) throw std::runtime_error("Jolt update exceeded contact/body-pair capacity");
    if(impl_->eventOverflow) throw std::runtime_error("Physics contact event capacity exceeded");
}
std::optional<PhysicsHit> PhysicsWorld::ray(PhysicsVector origin,PhysicsVector direction,BodyToken ignore) const {
    bounded(origin);bounded(direction);if(vec(direction).LengthSq()<1e-12f) throw std::invalid_argument("Empty physics ray");
    const auto& w=*impl_;JPH::RayCastResult result;
    const JPH::RRayCast ray(vec(origin),vec(direction));
    if(!w.system.GetNarrowPhaseQuery().CastRay(ray,result,{},{},IgnoreBody(w.ignored(ignore)))) return {};
    const auto point=ray.GetPointOnRay(result.mFraction);
    JPH::BodyLockRead lock(w.system.GetBodyLockInterface(),result.mBodyID);
    const auto normal=lock.Succeeded()?lock.GetBody().GetWorldSpaceSurfaceNormal(result.mSubShapeID2,point):JPH::Vec3::sZero();
    return w.hit(result.mBodyID,result.mFraction,array(point),array(normal));
}
std::optional<PhysicsHit> PhysicsWorld::sphereSweep(PhysicsVector origin,float radius,PhysicsVector direction,BodyToken ignore) const {
    bounded(origin);bounded(direction);dimension(radius);const auto& w=*impl_;
    const JPH::SphereShape shape(radius);
    JPH::ShapeCastSettings settings;settings.mReturnDeepestPoint=true;
    JPH::ClosestHitCollisionCollector<JPH::CastShapeCollector> collector;
    const JPH::RShapeCast cast(&shape,JPH::Vec3::sOne(),JPH::RMat44::sTranslation(vec(origin)),vec(direction));
    w.system.GetNarrowPhaseQuery().CastShape(cast,settings,JPH::RVec3::sZero(),collector,{},{},IgnoreBody(w.ignored(ignore)));
    if(!collector.HadHit()) return {};
    const auto& hit=collector.mHit;const auto normal=-hit.mPenetrationAxis.NormalizedOr(JPH::Vec3::sAxisY());
    return w.hit(hit.mBodyID2,std::max(0.0f,hit.mFraction),array(hit.mContactPointOn2),array(normal));
}
bool PhysicsWorld::capsuleOverlap(PhysicsVector center,float radius,float half,BodyToken ignore) const {
    bounded(center);capsuleDimensions(radius,half);const auto& w=*impl_;
    const JPH::RefConst<JPH::Shape> shape=half>0?static_cast<JPH::Shape*>(new JPH::CapsuleShape(half,radius)):static_cast<JPH::Shape*>(new JPH::SphereShape(radius));
    JPH::AnyHitCollisionCollector<JPH::CollideShapeCollector> collector;
    w.system.GetNarrowPhaseQuery().CollideShape(shape,JPH::Vec3::sOne(),JPH::RMat44::sTranslation(vec(center)),{},JPH::RVec3::sZero(),collector,{},{},IgnoreBody(w.ignored(ignore)));
    return collector.HadHit();
}
std::vector<ContactEvent> PhysicsWorld::takeContacts() {
    if(impl_->events.empty()) return {};
    std::vector<ContactEvent> events;events.swap(impl_->events);impl_->events.reserve(8192);
    std::sort(events.begin(),events.end(),[](const auto& a,const auto& b){return std::tie(a.idA,a.idB,a.subshapeA,a.subshapeB,a.kind)<std::tie(b.idA,b.idB,b.subshapeA,b.subshapeB,b.kind);});
    return events;
}
size_t PhysicsWorld::size() const {return impl_->bodies.size();}
struct CharacterController::Impl {
    std::shared_ptr<PhysicsWorld::Impl> world;
    JPH::Ref<JPH::CharacterVirtual> character;
    CharacterConfig config;
    BodyToken token;
    Impl(std::shared_ptr<PhysicsWorld::Impl> owner,std::string id,PhysicsVector feet,CharacterConfig settings):world(std::move(owner)),config(settings) {
        bounded(feet);dimension(config.radius);dimension(config.height);
        if(config.height<=2*config.radius || !std::isfinite(config.maxSlopeDegrees) || config.maxSlopeDegrees<1 || config.maxSlopeDegrees>80 ||
            !std::isfinite(config.stepHeight) || config.stepHeight<0 || config.stepHeight>1 || !std::isfinite(config.jumpSpeed) || config.jumpSpeed<0 || config.jumpSpeed>20)
            throw std::invalid_argument("Invalid character capsule configuration");
        world->checkId(id);
        const float half=config.height*.5f-config.radius;
        const auto shape=JPH::RotatedTranslatedShapeSettings({0,config.height*.5f,0},JPH::Quat::sIdentity(),new JPH::CapsuleShape(half,config.radius)).Create();
        const auto inner=JPH::RotatedTranslatedShapeSettings({0,config.height*.5f,0},JPH::Quat::sIdentity(),new JPH::CapsuleShape(half*.98f,config.radius*.98f)).Create();
        if(shape.HasError() || inner.HasError()) throw std::runtime_error("Character shape creation failed");
        JPH::CharacterVirtualSettings options;options.mShape=shape.Get();options.mInnerBodyShape=inner.Get();options.mInnerBodyLayer=dynamicLayer;
        options.mMaxSlopeAngle=config.maxSlopeDegrees*JPH::JPH_PI/180.0f;
        options.mSupportingVolume=JPH::Plane(JPH::Vec3::sAxisY(),-config.radius);
        options.mEnhancedInternalEdgeRemoval=true;
        character=new JPH::CharacterVirtual(&options,vec(feet),JPH::Quat::sIdentity(),0,&world->system);
        character->RefreshContacts(world->system.GetDefaultBroadPhaseLayerFilter(dynamicLayer),world->system.GetDefaultLayerFilter(dynamicLayer),{},{},world->allocator);
        const auto raw=character->GetInnerBodyID().GetIndexAndSequenceNumber();
        if(character->GetInnerBodyID().IsInvalid()) throw std::runtime_error("Character query body allocation failed");
        token={world->owner,++world->incarnation,raw};
        try {world->bodies.emplace(raw,PhysicsWorld::Impl::Record{token,id,true});world->identities.emplace(id,raw);}
        catch(...) {world->bodies.erase(raw);world->identities.erase(id);throw;}
    }
    ~Impl() {
        world->identities.erase(world->bodies.at(token.body).id);world->bodies.erase(token.body);
        character=nullptr;
    }
};
CharacterController::CharacterController(PhysicsWorld& world,std::string id,PhysicsVector feet,CharacterConfig config):impl_(std::make_unique<Impl>(world.impl_,std::move(id),feet,config)) {}
CharacterController::~CharacterController()=default;
void CharacterController::step(float seconds,PhysicsVector horizontal,bool jump) {
    if(!std::isfinite(seconds) || seconds<=0 || seconds>.1f) throw std::invalid_argument("Invalid character step");
    bounded(horizontal,30);
    auto& c=*impl_->character;auto& w=*impl_->world;c.UpdateGroundVelocity();
    const auto ground=c.GetGroundVelocity();const bool supported=c.GetGroundState()==JPH::CharacterBase::EGroundState::OnGround;
    float vertical=c.GetLinearVelocity().GetY();
    if(supported && vertical-ground.GetY()<.1f) vertical=ground.GetY()+(jump?impl_->config.jumpSpeed:0);
    JPH::Vec3 velocity(horizontal[0],vertical,horizontal[2]);
    if(supported) velocity+=JPH::Vec3(ground.GetX(),0,ground.GetZ());
    velocity+=w.system.GetGravity()*seconds;c.SetLinearVelocity(velocity);
    JPH::CharacterVirtual::ExtendedUpdateSettings settings;
    settings.mWalkStairsStepUp={0,impl_->config.stepHeight,0};
    settings.mStickToFloorStepDown={0,-impl_->config.stepHeight,0};
    c.ExtendedUpdate(seconds,w.system.GetGravity(),settings,w.system.GetDefaultBroadPhaseLayerFilter(dynamicLayer),w.system.GetDefaultLayerFilter(dynamicLayer),{},{},w.allocator);
    if(c.GetMaxHitsExceeded()) throw std::runtime_error("Character contact query capacity exceeded");
}
void CharacterController::restore(PhysicsVector feet,PhysicsVector velocity) {
    bounded(feet);bounded(velocity,100);auto& c=*impl_->character;auto& w=*impl_->world;
    c.SetPosition(vec(feet));c.SetLinearVelocity(vec(velocity));
    c.RefreshContacts(w.system.GetDefaultBroadPhaseLayerFilter(dynamicLayer),w.system.GetDefaultLayerFilter(dynamicLayer),{},{},w.allocator);
}
PhysicsVector CharacterController::position() const {return array(impl_->character->GetPosition());}
PhysicsVector CharacterController::velocity() const {return array(impl_->character->GetLinearVelocity());}
bool CharacterController::grounded() const {return impl_->character->GetGroundState()==JPH::CharacterBase::EGroundState::OnGround;}
BodyToken CharacterController::body() const {return impl_->token;}

}

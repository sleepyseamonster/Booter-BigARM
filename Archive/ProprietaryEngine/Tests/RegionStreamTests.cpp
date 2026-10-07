#include "World/Streaming/RegionStream.h"
#include "Game/CalibrationRuntime.h"
#include "Core/Visibility.h"
#include <chrono>
#include <iostream>
#include <set>
using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
void ready(RegionStream& stream,const std::vector<StreamAnchor>& anchors){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(20);
    for(;;){stream.update(anchors);bool done=!stream.slots().empty();for(const auto& [_,slot]:stream.slots()){if(!slot.error.empty())throw std::runtime_error(slot.error);if(slot.desired)done=done&&slot.ready.collision;}
        if(done)return;if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Region loading timed out");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
}
}
int main()try{
    CalibrationRuntime runtime;const size_t baseline=runtime.physics().size();
    std::map<RegionKey,BodyToken> bodies;std::vector<Region> adopted;
    RegionStream stream({}, {}, {},[&](const RegionContent& c){const auto r=c.terrain.region;auto token=runtime.physics().mesh(c.terrain.id.text(),WorldPosition{r,{}}.relativeTo({},256,4096),c.collision);bodies.emplace(RegionKey{r.x,r.z},token);adopted.push_back(r);return RegionReadiness{true,true};},[&](Region r){auto it=bodies.find({r.x,r.z});if(it!=bodies.end()){runtime.physics().remove(it->second);bodies.erase(it);}});
    runtime.streamingGuard([&](PhysicsVector a,PhysicsVector b){return stream.collisionReady({{},{a[0],a[1],a[2]}},{{},{b[0],b[1],b[2]}},1);});
    Actions actions;actions.set(Control::A,1);float yaw=0,pitch=.3f;
    runtime.advance(1./60,false,actions,yaw,pitch);require(runtime.waitingForWorld()&&runtime.feet()==PhysicsVector{0,0,0},"Unready collision allowed movement or falling");
    const std::vector<StreamAnchor> home{{{{},{32,0,32}},0}};
    ready(stream,home);require(adopted.front()==Region{}&&bodies.size()==9,"Primary center priority or initial residency");
    const auto initialId=stream.slots().at({0,0}).content->terrain.id;std::vector<std::string> ids;for(const auto& p:stream.slots().at({0,0}).content->terrain.rocks)ids.push_back(p.id.text());
    require(stream.collisionReady({{},{-.1,0,.1}},{{},{.1,0,.1}},1),"Loaded border was not collision ready");
    const auto hit=runtime.physics().ray({128,20,128},{0,-50,0});require(hit&&hit->stableId==initialId.text(),"Real streamed collider/query missing");
    for(int i=0;i<6;++i)runtime.advance(1./60,false,actions,yaw,pitch);
    require(!runtime.waitingForWorld()&&runtime.feet()[0]<0&&runtime.feet()[1]>-.1f,"Character failed loaded negative border traversal");
    const auto frozen=runtime.feet();stream.update({});runtime.advance(1./60,false,actions,yaw,pitch);require(runtime.waitingForWorld()&&runtime.feet()==frozen&&bodies.empty(),"Retirement failed to stop unsafe traversal");
    require(stream.residentBytes()==0&&runtime.physics().size()==baseline-1,"Retirement leaked static bodies or CPU residency");
    ready(stream,home);require(stream.slots().at({0,0}).content->terrain.id==initialId,"Reload changed region identity");
    std::vector<std::string> returned;for(const auto& p:stream.slots().at({0,0}).content->terrain.rocks)returned.push_back(p.id.text());require(returned==ids,"Reload changed rock placement identity");
    const auto retireBefore=stream.retired();ready(stream,{{{{},{255.9,0,32}},0}});stream.update({{{{},{256.1,0,32}},0}});
    require(stream.slots().contains({-1,0})&&stream.retired()==retireBefore,"Border crossing ignored unload hysteresis");
    ready(stream,{{{{},{32,0,32}},0},{{{4,0},{32,0,32}},1}});
    require(stream.slots().size()<=25&&stream.slots().at({4,0}).ready.collision&&stream.residentBytes()<64*1024*1024,"Multi-anchor residency or byte bound");
    // A request retired before its result arrives must not resurrect a region.
    stream.update({{{{-4,0},{32,0,32}},0}});stream.update({});
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(5);
    while(stream.jobStats().outstanding){stream.update({});if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Cancelled region jobs did not retire");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
    require(stream.slots().empty()&&bodies.empty()&&stream.residentBytes()==0,"Stale job republished after retirement");
    runtime.streamingGuard({});require(runtime.physics().size()==baseline,"Runtime ground restoration changed body baseline");
    {
        unsigned centerAttempts=0;
        RegionStream retrying({}, {}, {},[&](const RegionContent& content){if(content.terrain.region==Region{}&&centerAttempts++==0)throw std::runtime_error("transient attach failure");return RegionReadiness{true,true};},[](Region){});
        const auto retryDeadline=std::chrono::steady_clock::now()+std::chrono::seconds(20);
        for(;;){retrying.update(home);const auto found=retrying.slots().find({0,0});if(found!=retrying.slots().end()&&found->second.state==RegionStreamState::Ready)break;if(std::chrono::steady_clock::now()>retryDeadline)throw std::runtime_error("Retryable stream failure did not recover");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
        require(centerAttempts==2,"Transient stream failure retry count");
    }
    {
        RegionStream failing({}, {}, {},[](const RegionContent& content){if(content.terrain.region==Region{})throw std::runtime_error("permanent attach failure");return RegionReadiness{true,true};},[](Region){});
        const auto failureDeadline=std::chrono::steady_clock::now()+std::chrono::seconds(20);
        for(;;){failing.update(home);const auto found=failing.slots().find({0,0});if(found!=failing.slots().end()&&found->second.state==RegionStreamState::PermanentFailed)break;if(std::chrono::steady_clock::now()>failureDeadline)throw std::runtime_error("Permanent stream failure retried forever");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
        require(failing.slots().at({0,0}).failures==3&&!failing.slots().at({0,0}).error.empty(),"Permanent stream failure state");
    }
    const float identity[]={1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};require(visibleSphere(identity,{0,0,.5f},.1f,false)&&!visibleSphere(identity,{3,0,.5f},.1f,false)&&!visibleSphere(identity,{0,0,-.5f},.1f,false)&&visibleSphere(identity,{0,0,-.5f},.1f,true),"Frustum depth or side planes");
    std::cout<<"PASS: prioritized region jobs, bounded retry/permanent failure states, real Jolt attachment, safe traversal, hysteresis, stable reload identity, retirement/cancellation and frustum conventions; adopted="<<adopted.size()<<", retired="<<stream.retired()<<'\n';return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

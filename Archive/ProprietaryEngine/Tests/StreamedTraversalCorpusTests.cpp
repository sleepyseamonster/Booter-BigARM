#include "World/Streaming/RegionStream.h"
#include "Physics/PhysicsWorld.h"
#include "Persistence/Document.h"
#include <algorithm>
#include <chrono>
#include <cmath>
#include <iostream>
#include <map>
#include <thread>

using namespace engine;
namespace {
using Clock=std::chrono::steady_clock;
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
double percentile(std::vector<double> values,double p){
    if(values.empty())throw std::runtime_error("Missing traversal timings");
    std::sort(values.begin(),values.end());
    return values[std::min(values.size()-1,size_t(std::ceil(p*values.size())-1))];
}
double ready(RegionStream& stream,const StreamAnchor& anchor){
    const auto start=Clock::now(),deadline=start+std::chrono::seconds(20);
    for(;;){
        stream.update({anchor});bool complete=!stream.slots().empty();
        for(const auto& [_,slot]:stream.slots()){
            if(!slot.error.empty())throw std::runtime_error(slot.error);
            if(slot.desired)complete=complete&&slot.ready.collision;
        }
        if(complete)return std::chrono::duration<double,std::milli>(Clock::now()-start).count();
        if(Clock::now()>deadline)throw std::runtime_error("Traversal corpus region loading timed out");
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}
}
int main(int argc,char** argv)try{
    if(argc!=2)throw std::runtime_error("Usage: engine_streamed_traversal_tests output-file");
    const std::filesystem::path output=argv[1];std::filesystem::create_directories(output.parent_path());
    constexpr double span=256.;const Region localOrigin{4000000000000LL,-4000000000000LL};
    const WorldPosition exact{{localOrigin.x+1,localOrigin.z-1},{.125,7.5,255.875}};
    const auto local=exact.relativeTo({localOrigin,{}},span,4096);
    require(local==std::array<float,3>{256.125f,7.5f,-.125f},"Large-address local-frame precision changed");

    TerrainRecipe terrain;terrain.version=3;terrain.amplitudeMm=6000;
    PhysicsWorld physics;const size_t physicsBaseline=physics.size();
    std::map<RegionKey,BodyToken> bodies;std::map<RegionKey,std::pair<std::string,std::vector<std::string>>> identities;
    size_t peakBodies=0,peakResidentBytes=0,peakSlots=0;uint64_t reloadChecks=0;
    RegionStream stream(terrain,{}, {},[&](const RegionContent& content){
        const auto region=content.terrain.region;const RegionKey key{region.x,region.z};
        const auto offset=WorldPosition{region,{}}.relativeTo({localOrigin,{}},span,4096);
        if(bodies.contains(key))physics.replaceMesh(bodies.at(key),content.collision);
        else bodies.emplace(key,physics.mesh(content.terrain.id.text(),offset,content.collision));
        std::vector<std::string> rocks;rocks.reserve(content.terrain.rocks.size());
        for(const auto& rock:content.terrain.rocks)rocks.push_back(rock.id.text());
        const auto observed=std::pair(content.terrain.id.text(),std::move(rocks));
        const auto [it,inserted]=identities.emplace(key,observed);
        if(!inserted){require(it->second==observed,"Unload/reload changed generated identity");++reloadChecks;}
        return RegionReadiness{true,true};
    },[&](Region region){
        const auto key=RegionKey{region.x,region.z};const auto it=bodies.find(key);
        if(it!=bodies.end()){physics.remove(it->second);bodies.erase(it);}
    });

    const std::array<int,21> route{0,1,2,3,4,5,4,3,2,1,0,-1,-2,-3,-4,-5,-4,-2,0,2,0};
    std::vector<double> loadMs;loadMs.reserve(route.size());
    for(const int offset:route){
        const Region center{localOrigin.x+offset,localOrigin.z+(offset%3)-1};
        const StreamAnchor anchor{{center,{128,0,128}},0};loadMs.push_back(ready(stream,anchor));
        peakBodies=std::max(peakBodies,bodies.size());peakResidentBytes=std::max(peakResidentBytes,stream.residentBytes());peakSlots=std::max(peakSlots,stream.slots().size());
        require(stream.slots().size()<=25&&bodies.size()<=25,"Traversal exceeded bounded region residency");
        require(stream.residentBytes()<64*1024*1024,"Traversal exceeded CPU region residency budget");
        require(stream.collisionReady(anchor.position,anchor.position,1),"Center traversal surface was not collision ready");
        const auto centerOffset=WorldPosition{center,{}}.relativeTo({localOrigin,{}},span,4096);
        const auto hit=physics.ray({centerOffset[0]+128,40,centerOffset[2]+128},{0,-80,0});
        require(hit&&hit->stableId==stream.slots().at({center.x,center.z}).content->terrain.id.text(),"Local-frame collider/query identity mismatch");
    }
    require(stream.retired()>0&&reloadChecks>0,"Corpus did not exercise retirement and stable reload");
    const auto retired=stream.retired();
    stream.update({});
    const auto drainDeadline=Clock::now()+std::chrono::seconds(10);
    while(stream.jobStats().outstanding){stream.update({});if(Clock::now()>drainDeadline)throw std::runtime_error("Traversal jobs did not drain");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
    require(stream.slots().empty()&&bodies.empty()&&stream.residentBytes()==0,"Traversal teardown did not return residency to baseline");
    require(physics.size()==physicsBaseline,"Traversal teardown leaked physics bodies");

    const double p50=percentile(loadMs,.50),p95=percentile(loadMs,.95),p99=percentile(loadMs,.99);
    writeDocument(output,"engine.streamed-traversal-corpus",{
        {"passed",true},{"origin",{localOrigin.x,localOrigin.z}},{"route_steps",route.size()},
        {"unique_regions",identities.size()},{"reload_checks",reloadChecks},{"retired_regions",retired},
        {"peak_slots",peakSlots},{"peak_physics_bodies",peakBodies},{"peak_resident_bytes",peakResidentBytes},
        {"load_ms",{{"p50",p50},{"p95",p95},{"p99",p99}}},
        {"proof_limit","Mac CPU/Jolt integration corpus with technical terrain; no GPU, navigation-tile, gameplay-feel, dynamic in-place frame relocation or Windows claim."}
    });
    std::cout<<"PASS: precision-safe large-address traversal, stable reload identity, bounded residency/collision and timing tails; p50="<<p50<<"ms p95="<<p95<<"ms p99="<<p99<<"ms\n";
    return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

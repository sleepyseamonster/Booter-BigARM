#include "World/Terrain.h"
#include "Core/BoundedJobs.h"
#include <chrono>
#include <cmath>
#include <iostream>
#include <set>
using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected invalid request rejection");}
template<class F>void until(F condition){auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(5);while(!condition()){if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Timed out waiting for bounded job");std::this_thread::sleep_for(std::chrono::milliseconds(1));}}
}
int main(int argc,char** argv)try{
    if(argc!=2)throw std::runtime_error("Need output directory");std::filesystem::create_directories(argv[1]);
    TerrainRecipe recipe;RockRecipe rock;PlacementConstraints constraints;
    for(const Region r:std::array<Region,3>{{{0,0},{-2,-1},{int64_t(1)<<54,-(int64_t(1)<<54)}}}){
        const auto a=generateTerrain(recipe,r,rock,constraints),east=generateTerrain(recipe,{r.x+1,r.z},rock,constraints),north=generateTerrain(recipe,{r.x,r.z+1},rock,constraints),reload=generateTerrain(recipe,r,rock,constraints);
        require(a.mesh.indices.size()==6144&&a.collision.size()==6144,"Terrain mesh/collision count");
        for(size_t i=0;i<33;++i){const auto& edge=a.mesh.vertices[i*33+32];const auto& next=east.mesh.vertices[i*33];require(edge.position[1]==next.position[1]&&edge.normal==next.normal,"East border mismatch");const auto& top=a.mesh.vertices[32*33+i];const auto& other=north.mesh.vertices[i];require(top.position[1]==other.position[1]&&top.normal==other.normal,"North border mismatch");}
        require(a.id==reload.id&&a.rocks.size()==reload.rocks.size(),"Regeneration identity changed");
        for(size_t i=0;i<a.rocks.size();++i){const auto& p=a.rocks[i];require(p.id==reload.rocks[i].id&&p.position.local==reload.rocks[i].position.local,"Placement order/position changed");require(p.position.normalized(256).region==r&&placementAllowed(constraints,p.position,p.footprint),"Placement ownership/constraint failure");require(std::abs(p.position.local[1]-terrainSample(recipe,p.position).height)<1e-5,"Rock surface placement mismatch");}
        for(const auto& route:a.routes)for(int step=0;step<=32;++step){WorldPosition p=route.start;p.local[0]+=(route.end.local[0]-route.start.local[0])*step/32;p.local[2]+=(route.end.local[2]-route.start.local[2])*step/32;auto s=terrainSample(recipe,p);for(const auto& profile:constraints.profiles)require(traversable(profile,{route.halfWidth,100, std::acos(s.normal[1])*57.29578f,0}),"Shared macro route fails traversal class");}
        // Independent interpolation over both triangle halves agrees with surface query.
        for(const auto uv:std::array<std::array<float,2>,2>{{{.2f,.3f},{.8f,.7f}}}){const int x=13,z=17;const float h00=a.mesh.vertices[z*33+x].position[1],h10=a.mesh.vertices[z*33+x+1].position[1],h01=a.mesh.vertices[(z+1)*33+x].position[1],h11=a.mesh.vertices[(z+1)*33+x+1].position[1];const float u=uv[0],v=uv[1];const float expected=u+v<1?h00*(1-u-v)+h10*u+h01*v:h11*(u+v-1)+h10*(1-v)+h01*(1-u);require(std::abs(expected-terrainSample(recipe,{r,{(x+double(u))*8,0,(z+double(v))*8}}).height)<1e-5,"Triangle/query height mismatch");}
        for(size_t i=0;i<a.collision.size();++i)require(a.collision[i]==a.mesh.vertices[a.mesh.indices[i]].position,"Collision differs from render triangles");
    }
    auto base=generateTerrain(recipe,{},rock,constraints);require(!base.rocks.empty(),"Empty placement oracle");
    const auto removed=base.rocks.front();constraints.exclusions.push_back({"calibration-anchor",removed.position,3});auto authored=generateTerrain(recipe,{},rock,constraints);
    require(authored.landmarks.size()==1,"Authored landmark anchor missing");for(const auto& p:authored.rocks)require(p.id!=removed.id&&placementAllowed(constraints,p.position,p.footprint),"Excluded rock retained");
    std::set<std::string> prior;for(const auto& p:base.rocks)prior.insert(p.id.text());for(const auto& p:authored.rocks)require(prior.contains(p.id.text()),"Constraint edit renumbered unaffected placements");
    auto changed=recipe;++changed.seed;const auto variant=generateTerrain(changed,{},rock,{});require(variant.mesh.vertices[16*33+16].position!=base.mesh.vertices[16*33+16].position,"Seed did not alter terrain");
    saveTerrainRecipe(std::filesystem::path(argv[1])/"recipe.json",recipe);require(loadTerrainRecipe(std::filesystem::path(argv[1])/"recipe.json")==recipe,"Terrain recipe roundtrip");
    rejects([&]{generateTerrain(recipe,{},rock,{},[]{return true;});});rejects([&]{generateTerrain(recipe,{INT64_MAX,0},rock,{});});auto invalid=recipe;invalid.version=2;rejects([&]{generateTerrain(invalid,{},rock,{});});
    // The real terrain consumer uses the same bounded CPU queue as the cooker.
    BoundedJobs<TerrainPatch> terrain([](const auto& p){return p.bytes();});require(bool(terrain.submit("region:0:0",1,2*1024*1024,[&](const auto& token){return generateTerrain(recipe,{},rock,{},[&]{return token.cancelled();});})),"Terrain admission failed");
    UploadAdmission upload;std::optional<BoundedJobs<TerrainPatch>::Completion> patch;until([&]{patch=terrain.takeReady(upload);return bool(patch);});require(patch->value&&patch->value->id==base.id&&patch->error.empty(),"Queued terrain result failed");require(terrain.stats().reservedBytes==0,"Taken terrain reservation leaked");
    BoundedJobs<std::vector<int>> jobs([](const auto& v){return v.size()*sizeof(int);},2,3,128);
    std::atomic<bool> started=false,release=false;
    require(bool(jobs.submit("slow",1,32,[&](const auto&){started=true;while(!release.load())std::this_thread::yield();return std::vector<int>{1};})),"Slow admission");
    until([&]{return started.load();});
    require(bool(jobs.submit("fast",1,32,[](const auto&){return std::vector<int>{2};})),"Fast admission");
    std::optional<BoundedJobs<std::vector<int>>::Completion> completion;
    until([&]{upload.beginFrame();completion=jobs.takeReady(upload);return bool(completion);});
    require(completion->owner=="fast"&&completion->value->at(0)==2,"Out of order delivery blocked");
    require(!jobs.submit("too-large",1,129,[](const auto&){return std::vector<int>{};}),"Oversized job admitted");
    require(bool(jobs.submit("slow",2,32,[](const auto&){return std::vector<int>{3};})),"Replacement admission");
    until([&]{upload.beginFrame();completion=jobs.takeReady(upload);return bool(completion);});
    require(completion->epoch==2&&completion->value->at(0)==3,"Stale result published");release=true;
    until([&]{return jobs.stats().outstanding==0;});require(!jobs.takeReady(upload),"Cancelled old epoch escaped");
    require(bool(jobs.submit("failure",1,4,[](const auto&){return std::vector<int>(10);})),"Failure case admission");
    until([&]{upload.beginFrame();completion=jobs.takeReady(upload);return bool(completion);});require(!completion->value&&!completion->error.empty(),"Exceeded result reservation accepted");
    std::atomic<bool> active=false;
    require(bool(jobs.submit("shutdown",1,16,[&](const auto& token){active=true;while(!token.cancelled())std::this_thread::yield();return std::vector<int>{};})),"Shutdown admission");until([&]{return active.load();});jobs.shutdown();require(jobs.stats().outstanding==0&&jobs.stats().reservedBytes==0,"Shutdown failed to drain");
    UploadAdmission budget{8,32};require(budget.admit(6)&&!budget.admit(4),"Frame upload limit");budget.beginFrame();require(budget.admit(12)&&budget.oversizedItem&&!budget.admit(1),"Oversized single-item rule");budget.beginFrame();require(!budget.admit(33),"Hard upload limit");
    std::cout<<"PASS: terrain borders/large signed coordinates, triangle surface/collision agreement, deterministic constrained placement and two-class macro routes; bounded async generation, out-of-order/epoch replacement, failure and shutdown; terrain bytes="<<base.bytes()<<", rocks="<<base.rocks.size()<<'\n';return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

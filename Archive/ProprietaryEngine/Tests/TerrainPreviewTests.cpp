#include "World/Streaming/RegionStream.h"
#include "Game/WorldSession.h"
#include <chrono>
#include <cmath>
#include <iostream>
using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
void ready(RegionStream& stream){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(30);
    for(;;){stream.update({{{{},{32,0,32}},0}});bool done=stream.slots().size()==9;
        for(const auto& [_,slot]:stream.slots()){if(!slot.error.empty())throw std::runtime_error(slot.error);done=done&&slot.ready.collision;}
        if(done&&stream.jobStats().outstanding==0)return;if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Terrain preview loading timed out");
        std::this_thread::sleep_for(std::chrono::milliseconds(1));}
}
}
int main(int argc,char** argv)try{
    if(argc!=4)throw std::runtime_error("Need terrain recipe, rock recipe and new output directory");
    const auto terrain=loadTerrainRecipe(argv[1]);const auto rock=loadRockRecipe(argv[2]);const std::filesystem::path output=argv[3];
    if(std::filesystem::exists(output))throw std::runtime_error("Output exists");std::filesystem::create_directories(output);
    require(terrain.version==2&&rock.version==3,"Preview versions");
    size_t triangles[3]{};
    for(const Region region:std::array<Region,3>{{{0,0},{-2,-1},{int64_t(1)<<54,-(int64_t(1)<<54)}}}){
        const auto a=generateTerrain(terrain,region,rock,{}),east=generateTerrain(terrain,{region.x+1,region.z},rock,{}),north=generateTerrain(terrain,{region.x,region.z+1},rock,{});
        require(a.mesh.vertices.size()==65*65&&a.mesh.indices.size()==64*64*6,"V2 resolution");
        bool relief=false;for(size_t i=0;i<65;++i){const auto& v=a.mesh.vertices[i*65+64];const auto& other=east.mesh.vertices[i*65];require(v.position[1]==other.position[1]&&v.normal==other.normal,"East seam");relief=relief||std::abs(v.position[1])>.1f;
            require(a.mesh.vertices[64*65+i].position[1]==north.mesh.vertices[i].position[1]&&a.mesh.vertices[64*65+i].normal==north.mesh.vertices[i].normal,"North seam");}
        require(relief,"Artificial flat region border remains");
        for(size_t i=0;i<a.collision.size();++i)require(a.collision[i]==a.mesh.vertices[a.mesh.indices[i]].position,"Collision/render disagreement");
        for(const auto uv:std::array<std::array<float,2>,2>{{{.2f,.3f},{.8f,.7f}}}){
            const uint32_t x=23,z=19;const auto u=uv[0],v=uv[1];const float a0=a.mesh.vertices[z*65+x].position[1],b=a.mesh.vertices[z*65+x+1].position[1],c=a.mesh.vertices[(z+1)*65+x].position[1],d=a.mesh.vertices[(z+1)*65+x+1].position[1];
            const float expected=u+v<=1?a0*(1-u-v)+b*u+c*v:d*(u+v-1)+b*(1-v)+c*(1-u);
            require(std::abs(expected-terrainSample(terrain,{region,{(x+double(u))*4,0,(z+double(v))*4}}).height)<1e-5,"Triangle query disagreement");}
        for(uint32_t level=0;level<3;++level){auto lod=terrainRenderLod(a,level);triangles[level]=lod.indices.size()/3;
            const uint32_t stride=1u<<level,n=64/stride;for(uint32_t z=0;z<=n;++z)for(uint32_t x=0;x<=n;++x)require(lod.vertices[z*(n+1)+x].position==a.mesh.vertices[z*stride*65+x*stride].position,"LOD resampled another surface");}
        require(triangles[0]>triangles[1]&&triangles[1]>triangles[2],"LOD triangle reduction");
    }
    const auto baseline=generateTerrain(terrain,{},rock,{});require(!baseline.rocks.empty(),"No preview rocks");
    PlacementConstraints constraints;const auto first=baseline.rocks.front();constraints.routes.push_back({"authored-route",{{},{0,0,first.position.local[2]}},{{},{256,0,first.position.local[2]}},5,3});
    const auto constrained=generateTerrain(terrain,{},rock,constraints);for(const auto& p:constrained.rocks)require(p.id!=first.id&&placementAllowed(constraints,p.position,p.footprint),"Authored route blocked");
    WorldConfiguration configuration{terrain,rock,{}};WorldSession session(output/"profile",configuration);CalibrationRuntime runtime({},session.initial().player);
    std::map<RegionKey,BodyToken> bodies;
    RegionStream stream(terrain,rock,{},[&](const RegionContent& c){const auto r=c.terrain.region;const RegionKey key{r.x,r.z};
        if(bodies.contains(key))runtime.physics().replaceMesh(bodies.at(key),c.collision);
        else bodies.emplace(key,runtime.physics().mesh(c.terrain.id.text(),WorldPosition{r,{}}.relativeTo({},256,4096),c.collision));
        return RegionReadiness{true,true};},[&](Region r){auto it=bodies.find({r.x,r.z});if(it!=bodies.end()){runtime.physics().remove(it->second);bodies.erase(it);}});
    ready(stream);require(stream.residentBytes()<64*1024*1024,"Resident budget");
    auto content=stream.slots().at({0,0}).content;
    for(const auto& p:content->terrain.rocks){const float angle=p.yaw*.01745329252f,co=std::cos(angle),si=std::sin(angle);double closest=1e30;
        for(const auto& vertex:stream.rocks()[p.id.member%4].lods[0].mesh.vertices){const auto& v=vertex.position;auto point=p.position;point.local[0]+=co*v[0]+si*v[2];point.local[2]+=-si*v[0]+co*v[2];const double clearance=p.position.local[1]+v[1]-terrainSample(terrain,point).height;require(clearance>=-.1201,"Rock buried more than seating tolerance");closest=std::min(closest,clearance);}
        require(std::abs(closest+.12)<1e-4,"Rock floating above surface");}
    const auto sample=terrainSample(terrain,{{},{128,0,128}});const auto hit=runtime.physics().ray({128,sample.height+30,128},{0,-60,0});require(hit.has_value(),"Real terrain collider missing");
    const auto removed=content->terrain.rocks.front().id;require(stream.removeRock(removed),"Removal rejected");ready(stream);
    session.save(runtime,.3f,.25f,7,stream.deltas());WorldSession restored(output/"profile",configuration);require(restored.restored()&&restored.initial().deltas.removed({},removed.member),"V2 world save lost delta");
    stream.update({});require(bodies.empty(),"Terrain retirement left collision");ready(stream);
    for(const auto& p:stream.slots().at({0,0}).content->terrain.rocks)require(p.id!=removed,"Removed rock returned");
    writeDocument(output/"result.json","engine.terrain-preview-tests",{{"passed",true},{"terrain_lod_triangles",triangles},{"loaded_regions",bodies.size()},{"resident_bytes",stream.residentBytes()},{"saved_removed_rock",removed.text()},{"native_windows",false}});
    std::cout<<"PASS: v2 seams, large signed addresses, matching triangle queries, LOD reduction, authored clearance, v3 seating, Jolt attachment, removal/save/reload and retirement\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

#include "World/Placement.h"
#include "World/Rocks/RockGenerator.h"
#include <algorithm>
#include <cmath>
#include <cstring>
#include <iostream>
#include <map>
using namespace engine;
void require(bool c,const char* m){if(!c)throw std::runtime_error(m);}
template<class F>void rejects(F&& f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected rejected generator/constraint input");}
using V=std::array<float,3>;
bool same(const RockResult& a,const RockResult& b){return a.mesh.vertices.size()==b.mesh.vertices.size()&&a.mesh.indices==b.mesh.indices&&std::memcmp(a.mesh.vertices.data(),b.mesh.vertices.data(),a.mesh.vertices.size()*sizeof(SkinVertex))==0&&a.surfaces==b.surfaces;}
void topology(const RockResult& r){
    struct Edge{int count=0,balance=0;};std::map<std::pair<V,V>,Edge> edges;
    const auto& m=r.mesh;
    for(size_t i=0;i<m.indices.size();i+=3){
        const auto& a=m.vertices[m.indices[i]];const auto& b=m.vertices[m.indices[i+1]];const auto& c=m.vertices[m.indices[i+2]];
        V u{},v{};for(size_t j=0;j<3;++j){u[j]=b.position[j]-a.position[j];v[j]=c.position[j]-a.position[j];}
        const V cross{u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]};
        float area=0,alignment=0;for(size_t j=0;j<3;++j){area+=cross[j]*cross[j];alignment+=cross[j]*a.normal[j];}
        require(area>1e-12f&&alignment>0,"Nondegenerate triangles agree with outward normals");
        const V points[]={a.position,b.position,c.position};
        for(size_t j=0;j<3;++j){auto x=points[j],y=points[(j+1)%3];const bool forward=x<y;if(!forward)std::swap(x,y);auto& edge=edges[{x,y}];++edge.count;edge.balance+=forward?1:-1;}
    }
    for(const auto& [_,edge]:edges)require(edge.count==2&&edge.balance==0,"Closed mesh with matching opposite edge directions");
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Expected recipe and test output root");const std::filesystem::path root=argv[2];std::filesystem::create_directories(root);
    const auto recipe=loadRockRecipe(argv[1]);const GeneratedId id{77,1,{-1,2},9,"rock"};
    const auto a=generateRock(recipe,id);topology(a);require(a.mesh.indices.size()==384&&a.surfaces.size()==128,"Expected first-pass triangle/semantic budget");
    require(a.id=="generated:v1:rock:77:1:-1:2:9"&&same(a,generateRock(recipe,id)),"Stable structural identity and repeatable geometry");
    auto otherId=id;++otherId.member;generateRock(recipe,otherId);require(same(a,generateRock(recipe,id)),"Generation order does not affect output");
    auto edit=recipe;++edit.seed;const auto changed=generateRock(edit,id);require(!same(a,changed)&&changed.id==a.id,"Recipe seed edits change shape without changing object identity");
    auto coarse=recipe;coarse.subdivisions=1;const auto lod=generateRock(coarse,id);topology(lod);require(lod.id==a.id&&lod.mesh.indices.size()<a.mesh.indices.size(),"Resolution retains placement identity");
    require(std::abs(a.minimum[1])<.00001f&&std::abs(a.maximum[1]-2)<.00001f,"Common metre scale and ground anchor");
    saveRockRecipe(root/"recipe.json",recipe);require(loadRockRecipe(root/"recipe.json")==recipe,"Integer recipe roundtrip");
    const auto asset=root/"model";if(std::filesystem::exists(asset))std::filesystem::remove_all(asset);saveModel(asset,a.mesh);const auto loaded=loadModel(asset/"model.json");require(loaded.indices==a.mesh.indices&&loaded.vertices.size()==a.mesh.vertices.size(),"Generated rock uses existing asset path");
    auto bad=recipe;bad.subdivisions=99;rejects([&]{generateRock(bad,id);});bad=recipe;bad.version=0;rejects([&]{generateRock(bad,id);});
    PlacementConstraints c;c.routes.push_back({"authored:route",{{-1,0},{250,0,0}},{{0,0},{6,0,0}},1,1});
    const WorldPosition near{{0,0},{0,0,2}};
    require(placementAllowed(c,near,.5f),"Player-only route reserves player clearance");c.routes[0].agents=3;require(!placementAllowed(c,near,.5f),"Shared route reserves larger companion clearance");
    require(!placementAllowed(c,{{0,0},{0,0,0}},a.footprintRadius)&&placementAllowed(c,{{0,0},{0,0,8}},a.footprintRadius),"Rock footprint cannot occupy reserved route");
    c.exclusions.push_back({"authored:landmark",{{0,0},{0,0,8}},1});c.exclusions.push_back({"authored:distant",{{INT64_MAX-1,0},{0,0,0}},1});
    require(!placementAllowed(c,{{0,0},{0,0,8}},.1f),"Authored exclusion preserved");std::reverse(c.exclusions.begin(),c.exclusions.end());require(!placementAllowed(c,near,.5f),"Constraint order independent");
    saveConstraints(root/"constraints.json",c);const auto restored=loadConstraints(root/"constraints.json");require(!placementAllowed(restored,near,.5f)&&restored.routes[0].start.region.x==-1,"Negative-region route document roundtrip");
    const TraversalSample sample{.7f,2,30,.3f};require(traversable(c.profiles[0],sample)&&!traversable(c.profiles[1],sample),"Traversal profile applies clearance, height, slope and step");
    c.routes[0].agents=0;rejects([&]{validateConstraints(c);});
    std::cout<<"PASS: stable deterministic rocks, closed topology/semantics, cooked assets, two-agent clearance, reserved routes and constraint roundtrip\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

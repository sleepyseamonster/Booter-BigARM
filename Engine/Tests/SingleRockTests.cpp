#include "World/Rocks/SingleRock.h"
#include "World/Rocks/RockAsset.h"
#include "Persistence/Document.h"
#include "Runtime/EditHistory.h"
#include "Physics/PhysicsWorld.h"
#include <iostream>
#include <cmath>
#include <map>
using namespace engine;
void require(bool b,const char* m){if(!b)throw std::runtime_error(m);}
size_t verifyCollision(PhysicsWorld& world,const ModelData& mesh) {
    std::vector<PhysicsVector> triangles;
    PhysicsVector lo=mesh.vertices.front().position,hi=lo;
    for(const auto& v:mesh.vertices)for(size_t a=0;a<3;++a){lo[a]=std::min(lo[a],v.position[a]);hi[a]=std::max(hi[a],v.position[a]);}
    for(auto index:mesh.indices)triangles.push_back(mesh.vertices[index].position);
    const auto body=world.mesh("authored:single-rock-test",{},triangles);
    size_t hits=0;
    for(int ix=1;ix<8;++ix)for(int iz=1;iz<8;++iz){
        const float x=lo[0]+(hi[0]-lo[0])*ix/8,z=lo[2]+(hi[2]-lo[2])*iz/8;
        double highest=-1e10;
        for(size_t i=0;i<triangles.size();i+=3){
            const auto& a=triangles[i];const auto& b=triangles[i+1];const auto& c=triangles[i+2];
            const double dx1=double(b[0])-a[0],dz1=double(b[2])-a[2],dx2=double(c[0])-a[0],dz2=double(c[2])-a[2];
            const double determinant=dx1*dz2-dx2*dz1;if(std::abs(determinant)<1e-18)continue;
            const double u=((double(x)-a[0])*dz2-(double(z)-a[2])*dx2)/determinant;
            const double v=(dx1*(double(z)-a[2])-dz1*(double(x)-a[0]))/determinant;
            if(u>=0 && v>=0 && u+v<=1)highest=std::max(highest,a[1]+u*(double(b[1])-a[1])+v*(double(c[1])-a[1]));
        }
        const auto hit=world.ray({x,hi[1]+1,z},{0,lo[1]-hi[1]-2,0});
        require(bool(hit)==(highest>-1e9),"Cooked collision silhouette differs from mesh");
        if(hit){require(hit->body==body && std::abs(hit->position[1]-highest)<.0002,"Cooked collision surface differs from mesh");++hits;}
    }
    require(hits>0,"Collision probes missed entire rock");require(world.remove(body),"Collision body unload failed");return hits;
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Need Golden Rock library and new output directory");const std::filesystem::path lib=argv[1],out=argv[2];if(std::filesystem::exists(out))throw std::runtime_error("Output exists");std::filesystem::create_directories(out);
    auto base=loadRockRecipe(lib/"01-Golden-Rock.json");base.version=7;base.profile=7;base.volumes.clear();base.calibrationSeed=0;
    std::vector<std::pair<std::string,RockRecipe>> cases;
    for(const auto& file:{"01-Golden-Rock.json","02-Approved-Variation-A.json","03-Approved-Variation-B.json"}){auto r=loadRockRecipe(lib/file);r.version=7;r.single={};cases.emplace_back(std::string("captured-")+file,r);}
    cases.emplace_back("golden.json",base);
    for(uint32_t p=0;p<=8;++p){auto r=base;r.seed=1729;r.profile=p;r.asymmetry=620;r.compaction=300;r.fractures=600;cases.emplace_back("profile-"+std::to_string(p)+".json",r);}
    for(uint32_t seed:{0u,1u,362337u,3948960537u,2147483648u,4294967295u}){auto r=base;r.seed=seed;r.single.randomDimensions=true;cases.emplace_back("seed-"+std::to_string(seed)+".json",r);}
    for(auto dimensions:{std::array<float,2>{.12f,1.2f},std::array<float,2>{1.2f,.1f}}){auto r=base;r.single.width=dimensions[0];r.single.bodyLength=dimensions[1];r.profile=5;cases.emplace_back("aspect-"+std::to_string(cases.size())+".json",r);}
    PhysicsWorld physics;size_t collisionHits=0;
    for(auto& [name,r]:cases){const auto plan=planRockVolumes(r,{1,7,{},0,"rock"});const auto generated=generateRock(r,{1,7,{},0,"rock"});
        collisionHits+=verifyCollision(physics,generated.mesh);
        auto captured=r;captured.volumes=plan;const auto copy=generateRock(captured,{1,7,{},0,"rock"});require(generated.mesh.indices==copy.mesh.indices,"Capturing sources changes topology");require(generated.mesh.vertices.size()==copy.mesh.vertices.size(),"Capturing sources changes mesh");
        Json poses=Json::array(),vertices=Json::array();for(const auto& v:plan)poses.push_back({{"center",v.center},{"scale",std::array<float,3>{v.halfSize[0]*2,v.halfSize[1]*2,v.halfSize[2]*2}},{"orientation",v.orientation},{"primitive",v.primitive},{"seed",v.shapeSeed}});
        std::map<std::pair<uint32_t,uint32_t>,std::pair<int,int>> edges;
        for(size_t i=0;i<generated.mesh.indices.size();i+=3)for(int a=0;a<3;++a){auto x=generated.mesh.indices[i+a],y=generated.mesh.indices[i+(a+1)%3];auto& e=edges[std::minmax(x,y)];++e.first;e.second+=x<y?1:-1;}
        for(auto& [_,e]:edges)require(e.first==2&&e.second==0,"Single rock not closed and consistently wound");
        for(size_t i=0;i<generated.mesh.vertices.size();++i){auto& v=generated.mesh.vertices[i];require(v.position==copy.mesh.vertices[i].position,"Captured field changed geometry");vertices.push_back(v.position);}
        saveRockRecipe(out/name,r);require(loadRockRecipe(out/name)==r,"V7 recipe roundtrip failed");writeDocument(out/(name+".native.json"),"engine.single-rock-native",{{"poses",poses},{"vertices",vertices},{"triangles",generated.mesh.indices}});
    }
    float maximumPoseError=0;
    const char* files[]={"01-Golden-Rock.json","02-Approved-Variation-A.json","03-Approved-Variation-B.json"};
    const float dimensions[][2]={{1,.75f},{.66825736f,.60848314f},{.64600056f,.55279005f}};
    for(size_t variant=0;variant<3;++variant){auto reference=loadRockRecipe(lib/files[variant]);auto r=base;r.seed=reference.seed;r.single.width=dimensions[variant][0];r.single.bodyLength=dimensions[variant][1];auto plan=planSingleRock(r);require(plan.size()==reference.volumes.size(),"Saved source count mismatch");
    for(size_t i=0;i<plan.size();++i){require(plan[i].shapeSeed==reference.volumes[i].shapeSeed,"Saved shape seed mismatch");for(size_t a=0;a<3;++a){maximumPoseError=std::max(maximumPoseError,std::abs(plan[i].center[a]-reference.volumes[i].center[a]));maximumPoseError=std::max(maximumPoseError,std::abs(plan[i].halfSize[a]-reference.volumes[i].halfSize[a]));}float direct=0,opposite=0;for(size_t a=0;a<4;++a){direct=std::max(direct,std::abs(plan[i].orientation[a]-reference.volumes[i].orientation[a]));opposite=std::max(opposite,std::abs(plan[i].orientation[a]+reference.volumes[i].orientation[a]));}maximumPoseError=std::max(maximumPoseError,std::min(direct,opposite));}}
    require(maximumPoseError<.00002f,"Fresh plans differ from saved Unity family");
    const auto plan=planSingleRock(base);
    const auto asset=buildRockAsset(base,{1,7,{},0,"rock"});require(asset.lods.size()==3,"Missing LODs");for(size_t i=0;i<asset.collision.size();++i)require(asset.collision[i]==asset.lods[0].mesh.vertices[asset.lods[0].mesh.indices[i]].position,"Collision mismatch");
    auto changed=base;++changed.seed;require(planSingleRock(changed)!=plan,"Seed does not replan masses");EditHistory<RockRecipe> history(base);history.apply(changed,[](const auto& r){generateRock(r,{1,7,{},0,"rock"});});history.undo([](const auto&){});require(history.value()==base,"Undo lost recipe");
    saveRockResult(out/"export",asset.lods[0],base);require(loadRockRecipe(out/"export/recipe.json")==base,"Export lost plan");
    writeDocument(out/"result.json","engine.single-rock-tests",{{"passed",true},{"cases",cases.size()},{"saved_unity_pose_max_error",maximumPoseError},{"collision",true},{"cooked_collision_hits",collisionHits},{"recipe_roundtrip",true}});
    std::cout<<"PASS: "<<cases.size()<<" cases; saved Unity pose maximum error "<<maximumPoseError<<"; closed meshes, editable sources, fresh seeds, save/export/undo and collision\n";
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

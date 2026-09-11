#include "World/Rocks/RockAsset.h"
#include "Physics/PhysicsWorld.h"
#include "Persistence/Document.h"
#include "Runtime/EditHistory.h"
#include <cmath>
#include <cstring>
#include <iostream>
#include <map>
#include <set>
using namespace engine;
void require(bool c,const char* message){if(!c)throw std::runtime_error(message);}
template<class F>void rejects(F&& f){bool failed=false;try{f();}catch(const std::exception&){failed=true;}require(failed,"Expected rejection");}
bool same(const RockResult& a,const RockResult& b){return a.mesh.indices==b.mesh.indices&&a.mesh.vertices.size()==b.mesh.vertices.size()&&std::memcmp(a.mesh.vertices.data(),b.mesh.vertices.data(),a.mesh.vertices.size()*sizeof(SkinVertex))==0;}
void inspect(const RockResult& rock){
    validateModel(rock.mesh);std::map<std::pair<uint32_t,uint32_t>,std::pair<int,int>> edges;double volume=0;
    for(size_t i=0;i<rock.mesh.indices.size();i+=3){
        const auto a=rock.mesh.indices[i],b=rock.mesh.indices[i+1],c=rock.mesh.indices[i+2];const auto& p=rock.mesh.vertices[a].position;const auto& q=rock.mesh.vertices[b].position;const auto& r=rock.mesh.vertices[c].position;
        volume+=(double(p[0])*(q[1]*r[2]-q[2]*r[1])+double(p[1])*(q[2]*r[0]-q[0]*r[2])+double(p[2])*(q[0]*r[1]-q[1]*r[0]))/6;
        for(auto pair:{std::pair{a,b},std::pair{b,c},std::pair{c,a}}){const bool forward=pair.first<pair.second;if(!forward)std::swap(pair.first,pair.second);auto& e=edges[pair];++e.first;e.second+=forward?1:-1;}
    }
    require(volume>0,"Fused rock winding encloses positive volume");
    for(const auto& [_,edge]:edges)require(edge.first==2&&edge.second==0,"Fused mesh must remain closed and consistently oriented");
    for(const auto& v:rock.mesh.vertices){float n=0;for(auto x:v.normal)n+=x*x;require(std::abs(n-1)<.001f,"Normalized finite rock normals");}
    require(rock.surfaces.size()*3==rock.mesh.indices.size(),"All triangles retain surface classification");
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Expected preset and output directory");const auto recipe=loadRockRecipe(argv[1]);const std::filesystem::path root=argv[2];std::filesystem::create_directories(root);
    const GeneratedId id{71,3,{-2,4},11,"rock"};const auto a=generateRock(recipe,id);inspect(a);require(same(a,generateRock(recipe,id)),"Regeneration must match exactly");
    auto changed=recipe;changed.seed++;require(!same(a,generateRock(changed,id)),"Seed changes the rock");
    changed=recipe;changed.fractures=0;require(!same(a,generateRock(changed,id)),"Subtractive cuts change topology");
    changed=recipe;changed.volumes=planRockVolumes(recipe,id);const auto explicitRock=generateRock(changed,id);require(same(a,explicitRock),"Materializing source volumes preserves the shape");
    changed.volumes[0].halfSize[0]*=.7f;require(!same(a,generateRock(changed,id)),"Source volume edit affects mesh");
    saveRockRecipe(root/"fused.json",changed);require(loadRockRecipe(root/"fused.json")==changed,"V3 volumes/material/formation persist");
    EditHistory<RockRecipe> history(recipe);history.apply(changed,[](const auto&){});
    rejects([&]{history.apply(recipe,[](const auto&){throw std::runtime_error("Preparation failed");});});require(history.value()==changed,"Failed volume edit preserves accepted history");
    history.undo([](const auto&){});require(history.value()==recipe,"Undo variable-size source volumes");history.redo([](const auto&){});require(history.value()==changed,"Redo source-volume document");
    auto material=recipe;material.material.dust=900;require(same(a,generateRock(material,id)),"Material-only edits cannot change geometry or identity");
    auto bad=recipe;bad.volumes={{1,{}, {.5f,.5f,.5f},0,true}};rejects([&]{generateRock(bad,id);});bad=recipe;bad.material.cracks=1001;rejects([&]{validateRecipe(bad);});
    const auto asset=buildRockAsset(recipe,id);require(asset.lods.size()==3,"V3 has three LODs");for(const auto& lod:asset.lods){inspect(lod);require(lod.id==id.text()&&std::abs(lod.minimum[1])<.0001f,"LOD identity and ground anchoring");}
    require(asset.lods[0].mesh.indices.size()>asset.lods[2].mesh.indices.size(),"Lower LOD reduces geometry");
    PhysicsWorld physics;const auto body=physics.mesh(id.text(),{},asset.collision);bool hit=false;for(float x:{-.4f,0.f,.4f})if(const auto h=physics.ray({x,10,0},{0,-20,0})){require(h->body==body&&h->stableId==id.text(),"Collision retains identity");hit=true;}require(hit,"Fused rock collision exists");physics.remove(body);
    auto formation=recipe;formation.formation=2;formation.members=3;formation.subdivisions=1;const auto ground=[](float x,float z){return x*.12f+z*.07f;};const auto members=planRockFormation(formation,id,ground);
    require(members.size()==3&&members[0].id.text()!=id.text()&&members[0].id!=members[1].id,"Formation members have distinct stable child identities");
    auto sibling=id;++sibling.member;require(planRockFormation(formation,sibling,ground)[0].id!=members[0].id,"Neighboring formations cannot share member IDs");
    const auto f=generateRockFormation(formation,id,ground);inspect(f);require(same(f,generateRockFormation(formation,id,ground)),"Formation terrain reload is deterministic");
    for(const auto& v:f.mesh.vertices)require(v.position[1]>=ground(v.position[0],v.position[2])-.002f,"Seated formation must not penetrate its support plane");
    saveRockResult(root/"export",a,recipe);const auto exported=loadModel(root/"export/model.json");require(exported.indices==a.mesh.indices&&exported.vertices.size()==a.mesh.vertices.size(),"Export matches accepted topology");
    require(readDocument(root/"export/material.json","engine.rock-material").at("family")==rockMaterialFamily,"Export retains material identity");
    std::cout<<"PASS: fused topology, deterministic source plans and cuts, old-independent recipe version, saved material/volumes, LOD/collision/export, stable formation children and terrain seating; triangles="<<a.mesh.indices.size()/3<<'\n';return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

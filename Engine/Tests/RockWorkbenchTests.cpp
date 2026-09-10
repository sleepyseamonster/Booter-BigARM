#include "World/Rocks/RockAsset.h"
#include "Physics/PhysicsWorld.h"
#include "Runtime/EditHistory.h"
#include "Runtime/InspectionDocument.h"
#include "Persistence/Document.h"
#include <algorithm>
#include <cmath>
#include <fstream>
#include <iostream>
using namespace engine;
void require(bool c,const char* m){if(!c)throw std::runtime_error(m);}
template<class F>void rejects(F&& f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Expected invalid preparation rejection");}
float top(const std::vector<PhysicsVector>& triangles,float x,float z) {
    float height=-INFINITY;
    for(size_t i=0;i<triangles.size();i+=3){const auto a=triangles[i],b=triangles[i+1],c=triangles[i+2];const float denominator=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2]);if(std::abs(denominator)<1e-7f)continue;
        const float u=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/denominator,v=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/denominator;
        if(u>=0&&v>=0&&u+v<=1)height=std::max(height,u*a[1]+v*b[1]+(1-u-v)*c[1]);}
    return height;
}
int main(int argc,char** argv)try{
    if(argc!=2)throw std::runtime_error("Expected output directory");const std::filesystem::path root=argv[1];std::filesystem::create_directories(root);
    const RockRecipe recipe;const GeneratedId id{1,1,{},0,"rock"};auto asset=buildRockAsset(recipe,id);
    require(asset.lods.size()==3&&asset.lods[0].mesh.indices.size()==384&&asset.lods[1].mesh.indices.size()==96&&asset.lods[2].mesh.indices.size()==24,"128/32/8 triangle LOD chain");
    for(const auto& lod:asset.lods)require(lod.id==id.text()&&lod.minimum[1]==0&&std::abs(lod.maximum[1]-2)<.00001f,"LOD identity and ground anchor agreement");
    require(rockLod(asset,1)==0&&rockLod(asset,13)==1&&rockLod(asset,25)==2,"Bounded distance LOD selection");
    PhysicsWorld physics;const auto body=physics.mesh(id.text(),{-3.5f,0,0},asset.collision);
    auto query=[&]{const auto hit=physics.ray({-3.27f,10,.17f},{0,-20,0});require(hit&&hit->body==body&&hit->stableId==id.text(),"Collision retains generated identity");return hit->position[1];};
    require(std::abs(query()-top(asset.collision,.23f,.17f))<.0001f,"Jolt matches rendered highest-detail triangles");
    EditHistory<RockRecipe> history(recipe);auto prepare=[&](const RockRecipe& r){auto next=buildRockAsset(r,id);physics.replaceMesh(body,next.collision);asset=std::move(next);};
    auto changed=recipe;changed.radiiMm[1]=1600;history.apply(changed,prepare);const auto editedHeight=query();require(editedHeight>2&&std::abs(editedHeight-top(asset.collision,.23f,.17f))<.0001f,"Edit updates matching collision");
    history.undo(prepare);require(history.value()==recipe&&query()<2,"Undo rebuilds shared geometry/collision");history.redo(prepare);require(history.value()==changed&&query()==editedHeight,"Redo restores document and collision");
    auto invalid=changed;invalid.subdivisions=99;rejects([&]{history.apply(invalid,prepare);});require(history.value()==changed&&query()==editedHeight,"Failed edit preserves document and collision");
    rejects([&]{physics.replaceMesh(body,{});});require(query()==editedHeight&&physics.size()==1,"Failed mesh replacement preserves body");
    saveRockRecipe(root/"rock.json",history.value());require(loadRockRecipe(root/"rock.json")==changed,"Authored recipe save/reload");
    auto exported=root/"exported-rock";for(unsigned index=1;std::filesystem::exists(exported);++index)exported=root/("exported-rock-"+std::to_string(index));saveRockResult(exported,asset.lods.front(),history.value());const auto exportedMesh=loadModel(exported/"model.json");
    require(exportedMesh.indices==asset.lods.front().mesh.indices&&exportedMesh.vertices.size()==asset.lods.front().mesh.vertices.size(),"Export topology differs from accepted preview");
    for(size_t i=0;i<exportedMesh.vertices.size();++i)require(exportedMesh.vertices[i].position==asset.lods.front().mesh.vertices[i].position&&exportedMesh.vertices[i].normal==asset.lods.front().mesh.vertices[i].normal,"Export geometry differs from accepted preview");
    require(loadRockRecipe(exported/"recipe.json")==history.value()&&readDocument(exported/"rock.json","engine.rock-result").at("id")==asset.lods.front().id,"Export recipe or identity mismatch");
    rejects([&]{saveRockResult(exported,asset.lods.front(),history.value());});
    FixtureState original;EditHistory<FixtureState> settings(original);auto next=original;next.exposure=1;next.yaw=1.2f;const auto accept=[](const FixtureState&){};
    settings.apply(next,accept);saveInspection(root/"inspection.json",settings.value());FixtureState loaded;loadInspection(root/"inspection.json",loaded);require(loaded==next,"Saved inspection reproduces all fields");
    settings.undo(accept);require(settings.value()==original,"Inspection command undo");settings.redo(accept);require(settings.value()==next,"Inspection command redo");
    {std::ofstream f(root/"bad-inspection.json");f<<"{}";}rejects([&]{loadInspection(root/"bad-inspection.json",loaded);});require(loaded==next,"Invalid reload preserves settings");
    physics.remove(body);require(physics.size()==0,"Collision unload releases body");
    std::cout<<"PASS: LOD identity/anchors, render-triangle collision agreement, edit/undo/redo and failed-edit preservation, inspection save/reload, exact accepted mesh export and overwrite rejection; bytes="<<asset.bytes<<'\n';return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

#include "World/Rocks/RockAsset.h"
#include "World/Terrain.h"
#include "Runtime/EditHistory.h"
#include "Persistence/Document.h"
#include <cmath>
#include <iostream>
#include <map>
using namespace engine;
namespace {
void require(bool ok,const char* message){if(!ok)throw std::runtime_error(message);}
template<class F>void rejects(F f){bool rejected=false;try{f();}catch(const std::exception&){rejected=true;}require(rejected,"Invalid authoring data accepted");}
bool same(const RockResult& a,const RockResult& b){
    if(a.mesh.indices!=b.mesh.indices||a.mesh.vertices.size()!=b.mesh.vertices.size()||a.memberIds!=b.memberIds)return false;
    for(size_t i=0;i<a.mesh.vertices.size();++i)if(a.mesh.vertices[i].position!=b.mesh.vertices[i].position||a.mesh.vertices[i].normal!=b.mesh.vertices[i].normal)return false;
    return true;
}
void inspect(const RockResult& r){
    validateModel(r.mesh);std::map<std::pair<uint32_t,uint32_t>,std::pair<int,int>> edges;
    for(size_t i=0;i<r.mesh.indices.size();i+=3){const auto a=r.mesh.indices[i],b=r.mesh.indices[i+1],c=r.mesh.indices[i+2];
        for(auto edge:{std::pair{a,b},std::pair{b,c},std::pair{c,a}}){const int sign=edge.first<edge.second?1:-1;if(sign<0)std::swap(edge.first,edge.second);auto& e=edges[edge];++e.first;e.second+=sign;}
    }
    for(const auto& [_,edge]:edges)require(edge.first==2&&edge.second==0,"Profile mesh is open or inconsistently wound");
    for(const auto& v:r.mesh.vertices){require(std::abs(std::hypot(std::hypot(v.normal[0],v.normal[1]),v.normal[2])-1)<.001f,"Invalid surface normal");}
    require(r.surfaces.size()*3==r.mesh.indices.size(),"Missing triangle classifications");
}
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Expected preset library and new output directory");
    const std::filesystem::path library=argv[1],out=argv[2];if(std::filesystem::exists(out))throw std::runtime_error("Output exists");std::filesystem::create_directories(out);
    const GeneratedId id{1,5,{},0,"rock"};float boulderRatio=0;size_t checked=0;
    for(const auto& file:std::filesystem::directory_iterator(library))if(file.path().extension()==".json"){
        const auto recipe=loadRockRecipe(file.path());const auto asset=buildRockAsset(recipe,id);const auto& rock=asset.lods.front();
        for(const auto& lod:asset.lods)inspect(lod);
        require(same(rock,generateRock(recipe,id)),"Regeneration changed the accepted mesh");
        saveRockRecipe(out/file.path().filename(),recipe);const auto restored=loadRockRecipe(out/file.path().filename());require(restored==recipe&&same(rock,generateRock(restored,id)),"Saved recipe changed geometry");
        for(size_t i=0;i<asset.collision.size();++i)require(asset.collision[i]==rock.mesh.vertices[rock.mesh.indices[i]].position,"Collision diverges from accepted mesh");
        if(recipe.formation==0){
            auto explicitRecipe=recipe;explicitRecipe.volumes=planRockVolumes(recipe,id);require(same(rock,generateRock(explicitRecipe,id)),"Capturing volumes changed shape");
            saveRockRecipe(out/(file.path().stem().string()+"-volumes.json"),explicitRecipe);require(loadRockRecipe(out/(file.path().stem().string()+"-volumes.json"))==explicitRecipe,"Tilted primitive fields lost on save");
            const float ratio=rock.maximum[1]/(rock.maximum[0]-rock.minimum[0]);
            if(recipe.profile==1)boulderRatio=ratio;
            if(recipe.profile==2)require(ratio<.55f,"Broken slab lost its low silhouette");
            if(recipe.profile==4)require(ratio>1.4f,"Shard lost its vertical silhouette");
            explicitRecipe.volumes[0].pitch+=.15f;require(!same(rock,generateRock(explicitRecipe,id)),"Pitch edit had no effect");
        }
        ++checked;
    }
    require(boulderRatio>.55f&&boulderRatio<1.5f&&checked==6,"Expected distinct boulder and six presets");
    auto recipe=loadRockRecipe(library/"05-Braced-Outcrop.json");const auto before=generateRock(recipe,id);
    auto plan=planRockFormation(recipe,id,[](float,float){return 0.f;});RockMemberEdit edit;edit.slot=1;edit.variant=2;edit.translation={2,.3f,-1};edit.axes={.8f,1.2f,.7f};edit.yaw=.5f;edit.groundOnly=true;recipe.memberEdits={edit};
    const auto changed=generateRock(recipe,id);require(!same(before,changed)&&before.memberIds==changed.memberIds,"Member edit changed identity or had no effect");
    auto editedPlan=planRockFormation(recipe,id,[](float,float){return 0.f;});require(editedPlan[1].offset[0]==plan[1].offset[0]+2&&editedPlan[1].authoredLift==.3f,"Member displacement was ignored");
    for(size_t i=0;i<plan.size();++i)if(i!=1)require(editedPlan[i].offset==plan[i].offset&&editedPlan[i].axes==plan[i].axes&&editedPlan[i].variant==plan[i].variant,"Editing a member modified its siblings");
    saveRockRecipe(out/"edited.json",recipe);require(loadRockRecipe(out/"edited.json")==recipe&&same(changed,generateRock(loadRockRecipe(out/"edited.json"),id)),"Member edits lost on reload");
    auto other=id;other.region={-2,3};require(generateRock(recipe,other).memberIds!=changed.memberIds,"Regions share member identities");require(same(changed,generateRock(recipe,id)),"Return to region changed authored formation");
    EditHistory<RockRecipe> history(recipe);auto next=recipe;next.seed++;history.apply(next,[&](const auto& r){generateRock(r,id);});history.undo([&](const auto& r){generateRock(r,id);});require(history.value()==recipe,"Undo lost edits");history.redo([&](const auto& r){generateRock(r,id);});require(history.value()==next,"Redo lost edits");
    auto bad=recipe;bad.memberEdits.push_back(edit);rejects([&]{validateRecipe(bad);});bad=recipe;bad.memberEdits[0].axes[0]=0;rejects([&]{validateRecipe(bad);});bad=recipe;bad.version=4;rejects([&]{validateRecipe(bad);});
    bad=recipe;bad.volumes=planRockVolumes(recipe,id);bad.volumes[0].pitch=NAN;rejects([&]{validateRecipe(bad);});
    rejects([&]{generateTerrain(TerrainRecipe{}, {},recipe,{});});
    saveRockResult(out/"export",changed,recipe);require(loadRockRecipe(out/"export/recipe.json")==recipe,"Export lost authored recipe");
    writeDocument(out/"result.json","engine.rock-authoring-tests",{{"passed",true},{"presets",checked},{"closed_meshes",true},{"recipe_and_volume_roundtrips",true},{"member_identity",true},{"collision_agreement",true},{"windows_verified",false}});
    std::cout<<"PASS: six v5 presets, closed LOD meshes, distinct silhouettes, tilted volumes, saved edits, member/region identity, collision/export agreement, undo/redo and invalid input rejection\n";
    return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

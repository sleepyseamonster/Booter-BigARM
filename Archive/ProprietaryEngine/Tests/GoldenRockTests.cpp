#include "World/Rocks/RockAsset.h"
#include "World/Rocks/CalibratedRock.h"
#include "Persistence/Document.h"
#include "Runtime/EditHistory.h"
#include <cmath>
#include <iostream>
#include <map>
using namespace engine;
namespace {
void require(bool ok,const char* why){if(!ok)throw std::runtime_error(why);}
template<class F>void rejects(F action){bool failed=false;try{action();}catch(const std::exception&){failed=true;}require(failed,"Invalid calibrated data accepted");}
bool same(const RockResult& a,const RockResult& b){if(a.mesh.indices!=b.mesh.indices||a.mesh.vertices.size()!=b.mesh.vertices.size())return false;for(size_t i=0;i<a.mesh.vertices.size();++i)if(a.mesh.vertices[i].position!=b.mesh.vertices[i].position||a.mesh.vertices[i].normal!=b.mesh.vertices[i].normal)return false;return a.memberIds==b.memberIds;}
double inspect(const RockResult& r){
    validateModel(r.mesh);std::map<std::pair<uint32_t,uint32_t>,std::pair<int,int>> edges;double volume=0;
    for(size_t i=0;i<r.mesh.indices.size();i+=3){const auto a=r.mesh.indices[i],b=r.mesh.indices[i+1],c=r.mesh.indices[i+2];
        const auto p=r.mesh.vertices[a].position,q=r.mesh.vertices[b].position,t=r.mesh.vertices[c].position;
        volume+=(double(p[0])*(q[1]*t[2]-q[2]*t[1])+double(p[1])*(q[2]*t[0]-q[0]*t[2])+double(p[2])*(q[0]*t[1]-q[1]*t[0]))/6;
        for(auto e:{std::pair{a,b},std::pair{b,c},std::pair{c,a}}){const int sign=e.first<e.second?1:-1;if(sign<0)std::swap(e.first,e.second);auto& v=edges[e];++v.first;v.second+=sign;}
    }
    for(auto [_,v]:edges)require(v.first==2&&v.second==0,"Calibrated mesh is not closed and consistently wound");
    require(volume>0,"Rock has inverted volume");
    for(const auto& v:r.mesh.vertices)require(std::abs(std::hypot(std::hypot(v.normal[0],v.normal[1]),v.normal[2])-1)<.001,"Invalid calibrated normal");
    require(r.surfaces.size()*3==r.mesh.indices.size(),"Missing surface classification");return volume;
}
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Expected approved library and new output directory");const std::filesystem::path library=argv[1],out=argv[2];if(std::filesystem::exists(out))throw std::runtime_error("Output exists");std::filesystem::create_directories(out);
    const GeneratedId id{1,6,{},0,"rock"};size_t checked=0;
    for(auto entry:std::filesystem::directory_iterator(library))if(entry.path().extension()==".json"){
        const auto r=loadRockRecipe(entry.path());require(r.version==6&&r.volumes.size()==2,"Captured family changed");
        require(r.material.grit==220&&r.material.shale==200&&r.material.sideShale==0&&r.material.topShale==0&&r.material.geologyMm==2400&&r.material.worn==99,"Approved material controls lost");
        require(std::abs(r.fusion-.0657f)<1e-6f&&r.relaxation==.45f,"Approved meshing controls lost");
        const auto asset=buildRockAsset(r,id);for(auto& lod:asset.lods)inspect(lod);const auto& rock=asset.lods.front();
        saveRockRecipe(out/entry.path().filename(),r);require(loadRockRecipe(out/entry.path().filename())==r&&same(rock,generateRock(loadRockRecipe(out/entry.path().filename()),id)),"Captured recipe failed reload");
        for(size_t i=0;i<asset.collision.size();++i)require(asset.collision[i]==rock.mesh.vertices[rock.mesh.indices[i]].position,"Collider differs from accepted physical mesh");
        auto changed=r;changed.authoringScale=2;auto scaled=generateRock(changed,id);require(scaled.mesh.indices==rock.mesh.indices,"Scale changes topology");
        for(size_t i=0;i<rock.mesh.vertices.size();++i)for(size_t a=0;a<3;++a)require(scaled.mesh.vertices[i].position[a]==rock.mesh.vertices[i].position[a]*2,"Physical scale lost source placement");
        changed=r;changed.relaxation=0;auto raw=generateRock(changed,id);const auto rawVolume=inspect(raw),relaxedVolume=inspect(rock);
        require(std::abs(relaxedVolume/rawVolume-1)<.01&&std::abs(raw.minimum[1]-rock.minimum[1])<1e-5,"Relaxation changed volume or ground anchor");
        changed=r;++changed.seed;require(!same(rock,generateRock(changed,id)),"Variation seed has no effect");
        changed=r;changed.volumes[0].roll=.2f;require(!same(rock,generateRock(changed,id)),"Editable source pose has no effect");
        auto other=id;other.region={-1,3};require(generateRock(r,other).id!=rock.id&&same(rock,generateRock(r,id)),"Identity/regeneration failed");
        ++checked;
    }
    require(checked==3,"Missing approved variants");
    auto r=loadRockRecipe(library/"01-Golden-Rock.json");require(r.seed==2126351350&&r.volumes[0].shapeSeed==uint32_t(-1414301041)&&r.volumes[1].shapeSeed==uint32_t(-1128502573),"Reference seed bits lost");
    require(std::abs(r.volumes[0].center[0]+.3029579f)<1e-6&&std::abs(r.volumes[0].orientation[0]+.6820994f)<1e-6,"Captured resting transform lost");
    CalibratedRockVolume v(r.volumes[0],1,0);require(v.evaluate(r.volumes[0].center)<0&&v.evaluate({3,3,3})>0,"Primitive inside/outside orientation failed");
    auto bad=r;bad.volumes[0].orientation={0,0,0,0};rejects([&]{validateRecipe(bad);});bad=r;bad.volumes.clear();rejects([&]{validateRecipe(bad);});bad=r;bad.relaxation=NAN;rejects([&]{validateRecipe(bad);});bad=r;bad.material.geologyMm=0;rejects([&]{validateRecipe(bad);});
    bad=r;bad.volumes[0].halfSize={3,3,3};bad.samplingMm=5;bad.subdivisions=4;rejects([&]{generateRock(bad,id);});
    bad=r;bad.seed=uint64_t(UINT32_MAX)+1;rejects([&]{validateRecipe(bad);});bad=r;bad.version=5;rejects([&]{saveRockRecipe(out/"invalid-downgrade.json",bad);});
    bad={};bad.material.geologyMm=2400;rejects([&]{validateRecipe(bad);});bad={};bad.relaxation=.8f;rejects([&]{validateRecipe(bad);});
    EditHistory<RockRecipe> history(r);auto next=r;next.volumes[0].pitch=.15f;history.apply(next,[&](const auto& x){generateRock(x,id);});history.undo([&](const auto& x){generateRock(x,id);});require(history.value()==r,"Undo lost captured orientation");history.redo([&](const auto& x){generateRock(x,id);});require(history.value()==next,"Redo lost source edit");
    saveRockResult(out/"export",generateRock(r,id),r);require(loadRockRecipe(out/"export/recipe.json")==r,"Export lost physical authoring data");require(readDocument(out/"export/material.json","engine.rock-material").at("controls").at("geology_mm")==2400,"Export lost material calibration");
    writeDocument(out/"result.json","engine.golden-rock-tests",{{"passed",true},{"approved_variants",checked},{"captured_masses",6},{"physical_transforms_and_seed_bits",true},{"closed_lods",true},{"relaxation_preserves_volume_and_ground",true},{"saved_materials_and_edits",true},{"collision_agreement",true}});
    std::cout<<"PASS: three approved Unity recipes, six physical poses, calibrated materials, closed LODs, preserved volume/contact, deterministic chips, undo/save/export and collider agreement\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

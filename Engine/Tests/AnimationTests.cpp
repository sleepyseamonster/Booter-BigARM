#include "Assets/GltfImport.h"
#include "Animation/AnimationPlayer.h"
#include "Persistence/Document.h"
#include <fstream>
#include <iostream>
#include <cmath>
#include <cstring>
using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F&& f){bool failed=false;try{f();}catch(const std::exception&){failed=true;}require(failed,"Expected model rejection");}
float difference(const std::vector<SkinVertex>& a,const std::vector<SkinVertex>& b){float result=0;for(size_t i=0;i<a.size();++i)for(size_t j=0;j<3;++j)result=std::max(result,std::abs(a[i].position[j]-b[i].position[j]));return result;}
uint32_t word(const std::vector<char>& b,size_t offset){uint32_t v;std::memcpy(&v,b.data()+offset,4);return v;}
}
int main(int argc,char** argv)try{
    if(argc!=3)throw std::runtime_error("Expected source GLB and temporary directory");
    const std::filesystem::path source=argv[1],root=argv[2];std::filesystem::create_directories(root);
    const auto directory=root/"cooked";if(std::filesystem::exists(directory))std::filesystem::remove_all(directory);
    auto original=importGltf(source);require(original.vertices.size()==168&&original.joints.size()==7&&original.clips.size()==2,"Real proxy import topology");
    saveModel(directory,original);auto data=std::make_shared<ModelData>(loadModel(directory/"model.json"));
    require(data->indices==original.indices&&data->vertices.size()==original.vertices.size()&&data->baseColor==original.baseColor,"Cooked model roundtrip");
    AnimationPlayer player(data);const auto rest=skinVertices(*data,player.rest());
    require(difference(rest,data->vertices)<.0001f,"Hierarchy and inverse binds reproduce source bind pose");
    const auto walk=skinVertices(*data,player.sample(1,.25));require(difference(rest,walk)>.3f,"Walk clip moves weighted vertices");
    const auto wrapped=skinVertices(*data,player.sample(1,1.25));require(difference(walk,wrapped)<.0001f,"Clip looping is bounded and repeatable");
    const auto idle=skinVertices(*data,player.sample(0,.25));
    const auto blend0=skinVertices(*data,player.sample(0,.25,1,0));const auto blend1=skinVertices(*data,player.sample(0,.25,1,1));
    require(difference(blend0,idle)<.0001f&&difference(blend1,walk)<.0001f,"Blend endpoints preserve each clip");
    const auto middle=skinVertices(*data,player.sample(0,.25,1,.5f));require(difference(middle,idle)>.05f&&difference(middle,walk)>.05f,"Intermediate blend uses both poses");
    rejects([&]{player.sample(55,0);});rejects([&]{player.sample(0,NAN);});rejects([&]{player.sample(0,0,1,2);});
    auto bad=*data;bad.vertices[0].joints[0]=255;rejects([&]{validateModel(bad);});bad=*data;bad.nodes[1].parent=1;rejects([&]{validateModel(bad);});
    bad=*data;bad.clips[0].tracks[0].times[1]=0;rejects([&]{validateModel(bad);});
    auto document=readDocument(directory/"model.json","engine.model");document["mesh_crc"]=0;writeDocument(directory/"model.json","engine.model",document);rejects([&]{loadModel(directory/"model.json");});
    // Mutated source fixtures exercise importer rejection, not a substitute for source parsing.
    std::ifstream stream(source,std::ios::binary);std::vector<char> bytes((std::istreambuf_iterator<char>(stream)),{});
    const auto jsonBytes=word(bytes,12);auto json=Json::parse(bytes.begin()+20,bytes.begin()+20+jsonBytes);
    const auto binSize=word(bytes,20+jsonBytes);std::ofstream buffer(root/"source.bin",std::ios::binary);buffer.write(bytes.data()+28+jsonBytes,binSize);buffer.close();
    json["buffers"][0]["uri"]="source.bin";
    auto write=[&](const Json& data){std::ofstream f(root/"source.gltf");f<<data.dump();};
    write(json);const auto external=importGltf(root/"source.gltf");require(external.indices==data->indices,"Contained external glTF buffer supported");
    auto invalid=json;invalid["skins"][0]["joints"][0]=999;write(invalid);rejects([&]{importGltf(root/"source.gltf");});
    invalid=json;invalid["accessors"][0]["count"]=999999999;write(invalid);rejects([&]{importGltf(root/"source.gltf");});
    invalid=json;invalid["extensionsRequired"]={"UNKNOWN_required_feature"};write(invalid);rejects([&]{importGltf(root/"source.gltf");});
    invalid=json;invalid["buffers"][0]["uri"]="../outside.bin";write(invalid);rejects([&]{importGltf(root/"source.gltf");});
    invalid=json;invalid["animations"][0]["samplers"][0]["interpolation"]="STEP";write(invalid);rejects([&]{importGltf(root/"source.gltf");});
    // Static hierarchy transforms use the same palette contract without a skin.
    invalid=json;invalid.erase("skins");invalid.erase("animations");invalid["nodes"][1].erase("skin");invalid["nodes"][1]["translation"]={2,0,0};
    invalid["meshes"][0]["primitives"][0]["attributes"].erase("JOINTS_0");invalid["meshes"][0]["primitives"][0]["attributes"].erase("WEIGHTS_0");write(invalid);
    auto staticData=std::make_shared<ModelData>(importGltf(root/"source.gltf"));AnimationPlayer staticPlayer(staticData);const auto staticVertices=skinVertices(*staticData,staticPlayer.rest());
    require(std::abs(staticVertices[0].position[0]-staticData->vertices[0].position[0]-2)<.0001f,"Static mesh node transform retained");
    std::cout<<"PASS: GLB/glTF cook, skin hierarchy/bind pose, clip sampling/blending, static transforms and bounded rejection\n";return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

#include "Animation/Model.h"
#include "Assets/TextureData.h"
#include "Persistence/Document.h"
#include <bit>
#include <cmath>
#include <cstring>
#include <fstream>
#include <set>
#include <stdexcept>
namespace engine {
namespace {
void require(bool condition,const char* message) {if(!condition)throw std::runtime_error(message);}
template<size_t N> void finite(const std::array<float,N>& values,float limit=10000) {for(float v:values)require(std::isfinite(v)&&std::abs(v)<=limit,"Nonfinite or out-of-range model value");}
void rotation(const std::array<float,4>& q) {finite(q,1);float norm=0;for(float v:q)norm+=v*v;require(std::abs(norm-1)<.002f,"Model quaternion must be normalized");}
uint32_t word(const std::vector<uint8_t>& b,size_t o) {uint32_t value;std::memcpy(&value,b.data()+o,4);return value;}
}
void validateModel(const ModelData& m) {
    require(!m.vertices.empty()&&m.vertices.size()<=100000 && !m.indices.empty()&&m.indices.size()<=300000&&m.indices.size()%3==0,"Model mesh exceeds supported bounds");
    require(!m.nodes.empty()&&m.nodes.size()<=128 && !m.joints.empty()&&m.joints.size()<=64&&m.joints.size()==m.inverseBind.size()&&m.clips.size()<=16,"Model skeleton/clip bounds");
    require(std::isfinite(m.roughness)&&m.roughness>=0&&m.roughness<=1&&std::isfinite(m.metallic)&&m.metallic>=0&&m.metallic<=1,"Invalid material factors");
    finite(m.baseColor,1);for(float v:m.baseColor)require(v>=0,"Invalid model color");
    for(size_t i=0;i<m.nodes.size();++i) {const auto& n=m.nodes[i];require(n.parent>=-1&&n.parent<int(i),"Model nodes must be parent-first and acyclic");finite(n.translation);rotation(n.rotation);}
    std::set<uint32_t> joints;
    for(size_t i=0;i<m.joints.size();++i) {
        require(m.joints[i]<m.nodes.size()&&joints.insert(m.joints[i]).second,"Invalid or duplicate skin joint");finite(m.inverseBind[i]);
        const auto& matrix=m.inverseBind[i];require(std::abs(matrix[3])+std::abs(matrix[7])+std::abs(matrix[11])+std::abs(matrix[15]-1)<.001f,"Inverse bind must be affine");
        for(size_t c=0;c<3;++c)for(size_t d=0;d<3;++d) {float dot=0;for(size_t r=0;r<3;++r)dot+=matrix[c*4+r]*matrix[d*4+r];require(std::abs(dot-(c==d?1.f:0.f))<.002f,"Inverse bind basis must be rigid");}
        const float determinant=matrix[0]*(matrix[5]*matrix[10]-matrix[9]*matrix[6])-matrix[4]*(matrix[1]*matrix[10]-matrix[9]*matrix[2])+matrix[8]*(matrix[1]*matrix[6]-matrix[5]*matrix[2]);
        require(determinant>0,"Reflected inverse bind basis is unsupported");
    }
    for(const auto& v:m.vertices) {
        finite(v.position);finite(v.normal,1.001f);finite(v.uv);finite(v.tangent,1.001f);finite(v.weights,1.001f);
        float norm=0,weight=0,tangent=0,orthogonal=0;
        for(size_t i=0;i<3;++i) {norm+=v.normal[i]*v.normal[i];tangent+=v.tangent[i]*v.tangent[i];orthogonal+=v.tangent[i]*v.normal[i];}
        require(std::abs(norm-1)<.01f&&std::abs(tangent-1)<.01f&&std::abs(orthogonal)<.01f&&std::abs(std::abs(v.tangent[3])-1)<.001f,"Model needs unit normal and orthogonal tangent");
        for(size_t i=0;i<4;++i) {require(v.weights[i]>=0&&v.joints[i]<m.joints.size(),"Invalid skin influence");weight+=v.weights[i];}
        require(std::abs(weight-1)<.002f,"Skin weights must sum to one");
    }
    for(auto index:m.indices)require(index<m.vertices.size(),"Model index out of range");
    size_t keys=0;std::set<std::string> names;
    for(const auto& clip:m.clips) {
        require(!clip.name.empty()&&clip.name.size()<=128&&names.insert(clip.name).second&&std::isfinite(clip.duration)&&clip.duration>0&&clip.duration<=600&&clip.tracks.size()<=256,"Invalid animation clip");
        std::set<std::pair<uint32_t,bool>> tracks;
        for(const auto& t:clip.tracks) {
            require(t.node<m.nodes.size()&&tracks.emplace(t.node,t.rotation).second&&!t.times.empty()&&t.times.size()==t.values.size(),"Invalid or duplicate animation track");
            keys+=t.times.size();require(keys<=8192,"Animation key budget exceeded");float previous=-1;
            for(size_t i=0;i<t.times.size();++i) {
                require(std::isfinite(t.times[i])&&t.times[i]>=0&&t.times[i]>previous&&t.times[i]<=clip.duration,"Animation times must be increasing within duration");previous=t.times[i];
                if(t.rotation)rotation(t.values[i]);else finite(t.values[i]);
            }
        }
    }
}
void saveModel(const std::filesystem::path& directory,const ModelData& m) {
    static_assert(std::endian::native==std::endian::little);
    validateModel(m);require(!std::filesystem::exists(directory),"Model output directory already exists");
    Json nodes=Json::array(),clips=Json::array();
    for(const auto& n:m.nodes)nodes.push_back({{"parent",n.parent},{"translation",n.translation},{"rotation",n.rotation}});
    for(const auto& c:m.clips) {Json tracks=Json::array();for(const auto& t:c.tracks)tracks.push_back({{"node",t.node},{"rotation",t.rotation},{"times",t.times},{"values",t.values}});clips.push_back({{"name",c.name},{"duration",c.duration},{"tracks",tracks}});}
    std::vector<uint8_t> bytes(16+m.vertices.size()*sizeof(SkinVertex)+m.indices.size()*4);
    const uint32_t header[]={0x3148534d,1,uint32_t(m.vertices.size()),uint32_t(m.indices.size())};
    std::memcpy(bytes.data(),header,16);std::memcpy(bytes.data()+16,m.vertices.data(),m.vertices.size()*sizeof(SkinVertex));
    std::memcpy(bytes.data()+16+m.vertices.size()*sizeof(SkinVertex),m.indices.data(),m.indices.size()*4);
    Json payload={{"mesh","mesh.bin"},{"mesh_bytes",bytes.size()},{"mesh_crc",textureCrc(bytes)},{"nodes",nodes},{"joints",m.joints},{"inverse_bind",m.inverseBind},{"clips",clips},{"base_color_linear",m.baseColor},{"roughness",m.roughness},{"metallic",m.metallic}};
    require(payload.dump().size()<documentLimit-128,"Cooked model metadata exceeds document limit");
    std::filesystem::create_directories(directory);
    std::ofstream out(directory/"mesh.bin",std::ios::binary);out.write(reinterpret_cast<const char*>(bytes.data()),std::streamsize(bytes.size()));out.close();require(bool(out),"Cannot write model mesh");
    writeDocument(directory/"model.json","engine.model",payload);
}
ModelData loadModel(const std::filesystem::path& manifest) {
    const auto p=readDocument(manifest,"engine.model");require(p.size()==10,"Unexpected model fields");
    require(p.at("mesh_bytes").is_number_integer()&&p.at("mesh_bytes")>=16&&p.at("mesh_bytes")<=32*1024*1024&&p.at("mesh_crc").is_number_integer()&&p.at("mesh_crc")>=0&&p.at("mesh_crc")<=UINT32_MAX,"Invalid model payload metadata");
    const auto file=contentPath(std::filesystem::absolute(manifest).parent_path(),p.at("mesh").get<std::string>());
    const auto bytes=readTextureFile(file);require(bytes.size()>=16&&bytes.size()==p.at("mesh_bytes").get<size_t>()&&textureCrc(bytes)==p.at("mesh_crc").get<uint32_t>(),"Model payload checksum/size mismatch");
    const uint32_t nv=word(bytes,8),ni=word(bytes,12);
    require(word(bytes,0)==0x3148534d&&word(bytes,4)==1&&nv>0&&nv<=100000&&ni>0&&ni<=300000&&bytes.size()==16+size_t(nv)*sizeof(SkinVertex)+size_t(ni)*4,"Unsupported model mesh header");
    ModelData m;m.vertices.resize(nv);m.indices.resize(ni);
    std::memcpy(m.vertices.data(),bytes.data()+16,nv*sizeof(SkinVertex));std::memcpy(m.indices.data(),bytes.data()+16+nv*sizeof(SkinVertex),ni*4);
    const auto& nodes=p.at("nodes");require(nodes.is_array()&&nodes.size()<=128,"Model node bound");
    for(const auto& n:nodes) {require(n.is_object()&&n.size()==3&&n.at("parent").is_number_integer()&&n.at("parent")>=-1&&n.at("parent")<128,"Invalid model node");m.nodes.push_back({n.at("parent").get<int>(),n.at("translation").get<std::array<float,3>>(),n.at("rotation").get<std::array<float,4>>()});}
    require(p.at("joints").is_array()&&p.at("joints").size()<=64&&p.at("inverse_bind").size()<=64,"Model palette bound");
    for(const auto& joint:p.at("joints"))require(joint.is_number_integer()&&joint>=0&&joint<128,"Invalid joint node index");
    m.roughness=p.at("roughness").get<float>();m.metallic=p.at("metallic").get<float>();
    m.joints=p.at("joints").get<std::vector<uint32_t>>();m.inverseBind=p.at("inverse_bind").get<std::vector<SkinMatrix>>();m.baseColor=p.at("base_color_linear").get<std::array<float,4>>();
    const auto& clips=p.at("clips");require(clips.is_array()&&clips.size()<=16,"Clip count bound");size_t keys=0;
    for(const auto& c:clips) {
        require(c.size()==3&&c.at("tracks").is_array()&&c.at("tracks").size()<=256,"Invalid clip fields");ModelClip clip{c.at("name").get<std::string>(),c.at("duration").get<float>(),{}};
        for(const auto& t:c.at("tracks")) {require(t.size()==4&&t.at("times").is_array()&&t.at("values").is_array()&&t.at("rotation").is_boolean()&&t.at("node").is_number_integer()&&t.at("node")>=0&&t.at("node")<128,"Invalid track fields");keys+=t.at("times").size();require(keys<=8192&&t.at("values").size()==t.at("times").size(),"Animation key bound");clip.tracks.push_back({t.at("node").get<uint32_t>(),t.at("rotation").get<bool>(),t.at("times").get<std::vector<float>>(),t.at("values").get<std::vector<std::array<float,4>>>()});}
        m.clips.push_back(std::move(clip));
    }
    validateModel(m);return m;
}
}

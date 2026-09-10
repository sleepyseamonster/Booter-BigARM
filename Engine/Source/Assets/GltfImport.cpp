#include "Assets/GltfImport.h"
#include "Assets/TextureData.h"
#include "Persistence/Document.h"
#include <fastgltf/core.hpp>
#include <fastgltf/tools.hpp>
#include <algorithm>
#include <cmath>
#include <cstring>
#include <functional>
#include <numeric>
#include <stdexcept>
namespace engine {
namespace {
void require(bool c,const char* message) {if(!c)throw std::runtime_error(message);}
SkinMatrix identity() {return {1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};}
template<class T> std::vector<T> read(const fastgltf::Asset& asset,size_t index,fastgltf::AccessorType type) {
    require(index<asset.accessors.size(),"glTF accessor index out of range");const auto& accessor=asset.accessors[index];require(accessor.type==type,"Unexpected glTF accessor shape");
    std::vector<T> data;data.reserve(accessor.count);fastgltf::iterateAccessor<T>(asset,accessor,[&](T v){data.push_back(v);});return data;
}
}
ModelData importGltf(const std::filesystem::path& path) {
    const auto fileBytes=std::filesystem::file_size(path);require(fileBytes>0&&fileBytes<=32*1024*1024,"glTF file must be at most 32 MiB");
    auto file=fastgltf::MappedGltfFile::FromPath(path);if(!file)throw std::runtime_error("Cannot open glTF source");
    fastgltf::Parser parser;auto parsed=parser.loadGltf(file.get(),path.parent_path(),fastgltf::Options::DecomposeNodeMatrices);
    if(parsed.error()!=fastgltf::Error::None)throw std::runtime_error(std::string("glTF parse: ")+std::string(fastgltf::getErrorMessage(parsed.error())));
    auto asset=std::move(parsed.get());
    require(asset.extensionsUsed.empty()&&asset.extensionsRequired.empty(),"This model profile does not support glTF extensions");
    require(asset.nodes.size()>0&&asset.nodes.size()<=127&&asset.meshes.size()==1&&asset.meshes[0].primitives.size()==1&&asset.skins.size()<=1&&asset.scenes.size()==1&&asset.animations.size()<=16,"Expected one scene, one mesh primitive and at most one skin (127 nodes / 16 clips)");
    require(asset.images.empty()&&asset.textures.empty()&&asset.cameras.empty()&&asset.lights.empty(),"External image/material bindings and scene cameras/lights require a later profile");
    require(asset.buffers.size()<=8&&asset.bufferViews.size()<=1024&&asset.accessors.size()<=1024,"glTF table budget exceeded");
    size_t totalBytes=0;
    for(auto& buffer:asset.buffers) {
        require(buffer.byteLength<=32*1024*1024,"glTF buffer byte budget");totalBytes+=buffer.byteLength;require(totalBytes<=32*1024*1024,"glTF aggregate buffer byte budget");
        if(auto* uri=std::get_if<fastgltf::sources::URI>(&buffer.data)) {
            require(uri->uri.isLocalPath()&&uri->fileByteOffset==0,"Only contained relative glTF buffers are supported");
            const auto source=contentPath(std::filesystem::absolute(path).parent_path(),uri->uri.fspath().generic_string());
            const auto bytes=readTextureFile(source);require(bytes.size()==buffer.byteLength,"External glTF buffer size mismatch");
            fastgltf::sources::Array copy{fastgltf::StaticVector<std::byte>(bytes.size())};std::memcpy(copy.bytes.data(),bytes.data(),bytes.size());buffer.data=std::move(copy);
        }
        const auto* bytes=std::get_if<fastgltf::sources::Array>(&buffer.data);require(bytes&&bytes->bytes.size()>=buffer.byteLength,"Unsupported glTF buffer storage");
    }
    for(const auto& view:asset.bufferViews) {
        require(!view.meshoptCompression&&view.bufferIndex<asset.buffers.size(),"Unsupported/compressed buffer view");const auto size=asset.buffers[view.bufferIndex].byteLength;
        require(view.byteOffset<=size&&view.byteLength<=size-view.byteOffset,"glTF buffer view exceeds payload");
    }
    for(const auto& accessor:asset.accessors) {
        require(!accessor.sparse&&accessor.bufferViewIndex&&*accessor.bufferViewIndex<asset.bufferViews.size()&&accessor.count>0&&accessor.count<=300000,"Sparse/missing/unbounded accessor unsupported");
        const auto& view=asset.bufferViews[*accessor.bufferViewIndex];const auto bytes=fastgltf::getElementByteSize(accessor.type,accessor.componentType);const size_t stride=view.byteStride.value_or(bytes);
        require(bytes>0&&stride>=bytes&&stride<=256&&accessor.byteOffset<=view.byteLength,"Invalid accessor layout");
        require((accessor.count-1)<=((view.byteLength-accessor.byteOffset)/stride)&&bytes<=view.byteLength-accessor.byteOffset-(accessor.count-1)*stride,"Accessor exceeds its buffer view");
    }
    require(fastgltf::validate(asset)==fastgltf::Error::None,"glTF structural validation failed");
    ModelData model;model.nodes.push_back({});
    std::vector<int> parents(asset.nodes.size(),-1);size_t meshNode=SIZE_MAX;
    for(size_t i=0;i<asset.nodes.size();++i) {
        const auto& node=asset.nodes[i];
        if(node.meshIndex) {require(meshNode==SIZE_MAX&&*node.meshIndex==0,"Expected one mesh instance");meshNode=i;}
        require(std::holds_alternative<fastgltf::TRS>(node.transform),"Node transform cannot be decomposed");
        for(auto child:node.children) {require(child<asset.nodes.size()&&child!=i&&parents[child]==-1,"Invalid/multiple-parent node");parents[child]=int(i);}
    }
    require(meshNode!=SIZE_MAX,"Missing mesh instance");
    std::vector<uint32_t> mapping(asset.nodes.size(),UINT32_MAX);std::vector<bool> visiting(asset.nodes.size(),false);
    std::function<void(size_t,int)> visit=[&](size_t index,int parent) {
        require(!visiting[index]&&mapping[index]==UINT32_MAX,"Node hierarchy is cyclic or duplicated");visiting[index]=true;
        const auto& node=asset.nodes[index];const auto& trs=std::get<fastgltf::TRS>(node.transform);
        for(size_t i=0;i<3;++i)require(std::isfinite(trs.scale[i])&&std::abs(trs.scale[i]-1)<.0001f,"Bake node scale into the mesh before import");
        ModelNode next;next.parent=parent;for(size_t i=0;i<3;++i)next.translation[i]=trs.translation[i];for(size_t i=0;i<4;++i)next.rotation[i]=trs.rotation[i];
        mapping[index]=uint32_t(model.nodes.size());model.nodes.push_back(next);
        for(auto child:node.children)visit(child,int(mapping[index]));visiting[index]=false;
    };
    for(auto root:asset.scenes[0].nodeIndices) {require(root<asset.nodes.size()&&parents[root]==-1,"Invalid scene root");visit(root,0);}
    require(std::none_of(mapping.begin(),mapping.end(),[](auto i){return i==UINT32_MAX;}),"All model nodes must belong to the selected scene");
    const auto& primitive=asset.meshes[0].primitives[0];require(primitive.type==fastgltf::PrimitiveType::Triangles&&primitive.targets.empty(),"Only triangle meshes without morph targets are supported");
    for(const auto& attribute:primitive.attributes) {
        const auto& name=attribute.name;const auto& a=asset.accessors.at(attribute.accessorIndex);
        using C=fastgltf::ComponentType;const bool smallUnsigned=a.componentType==C::UnsignedByte||a.componentType==C::UnsignedShort;
        if(name=="POSITION"||name=="NORMAL"||name=="TANGENT")require(a.componentType==C::Float&&!a.normalized,"Position/normal/tangent attributes must use floats");
        else if(name=="JOINTS_0")require(smallUnsigned&&!a.normalized,"Joint indices must be unsigned byte/short");
        else if(name=="WEIGHTS_0"||name=="TEXCOORD_0")require((a.componentType==C::Float&&!a.normalized)||(smallUnsigned&&a.normalized),"Unsupported weight/UV component format");
        else require(false,"Unsupported vertex attribute in this model profile");
    }
    if(primitive.indicesAccessor) {const auto& a=asset.accessors.at(*primitive.indicesAccessor);using C=fastgltf::ComponentType;require(!a.normalized&&(a.componentType==C::UnsignedByte||a.componentType==C::UnsignedShort||a.componentType==C::UnsignedInt),"Invalid index component format");}
    auto attribute=[&](const char* name) {const auto found=primitive.findAttribute(name);require(found!=primitive.attributes.end(),"Required vertex attribute missing (POSITION/NORMAL or skin data)");return found->accessorIndex;};
    const auto positions=read<fastgltf::math::fvec3>(asset,attribute("POSITION"),fastgltf::AccessorType::Vec3);
    const auto normals=read<fastgltf::math::fvec3>(asset,attribute("NORMAL"),fastgltf::AccessorType::Vec3);
    require(positions.size()<=100000&&normals.size()==positions.size(),"Position/normal count mismatch");model.vertices.resize(positions.size());
    for(size_t i=0;i<positions.size();++i)for(size_t c=0;c<3;++c) {model.vertices[i].position[c]=positions[i][c];model.vertices[i].normal[c]=normals[i][c];}
    if(const auto uv=primitive.findAttribute("TEXCOORD_0");uv!=primitive.attributes.end()) {
        const auto values=read<fastgltf::math::fvec2>(asset,uv->accessorIndex,fastgltf::AccessorType::Vec2);require(values.size()==positions.size(),"UV count mismatch");
        for(size_t i=0;i<values.size();++i)for(size_t c=0;c<2;++c)model.vertices[i].uv[c]=values[i][c];
    }
    if(const auto tangent=primitive.findAttribute("TANGENT");tangent!=primitive.attributes.end()) {
        const auto values=read<fastgltf::math::fvec4>(asset,tangent->accessorIndex,fastgltf::AccessorType::Vec4);require(values.size()==positions.size(),"Tangent count mismatch");
        for(size_t i=0;i<values.size();++i)for(size_t c=0;c<4;++c)model.vertices[i].tangent[c]=values[i][c];
    } else for(auto& v:model.vertices) {
        const auto& n=v.normal;std::array<float,3> t=std::abs(n[1])<.9f?std::array<float,3>{n[2],0,-n[0]}:std::array<float,3>{0,-n[2],n[1]};
        const float length=std::sqrt(t[0]*t[0]+t[1]*t[1]+t[2]*t[2]);require(length>1e-6f,"Degenerate source normal");for(size_t c=0;c<3;++c)v.tangent[c]=t[c]/length;
    }
    if(primitive.indicesAccessor) model.indices=read<uint32_t>(asset,*primitive.indicesAccessor,fastgltf::AccessorType::Scalar);
    else {model.indices.resize(model.vertices.size());std::iota(model.indices.begin(),model.indices.end(),0);}
    if(asset.nodes[meshNode].skinIndex) {
        require(*asset.nodes[meshNode].skinIndex<asset.skins.size(),"Skin index out of range");const auto& skin=asset.skins[*asset.nodes[meshNode].skinIndex];require(!skin.joints.empty()&&skin.joints.size()<=64,"Skin joint budget exceeded");
        for(auto joint:skin.joints) {require(joint<mapping.size(),"Missing skin joint node");model.joints.push_back(mapping[joint]);}
        if(skin.inverseBindMatrices) {
            const auto matrices=read<fastgltf::math::fmat4x4>(asset,*skin.inverseBindMatrices,fastgltf::AccessorType::Mat4);require(matrices.size()==skin.joints.size(),"Inverse bind matrix count mismatch");
            for(const auto& m:matrices) {SkinMatrix matrix;for(size_t c=0;c<4;++c)for(size_t row=0;row<4;++row)matrix[c*4+row]=m[c][row];model.inverseBind.push_back(matrix);}
        } else model.inverseBind.resize(skin.joints.size(),identity());
        const auto joints=read<fastgltf::math::uvec4>(asset,attribute("JOINTS_0"),fastgltf::AccessorType::Vec4);
        const auto weights=read<fastgltf::math::fvec4>(asset,attribute("WEIGHTS_0"),fastgltf::AccessorType::Vec4);require(joints.size()==positions.size()&&weights.size()==positions.size(),"Skin attribute count mismatch");
        for(size_t i=0;i<positions.size();++i) {float total=0;for(size_t k=0;k<4;++k) {require(joints[i][k]<skin.joints.size()&&std::isfinite(weights[i][k])&&weights[i][k]>=0,"Invalid skin influence");model.vertices[i].joints[k]=uint8_t(joints[i][k]);model.vertices[i].weights[k]=weights[i][k];total+=weights[i][k];}require(total>.0001f,"Zero skin weights");for(auto& weight:model.vertices[i].weights)weight/=total;}
    } else {model.joints={mapping[meshNode]};model.inverseBind={identity()};}
    if(primitive.materialIndex) {
        const auto& material=asset.materials.at(*primitive.materialIndex);require(material.alphaMode==fastgltf::AlphaMode::Opaque&&!material.doubleSided,"Only opaque single-sided model materials are supported");
        for(size_t i=0;i<3;++i)require(material.emissiveFactor[i]==0,"Emissive model materials are not in this profile");
        for(size_t i=0;i<4;++i)model.baseColor[i]=material.pbrData.baseColorFactor[i];
        model.roughness=material.pbrData.roughnessFactor;model.metallic=material.pbrData.metallicFactor;
    }
    size_t keys=0;
    for(size_t index=0;index<asset.animations.size();++index) {
        const auto& animation=asset.animations[index];ModelClip clip;clip.name=animation.name.empty()?"clip-"+std::to_string(index):std::string(animation.name);clip.duration=0;
        for(const auto& channel:animation.channels) {
            require(channel.nodeIndex&&*channel.nodeIndex<mapping.size()&&channel.samplerIndex<animation.samplers.size(),"Invalid animation target");const auto& sampler=animation.samplers[channel.samplerIndex];
            require(sampler.interpolation==fastgltf::AnimationInterpolation::Linear,"Only LINEAR animation interpolation is supported");
            require(channel.path==fastgltf::AnimationPath::Rotation||channel.path==fastgltf::AnimationPath::Translation,"Animated scales and morph weights require another profile");
            ModelTrack track;track.node=mapping[*channel.nodeIndex];track.rotation=channel.path==fastgltf::AnimationPath::Rotation;
            track.times=read<float>(asset,sampler.inputAccessor,fastgltf::AccessorType::Scalar);keys+=track.times.size();require(keys<=8192,"Animation key budget exceeded");
            if(track.rotation) {const auto values=read<fastgltf::math::fvec4>(asset,sampler.outputAccessor,fastgltf::AccessorType::Vec4);for(const auto& v:values)track.values.push_back({v[0],v[1],v[2],v[3]});}
            else {const auto values=read<fastgltf::math::fvec3>(asset,sampler.outputAccessor,fastgltf::AccessorType::Vec3);for(const auto& v:values)track.values.push_back({v[0],v[1],v[2],0});}
            require(track.values.size()==track.times.size(),"Animation key count mismatch");for(float time:track.times)clip.duration=std::max(clip.duration,time);clip.tracks.push_back(std::move(track));
        }
        if(clip.duration==0)clip.duration=1;model.clips.push_back(std::move(clip));
    }
    validateModel(model);return model;
}
}

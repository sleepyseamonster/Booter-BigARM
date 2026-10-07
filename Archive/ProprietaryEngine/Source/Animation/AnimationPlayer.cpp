#include "Animation/AnimationPlayer.h"
#include <ozz/animation/offline/raw_skeleton.h>
#include <ozz/animation/offline/skeleton_builder.h>
#include <ozz/animation/offline/raw_animation.h>
#include <ozz/animation/offline/animation_builder.h>
#include <ozz/animation/runtime/animation.h>
#include <ozz/animation/runtime/skeleton.h>
#include <ozz/animation/runtime/sampling_job.h>
#include <ozz/animation/runtime/blending_job.h>
#include <ozz/animation/runtime/local_to_model_job.h>
#include <ozz/base/maths/soa_transform.h>
#include <ozz/base/maths/simd_math.h>
#include <ozz/base/containers/vector.h>
#include <cmath>
#include <functional>
#include <stdexcept>
namespace engine {
struct AnimationPlayer::Impl {
    std::shared_ptr<const ModelData> source;
    ozz::unique_ptr<ozz::animation::Skeleton> skeleton;
    std::vector<ozz::unique_ptr<ozz::animation::Animation>> clips;
    std::vector<size_t> jointMap;
    ozz::animation::SamplingJob::Context contextA,contextB;
    ozz::vector<ozz::math::SoaTransform> a,b,blended;
    ozz::vector<ozz::math::Float4x4> models;
    std::vector<SkinMatrix> palette;
    explicit Impl(std::shared_ptr<const ModelData> data):source(std::move(data)) {
        if(!source)throw std::invalid_argument("Animation requires model data");validateModel(*source);
        ozz::animation::offline::RawSkeleton raw;
        std::function<void(size_t,ozz::animation::offline::RawSkeleton::Joint&)> build=[&](size_t index,auto& joint) {
            const auto& n=source->nodes[index];joint.name=std::to_string(index).c_str();
            joint.transform.translation={n.translation[0],n.translation[1],n.translation[2]};
            joint.transform.rotation={n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]};joint.transform.scale={1,1,1};
            size_t count=0;for(const auto& child:source->nodes)count+=child.parent==int(index);joint.children.resize(count);
            size_t next=0;for(size_t i=0;i<source->nodes.size();++i)if(source->nodes[i].parent==int(index))build(i,joint.children[next++]);
        };
        size_t roots=0;for(const auto& n:source->nodes)roots+=n.parent==-1;raw.roots.resize(roots);
        size_t root=0;for(size_t i=0;i<source->nodes.size();++i)if(source->nodes[i].parent==-1)build(i,raw.roots[root++]);
        skeleton=ozz::animation::offline::SkeletonBuilder{}(raw);if(!skeleton)throw std::runtime_error("Ozz skeleton conversion failed");
        jointMap.resize(source->nodes.size());
        for(size_t i=0;i<source->nodes.size();++i)jointMap.at(std::stoul(skeleton->joint_names()[i]))=i;
        for(const auto& clip:source->clips) {
            ozz::animation::offline::RawAnimation animation;animation.duration=clip.duration;animation.name=clip.name.c_str();animation.tracks.resize(source->nodes.size());
            for(size_t i=0;i<source->nodes.size();++i) {
                const auto& n=source->nodes[i];auto& track=animation.tracks[jointMap[i]];
                track.translations.push_back({0,{n.translation[0],n.translation[1],n.translation[2]}});
                track.rotations.push_back({0,{n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]}});track.scales.push_back({0,{1,1,1}});
            }
            for(const auto& input:clip.tracks) {
                auto& track=animation.tracks[jointMap[input.node]];
                if(input.rotation) {track.rotations.clear();for(size_t i=0;i<input.times.size();++i) {const auto& q=input.values[i];track.rotations.push_back({input.times[i],{q[0],q[1],q[2],q[3]}});}}
                else {track.translations.clear();for(size_t i=0;i<input.times.size();++i) {const auto& v=input.values[i];track.translations.push_back({input.times[i],{v[0],v[1],v[2]}});}}
            }
            auto result=ozz::animation::offline::AnimationBuilder{}(animation);if(!result)throw std::runtime_error("Ozz animation conversion failed");clips.push_back(std::move(result));
        }
        contextA.Resize(skeleton->num_joints());contextB.Resize(skeleton->num_joints());
        a.resize(skeleton->num_soa_joints());b.resize(a.size());blended.resize(a.size());models.resize(skeleton->num_joints());palette.resize(source->joints.size());
    }
    void matrices(ozz::span<const ozz::math::SoaTransform> local) {
        ozz::animation::LocalToModelJob job;job.skeleton=skeleton.get();job.input=local;job.output=ozz::make_span(models);
        if(!job.Run())throw std::runtime_error("Ozz hierarchy evaluation failed");
        auto next=palette;
        for(size_t i=0;i<source->joints.size();++i) {
            ozz::math::Float4x4 inverse;
            for(size_t c=0;c<4;++c)inverse.cols[c]=ozz::math::simd_float4::LoadPtrU(source->inverseBind[i].data()+c*4);
            const auto matrix=models[jointMap[source->joints[i]]]*inverse;
            for(size_t c=0;c<4;++c)ozz::math::StorePtrU(matrix.cols[c],next[i].data()+c*4);
        }
        palette=std::move(next);
    }
    void sampleClip(size_t index,double seconds,ozz::animation::SamplingJob::Context& context,ozz::vector<ozz::math::SoaTransform>& output) {
        ozz::animation::SamplingJob job;job.animation=clips.at(index).get();job.context=&context;
        job.ratio=float(std::fmod(seconds,double(job.animation->duration()))/job.animation->duration());job.output=ozz::make_span(output);
        if(!job.Run())throw std::runtime_error("Ozz animation sampling failed");
    }
};
AnimationPlayer::AnimationPlayer(std::shared_ptr<const ModelData> data):impl_(std::make_unique<Impl>(std::move(data))) {rest();}
AnimationPlayer::~AnimationPlayer()=default;
const std::vector<SkinMatrix>& AnimationPlayer::rest() {impl_->matrices(impl_->skeleton->joint_rest_poses());return impl_->palette;}
const std::vector<SkinMatrix>& AnimationPlayer::sample(size_t clip,double seconds,size_t blendClip,float weight) {
    auto& p=*impl_;
    if(!std::isfinite(seconds)||seconds<0||!std::isfinite(weight)||weight<0||weight>1)throw std::invalid_argument("Invalid animation time/blend");
    if(p.clips.empty()) {if(clip!=0)throw std::out_of_range("Static model has no clip");return rest();}
    if(clip>=p.clips.size()||(blendClip!=std::numeric_limits<size_t>::max()&&blendClip>=p.clips.size()))throw std::out_of_range("Unknown animation clip");
    p.sampleClip(clip,seconds,p.contextA,p.a);
    if(blendClip==std::numeric_limits<size_t>::max()||weight==0) p.matrices(ozz::make_span(p.a));
    else {
        p.sampleClip(blendClip,seconds,p.contextB,p.b);
        ozz::animation::BlendingJob::Layer layers[2];layers[0].weight=1-weight;layers[0].transform=ozz::make_span(p.a);layers[1].weight=weight;layers[1].transform=ozz::make_span(p.b);
        ozz::animation::BlendingJob blend;blend.layers=ozz::make_span(layers);blend.rest_pose=p.skeleton->joint_rest_poses();blend.output=ozz::make_span(p.blended);
        if(!blend.Run())throw std::runtime_error("Ozz pose blending failed");p.matrices(ozz::make_span(p.blended));
    }
    return p.palette;
}
std::vector<SkinVertex> skinVertices(const ModelData& model,const std::vector<SkinMatrix>& palette) {
    if(palette.size()!=model.joints.size())throw std::invalid_argument("Skin palette size mismatch");
    auto output=model.vertices;
    for(auto& v:output) {
        std::array<float,3> p{},n{},t{};
        for(size_t k=0;k<4;++k) {const auto& m=palette.at(v.joints[k]);for(size_t i=0;i<3;++i) {
            p[i]+=v.weights[k]*(m[i]*v.position[0]+m[4+i]*v.position[1]+m[8+i]*v.position[2]+m[12+i]);
            n[i]+=v.weights[k]*(m[i]*v.normal[0]+m[4+i]*v.normal[1]+m[8+i]*v.normal[2]);
            t[i]+=v.weights[k]*(m[i]*v.tangent[0]+m[4+i]*v.tangent[1]+m[8+i]*v.tangent[2]);
        }}
        auto normalize=[](auto& v) {float len=std::sqrt(v[0]*v[0]+v[1]*v[1]+v[2]*v[2]);if(len<1e-6f)throw std::runtime_error("Degenerate skinned direction");for(auto& x:v)x/=len;};
        normalize(n);float dot=0;for(size_t i=0;i<3;++i)dot+=t[i]*n[i];for(size_t i=0;i<3;++i)t[i]-=dot*n[i];normalize(t);
        v.position=p;v.normal=n;for(size_t i=0;i<3;++i)v.tangent[i]=t[i];
    }
    return output;
}
}

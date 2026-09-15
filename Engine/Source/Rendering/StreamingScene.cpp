#include "Rendering/StreamingScene.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
namespace engine {
namespace {
RegionKey collisionKey(Region region){return {region.x,region.z};}
std::string collisionOwner(Region region){return "physics:region:"+std::to_string(region.x)+":"+std::to_string(region.z);}
}
StreamingScene::StreamingScene(CalibrationRuntime& runtime,TerrainRecipe terrain,RockRecipe rock,PlacementConstraints constraints,WorldDeltas deltas,FrameTelemetry* telemetry):runtime_(runtime),telemetry_(telemetry),collisionJobs_([](const PreparedCollision& prepared){return prepared.bytes();}){
    stream_=std::make_unique<RegionStream>(terrain,rock,std::move(constraints),[this](const RegionContent& content){
        const auto region=content.terrain.region;const auto offset=WorldPosition{region,{}}.relativeTo({},256,4096);
        uint64_t contentRevision=1;
        if(stream_){const auto slot=stream_->slots().find(collisionKey(region));if(slot!=stream_->slots().end())contentRevision=slot->second.contentRevision+1;}
        Resident prepared;
        auto prepare=[&]{if(content.terrain.id.generatorVersion>=2){for(uint32_t level=0;level<3;++level)prepared.terrain[level]=std::make_unique<RenderModel>(terrainRenderLod(content.terrain,level));}
                        else prepared.terrain[0]=std::make_unique<RenderModel>(content.terrain.mesh);};
        if(telemetry_)telemetry_->measure(FramePhase::StreamingPrepare,prepare);else prepare();
        const auto existing=residents_.find(collisionKey(region));
        if(collisionEpoch_==UINT64_MAX)throw std::overflow_error("Physics preparation epoch exhausted");
        const size_t bytes=content.collision.size()*sizeof(PhysicsVector);
        const auto reservation=std::max<size_t>(bytes*2,64*1024);
        auto vertices=content.collision;
        const auto ticket=collisionJobs_.submit(collisionOwner(region),++collisionEpoch_,reservation,[this,region,contentRevision,vertices=std::move(vertices)](const auto& token){
            if(token.cancelled())throw std::runtime_error("Physics preparation cancelled");
            return PreparedCollision{region,contentRevision,runtime_.physics().prepareMesh(vertices)};
        });
        if(!ticket)throw std::runtime_error("Physics preparation queue is full");
        pendingCollisions_[collisionKey(region)]={*ticket,contentRevision};
        if(existing!=residents_.end()){
            existing->second.terrain=std::move(prepared.terrain);return RegionReadiness{true,false};
        }
        try{residents_.emplace(collisionKey(region),std::move(prepared));}
        catch(...){collisionJobs_.cancel(collisionOwner(region));pendingCollisions_.erase(collisionKey(region));throw;}
        return RegionReadiness{true,false};
    },[this](Region region){retire(region);},std::move(deltas));
    for(size_t variant=0;variant<4;++variant)for(const auto& lod:stream_->rocks()[variant].lods)rockModels_[variant].push_back(std::make_unique<RenderModel>(lod.mesh));
    runtime_.streamingGuard([this](PhysicsVector from,PhysicsVector to){
        // Origin rebasing and region-aware player saves follow in P21/P22.
        if(std::abs(to[0])>3800||std::abs(to[2])>3800)return false;
        return stream_->collisionReady({{},{from[0],from[1],from[2]}},{{},{to[0],to[1],to[2]}},1.f);
    });
}
StreamingScene::~StreamingScene(){
    // Teardown must only disconnect and release. Recreating the calibration
    // floor is a separate fallible transition after streamed bodies retire.
    runtime_.disconnectStreamingGuard();
    collisionJobs_.shutdown();
    stream_.reset();
    residents_.clear();
}
void StreamingScene::retire(Region region){collisionJobs_.cancel(collisionOwner(region));pendingCollisions_.erase(collisionKey(region));auto it=residents_.find(collisionKey(region));if(it!=residents_.end()){if(it->second.collider.owner)runtime_.physics().remove(it->second.collider);residents_.erase(it);}}
void StreamingScene::adoptCollisions(){
    UploadAdmission budget{16*1024*1024,64*1024*1024};
    while(auto completed=collisionJobs_.takeReady(budget)){
        const auto pending=std::find_if(pendingCollisions_.begin(),pendingCollisions_.end(),[&](const auto& entry){return entry.second.ticket==completed->ticket;});
        if(pending==pendingCollisions_.end())continue;
        const Region region{pending->first.first,pending->first.second};const auto key=pending->first;const auto expectedRevision=pending->second.contentRevision;
        pendingCollisions_.erase(pending);
        if(!completed->error.empty()||!completed->value){stream_->markCollisionError(region,completed->error.empty()?"Physics preparation returned no shape":completed->error);continue;}
        const auto slot=stream_->slots().find(key);if(slot==stream_->slots().end()||!slot->second.content||
            slot->second.contentRevision!=completed->value->contentRevision||expectedRevision!=completed->value->contentRevision)continue;
        try{
            auto resident=residents_.find(key);if(resident==residents_.end())continue;
            auto prepared=std::move(completed->value->shape);
            if(resident->second.collider.owner){
                auto replace=[&]{runtime_.physics().replaceMeshPrepared(resident->second.collider,prepared);};
                if(telemetry_)telemetry_->measure(FramePhase::Physics,replace);else replace();
            }else{
                const auto offset=WorldPosition{region,{}}.relativeTo({},256,4096);BodyToken collider;
                auto create=[&]{collider=runtime_.physics().meshPrepared(slot->second.content->terrain.id.text(),offset,prepared);};
                if(telemetry_)telemetry_->measure(FramePhase::Physics,create);else create();
                resident->second.collider=collider;
            }
            stream_->markCollisionReady(region);
        }catch(const std::exception& error){stream_->markCollisionError(region,error.what());}
    }
}
bool StreamingScene::removeNearest(float maximumDistance){
    if(!std::isfinite(maximumDistance)||maximumDistance<=0||maximumDistance>32)throw std::invalid_argument("Invalid rock editing range");
    std::optional<GeneratedId> chosen;float nearest=maximumDistance;const auto feet=runtime_.feet();
    for(const auto& [_,slot]:stream_->slots())if(slot.content)for(const auto& p:slot.content->terrain.rocks){if(stream_->deltas().removed(p.id.region,p.id.member))continue;const auto local=p.position.relativeTo({},256,4096);const auto distance=std::hypot(local[0]-feet[0],local[2]-feet[2]);if(distance<nearest){nearest=distance;chosen=p.id;}}
    return chosen&&stream_->removeRock(*chosen);
}
void StreamingScene::update(){const auto p=runtime_.feet();anchors({{{{},{p[0],p[1],p[2]}},0}});}
void StreamingScene::anchors(const std::vector<StreamAnchor>& a){stream_->update(a);adoptCollisions();}
bool StreamingScene::readyAt(PhysicsVector p)const{return stream_->collisionReady({{},{p[0],p[1],p[2]}},{{},{p[0],p[1],p[2]}},1);}
const std::vector<RenderInstance>& StreamingScene::instances(std::array<float,3> eye){
    instances_.clear();
    for(const auto& [key,slot]:stream_->slots()){
        auto it=residents_.find(key);if(!slot.ready.render||it==residents_.end())continue;
        const auto offset=WorldPosition{slot.region,{}}.relativeTo({},256,4096);
        // Use distance to the patch edge, keeping near ground at full collision resolution.
        const float dx=std::max(0.f,std::abs(eye[0]-(offset[0]+128))-128),dz=std::max(0.f,std::abs(eye[2]-(offset[2]+128))-128);
        const float distance=std::hypot(dx,dz);const uint32_t level=slot.content->terrain.id.generatorVersion==1?0:(distance<96?0:(distance<240?1:2));
        instances_.push_back({it->second.terrain[level].get(),offset,{offset[0]+128,0,offset[2]+128},0,200,true});
        for(const auto& rock:slot.content->terrain.rocks){
            if(!rock.members.empty()){
                const auto group=rock.position.relativeTo({},256,4096);
                for(const auto& m:rock.members){const auto& asset=stream_->rocks()[m.variant];const auto& shape=asset.lods[0];const float height=shape.maximum[1]*m.axes[1]*m.scale;
                    const std::array<float,3> pos{group[0]+m.offset[0],m.offset[1],group[2]+m.offset[2]};const float d=std::hypot(std::hypot(pos[0]-eye[0],pos[2]-eye[2]),pos[1]-eye[1]);
                    instances_.push_back({rockModels_[m.variant][rockLod(asset,d)].get(),pos,{pos[0],pos[1]+height*.5f,pos[2]},m.yaw,std::hypot(shape.footprintRadius*std::max(m.axes[0],m.axes[2])*m.scale,height*.5f),false,{m.axes[0]*m.scale,m.axes[1]*m.scale,m.axes[2]*m.scale}});
                }continue;
            }
            const auto position=rock.position.relativeTo({},256,4096);const float distance=std::hypot(std::hypot(position[0]-eye[0],position[2]-eye[2]),position[1]-eye[1]);const auto variant=rock.id.member%4;
            const auto& asset=stream_->rocks()[variant];const auto& shape=asset.lods[0];const float height=shape.maximum[1];const float radius=std::hypot(shape.footprintRadius,height*.5f);
            instances_.push_back({rockModels_[variant][rockLod(asset,distance)].get(),position,{position[0],position[1]+height*.5f,position[2]},rock.yaw*.01745329252f,radius,false});
        }
    }return instances_;
}
}

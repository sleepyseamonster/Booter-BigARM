#include "Rendering/StreamingScene.h"
#include <cmath>
namespace engine {
StreamingScene::StreamingScene(CalibrationRuntime& runtime,TerrainRecipe terrain,RockRecipe rock,PlacementConstraints constraints):runtime_(runtime){
    stream_=std::make_unique<RegionStream>(terrain,rock,std::move(constraints),[this](const RegionContent& content){
        const auto region=content.terrain.region;const auto offset=WorldPosition{region,{}}.relativeTo({},256,4096);
        Resident prepared;prepared.terrain=std::make_unique<RenderModel>(content.terrain.mesh);
        const auto collider=runtime_.physics().mesh(content.terrain.id.text(),offset,content.collision);
        prepared.collider=collider;
        try{residents_.emplace(RegionKey{region.x,region.z},std::move(prepared));}catch(...){runtime_.physics().remove(collider);throw;}
        return RegionReadiness{true,true};
    },[this](Region region){retire(region);});
    for(size_t variant=0;variant<4;++variant)for(const auto& lod:stream_->rocks()[variant].lods)rockModels_[variant].push_back(std::make_unique<RenderModel>(lod.mesh));
    runtime_.streamingGuard([this](PhysicsVector from,PhysicsVector to){
        // Origin rebasing and region-aware player saves follow in P21/P22.
        if(std::abs(to[0])>3800||std::abs(to[2])>3800)return false;
        return stream_->collisionReady({{},{from[0],from[1],from[2]}},{{},{to[0],to[1],to[2]}},1.f);
    });
}
StreamingScene::~StreamingScene(){runtime_.streamingGuard({});stream_.reset();}
void StreamingScene::retire(Region region){auto it=residents_.find({region.x,region.z});if(it!=residents_.end()){runtime_.physics().remove(it->second.collider);residents_.erase(it);}}
void StreamingScene::update(){const auto p=runtime_.feet();anchors({{{{},{p[0],p[1],p[2]}},0}});}
void StreamingScene::anchors(const std::vector<StreamAnchor>& a){stream_->update(a);}
bool StreamingScene::readyAt(PhysicsVector p)const{return stream_->collisionReady({{},{p[0],p[1],p[2]}},{{},{p[0],p[1],p[2]}},1);}
const std::vector<RenderInstance>& StreamingScene::instances(std::array<float,3> eye){
    instances_.clear();
    for(const auto& [key,slot]:stream_->slots()){
        auto it=residents_.find(key);if(!slot.ready.render||it==residents_.end())continue;
        const auto offset=WorldPosition{slot.region,{}}.relativeTo({},256,4096);
        instances_.push_back({it->second.terrain.get(),offset,{offset[0]+128,0,offset[2]+128},0,183,true});
        for(const auto& rock:slot.content->terrain.rocks){const auto position=rock.position.relativeTo({},256,4096);const float distance=std::hypot(std::hypot(position[0]-eye[0],position[2]-eye[2]),position[1]-eye[1]);const auto variant=rock.id.member%4;
            const auto& asset=stream_->rocks()[variant];const auto& shape=asset.lods[0];const float height=shape.maximum[1];const float radius=std::hypot(shape.footprintRadius,height*.5f);
            instances_.push_back({rockModels_[variant][rockLod(asset,distance)].get(),position,{position[0],position[1]+height*.5f,position[2]},rock.yaw*.01745329252f,radius,false});
        }
    }return instances_;
}
}

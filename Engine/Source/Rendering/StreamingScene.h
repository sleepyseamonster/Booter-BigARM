#pragma once
#include "Rendering/Renderer.h"
#include "Game/CalibrationRuntime.h"
#include "World/Streaming/RegionStream.h"
namespace engine {
// Main-thread adapter; runtime outlives this object, and this object dies before Renderer::stop.
class StreamingScene {
public:
    StreamingScene(CalibrationRuntime&,TerrainRecipe,RockRecipe,PlacementConstraints,WorldDeltas={},FrameTelemetry* telemetry=nullptr);
    ~StreamingScene();
    void update();
    bool removeNearest(float maximumDistance=8);
    const WorldDeltas& deltas()const{return stream_->deltas();}
    void anchors(const std::vector<StreamAnchor>&);
    const std::vector<RenderInstance>& instances(std::array<float,3> eye);
    const RegionStream& stream()const{return *stream_;}
    bool readyAt(PhysicsVector p)const;
private:
    struct Resident {std::array<std::unique_ptr<RenderModel>,3> terrain;BodyToken collider;};
    struct PreparedCollision {Region region;PreparedMesh shape;size_t bytes()const{return shape.bytes;}};
    void retire(Region);
    void adoptCollisions();
    CalibrationRuntime& runtime_;
    FrameTelemetry* telemetry_=nullptr;
    std::map<RegionKey,Resident> residents_;
    std::array<std::vector<std::unique_ptr<RenderModel>>,4> rockModels_;
    BoundedJobs<PreparedCollision> collisionJobs_;
    uint64_t collisionEpoch_=0;
    std::map<RegionKey,uint64_t> pendingCollisions_;
    std::unique_ptr<RegionStream> stream_;
    std::vector<RenderInstance> instances_;
};
}

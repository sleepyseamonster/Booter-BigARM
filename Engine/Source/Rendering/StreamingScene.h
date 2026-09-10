#pragma once
#include "Rendering/Renderer.h"
#include "Game/CalibrationRuntime.h"
#include "World/Streaming/RegionStream.h"
namespace engine {
// Main-thread adapter; runtime outlives this object, and this object dies before Renderer::stop.
class StreamingScene {
public:
    StreamingScene(CalibrationRuntime&,TerrainRecipe,RockRecipe,PlacementConstraints);
    ~StreamingScene();
    void update();
    void anchors(const std::vector<StreamAnchor>&);
    const std::vector<RenderInstance>& instances(std::array<float,3> eye);
    const RegionStream& stream()const{return *stream_;}
    bool readyAt(PhysicsVector p)const;
private:
    struct Resident {std::unique_ptr<RenderModel> terrain;BodyToken collider;};
    void retire(Region);
    CalibrationRuntime& runtime_;
    std::map<RegionKey,Resident> residents_;
    std::array<std::vector<std::unique_ptr<RenderModel>>,4> rockModels_;
    std::unique_ptr<RegionStream> stream_;
    std::vector<RenderInstance> instances_;
};
}

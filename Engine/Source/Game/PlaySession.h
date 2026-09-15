#pragma once
#include "Authoring/SceneDocument.h"
#include "Game/CalibrationRuntime.h"
#include "Persistence/WorldSave.h"

namespace engine {

enum class PlaySessionState : uint8_t { Starting,Running,Paused,Stopping,Stopped,Failed };

struct PlaySessionStart {
    std::shared_ptr<const AuthoringSceneDocument> source;
    PlayerSnapshot player;
    std::shared_ptr<const ModelData> model;
};

struct PlaySessionInspection {
    PlaySessionState state=PlaySessionState::Stopped;
    uint64_t epoch=0,sourceRevision=0,ticks=0;
    bool focused=false,inputEnabled=false;
    PhysicsVector feet{};
    size_t physicsBodies=0;
    std::string sourceChecksum;
    std::string failure;
};

// Shared by standalone Player and the Workbench Game View. Runtime state is
// isolated from the immutable authoring source and is discarded by stop.
class PlaySession {
public:
    PlaySession()=default;
    ~PlaySession(){stop();}
    void start(PlaySessionStart);
    void restart(PlaySessionStart start){stop();this->start(std::move(start));}
    void stop()noexcept;
    void pause(uint64_t epoch,bool paused);
    void focus(uint64_t epoch,bool focused);
    void advance(uint64_t epoch,double seconds,float& cameraYaw,float& cameraPitch);
    CalibrationFrame present(uint64_t epoch,float cameraYaw,float cameraPitch,float distance,float seconds);
    BodyToken addStaticBox(uint64_t epoch,std::string id,PhysicsVector center,PhysicsVector halfExtent);
    BodyToken addDynamicCapsule(uint64_t epoch,std::string id,PhysicsVector center,float radius,float halfCylinder);
    WorldSave capture(uint64_t epoch,const WorldConfiguration&,Region origin,const WorldDeltas&,
                      float cameraYaw,float cameraPitch,float cameraDistance)const;
    CalibrationRuntime& runtime(uint64_t epoch);
    const CalibrationRuntime& runtime(uint64_t epoch)const;
    Actions& actions(){return actions_;}
    PlaySessionInspection inspect()const;
    PlaySessionState state()const noexcept{return state_;}
    uint64_t epoch()const noexcept{return epoch_;}
private:
    void requireEpoch(uint64_t)const;
    PlaySessionState state_=PlaySessionState::Stopped;
    uint64_t epoch_=0,sourceRevision_=0;
    bool focused_=false,manuallyPaused_=false;
    std::shared_ptr<const AuthoringSceneDocument> source_;
    std::string sourceChecksum_,failure_;
    std::unique_ptr<CalibrationRuntime> runtime_;
    Actions actions_;
};

}

#include "Game/PlaySession.h"
#include "Assets/TextureData.h"

namespace engine {
namespace {
std::string checksum(const AuthoringSceneDocument& scene){const auto wire=scene.toJson().dump();const std::vector<uint8_t> bytes(wire.begin(),wire.end());return std::to_string(textureCrc(bytes));}
}

void PlaySession::start(PlaySessionStart start){
    if(state_!=PlaySessionState::Stopped&&state_!=PlaySessionState::Failed)throw std::logic_error("Play session is already active");
    if(!start.source)throw std::invalid_argument("Play session requires an immutable source scene");
    if(epoch_==UINT64_MAX)throw std::overflow_error("Play session epoch exhausted");
    state_=PlaySessionState::Starting;failure_.clear();focused_=false;manuallyPaused_=false;
    try{
        validateSnapshot(start.player);
        for(size_t i=0;i<start.source->entityCount();++i){const auto& entity=start.source->entityAt(i);start.source->worldMatrix(entity.id);}
        sourceRevision_=start.source->version();sourceChecksum_=checksum(*start.source);source_=std::move(start.source);
        runtime_=std::make_unique<CalibrationRuntime>(std::move(start.model),start.player);
        ++epoch_;actions_.gameplay(true);actions_.focus(false);state_=PlaySessionState::Paused;
    }catch(const std::exception& error){runtime_.reset();source_.reset();failure_=error.what();state_=PlaySessionState::Failed;throw;}
    catch(...){runtime_.reset();source_.reset();failure_="Unknown play start failure";state_=PlaySessionState::Failed;throw;}
}

void PlaySession::stop()noexcept{
    if(state_==PlaySessionState::Stopped)return;
    state_=PlaySessionState::Stopping;actions_.focus(false);actions_.gameplay(false);
    runtime_.reset();source_.reset();sourceChecksum_.clear();sourceRevision_=0;focused_=false;manuallyPaused_=false;state_=PlaySessionState::Stopped;
}

void PlaySession::requireEpoch(uint64_t epoch)const{
    if(!epoch||epoch!=epoch_)throw std::runtime_error("Stale play session epoch");
    if((state_!=PlaySessionState::Running&&state_!=PlaySessionState::Paused)||!runtime_)throw std::logic_error("Play session is not running");
}
void PlaySession::pause(uint64_t epoch,bool paused){requireEpoch(epoch);manuallyPaused_=paused;state_=(paused||!focused_)?PlaySessionState::Paused:PlaySessionState::Running;}
void PlaySession::focus(uint64_t epoch,bool focused){requireEpoch(epoch);focused_=focused;actions_.focus(focused);state_=(!focused||manuallyPaused_)?PlaySessionState::Paused:PlaySessionState::Running;}
void PlaySession::advance(uint64_t epoch,double seconds,float& yaw,float& pitch){requireEpoch(epoch);runtime_->advance(seconds,state_==PlaySessionState::Paused,actions_,yaw,pitch);}
CalibrationFrame PlaySession::present(uint64_t epoch,float yaw,float pitch,float distance,float seconds){requireEpoch(epoch);return runtime_->present(yaw,pitch,distance,seconds);}
BodyToken PlaySession::addStaticBox(uint64_t epoch,std::string id,PhysicsVector center,PhysicsVector half){requireEpoch(epoch);return runtime_->physics().box(std::move(id),center,half);}
BodyToken PlaySession::addDynamicCapsule(uint64_t epoch,std::string id,PhysicsVector center,float radius,float half){requireEpoch(epoch);return runtime_->physics().capsule(std::move(id),center,radius,half,true);}
CalibrationRuntime& PlaySession::runtime(uint64_t epoch){requireEpoch(epoch);return *runtime_;}
const CalibrationRuntime& PlaySession::runtime(uint64_t epoch)const{requireEpoch(epoch);return *runtime_;}
WorldSave PlaySession::capture(uint64_t epoch,const WorldConfiguration& configuration,Region origin,const WorldDeltas& deltas,float yaw,float pitch,float distance)const{
    requireEpoch(epoch);validateDeltas(deltas);return {configuration,runtime_->snapshot(yaw,pitch,distance),origin,deltas};
}
PlaySessionInspection PlaySession::inspect()const{
    PlaySessionInspection result{state_,epoch_,sourceRevision_,runtime_?runtime_->clock().ticks():0,focused_,state_==PlaySessionState::Running&&focused_,{},runtime_?runtime_->physics().size():0,sourceChecksum_,failure_};
    if(runtime_)result.feet=runtime_->feet();return result;
}

}

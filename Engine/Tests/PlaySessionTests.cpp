#include "Game/PlaySession.h"
#include "Persistence/DurabilityService.h"
#include <chrono>
#include <cmath>
#include <condition_variable>
#include <fstream>
#include <iostream>
#include <mutex>
#include <thread>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F&& fn){bool rejected=false;try{fn();}catch(const std::exception&){rejected=true;}require(rejected,"Expected rejection");}
DurableReceipt await(DurabilityService& service,uint64_t id){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(10);
    for(;;){auto receipt=service.receipt(id);if(receipt.terminal())return receipt;if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Durability receipt timed out");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
}
}

int main(int argc,char** argv)try{
    if(argc!=2)throw std::runtime_error("Expected test output directory");
    const std::filesystem::path root=argv[1];std::error_code ignored;std::filesystem::remove_all(root,ignored);std::filesystem::create_directories(root);
    auto authored=std::make_shared<AuthoringSceneDocument>("play-source");authored->createEntity("Immutable authored object");
    const auto source=std::make_shared<const AuthoringSceneDocument>(authored->readSnapshot());const auto sourceWire=source->toJson().dump();
    PlaySession play;play.start({source,{},{}});const auto epoch=play.epoch();require(play.inspect().state==PlaySessionState::Paused&&play.inspect().sourceRevision==source->version()&&!play.inspect().inputEnabled,"Play start identity and initial input ownership");
    play.focus(epoch,true);require(play.inspect().state==PlaySessionState::Running&&play.inspect().inputEnabled,"Focused play did not enter running input state");play.actions().set(Control::A,1);float yaw=0,pitch=.3f;
    play.advance(epoch,FixedClock::step*1.5,yaw,pitch);const auto beforePause=play.present(epoch,yaw,pitch,5,0);
    const auto authoritative=play.runtime(epoch).feet();require(authoritative[0]<beforePause.feet[0]&&beforePause.feet[0]<0,"Fractional presentation fixture");
    play.pause(epoch,true);play.advance(epoch,5,yaw,pitch);const auto paused=play.present(epoch,yaw,pitch,5,0);
    require(paused.feet==beforePause.feet&&play.runtime(epoch).feet()==authoritative,"Pause preserves fractional presentation and authoritative pose");
    play.pause(epoch,false);play.advance(epoch,0,yaw,pitch);require(play.present(epoch,yaw,pitch,5,0).feet==paused.feet,"Resume does not accumulate paused wall time");
    play.focus(epoch,false);const auto ticks=play.inspect().ticks;play.advance(epoch,1,yaw,pitch);require(play.inspect().ticks==ticks&&!play.inspect().inputEnabled,"Focus suspension inhibits input and time");
    require(source->toJson().dump()==sourceWire,"Play mutated authoring source");
    const auto firstFeet=play.runtime(epoch).feet();play.stop();require(play.state()==PlaySessionState::Stopped,"Play stop state");rejects([&]{play.runtime(epoch);});
    play.start({source,{},{}});require(play.epoch()!=epoch&&play.runtime(play.epoch()).feet()!=firstFeet,"Restart gets a fresh epoch and discards runtime state");rejects([&]{play.focus(epoch,true);});play.stop();
    PlayerSnapshot invalid;invalid.feet[0]=INFINITY;rejects([&]{play.start({source,invalid,{}});});require(play.state()==PlaySessionState::Failed,"Failed start did not publish failed state");play.stop();

    // Release-only support disconnect cannot allocate at physics capacity.
    CalibrationRuntime capacity;capacity.streamingGuard([](PhysicsVector,PhysicsVector){return true;});
    for(uint32_t i=0;;++i){try{capacity.physics().box("authored:capacity:"+std::to_string(i),{float(i%40),-10,float((i/40)%40)},{.01f,.01f,.01f});}catch(const std::exception&){break;}}
    const auto full=capacity.physics().size();capacity.disconnectStreamingGuard();require(capacity.physics().size()==full,"Release-only streaming disconnect allocated or removed bodies");

    WorldConfiguration configuration;WorldSave save{configuration,{}, {}, {}};
    std::mutex gateMutex;std::condition_variable gateWake;bool entered=false,release=false;
    DurabilityService durability({[&](uint64_t){std::unique_lock lock(gateMutex);entered=true;gateWake.notify_all();gateWake.wait(lock,[&]{return release;});},{}});
    const auto saveId=durability.save(root/"profile",save,0,7,source->version(),true);
    {std::unique_lock lock(gateMutex);gateWake.wait(lock,[&]{return entered;});}
    require(durability.receipt(saveId).state==DurableState::Writing,"Slow save did not leave frame-thread-visible writing receipt");
    {std::lock_guard lock(gateMutex);release=true;}gateWake.notify_all();
    const auto saved=await(durability,saveId);require(saved.state==DurableState::Durable&&saved.diskGeneration==1,"Explicit save durable completion");
    const auto loadId=durability.load(root/"profile",configuration,7,source->version());require(await(durability,loadId).state==DurableState::Loaded,"Asynchronous load completion");
    rejects([&]{durability.takeLoaded(loadId,8,source->version());});
    auto loaded=durability.takeLoaded(loadId,7,source->version());require(loaded&&loaded->value&&loaded->generation==1,"Revision-bound load adoption");
    const auto staleId=durability.save(root/"profile",save,0,7,source->version(),true);require(await(durability,staleId).state==DurableState::Rejected,"Stale disk generation was not rejected");
    std::ofstream blocker(root/"not-a-directory");blocker<<"x";blocker.close();
    const auto failedId=durability.save(root/"not-a-directory",save,0,7,source->version(),true);require(await(durability,failedId).state==DurableState::Rejected,"Permanent disk failure not surfaced");
    durability.shutdown();
    std::mutex coalesceMutex;std::condition_variable coalesceWake;bool loadEntered=false,loadRelease=false;
    DurabilityService coalescing({{},[&](uint64_t){std::unique_lock lock(coalesceMutex);loadEntered=true;coalesceWake.notify_all();coalesceWake.wait(lock,[&]{return loadRelease;});}});
    const auto blockingLoad=coalescing.load(root/"profile",configuration,9,source->version());
    {std::unique_lock lock(coalesceMutex);coalesceWake.wait(lock,[&]{return loadEntered;});}
    const auto oldAuto=coalescing.save(root/"autosave",save,0,9,source->version(),false);
    const auto newAuto=coalescing.save(root/"autosave",save,0,9,source->version(),false);
    require(coalescing.receipt(oldAuto).state==DurableState::Superseded,"Superseded autosave receipt was not observable");
    {std::lock_guard lock(coalesceMutex);loadRelease=true;}coalesceWake.notify_all();require(await(coalescing,blockingLoad).state==DurableState::Loaded,"Blocking load completion");
    require(await(coalescing,newAuto).state==DurableState::Durable,"Latest autosave did not become durable");
    const auto shutdownSave=coalescing.save(root/"shutdown-save",save,0,9,source->version(),true);coalescing.shutdown();
    require(coalescing.receipt(shutdownSave).state==DurableState::Durable,"Shutdown did not flush pending explicit save");
    require(source->toJson().dump()==sourceWire,"Durability path mutated authoring source");
    std::cout<<"PASS: isolated play lifecycle, fractional pause/focus, fresh epochs, release-only capacity stop, asynchronous durable save/load, autosave coalescing and shutdown flush\n";return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

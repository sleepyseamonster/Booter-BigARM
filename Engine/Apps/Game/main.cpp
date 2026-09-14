#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Platform/Window.h"
#include "Platform/ActionInput.h"
#include "Rendering/Renderer.h"
#include "Rendering/TextureStore.h"
#include "Rendering/StreamingScene.h"
#include "Game/CalibrationRuntime.h"
#include "Game/WorldSession.h"
#include "Audio/Audio.h"
#include "Persistence/Document.h"
#include <chrono>
#include <iostream>
namespace {
struct SDLSession {
    SDLSession(){SDL_SetMainReady();if(!SDL_Init(SDL_INIT_VIDEO|SDL_INIT_EVENTS|SDL_INIT_GAMEPAD))throw std::runtime_error(SDL_GetError());}
    ~SDLSession(){SDL_Quit();}
};
int run(int argc,char** argv) {
    std::filesystem::path model,shaders,capture,profile,worldProfile,terrain,streamRock,constraints;bool silent=false;
    for(int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if(arg=="--silent")silent=true;
        else if((arg=="--world-profile"||arg=="--terrain"||arg=="--stream-rock"||arg=="--constraints"||arg=="--model"||arg=="--shaders"||arg=="--capture"||arg=="--profile")&&i+1<argc) {
            if(arg=="--world-profile")worldProfile=argv[++i];else if(arg=="--terrain")terrain=argv[++i];else if(arg=="--stream-rock")streamRock=argv[++i];else if(arg=="--constraints")constraints=argv[++i];else if(arg=="--model")model=argv[++i];else if(arg=="--shaders")shaders=argv[++i];else if(arg=="--profile")profile=argv[++i];else capture=argv[++i];
        } else throw std::runtime_error("Usage: engine_player --model model.json [--shaders directory] [--silent] [--profile calibration-directory] [--world-profile world-directory] [--terrain recipe.json] [--stream-rock recipe.json] [--constraints file.json] [--capture new-directory]");
    }
    if(terrain.empty()&&(!worldProfile.empty()||!streamRock.empty()||!constraints.empty()))throw std::runtime_error("World options require --terrain");
    if(!terrain.empty()&&!profile.empty())throw std::runtime_error("Use --world-profile with --terrain; --profile is for calibration snapshots");
    const bool technical=!capture.empty();
    if(technical){if(std::filesystem::exists(capture))throw std::runtime_error("Capture directory already exists");std::filesystem::create_directories(capture);}
    SDLSession session;const auto base=std::filesystem::path(SDL_GetBasePath());
    if(shaders.empty())shaders=base/"Shaders";
    if(model.empty())model=base/"Assets/Models/Calibration/model.json";
    std::unique_ptr<engine::WorldSession> worldSession;
    if(!terrain.empty()) {
        if(worldProfile.empty()&&!technical)worldProfile=base/"UserData/World";
        worldSession=std::make_unique<engine::WorldSession>(worldProfile,engine::WorldConfiguration{engine::loadTerrainRecipe(terrain),streamRock.empty()?engine::RockRecipe{}:engine::loadRockRecipe(streamRock),constraints.empty()?engine::PlacementConstraints{}:engine::loadConstraints(constraints)});
    } else if(profile.empty()&&!technical)profile=base/"UserData";
    const auto loaded=profile.empty()?engine::SnapshotRead{}:engine::loadSnapshot(profile);
    const auto initial=worldSession?worldSession->initial().player:loaded.value.value_or(engine::PlayerSnapshot{});
    const auto data=std::make_shared<engine::ModelData>(engine::loadModel(model));
    engine::Window window(technical);SDL_SetWindowTitle(window.get(),"Booter & BigARM | Player skeleton");
    engine::Renderer renderer;renderer.start(window,shaders);
    const std::string backend=renderer.name();
    {
        engine::RenderModel mesh(*data);
        engine::TextureStore textures;
        auto albedo=textures.fallback(engine::TextureRole::Color),normal=textures.fallback(engine::TextureRole::Normal),surface=textures.fallback(engine::TextureRole::Surface);
        engine::SceneSurfaces surfaces;surfaces.rock={false,textures.resolve(albedo.token()),textures.resolve(normal.token()),textures.resolve(surface.token())};surfaces.ground=surfaces.rock;
        engine::CalibrationRuntime runtime(data,initial);
        std::unique_ptr<engine::StreamingScene> streaming;
        if(worldSession){const auto& saved=worldSession->initial();streaming=std::make_unique<engine::StreamingScene>(runtime,saved.configuration.terrain,saved.configuration.rock,saved.configuration.constraints,saved.deltas);}
        engine::Actions actions;engine::ActionInput input(actions);
        std::unique_ptr<engine::Audio> audio;
        if(!silent&&!technical)try{audio=std::make_unique<engine::Audio>();}catch(const std::exception& error){std::cerr<<error.what()<<"; continuing silently\n";}
        engine::FixtureState state;state.distance=initial.cameraDistance;state.pitch=initial.cameraPitch;state.yaw=initial.cameraYaw;state.surfaceTextures=false;
        uint64_t saveAttemptTick=initial.ticks;std::string saveStatus=worldSession?(worldSession->recovered()?"Recovered complete world save":(worldSession->restored()?"World restored":"New world")):loaded.recovered?"Recovered last valid snapshot":(loaded.value?"Snapshot restored":"New session");
        auto save=[&] {
            if(technical)return;
            saveAttemptTick=runtime.clock().ticks();
            try{
                if(worldSession)worldSession->save(runtime,state.yaw,state.pitch,state.distance,streaming->deltas());
                else engine::saveSnapshot(profile,runtime.snapshot(state.yaw,state.pitch,state.distance));
                saveStatus=worldSession?"World saved (generation "+std::to_string(worldSession->generation())+")":"Saved";
            }
            catch(const std::exception& error){saveStatus=error.what();std::cerr<<"Save failed: "<<error.what()<<'\n';}
        };
        bool running=true,paused=false;auto last=std::chrono::steady_clock::now();
        bgfx::setDebug(BGFX_DEBUG_TEXT);
        for(unsigned frame=0;running;++frame) {
            SDL_Event event;float dx=0,dy=0,wheel=0;bool saveRequested=false;
            while(SDL_PollEvent(&event)) {
                input.event(event);
                if(event.type==SDL_EVENT_QUIT||event.type==SDL_EVENT_WINDOW_CLOSE_REQUESTED)running=false;
                if(event.type==SDL_EVENT_KEY_DOWN&&event.key.key==SDLK_ESCAPE)running=false;
                if(event.type==SDL_EVENT_KEY_DOWN&&event.key.key==SDLK_F5&&!event.key.repeat)saveRequested=true;
                if(event.type==SDL_EVENT_MOUSE_MOTION&&(event.motion.state&SDL_BUTTON_RMASK)){dx+=event.motion.xrel;dy+=event.motion.yrel;}
                if(event.type==SDL_EVENT_MOUSE_WHEEL)wheel+=event.wheel.y;
            }
            if(!running)break;
            const auto now=std::chrono::steady_clock::now();const double seconds=std::chrono::duration<double>(now-last).count();last=now;
            int width,height;window.pixels(width,height);const auto flags=SDL_GetWindowFlags(window.get());
            const bool focused=(flags&SDL_WINDOW_INPUT_FOCUS)!=0;
            actions.focus(focused);input.sample();
            if(actions.consume(engine::Action::Pause).pressed)paused=!paused;
            const bool suspended=technical||paused||!focused||(flags&SDL_WINDOW_MINIMIZED)||width<=0||height<=0;
            actions.gameplay(!suspended);
            if(!suspended){state.orbit(dx,dy,false);state.zoom(wheel,false);}
            if(streaming)renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{streaming->update();});
            renderer.telemetry.measure(engine::FramePhase::Simulation,[&]{runtime.advance(seconds,suspended,actions,state.yaw,state.pitch);});
            if(saveRequested||runtime.clock().ticks()-saveAttemptTick>=600)save();
            for(const auto& cue:runtime.takeCues())if(audio)audio->cue(cue.sequence);
            if(width<=0||height<=0||(flags&SDL_WINDOW_MINIMIZED)){SDL_Delay(16);continue;}
            renderer.resize(width,height);
            const auto view=runtime.present(state.yaw,state.pitch,state.distance,float(std::min(seconds,.1)));
            engine::ScenePlacement placement;placement.physicalCharacter=true;placement.offset=view.feet;placement.offset[1]+=.15f;
            placement.eye=view.eye;placement.target=view.target;placement.model=&mesh;placement.pose=view.palette;placement.markerActive=runtime.targetActive();state.objectYaw=view.yaw;
            if(streaming){placement.streamedWorld=true;renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{placement.instances=&streaming->instances(view.eye);});}
            renderer.telemetry.measure(engine::FramePhase::Draw,[&]{renderer.draw(state,engine::GeometryCheck::None,false,nullptr,&surfaces,&placement);});
            bgfx::dbgTextClear();bgfx::dbgTextPrintf(2,2,0x0f,"BOOTER & BIGARM - ENGINE SKELETON");
            bgfx::dbgTextPrintf(2,4,0x07,"WASD: move  Space: jump  Shift: run  Right drag: camera");
            bgfx::dbgTextPrintf(2,5,0x07,"E: interact  F5: save  P/Start: pause  Esc: exit");
            bgfx::dbgTextPrintf(2,7,0x0f,"%s",paused?"Paused":(!focused&&!technical?"Paused while window is inactive":""));
            bgfx::dbgTextPrintf(2,9,0x0b,"%s",runtime.canInteract()?"E - toggle calibration marker":"Move near the marker to interact");
            bgfx::dbgTextPrintf(2,10,0x07,"Marker: %s | Audio: %s",runtime.targetActive()?"on":"off",audio?"on":"silent");
            bgfx::dbgTextPrintf(2,12,0x07,"%s",saveStatus.c_str());
            if(streaming)bgfx::dbgTextPrintf(2,14,0x07,"%zu regions | %s",streaming->stream().slots().size(),runtime.waitingForWorld()?"Waiting for ground collision":"World ready");
            if(technical&&frame==20)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(capture/"player.png").string().c_str());
            renderer.finishFrame(technical);
            if(technical&&frame==30)running=false;
            if(technical)SDL_Delay(10);
        }
        save();
        streaming.reset();
        albedo.reset();normal.reset();surface.reset();textures.stop();
    }
    renderer.stop();
    if(technical) {
        const bool passed=renderer.callbacks.captures==1&&renderer.callbacks.errors==0;
        engine::writeDocument(capture/"player.json","engine.player-render",{{"passed",passed},{"backend",backend},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"simulation_advanced",false},{"snapshot_loaded",worldSession?worldSession->restored():loaded.value.has_value()},{"snapshot_generation",worldSession?worldSession->generation():loaded.generation},{"recovered",worldSession?worldSession->recovered():loaded.recovered},{"world_profile",bool(worldSession)},{"changed_regions",worldSession?worldSession->initial().deltas.removedRocks.size():0},{"marker_active",initial.markerActive},{"feet",initial.feet}});
        if(!passed)throw std::runtime_error("Player render capture failed");
    }
    return 0;
}
}
int main(int argc,char** argv){try{return run(argc,argv);}catch(const std::exception& error){std::cerr<<"ERROR: "<<error.what()<<'\n';return 1;}}

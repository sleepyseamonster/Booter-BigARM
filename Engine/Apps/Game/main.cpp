#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Platform/Window.h"
#include "Platform/ActionInput.h"
#include "Rendering/Renderer.h"
#include "Rendering/TextureStore.h"
#include "Game/CalibrationRuntime.h"
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
    std::filesystem::path model,shaders,capture;bool silent=false;
    for(int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if(arg=="--silent")silent=true;
        else if((arg=="--model"||arg=="--shaders"||arg=="--capture")&&i+1<argc) {
            if(arg=="--model")model=argv[++i];else if(arg=="--shaders")shaders=argv[++i];else capture=argv[++i];
        } else throw std::runtime_error("Usage: engine_player --model model.json [--shaders directory] [--silent] [--capture new-directory]");
    }
    const bool technical=!capture.empty();
    if(technical){if(std::filesystem::exists(capture))throw std::runtime_error("Capture directory already exists");std::filesystem::create_directories(capture);}
    SDLSession session;const auto base=std::filesystem::path(SDL_GetBasePath());
    if(shaders.empty())shaders=base/"Shaders";
    if(model.empty())model=base/"Assets/Models/Calibration/model.json";
    const auto data=std::make_shared<engine::ModelData>(engine::loadModel(model));
    engine::Window window(technical);SDL_SetWindowTitle(window.get(),"Booter & BigARM | Player skeleton");
    engine::Renderer renderer;renderer.start(window,shaders);
    const std::string backend=renderer.name();
    {
        engine::RenderModel mesh(*data);
        engine::TextureStore textures;
        auto albedo=textures.fallback(engine::TextureRole::Color),normal=textures.fallback(engine::TextureRole::Normal),surface=textures.fallback(engine::TextureRole::Surface);
        engine::SceneSurfaces surfaces;surfaces.rock={false,textures.resolve(albedo.token()),textures.resolve(normal.token()),textures.resolve(surface.token())};surfaces.ground=surfaces.rock;
        engine::CalibrationRuntime runtime(data);
        engine::Actions actions;engine::ActionInput input(actions);
        std::unique_ptr<engine::Audio> audio;
        if(!silent&&!technical)try{audio=std::make_unique<engine::Audio>();}catch(const std::exception& error){std::cerr<<error.what()<<"; continuing silently\n";}
        engine::FixtureState state;state.distance=5;state.pitch=.3f;state.surfaceTextures=false;
        bool running=true,paused=false;auto last=std::chrono::steady_clock::now();
        bgfx::setDebug(BGFX_DEBUG_TEXT);
        for(unsigned frame=0;running;++frame) {
            SDL_Event event;float dx=0,dy=0,wheel=0;
            while(SDL_PollEvent(&event)) {
                input.event(event);
                if(event.type==SDL_EVENT_QUIT||event.type==SDL_EVENT_WINDOW_CLOSE_REQUESTED)running=false;
                if(event.type==SDL_EVENT_KEY_DOWN&&event.key.key==SDLK_ESCAPE)running=false;
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
            runtime.advance(seconds,suspended,actions,state.yaw,state.pitch);
            for(const auto& cue:runtime.takeCues())if(audio)audio->cue(cue.sequence);
            if(width<=0||height<=0||(flags&SDL_WINDOW_MINIMIZED)){SDL_Delay(16);continue;}
            renderer.resize(width,height);
            const auto view=runtime.present(state.yaw,state.pitch,state.distance,float(std::min(seconds,.1)));
            engine::ScenePlacement placement;placement.physicalCharacter=true;placement.offset=view.feet;placement.offset[1]+=.15f;
            placement.eye=view.eye;placement.target=view.target;placement.model=&mesh;placement.pose=view.palette;placement.markerActive=runtime.targetActive();state.objectYaw=view.yaw;
            renderer.draw(state,engine::GeometryCheck::None,false,nullptr,&surfaces,&placement);
            bgfx::dbgTextClear();bgfx::dbgTextPrintf(2,2,0x0f,"BOOTER & BIGARM - ENGINE SKELETON");
            bgfx::dbgTextPrintf(2,4,0x07,"WASD: move  Space: jump  Shift: run  Right drag: camera");
            bgfx::dbgTextPrintf(2,5,0x07,"E: interact  P/Start: pause  Esc: exit");
            bgfx::dbgTextPrintf(2,7,0x0f,"%s",paused?"Paused":(!focused&&!technical?"Paused while window is inactive":""));
            bgfx::dbgTextPrintf(2,9,0x0b,"%s",runtime.canInteract()?"E - toggle calibration marker":"Move near the marker to interact");
            bgfx::dbgTextPrintf(2,10,0x07,"Marker: %s | Audio: %s",runtime.targetActive()?"on":"off",audio?"on":"silent");
            if(technical&&frame==20)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(capture/"player.png").string().c_str());
            bgfx::frame();
            if(technical&&frame==30)running=false;
            if(technical)SDL_Delay(10);
        }
        albedo.reset();normal.reset();surface.reset();textures.stop();
    }
    renderer.stop();
    if(technical) {
        const bool passed=renderer.callbacks.captures==1&&renderer.callbacks.errors==0;
        engine::writeDocument(capture/"player.json","engine.player-render",{{"passed",passed},{"backend",backend},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"simulation_advanced",false}});
        if(!passed)throw std::runtime_error("Player render capture failed");
    }
    return 0;
}
}
int main(int argc,char** argv){try{return run(argc,argv);}catch(const std::exception& error){std::cerr<<"ERROR: "<<error.what()<<'\n';return 1;}}

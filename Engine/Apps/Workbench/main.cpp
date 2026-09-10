#include "Rendering/StreamingScene.h"
#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Core/FixtureState.h"
#include "Runtime/InspectionDocument.h"
#include "Rendering/TextureStore.h"
#include "Rendering/TextureChecks.h"
#include "Platform/Window.h"
#include "Rendering/Renderer.h"
#include "Rendering/InspectorRenderer.h"
#include <imgui.h>
#include <imgui_impl_sdl3.h>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <cmath>
#include <algorithm>
#include "Platform/ActionInput.h"
#include "Simulation/FixedClock.h"
#include "Simulation/World.h"
#include "Physics/CharacterController.h"
#include "Game/ThirdPersonCamera.h"
#include "Game/CalibrationRuntime.h"
#include "Audio/Audio.h"
#include "Animation/AnimationPlayer.h"
#include "Tools/RockWorkbench.h"
#include "Tools/InspectionWorkbench.h"

namespace {
struct Options { std::filesystem::path shaders, verify, inspection, saveInspection, catalog, bindings, saveBindings, model, rock, terrain, streamRock, constraints; bool buildInfo=false, lightingVerify=false, animationVerify=false, rockVerify=false,streamVerify=false; };
Options parse(int argc,char** argv) {
    Options options;
    for (int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if ((arg=="--terrain" || arg=="--stream-rock" || arg=="--constraints" || arg=="--verify-stream" || arg=="--rock" || arg=="--verify-rock" || arg=="--model" || arg=="--verify-animation" || arg=="--bindings" || arg=="--save-bindings" || arg=="--verify-lighting" || arg=="--verify" || arg=="--shaders" || arg=="--inspection" || arg=="--save-inspection" || arg=="--catalog") && i+1<argc) {
            if(arg=="--terrain")options.terrain=argv[++i];
            else if(arg=="--stream-rock")options.streamRock=argv[++i];
            else if(arg=="--constraints")options.constraints=argv[++i];
            else if(arg=="--verify-stream"){options.streamVerify=true;options.verify=argv[++i];}
            else if(arg=="--rock")options.rock=argv[++i];
            else if(arg=="--verify-rock"){options.rockVerify=true;options.verify=argv[++i];}
            else if (arg=="--model") options.model=argv[++i];
            else if (arg=="--verify-animation") {options.animationVerify=true;options.verify=argv[++i];}
            else if (arg=="--verify-lighting") { options.lightingVerify=true;options.verify=argv[++i]; }
            else if (arg=="--verify") options.verify=argv[++i];
            else if (arg=="--inspection") options.inspection=argv[++i];
            else if (arg=="--save-inspection") options.saveInspection=argv[++i];
            else if (arg=="--catalog") options.catalog=argv[++i];
            else if (arg=="--bindings") options.bindings=argv[++i];
            else if (arg=="--save-bindings") options.saveBindings=argv[++i];
            else options.shaders=argv[++i];
        } else if (arg=="--build-info") options.buildInfo=true;
        else throw std::runtime_error("Usage: engine_workbench [--shaders directory] [--inspection file] [--save-inspection file] [--build-info] [--catalog catalog.json] [--model model.json] [--rock recipe.json] [--terrain recipe.json] [--stream-rock recipe.json] [--constraints file.json] [--verify-stream new-directory] [--verify-rock new-output-directory] [--verify-animation new-output-directory] [--verify new-output-directory] [--verify-lighting new-output-directory]");
    }
    return options;
}
struct SDLSession {
    SDLSession() { SDL_SetMainReady(); if (!SDL_Init(SDL_INIT_VIDEO|SDL_INIT_EVENTS|SDL_INIT_GAMEPAD)) throw std::runtime_error(SDL_GetError()); }
    ~SDLSession() { SDL_Quit(); }
};
struct UIContext {
    bool platform=false;
    UIContext(SDL_Window* window) {
        IMGUI_CHECKVERSION(); ImGui::CreateContext();
        auto& io=ImGui::GetIO(); io.IniFilename=nullptr;
        io.ConfigFlags|=ImGuiConfigFlags_NavEnableKeyboard;
        platform=ImGui_ImplSDL3_InitForOther(window);
        if (!platform) { ImGui::DestroyContext(); throw std::runtime_error("Inspector platform initialization failed"); }
        ImGui::StyleColorsDark();
        auto& style=ImGui::GetStyle(); style.WindowRounding=7; style.FrameRounding=4;
        style.WindowPadding=ImVec2(18,18); style.ItemSpacing=ImVec2(10,9);
    }
    ~UIContext() { if (platform) ImGui_ImplSDL3_Shutdown(); ImGui::DestroyContext(); }
};
struct TextureControls {
    const std::vector<engine::TextureRecord>* records=nullptr;
    int selected=0,loaded=-1,channel=0;float lod=0,repeat=1;bool enabled=false;
    std::string error;
};
struct SimulationControls { bool enabled=false,paused=false,grounded=false;uint64_t ticks=0;double dropped=0; };
struct ButtonPosition { float x=0,y=0; };
ButtonPosition inspector(engine::FixtureState& state,const engine::Renderer& renderer,float milliseconds,TextureControls& textures,SimulationControls& simulation,engine::InspectionWorkbench& documents,bool technical) {
    ImGui::SetNextWindowPos(ImVec2(20,20),ImGuiCond_Always);
    ImGui::SetNextWindowSize(ImVec2(310,std::min(650.0f,ImGui::GetIO().DisplaySize.y-40.0f)),ImGuiCond_Always);
    ImGui::Begin("Engine foundation",nullptr,ImGuiWindowFlags_NoResize|ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoCollapse);
    ImGui::PushItemWidth(150);
    ImGui::TextUnformatted("BOOTER & BIGARM");
    ImGui::TextDisabled("Perspective inspection fixture");
    ImGui::Separator();
    ImGui::TextUnformatted("Object");
    ImGui::SliderAngle("Rotation",&state.objectYaw,-180,180);
    ImGui::ColorEdit3("Color",state.color.data());
    if (ImGui::Button("Cool material",ImVec2(130,28))) state.color={0.18f,0.55f,0.85f};
    const auto min=ImGui::GetItemRectMin(),max=ImGui::GetItemRectMax();
    ButtonPosition button{(min.x+max.x)/2,(min.y+max.y)/2};
    if (ImGui::CollapsingHeader("Geometry inspection")) {
        ImGui::Combo("Shape",&state.mesh,"Cube\0Sloped solid\0Sphere\0");
        ImGui::SliderFloat3("Scale XYZ",state.objectScale.data(),0.2f,3.0f,"%.2f");
        ImGui::Checkbox("Show world normals",&state.showNormals);
    }
    ImGui::Separator();
    ImGui::TextUnformatted("Lighting");
    ImGui::SliderAngle("Direction",&state.lightAzimuth,-180,180);
    ImGui::SliderFloat("Intensity",&state.lightIntensity,0.0f,1.5f,"%.2f");
    ImGui::SliderFloat("Exposure",&state.exposure,-4.0f,4.0f,"%.1f stops");
    if (ImGui::CollapsingHeader("Sun and surface")) {
        ImGui::Checkbox("Cast shadows",&state.shadows);
        ImGui::SliderFloat("Shadow bias",&state.shadowBias,0,0.02f,"%.4f");
        ImGui::Checkbox("Use surface textures",&state.surfaceTextures);
        ImGui::SliderFloat("Roughness",&state.roughness,0.045f,1,"%.2f");
        ImGui::SliderFloat("Metallic",&state.metallic,0,1,"%.2f");
        ImGui::SliderFloat("Normal strength",&state.normalStrength,0,2,"%.2f");
        ImGui::SliderFloat("Texture repeats/m",&state.textureScale,0.1f,8,"%.2f");
        ImGui::SliderFloat("Ambient fill",&state.ambient,0,1,"%.2f");
    }
    ImGui::Separator();
    ImGui::TextUnformatted("Camera");
    ImGui::SliderFloat("Distance",&state.distance,2.5f,simulation.enabled?8.0f:30.0f,"%.1f m");
    ImGui::SliderFloat("Field of view",&state.fieldOfView,30.0f,90.0f,"%.0f deg");
    if (ImGui::Button("Reset view")) { state.yaw=0.65f; state.pitch=0.28f; state.distance=7.5f; state.fieldOfView=55.0f; }
    ImGui::Separator();
    ImGui::Text("%s | %d x %d",renderer.name(),renderer.width(),renderer.height());
    ImGui::Text("Frame interval: %.2f ms",milliseconds);
    const auto* stats=bgfx::getStats();
    ImGui::Text("Draw calls: %u",stats->numDraw);
    ImGui::Text("Buffers: %u vertex / %u index",stats->numVertexBuffers,stats->numIndexBuffers);
    ImGui::Text("Textures: %u",stats->numTextures);
    ImGui::Separator();
    ImGui::TextWrapped("Right drag: orbit  |  Wheel: distance\nTurquoise marker: 1.8 m tall\nTemporary scale and local coordinates");
    if (textures.records && !textures.records->empty() && ImGui::CollapsingHeader("Texture inspection",ImGuiTreeNodeFlags_DefaultOpen)) {
        ImGui::Checkbox("Preview texture",&textures.enabled);
        if (ImGui::BeginCombo("Asset",textures.records->at(size_t(textures.selected)).id.c_str())) {
            for (int i=0;i<int(textures.records->size());++i)
                if (ImGui::Selectable(textures.records->at(size_t(i)).id.c_str(),i==textures.selected)) {textures.selected=i;textures.lod=0;}
            ImGui::EndCombo();
        }
        ImGui::SliderFloat("Mip level",&textures.lod,0,float(textures.records->at(size_t(textures.selected)).mips-1),"%.0f");
        ImGui::Combo("Channel",&textures.channel,"RGB\0R\0G\0B\0Alpha\0");
        ImGui::SliderFloat("UV repeats",&textures.repeat,1,8,"%.1f");
        if (!textures.error.empty()) ImGui::TextWrapped("%s",textures.error.c_str());
    }
    if(ImGui::CollapsingHeader("Shared simulation")) {
        ImGui::Checkbox("Enable character",&simulation.enabled);
        ImGui::Checkbox("Paused",&simulation.paused);
        ImGui::Text("Tick: %llu",static_cast<unsigned long long>(simulation.ticks));
        ImGui::Text("Grounded: %s",simulation.grounded?"yes":"no");
        ImGui::Text("Dropped time: %.3f s",simulation.dropped);
        ImGui::TextWrapped("WASD / left stick: move. Space / South: jump. Shift / stick click: run. Right drag / right stick: camera. P / Start: pause. E: toggle nearby marker.");
    }
    documents.draw(state,!technical&&!simulation.enabled);
    ImGui::PopItemWidth();
    ImGui::End();
    state.constrain();
    if(simulation.enabled) state.distance=std::min(state.distance,8.0f);
    return button;
}
int run(Options options) {
    std::cout << "Engine build " << ENGINE_BUILD_ID << " | compiler " <<
#ifdef _MSC_VER
        "MSVC " << _MSC_VER
#else
        __VERSION__
#endif
        << '\n';
    if (options.buildInfo) return 0;
    engine::FixtureState state;
    if (!options.inspection.empty()) engine::loadInspection(options.inspection,state);
    const bool technical=!options.verify.empty();
    const bool verify=technical && !options.lightingVerify && !options.animationVerify && !options.rockVerify && !options.streamVerify;
    const bool rockVerify=options.rockVerify,streamVerify=options.streamVerify;
    if(streamVerify&&options.terrain.empty())throw std::runtime_error("Stream verification requires --terrain");
    if(!options.terrain.empty()&&!options.rock.empty())throw std::runtime_error("Choose terrain streaming or the single-rock workbench");
    if(rockVerify&&options.rock.empty())throw std::runtime_error("Rock verification requires --rock");
    const bool animationVerify=options.animationVerify;
    if(animationVerify && options.model.empty())throw std::runtime_error("Animation verification requires --model");
    if(int(options.animationVerify)+int(options.lightingVerify)+int(options.rockVerify)+int(options.streamVerify)>1)throw std::runtime_error("Choose one verification mode");
    const bool lightingVerify=options.lightingVerify;
    if (technical) {
        if (std::filesystem::exists(options.verify)) throw std::runtime_error("Verification output exists; choose a new directory");
        std::filesystem::create_directories(options.verify);
    }
    SDLSession session;
    if (options.shaders.empty()) {
        const char* base=SDL_GetBasePath();
        if (!base) throw std::runtime_error("Cannot locate executable assets");
        options.shaders=std::filesystem::path(base)/"Shaders";
    }
    if (options.catalog.empty()) {
        const char* base=SDL_GetBasePath();
        if (base && std::filesystem::exists(std::filesystem::path(base)/"Assets/catalog.json")) options.catalog=std::filesystem::path(base)/"Assets/catalog.json";
    }
    engine::Window window(technical);
    engine::Renderer renderer;
    renderer.start(window,options.shaders);
    const std::string backend=renderer.name();
    const auto records=options.catalog.empty()?std::vector<engine::TextureRecord>{}:engine::loadTextureCatalog(options.catalog);
    std::shared_ptr<const engine::ModelData> modelData;
    std::unique_ptr<engine::AnimationPlayer> animation;
    std::unique_ptr<engine::RenderModel> renderModel;
    if(!options.model.empty()) {
        modelData=std::make_shared<engine::ModelData>(engine::loadModel(options.model));
        animation=std::make_unique<engine::AnimationPlayer>(modelData);
        renderModel=std::make_unique<engine::RenderModel>(*modelData);
    }
    if(animationVerify && modelData->clips.size()<2)throw std::runtime_error("Animation verification requires two clips");
    double animationSeconds=0;float previewBlend=0;int previewClip=0;bool animationPlaying=true;
    engine::TextureStore textures;
    if (verify && !records.empty()) {
        const auto result=engine::verifyTextureStore(textures,records.at(0));
        engine::writeDocument(options.verify/"texture-resources.json","engine.texture-verification",result);
    }

    engine::TextureLease texture;
    bool materialAvailable=!records.empty();
    std::array<engine::TextureLease,6> surfaceLeases;
    auto acquireSurface=[&](size_t slot,const char* id,engine::TextureRole role) {
        const auto found=std::find_if(records.begin(),records.end(),[&](const auto& record){return record.id==id;});
        if (found==records.end()) {
            if (*id) materialAvailable=false;
            surfaceLeases[slot]=textures.fallback(role);
        } else {
            if(found->role!=role) throw std::runtime_error(std::string("Incorrect material texture role: ")+id);
            surfaceLeases[slot]=textures.acquire(*found);
        }
        return textures.resolve(surfaceLeases[slot].token());
    };
    engine::SceneSurfaces surfaces;
    surfaces.rock.albedo=acquireSurface(0,"surface/textures/rocks/workbench/layered/rockworkbenchside_albedo",engine::TextureRole::Color);
    surfaces.rock.normal=acquireSurface(1,"surface/textures/rocks/workbench/layered/rockworkbenchside_normal",engine::TextureRole::Normal);
    surfaces.rock.surface=acquireSurface(2,"surface/textures/rocks/workbench/layered/rockworkbenchside_surface",engine::TextureRole::Surface);
    surfaces.ground.albedo=acquireSurface(3,"surface/textures/ground/sanddirt/brokenworldsanddirtalbedo",engine::TextureRole::Color);
    surfaces.ground.normal=acquireSurface(4,"",engine::TextureRole::Normal);
    surfaces.ground.surface=acquireSurface(5,"",engine::TextureRole::Surface);
    // This source ground material has no paired normal/surface map. Use explicit
    // neutral normal and scalar roughness rather than substituting another ground.
    surfaces.ground.packedSurface=false;
    if (!materialAvailable) state.surfaceTextures=false;
    if(verify) state.surfaceTextures=false;
    TextureControls textureControls;textureControls.records=&records;
    engine::Actions actions;
    if(!options.bindings.empty()) engine::loadBindings(options.bindings,actions);
    engine::ActionInput actionInput(actions);
    SimulationControls simulation;
    engine::CalibrationRuntime runtime(modelData);
    std::unique_ptr<engine::RockWorkbench> rock;
    if(!options.rock.empty())rock=std::make_unique<engine::RockWorkbench>(options.rock,runtime);
    engine::InspectionWorkbench documents(state,!options.inspection.empty()?options.inspection:(!options.saveInspection.empty()?options.saveInspection:std::filesystem::path(SDL_GetBasePath())/"inspection.json"));
    std::unique_ptr<engine::StreamingScene> streaming;
    if(!options.terrain.empty()){
        streaming=std::make_unique<engine::StreamingScene>(runtime,engine::loadTerrainRecipe(options.terrain),options.streamRock.empty()?engine::RockRecipe{}:engine::loadRockRecipe(options.streamRock),options.constraints.empty()?engine::PlacementConstraints{}:engine::loadConstraints(options.constraints));
        simulation.enabled=true;
    }
    unsigned streamPhase=0,streamStable=0,streamVertices=0,streamIndices=0,streamFinalVertices=0,streamFinalIndices=0;uint64_t streamRetired=0;bool streamDone=false;
    unsigned rockBaselineVertices=0,rockBaselineIndices=0,rockFinalVertices=0,rockFinalIndices=0;
    const auto originalRecipe=rock?rock->recipe():engine::RockRecipe{};
    std::unique_ptr<engine::Audio> audio;bool audioAttempted=false;
    bool clicked=false,orbited=false,resized=false,closed=false;
    unsigned baselineBuffers=0,finalBuffers=0;
    {
        UIContext context(window.get());
        engine::InspectorRenderer ui;
        ui.start(options.shaders);
        bool running=true;
        ButtonPosition button;
        auto last=std::chrono::steady_clock::now();
        for (unsigned frame=0;running;++frame) {
            if (technical && frame>530) throw std::runtime_error("Verification exceeded frame limit");
            SDL_Event event;
            float dx=0,dy=0,wheel=0;
            while (SDL_PollEvent(&event)) {
                ImGui_ImplSDL3_ProcessEvent(&event);
                actionInput.event(event);
                if (event.type==SDL_EVENT_QUIT || event.type==SDL_EVENT_WINDOW_CLOSE_REQUESTED) { running=false; closed=true; }
                if (event.type==SDL_EVENT_MOUSE_MOTION && (event.motion.state & SDL_BUTTON_RMASK)) { dx+=event.motion.xrel; dy+=event.motion.yrel; }
                if (event.type==SDL_EVENT_MOUSE_WHEEL) wheel+=event.wheel.y;
                if (event.type==SDL_EVENT_KEY_DOWN && event.key.key==SDLK_ESCAPE && !ImGui::GetIO().WantCaptureKeyboard) running=false;
            }
            if (!running) break;
            int width=0,height=0; window.pixels(width,height);
            if (width<=0 || height<=0 || (SDL_GetWindowFlags(window.get())&SDL_WINDOW_MINIMIZED)) { actions.focus(false);runtime.advance(0,true,actions,state.yaw,state.pitch);last=std::chrono::steady_clock::now();SDL_Delay(16); continue; }
            renderer.resize(width,height);
            ImGui_ImplSDL3_NewFrame();
            auto& io=ImGui::GetIO();
            // Deterministic UI injection is confined to the explicit technical verification mode.
            if (verify) {
                io.AddFocusEvent(true);
                if (frame==35 || frame==36 || frame==37) io.AddMousePosEvent(button.x,button.y);
                if (frame==36) io.AddMouseButtonEvent(0,true);
                if (frame==37) io.AddMouseButtonEvent(0,false);
                if (frame==65) io.AddMousePosEvent(800,300);
            }
            ImGui::NewFrame();
            const auto now=std::chrono::steady_clock::now();
            const float milliseconds=std::chrono::duration<float,std::milli>(now-last).count(); last=now;
            state.orbit(dx,dy,io.WantCaptureMouse); state.zoom(wheel,io.WantCaptureMouse);
            if (verify && frame==155) state=engine::geometryProofState();
            if (verify && frame==230) state.mesh=2;
            if (verify && frame>=350 && !records.empty()) {
                auto select=[&](const char* id) {
                    const auto found=std::find_if(records.begin(),records.end(),[&](const auto& r){return r.id==id;});
                    if (found==records.end()) throw std::runtime_error("Missing texture verification asset");
                    textureControls.selected=int(found-records.begin());textureControls.lod=0;textureControls.channel=0;
                };
                state.exposure=0;state.showNormals=false;
                if (frame==350) select("diagnostic/color");
                if (frame==375) textureControls.lod=2;
                if (frame==395) select("diagnostic/normal");
                if (frame==415) {select("diagnostic/surface");textureControls.channel=2;}
                if (frame==435) select("surface/textures/rocks/workbench/layered/rockworkbenchside_albedo");
                if (frame==455) select("surface/textures/ground/sanddirt/brokenworldsanddirtalbedo");
            }
            if (lightingVerify) {
                if(frame==0) {state=engine::FixtureState{};state.mesh=2;state.pitch=.6f;state.distance=6;state.surfaceTextures=false;state.shadows=false;}
                if(frame==15) state.shadows=true;
                if(frame==30) state.lightAzimuth=-.8f;
                if(frame==45) {state.lightAzimuth=.8f;state.surfaceTextures=materialAvailable;state.roughness=1;}
                if(frame==60) state.normalStrength=0;
                if(frame==75) {state.normalStrength=1;state.roughness=.15f;}
                if(frame==90) {state.roughness=1;state.exposure=-1;}
                if(frame==105) {state.exposure=0;state.shadowBias=.008f;state.yaw=-.6f;}
            }
            if (verify && frame==350) textureControls.enabled=true;
            if(animationVerify) {state=engine::FixtureState{};state.surfaceTextures=false;state.shadows=true;state.yaw=.6f;state.pitch=.35f;state.distance=4;state.showNormals=frame>=45;}
            if(rockVerify) {
                if(frame==0){state=engine::FixtureState{};state.distance=7;state.surfaceTextures=materialAvailable;rock->forcedLod=0;}
                if(frame==15)rock->forcedLod=1;
                if(frame==30)rock->forcedLod=2;
                if(frame==45){rock->forcedLod=0;auto changed=originalRecipe;changed.radiiMm[1]+=600;rock->apply(changed);}
                if(frame==60)rock->undo();
                if(frame==75){auto invalid=originalRecipe;invalid.subdivisions=99;bool rejected=false;try{rock->apply(invalid);}catch(const std::exception&){rejected=true;}if(!rejected||rock->recipe()!=originalRecipe)throw std::runtime_error("Failed rock edit changed the document");state.yaw+=3.14159265f;}
            }
            button=inspector(state,renderer,milliseconds,textureControls,simulation,documents,technical);
            if(rock&&!rockVerify)rock->drawControls(simulation.enabled);
            if(animation && !animationVerify) {
                ImGui::Begin("Model animation");
                ImGui::Text("%zu joints | %zu clips",modelData->joints.size(),modelData->clips.size());
                if(!modelData->clips.empty() && !simulation.enabled) {
                    ImGui::Checkbox("Play",&animationPlaying);
                    if(ImGui::BeginCombo("Clip",modelData->clips[size_t(previewClip)].name.c_str())) {
                        for(size_t i=0;i<modelData->clips.size();++i)if(ImGui::Selectable(modelData->clips[i].name.c_str(),previewClip==int(i))) {previewClip=int(i);animationSeconds=0;}
                        ImGui::EndCombo();
                    }
                    if(modelData->clips.size()>1)ImGui::SliderFloat("Blend to next clip",&previewBlend,0,1);
                }
                ImGui::TextUnformatted(simulation.enabled?"Animation follows character movement":"Model pose preview");
                if(simulation.enabled)ImGui::Text("Marker: %s | %s",runtime.targetActive()?"on":"off",runtime.canInteract()?"E to interact":"out of reach");ImGui::End();
            }
            const bool focused=(SDL_GetWindowFlags(window.get())&SDL_WINDOW_INPUT_FOCUS)!=0;
            actions.focus(focused);actionInput.sample();
            if(actions.consume(engine::Action::Pause).pressed) simulation.paused=!simulation.paused;
            actions.gameplay(simulation.enabled && !simulation.paused && !io.WantCaptureKeyboard);
            if(streaming){
                if(streamVerify)streaming->anchors({{{{streamPhase==1?4:0,0},{32,0,32}},0}});
                else streaming->update();
                if(!streamVerify){ImGui::Begin("World streaming");ImGui::Text("%zu regions | %.1f MiB CPU",streaming->stream().slots().size(),streaming->stream().residentBytes()/1048576.f);ImGui::TextWrapped(runtime.waitingForWorld()?"Waiting for ground collision":"World ready around player");for(const auto& [_,slot]:streaming->stream().slots())if(!slot.error.empty())ImGui::TextWrapped("%s",slot.error.c_str());ImGui::End();}
            }
            runtime.advance(double(milliseconds)/1000.0,technical||!simulation.enabled||simulation.paused||!focused,actions,state.yaw,state.pitch);
            simulation.grounded=runtime.grounded();simulation.ticks=runtime.clock().ticks();simulation.dropped=runtime.clock().droppedSeconds();
            if(simulation.enabled&&!technical&&!audioAttempted) {
                audioAttempted=true;try{audio=std::make_unique<engine::Audio>();}catch(const std::exception& error){std::cerr<<error.what()<<"; continuing silently\n";}
            }
            for(const auto& cue:runtime.takeCues())if(audio)audio->cue(cue.sequence);
            engine::ScenePlacement placement;
            if(simulation.enabled) {
                const auto frame=runtime.present(state.yaw,state.pitch,state.distance,std::min(milliseconds/1000.f,.1f));
                placement.offset=frame.feet;placement.offset[1]+=.15f;placement.eye=frame.eye;placement.target=frame.target;
                placement.physicalCharacter=true;placement.pose=frame.palette;placement.markerActive=runtime.targetActive();state.objectYaw=frame.yaw;
            }
            if(animation) {
                placement.model=renderModel.get();
                if(animationVerify) {
                    placement.pose=frame<15?&animation->rest():&animation->sample(1,.25);
                    placement.cpuReference=(frame>=30&&frame<45)||frame>=60;
                    if(frame==30||frame==60)renderModel->bakeReference(*modelData,*placement.pose);
                } else if(simulation.enabled) { /* Shared runtime supplied the pose. */ }
                else if(modelData->clips.empty())placement.pose=&animation->rest();
                else {
                    if(!simulation.enabled && animationPlaying)animationSeconds+=std::min(double(milliseconds)/1000.0,.1);
                    const size_t clip=simulation.enabled?0:size_t(previewClip);
                    placement.pose=&animation->sample(clip,animationSeconds,modelData->clips.size()>1?(clip+1)%modelData->clips.size():SIZE_MAX,previewBlend);
                }
            }
            engine::TexturePreview preview;
            const bool showTexture=!records.empty() && textureControls.enabled && (!verify || frame>=350);
            if (showTexture) {
                const auto& record=records.at(size_t(textureControls.selected));
                if (textureControls.loaded!=textureControls.selected) {
                    try {texture=textures.acquire(record);textureControls.loaded=textureControls.selected;textureControls.error.clear();}
                    catch(const std::exception& error) {textureControls.error=error.what();if(verify)throw;}
                }
                if (textureControls.loaded>=0) preview={textures.resolve(texture.token()),textureControls.lod,float(textureControls.channel),records.at(size_t(textureControls.loaded)).srgb,textureControls.repeat};
            }
            if(rock){float distance=state.distance;if(simulation.enabled){float squared=0;for(size_t i=0;i<3;++i){const float delta=placement.eye[i]-engine::RockWorkbench::offset[i];squared+=delta*delta;}distance=std::sqrt(squared);}placement.rock=rock->model(distance);placement.rockOffset=engine::RockWorkbench::offset;}
            if(streaming){
                if(streamVerify){const float shift=streamPhase==1?1024.f:0.f;placement.physicalCharacter=true;placement.eye={32+shift,18,52};placement.target={32+shift,0,24};}
                placement.streamedWorld=true;placement.instances=&streaming->instances(placement.physicalCharacter?placement.eye:state.eye());
            }
            ImGui::Render();
            engine::GeometryCheck geometryCheck=engine::GeometryCheck::None;
            constexpr engine::GeometryCheck checks[]={engine::GeometryCheck::Transformed,engine::GeometryCheck::BakedReference,
                engine::GeometryCheck::Unculled,engine::GeometryCheck::FrontCull,engine::GeometryCheck::ReverseOrder,engine::GeometryCheck::Transformed};
            if (verify && frame>=155 && frame<245) geometryCheck=checks[(frame-155)/15];
            renderer.draw(state,geometryCheck,verify && frame>=265 && frame<345,showTexture?&preview:nullptr,&surfaces,(simulation.enabled||animation||rock||streaming)?&placement:nullptr); if(!animationVerify&&!rockVerify&&!streamVerify)ui.draw(ImGui::GetDrawData());
            if (verify) {
                auto capture=[&](const char* file) { bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/file).string().c_str()); };
                if (frame==25) { capture("baseline.png"); baselineBuffers=bgfx::getStats()->numVertexBuffers; }
                if (frame==55) { clicked=state.color[2]>0.8f; capture("material.png"); }
                if (frame==66) {
                    SDL_Event drag{}; drag.type=SDL_EVENT_MOUSE_MOTION; drag.motion.windowID=SDL_GetWindowID(window.get());
                    drag.motion.state=SDL_BUTTON_RMASK; drag.motion.x=800; drag.motion.y=300; drag.motion.xrel=70; drag.motion.yrel=10;
                    if (!SDL_PushEvent(&drag)) throw std::runtime_error("Cannot inject camera event");
                }
                if (frame==85) { orbited=std::abs(state.yaw-0.65f)>0.1f; capture("orbit.png"); }
                if (frame==95 && !SDL_SetWindowSize(window.get(),1000,680)) throw std::runtime_error("Cannot resize fixture");
                if (frame>=100 && frame<120) renderer.rebuildMesh();
                if (frame==145) {
                    int logicalW=0,logicalH=0; SDL_GetWindowSize(window.get(),&logicalW,&logicalH);
                    resized=logicalW==1000 && logicalH==680;
                    finalBuffers=bgfx::getStats()->numVertexBuffers;
                    capture("resized.png");
                }
                if (frame>=165 && frame<=240 && (frame-165)%15==0) {
                    constexpr const char* names[]={"normals.png","baked.png","unculled.png","front-cull.png","reverse-order.png","sphere.png"};
                    capture(names[(frame-165)/15]);
                }
                if (frame==260) { state.showNormals=false; state.exposure=0; }
                if (frame==275) capture("color-linear.png");
                if (frame==280) state.exposure=-2;
                if (frame==295) capture("color-hdr.png");
                if (frame>=300 && frame<320) renderer.resize(frame%2?1000:996,680);
                if (frame==330) capture("color-restored.png");
                if (!records.empty() && frame>=365 && frame<=465 && (frame-365)%20==0) {
                    constexpr const char* names[]={"texture-color.png","texture-mip.png","texture-normal.png","texture-surface.png","texture-rock.png","texture-ground.png"};
                    capture(names[(frame-365)/20]);
                }
                if ((frame==345 && records.empty()) || (frame==480 && !records.empty())) { SDL_Event quit{}; quit.type=SDL_EVENT_QUIT; if (!SDL_PushEvent(&quit)) throw std::runtime_error("Cannot inject close event"); }
            }
            if(rockVerify) {
                if(frame==10){rockBaselineVertices=bgfx::getStats()->numVertexBuffers;rockBaselineIndices=bgfx::getStats()->numIndexBuffers;}
                if(frame==85){rockFinalVertices=bgfx::getStats()->numVertexBuffers;rockFinalIndices=bgfx::getStats()->numIndexBuffers;}
                if(frame>=10&&frame<=85&&(frame-10)%15==0){constexpr const char* names[]={"lod0.png","lod1.png","lod2.png","edited.png","undo.png","opposite.png"};bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/names[(frame-10)/15]).string().c_str());}
                if(frame==95)running=false;
            }
            if(animationVerify) {
                if(frame>=10&&frame<=70&&(frame-10)%15==0) {
                    constexpr const char* names[]={"rest.png","gpu-walk.png","cpu-walk.png","gpu-normals.png","cpu-normals.png"};
                    bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/names[(frame-10)/15]).string().c_str());
                }
                if(frame==80)running=false;
            }
            if(lightingVerify) {
                if(frame>=10 && frame<=115 && (frame-10)%15==0) {
                    constexpr const char* names[]={"unshadowed.png","shadowed.png","sun-moved.png","surfaces.png","flat-normal.png","smooth.png","exposure.png","camera-bias.png"};
                    bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/names[(frame-10)/15]).string().c_str());
                }
                if(frame==125) running=false;
            }
            if(streamVerify){
                if(frame>900)throw std::runtime_error("Stream verification timed out");
                bool ready=streaming->stream().slots().size()==9;for(const auto& [_,slot]:streaming->stream().slots())ready=ready&&slot.ready.render&&slot.ready.collision;
                streamStable=ready?streamStable+1:0;
                if(streamStable==10){const char* name=streamPhase==0?"origin.png":(streamPhase==1?"distant.png":"returned.png");bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/name).string().c_str());
                    if(streamPhase==0){streamVertices=bgfx::getStats()->numVertexBuffers;streamIndices=bgfx::getStats()->numIndexBuffers;}
                    if(streamPhase==2){streamFinalVertices=bgfx::getStats()->numVertexBuffers;streamFinalIndices=bgfx::getStats()->numIndexBuffers;streamRetired=streaming->stream().retired();streamDone=true;}
                }
                if(streamStable==15){if(streamDone)running=false;else{++streamPhase;streamStable=0;}}
            }
            bgfx::frame();
            if (technical) SDL_Delay(10);
        }
        ui.stop();
    }
    if(!options.saveBindings.empty()) engine::saveBindings(options.saveBindings,actions);
    texture.reset();for(auto& lease:surfaceLeases)lease.reset();textures.stop();
    streaming.reset();rock.reset();renderModel.reset();animation.reset();
    renderer.stop();
    if (!options.saveInspection.empty()) engine::saveInspection(options.saveInspection,state);
    if(streamVerify){const bool passed=streamDone&&renderer.callbacks.captures==3&&renderer.callbacks.errors==0&&streamVertices==streamFinalVertices&&streamIndices==streamFinalIndices&&streamRetired>=18;
        engine::writeDocument(options.verify/"stream.json","engine.stream-verification",{{"passed",passed},{"backend",backend},{"real_surfaces",materialAvailable},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"vertices_before",streamVertices},{"vertices_after",streamFinalVertices},{"indices_before",streamIndices},{"indices_after",streamFinalIndices},{"retired_regions",streamRetired},{"simulation_advanced",false}});
        if(!passed)throw std::runtime_error("Stream capture/resource check failed");}
    if(rockVerify) {
        const bool passed=renderer.callbacks.captures==6&&renderer.callbacks.errors==0&&rockBaselineVertices==rockFinalVertices&&rockBaselineIndices==rockFinalIndices;
        engine::writeDocument(options.verify/"rock.json","engine.rock-verification",{{"passed",passed},{"backend",backend},{"real_surfaces",materialAvailable},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"vertices_before",rockBaselineVertices},{"vertices_after",rockFinalVertices},{"indices_before",rockBaselineIndices},{"indices_after",rockFinalIndices}});
        if(!passed)throw std::runtime_error("Rock capture/resource check failed");
    }
    if(animationVerify) {
        const bool passed=renderer.callbacks.captures==5&&renderer.callbacks.errors==0;
        engine::writeDocument(options.verify/"animation.json","engine.animation-verification",{{"backend",backend},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"passed",passed}});
        if(!passed)throw std::runtime_error("Animation render capture failed");
    }
    if(lightingVerify) {
        const bool passed=renderer.callbacks.captures==8 && renderer.callbacks.errors==0;
        engine::writeDocument(options.verify/"lighting.json","engine.lighting-verification",{{"backend",backend},{"captures",renderer.callbacks.captures.load()},
            {"gpu_errors",renderer.callbacks.errors.load()},{"real_surfaces",materialAvailable},{"passed",passed}});
        if(!passed) throw std::runtime_error("Lighting capture failed");
    }
    if (verify) {
        const bool success=clicked && orbited && resized && closed && baselineBuffers==finalBuffers &&
            renderer.callbacks.captures==(records.empty()?13u:19u) && renderer.callbacks.errors==0;
        std::ofstream report(options.verify/"verification.json");
        report << "{\n  \"schema_version\": 1,\n  \"backend\": \"" << backend
               << "\",\n  \"inspector_click\": " << (clicked?"true":"false")
               << ",\n  \"camera_event\": " << (orbited?"true":"false")
               << ",\n  \"resize\": " << (resized?"true":"false")
               << ",\n  \"close_event\": " << (closed?"true":"false")
               << ",\n  \"vertex_buffers_before\": " << baselineBuffers
               << ",\n  \"vertex_buffers_after\": " << finalBuffers
               << ",\n  \"captures\": " << renderer.callbacks.captures
               << ",\n  \"capture_errors\": " << renderer.callbacks.errors
               << ",\n  \"passed\": " << (success?"true":"false") << "\n}\n";
        if (!report) throw std::runtime_error("Cannot save verification report");
        if (!success) throw std::runtime_error("Technical verification failed; inspect verification.json and GPU images");
        std::cout << "PASS: GPU fixture, inspector click, camera event, resize, 20 mesh rebuilds, close and cleanup\n";
    }
    return 0;
}
}
int main(int argc,char** argv) {
    try { return run(parse(argc,argv)); }
    catch (const std::exception& error) { std::cerr << "ERROR: " << error.what() << '\n'; return 1; }
}

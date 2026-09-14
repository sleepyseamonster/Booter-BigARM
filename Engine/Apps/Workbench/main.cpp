#include "Rendering/StreamingScene.h"
#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Core/FixtureState.h"
#include "Core/SceneCameraControls.h"
#include "Runtime/InspectionDocument.h"
#include "Rendering/TextureStore.h"
#include "Rendering/TextureChecks.h"
#include "Platform/Window.h"
#include "Rendering/Renderer.h"
#include "Rendering/InspectorRenderer.h"
#include <imgui.h>
#include <imgui_internal.h>
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
#include "Game/WorldSession.h"
#include "Audio/Audio.h"
#include "Animation/AnimationPlayer.h"
#include "Tools/RockWorkbench.h"
#include "Tools/InspectionWorkbench.h"
#include "Tools/EngineMenu.h"
#include "Tools/ViewerControls.h"

namespace {
struct Options { std::string testControls; std::filesystem::path shaders, verify, inspection, saveInspection, catalog, bindings, saveBindings, model, rock, terrain, streamRock, constraints, worldProfile, rockLibrary, captureRock; bool captureRockUi=false,terrainPreview=false,buildInfo=false, lightingVerify=false, environmentVerify=false, animationVerify=false, rockVerify=false,streamVerify=false,cameraVerify=false; };
Options parse(int argc,char** argv) {
    Options options;
    for (int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if(arg=="--test-controls"&&i+1<argc){options.testControls=argv[++i];continue;}
        if(arg=="--capture-rock-ui"){options.captureRockUi=true;continue;}
        if(arg=="--terrain-preview"){options.terrainPreview=true;continue;}
        if ((arg=="--verify-camera" || arg=="--rock-library" || arg=="--capture-rock" || arg=="--world-profile" || arg=="--terrain" || arg=="--stream-rock" || arg=="--constraints" || arg=="--verify-stream" || arg=="--rock" || arg=="--verify-rock" || arg=="--model" || arg=="--verify-animation" || arg=="--bindings" || arg=="--save-bindings" || arg=="--verify-environment" || arg=="--verify-lighting" || arg=="--verify" || arg=="--shaders" || arg=="--inspection" || arg=="--save-inspection" || arg=="--catalog") && i+1<argc) {
            if(arg=="--verify-camera"){options.cameraVerify=true;options.verify=argv[++i];}
            else if(arg=="--rock-library")options.rockLibrary=argv[++i];
            else if(arg=="--capture-rock"){options.captureRock=argv[++i];options.verify=options.captureRock;}
            else if(arg=="--world-profile")options.worldProfile=argv[++i];
            else if(arg=="--terrain")options.terrain=argv[++i];
            else if(arg=="--stream-rock")options.streamRock=argv[++i];
            else if(arg=="--constraints")options.constraints=argv[++i];
            else if(arg=="--verify-stream"){options.streamVerify=true;options.verify=argv[++i];}
            else if(arg=="--rock")options.rock=argv[++i];
            else if(arg=="--verify-rock"){options.rockVerify=true;options.verify=argv[++i];}
            else if (arg=="--model") options.model=argv[++i];
            else if (arg=="--verify-animation") {options.animationVerify=true;options.verify=argv[++i];}
            else if (arg=="--verify-environment") {options.environmentVerify=true;options.verify=argv[++i];}
            else if (arg=="--verify-lighting") { options.lightingVerify=true;options.verify=argv[++i]; }
            else if (arg=="--verify") options.verify=argv[++i];
            else if (arg=="--inspection") options.inspection=argv[++i];
            else if (arg=="--save-inspection") options.saveInspection=argv[++i];
            else if (arg=="--catalog") options.catalog=argv[++i];
            else if (arg=="--bindings") options.bindings=argv[++i];
            else if (arg=="--save-bindings") options.saveBindings=argv[++i];
            else options.shaders=argv[++i];
        } else if (arg=="--build-info") options.buildInfo=true;
        else throw std::runtime_error("Usage: engine_workbench [--test-controls rock|engine|animation|terrain] [--shaders directory] [--inspection file] [--save-inspection file] [--build-info] [--catalog catalog.json] [--model model.json] [--rock recipe.json] [--rock-library directory] [--capture-rock new-directory] [--verify-camera new-directory] [--terrain recipe.json] [--terrain-preview] [--world-profile directory] [--stream-rock recipe.json] [--constraints file.json] [--verify-stream new-directory] [--verify-rock new-output-directory] [--verify-animation new-output-directory] [--verify new-output-directory] [--verify-lighting new-output-directory]");
    }
    if(!options.testControls.empty()&&options.testControls!="rock"&&options.testControls!="engine"&&options.testControls!="animation"&&options.testControls!="terrain")throw std::runtime_error("Test controls must be rock, engine, animation or terrain");
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
ButtonPosition inspector(engine::FixtureState& state,const engine::Renderer& renderer,float milliseconds,TextureControls& textures,SimulationControls& simulation,engine::InspectionWorkbench& documents,bool technical,engine::ViewerControls& viewer,bool fullscreen) {
    engine::EngineMenu menu(viewer.advanced);
    if(!menu.visible) {
        documents.draw(state,!technical&&!simulation.enabled,false);
        state.constrain();
        if(simulation.enabled)state.distance=std::min(state.distance,8.0f);
        return {menu.button.x,menu.button.y};
    }
    viewer.draw(state.exposure,fullscreen,!simulation.enabled);
    if(!viewer.advanced) {
        documents.draw(state,!technical&&!simulation.enabled,false);
        state.constrain();
        if(simulation.enabled)state.distance=std::min(state.distance,8.0f);
        return {menu.button.x,menu.button.y};
    }
    ImGui::Separator();
    ImGui::PushID("Advanced");
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
    if(ImGui::CollapsingHeader("Sky and environment")) {
        ImGui::SliderAngle("Sun elevation",&state.environment.sunElevation,2,85);
        ImGui::ColorEdit3("Sun color",state.environment.sunColor.data(),ImGuiColorEditFlags_Float);
        ImGui::Checkbox("Show sky",&state.environment.sky);
        ImGui::Checkbox("PBR Neutral tone mapping",&state.environment.toneMapping);
        ImGui::ColorEdit3("Sky zenith",state.environment.zenith.data(),ImGuiColorEditFlags_Float);
        ImGui::ColorEdit3("Sky horizon",state.environment.horizon.data(),ImGuiColorEditFlags_Float);
        ImGui::ColorEdit3("Ground bounce",state.environment.ground.data(),ImGuiColorEditFlags_Float);
        ImGui::TextDisabled("Environment colors also control ambient fill.");
    }
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
    if (ImGui::Button("Reset view")) { state.yaw=0.65f; state.pitch=0.28f; state.distance=7.5f; state.fieldOfView=55.0f; state.viewOffset={}; }
    ImGui::Separator();
    ImGui::Text("%s | %d x %d",renderer.name(),renderer.width(),renderer.height());
    ImGui::Text("Frame interval: %.2f ms",milliseconds);
    const auto* stats=bgfx::getStats();
    ImGui::Text("Draw calls: %u",stats->numDraw);
    ImGui::Text("Buffers: %u vertex / %u index",stats->numVertexBuffers,stats->numIndexBuffers);
    ImGui::Text("Textures: %u",stats->numTextures);
    ImGui::Separator();
    ImGui::TextWrapped("Alt/Option + left drag: orbit\nSpace + left drag or middle drag: pan\nRight drag: orbit | Wheel: zoom | F: recenter");
    if(!simulation.enabled&&ImGui::Button("Recenter view (F)"))state.viewOffset={};
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
    ImGui::PopID();
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
    const bool captureRock=!options.captureRock.empty();
    if(options.testControls=="rock"&&options.rock.empty())throw std::runtime_error("Rock test controls require --rock");
    if(options.testControls=="animation"&&options.model.empty())throw std::runtime_error("Animation test controls require --model");
    if(options.testControls=="terrain"&&options.terrain.empty())throw std::runtime_error("Terrain test controls require --terrain");
    if(options.captureRockUi&&!captureRock)throw std::runtime_error("UI capture requires --capture-rock");
    if((captureRock||!options.rockLibrary.empty())&&options.rock.empty())throw std::runtime_error("Rock capture/library requires --rock");
    const bool verify=technical && !options.lightingVerify && !options.environmentVerify && !options.animationVerify && !options.rockVerify && !options.streamVerify && !captureRock && !options.cameraVerify;
    const bool rockVerify=options.rockVerify,streamVerify=options.streamVerify;
    if(options.terrainPreview&&options.terrain.empty())throw std::runtime_error("Terrain preview requires --terrain");
    if(streamVerify&&options.terrain.empty())throw std::runtime_error("Stream verification requires --terrain");
    if(!options.terrain.empty()&&!options.rock.empty())throw std::runtime_error("Choose terrain streaming or the single-rock workbench");
    if(rockVerify&&options.rock.empty())throw std::runtime_error("Rock verification requires --rock");
    const bool animationVerify=options.animationVerify;
    if(animationVerify && options.model.empty())throw std::runtime_error("Animation verification requires --model");
    if(int(options.environmentVerify)+int(options.animationVerify)+int(options.lightingVerify)+int(options.rockVerify)+int(options.streamVerify)+int(captureRock)+int(options.cameraVerify)>1)throw std::runtime_error("Choose one verification mode");
    const bool lightingVerify=options.lightingVerify||options.environmentVerify;
    if (technical) {
        if (std::filesystem::exists(options.verify)) throw std::runtime_error("Verification output exists; choose a new directory");
        std::filesystem::create_directories(options.verify);
    }
    if(options.terrain.empty()&&(!options.worldProfile.empty()||!options.streamRock.empty()||!options.constraints.empty()))throw std::runtime_error("World options require --terrain");
    SDLSession session;
    std::unique_ptr<engine::WorldSession> worldSession;
    if(!options.terrain.empty()) {
        if(options.worldProfile.empty()&&!technical)options.worldProfile=std::filesystem::path(SDL_GetBasePath())/"UserData/World";
        worldSession=std::make_unique<engine::WorldSession>(options.worldProfile,engine::WorldConfiguration{engine::loadTerrainRecipe(options.terrain),options.streamRock.empty()?engine::RockRecipe{}:engine::loadRockRecipe(options.streamRock),options.constraints.empty()?engine::PlacementConstraints{}:engine::loadConstraints(options.constraints)});
        if(worldSession->restored()&&!options.terrainPreview){const auto& player=worldSession->initial().player;state.yaw=player.cameraYaw;state.pitch=player.cameraPitch;state.distance=player.cameraDistance;}
    }
    if (options.shaders.empty()) {
        const char* base=SDL_GetBasePath();
        if (!base) throw std::runtime_error("Cannot locate executable assets");
        options.shaders=std::filesystem::path(base)/"Shaders";
    }
    if (options.catalog.empty()) {
        const char* base=SDL_GetBasePath();
        if (base && std::filesystem::exists(std::filesystem::path(base)/"Assets/catalog.json")) options.catalog=std::filesystem::path(base)/"Assets/catalog.json";
    }
    engine::Window window(technical,!technical);
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
    std::array<engine::TextureLease,23> surfaceLeases;
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
    const bool baseMaterialAvailable=materialAvailable;
    for(size_t i=0;i<engine::rockLayerTextureIds.size();++i)surfaces.rock.layers[i]=acquireSurface(i+6,engine::rockLayerTextureIds[i],i==9?engine::TextureRole::Mask:(i%3==0?engine::TextureRole::Color:(i%3==1?engine::TextureRole::Normal:engine::TextureRole::Surface)));
    const bool layeredMaterialAvailable=materialAvailable;materialAvailable=baseMaterialAvailable;

    if(worldSession&&worldSession->initial().configuration.terrain.version>=2){
        surfaces.ground.layers[0]=acquireSurface(16,"surface/textures/ground/sanddirt/brokenworldsweptsandalbedo",engine::TextureRole::Color);
        surfaces.ground.layers[3]=acquireSurface(17,"surface/textures/ground/sanddirt/brokenworldgravelalbedo",engine::TextureRole::Color);
        surfaces.ground.layers[6]=acquireSurface(18,"surface/textures/ground/sanddirt/brokenworldmixedrockyalbedo",engine::TextureRole::Color);
        surfaces.ground.layers[7]=acquireSurface(19,"surface/textures/ground/sanddirt/brokenworldmixedrockynormal",engine::TextureRole::Normal);
        if(!materialAvailable)throw std::runtime_error("Wasteland terrain requires the original ground texture family");
        surfaces.ground.terrainBlend=true;
        if(worldSession->initial().configuration.terrain.version==3){
            surfaces.ground.layers[1]=acquireSurface(20,"surface/textures/ground/sanddirt/brokenworldsweptsandtransitionalbedo",engine::TextureRole::Color);
            surfaces.ground.layers[4]=acquireSurface(21,"surface/textures/ground/sanddirt/brokenworldgraveltransitionalbedo",engine::TextureRole::Color);
            surfaces.ground.layers[2]=acquireSurface(22,"surface/textures/ground/sanddirt/brokenworldrockytransitionalbedo",engine::TextureRole::Color);
            if(!materialAvailable)throw std::runtime_error("Formation terrain requires original transition textures");surfaces.ground.terrainNatural=true;
        }
        const auto& recipe=worldSession->initial().configuration.rock;
        surfaces.rock.layered=recipe.version>=3&&layeredMaterialAvailable;surfaces.rock.material=recipe.material;surfaces.rock.seed=float(recipe.seed%65536)/65536.f;
    }
    if (!materialAvailable) state.surfaceTextures=false;
    if(verify) state.surfaceTextures=false;
    TextureControls textureControls;textureControls.records=&records;
    engine::Actions actions;
    if(!options.bindings.empty()) engine::loadBindings(options.bindings,actions);
    engine::ActionInput actionInput(actions);
    engine::SceneCameraControls sceneCamera;
    SimulationControls simulation;
    engine::CalibrationRuntime runtime(modelData,worldSession?worldSession->initial().player:engine::PlayerSnapshot{});
    std::unique_ptr<engine::RockWorkbench> rock;
    if(!options.rock.empty())rock=std::make_unique<engine::RockWorkbench>(options.rock,runtime,options.rockLibrary,layeredMaterialAvailable);
    engine::InspectionWorkbench documents(state,!options.inspection.empty()?options.inspection:(!options.saveInspection.empty()?options.saveInspection:std::filesystem::path(SDL_GetBasePath())/"inspection.json"));
    std::unique_ptr<engine::StreamingScene> streaming;
    if(worldSession){
        const auto& saved=worldSession->initial();streaming=std::make_unique<engine::StreamingScene>(runtime,saved.configuration.terrain,saved.configuration.rock,saved.configuration.constraints,saved.deltas);
        simulation.enabled=!options.terrainPreview;
    }
    uint64_t saveAttemptTick=runtime.clock().ticks();
    std::string worldStatus=worldSession?(worldSession->recovered()?"Recovered complete world save":(worldSession->restored()?"World restored":"New world")):"";
    auto saveWorld=[&] {
        if(!worldSession||technical)return;
        saveAttemptTick=runtime.clock().ticks();
        try{worldSession->save(runtime,state.yaw,state.pitch,state.distance,streaming->deltas());worldStatus="World saved (generation "+std::to_string(worldSession->generation())+")";}
        catch(const std::exception& error){worldStatus=error.what();std::cerr<<"World save failed: "<<error.what()<<'\n';}
    };
    unsigned streamPhase=0,streamStable=0,streamVertices=0,streamIndices=0,streamFinalVertices=0,streamFinalIndices=0;uint64_t streamRetired=0;bool streamDone=false;
    std::array<bool,3> rockUiChecks{};
    unsigned rockBaselineVertices=0,rockBaselineIndices=0,rockFinalVertices=0,rockFinalIndices=0;
    const auto originalRecipe=rock?rock->recipe():engine::RockRecipe{};
    std::unique_ptr<engine::Audio> audio;bool audioAttempted=false;
    bool clicked=false,orbited=false,resized=false,closed=false;
    unsigned baselineBuffers=0,finalBuffers=0;
    const auto cameraBaseline=state;engine::FixtureState cameraOrbit,cameraPan;
    std::array<bool,4> cameraChecks{};
    {
        UIContext context(window.get());
        engine::InspectorRenderer ui;
        ui.start(options.shaders);
        bool running=true;
        engine::ViewerControls viewer;
        viewer.advanced=true; // Only explicit engine-testing sessions reach this inspector.
        std::string viewerError;
        auto setFullscreen=[&](bool enabled) {
            if(!window.setWindowedFullscreen(enabled))viewerError=SDL_GetError();
            else viewerError.clear();
        };
        ButtonPosition button;
        auto last=std::chrono::steady_clock::now();
        for (unsigned frame=0;running;++frame) {
            if (technical && frame>530) throw std::runtime_error("Verification exceeded frame limit");
            SDL_Event event;
            float dx=0,dy=0,gameDx=0,gameDy=0,wheel=0;bool saveRequested=false,removeRequested=false;
            while (SDL_PollEvent(&event)) {
                ImGui_ImplSDL3_ProcessEvent(&event);
                actionInput.event(event);
                if (event.type==SDL_EVENT_QUIT || event.type==SDL_EVENT_WINDOW_CLOSE_REQUESTED) { running=false; closed=true; }
                if (event.type==SDL_EVENT_MOUSE_MOTION) { dx+=event.motion.xrel; dy+=event.motion.yrel;if(event.motion.state&SDL_BUTTON_RMASK){gameDx+=event.motion.xrel;gameDy+=event.motion.yrel;} }
                if (event.type==SDL_EVENT_MOUSE_WHEEL) wheel+=event.wheel.y;
                if (event.type==SDL_EVENT_KEY_DOWN && event.key.key==SDLK_F5 && !event.key.repeat && !ImGui::GetIO().WantCaptureKeyboard)saveRequested=true;
                if(event.type==SDL_EVENT_KEY_DOWN&&!event.key.repeat&&!ImGui::GetIO().WantCaptureKeyboard) {
                    if(event.key.key==SDLK_F11)setFullscreen(!window.windowedFullscreen());
                    if(event.key.key==SDLK_ESCAPE) {
                        if(window.windowedFullscreen())setFullscreen(false);
                    }
                }
            }
            if (!running) break;
            int width=0,height=0; window.pixels(width,height);
            if (width<=0 || height<=0 || (SDL_GetWindowFlags(window.get())&SDL_WINDOW_MINIMIZED)) { actions.focus(false);renderer.telemetry.measure(engine::FramePhase::Simulation,[&]{runtime.advance(0,true,actions,state.yaw,state.pitch);});last=std::chrono::steady_clock::now();SDL_Delay(16); continue; }
            renderer.resize(width,height);
            ImGui_ImplSDL3_NewFrame();
            auto& io=ImGui::GetIO();
            // Deterministic UI injection is confined to the explicit technical verification mode.
            if (verify) {
                io.AddFocusEvent(true);
                if(frame>=30&&frame<=32)io.AddMousePosEvent(button.x,button.y);
                if(frame==31)io.AddMouseButtonEvent(0,true);
                if(frame==32)io.AddMouseButtonEvent(0,false);
                if(frame==55)io.AddKeyEvent(ImGuiKey_Escape,true);
                if(frame==56)io.AddKeyEvent(ImGuiKey_Escape,false);
                if (frame==35 || frame==36 || frame==37) io.AddMousePosEvent(button.x,button.y);
                if (frame==36) io.AddMouseButtonEvent(0,true);
                if (frame==37) io.AddMouseButtonEvent(0,false);
                if (frame==65) io.AddMousePosEvent(800,300);
                if (frame==65) io.AddMouseButtonEvent(1,true);
                if (frame==69) io.AddMouseButtonEvent(1,false);
            }
            if(options.cameraVerify){
                io.AddFocusEvent(true);io.AddMousePosEvent(560,300);
                if(frame==12)io.AddKeyEvent(ImGuiMod_Alt,true);
                if(frame==14||frame==27)io.AddMouseButtonEvent(0,true);
                if(frame==18||frame==31)io.AddMouseButtonEvent(0,false);
                if(frame==19)io.AddKeyEvent(ImGuiMod_Alt,false);
                if(frame==25)io.AddKeyEvent(ImGuiKey_Space,true);
                if(frame==32)io.AddKeyEvent(ImGuiKey_Space,false);
                if(frame==38)io.AddMouseButtonEvent(2,true);
                if(frame==42)io.AddMouseButtonEvent(2,false);
                if(frame==48)io.AddKeyEvent(ImGuiKey_F,true);
                if(frame==50)io.AddKeyEvent(ImGuiKey_F,false);
            }
            ImGui::NewFrame();
            const auto now=std::chrono::steady_clock::now();
            const float milliseconds=std::chrono::duration<float,std::milli>(now-last).count(); last=now;
            const bool cameraFocused=technical||(SDL_GetWindowFlags(window.get())&SDL_WINDOW_INPUT_FOCUS);
            // Idle keyboard-navigation focus must not swallow the first viewport drag.
            const bool sceneKeyboardCaptured=io.WantCaptureKeyboard&&(io.WantTextInput||ImGui::IsAnyItemActive());
            const auto drag=sceneCamera.update(io.MouseDown[0],io.MouseDown[2],io.MouseDown[1],io.KeyAlt,ImGui::IsKeyDown(ImGuiKey_Space),io.WantCaptureMouse,sceneKeyboardCaptured,cameraFocused&&!simulation.enabled);
            if(simulation.enabled)state.orbit(gameDx,gameDy,io.WantCaptureMouse||!cameraFocused);
            else {
                if(drag==engine::SceneDrag::Orbit)state.orbit(dx,dy,false);
                if(drag==engine::SceneDrag::Pan){int logicalWidth=0,logicalHeight=0;SDL_GetWindowSize(window.get(),&logicalWidth,&logicalHeight);state.pan(dx,dy,float(logicalHeight),false);}
                if(cameraFocused&&!io.WantCaptureKeyboard&&ImGui::IsKeyPressed(ImGuiKey_F,false))state.viewOffset={};
            }
            state.zoom(wheel,io.WantCaptureMouse||!cameraFocused);
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
            if(options.environmentVerify) {
                if(frame==0||frame==45||frame==90||frame==105||frame==120) {
                    state=engine::FixtureState{};state.mesh=2;state.pitch=.15f;state.distance=7;
                    state.surfaceTextures=false;state.color={.72f,.72f,.72f};
                }
                if(frame==15){state.exposure=3;state.environment.toneMapping=false;}
                if(frame==30)state.environment.toneMapping=true;
                if(frame==45)state.environment.sunElevation=.12f;
                if(frame==60){state.showNormals=true;state.exposure=0;}
                if(frame==75)state.exposure=3;
                if(frame==90)state.environment.sky=false;
                if(frame==105){state.environment.zenith={.08f,.15f,.6f};state.environment.horizon={.3f,.4f,.8f};}
                if(frame==135||frame==165)state.environment.toneMapping=false;
                if(frame==150){
                    state.environment.toneMapping=true;textureControls.enabled=true;
                    if(records.empty())throw std::runtime_error("Environment preview verification requires a catalog");
                    textureControls.selected=0;
                }
            } else if (lightingVerify) {
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
            // Testing exposes only the named subsystem. Normal viewing submits no controls.
            const std::string testControls=options.captureRockUi
                ? ((frame>=26&&frame<=32)||frame>=36?"rock":"") : options.testControls;
            if(verify||testControls=="engine")
                button=inspector(state,renderer,milliseconds,textureControls,simulation,documents,technical,viewer,window.windowedFullscreen());
            else {
                documents.draw(state,!technical&&!simulation.enabled,false);
                state.constrain();
                if(simulation.enabled)state.distance=std::min(state.distance,8.0f);
            }
            if(viewer.fullscreenRequested){setFullscreen(!window.windowedFullscreen());viewer.fullscreenRequested=false;}
            if(viewer.frameRequested) {
                if(rock)rock->frameRequested=true;
                else {state.viewOffset={};state.distance=streaming?30.f:7.5f;}
                viewer.frameRequested=false;
            }
            if(!viewerError.empty()){ImGui::Begin("Display error");ImGui::TextWrapped("%s",viewerError.c_str());if(ImGui::Button("Dismiss"))viewerError.clear();ImGui::End();}
            if(rock&&!rockVerify&&testControls=="rock")rock->drawControls(simulation.enabled);
            if(rock&&rock->frameRequested&&!simulation.enabled){
                const auto& a=rock->asset().lods.front();float squared=0;for(size_t i=0;i<3;++i){const float extent=(a.maximum[i]-a.minimum[i])*.5f;squared+=extent*extent;}
                state.distance=std::clamp(std::sqrt(squared)/std::sin(state.fieldOfView*.00872664626f)*1.05f,2.5f,30.f);state.viewOffset={(a.minimum[0]+a.maximum[0])*.5f,0,(a.minimum[2]+a.maximum[2])*.5f};rock->frameRequested=false;
            }
            if(animation && !animationVerify && testControls=="animation") {
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
                if(streamVerify)renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{streaming->anchors({{{{streamPhase==1?4:0,0},{32,0,32}},0}});});
                else if(!simulation.enabled){state.viewOffset[0]=std::clamp(state.viewOffset[0],-3500.f,3500.f);state.viewOffset[2]=std::clamp(state.viewOffset[2],-3500.f,3500.f);const auto eye=state.eye();renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{streaming->anchors({{{{},{eye[0],eye[1],eye[2]}},0}});});}
                else renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{streaming->update();});
                if(!streamVerify&&testControls=="terrain"){
                    ImGui::SetNextWindowPos(ImVec2(12,54),ImGuiCond_FirstUseEver);
                    ImGui::Begin("Wasteland terrain");
                    ImGui::TextUnformatted("Option/Alt + drag: orbit | Middle/Space + drag: pan");
                    ImGui::Text("Terrain v%u | %u m samples",worldSession->initial().configuration.terrain.version,256/engine::terrainCells(worldSession->initial().configuration.terrain));
                    if(ImGui::Button("Scene camera")){simulation.enabled=false;const auto p=runtime.feet();state.viewOffset={p[0],p[1],p[2]};state.distance=30;state.pitch=.28f;}
                    ImGui::SameLine();if(ImGui::Button("Character camera")){simulation.enabled=true;state.distance=7;}
                    if(worldSession->initial().configuration.terrain.version>=2)ImGui::Checkbox("Blend ground materials",&surfaces.ground.terrainBlend);
                    ImGui::Text("%zu regions | %.1f MiB CPU",streaming->stream().slots().size(),streaming->stream().residentBytes()/1048576.f);
                    ImGui::TextWrapped(runtime.waitingForWorld()?"Waiting for ground collision":"World ready around player");
                    ImGui::BeginDisabled(technical);
                    ImGui::BeginDisabled(!simulation.enabled);removeRequested=ImGui::Button(worldSession->initial().configuration.rock.formation?"Remove nearby formation":"Remove nearby rock");ImGui::EndDisabled();ImGui::SameLine();
                    saveRequested=ImGui::Button("Save world (F5)")||saveRequested;
                    ImGui::EndDisabled();
                    ImGui::TextWrapped("%s",worldStatus.c_str());
                    for(const auto& [_,slot]:streaming->stream().slots())if(!slot.error.empty())ImGui::TextWrapped("%s",slot.error.c_str());ImGui::End();
                }
            }
            renderer.telemetry.measure(engine::FramePhase::Simulation,[&]{runtime.advance(double(milliseconds)/1000.0,technical||!simulation.enabled||simulation.paused||!focused,actions,state.yaw,state.pitch);});
            if(removeRequested)try{worldStatus=streaming->removeNearest()?"Rock removed; save to keep this change":"No rock within 8 metres";}catch(const std::exception& error){worldStatus=error.what();}
            if(saveRequested||runtime.clock().ticks()-saveAttemptTick>=600)saveWorld();
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
            if(rock){surfaces.rock.layered=rock->recipe().version>=3&&layeredMaterialAvailable;surfaces.rock.material=rock->recipe().material;surfaces.rock.seed=float(rock->recipe().seed%65536)/65536.f;float distance=state.distance;if(simulation.enabled){float squared=0;for(size_t i=0;i<3;++i){const float delta=placement.eye[i]-engine::RockWorkbench::offset[i];squared+=delta*delta;}distance=std::sqrt(squared);}placement.rock=rock->model(distance);placement.rockOffset=engine::RockWorkbench::offset;placement.rockFocusHeight=(rock->asset().lods[0].minimum[1]+rock->asset().lods[0].maximum[1])*.5f;}
            if(streaming){
                if(streamVerify){const float shift=streamPhase==1?1024.f:0.f;placement.physicalCharacter=true;const auto& terrain=worldSession->initial().configuration.terrain;const float ground=terrain.version>=2?engine::terrainSample(terrain,{{streamPhase==1?4:0,0},{32,0,24}}).height:0;placement.eye={32+shift,ground+(terrain.version>=2?7.f:18.f),52};placement.target={32+shift,ground+(terrain.version>=2?1.f:0.f),24};}
                placement.streamedWorld=true;renderer.telemetry.measure(engine::FramePhase::Streaming,[&]{placement.instances=&streaming->instances(placement.physicalCharacter?placement.eye:state.eye());});
            }
            ImGui::Render();
            engine::GeometryCheck geometryCheck=engine::GeometryCheck::None;
            constexpr engine::GeometryCheck checks[]={engine::GeometryCheck::Transformed,engine::GeometryCheck::BakedReference,
                engine::GeometryCheck::Unculled,engine::GeometryCheck::FrontCull,engine::GeometryCheck::ReverseOrder,engine::GeometryCheck::Transformed};
            if (verify && frame>=155 && frame<245) geometryCheck=checks[(frame-155)/15];
            renderer.telemetry.measure(engine::FramePhase::Draw,[&]{renderer.draw(state,geometryCheck,(verify && frame>=265 && frame<345)||(options.environmentVerify&&frame>=120&&frame<150),showTexture?&preview:nullptr,&surfaces,(simulation.enabled||animation||rock||streaming)?&placement:nullptr);}); if(!animationVerify&&!rockVerify&&!streamVerify&&(!captureRock||(options.captureRockUi&&frame>=24)))ui.draw(ImGui::GetDrawData());
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
                if(frame>=10 && frame<=(options.environmentVerify?175:115) && (frame-10)%15==0) {
                    constexpr const char* names[]={"unshadowed.png","shadowed.png","sun-moved.png","surfaces.png","flat-normal.png","smooth.png","exposure.png","camera-bias.png"};
                    constexpr const char* environmentNames[]={"baseline.png","clipped.png","tone-bright.png","low-sun.png","normals.png","normals-exposed.png","sky-off.png","cool-sky.png","calibration-tone.png","calibration-linear.png","preview-tone.png","preview-linear.png"};
                    const char* name=options.environmentVerify?environmentNames[(frame-10)/15]:names[(frame-10)/15];
                    bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/name).string().c_str());
                    if(options.environmentVerify)engine::saveInspection(options.verify/(std::string(name)+".inspection.json"),state);
                }
                if(frame==(options.environmentVerify?185:125)) running=false;
            }
            if(options.cameraVerify){
                // Exercise the production SDL motion -> ImGui gesture -> scene state path.
                if(frame==15||frame==28||frame==39){
                    SDL_Event motion{};motion.type=SDL_EVENT_MOUSE_MOTION;motion.motion.windowID=SDL_GetWindowID(window.get());
                    motion.motion.x=560;motion.motion.y=300;motion.motion.xrel=40;motion.motion.yrel=20;
                    if(!SDL_PushEvent(&motion))throw std::runtime_error("Cannot inject camera motion");
                }
                const char* capture=nullptr;
                if(frame==10)capture="baseline.png";
                if(frame==22){cameraOrbit=state;cameraChecks[0]=std::abs(state.yaw-cameraBaseline.yaw)>.1f&&state.viewOffset==cameraBaseline.viewOffset;capture="orbit.png";}
                if(frame==35){cameraPan=state;cameraChecks[1]=state.yaw==cameraOrbit.yaw&&state.pitch==cameraOrbit.pitch&&state.distance==cameraOrbit.distance&&state.viewOffset!=cameraOrbit.viewOffset;capture="space-pan.png";}
                if(frame==45){cameraChecks[2]=state.yaw==cameraPan.yaw&&state.pitch==cameraPan.pitch&&state.distance==cameraPan.distance&&state.viewOffset!=cameraPan.viewOffset;capture="middle-pan.png";}
                if(frame==53){cameraChecks[3]=state.viewOffset==std::array<float,3>{}&&state.yaw==cameraPan.yaw;capture="recenter.png";}
                if(capture)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.verify/capture).string().c_str());
                if(frame==60)running=false;
            }
            if(captureRock){
                if(frame==20){
                    engine::saveRockResult(options.captureRock/"export",rock->asset().lods.front(),rock->recipe());
                    bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.captureRock/"rock.png").string().c_str());
                }
                if(options.captureRockUi){
                    if((frame==25||frame==35)&&ImGui::GetDrawData()->TotalVtxCount!=0)throw std::runtime_error("Scene-only viewer submitted UI geometry");
                    if(frame==25)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.captureRock/"workbench.png").string().c_str());
                    if(frame==29)SDL_SetWindowSize(window.get(),800,600);
                    if(frame==31)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.captureRock/"test-controls.png").string().c_str());
                    if(frame==35)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.captureRock/"workbench-small.png").string().c_str());
                    if(frame==36||frame==38||frame==40){
                        auto* panel=ImGui::FindWindowByName("Rock & formation workbench");
                        if(!panel)throw std::runtime_error("Rock authoring panel missing");
                        ImGui::ActivateItemByID(panel->GetID(frame==36?"Next seed":(frame==38?"Apply recipe":"Undo")));
                    }
                    if(frame==37)rockUiChecks[0]=rock->hasPendingEdits()&&rock->recipe()==originalRecipe;
                    if(frame==39)rockUiChecks[1]=!rock->hasPendingEdits()&&rock->recipe().seed==originalRecipe.seed+1;
                    if(frame==41)rockUiChecks[2]=!rock->hasPendingEdits()&&rock->recipe()==originalRecipe;
                    if(frame==42){
                        auto* panel=ImGui::FindWindowByName("Rock & formation workbench");
                        panel->StateStorage.SetInt(ImHashStr("Member",0,panel->GetID(1)),1);
                        ImGui::SetScrollY(panel,panel->ScrollMax.y*.65f);
                    }
                    if(frame==47)bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(options.captureRock/"member-controls.png").string().c_str());
                    if(frame==52)running=false;
                }else if(frame==30)running=false;
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
            renderer.finishFrame(technical);
            if (technical) SDL_Delay(10);
        }
        ui.stop();
    }
    if(!options.saveBindings.empty()) engine::saveBindings(options.saveBindings,actions);
    texture.reset();for(auto& lease:surfaceLeases)lease.reset();textures.stop();
    saveWorld();
    streaming.reset();rock.reset();renderModel.reset();animation.reset();
    renderer.stop();
    if (!options.saveInspection.empty()) engine::saveInspection(options.saveInspection,state);
    if(options.cameraVerify){
        const bool passed=std::all_of(cameraChecks.begin(),cameraChecks.end(),[](bool value){return value;})&&renderer.callbacks.captures==5&&renderer.callbacks.errors==0;
        engine::writeDocument(options.verify/"camera.json","engine.scene-camera-verification",{{"passed",passed},{"backend",backend},{"alt_left_orbit",cameraChecks[0]},{"space_left_pan",cameraChecks[1]},{"middle_pan",cameraChecks[2]},{"recenter",cameraChecks[3]},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"simulation_advanced",false}});
        if(!passed)throw std::runtime_error("Scene camera input verification failed");
    }
    if(captureRock){
        const bool passed=renderer.callbacks.captures==(options.captureRockUi?5:1)&&renderer.callbacks.errors==0&&(!options.captureRockUi||std::all_of(rockUiChecks.begin(),rockUiChecks.end(),[](bool value){return value;}));
        engine::writeDocument(options.captureRock/"result.json","engine.rock-capture",{{"passed",passed},{"ui_checked",options.captureRockUi},{"draft_apply_undo",rockUiChecks},{"backend",backend},{"real_surfaces",materialAvailable},{"captures",renderer.callbacks.captures.load()},{"gpu_errors",renderer.callbacks.errors.load()},{"recipe",options.rock.string()},{"simulation_advanced",false}});
        if(!passed)throw std::runtime_error("Rock capture failed");
    }
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
        const bool passed=renderer.callbacks.captures==(options.environmentVerify?12u:8u) && renderer.callbacks.errors==0;
        engine::writeDocument(options.verify/(options.environmentVerify?"environment.json":"lighting.json"),"engine.lighting-verification",{{"backend",backend},{"captures",renderer.callbacks.captures.load()},
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

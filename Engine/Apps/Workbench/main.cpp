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

namespace {
struct Options { std::filesystem::path shaders, verify, inspection, saveInspection, catalog; bool buildInfo=false; };
Options parse(int argc,char** argv) {
    Options options;
    for (int i=1;i<argc;++i) {
        const std::string arg=argv[i];
        if ((arg=="--verify" || arg=="--shaders" || arg=="--inspection" || arg=="--save-inspection" || arg=="--catalog") && i+1<argc) {
            if (arg=="--verify") options.verify=argv[++i];
            else if (arg=="--inspection") options.inspection=argv[++i];
            else if (arg=="--save-inspection") options.saveInspection=argv[++i];
            else if (arg=="--catalog") options.catalog=argv[++i];
            else options.shaders=argv[++i];
        } else if (arg=="--build-info") options.buildInfo=true;
        else throw std::runtime_error("Usage: engine_workbench [--shaders directory] [--inspection file] [--save-inspection file] [--build-info] [--catalog catalog.json] [--verify new-output-directory]");
    }
    return options;
}
struct SDLSession {
    SDLSession() { SDL_SetMainReady(); if (!SDL_Init(SDL_INIT_VIDEO|SDL_INIT_EVENTS)) throw std::runtime_error(SDL_GetError()); }
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
    int selected=0,loaded=-1,channel=0;float lod=0,repeat=1;bool enabled=true;
    std::string error;
};
struct ButtonPosition { float x=0,y=0; };
ButtonPosition inspector(engine::FixtureState& state,const engine::Renderer& renderer,float milliseconds,TextureControls& textures) {
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
    ImGui::Separator();
    ImGui::TextUnformatted("Camera");
    ImGui::SliderFloat("Distance",&state.distance,2.5f,30.0f,"%.1f m");
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
    ImGui::PopItemWidth();
    ImGui::End();
    state.constrain();
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
    const bool verify=!options.verify.empty();
    if (verify) {
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
    engine::Window window(verify);
    engine::Renderer renderer;
    renderer.start(window,options.shaders);
    const std::string backend=renderer.name();
    const auto records=options.catalog.empty()?std::vector<engine::TextureRecord>{}:engine::loadTextureCatalog(options.catalog);
    engine::TextureStore textures;
    engine::TextureLease texture;
    TextureControls textureControls;textureControls.records=&records;
    if (verify && !records.empty()) {
        const auto result=engine::verifyTextureStore(textures,records.at(0));
        engine::writeDocument(options.verify/"texture-resources.json","engine.texture-verification",result);
    }
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
            if (verify && frame>530) throw std::runtime_error("Verification exceeded frame limit");
            SDL_Event event;
            float dx=0,dy=0,wheel=0;
            while (SDL_PollEvent(&event)) {
                ImGui_ImplSDL3_ProcessEvent(&event);
                if (event.type==SDL_EVENT_QUIT || event.type==SDL_EVENT_WINDOW_CLOSE_REQUESTED) { running=false; closed=true; }
                if (event.type==SDL_EVENT_MOUSE_MOTION && (event.motion.state & SDL_BUTTON_RMASK)) { dx+=event.motion.xrel; dy+=event.motion.yrel; }
                if (event.type==SDL_EVENT_MOUSE_WHEEL) wheel+=event.wheel.y;
                if (event.type==SDL_EVENT_KEY_DOWN && event.key.key==SDLK_ESCAPE && !ImGui::GetIO().WantCaptureKeyboard) running=false;
            }
            if (!running) break;
            int width=0,height=0; window.pixels(width,height);
            if (width<=0 || height<=0 || (SDL_GetWindowFlags(window.get())&SDL_WINDOW_MINIMIZED)) { SDL_Delay(16); continue; }
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
            button=inspector(state,renderer,milliseconds,textureControls);
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
            ImGui::Render();
            engine::GeometryCheck geometryCheck=engine::GeometryCheck::None;
            constexpr engine::GeometryCheck checks[]={engine::GeometryCheck::Transformed,engine::GeometryCheck::BakedReference,
                engine::GeometryCheck::Unculled,engine::GeometryCheck::FrontCull,engine::GeometryCheck::ReverseOrder,engine::GeometryCheck::Transformed};
            if (verify && frame>=155 && frame<245) geometryCheck=checks[(frame-155)/15];
            renderer.draw(state,geometryCheck,verify && frame>=265 && frame<345,showTexture?&preview:nullptr); ui.draw(ImGui::GetDrawData());
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
            bgfx::frame();
            if (verify) SDL_Delay(10);
        }
        ui.stop();
    }
    texture.reset();textures.stop();
    renderer.stop();
    if (!options.saveInspection.empty()) engine::saveInspection(options.saveInspection,state);
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

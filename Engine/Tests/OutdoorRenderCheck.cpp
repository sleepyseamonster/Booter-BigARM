#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Rendering/Renderer.h"
#include "Core/FixtureGeometry.h"
#include "Core/Visibility.h"
#include "Core/SunCascade.h"
#include <bx/math.h>
#include <iostream>

using namespace engine;
ModelData model(FixtureMesh mesh){
    ModelData data;data.nodes.push_back({});data.joints={0};SkinMatrix identity{};for(int i=0;i<4;++i)identity[i*5]=1;data.inverseBind={identity};data.metallic=0;data.baseColor={.48f,.48f,.48f,1};
    for(const auto& v:mesh){SkinVertex vertex;vertex.position={v.x,v.y,v.z};vertex.normal={v.nx,v.ny,v.nz};const auto tangent=sunUnit(sunCross(std::abs(v.ny)>.9f?SunVector{1,0,0}:SunVector{0,1,0},vertex.normal));vertex.tangent={tangent[0],tangent[1],tangent[2],1};data.vertices.push_back(vertex);data.indices.push_back(uint32_t(data.indices.size()));}
    return data;
}
int main(int argc,char** argv){try{
    if(argc<3||argc>4)throw std::runtime_error("shaders new-output-directory [bench]");
    const bool bench=argc==4;const std::filesystem::path out=argv[2];
    if(std::filesystem::exists(out))throw std::runtime_error("Choose a new output directory");std::filesystem::create_directories(out);
    SDL_SetMainReady();if(!SDL_Init(SDL_INIT_VIDEO|SDL_INIT_EVENTS))throw std::runtime_error(SDL_GetError());
    unsigned expected=0;std::string backend;
    {
    Window window(true);Renderer renderer;renderer.start(window,argv[1]);backend=renderer.name();
    {
    RenderModel cube(model(fixtureCube())),sphere(model(fixtureSphere()));
    std::vector<RenderInstance> instances;
    instances.push_back({&cube,{0,-.2f,-50},{0,-.2f,-50},0,160,true,{160,.4f,220}});
    for(int i=0;i<5;++i){float z=-float(i)*20;instances.push_back({&sphere,{float(i%2?6:-6),2,z},{float(i%2?6:-6),2,z},0,4,false,{4,4,4}});}
    instances.push_back({&cube,{45,10,0},{45,10,0},0,11,false,{2,20,2}});
    FixtureState state;state.lightAzimuth=.785398f;state.environment.sunElevation=.18f;state.exposure=1;state.ambient=.5f;state.ambientOcclusion=true;state.aoStrength=.65f;state.aoRadius=1.75f;state.contactShadows=true;state.contactStrength=.8f;state.contactDistance=1.25f;
    ScenePlacement placement;placement.streamedWorld=true;placement.physicalCharacter=true;placement.eye={0,12,25};placement.target={0,1,-30};placement.instances=&instances;
    float view[16],projection[16],vp[16];bx::mtxLookAt(view,{0,12,25},{0,1,-30},{0,1,0},bx::Handedness::Right);
    bx::mtxProj(projection,state.fieldOfView,float(renderer.width())/renderer.height(),.1f,600,bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);bx::mtxMul(vp,view,projection);
    if(visibleSphere(vp,{45,10,0},11,bgfx::getCaps()->homogeneousDepth))throw std::runtime_error("Reference caster must be outside the camera");
    for(int frame=0;frame<(bench?160:95);++frame){
        SDL_Event event;while(SDL_PollEvent(&event)){}
        if(!bench){
            state.shadows=frame>=20;
            if(frame==40)instances.pop_back();
            if(frame==60){placement.eye[0]+=.01f;placement.target[0]+=.01f;}
            if(frame==80)SDL_SetWindowSize(window.get(),960,640);
        }
        int width,height;window.pixels(width,height);renderer.resize(width,height);
        renderer.telemetry.measure(FramePhase::Draw,[&]{renderer.draw(state,GeometryCheck::None,false,nullptr,nullptr,&placement);});
        if(!bench&&(frame==10||frame==30||frame==50||frame==70||frame==90)){
            const char* names[]={"unshadowed.png","shadowed.png","without-offscreen-caster.png","camera-step.png","resized.png"};
            bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(out/names[expected++]).string().c_str());
        }
        renderer.finishFrame(false); // No artificial frame delay in either workload.
    }
    }
    renderer.stop();
    const bool passed=renderer.callbacks.errors==0&&renderer.callbacks.captures==expected;
    writeDocument(out/"result.json","engine.outdoor-render-check",{{"passed",passed},{"backend",backend},{"captures",renderer.callbacks.captures.load()},{"offscreen_caster_confirmed",true},{"benchmark",bench},{"limits","Resident synthetic opaque workload through production renderer; no gameplay, streaming or input-to-photon measurement."}});
    if(!passed)throw std::runtime_error("Outdoor renderer check failed");
    }
    SDL_Quit();
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}

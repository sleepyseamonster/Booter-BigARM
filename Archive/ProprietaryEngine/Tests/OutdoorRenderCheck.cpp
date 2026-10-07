#define SDL_MAIN_HANDLED
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include "Rendering/Renderer.h"
#include "Assets/MaterialDefinition.h"
#include "Assets/TextureCatalog.h"
#include "Core/FixtureGeometry.h"
#include "Core/Visibility.h"
#include "Core/SunCascade.h"
#include "Physics/PhysicsWorld.h"
#include <bx/math.h>
#include <algorithm>
#include <iostream>

using namespace engine;
ModelData model(FixtureMesh mesh){
    ModelData data;data.nodes.push_back({});data.joints={0};SkinMatrix identity{};for(int i=0;i<4;++i)identity[i*5]=1;data.inverseBind={identity};data.metallic=0;data.baseColor={.48f,.48f,.48f,1};
    for(const auto& v:mesh){SkinVertex vertex;vertex.position={v.x,v.y,v.z};vertex.normal={v.nx,v.ny,v.nz};vertex.uv={v.x+.5f,v.z+.5f};const auto tangent=sunUnit(sunCross(std::abs(v.ny)>.9f?SunVector{1,0,0}:SunVector{0,1,0},vertex.normal));vertex.tangent={tangent[0],tangent[1],tangent[2],1};data.vertices.push_back(vertex);data.indices.push_back(uint32_t(data.indices.size()));}
    return data;
}
int main(int argc,char** argv){try{
    const bool bench=argc==5&&std::string(argv[3])=="bench";
    if((!bench&&argc!=3)||(bench&&argc!=5))throw std::runtime_error("shaders new-output-directory [bench texture-catalog]");
    const std::filesystem::path out=argv[2];
    if(std::filesystem::exists(out))throw std::runtime_error("Choose a new output directory");std::filesystem::create_directories(out);
    SDL_SetMainReady();if(!SDL_Init(SDL_INIT_VIDEO|SDL_INIT_EVENTS))throw std::runtime_error(SDL_GetError());
    unsigned expected=0;std::string backend;RenderResourceInspection resource,retiredResource;size_t authoredDraws=0,staticInstances=0,materialInstances=0;int renderWidth=0,renderHeight=0;
    {
    Window window(true,false,bench?1920:1120,bench?1080:720,!bench);Renderer renderer;renderer.start(window,argv[1]);backend=renderer.name();
    {
    TextureStore textures;
    auto fallbackColor=textures.fallback(TextureRole::Color),fallbackNormal=textures.fallback(TextureRole::Normal),fallbackSurface=textures.fallback(TextureRole::Surface);
    SurfaceTextures base;base.projection=MaterialProjection::UV0;base.albedo=textures.resolve(fallbackColor.token());base.normal=textures.resolve(fallbackNormal.token());base.surface=textures.resolve(fallbackSurface.token());
    std::shared_ptr<const SurfaceMaterialBinding> resolved;
    if(bench){const auto records=loadTextureCatalog(argv[4]);resolved=resolveSurfaceMaterial(uvMaterial("diagnostic/uv","diagnostic/color","diagnostic/normal","diagnostic/surface",{2,2,0,0}),records,textures);base=resolved->surface();}
    std::array<SurfaceTextures,3> materials{base,base,base};materials[1].uvTransform={1,3,.2f,0};materials[2].uvTransform={4,1,0,.35f};
    SceneSurfaces surfaces{materials[0],materials[1]};
    RenderModel cube(model(fixtureCube())),sphere(model(fixtureSphere())),actor(model(fixtureCube()));
    std::vector<RenderInstance> instances;
    instances.push_back({&cube,{0,-.2f,-50},{0,-.2f,-50},0,160,true,{160,.4f,220}});
    for(int i=0;i<5;++i){float z=-float(i)*20;instances.push_back({&sphere,{float(i%2?6:-6),2,z},{float(i%2?6:-6),2,z},0,4,false,{4,4,4}});}
    instances.push_back({&cube,{45,10,0},{45,10,0},0,11,false,{2,20,2}});
    if(bench)for(int z=0;z<8;++z)for(int x=0;x<16;++x){const auto* mesh=(x+z)%3?&cube:&sphere;const std::array<float,3> position{float(x-8)*4.2f,.6f,-float(z)*7-8};instances.push_back({mesh,position,position,float((x+z)%5)*.2f,1.5f,false,{1,1,1},&materials[size_t(x+z)%materials.size()]});}
    FixtureState state;state.lightAzimuth=.785398f;state.environment.sunElevation=.18f;state.exposure=1;state.ambient=.5f;state.ambientOcclusion=true;state.aoStrength=.65f;state.aoRadius=1.75f;state.contactShadows=true;state.contactStrength=.8f;state.contactDistance=1.25f;
    const SkinMatrix identity{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};std::vector<SkinMatrix> pose{identity};
    ScenePlacement placement;placement.streamedWorld=true;placement.physicalCharacter=true;placement.eye={0,12,25};placement.target={0,1,-30};placement.instances=&instances;placement.model=&actor;placement.pose=&pose;
    auto authoredDocument=std::make_shared<AuthoringSceneDocument>("render-check");
    const auto authoredA=authoredDocument->createEntity("Mirrored nonuniform cube"),authoredB=authoredDocument->createEntity("Rotated foreground cube",authoredA);
    std::vector<SceneEntityId> hierarchy;
    if(bench)for(int i=0;i<24;++i)hierarchy.push_back(authoredDocument->createEntity("Representative child "+std::to_string(i),authoredA));
    auto authoredEdit=authoredDocument->beginTransaction(authoredDocument->version());
    SceneTransform authoredTransformA;authoredTransformA.translation={-2,2,-12};authoredTransformA.rotation={0,.258819f,0,.965926f};authoredTransformA.scale={-3,1,1.5f};
    SceneTransform authoredTransformB;authoredTransformB.translation={1,2,-9};authoredTransformB.rotation={.130526f,0,0,.991445f};authoredTransformB.scale={1,2,.7f};
    authoredEdit.setTransform(authoredA,authoredTransformA);authoredEdit.setTransform(authoredB,authoredTransformB);
    authoredEdit.setMetadata(authoredA,"engine://mesh/cube","engine://material/default","",0,true,{});
    authoredEdit.setMetadata(authoredB,"engine://mesh/sloped-solid","engine://material/default","",0,true,{});
    for(size_t i=0;i<hierarchy.size();++i){SceneTransform child;child.translation={float(int(i%8)-4)*1.5f,2.0f,float(i/8)*-2.0f};child.scale={.4f,.4f,.4f};authoredEdit.setTransform(hierarchy[i],child);authoredEdit.setMetadata(hierarchy[i],"engine://mesh/sphere","engine://material/uv-diagnostic","",uint8_t(i%3),true,{});}
    if(!authoredEdit.commit().applied)throw std::runtime_error("Cannot seed authored render check");
    RenderAssetCatalog authoredAssets;
    if(resolved){
        const std::array<double,3> low{-.5,-.5,-.5},high{.5,.5,.5};
        authoredAssets.publish({{"engine://mesh/cube"},BuiltinRenderMesh::Cube,low,high,true,true});
        authoredAssets.publish({{"engine://mesh/sloped-solid"},BuiltinRenderMesh::SlopedSolid,low,high,true,true});
        authoredAssets.publish({{"engine://mesh/sphere"},BuiltinRenderMesh::Sphere,low,high,true,true});
        RenderMaterialAsset material;material.id={"engine://material/uv-diagnostic"};material.normalMapped=true;material.surface=resolved;authoredAssets.publish(std::move(material));
    }
    auto authored=extractRenderScene(authoredDocument,authoredAssets,false);if(bench)placement.authored=&authored;
    PhysicsWorld physics;physics.box("authored:representative-floor",{0,-.5f,-20},{80,.5f,100});const auto dynamic=physics.capsule("authored:representative-dynamic",{0,4,-12},.4f,.6f,true);
    float view[16],projection[16],vp[16];bx::mtxLookAt(view,{0,12,25},{0,1,-30},{0,1,0},bx::Handedness::Right);
    bx::mtxProj(projection,state.fieldOfView,float(renderer.width())/renderer.height(),.1f,600,bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);bx::mtxMul(vp,view,projection);
    if(visibleSphere(vp,{45,10,0},11,bgfx::getCaps()->homogeneousDepth))throw std::runtime_error("Reference caster must be outside the camera");
    for(int frame=0;frame<(bench?1120:135);++frame){
        SDL_Event event;while(SDL_PollEvent(&event)){}
        if(!bench){
            state.shadows=frame>=20;
            if(frame==40)instances.pop_back();
            if(frame==60){placement.eye[0]+=.01f;placement.target[0]+=.01f;}
            if(frame==80)SDL_SetWindowSize(window.get(),960,640);
            if(frame==96){placement.instances=nullptr;placement.authored=&authored;}
            if(frame==110)std::reverse(authored.draws.begin(),authored.draws.end());
        }
        if(bench&&frame==120)renderer.telemetry.enable(true); // Exclude allocation/shader warmup; retain exactly 1,000 measured frames.
        renderer.telemetry.measure(FramePhase::Physics,[&]{physics.step(1.0f/60.0f);});
        if(const auto position=physics.position(dynamic);position&&bench){instances.back().offset=*position;instances.back().boundsCenter=*position;}
        int width,height;window.pixels(width,height);renderer.resize(bench?1920:width,bench?1080:height);
        renderer.telemetry.measure(FramePhase::Draw,[&]{renderer.draw(state,GeometryCheck::None,false,nullptr,&surfaces,&placement);});
        if(!bench&&(frame==10||frame==30||frame==50||frame==70||frame==90)){
            const char* names[]={"unshadowed.png","shadowed.png","without-offscreen-caster.png","camera-step.png","resized.png"};
            bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(out/names[expected++]).string().c_str());
        }
        if(!bench&&(frame==105||frame==125)){
            const char* name=frame==105?"authored-order-a.png":"authored-order-b.png";
            bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(out/name).string().c_str());++expected;
        }
        if(bench&&frame==1100){bgfx::requestScreenShot(BGFX_INVALID_HANDLE,(out/"representative.png").string().c_str());++expected;}
        renderer.finishFrame(false); // No artificial frame delay in either workload.
    }
    resource=renderer.resourceInspection();renderWidth=renderer.width();renderHeight=renderer.height();authoredDraws=authored.draws.size();staticInstances=instances.size();materialInstances=materials.size();
    retiredResource=resource;
    if(bench){
        state.ambientOcclusion=false;state.contactShadows=false;
        renderer.draw(state,GeometryCheck::None,false,nullptr,&surfaces,&placement);
        renderer.finishFrame(false);
        retiredResource=renderer.resourceInspection();
    }
    }
    renderer.stop();
    const bool passed=renderer.callbacks.errors==0&&renderer.callbacks.captures==expected;
    writeDocument(out/"result.json","engine.outdoor-render-check",{{"passed",passed},{"backend",backend},{"captures",renderer.callbacks.captures.load()},{"width",renderWidth},{"height",renderHeight},
        {"offscreen_caster_confirmed",true},{"authored_affine_draws",authoredDraws},{"authored_draw_order_pair",!bench},{"benchmark",bench},{"measured_frames",bench?1000:0},{"static_instances",staticInstances},
        {"skinned_actors",1},{"dynamic_bodies",1},{"material_instances",materialInstances},{"active_target_bytes",resource.currentTargetBytes},{"post_retirement_target_bytes",retiredResource.currentTargetBytes},{"target_high_water_bytes",resource.targetHighWaterBytes},
        {"resize_overlap_high_water_bytes",resource.resizeOverlapHighWaterBytes},{"auxiliary_targets",resource.auxiliaryTargets},{"target_degradation",resource.degradation},
        {"limits","Fixed synthetic opaque runtime workload through the production renderer; no gameplay, streaming I/O, input-to-photon or Windows performance measurement."}});
    if(!passed)throw std::runtime_error("Outdoor renderer check failed");
    }
    SDL_Quit();
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}

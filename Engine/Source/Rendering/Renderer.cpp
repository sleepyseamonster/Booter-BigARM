#include "Rendering/Renderer.h"
#include "Rendering/RenderViews.h"
#include "Core/FixtureGeometry.h"
#include "Core/Color.h"
#include "Core/Visibility.h"
#include "Core/SunCascade.h"
#include <bx/math.h>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cmath>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <vector>

namespace engine {
static bgfx::ShaderHandle loadShader(const std::filesystem::path& path) {
    std::ifstream file(path, std::ios::binary | std::ios::ate);
    if (!file) throw std::runtime_error("Missing shader: " + path.string());
    const auto length = file.tellg();
    if (length <= 0 || length > 16*1024*1024) throw std::runtime_error("Invalid shader size: " + path.string());
    std::vector<char> bytes(static_cast<size_t>(length));
    file.seekg(0); file.read(bytes.data(), length);
    if (!file) throw std::runtime_error("Cannot read shader: " + path.string());
    auto shader = bgfx::createShader(bgfx::copy(bytes.data(), uint32_t(bytes.size())));
    if (!bgfx::isValid(shader)) throw std::runtime_error("Cannot create shader: " + path.string());
    bgfx::setName(shader, path.filename().string().c_str());
    return shader;
}
bgfx::ProgramHandle loadProgram(const std::filesystem::path& directory, const char* vertex, const char* fragment) {
    auto vs = loadShader(directory / vertex);
    bgfx::ShaderHandle fs = BGFX_INVALID_HANDLE;
    try { fs = loadShader(directory / fragment); }
    catch (...) { bgfx::destroy(vs); throw; }
    auto program = bgfx::createProgram(vs, fs, true);
    if (!bgfx::isValid(program)) throw std::runtime_error("Shader program link failed");
    return program;
}
void CaptureCallbacks::fatal(const char* file, uint16_t line, bgfx::Fatal::Enum, const char* message) {
    std::fprintf(stderr, "GPU fatal %s:%u: %s\n", file, line, message);
    ++errors;
    std::abort();
}
void CaptureCallbacks::traceVargs(const char*, uint16_t, const char* format, va_list arguments) {
    std::vfprintf(stderr, format, arguments);
}
void CaptureCallbacks::screenShot(const char* path, uint32_t width, uint32_t height, uint32_t pitch,
    bgfx::TextureFormat::Enum format, const void* data, uint32_t size, bool yflip) {
    if ((format != bgfx::TextureFormat::BGRA8 && format != bgfx::TextureFormat::RGBA8) ||
        !width || !height || pitch < width*4 || uint64_t(pitch)*height > size) {
        ++errors; std::fprintf(stderr, "Unsupported screenshot data\n"); return;
    }
    std::vector<uint8_t> pixels(size_t(width)*height*4);
    for (uint32_t row=0; row<height; ++row) {
        const auto sourceRow = yflip ? height-1-row : row;
        std::memcpy(pixels.data()+size_t(row)*width*4, static_cast<const uint8_t*>(data)+size_t(sourceRow)*pitch, size_t(width)*4);
    }
    auto* surface = SDL_CreateSurfaceFrom(int(width), int(height),
        format == bgfx::TextureFormat::BGRA8 ? SDL_PIXELFORMAT_BGRA32 : SDL_PIXELFORMAT_RGBA32,
        pixels.data(), int(width)*4);
    if (!surface || !SDL_SavePNG(surface, path)) {
        ++errors; std::fprintf(stderr, "Screenshot save failed: %s\n", SDL_GetError());
    } else { ++captures; std::fprintf(stdout, "CAPTURE %s %ux%u\n", path, width, height); }
    if (surface) SDL_DestroySurface(surface);
}
Renderer::~Renderer() { stop(); }
void Renderer::start(const Window& window, const std::filesystem::path& shaders, const RenderConfiguration& configuration) {
    window.pixels(width_, height_);
    if (!window.nativeHandle() || width_ <= 0 || height_ <= 0) throw std::runtime_error("Invalid window surface");
    configuration_=configuration;
    telemetry.enable(!configuration_.trace.empty());
    bgfx::Init init;
#ifdef __APPLE__
    init.type = bgfx::RendererType::Metal;
#else
    init.type = bgfx::RendererType::Direct3D11;
#endif
    init.fallback = false;
    init.debug = true;
    init.profile = true;
    init.callback = &callbacks;
    init.swapChain.nwh = window.nativeHandle();
    init.swapChain.width = uint32_t(width_);
    init.swapChain.height = uint32_t(height_);
    init.reset = configuration_.present==PresentMode::VSync?BGFX_RESET_VSYNC:BGFX_RESET_NONE;
    if (!bgfx::init(init)) throw std::runtime_error("Requested GPU backend failed to initialize");
    started_ = true;
    if (bgfx::getRendererType() != init.type) throw std::runtime_error("Unexpected GPU backend");
    skinProgram_=loadProgram(shaders,"vs_skin.bin","fs_scene.bin");
    skinShadowProgram_=loadProgram(shaders,"vs_skin_shadow.bin","fs_shadow.bin");
    joints_=bgfx::createUniform("u_joints",bgfx::UniformType::Mat4,64);
    program_ = loadProgram(shaders, "vs_scene.bin", "fs_scene.bin");
    shadowProgram_=loadProgram(shaders,"vs_shadow.bin","fs_shadow.bin");
    shadowMatrix_=bgfx::createUniform("u_shadowMatrix",bgfx::UniformType::Mat4);
    shadowFarMatrix_=bgfx::createUniform("u_shadowFarMatrix",bgfx::UniformType::Mat4);
    shadowRange_=bgfx::createUniform("u_shadowRange",bgfx::UniformType::Vec4);
    shadowCamera_=bgfx::createUniform("u_shadowCamera",bgfx::UniformType::Vec4);
    shadowOptions_=bgfx::createUniform("u_shadowOptions",bgfx::UniformType::Vec4);
    shadowSampler_=bgfx::createUniform("s_shadow",bgfx::UniformType::Sampler);
    eye_=bgfx::createUniform("u_eye",bgfx::UniformType::Vec4);
    surfaceParams_=bgfx::createUniform("u_surfaceParams",bgfx::UniformType::Vec4);
    albedoSampler_=bgfx::createUniform("s_albedo",bgfx::UniformType::Sampler);
    normalSampler_=bgfx::createUniform("s_normal",bgfx::UniformType::Sampler);
    surfaceSampler_=bgfx::createUniform("s_surface",bgfx::UniformType::Sampler);
    materialMapping_=bgfx::createUniform("u_materialMapping",bgfx::UniformType::Vec4);
    materialUvOffset_=bgfx::createUniform("u_materialUvOffset",bgfx::UniformType::Vec4);
    for (auto uniform:{shadowMatrix_,shadowFarMatrix_,shadowRange_,shadowCamera_,shadowOptions_,shadowSampler_,eye_,surfaceParams_,materialMapping_,materialUvOffset_,albedoSampler_,normalSampler_,surfaceSampler_})
        if (!bgfx::isValid(uniform)) throw std::runtime_error("Lighting uniform allocation failed");
    const char* layerNames[]={"s_topColor","s_topNormal","s_topSurface","s_bottomColor","s_bottomNormal","s_bottomSurface","s_gritColor","s_gritNormal","s_gritSurface","s_cracks"};
    for(size_t i=0;i<layerSamplers_.size();++i){layerSamplers_[i]=bgfx::createUniform(layerNames[i],bgfx::UniformType::Sampler);if(!bgfx::isValid(layerSamplers_[i]))throw std::runtime_error("Rock layer sampler allocation failed");}
    layerParams_=bgfx::createUniform("u_rockLayers",bgfx::UniformType::Vec4,5);
    if(!bgfx::isValid(layerParams_))throw std::runtime_error("Rock layer parameters allocation failed");
    if(bgfx::getCaps()->limits.maxTextureSize<4096)throw std::runtime_error("Two-cascade atlas requires a 4096-wide target");
    constexpr uint64_t shadowFlags=BGFX_TEXTURE_RT|BGFX_SAMPLER_U_CLAMP|BGFX_SAMPLER_V_CLAMP|BGFX_SAMPLER_MIN_POINT|BGFX_SAMPLER_MAG_POINT;
    if (!bgfx::isTextureValid(0,false,1,bgfx::TextureFormat::R32F,shadowFlags)) throw std::runtime_error("Shadow R32F target unsupported");
    bgfx::TextureHandle shadowTargets[]={
        bgfx::createTexture2D(4096,2048,false,1,bgfx::TextureFormat::R32F,shadowFlags),
        bgfx::createTexture2D(4096,2048,false,1,bgfx::TextureFormat::D24S8,BGFX_TEXTURE_RT)};
    if (bgfx::isValid(shadowTargets[0]) && bgfx::isValid(shadowTargets[1])) shadow_=bgfx::createFrameBuffer(2,shadowTargets,true);
    if (!bgfx::isValid(shadow_)) {
        for(auto texture:shadowTargets) if(bgfx::isValid(texture)) bgfx::destroy(texture);
        throw std::runtime_error("Shadow target allocation failed");
    }
    bgfx::setViewName(views::shadow,"Sun near cascade");
    bgfx::setViewName(views::shadowFar,"Sun far cascade");
    material_ = bgfx::createUniform("u_material", bgfx::UniformType::Vec4);
    light_ = bgfx::createUniform("u_light", bgfx::UniformType::Vec4);
    if (!bgfx::isValid(material_) || !bgfx::isValid(light_)) throw std::runtime_error("Scene uniform allocation failed");
    normal_ = bgfx::createUniform("u_normalMatrix", bgfx::UniformType::Mat4);
    options_ = bgfx::createUniform("u_sceneOptions", bgfx::UniformType::Vec4);
    if (!bgfx::isValid(normal_) || !bgfx::isValid(options_)) throw std::runtime_error("Geometry uniform allocation failed");
    textureProgram_=loadProgram(shaders,"vs_fullscreen.bin","fs_texture_preview.bin");
    textureOptions_=bgfx::createUniform("u_textureOptions",bgfx::UniformType::Vec4);
    previewSampler_=bgfx::createUniform("s_preview",bgfx::UniformType::Sampler);
    if (!bgfx::isValid(textureOptions_) || !bgfx::isValid(previewSampler_)) throw std::runtime_error("Texture preview resources failed");
    displayProgram_=loadProgram(shaders,"vs_fullscreen.bin","fs_display.bin");
    skyProgram_=loadProgram(shaders,"vs_fullscreen.bin","fs_sky.bin");
    environment_=bgfx::createUniform("u_environment",bgfx::UniformType::Vec4,4);
    inverseViewProjection_=bgfx::createUniform("u_inverseViewProjection",bgfx::UniformType::Mat4);
    if(!bgfx::isValid(environment_)||!bgfx::isValid(inverseViewProjection_))throw std::runtime_error("Environment resources failed");
    calibrationProgram_=loadProgram(shaders,"vs_fullscreen.bin","fs_calibration.bin");
    display_=bgfx::createUniform("u_display",bgfx::UniformType::Vec4);
    sceneSampler_=bgfx::createUniform("s_scene",bgfx::UniformType::Sampler);
    struct FullscreenVertex { float x,y,z,u,v; };
    const FullscreenVertex vertices[]={{-1,-1,0,0,1},{3,-1,0,2,1},{-1,3,0,0,-1}};
    bgfx::VertexLayout layout;
    layout.begin().add(bgfx::Attrib::Position,3,bgfx::AttribType::Float).add(bgfx::Attrib::TexCoord0,2,bgfx::AttribType::Float).end();
    fullscreen_=bgfx::createVertexBuffer(bgfx::copy(vertices,sizeof(vertices)),layout);
    if (!bgfx::isValid(display_) || !bgfx::isValid(sceneSampler_) || !bgfx::isValid(fullscreen_))
        throw std::runtime_error("Display resources failed");
    resizeTargets(width_,height_);
    bgfx::setViewMode(views::display,bgfx::ViewMode::Sequential);
    bgfx::setViewName(views::display,"Linear HDR to SDR display");
    rebuildMesh();
    // Preserve submission order for the depth-order diagnostic; this is a small fixture.
    bgfx::setViewMode(views::scene, bgfx::ViewMode::Sequential);
    bgfx::setViewName(views::scene, "Perspective fixture");
    std::cout << "RENDERER " << name() << " framebuffer=" << width_ << 'x' << height_ << '\n';
}
void Renderer::resizeTargets(int width,int height) {
    const uint64_t flags=BGFX_TEXTURE_RT|BGFX_SAMPLER_U_CLAMP|BGFX_SAMPLER_V_CLAMP|BGFX_SAMPLER_MIN_POINT|BGFX_SAMPLER_MAG_POINT;
    if (!bgfx::isTextureValid(0,false,1,bgfx::TextureFormat::RGBA16F,flags) ||
        !bgfx::isTextureValid(0,false,1,bgfx::TextureFormat::D24S8,BGFX_TEXTURE_RT))
        throw std::runtime_error("RGBA16F scene/depth targets unsupported");
    bgfx::TextureHandle attachments[]={BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE};
    bgfx::FrameBufferHandle next=BGFX_INVALID_HANDLE;
    try {
        attachments[0]=bgfx::createTexture2D(uint16_t(width),uint16_t(height),false,1,bgfx::TextureFormat::RGBA16F,flags);
        attachments[1]=bgfx::createTexture2D(uint16_t(width),uint16_t(height),false,1,bgfx::TextureFormat::D24S8,BGFX_TEXTURE_RT);
        if (!bgfx::isValid(attachments[0]) || !bgfx::isValid(attachments[1])) throw std::runtime_error("Scene target allocation failed");
        next=bgfx::createFrameBuffer(2,attachments,true);
        if (!bgfx::isValid(next)) throw std::runtime_error("Scene framebuffer allocation failed");
    } catch (...) {
        for (auto texture:attachments) if (bgfx::isValid(texture)) bgfx::destroy(texture);
        throw;
    }
    if (bgfx::isValid(scene_)) bgfx::destroy(scene_);
    scene_=next;
    bgfx::setViewFrameBuffer(views::scene,scene_);
}
void Renderer::resize(int width, int height) {
    if (width <= 0 || height <= 0 || (width == width_ && height == height_)) return;
    if (width > 65535 || height > 65535) throw std::runtime_error("Framebuffer exceeds view limits");
    resizeTargets(width,height);
    bgfx::SwapChain swap;
    swap.width = uint32_t(width); swap.height = uint32_t(height);
    bgfx::reset(configuration_.present==PresentMode::VSync?BGFX_RESET_VSYNC:BGFX_RESET_NONE, &swap);
    width_ = width; height_ = height;
    std::cout << "RESIZE " << width_ << 'x' << height_ << '\n';
}
namespace {
Matrix4 subjectTransform(const FixtureState& state) {
    Matrix4 transform;
    bx::mtxSRT(transform.data(),state.objectScale[0],state.objectScale[1],state.objectScale[2],
        0,state.objectYaw,0,0,0.75f,0);
    return transform;
}
}
void Renderer::rebuildMesh() {
    const auto sloped=fixtureSlopedSolid();
    const std::array<FixtureMesh,5> data{fixtureCube(),sloped,fixtureSphere(),
        bakeFlatReference(sloped,subjectTransform(geometryProofState())),fixtureCapsule()};
    std::array<bgfx::VertexBufferHandle,5> replacement{{BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE}};
    bgfx::VertexLayout layout;
    layout.begin().add(bgfx::Attrib::Position,3,bgfx::AttribType::Float).add(bgfx::Attrib::Normal,3,bgfx::AttribType::Float).end();
    try {
        for (size_t i=0;i<data.size();++i) {
            replacement[i]=bgfx::createVertexBuffer(bgfx::copy(data[i].data(),uint32_t(data[i].size()*sizeof(FixtureVertex))),layout);
            if (!bgfx::isValid(replacement[i])) throw std::runtime_error("Mesh allocation failed");
        }
    } catch (...) {
        for (auto handle : replacement) if (bgfx::isValid(handle)) bgfx::destroy(handle);
        throw;
    }
    for (auto handle : meshes_) if (bgfx::isValid(handle)) bgfx::destroy(handle);
    meshes_=replacement;
    const char* names[]={"Reference cube", "Reference sloped solid", "Reference sphere", "CPU-baked flat-normal reference","Character capsule"};
    for (size_t i=0;i<meshes_.size();++i) bgfx::setName(meshes_[i],names[i]);
}
void Renderer::draw(const FixtureState& state, GeometryCheck check, bool calibration, const TexturePreview* preview, const SceneSurfaces* surfaces, const ScenePlacement* placement) {
    auto eye=state.eye();
    const auto* rockModel=placement?placement->rock:nullptr;
    const bool physical=placement && placement->physicalCharacter;
    const bool streamed=placement&&placement->streamedWorld;
    const bool rockOnly=rockModel&&!physical;
    const auto offset=rockOnly?placement->rockOffset:(placement?placement->offset:std::array<float,3>{});
    for(size_t i=0;i<3;++i) eye[i]+=offset[i];
    if(rockOnly)eye[1]+=placement->rockFocusHeight-.85f;
    const auto* model=placement?placement->model:nullptr;
    if(model && (!placement->pose||placement->pose->size()!=model->jointCount))throw std::runtime_error("Skin pose does not match model");
    const bool gpuSkin=model && !placement->cpuReference;
    if(physical) eye=placement->eye;
    const auto target=physical?placement->target:std::array<float,3>{offset[0]+state.viewOffset[0],(rockOnly?placement->rockFocusHeight:.85f)+offset[1]+state.viewOffset[1],offset[2]+state.viewOffset[2]};
    float view[16], projection[16];
    bx::mtxLookAt(view,{eye[0],eye[1],eye[2]},{target[0],target[1],target[2]},{0,1,0},bx::Handedness::Right);
    bx::mtxProj(projection,state.fieldOfView,float(width_)/float(height_),0.1f,streamed?600.0f:100.0f,
        bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);
    float viewProjection[16];bx::mtxMul(viewProjection,view,projection);
    // reset clears view framebuffer bindings; restore ownership on every frame.
    bgfx::setViewFrameBuffer(views::scene,scene_);
    bgfx::setViewRect(views::scene,0,0,uint16_t(width_),uint16_t(height_));
    const float clear[]={srgbToLinear(28.0f/255),srgbToLinear(37.0f/255),srgbToLinear(50.0f/255),1};
    bgfx::setPaletteColor(0,clear);
    bgfx::setViewClear(views::scene,BGFX_CLEAR_COLOR|BGFX_CLEAR_DEPTH,1.0f,0,uint8_t(0));
    bgfx::setViewTransform(views::scene,view,projection);
    bgfx::touch(views::scene);
    const float elevation=state.environment.sunElevation;
    const float light[4]={std::sin(state.lightAzimuth)*std::cos(elevation),std::sin(elevation),std::cos(state.lightAzimuth)*std::cos(elevation),state.lightIntensity};
    float environment[16]{};
    const std::array<std::array<float,3>,4> colors{state.environment.sunColor,state.environment.zenith,state.environment.horizon,state.environment.ground};
    for(size_t i=0;i<4;++i)for(size_t j=0;j<3;++j)environment[i*4+j]=colors[i][j];
    float lightView[2][16],lightProjection[2][16],lightViewProjection[2][16],shadowMatrix[2][16];
    const auto direction=sunUnit(SunVector{target[0]-eye[0],target[1]-eye[1],target[2]-eye[2]});
    const float shadowEnd=streamed?128.0f:80.0f;
    const float shadowRange[]={21.6f,24.0f,shadowEnd*.9f,shadowEnd};
    const float shadowCamera[]={direction[0],direction[1],direction[2],0};
    const auto* caps=bgfx::getCaps();
    const float crop[]={0.5f,0,0,0, 0,caps->originBottomLeft?0.5f:-0.5f,0,0,
        0,0,caps->homogeneousDepth?0.5f:1.0f,0, 0.5f,0.5f,caps->homogeneousDepth?0.5f:0.0f,1};
    float biases[2];
    for(unsigned cascade=0;cascade<2;++cascade) {
        const auto fit=fitSunCascade(eye,direction,{light[0],light[1],light[2]},state.fieldOfView,float(width_)/float(height_),cascade?21.6f:.1f,cascade?shadowEnd:24.f,2048);
        const bx::Vec3 center{fit.center[0],fit.center[1],fit.center[2]},lightDirection{light[0],light[1],light[2]};
        const float depth=2*(fit.radius+96.f);
        const auto lightEye=bx::add(center,bx::mul(lightDirection,fit.radius+96.f));
        bx::mtxLookAt(lightView[cascade],lightEye,center,{0,1,0},bx::Handedness::Right);
        bx::mtxOrtho(lightProjection[cascade],-fit.radius,fit.radius,-fit.radius,fit.radius,.1f,depth,0,caps->homogeneousDepth,bx::Handedness::Right);
        bx::mtxMul(lightViewProjection[cascade],lightView[cascade],lightProjection[cascade]);
        bx::mtxMul(shadowMatrix[cascade],lightViewProjection[cascade],crop);
        biases[cascade]=state.shadowBias*20.f/(depth-.1f); // Convert reference bias to world-space distance.
    }
    const float shadowOptions[]={state.shadows?1.0f:0.0f,biases[0],1.0f/2048,biases[1]};
    const float eyePosition[]={eye[0],eye[1],eye[2],0};
    uint64_t cull=BGFX_STATE_CULL_CW; // Authored fixture triangles are outward CCW.
    if (check==GeometryCheck::Unculled) cull=0;
    if (check==GeometryCheck::FrontCull) cull=BGFX_STATE_CULL_CCW;
    auto submit=[&](int mesh,const Matrix4& transform,const float* color,const SurfaceTextures* surface=nullptr,const RenderModel* instanceModel=nullptr) {
        const auto normal=normalMatrix(transform);
        bgfx::setTransform(transform.data());
        const auto* resource=mesh==-3?instanceModel:(mesh==-2?rockModel:model);
        if(mesh<0) {resource->bind(mesh==-1&&placement->cpuReference);if(mesh==-1&&gpuSkin)bgfx::setUniform(joints_,placement->pose->data(),uint16_t(model->jointCount));}
        else bgfx::setVertexBuffer(0,meshes_.at(size_t(mesh)));
        const bool textured=mesh!=-1 && surface && state.surfaceTextures && bgfx::isValid(surface->albedo) && bgfx::isValid(surface->normal) && bgfx::isValid(surface->surface);
        const float linear[]={textured?1.0f:(mesh<0?resource->color[0]:srgbToLinear(color[0])),textured?1.0f:(mesh<0?resource->color[1]:srgbToLinear(color[1])),textured?1.0f:(mesh<0?resource->color[2]:srgbToLinear(color[2])),color[3]};
        const float options[]={state.showNormals?1.0f:0.0f,textured?1.0f:0.0f,surface&&surface->material.geologyMm?1000.f/surface->material.geologyMm:state.textureScale,state.ambient};
        const float params[]={mesh<0?resource->roughness:state.roughness,mesh<0?resource->metallic:state.metallic,state.normalStrength,surface && surface->packedSurface?1.0f:0.0f};
        const float mapping[]={surface&&surface->mapping==MaterialMapping::UV?1.0f:0.0f,surface?surface->uvTransform[0]:1.0f,surface?surface->uvTransform[1]:1.0f,0.0f};
        const float uvOffset[]={surface?surface->uvTransform[2]:0.0f,surface?surface->uvTransform[3]:0.0f,0.0f,0.0f};
        bgfx::setUniform(material_,linear); bgfx::setUniform(light_,light);bgfx::setUniform(environment_,environment,4);
        bgfx::setUniform(shadowMatrix_,shadowMatrix[0]);bgfx::setUniform(shadowFarMatrix_,shadowMatrix[1]);
        bgfx::setUniform(shadowRange_,shadowRange);bgfx::setUniform(shadowCamera_,shadowCamera);bgfx::setUniform(shadowOptions_,shadowOptions);
        bgfx::setUniform(eye_,eyePosition);bgfx::setUniform(surfaceParams_,params);bgfx::setUniform(materialMapping_,mapping);bgfx::setUniform(materialUvOffset_,uvOffset);
        bgfx::setTexture(0,shadowSampler_,bgfx::getTexture(shadow_));
        const auto* bound=surface?surface:(surfaces?&surfaces->rock:nullptr);
        if (bound) {
            bgfx::setTexture(1,albedoSampler_,bound->albedo);
            bgfx::setTexture(2,normalSampler_,bound->normal);
            bgfx::setTexture(3,surfaceSampler_,bound->surface);
        }
        const bool layered=textured&&surface->layered;
        const auto material=surface?surface->material:RockMaterial{};
        const float layers[20]={textured&&surface->terrainBlend?(surface->terrainNatural?3.f:2.f):(layered?1.f:0.f),material.grit*.001f,material.shale*.001f,material.cracks*.001f,
            material.dust*.001f,material.variation*.001f,material.worn*.001f,surface?surface->seed:0.f,
            transform[12],transform[13],transform[14],0,
            material.sideShale*.001f,material.topShale*.001f,0,0,
            material.dustColor[0],material.dustColor[1],material.dustColor[2],0};
        bgfx::setUniform(layerParams_,layers,5);
        if(bound)for(size_t i=0;i<layerSamplers_.size();++i){const auto handle=bgfx::isValid(bound->layers[i])?bound->layers[i]:bound->albedo;bgfx::setTexture(uint8_t(i+4),layerSamplers_[i],handle);}
        bgfx::setUniform(normal_,normal.data()); bgfx::setUniform(options_,options);
        bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A|BGFX_STATE_WRITE_Z|BGFX_STATE_DEPTH_TEST_LESS|BGFX_STATE_MSAA|cull);
        bgfx::submit(views::scene,mesh==-1&&gpuSkin?skinProgram_:program_);
    };
    const float ground[4]={0.28f,0.31f,0.34f,1};
    const float color[4]={state.color[0],state.color[1],state.color[2],1};
    const float marker[4]={placement&&placement->markerActive?.9f:.33f,placement&&placement->markerActive?.7f:.67f,placement&&placement->markerActive?.15f:.64f,1};
    Matrix4 groundTransform, markerTransform,rockTransform;
    if(rockModel)bx::mtxSRT(rockTransform.data(),1,1,1,0,0,0,placement->rockOffset[0],placement->rockOffset[1],placement->rockOffset[2]);
    bx::mtxSRT(groundTransform.data(),20,.1f,20,0,0,0,0,-.05f,0);
    bx::mtxSRT(markerTransform.data(),.35f,1.8f,.35f,0,0,0,2.1f,.9f,0);
    auto subject=subjectTransform(state);
    if(physical) bx::mtxSRT(subject.data(),1,1,1,0,state.objectYaw,0,0,.75f,0);
    if(model) bx::mtxSRT(subject.data(),1,1,1,0,state.objectYaw,0,0,physical?-.15f:0,0);
    for(size_t i=0;i<3;++i) subject[12+i]+=offset[i];
    const bool baked=check==GeometryCheck::BakedReference;
    const int subjectMesh=model?-1:(physical?4:(baked?3:state.mesh));
    if (baked) bx::mtxIdentity(subject.data());
    if (state.shadows && !state.showNormals && !calibration && !preview) {
        for(unsigned cascade=0;cascade<2;++cascade) {
        const auto shadowView=cascade?views::shadowFar:views::shadow;
        bgfx::setViewFrameBuffer(shadowView,shadow_);
        bgfx::setViewRect(shadowView,uint16_t(cascade*2048),0,2048,2048);
        bgfx::setViewClear(shadowView,BGFX_CLEAR_COLOR|BGFX_CLEAR_DEPTH,0xffffffff,1.0f,0);
        bgfx::setViewTransform(shadowView,lightView[cascade],lightProjection[cascade]);
        bgfx::touch(shadowView);
        auto cast=[&](int mesh,const Matrix4& transform,const RenderModel* instanceModel=nullptr) {
            bgfx::setTransform(transform.data());
            const auto* resource=mesh==-3?instanceModel:(mesh==-2?rockModel:model);
            if(mesh<0) {resource->bind(mesh==-1&&placement->cpuReference);if(mesh==-1&&gpuSkin)bgfx::setUniform(joints_,placement->pose->data(),uint16_t(model->jointCount));}
            else bgfx::setVertexBuffer(0,meshes_.at(size_t(mesh)));
            bgfx::setState(BGFX_STATE_WRITE_R|BGFX_STATE_WRITE_Z|BGFX_STATE_DEPTH_TEST_LESS|BGFX_STATE_CULL_CW);
            bgfx::submit(shadowView,mesh==-1&&gpuSkin?skinShadowProgram_:shadowProgram_);
        };
        if(!streamed)cast(0,groundTransform);if(!rockOnly)cast(subjectMesh,subject);cast(0,markerTransform);if(rockModel)cast(-2,rockTransform);
        if(placement&&placement->instances)for(const auto& instance:*placement->instances){
            if(!visibleSphere(lightViewProjection[cascade],instance.boundsCenter,instance.boundsRadius,caps->homogeneousDepth))continue;
            Matrix4 transform;bx::mtxSRT(transform.data(),instance.scale[0],instance.scale[1],instance.scale[2],0,instance.yaw,0,instance.offset[0],instance.offset[1],instance.offset[2]);cast(-3,transform,instance.model);
        }
        } // cascade loop
    }
    if(state.environment.sky&&!state.showNormals&&!preview&&!calibration) {
        float inverse[16];bx::mtxInverse(inverse,viewProjection);
        const float skyDisplay[]={0,0,0,0};
        const float skyEye[]={eye[0],eye[1],eye[2],0};
        bgfx::setUniform(display_,skyDisplay);bgfx::setUniform(inverseViewProjection_,inverse);
        bgfx::setUniform(environment_,environment,4);bgfx::setUniform(eye_,skyEye);bgfx::setUniform(light_,light);
        bgfx::setVertexBuffer(0,fullscreen_);bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A);
        bgfx::submit(views::scene,skyProgram_);
    }
    const auto* rock=surfaces?&surfaces->rock:nullptr;
    const auto* soil=surfaces?&surfaces->ground:nullptr;
    if (preview && bgfx::isValid(preview->texture)) {
        const float options[]={preview->lod,preview->channel,preview->srgb?1.0f:0.0f,preview->repeat};
        const float display[]={0,0,0,0};bgfx::setUniform(display_,display);
        bgfx::setUniform(textureOptions_,options);
        bgfx::setTexture(0,previewSampler_,preview->texture,BGFX_SAMPLER_MIN_POINT|BGFX_SAMPLER_MAG_POINT|BGFX_SAMPLER_MIP_POINT);
        bgfx::setVertexBuffer(0,fullscreen_);bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A);
        bgfx::submit(views::scene,textureProgram_);
    } else if (calibration) {
        const float display[]={0,0,0,0};
        bgfx::setUniform(display_,display);
        bgfx::setVertexBuffer(0,fullscreen_); bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A);
        bgfx::submit(views::scene,calibrationProgram_);
    } else if (check==GeometryCheck::ReverseOrder) {
        submit(0,markerTransform,marker); if(!rockOnly)submit(subjectMesh,subject,color,rock); if(!streamed)submit(0,groundTransform,ground,soil);if(rockModel)submit(-2,rockTransform,color,rock);
    } else {
        if(!streamed)submit(0,groundTransform,ground,soil); if(!rockOnly)submit(subjectMesh,subject,color,rock); submit(0,markerTransform,marker);if(rockModel)submit(-2,rockTransform,color,rock);
    }
    if(!preview&&!calibration&&placement&&placement->instances)for(const auto& instance:*placement->instances){
        if(!visibleSphere(viewProjection,instance.boundsCenter,instance.boundsRadius,bgfx::getCaps()->homogeneousDepth))continue;
        Matrix4 transform;bx::mtxSRT(transform.data(),instance.scale[0],instance.scale[1],instance.scale[2],0,instance.yaw,0,instance.offset[0],instance.offset[1],instance.offset[2]);submit(-3,transform,color,instance.ground?soil:rock,instance.model);
    }
    const float display[]={state.exposure,state.showNormals && !calibration && !preview?1.0f:0.0f,bgfx::getCaps()->originBottomLeft?1.0f:0.0f,state.environment.toneMapping&&!calibration&&!preview?1.0f:0.0f};
    bgfx::setViewRect(views::display,0,0,uint16_t(width_),uint16_t(height_));
    bgfx::setUniform(display_,display);
    bgfx::setTexture(0,sceneSampler_,bgfx::getTexture(scene_));
    bgfx::setVertexBuffer(0,fullscreen_); bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A);
    bgfx::submit(views::display,displayProgram_);
}
const char* Renderer::name() const { return bgfx::getRendererName(bgfx::getRendererType()); }
void Renderer::stop() {
    if (!started_) return;
    try{telemetry.save(configuration_,name());}catch(const std::exception& e){++callbacks.errors;std::cerr<<"FRAME_TRACE_ERROR "<<e.what()<<'\n';}
    if(bgfx::isValid(skyProgram_))bgfx::destroy(skyProgram_);
    if(bgfx::isValid(environment_))bgfx::destroy(environment_);
    if(bgfx::isValid(inverseViewProjection_))bgfx::destroy(inverseViewProjection_);
    skyProgram_=BGFX_INVALID_HANDLE;environment_=BGFX_INVALID_HANDLE;inverseViewProjection_=BGFX_INVALID_HANDLE;
    if(bgfx::isValid(skinProgram_))bgfx::destroy(skinProgram_);
    if(bgfx::isValid(skinShadowProgram_))bgfx::destroy(skinShadowProgram_);
    if(bgfx::isValid(joints_))bgfx::destroy(joints_);
    skinProgram_=BGFX_INVALID_HANDLE;skinShadowProgram_=BGFX_INVALID_HANDLE;joints_=BGFX_INVALID_HANDLE;
    if(bgfx::isValid(shadow_)) bgfx::destroy(shadow_);
    if(bgfx::isValid(shadowProgram_)) bgfx::destroy(shadowProgram_);
    shadow_=BGFX_INVALID_HANDLE;shadowProgram_=BGFX_INVALID_HANDLE;
    for(auto* uniform:{&shadowMatrix_,&shadowFarMatrix_,&shadowRange_,&shadowCamera_,&shadowOptions_,&shadowSampler_,&eye_,&surfaceParams_,&materialMapping_,&materialUvOffset_,&albedoSampler_,&normalSampler_,&surfaceSampler_}) {
        if(bgfx::isValid(*uniform)) bgfx::destroy(*uniform);
        *uniform=BGFX_INVALID_HANDLE;
    }
    if (bgfx::isValid(textureProgram_)) bgfx::destroy(textureProgram_);
    if (bgfx::isValid(textureOptions_)) bgfx::destroy(textureOptions_);
    if (bgfx::isValid(previewSampler_)) bgfx::destroy(previewSampler_);
    textureProgram_=BGFX_INVALID_HANDLE;textureOptions_=BGFX_INVALID_HANDLE;previewSampler_=BGFX_INVALID_HANDLE;
    if (bgfx::isValid(scene_)) bgfx::destroy(scene_);
    if (bgfx::isValid(displayProgram_)) bgfx::destroy(displayProgram_);
    if (bgfx::isValid(calibrationProgram_)) bgfx::destroy(calibrationProgram_);
    if (bgfx::isValid(fullscreen_)) bgfx::destroy(fullscreen_);
    if (bgfx::isValid(display_)) bgfx::destroy(display_);
    if (bgfx::isValid(sceneSampler_)) bgfx::destroy(sceneSampler_);
    scene_=BGFX_INVALID_HANDLE; displayProgram_=BGFX_INVALID_HANDLE; calibrationProgram_=BGFX_INVALID_HANDLE;
    fullscreen_=BGFX_INVALID_HANDLE; display_=BGFX_INVALID_HANDLE; sceneSampler_=BGFX_INVALID_HANDLE;
    for (auto handle : meshes_) if (bgfx::isValid(handle)) bgfx::destroy(handle);
    if (bgfx::isValid(program_)) bgfx::destroy(program_);
    if(bgfx::isValid(layerParams_))bgfx::destroy(layerParams_);layerParams_=BGFX_INVALID_HANDLE;
    for(auto& h:layerSamplers_){if(bgfx::isValid(h))bgfx::destroy(h);h=BGFX_INVALID_HANDLE;}
    if (bgfx::isValid(material_)) bgfx::destroy(material_);
    if (bgfx::isValid(light_)) bgfx::destroy(light_);
    if (bgfx::isValid(normal_)) bgfx::destroy(normal_);
    if (bgfx::isValid(options_)) bgfx::destroy(options_);
    for (auto& handle : meshes_) handle=BGFX_INVALID_HANDLE;
    program_=BGFX_INVALID_HANDLE; normal_=BGFX_INVALID_HANDLE; options_=BGFX_INVALID_HANDLE;
    material_=BGFX_INVALID_HANDLE; light_=BGFX_INVALID_HANDLE;
    bgfx::frame(); bgfx::shutdown(); started_=false;
    std::cout << "SHUTDOWN renderer resources released\n";
}
}

#include "Rendering/Renderer.h"
#include "Rendering/RenderViews.h"
#include "Core/FixtureGeometry.h"
#include "Core/Color.h"
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
void Renderer::start(const Window& window, const std::filesystem::path& shaders) {
    window.pixels(width_, height_);
    if (!window.nativeHandle() || width_ <= 0 || height_ <= 0) throw std::runtime_error("Invalid window surface");
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
    init.reset = BGFX_RESET_VSYNC;
    if (!bgfx::init(init)) throw std::runtime_error("Requested GPU backend failed to initialize");
    started_ = true;
    if (bgfx::getRendererType() != init.type) throw std::runtime_error("Unexpected GPU backend");
    program_ = loadProgram(shaders, "vs_scene.bin", "fs_scene.bin");
    shadowProgram_=loadProgram(shaders,"vs_shadow.bin","fs_shadow.bin");
    shadowMatrix_=bgfx::createUniform("u_shadowMatrix",bgfx::UniformType::Mat4);
    shadowOptions_=bgfx::createUniform("u_shadowOptions",bgfx::UniformType::Vec4);
    shadowSampler_=bgfx::createUniform("s_shadow",bgfx::UniformType::Sampler);
    eye_=bgfx::createUniform("u_eye",bgfx::UniformType::Vec4);
    surfaceParams_=bgfx::createUniform("u_surfaceParams",bgfx::UniformType::Vec4);
    albedoSampler_=bgfx::createUniform("s_albedo",bgfx::UniformType::Sampler);
    normalSampler_=bgfx::createUniform("s_normal",bgfx::UniformType::Sampler);
    surfaceSampler_=bgfx::createUniform("s_surface",bgfx::UniformType::Sampler);
    for (auto uniform:{shadowMatrix_,shadowOptions_,shadowSampler_,eye_,surfaceParams_,albedoSampler_,normalSampler_,surfaceSampler_})
        if (!bgfx::isValid(uniform)) throw std::runtime_error("Lighting uniform allocation failed");
    constexpr uint64_t shadowFlags=BGFX_TEXTURE_RT|BGFX_SAMPLER_U_CLAMP|BGFX_SAMPLER_V_CLAMP|BGFX_SAMPLER_MIN_POINT|BGFX_SAMPLER_MAG_POINT;
    if (!bgfx::isTextureValid(0,false,1,bgfx::TextureFormat::R32F,shadowFlags)) throw std::runtime_error("Shadow R32F target unsupported");
    bgfx::TextureHandle shadowTargets[]={
        bgfx::createTexture2D(2048,2048,false,1,bgfx::TextureFormat::R32F,shadowFlags),
        bgfx::createTexture2D(2048,2048,false,1,bgfx::TextureFormat::D24S8,BGFX_TEXTURE_RT)};
    if (bgfx::isValid(shadowTargets[0]) && bgfx::isValid(shadowTargets[1])) shadow_=bgfx::createFrameBuffer(2,shadowTargets,true);
    if (!bgfx::isValid(shadow_)) {
        for(auto texture:shadowTargets) if(bgfx::isValid(texture)) bgfx::destroy(texture);
        throw std::runtime_error("Shadow target allocation failed");
    }
    bgfx::setViewName(views::shadow,"Directional sun shadow");
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
    bgfx::reset(BGFX_RESET_VSYNC, &swap);
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
    const std::array<FixtureMesh,4> data{fixtureCube(),sloped,fixtureSphere(),
        bakeFlatReference(sloped,subjectTransform(geometryProofState()))};
    std::array<bgfx::VertexBufferHandle,4> replacement{{BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE,BGFX_INVALID_HANDLE}};
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
    const char* names[]={"Reference cube", "Reference sloped solid", "Reference sphere", "CPU-baked flat-normal reference"};
    for (size_t i=0;i<meshes_.size();++i) bgfx::setName(meshes_[i],names[i]);
}
void Renderer::draw(const FixtureState& state, GeometryCheck check, bool calibration, const TexturePreview* preview, const SceneSurfaces* surfaces, const ScenePlacement* placement) {
    auto eye=state.eye();
    const auto offset=placement?placement->offset:std::array<float,3>{};
    for(size_t i=0;i<3;++i) eye[i]+=offset[i];
    float view[16], projection[16];
    bx::mtxLookAt(view,{eye[0],eye[1],eye[2]},{offset[0],0.85f+offset[1],offset[2]},{0,1,0},bx::Handedness::Right);
    bx::mtxProj(projection,state.fieldOfView,float(width_)/float(height_),0.1f,100.0f,
        bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);
    // reset clears view framebuffer bindings; restore ownership on every frame.
    bgfx::setViewFrameBuffer(views::scene,scene_);
    bgfx::setViewRect(views::scene,0,0,uint16_t(width_),uint16_t(height_));
    const float clear[]={srgbToLinear(28.0f/255),srgbToLinear(37.0f/255),srgbToLinear(50.0f/255),1};
    bgfx::setPaletteColor(0,clear);
    bgfx::setViewClear(views::scene,BGFX_CLEAR_COLOR|BGFX_CLEAR_DEPTH,1.0f,0,uint8_t(0));
    bgfx::setViewTransform(views::scene,view,projection);
    bgfx::touch(views::scene);
    const float light[4]={std::sin(state.lightAzimuth),0.9f,std::cos(state.lightAzimuth),state.lightIntensity};
    float lightView[16],lightProjection[16],lightViewProjection[16],shadowMatrix[16];
    const auto lightDirection=bx::normalize(bx::Vec3{light[0],light[1],light[2]});
    const auto lightEye=bx::mul(lightDirection,20.0f);
    bx::mtxLookAt(lightView,lightEye,{0,0,0},{0,1,0},bx::Handedness::Right);
    bx::mtxOrtho(lightProjection,-12,12,-12,12,0.1f,45.0f,0,bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);
    const auto* caps=bgfx::getCaps();
    const float crop[]={0.5f,0,0,0, 0,caps->originBottomLeft?0.5f:-0.5f,0,0,
        0,0,caps->homogeneousDepth?0.5f:1.0f,0, 0.5f,0.5f,caps->homogeneousDepth?0.5f:0.0f,1};
    bx::mtxMul(lightViewProjection,lightView,lightProjection);
    bx::mtxMul(shadowMatrix,lightViewProjection,crop);
    const float shadowOptions[]={state.shadows?1.0f:0.0f,state.shadowBias,1.0f/2048,0};
    const float eyePosition[]={eye[0],eye[1],eye[2],0};
    uint64_t cull=BGFX_STATE_CULL_CW; // Authored fixture triangles are outward CCW.
    if (check==GeometryCheck::Unculled) cull=0;
    if (check==GeometryCheck::FrontCull) cull=BGFX_STATE_CULL_CCW;
    auto submit=[&](int mesh,const Matrix4& transform,const float* color,const SurfaceTextures* surface=nullptr) {
        const auto normal=normalMatrix(transform);
        bgfx::setTransform(transform.data());
        bgfx::setVertexBuffer(0,meshes_.at(size_t(mesh)));
        const bool textured=surface && state.surfaceTextures && bgfx::isValid(surface->albedo) && bgfx::isValid(surface->normal) && bgfx::isValid(surface->surface);
        const float linear[]={textured?1.0f:srgbToLinear(color[0]),textured?1.0f:srgbToLinear(color[1]),textured?1.0f:srgbToLinear(color[2]),color[3]};
        const float options[]={state.showNormals?1.0f:0.0f,textured?1.0f:0.0f,state.textureScale,state.ambient};
        const float params[]={state.roughness,state.metallic,state.normalStrength,surface && surface->packedSurface?1.0f:0.0f};
        bgfx::setUniform(material_,linear); bgfx::setUniform(light_,light);
        bgfx::setUniform(shadowMatrix_,shadowMatrix);bgfx::setUniform(shadowOptions_,shadowOptions);
        bgfx::setUniform(eye_,eyePosition);bgfx::setUniform(surfaceParams_,params);
        bgfx::setTexture(0,shadowSampler_,bgfx::getTexture(shadow_));
        const auto* bound=surface?surface:(surfaces?&surfaces->rock:nullptr);
        if (bound) {
            bgfx::setTexture(1,albedoSampler_,bound->albedo);
            bgfx::setTexture(2,normalSampler_,bound->normal);
            bgfx::setTexture(3,surfaceSampler_,bound->surface);
        }
        bgfx::setUniform(normal_,normal.data()); bgfx::setUniform(options_,options);
        bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A|BGFX_STATE_WRITE_Z|BGFX_STATE_DEPTH_TEST_LESS|BGFX_STATE_MSAA|cull);
        bgfx::submit(views::scene,program_);
    };
    const float ground[4]={0.28f,0.31f,0.34f,1};
    const float color[4]={state.color[0],state.color[1],state.color[2],1};
    const float marker[4]={0.33f,0.67f,0.64f,1};
    Matrix4 groundTransform, markerTransform;
    bx::mtxSRT(groundTransform.data(),20,.1f,20,0,0,0,0,-.05f,0);
    bx::mtxSRT(markerTransform.data(),.35f,1.8f,.35f,0,0,0,2.1f,.9f,0);
    auto subject=subjectTransform(state);
    for(size_t i=0;i<3;++i) subject[12+i]+=offset[i];
    const bool baked=check==GeometryCheck::BakedReference;
    if (baked) bx::mtxIdentity(subject.data());
    if (state.shadows && !state.showNormals && !calibration && !preview) {
        bgfx::setViewFrameBuffer(views::shadow,shadow_);
        bgfx::setViewRect(views::shadow,0,0,2048,2048);
        bgfx::setViewClear(views::shadow,BGFX_CLEAR_COLOR|BGFX_CLEAR_DEPTH,0xffffffff,1.0f,0);
        bgfx::setViewTransform(views::shadow,lightView,lightProjection);
        auto cast=[&](int mesh,const Matrix4& transform) {
            bgfx::setTransform(transform.data());bgfx::setVertexBuffer(0,meshes_.at(size_t(mesh)));
            bgfx::setState(BGFX_STATE_WRITE_R|BGFX_STATE_WRITE_Z|BGFX_STATE_DEPTH_TEST_LESS|BGFX_STATE_CULL_CW);
            bgfx::submit(views::shadow,shadowProgram_);
        };
        cast(0,groundTransform);cast(baked?3:state.mesh,subject);cast(0,markerTransform);
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
        submit(0,markerTransform,marker); submit(state.mesh,subject,color,rock); submit(0,groundTransform,ground,soil);
    } else {
        submit(0,groundTransform,ground,soil); submit(baked?3:state.mesh,subject,color,rock); submit(0,markerTransform,marker);
    }
    const float display[]={state.exposure,state.showNormals && !calibration && !preview?1.0f:0.0f,bgfx::getCaps()->originBottomLeft?1.0f:0.0f,0};
    bgfx::setViewRect(views::display,0,0,uint16_t(width_),uint16_t(height_));
    bgfx::setUniform(display_,display);
    bgfx::setTexture(0,sceneSampler_,bgfx::getTexture(scene_));
    bgfx::setVertexBuffer(0,fullscreen_); bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A);
    bgfx::submit(views::display,displayProgram_);
}
const char* Renderer::name() const { return bgfx::getRendererName(bgfx::getRendererType()); }
void Renderer::stop() {
    if (!started_) return;
    if(bgfx::isValid(shadow_)) bgfx::destroy(shadow_);
    if(bgfx::isValid(shadowProgram_)) bgfx::destroy(shadowProgram_);
    shadow_=BGFX_INVALID_HANDLE;shadowProgram_=BGFX_INVALID_HANDLE;
    for(auto* uniform:{&shadowMatrix_,&shadowOptions_,&shadowSampler_,&eye_,&surfaceParams_,&albedoSampler_,&normalSampler_,&surfaceSampler_}) {
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

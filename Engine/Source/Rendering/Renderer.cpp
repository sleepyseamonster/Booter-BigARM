#include "Rendering/Renderer.h"
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
    material_ = bgfx::createUniform("u_material", bgfx::UniformType::Vec4);
    light_ = bgfx::createUniform("u_light", bgfx::UniformType::Vec4);
    if (!bgfx::isValid(material_) || !bgfx::isValid(light_)) throw std::runtime_error("Scene uniform allocation failed");
    rebuildMesh();
    bgfx::setViewName(0, "Perspective fixture");
    std::cout << "RENDERER " << name() << " framebuffer=" << width_ << 'x' << height_ << '\n';
}
void Renderer::resize(int width, int height) {
    if (width <= 0 || height <= 0 || (width == width_ && height == height_)) return;
    if (width > 65535 || height > 65535) throw std::runtime_error("Framebuffer exceeds view limits");
    bgfx::SwapChain swap;
    swap.width = uint32_t(width); swap.height = uint32_t(height);
    bgfx::reset(BGFX_RESET_VSYNC, &swap);
    width_ = width; height_ = height;
    std::cout << "RESIZE " << width_ << 'x' << height_ << '\n';
}
void Renderer::rebuildMesh() {
    struct Vertex { float x,y,z,nx,ny,nz; };
    std::vector<Vertex> vertices;
    const float positions[6][4][3] = {
        {{-0.5f,-0.5f,0.5f},{0.5f,-0.5f,0.5f},{0.5f,0.5f,0.5f},{-0.5f,0.5f,0.5f}},
        {{0.5f,-0.5f,-0.5f},{-0.5f,-0.5f,-0.5f},{-0.5f,0.5f,-0.5f},{0.5f,0.5f,-0.5f}},
        {{0.5f,-0.5f,0.5f},{0.5f,-0.5f,-0.5f},{0.5f,0.5f,-0.5f},{0.5f,0.5f,0.5f}},
        {{-0.5f,-0.5f,-0.5f},{-0.5f,-0.5f,0.5f},{-0.5f,0.5f,0.5f},{-0.5f,0.5f,-0.5f}},
        {{-0.5f,0.5f,0.5f},{0.5f,0.5f,0.5f},{0.5f,0.5f,-0.5f},{-0.5f,0.5f,-0.5f}},
        {{-0.5f,-0.5f,-0.5f},{0.5f,-0.5f,-0.5f},{0.5f,-0.5f,0.5f},{-0.5f,-0.5f,0.5f}}};
    const float normals[6][3] = {{0,0,1},{0,0,-1},{1,0,0},{-1,0,0},{0,1,0},{0,-1,0}};
    for (int face=0; face<6; ++face) for (int index : {0,1,2,0,2,3}) {
        const auto& p=positions[face][index]; const auto& n=normals[face];
        vertices.push_back({p[0],p[1],p[2],n[0],n[1],n[2]});
    }
    bgfx::VertexLayout layout;
    layout.begin().add(bgfx::Attrib::Position,3,bgfx::AttribType::Float).add(bgfx::Attrib::Normal,3,bgfx::AttribType::Float).end();
    auto replacement=bgfx::createVertexBuffer(bgfx::copy(vertices.data(), uint32_t(vertices.size()*sizeof(Vertex))),layout);
    if (!bgfx::isValid(replacement)) throw std::runtime_error("Mesh allocation failed");
    if (bgfx::isValid(mesh_)) bgfx::destroy(mesh_);
    mesh_=replacement;
    bgfx::setName(mesh_, "Fixture cube shared by ground/subject/scale marker");
}
void Renderer::draw(const FixtureState& state) {
    const auto eye=state.eye();
    float view[16], projection[16];
    bx::mtxLookAt(view,{eye[0],eye[1],eye[2]},{0,0.85f,0},{0,1,0},bx::Handedness::Right);
    bx::mtxProj(projection,state.fieldOfView,float(width_)/float(height_),0.1f,100.0f,
        bgfx::getCaps()->homogeneousDepth,bx::Handedness::Right);
    bgfx::setViewRect(0,0,0,uint16_t(width_),uint16_t(height_));
    bgfx::setViewClear(0,BGFX_CLEAR_COLOR|BGFX_CLEAR_DEPTH,0x1c2532ff,1.0f,0);
    bgfx::setViewTransform(0,view,projection);
    bgfx::touch(0);
    const float light[4]={std::sin(state.lightAzimuth),0.9f,std::cos(state.lightAzimuth),state.lightIntensity};
    auto submit=[&](float sx,float sy,float sz,float x,float y,float z,float yaw,const float* color) {
        float transform[16]; bx::mtxSRT(transform,sx,sy,sz,0,yaw,0,x,y,z);
        bgfx::setTransform(transform);
        bgfx::setVertexBuffer(0,mesh_);
        bgfx::setUniform(material_,color); bgfx::setUniform(light_,light);
        bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A|BGFX_STATE_WRITE_Z|BGFX_STATE_DEPTH_TEST_LESS|BGFX_STATE_MSAA);
        bgfx::submit(0,program_);
    };
    const float ground[4]={0.28f,0.31f,0.34f,1};
    const float color[4]={state.color[0],state.color[1],state.color[2],1};
    const float marker[4]={0.33f,0.67f,0.64f,1};
    submit(20,0.1f,20,0,-0.05f,0,0,ground);
    submit(1.5f,1.5f,1.5f,0,0.75f,0,state.objectYaw,color);
    submit(0.35f,1.8f,0.35f,2.1f,0.9f,0,0,marker);
}
const char* Renderer::name() const { return bgfx::getRendererName(bgfx::getRendererType()); }
void Renderer::stop() {
    if (!started_) return;
    if (bgfx::isValid(mesh_)) bgfx::destroy(mesh_);
    if (bgfx::isValid(program_)) bgfx::destroy(program_);
    if (bgfx::isValid(material_)) bgfx::destroy(material_);
    if (bgfx::isValid(light_)) bgfx::destroy(light_);
    mesh_=BGFX_INVALID_HANDLE; program_=BGFX_INVALID_HANDLE;
    material_=BGFX_INVALID_HANDLE; light_=BGFX_INVALID_HANDLE;
    bgfx::frame(); bgfx::shutdown(); started_=false;
    std::cout << "SHUTDOWN renderer resources released\n";
}
}

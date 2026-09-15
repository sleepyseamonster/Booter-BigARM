#pragma once
#include "Core/FixtureState.h"
#include "Core/RockMaterial.h"
#include "Platform/Window.h"
#include "Rendering/RenderModel.h"
#include "Rendering/FrameTelemetry.h"
#include <bgfx/bgfx.h>
#include <atomic>
#include <filesystem>

namespace engine {
enum class GeometryCheck { None, Transformed, BakedReference, Unculled, FrontCull, ReverseOrder };
bgfx::ProgramHandle loadProgram(const std::filesystem::path& directory, const char* vertex, const char* fragment);
class CaptureCallbacks final : public bgfx::CallbackI {
public:
    std::atomic<unsigned> captures{0}, errors{0};
    void fatal(const char*, uint16_t, bgfx::Fatal::Enum, const char*) override;
    void traceVargs(const char*, uint16_t, const char*, va_list) override;
    void profilerBegin(const char*, uint32_t, const char*, uint16_t) override {}
    void profilerBeginLiteral(const char*, uint32_t, const char*, uint16_t) override {}
    void profilerEnd() override {}
    uint32_t cacheReadSize(uint64_t) override { return 0; }
    bool cacheRead(uint64_t, void*, uint32_t) override { return false; }
    void cacheWrite(uint64_t, const void*, uint32_t) override {}
    void screenShot(const char*, uint32_t, uint32_t, uint32_t, bgfx::TextureFormat::Enum, const void*, uint32_t, bool) override;
    void captureBegin(uint32_t, uint32_t, uint32_t, bgfx::TextureFormat::Enum, bool) override {}
    void captureEnd() override {}
    void captureFrame(const void*, uint32_t) override {}
};
struct TexturePreview { bgfx::TextureHandle texture=BGFX_INVALID_HANDLE;float lod=0,channel=0;bool srgb=false;float repeat=1; };
enum class MaterialMapping { WorldTriplanar, UV };
// Borrowed for a frame; TextureStore leases own the resources. No persistent GPU IDs.
struct SurfaceTextures {
    bool packedSurface=true;
    bgfx::TextureHandle albedo=BGFX_INVALID_HANDLE, normal=BGFX_INVALID_HANDLE, surface=BGFX_INVALID_HANDLE;
    std::array<bgfx::TextureHandle,10> layers=[] {std::array<bgfx::TextureHandle,10> a;for(auto& h:a)h=BGFX_INVALID_HANDLE;return a;}();
    bool terrainBlend=false,terrainNatural=false;bool layered=false;RockMaterial material;float seed=0;
    MaterialMapping mapping=MaterialMapping::WorldTriplanar;
    std::array<float,4> uvTransform{1,1,0,0}; // scale.xy, offset.xy

};
struct RenderInstance {const RenderModel* model=nullptr;std::array<float,3> offset{},boundsCenter{};float yaw=0,boundsRadius=1;bool ground=false;std::array<float,3> scale{1,1,1};};
struct ScenePlacement {const std::vector<RenderInstance>* instances=nullptr;bool streamedWorld=false; std::array<float,3> offset{},eye{},target{};bool physicalCharacter=false;const RenderModel* model=nullptr;const std::vector<SkinMatrix>* pose=nullptr;bool cpuReference=false,markerActive=false;const RenderModel* rock=nullptr;std::array<float,3> rockOffset{-3.5f,0,0};float rockFocusHeight=.85f; };
struct SceneSurfaces { SurfaceTextures rock, ground; };
class Renderer {
public:
    Renderer() = default;
    ~Renderer();
    Renderer(const Renderer&) = delete;
    Renderer& operator=(const Renderer&) = delete;
    void start(const Window&, const std::filesystem::path& shaders, const RenderConfiguration& configuration=RenderConfiguration::fromEnvironment());
    FrameTelemetry telemetry;
    uint32_t finishFrame(bool technical=false){return telemetry.finish(technical);}
    void resize(int width, int height);
    void draw(const FixtureState&, GeometryCheck check = GeometryCheck::None, bool calibration = false, const TexturePreview* preview = nullptr, const SceneSurfaces* surfaces = nullptr, const ScenePlacement* placement = nullptr);
    void rebuildMesh();
    void stop();
    const char* name() const;
    int width() const { return width_; }
    int height() const { return height_; }
    CaptureCallbacks callbacks;
private:
    void resizeTargets(int width,int height);
    bool started_ = false;
    RenderConfiguration configuration_;
    int width_ = 0, height_ = 0;
    bgfx::ProgramHandle textureProgram_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle textureOptions_ = BGFX_INVALID_HANDLE, previewSampler_ = BGFX_INVALID_HANDLE;
    bgfx::FrameBufferHandle scene_ = BGFX_INVALID_HANDLE;
    bgfx::ProgramHandle displayProgram_ = BGFX_INVALID_HANDLE, calibrationProgram_ = BGFX_INVALID_HANDLE;
    bgfx::ProgramHandle skyProgram_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle environment_ = BGFX_INVALID_HANDLE, inverseViewProjection_ = BGFX_INVALID_HANDLE;
    bgfx::VertexBufferHandle fullscreen_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle display_ = BGFX_INVALID_HANDLE, sceneSampler_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle ao_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle sceneNormalSampler_ = BGFX_INVALID_HANDLE, sceneDepthSampler_ = BGFX_INVALID_HANDLE;
    bgfx::ProgramHandle skinProgram_=BGFX_INVALID_HANDLE,skinShadowProgram_=BGFX_INVALID_HANDLE;
    bgfx::UniformHandle joints_=BGFX_INVALID_HANDLE;
    bgfx::ProgramHandle program_ = BGFX_INVALID_HANDLE;
    bgfx::FrameBufferHandle shadow_ = BGFX_INVALID_HANDLE;
    bgfx::ProgramHandle shadowProgram_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle shadowMatrix_ = BGFX_INVALID_HANDLE, shadowOptions_ = BGFX_INVALID_HANDLE, shadowSampler_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle shadowFarMatrix_ = BGFX_INVALID_HANDLE, shadowRange_ = BGFX_INVALID_HANDLE, shadowCamera_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle eye_ = BGFX_INVALID_HANDLE, surfaceParams_ = BGFX_INVALID_HANDLE, materialMapping_ = BGFX_INVALID_HANDLE, materialUvOffset_ = BGFX_INVALID_HANDLE, fog_ = BGFX_INVALID_HANDLE, fogColor_ = BGFX_INVALID_HANDLE;
    std::array<bgfx::UniformHandle,10> layerSamplers_=[] {std::array<bgfx::UniformHandle,10> a;for(auto& h:a)h=BGFX_INVALID_HANDLE;return a;}();
    bgfx::UniformHandle layerParams_=BGFX_INVALID_HANDLE;
    bgfx::UniformHandle albedoSampler_ = BGFX_INVALID_HANDLE, normalSampler_ = BGFX_INVALID_HANDLE, surfaceSampler_ = BGFX_INVALID_HANDLE;
    std::array<bgfx::VertexBufferHandle, 5> meshes_{{BGFX_INVALID_HANDLE, BGFX_INVALID_HANDLE, BGFX_INVALID_HANDLE, BGFX_INVALID_HANDLE, BGFX_INVALID_HANDLE}};
    bgfx::UniformHandle material_ = BGFX_INVALID_HANDLE, light_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle normal_ = BGFX_INVALID_HANDLE, options_ = BGFX_INVALID_HANDLE;
};
}

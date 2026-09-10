#pragma once
#include "Core/FixtureState.h"
#include "Platform/Window.h"
#include <bgfx/bgfx.h>
#include <atomic>
#include <filesystem>

namespace engine {
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
class Renderer {
public:
    Renderer() = default;
    ~Renderer();
    Renderer(const Renderer&) = delete;
    Renderer& operator=(const Renderer&) = delete;
    void start(const Window&, const std::filesystem::path& shaders);
    void resize(int width, int height);
    void draw(const FixtureState&);
    void rebuildMesh();
    void stop();
    const char* name() const;
    int width() const { return width_; }
    int height() const { return height_; }
    CaptureCallbacks callbacks;
private:
    bool started_ = false;
    int width_ = 0, height_ = 0;
    bgfx::ProgramHandle program_ = BGFX_INVALID_HANDLE;
    bgfx::VertexBufferHandle mesh_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle material_ = BGFX_INVALID_HANDLE, light_ = BGFX_INVALID_HANDLE;
};
}

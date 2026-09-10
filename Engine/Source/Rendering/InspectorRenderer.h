#pragma once
#include <bgfx/bgfx.h>
#include <filesystem>
#include <imgui.h>

namespace engine {
class InspectorRenderer {
public:
    ~InspectorRenderer();
    InspectorRenderer() = default;
    InspectorRenderer(const InspectorRenderer&) = delete;
    InspectorRenderer& operator=(const InspectorRenderer&) = delete;
    void start(const std::filesystem::path& shaders);
    void draw(ImDrawData* data);
    void stop();
private:
    bgfx::VertexLayout layout_;
    bgfx::ProgramHandle program_ = BGFX_INVALID_HANDLE;
    bgfx::UniformHandle sampler_ = BGFX_INVALID_HANDLE;
    bgfx::TextureHandle font_ = BGFX_INVALID_HANDLE;
};
}

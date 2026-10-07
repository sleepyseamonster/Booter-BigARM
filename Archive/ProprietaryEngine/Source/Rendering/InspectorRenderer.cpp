#include "Rendering/InspectorRenderer.h"
#include "Rendering/Renderer.h"
#include "Rendering/RenderViews.h"
#include "Core/FixtureState.h"
#include <bx/math.h>
#include <cstring>
#include <stdexcept>

namespace engine {
InspectorRenderer::~InspectorRenderer() { stop(); }
void InspectorRenderer::start(const std::filesystem::path& shaders) {
    program_=loadProgram(shaders,"vs_inspector.bin","fs_inspector.bin");
    sampler_=bgfx::createUniform("s_texture",bgfx::UniformType::Sampler);
    layout_.begin().add(bgfx::Attrib::Position,2,bgfx::AttribType::Float)
        .add(bgfx::Attrib::TexCoord0,2,bgfx::AttribType::Float)
        .add(bgfx::Attrib::Color0,4,bgfx::AttribType::Uint8,true).end();
    static_assert(sizeof(ImDrawVert)==20, "Adapter expects default ImDrawVert layout");
    auto& io=ImGui::GetIO();
    unsigned char* pixels=nullptr; int width=0,height=0;
    io.Fonts->GetTexDataAsRGBA32(&pixels,&width,&height);
    if (!pixels || width<=0 || height<=0 || width>65535 || height>65535) throw std::runtime_error("Invalid font atlas");
    font_=bgfx::createTexture2D(uint16_t(width),uint16_t(height),false,1,bgfx::TextureFormat::RGBA8,
        BGFX_SAMPLER_U_CLAMP|BGFX_SAMPLER_V_CLAMP,bgfx::copy(pixels,uint32_t(width*height*4)));
    if (!bgfx::isValid(font_) || !bgfx::isValid(sampler_)) throw std::runtime_error("Inspector resources failed");
    io.Fonts->SetTexID(ImTextureID(font_.idx)+1);
    io.BackendRendererName="BooterBigARM_bgfx_fixed_atlas";
    io.BackendFlags|=ImGuiBackendFlags_RendererHasVtxOffset;
    bgfx::setViewName(views::inspector,"Inspector");
    bgfx::setViewMode(views::inspector,bgfx::ViewMode::Sequential);
}
void InspectorRenderer::draw(ImDrawData* data) {
    const int width=int(data->DisplaySize.x*data->FramebufferScale.x);
    const int height=int(data->DisplaySize.y*data->FramebufferScale.y);
    if (width<=0 || height<=0) return;
    if (width>65535 || height>65535) throw std::runtime_error("Inspector framebuffer exceeds view limits");
    float projection[16];
    bx::mtxOrtho(projection,data->DisplayPos.x,data->DisplayPos.x+data->DisplaySize.x,
        data->DisplayPos.y+data->DisplaySize.y,data->DisplayPos.y,0,1000,0,bgfx::getCaps()->homogeneousDepth);
    bgfx::setViewRect(views::inspector,0,0,uint16_t(width),uint16_t(height));
    bgfx::setViewTransform(views::inspector,nullptr,projection);
    for (const ImDrawList* list : data->CmdLists) {
        const uint32_t vertices=uint32_t(list->VtxBuffer.Size),indices=uint32_t(list->IdxBuffer.Size);
        if (!vertices || !indices) continue;
        bgfx::TransientVertexBuffer vb; bgfx::TransientIndexBuffer ib;
        if (!bgfx::allocTransientBuffers(&vb,layout_,vertices,&ib,indices,sizeof(ImDrawIdx)==4))
            throw std::runtime_error("Inspector transient buffer budget exceeded");
        std::memcpy(vb.data,list->VtxBuffer.Data,size_t(vertices)*sizeof(ImDrawVert));
        std::memcpy(ib.data,list->IdxBuffer.Data,size_t(indices)*sizeof(ImDrawIdx));
        for (const ImDrawCmd& command : list->CmdBuffer) {
            if (command.UserCallback) {
                if (command.UserCallback!=ImGui::GetPlatformIO().DrawCallback_ResetRenderState &&
                    command.UserCallback!=ImDrawCallback_ResetRenderState) command.UserCallback(list,&command);
                continue;
            }
            ClipRect rect{};
            const auto& clip=command.ClipRect;
            if (!clipRect((clip.x-data->DisplayPos.x)*data->FramebufferScale.x,
                          (clip.y-data->DisplayPos.y)*data->FramebufferScale.y,
                          (clip.z-data->DisplayPos.x)*data->FramebufferScale.x,
                          (clip.w-data->DisplayPos.y)*data->FramebufferScale.y,width,height,rect)) continue;
            if (command.GetTexID()!=ImTextureID(font_.idx)+1) throw std::runtime_error("Unsupported inspector texture");
            if (command.VtxOffset>=vertices || command.IdxOffset+command.ElemCount>indices)
                throw std::runtime_error("Invalid inspector draw offsets");
            bgfx::setScissor(rect.x,rect.y,rect.width,rect.height);
            bgfx::setState(BGFX_STATE_WRITE_RGB|BGFX_STATE_WRITE_A|BGFX_STATE_MSAA|
                BGFX_STATE_BLEND_FUNC(BGFX_STATE_BLEND_SRC_ALPHA,BGFX_STATE_BLEND_INV_SRC_ALPHA));
            bgfx::setTexture(0,sampler_,font_);
            bgfx::setVertexBuffer(0,&vb,command.VtxOffset,vertices-command.VtxOffset);
            bgfx::setIndexBuffer(&ib,command.IdxOffset,command.ElemCount);
            bgfx::submit(views::inspector,program_);
        }
    }
}
void InspectorRenderer::stop() {
    if (ImGui::GetCurrentContext()) {
        auto& io=ImGui::GetIO();
        io.BackendRendererName=nullptr;
        io.BackendFlags&=~ImGuiBackendFlags_RendererHasVtxOffset;
        if (io.Fonts->TexRef._TexData) io.Fonts->SetTexID(ImTextureID_Invalid);
    }
    if (bgfx::isValid(font_)) bgfx::destroy(font_);
    if (bgfx::isValid(program_)) bgfx::destroy(program_);
    if (bgfx::isValid(sampler_)) bgfx::destroy(sampler_);
    font_=BGFX_INVALID_HANDLE; program_=BGFX_INVALID_HANDLE; sampler_=BGFX_INVALID_HANDLE;
}
}

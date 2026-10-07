#include <SDL3/SDL.h>
#include <bgfx/bgfx.h>
#include <imgui.h>
#include <iostream>

int main()
{
    if (!SDL_Init(SDL_INIT_EVENTS)) {
        std::cerr << "SDL event initialization failed: " << SDL_GetError() << '\n';
        return 1;
    }
    const auto eventType = SDL_RegisterEvents(1);
    SDL_Event sent{};
    sent.type = eventType;
    sent.user.code = 42;
    bool received = false;
    if (eventType != 0 && SDL_PushEvent(&sent)) {
        SDL_Event event{};
        while (SDL_PollEvent(&event)) {
            received |= event.type == eventType && event.user.code == 42;
        }
    }
    if (!received) {
        std::cerr << "SDL synthetic event round-trip failed\n";
        SDL_Quit();
        return 2;
    }

    bgfx::Init init;
    init.type = bgfx::RendererType::Noop;
    init.swapChain.width = 64;
    init.swapChain.height = 64;
    if (!bgfx::init(init)) {
        std::cerr << "bgfx Noop initialization failed\n";
        SDL_Quit();
        return 3;
    }
    bgfx::VertexLayout layout;
    layout.begin().add(bgfx::Attrib::Position, 3, bgfx::AttribType::Float).end();
    const float vertices[] = {0, 0, 0, 1, 0, 0, 0, 1, 0};
    auto buffer = bgfx::createVertexBuffer(bgfx::copy(vertices, sizeof(vertices)), layout);
    const bool resourceValid = bgfx::isValid(buffer);
    if (resourceValid) bgfx::destroy(buffer);
    bgfx::frame();
    bgfx::shutdown();

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    auto& io = ImGui::GetIO();
    io.IniFilename = nullptr;
    io.DisplaySize = ImVec2(640, 480);
    io.DeltaTime = 1.0f / 60.0f;
    unsigned char* pixels = nullptr;
    int width = 0, height = 0;
    io.Fonts->GetTexDataAsRGBA32(&pixels, &width, &height);
    ImGui::NewFrame();
    ImGui::SetNextWindowPos(ImVec2(10, 10));
    ImGui::SetNextWindowSize(ImVec2(300, 150));
    ImGui::Begin("Integration probe");
    ImGui::TextUnformatted("CPU inspector draw data only");
    ImGui::End();
    ImGui::Render();
    const bool drawData = ImGui::GetDrawData()->TotalVtxCount > 0;
    const bool fontData = pixels != nullptr && width > 0 && height > 0;
    ImGui::DestroyContext();
    SDL_Quit();
    if (!resourceValid || !drawData || !fontData) {
        std::cerr << "Noop resource or ImGui CPU output check failed\n";
        return 4;
    }
    std::cout << "PASS: SDL synthetic events, bgfx Noop resource lifecycle, ImGui CPU draw data\n"
              << "SDL=" << SDL_GetVersion() << " ImGui=" << IMGUI_VERSION << '\n'
              << "NOT TESTED: real window/input devices, GPU rendering, shaders, inspector rendering, Windows\n";
    return 0;
}

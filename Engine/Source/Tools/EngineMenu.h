#pragma once
#include <imgui.h>
#include <algorithm>

namespace engine {
// Frame-local scope: keep the toolbar alive while its popup submits controls.
class EngineMenu {
public:
    EngineMenu() {
        const auto* viewport=ImGui::GetMainViewport();
        const ImVec2 edge{viewport->WorkPos.x+viewport->WorkSize.x-12,viewport->WorkPos.y+12};
        ImGui::SetNextWindowPos(edge,ImGuiCond_Always,{1,0});
        ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding,{0,0});
        ImGui::Begin("##EngineToolbar",nullptr,ImGuiWindowFlags_NoDecoration|ImGuiWindowFlags_AlwaysAutoResize|
            ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoSavedSettings|ImGuiWindowFlags_NoBackground);
        ImGui::PopStyleVar();
        if(ImGui::Button("Engine",{84,30}))ImGui::OpenPopup("Engine menu");
        const auto min=ImGui::GetItemRectMin(),max=ImGui::GetItemRectMax();
        button={(min.x+max.x)*.5f,(min.y+max.y)*.5f};
        ImGui::SetNextWindowPos({edge.x,max.y+6},ImGuiCond_Always,{1,0});
        ImGui::SetNextWindowSize({std::min(350.f,viewport->WorkSize.x-24),
            std::min(650.f,viewport->WorkPos.y+viewport->WorkSize.y-max.y-18)},ImGuiCond_Always);
        visible=ImGui::BeginPopup("Engine menu",ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoResize|ImGuiWindowFlags_NoSavedSettings);
    }
    ~EngineMenu() { if(visible)ImGui::EndPopup();ImGui::End(); }
    EngineMenu(const EngineMenu&)=delete;
    EngineMenu& operator=(const EngineMenu&)=delete;
    bool visible=false;
    ImVec2 button{};
};
}

#pragma once
#include <imgui.h>

namespace engine {
// Transient presentation state; never serialized into a scene or recipe.
struct ViewerControls {
    bool advanced=false;
    bool frameRequested=false;
    bool fullscreenRequested=false;

    void draw(float& exposure,bool fullscreen,bool canFrame) {
        ImGui::TextUnformatted("Scene view");
        ImGui::Separator();
        ImGui::BeginDisabled(!canFrame);
        if(ImGui::Button("Frame scene"))frameRequested=true;
        ImGui::EndDisabled();
        ImGui::SetNextItemWidth(140);
        ImGui::SliderFloat("Exposure",&exposure,-4.f,4.f,"%.1f");
        if(ImGui::Checkbox("Fullscreen",&fullscreen))fullscreenRequested=true;
        ImGui::Checkbox("Advanced controls",&advanced);
        ImGui::TextDisabled("F11: fullscreen | Esc: return");
    }
};
}

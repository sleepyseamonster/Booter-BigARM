#include "Tools/InspectionWorkbench.h"
#include <imgui.h>
#include <cstdio>
namespace engine {
InspectionWorkbench::InspectionWorkbench(const FixtureState& state,const std::filesystem::path& path):history_(state){if(path.string().size()>=path_.size())throw std::invalid_argument("Inspection path too long");std::snprintf(path_.data(),path_.size(),"%s",path.string().c_str());}
void InspectionWorkbench::draw(FixtureState& state,bool enabled) {
    const auto adopt=[](const FixtureState&){};
    if(enabled&&!ImGui::IsAnyItemActive()&&!ImGui::GetIO().MouseDown[1])history_.apply(state,adopt);
    if(!ImGui::CollapsingHeader("Inspection document"))return;
    ImGui::BeginDisabled(!enabled);
    if(ImGui::Button("Undo settings")){history_.undo(adopt);state=history_.value();}ImGui::SameLine();
    if(ImGui::Button("Redo settings")){history_.redo(adopt);state=history_.value();}
    ImGui::InputText("Preset file",path_.data(),path_.size());
    auto run=[&](auto&& action){try{action();error_.clear();}catch(const std::exception& e){error_=e.what();}};
    if(ImGui::Button("Save settings"))run([&]{saveInspection(path_.data(),state);});ImGui::SameLine();
    if(ImGui::Button("Reload settings"))run([&]{auto next=state;loadInspection(path_.data(),next);history_.apply(next,adopt);state=history_.value();});
    ImGui::EndDisabled();if(!error_.empty())ImGui::TextWrapped("%s",error_.c_str());
}
}

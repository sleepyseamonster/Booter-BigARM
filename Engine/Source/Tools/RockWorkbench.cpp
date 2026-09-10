#include "Tools/RockWorkbench.h"
#include <imgui.h>
#include <cstdio>
#include <algorithm>
namespace engine {
RockWorkbench::RockWorkbench(const std::filesystem::path& path,CalibrationRuntime& runtime):runtime_(runtime),history_(loadRockRecipe(path)),draft_(history_.value()) {
    if(path.string().size()>=path_.size())throw std::invalid_argument("Recipe path too long");std::snprintf(path_.data(),path_.size(),"%s",path.string().c_str());rebuild(history_.value());
}
void RockWorkbench::rebuild(const RockRecipe& recipe) {
    auto next=buildRockAsset(recipe,{1,recipe.version,{},0,"rock"});std::vector<std::unique_ptr<RenderModel>> models;
    for(const auto& lod:next.lods)models.push_back(std::make_unique<RenderModel>(lod.mesh));
    runtime_.setRock(next.lods[0].id,next.collision,offset);
    asset_=std::move(next);models_=std::move(models);
}
void RockWorkbench::apply(const RockRecipe& next){history_.apply(next,[&](const auto& r){rebuild(r);});draft_=history_.value();}
void RockWorkbench::undo(){history_.undo([&](const auto& r){rebuild(r);});draft_=history_.value();}
void RockWorkbench::redo(){history_.redo([&](const auto& r){rebuild(r);});draft_=history_.value();}
const RenderModel* RockWorkbench::model(float distance)const{return models_.at(forcedLod<0?rockLod(asset_,distance):std::min(size_t(forcedLod),models_.size()-1)).get();}
void RockWorkbench::drawControls(bool characterMode) {
    ImGui::SetNextWindowPos({ImGui::GetIO().DisplaySize.x-365,20},ImGuiCond_FirstUseEver);ImGui::SetNextWindowSize({345,460},ImGuiCond_FirstUseEver);
    ImGui::Begin("Native rock recipe");ImGui::Text("%zu LODs | %zu triangles | %.1f KiB",asset_.lods.size(),asset_.lods[0].mesh.indices.size()/3,asset_.bytes/1024.f);
    if(characterMode)ImGui::TextWrapped("Disable character mode to edit this rock.");
    ImGui::BeginDisabled(characterMode);
    ImGui::InputScalar("Seed",ImGuiDataType_U64,&draft_.seed);
    int radii[3]={int(draft_.radiiMm[0]),int(draft_.radiiMm[1]),int(draft_.radiiMm[2])};if(ImGui::SliderInt3("Radii (mm)",radii,100,5000))for(size_t i=0;i<3;++i)draft_.radiiMm[i]=uint32_t(radii[i]);
    int detail=int(draft_.subdivisions),distortion=int(draft_.distortionPermille),band=int(draft_.bandPermille),count=int(draft_.bands);
    if(ImGui::SliderInt("Detail",&detail,0,4))draft_.subdivisions=uint32_t(detail);
    if(ImGui::SliderInt("Irregularity",&distortion,0,250))draft_.distortionPermille=uint32_t(distortion);
    if(ImGui::SliderInt("Band strength",&band,0,150))draft_.bandPermille=uint32_t(band);
    if(ImGui::SliderInt("Bands",&count,1,32))draft_.bands=uint32_t(count);
    auto run=[&](auto&& action){try{action();error_.clear();}catch(const std::exception& e){error_=e.what();}};
    if(ImGui::Button("Apply recipe"))run([&]{apply(draft_);});ImGui::SameLine();
    if(ImGui::Button("Undo"))run([&]{undo();});ImGui::SameLine();if(ImGui::Button("Redo"))run([&]{redo();});
    ImGui::InputText("File",path_.data(),path_.size());
    if(ImGui::Button("Save recipe"))run([&]{saveRockRecipe(path_.data(),history_.value());});ImGui::SameLine();
    if(ImGui::Button("Reload"))run([&]{apply(loadRockRecipe(path_.data()));});
    ImGui::EndDisabled();
    ImGui::SliderInt("LOD (-1 = auto)",&forcedLod,-1,int(models_.size()-1));
    ImGui::TextWrapped("Collision keeps the highest detail. Orbit to inspect; enable character mode to walk around the rock.");
    if(!error_.empty())ImGui::TextWrapped("%s",error_.c_str());ImGui::End();
}
}

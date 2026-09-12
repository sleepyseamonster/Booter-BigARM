#include "Tools/RockWorkbench.h"
#include <imgui.h>
#include <cstdio>
#include <algorithm>
namespace engine {
RockWorkbench::RockWorkbench(const std::filesystem::path& path,CalibrationRuntime& runtime,const std::filesystem::path& library,bool layeredMaterialsAvailable):runtime_(runtime),layeredMaterialsAvailable_(layeredMaterialsAvailable),history_(loadRockRecipe(path)),draft_(history_.value()) {
    if(path.string().size()>=path_.size())throw std::invalid_argument("Recipe path too long");std::snprintf(path_.data(),path_.size(),"%s",path.string().c_str());rebuild(history_.value());
    const auto output=(path.parent_path()/"Exports/rock-001").string();if(output.size()>=exportPath_.size())throw std::invalid_argument("Export path too long");std::snprintf(exportPath_.data(),exportPath_.size(),"%s",output.c_str());
    if(!library.empty())for(const auto& entry:std::filesystem::directory_iterator(library))if(entry.is_regular_file()&&entry.path().extension()==".json"){
        if(presets_.size()==32)throw std::runtime_error("Rock library supports up to 32 presets");presets_.push_back(entry.path());
    }
    std::sort(presets_.begin(),presets_.end());
}
void RockWorkbench::rebuild(const RockRecipe& recipe) {
    if(recipe.version>=3&&!layeredMaterialsAvailable_)throw std::runtime_error("Fused rocks require the complete layered texture catalog");
    auto next=buildRockAsset(recipe,{1,recipe.version,{},0,"rock"});std::vector<std::unique_ptr<RenderModel>> models;
    for(const auto& lod:next.lods)models.push_back(std::make_unique<RenderModel>(lod.mesh));
    // This authored preview slot survives recipe/generator changes; exported mesh IDs remain generated.
    runtime_.setRock("authored:workbench:rock",next.collision,offset);
    asset_=std::move(next);models_=std::move(models);
}
void RockWorkbench::apply(const RockRecipe& next){history_.apply(next,[&](const auto& r){rebuild(r);});draft_=history_.value();}
void RockWorkbench::undo(){history_.undo([&](const auto& r){rebuild(r);});draft_=history_.value();}
void RockWorkbench::redo(){history_.redo([&](const auto& r){rebuild(r);});draft_=history_.value();}
const RenderModel* RockWorkbench::model(float distance)const{return models_.at(forcedLod<0?rockLod(asset_,distance):std::min(size_t(forcedLod),models_.size()-1)).get();}
void RockWorkbench::drawControls(bool characterMode) {
    const auto display=ImGui::GetIO().DisplaySize;
    ImGui::SetNextWindowPos({std::max(0.f,display.x-425),54},ImGuiCond_Always);
    ImGui::SetNextWindowSize({405,std::max(300.f,display.y-74)},ImGuiCond_Always);
    ImGui::SetNextWindowSizeConstraints({320,280},{std::max(320.f,display.x),std::max(280.f,display.y-54)});
    ImGui::Begin("Rock & formation workbench",nullptr,ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoResize);
    ImGui::PushItemWidth(std::max(100.f,ImGui::GetContentRegionAvail().x-145));ImGui::Text("%zu LODs | %zu triangles | %.1f KiB",asset_.lods.size(),asset_.lods[0].mesh.indices.size()/3,asset_.bytes/1024.f);
    if(characterMode)ImGui::TextWrapped("Disable character mode to edit this rock.");
    ImGui::BeginDisabled(characterMode);
    auto run=[&](auto&& action){try{action();error_.clear();}catch(const std::exception& e){error_=e.what();}};
    if(!presets_.empty()&&ImGui::BeginCombo("Rock preset","Choose a rock...")){
        for(const auto& file:presets_)if(ImGui::Selectable(file.stem().string().c_str()))run([&]{apply(loadRockRecipe(file));forcedLod=0;frameRequested=true;});
        ImGui::EndCombo();
    }
    const bool pending=draft_!=history_.value();
    ImGui::TextUnformatted(pending?"Draft changes - Apply to update the preview":"Preview matches the recipe");
    if(ImGui::Button("Apply recipe"))run([&]{apply(draft_);});ImGui::SameLine();
    if(ImGui::Button("Undo"))run([&]{undo();});ImGui::SameLine();if(ImGui::Button("Redo"))run([&]{redo();});
    if(ImGui::Button("Frame rock / formation"))frameRequested=true;
    ImGui::Separator();
    int generator=int(draft_.version)-1;
    if(ImGui::Combo("Generator",&generator,"Classic v1\0Weathered v2\0Fused volumes v3\0Geological formations v4\0Authored silhouettes v5\0Unity Golden Rock v6\0")){
        if(generator==5&&draft_.version!=6){error_="Choose a Golden Rock preset to load the captured Unity masses.";}else{
        draft_.version=uint32_t(generator+1);
        draft_.formation=std::min(draft_.formation,draft_.version>=4?5u:3u);draft_.members=std::min(draft_.members,draft_.version>=4?16u:8u);
        if(draft_.version<6){draft_.fusion=.0657f;draft_.relaxation=.45f;draft_.authoringScale=1;draft_.samplingMm=50;draft_.calibrationSeed=0;draft_.material.sideShale=360;draft_.material.topShale=420;draft_.material.geologyMm=0;draft_.material.dustColor={.196f,.095f,.047f};for(auto& v:draft_.volumes){v.orientation={0,0,0,1};v.shapeSeed=0;}}
        if(draft_.version<5){draft_.profile=0;draft_.memberEdits.clear();for(auto& v:draft_.volumes){v.primitive=0;v.pitch=v.roll=v.taper=0;}}
        }
    }

    if(!presets_.empty())ImGui::TextWrapped("Presets replace the preview. Save recipe keeps your working copy; originals remain in the library.");
    ImGui::InputScalar("Seed",ImGuiDataType_U64,&draft_.seed);
    if(ImGui::Button("Next seed"))++draft_.seed;
    if(draft_.version==5){int profile=int(draft_.profile);if(ImGui::Combo("Silhouette",&profile,"Auto / role-based\0Fractured boulder\0Broken slab\0Angular chunk\0Tapered shard\0"))draft_.profile=uint32_t(profile);}
    if(draft_.version==6){
        ImGui::TextWrapped("Captured Unity Golden Rock masses. Seed changes the chipped surfaces; presets switch the three approved shapes. Source angles adjust the captured resting pose.");
        ImGui::SliderFloat("Overall scale",&draft_.authoringScale,.1f,5,"%.2f");
        ImGui::SliderFloat("Fusion (m)",&draft_.fusion,0,.2f,"%.4f");
        ImGui::SliderFloat("Relaxation",&draft_.relaxation,0,1,"%.2f");
        int sampling=int(draft_.samplingMm);if(ImGui::SliderInt("Voxel size (mm)",&sampling,5,100))draft_.samplingMm=uint32_t(sampling);
        float damage=draft_.edgeDamage*.001f;if(ImGui::SliderFloat("Edge damage",&damage,0,1,"%.2f"))draft_.edgeDamage=uint32_t(damage*1000+.5f);
    }else{
    int radii[3]={int(draft_.radiiMm[0]),int(draft_.radiiMm[1]),int(draft_.radiiMm[2])};if(ImGui::SliderInt3(draft_.version==5?"Fit envelope (mm)":"Radii (mm)",radii,100,5000))for(size_t i=0;i<3;++i)draft_.radiiMm[i]=uint32_t(radii[i]);
    }
    int detail=int(draft_.subdivisions),distortion=int(draft_.distortionPermille),band=int(draft_.bandPermille),count=int(draft_.bands);
    if(ImGui::SliderInt("Detail",&detail,0,4))draft_.subdivisions=uint32_t(detail);
    if(draft_.version!=6){
    if(ImGui::SliderInt("Irregularity",&distortion,0,250))draft_.distortionPermille=uint32_t(distortion);
    if(ImGui::SliderInt("Band strength",&band,0,150))draft_.bandPermille=uint32_t(band);
    if(ImGui::SliderInt("Bands",&count,1,32))draft_.bands=uint32_t(count);
    }
    if(draft_.version>=3){
        auto knob=[](const char* label,uint32_t& value,int low=0,int high=1000){if(low==0&&high==1000){float v=value*.001f;if(ImGui::SliderFloat(label,&v,0,1,"%.2f"))value=uint32_t(v*1000+.5f);}else{int v=int(value);if(ImGui::SliderInt(label,&v,low,high))value=uint32_t(v);}};
        if(draft_.version!=6&&ImGui::CollapsingHeader("Fused shape",ImGuiTreeNodeFlags_DefaultOpen)){
            knob("Masses",draft_.massCount,1,12);knob("Compaction",draft_.compaction);knob("Lopsidedness",draft_.asymmetry);knob("Major fractures",draft_.fractures);knob("Edge damage",draft_.edgeDamage);
            ImGui::TextWrapped("Shape controls create a seeded mass plan. Source-volume edits take precedence until you return to a seeded plan.");
        }
        if(ImGui::CollapsingHeader("Source volumes")){
            if(draft_.volumes.empty()){
                if(ImGui::Button("Edit generated volumes"))run([&]{draft_.volumes=planRockVolumes(draft_,{1,draft_.version,{},0,"rock"});});
            }else{
                if(draft_.version!=6&&ImGui::Button("Return to seeded plan"))draft_.volumes.clear();
                int remove=-1;
                for(size_t i=0;i<draft_.volumes.size();++i){auto& volume=draft_.volumes[i];ImGui::PushID(int(volume.id));
                    if(ImGui::TreeNode("Volume","%s %u",volume.subtractive?"Cut":"Mass",volume.id)){
                        ImGui::Checkbox("Subtractive cut",&volume.subtractive);
                        ImGui::SliderFloat3("Center",volume.center.data(),-3,3,"%.2f");ImGui::SliderFloat3("Half size",volume.halfSize.data(),.03f,2,"%.2f");ImGui::SliderAngle("Yaw",&volume.yaw,-180,180);
                        if(draft_.version>=5){
                            int primitive=int(volume.primitive);if(ImGui::Combo("Primitive",&primitive,"Rounded block\0Tapered stone\0Wedge\0"))volume.primitive=uint32_t(primitive);
                            if(draft_.version==5)ImGui::SliderFloat("Taper / wedge",&volume.taper,0,.8f,"%.2f");
                            ImGui::SliderAngle("Pitch",&volume.pitch,-45,45);ImGui::SliderAngle("Roll",&volume.roll,-45,45);
                        }
                        if(ImGui::Button("Remove volume"))remove=int(i);ImGui::TreePop();
                    }ImGui::PopID();
                }
                if(remove>=0)draft_.volumes.erase(draft_.volumes.begin()+remove);
                if(draft_.volumes.size()<24&&ImGui::Button("Add mass")){uint32_t next=0;for(const auto& v:draft_.volumes)next=std::max(next,v.id+1);draft_.volumes.push_back({next});}
            }
        }
        if(ImGui::CollapsingHeader("Layered rock material",ImGuiTreeNodeFlags_DefaultOpen)){
            knob("Side grit",draft_.material.grit);knob("Shale",draft_.material.shale);knob("Cracks",draft_.material.cracks);knob("Dust",draft_.material.dust);knob("Surface variation",draft_.material.variation);knob("Worn shine",draft_.material.worn);
            if(draft_.version==6){knob("Side shale",draft_.material.sideShale);knob("Top shale",draft_.material.topShale);knob("Geology scale (mm)",draft_.material.geologyMm,450,3000);}
        }
        if(draft_.version!=6&&ImGui::CollapsingHeader("Formation",ImGuiTreeNodeFlags_DefaultOpen)){
            int kind=int(draft_.formation);if(ImGui::Combo("Layout",&kind,draft_.version>=4?"Single rock\0Connected outcrop\0Scattered rocks\0Supported pile\0Low ridge\0Mixed formations\0":"Single rock\0Connected outcrop\0Scattered rocks\0Rock pile\0"))draft_.formation=uint32_t(kind);
            if(draft_.formation){knob("Members",draft_.members,1,draft_.version>=4?16:8);std::erase_if(draft_.memberEdits,[&](const auto& e){return e.slot>=draft_.members;});knob("Spacing (mm)",draft_.spacingMm,500,12000);}
            if(draft_.version==5&&draft_.formation){
                ImGui::TextWrapped("Edit individual members below. Offsets are relative to the seeded layout; Lift adjusts the seated height. Auto silhouettes use each member's shape choice.");
                if(ImGui::Button("Reset member edits"))draft_.memberEdits.clear();
                try{
                    const auto plan=planRockFormation(draft_,{1,5,{},0,"rock"},[](float,float){return 0.f;});
                    static constexpr const char* roles[]={"Core","Buttress","Pillar","Talus","Slab","Fragment","Base","Middle","Cap"};
                    for(const auto& member:plan){
                        ImGui::PushID(int(member.id.member));
                        if(ImGui::TreeNode("Member","Member %u - %s",uint32_t(member.id.member)+1,roles[size_t(member.role)])){
                            const auto found=std::find_if(draft_.memberEdits.begin(),draft_.memberEdits.end(),[&](const auto& e){return e.slot==member.id.member;});
                            RockMemberEdit edit=found==draft_.memberEdits.end()?RockMemberEdit{}:*found;
                            if(found==draft_.memberEdits.end()){edit.slot=uint32_t(member.id.member);edit.variant=member.variant;}
                            bool changed=ImGui::SliderFloat("Move X (m)",&edit.translation[0],-10,10,"%.2f");
                            changed|=ImGui::SliderFloat("Move Z (m)",&edit.translation[2],-10,10,"%.2f");
                            changed|=ImGui::SliderFloat("Lift (m)",&edit.translation[1],-2,4,"%.2f");
                            changed|=ImGui::SliderFloat3("Proportions",edit.axes.data(),.1f,3,"%.2f");
                            changed|=ImGui::SliderAngle("Turn",&edit.yaw,-180,180);
                            int variant=int(edit.variant);changed|=ImGui::Combo("Shape variant",&variant,draft_.profile==0?"Boulder\0Slab\0Angular chunk\0Shard\0":"Variation 1\0Variation 2\0Variation 3\0Variation 4\0");edit.variant=uint32_t(variant);
                            changed|=ImGui::Checkbox("Seat on ground only",&edit.groundOnly);
                            if(changed){if(found==draft_.memberEdits.end())draft_.memberEdits.push_back(edit);else *found=edit;}
                            if(ImGui::Button("Reset member"))std::erase_if(draft_.memberEdits,[&](const auto& e){return e.slot==member.id.member;});
                            ImGui::TreePop();
                        }ImGui::PopID();
                    }
                }catch(const std::exception& e){error_=e.what();}
            }
            ImGui::TextWrapped("The workbench seats members on its flat ground. Saved formation recipes retain deterministic member identities.");
        }
    }
    ImGui::Separator();
    ImGui::InputText("File",path_.data(),path_.size());
    ImGui::BeginDisabled(draft_!=history_.value());
    if(ImGui::Button("Save recipe"))run([&]{saveRockRecipe(path_.data(),history_.value());});ImGui::EndDisabled();ImGui::SameLine();
    if(ImGui::Button("Reload"))run([&]{apply(loadRockRecipe(path_.data()));});
    ImGui::InputText("Export directory",exportPath_.data(),exportPath_.size());
    ImGui::BeginDisabled(draft_!=history_.value());
    if(ImGui::Button("Export rock"))run([&]{exportStatus_.clear();saveRockResult(exportPath_.data(),asset_.lods.front(),history_.value());exportStatus_="Exported accepted rock to "+std::string(exportPath_.data());});
    ImGui::EndDisabled();
    ImGui::TextWrapped("Apply draft changes before saving or exporting. Export writes the accepted highest-detail mesh and recipe. Choose a new directory for each export.");
    if(!exportStatus_.empty())ImGui::TextWrapped("%s",exportStatus_.c_str());
    ImGui::EndDisabled();
    ImGui::SliderInt("LOD (-1 = auto)",&forcedLod,-1,int(models_.size()-1));
    ImGui::TextWrapped("Collision keeps the highest detail. Orbit to inspect; enable character mode to walk around the rock.");
    if(!error_.empty())ImGui::TextWrapped("%s",error_.c_str());ImGui::PopItemWidth();ImGui::End();
}
}

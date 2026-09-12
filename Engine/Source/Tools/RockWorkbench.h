#pragma once
#include "Runtime/EditHistory.h"
#include "World/Rocks/RockAsset.h"
#include "Rendering/RenderModel.h"
#include "Game/CalibrationRuntime.h"
namespace engine {
class RockWorkbench {
public:
    RockWorkbench(const std::filesystem::path&,CalibrationRuntime&,const std::filesystem::path& library={},bool layeredMaterialsAvailable=true);
    void drawControls(bool characterMode);
    const RenderModel* model(float cameraDistance)const;
    void apply(const RockRecipe&);
    void undo();
    void redo();
    bool hasPendingEdits()const{return draft_!=history_.value();}
    const RockRecipe& recipe()const{return history_.value();}
    const RockAsset& asset()const{return asset_;}
    int forcedLod=-1;
    bool frameRequested=false;
    static constexpr PhysicsVector offset{-3.5f,0,0};
private:
    void rebuild(const RockRecipe&);
    CalibrationRuntime& runtime_;
    bool layeredMaterialsAvailable_;
    EditHistory<RockRecipe> history_;
    RockRecipe draft_;
    RockAsset asset_;
    std::vector<std::unique_ptr<RenderModel>> models_;
    std::array<char,512> path_{},exportPath_{};
    std::string exportStatus_;
    std::string error_;
    std::vector<std::filesystem::path> presets_;
};
}

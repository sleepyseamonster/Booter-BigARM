#pragma once
#include "Runtime/EditHistory.h"
#include "Runtime/InspectionDocument.h"
#include <string>
namespace engine {
class InspectionWorkbench {
public:
    InspectionWorkbench(const FixtureState&,const std::filesystem::path&);
    void draw(FixtureState&,bool enabled);
private:
    EditHistory<FixtureState> history_;
    std::array<char,512> path_{};
    std::string error_;
};
}

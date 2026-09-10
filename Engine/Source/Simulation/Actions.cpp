#include "Simulation/Actions.h"
#include "Persistence/Document.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
#include <string_view>
namespace engine {
namespace {
constexpr std::array<std::string_view,actionCount> actionNames={"move_x","move_z","look_x","look_y","jump","sprint","interact","pause","confirm","cancel"};
constexpr std::array<std::string_view,controlCount> controlNames={"key_w","key_a","key_s","key_d","key_space","key_shift","key_e","key_p","key_escape","key_enter","key_up","key_down","key_left","key_right","pad_left_x","pad_left_y","pad_right_x","pad_right_y","pad_south","pad_east","pad_start","pad_left_stick"};
bool active(float v) {return std::abs(v)>0.001f;}
}
Actions::Actions() {
    bindings_={{Action::MoveX,Control::A,-1},{Action::MoveX,Control::D,1},
        {Action::MoveZ,Control::W,-1},{Action::MoveZ,Control::S,1},
        {Action::MoveX,Control::PadLeftX,1},{Action::MoveZ,Control::PadLeftY,1},
        {Action::LookX,Control::PadRightX,1},{Action::LookY,Control::PadRightY,1},
        {Action::Jump,Control::Space,1},{Action::Jump,Control::PadSouth,1},
        {Action::Sprint,Control::Shift,1},{Action::Sprint,Control::PadLeftStick,1},
        {Action::Interact,Control::E,1},{Action::Pause,Control::P,1},{Action::Pause,Control::PadStart,1},
        {Action::Confirm,Control::Enter,1},{Action::Confirm,Control::PadSouth,1},
        {Action::Cancel,Control::Escape,1},{Action::Cancel,Control::PadEast,1}};
}
void Actions::refresh() {
    std::array<float,actionCount> next{};
    if(focused_) for(const auto& b:bindings_) {
        const bool game=size_t(b.action)<size_t(Action::Pause);
        const bool ui=b.action==Action::Confirm || b.action==Action::Cancel;
        if((game&&!gameplay_) || (ui&&gameplay_) || blocked_[size_t(b.control)]) continue;
        next[size_t(b.action)]+=raw_[size_t(b.control)]*b.scale;
    }
    for(size_t i=0;i<actionCount;++i) {
        const float value=std::clamp(next[i],-1.0f,1.0f);
        actions_[i].pressed|=active(value)&&!active(actions_[i].value);
        actions_[i].released|=!active(value)&&active(actions_[i].value);
        actions_[i].value=value;
    }
}
void Actions::set(Control control,float value) {
    if(size_t(control)>=controlCount || !std::isfinite(value) || value < -1 || value > 1) throw std::invalid_argument("Invalid physical control");
    const auto index=size_t(control);
    raw_[index]=value;
    if(!active(value)) blocked_[index]=false;
    else if(!focused_) blocked_[index]=true;
    refresh();
}
void Actions::inhibitHeld() {
    for(size_t i=0;i<controlCount;++i) blocked_[i]=active(raw_[i]);
    actions_={};refresh();clearEdges();
}
void Actions::focus(bool focused) {if(focused_!=focused) {focused_=focused;inhibitHeld();}}
void Actions::gameplay(bool enabled) {if(gameplay_!=enabled) {gameplay_=enabled;inhibitHeld();}}
void Actions::disconnectGamepad() {
    for(size_t i=size_t(Control::PadLeftX);i<controlCount;++i) {raw_[i]=0;blocked_[i]=false;}
    refresh();clearEdges();
}
void Actions::connectGamepad() {
    // Newly adopted controls must report neutral before a held button or axis
    // can become a new gameplay action, including replacement controllers.
    for(size_t i=size_t(Control::PadLeftX);i<controlCount;++i) {raw_[i]=0;blocked_[i]=true;}
    refresh();clearEdges();
}
void Actions::replaceBindings(std::vector<Binding> bindings) {
    if(bindings.empty() || bindings.size()>64) throw std::invalid_argument("Input binding count must be 1..64");
    for(size_t i=0;i<bindings.size();++i) {
        const auto& b=bindings[i];
        if(size_t(b.action)>=actionCount || size_t(b.control)>=controlCount || !std::isfinite(b.scale) || std::abs(b.scale)!=1)
            throw std::invalid_argument("Invalid input binding");
        for(size_t j=0;j<i;++j) if(bindings[j].action==b.action && bindings[j].control==b.control) throw std::invalid_argument("Duplicate input binding");
    }
    bindings_=std::move(bindings);inhibitHeld();
}
ActionValue Actions::consume(Action action) {
    auto& value=actions_.at(size_t(action));const auto result=value;value.pressed=value.released=false;return result;
}
ActionFrame Actions::takeTick() {const auto result=actions_;clearEdges();return result;}
void Actions::clearEdges() {for(auto& a:actions_) a.pressed=a.released=false;}
void saveBindings(const std::filesystem::path& path,const Actions& actions) {
    Json rows=Json::array();
    for(const auto& b:actions.bindings()) rows.push_back({{"action",actionNames[size_t(b.action)]},{"control",controlNames[size_t(b.control)]},{"scale",b.scale}});
    writeDocument(path,"engine.input-bindings",{{"bindings",rows}});
}
void loadBindings(const std::filesystem::path& path,Actions& actions) {
    const auto p=readDocument(path,"engine.input-bindings");
    if(p.size()!=1 || !p.contains("bindings") || !p.at("bindings").is_array()) throw std::runtime_error("Invalid input document");
    const auto& rows=p.at("bindings");
    if(rows.empty() || rows.size()>64) throw std::runtime_error("Input binding count must be 1..64");
    std::vector<Binding> next;
    for(const auto& row:rows) {
        if(!row.is_object() || row.size()!=3 || !row.at("action").is_string() || !row.at("control").is_string() || !row.at("scale").is_number()) throw std::runtime_error("Invalid input binding row");
        const auto a=std::find(actionNames.begin(),actionNames.end(),row.at("action").get<std::string>());
        const auto c=std::find(controlNames.begin(),controlNames.end(),row.at("control").get<std::string>());
        if(a==actionNames.end() || c==controlNames.end()) throw std::runtime_error("Unknown input action or control");
        next.push_back({Action(a-actionNames.begin()),Control(c-controlNames.begin()),row.at("scale").get<float>()});
    }
    actions.replaceBindings(std::move(next));
}
}

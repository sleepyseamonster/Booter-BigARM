#pragma once
#include <array>
#include <cstdint>
#include <filesystem>
#include <vector>
namespace engine {
enum class Action : uint8_t { MoveX,MoveZ,LookX,LookY,Jump,Sprint,Interact,Pause,Confirm,Cancel,Count };
enum class Control : uint8_t { W,A,S,D,Space,Shift,E,P,Escape,Enter,Up,Down,Left,Right,
    PadLeftX,PadLeftY,PadRightX,PadRightY,PadSouth,PadEast,PadStart,PadLeftStick,Count };
inline constexpr size_t actionCount=size_t(Action::Count),controlCount=size_t(Control::Count);
struct Binding {Action action;Control control;float scale=1;};
struct ActionValue {float value=0;bool pressed=false,released=false;};
using ActionFrame=std::array<ActionValue,actionCount>;
class Actions {
public:
    Actions();
    void set(Control,float value);
    void focus(bool focused);
    void gameplay(bool enabled);
    void disconnectGamepad();
    void connectGamepad();
    void replaceBindings(std::vector<Binding> bindings);
    const std::vector<Binding>& bindings() const {return bindings_;}
    const ActionFrame& peek() const {return actions_;}
    ActionValue consume(Action action);
    ActionFrame takeTick();
    void clearEdges();
private:
    void refresh();
    void inhibitHeld();
    bool focused_=true,gameplay_=true;
    std::array<float,controlCount> raw_{};
    std::array<bool,controlCount> blocked_{};
    ActionFrame actions_{};
    std::vector<Binding> bindings_;
};
void saveBindings(const std::filesystem::path&,const Actions&);
void loadBindings(const std::filesystem::path&,Actions&);
}

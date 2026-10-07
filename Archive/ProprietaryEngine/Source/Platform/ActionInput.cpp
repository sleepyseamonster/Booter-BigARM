#include "Platform/ActionInput.h"
#include <array>
#include <cmath>
namespace engine {
namespace {
constexpr std::array<SDL_Scancode,14> keys={SDL_SCANCODE_W,SDL_SCANCODE_A,SDL_SCANCODE_S,SDL_SCANCODE_D,
    SDL_SCANCODE_SPACE,SDL_SCANCODE_LSHIFT,SDL_SCANCODE_E,SDL_SCANCODE_P,SDL_SCANCODE_ESCAPE,SDL_SCANCODE_RETURN,
    SDL_SCANCODE_UP,SDL_SCANCODE_DOWN,SDL_SCANCODE_LEFT,SDL_SCANCODE_RIGHT};
constexpr std::array<SDL_GamepadButton,4> buttons={SDL_GAMEPAD_BUTTON_SOUTH,SDL_GAMEPAD_BUTTON_EAST,SDL_GAMEPAD_BUTTON_START,SDL_GAMEPAD_BUTTON_LEFT_STICK};
float axis(Sint16 value) {const float v=float(value)/(value<0?32768.0f:32767.0f);return std::abs(v)<.2f?0:std::copysign((std::abs(v)-.2f)/.8f,v);}
}
ActionInput::ActionInput(Actions& actions):actions_(actions) {
    int count=0;auto* ids=SDL_GetGamepads(&count);
    for(int i=0;i<count && !gamepad_;++i) open(ids[i]);
    SDL_free(ids);
}
ActionInput::~ActionInput() {if(gamepad_) SDL_CloseGamepad(gamepad_);}
void ActionInput::open(SDL_JoystickID id) {
    if(!gamepad_) {gamepad_=SDL_OpenGamepad(id);if(gamepad_) actions_.connectGamepad();}
}
void ActionInput::event(const SDL_Event& event) {
    if(event.type==SDL_EVENT_WINDOW_FOCUS_LOST) actions_.focus(false);
    if(event.type==SDL_EVENT_WINDOW_FOCUS_GAINED) actions_.focus(true);
    if(event.type==SDL_EVENT_KEY_DOWN || event.type==SDL_EVENT_KEY_UP) {
        for(size_t i=0;i<keys.size();++i) if(keys[i]==event.key.scancode) actions_.set(Control(i),event.key.down?1:0);
    }
    if(event.type==SDL_EVENT_GAMEPAD_ADDED) open(event.gdevice.which);
    if(event.type==SDL_EVENT_GAMEPAD_REMOVED && gamepad_ && event.gdevice.which==SDL_GetGamepadID(gamepad_)) {
        SDL_CloseGamepad(gamepad_);gamepad_=nullptr;actions_.disconnectGamepad();
        int count=0;auto* ids=SDL_GetGamepads(&count);for(int i=0;i<count&&!gamepad_;++i) open(ids[i]);SDL_free(ids);
    }
    if(gamepad_ && (event.type==SDL_EVENT_GAMEPAD_BUTTON_DOWN || event.type==SDL_EVENT_GAMEPAD_BUTTON_UP) && event.gbutton.which==SDL_GetGamepadID(gamepad_))
        for(size_t i=0;i<buttons.size();++i) if(buttons[i]==event.gbutton.button) actions_.set(Control(size_t(Control::PadSouth)+i),event.gbutton.down?1:0);
}
void ActionInput::sample() {
    // Reconcile held state after focus/hotplug while the router inhibits held
    // controls until neutral. Event edges preserve taps between fixed ticks.
    const auto* keyboard=SDL_GetKeyboardState(nullptr);
    for(size_t i=0;i<keys.size();++i) actions_.set(Control(i),keyboard[keys[i]]?1:0);
    if(gamepad_) {
        for(int i=0;i<4;++i) actions_.set(Control(size_t(Control::PadLeftX)+size_t(i)),axis(SDL_GetGamepadAxis(gamepad_,SDL_GamepadAxis(i))));
        for(size_t i=0;i<buttons.size();++i) actions_.set(Control(size_t(Control::PadSouth)+i),SDL_GetGamepadButton(gamepad_,buttons[i])?1:0);
    }
}
}

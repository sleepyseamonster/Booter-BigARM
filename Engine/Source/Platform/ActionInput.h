#pragma once
#include "Simulation/Actions.h"
#include <SDL3/SDL.h>
namespace engine {
class ActionInput {
public:
    explicit ActionInput(Actions&);
    ~ActionInput();
    ActionInput(const ActionInput&)=delete;
    ActionInput& operator=(const ActionInput&)=delete;
    void event(const SDL_Event&);
    void sample();
    bool hasGamepad() const {return gamepad_!=nullptr;}
private:
    void open(SDL_JoystickID);
    Actions& actions_;
    SDL_Gamepad* gamepad_=nullptr;
};
}

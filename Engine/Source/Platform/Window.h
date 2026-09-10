#pragma once
#include <SDL3/SDL.h>

namespace engine {
class Window {
public:
    explicit Window(bool verification);
    ~Window();
    Window(const Window&) = delete;
    Window& operator=(const Window&) = delete;
    SDL_Window* get() const { return window_; }
    void* nativeHandle() const;
    void pixels(int& width, int& height) const;
private:
    SDL_Window* window_ = nullptr;
    void* metalView_ = nullptr;
};
}

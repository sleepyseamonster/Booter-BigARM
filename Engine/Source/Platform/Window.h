#pragma once
#include <SDL3/SDL.h>

namespace engine {
class Window {
public:
    explicit Window(bool verification,bool windowedFullscreen=false);
    ~Window();
    Window(const Window&) = delete;
    Window& operator=(const Window&) = delete;
    SDL_Window* get() const { return window_; }
    void* nativeHandle() const;
    void pixels(int& width, int& height) const;
    bool setWindowedFullscreen(bool enabled);
    bool windowedFullscreen() const { return windowedFullscreen_; }
private:
    SDL_Window* window_ = nullptr;
    void* metalView_ = nullptr;
    bool windowedFullscreen_ = false;
    SDL_Rect restoredBounds_{0,0,1120,720};
};
}

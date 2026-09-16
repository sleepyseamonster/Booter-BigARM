#include "Platform/Window.h"
#include <stdexcept>
#include <string>
#ifdef __APPLE__
#include <SDL3/SDL_metal.h>
#endif

namespace engine {
Window::Window(bool verification,bool windowedFullscreen,int width,int height,bool highDensity) {
    if(width<1||height<1)throw std::invalid_argument("Window dimensions must be positive");
    SDL_SetHint(SDL_HINT_WINDOW_ACTIVATE_WHEN_SHOWN, "0");
    SDL_SetHint(SDL_HINT_WINDOW_ACTIVATE_WHEN_RAISED, "0");
    SDL_SetHint(SDL_HINT_MAC_BACKGROUND_APP, "1");
    SDL_WindowFlags flags = SDL_WINDOW_RESIZABLE | SDL_WINDOW_HIDDEN;
    if(highDensity)flags|=SDL_WINDOW_HIGH_PIXEL_DENSITY;
    if (verification) flags |= SDL_WINDOW_NOT_FOCUSABLE;
#ifdef __APPLE__
    flags |= SDL_WINDOW_METAL;
#endif
    window_ = SDL_CreateWindow("Booter & BigARM | Engine Foundation", width, height, flags);
    if (!window_) throw std::runtime_error(std::string("Window creation failed: ") + SDL_GetError());
    SDL_SetWindowMinimumSize(window_,800,600);
    if(windowedFullscreen&&!setWindowedFullscreen(true)) {
        const std::string error=SDL_GetError();
        SDL_DestroyWindow(window_);window_=nullptr;
        throw std::runtime_error("Windowed fullscreen failed: "+error);
    }
#ifdef __APPLE__
    metalView_ = SDL_Metal_CreateView(window_);
    if (!metalView_) {
        SDL_DestroyWindow(window_); window_ = nullptr;
        throw std::runtime_error(std::string("Metal view creation failed: ") + SDL_GetError());
    }
#endif
    SDL_ShowWindow(window_);
}
Window::~Window() {
#ifdef __APPLE__
    if (metalView_) SDL_Metal_DestroyView(metalView_);
#endif
    if (window_) SDL_DestroyWindow(window_);
}
void* Window::nativeHandle() const {
#ifdef __APPLE__
    return SDL_Metal_GetLayer(metalView_);
#elif defined(_WIN32)
    return SDL_GetPointerProperty(SDL_GetWindowProperties(window_), SDL_PROP_WINDOW_WIN32_HWND_POINTER, nullptr);
#endif
}
void Window::pixels(int& width, int& height) const {
    if (!SDL_GetWindowSizeInPixels(window_, &width, &height)) { width = 0; height = 0; }
}
bool Window::setWindowedFullscreen(bool enabled) {
    if(enabled==windowedFullscreen_)return true;
    SDL_Rect bounds=restoredBounds_;
    if(enabled) {
        if(!SDL_GetDisplayBounds(SDL_GetDisplayForWindow(window_),&bounds))return false;
        if(!SDL_GetWindowPosition(window_,&restoredBounds_.x,&restoredBounds_.y)||
           !SDL_GetWindowSize(window_,&restoredBounds_.w,&restoredBounds_.h))return false;
    }
    // A borderless desktop window: no display-mode switch or native fullscreen Space.
    if(!SDL_SetWindowBordered(window_,!enabled)||!SDL_SetWindowResizable(window_,!enabled)||
       !SDL_SetWindowSize(window_,bounds.w,bounds.h)||!SDL_SetWindowPosition(window_,bounds.x,bounds.y))return false;
    windowedFullscreen_=enabled;
    return true;
}
}

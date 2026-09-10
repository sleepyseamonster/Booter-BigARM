#include "Platform/Window.h"
#include <stdexcept>
#include <string>
#ifdef __APPLE__
#include <SDL3/SDL_metal.h>
#endif

namespace engine {
Window::Window(bool verification) {
    SDL_SetHint(SDL_HINT_WINDOW_ACTIVATE_WHEN_SHOWN, "0");
    SDL_SetHint(SDL_HINT_WINDOW_ACTIVATE_WHEN_RAISED, "0");
    SDL_SetHint(SDL_HINT_MAC_BACKGROUND_APP, "1");
    SDL_WindowFlags flags = SDL_WINDOW_RESIZABLE | SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WINDOW_HIDDEN;
    if (verification) flags |= SDL_WINDOW_NOT_FOCUSABLE;
#ifdef __APPLE__
    flags |= SDL_WINDOW_METAL;
#endif
    window_ = SDL_CreateWindow("Booter & BigARM | Engine Foundation", 1120, 720, flags);
    if (!window_) throw std::runtime_error(std::string("Window creation failed: ") + SDL_GetError());
    SDL_SetWindowMinimumSize(window_,800,600);
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
}

#include "Platform/Window.h"
#include <SDL3/SDL_main.h>
#include <iostream>
#include <stdexcept>

int main() {
    SDL_SetMainReady();
    if(!SDL_Init(SDL_INIT_VIDEO))return 1;
    try {
        engine::Window window(true,true);
        SDL_SyncWindow(window.get());
        SDL_PumpEvents();
        SDL_Rect display{};int x=0,y=0,w=0,h=0;
        SDL_GetDisplayBounds(SDL_GetDisplayForWindow(window.get()),&display);
        SDL_GetWindowPosition(window.get(),&x,&y);SDL_GetWindowSize(window.get(),&w,&h);
        const auto flags=SDL_GetWindowFlags(window.get());
        if(!window.windowedFullscreen()||!(flags&SDL_WINDOW_BORDERLESS)||(flags&SDL_WINDOW_FULLSCREEN)||
           x!=display.x||y!=display.y||w!=display.w||h!=display.h)
            throw std::runtime_error("Window does not match borderless desktop display bounds");
        std::cout<<"PASS: borderless desktop window "<<w<<"x"<<h<<" at "<<x<<","<<y<<"; native fullscreen flag off\n";
        if(!window.setWindowedFullscreen(false))throw std::runtime_error(SDL_GetError());
        SDL_SyncWindow(window.get());
        SDL_GetWindowSize(window.get(),&w,&h);
        if(window.windowedFullscreen()||(SDL_GetWindowFlags(window.get())&SDL_WINDOW_BORDERLESS)||w!=1120||h!=720)
            throw std::runtime_error("Restored window mismatch");
        std::cout<<"PASS: window restoration\n";
    }catch(const std::exception& error){std::cerr<<error.what()<<" | "<<SDL_GetError()<<'\n';SDL_Quit();return 1;}
    SDL_Quit();
}

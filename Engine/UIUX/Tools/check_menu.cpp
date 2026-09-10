#include "Tools/EngineMenu.h"
#include <imgui_internal.h>
#include <cstdlib>
#include <iostream>

void require(bool value,const char* message) {
    if(!value){std::cerr<<message<<'\n';std::exit(1);}
}
int main() {
    ImGui::CreateContext();
    auto& io=ImGui::GetIO();io.IniFilename=nullptr;io.ConfigFlags|=ImGuiConfigFlags_NavEnableKeyboard;io.DisplaySize={1280,800};io.DeltaTime=1.f/60;
    unsigned char* pixels;int width,height;io.Fonts->GetTexDataAsRGBA32(&pixels,&width,&height);
    ImVec2 button{};bool open=false;ImVec2 popupMin{},popupMax{};float scrollMax=0;
    auto frame=[&] {
        ImGui::NewFrame();
        {
            engine::EngineMenu menu;button=menu.button;open=menu.visible;
            if(open){
                for(int row=0;row<60;++row)ImGui::Text("Engine control %d",row);
                popupMin=ImGui::GetWindowPos();auto size=ImGui::GetWindowSize();popupMax={popupMin.x+size.x,popupMin.y+size.y};
                scrollMax=ImGui::GetScrollMaxY();
            }
        }
        ImGui::Render();
    };
    auto click=[&](ImVec2 at) {
        io.AddMousePosEvent(at.x,at.y);frame();
        io.AddMouseButtonEvent(0,true);frame();io.AddMouseButtonEvent(0,false);frame();frame();
    };
    frame();frame();require(!open,"Menu must start closed");
    require(button.x>1180&&button.y<50,"Button must be compact and at top right");
    click(button);require(open,"Click must open menu");
    require(popupMin.y>=48&&popupMax.x<=1268.1f&&popupMax.y<=788.1f,"Dropdown must stay below button and inside viewport");
    require(scrollMax>0,"Long menu must scroll");
    io.AddKeyEvent(ImGuiKey_Escape,true);frame();io.AddKeyEvent(ImGuiKey_Escape,false);frame();
    require(!open,"Escape must dismiss menu");
    click(button);require(open,"Menu must reopen");
    click({500,400});require(!open,"Outside click must dismiss menu");
    io.DisplaySize={640,480};frame();frame();
    require(button.x>540&&button.x<640,"Button must track resize");
    click(button);require(open,"Menu must open at minimum window size");
    require(popupMin.x>=0&&popupMax.x<=628.1f&&popupMax.y<=468.1f,"Resized dropdown must remain on screen");
    require(scrollMax>0,"Short-window menu must scroll");
    ImGui::DestroyContext();
    std::cout<<"PASS: closed default, top-right anchor, click open, scrolling, Escape, outside dismissal, reopen and 640x480 resize\n";
}

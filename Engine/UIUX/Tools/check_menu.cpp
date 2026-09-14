#include "Tools/EngineMenu.h"
#include "Tools/ViewerControls.h"
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
    io.AddKeyEvent(ImGuiKey_Escape,true);frame();io.AddKeyEvent(ImGuiKey_Escape,false);frame();
    engine::ViewerControls viewer;float exposure=0;ImGuiID advancedId=0,fullscreenId=0,frameId=0;
    auto compactFrame=[&] {
        ImGui::NewFrame();
        {
            const bool wasAdvanced=viewer.advanced;
            engine::EngineMenu menu(wasAdvanced);button=menu.button;open=menu.visible;
            if(open) {
                viewer.draw(exposure,false,true);
                auto* popup=ImGui::GetCurrentWindow();
                advancedId=popup->GetID("Advanced controls");fullscreenId=popup->GetID("Fullscreen");frameId=popup->GetID("Frame scene");
                if(!wasAdvanced)require(ImGui::GetWindowSize().y<=240,"Simple menu must be compact");
            }
        }
        ImGui::Render();
    };
    compactFrame();compactFrame();require(!viewer.advanced&&!open,"Viewer must start with no advanced panels or menu");
    io.AddMousePosEvent(button.x,button.y);compactFrame();
    io.AddMouseButtonEvent(0,true);compactFrame();io.AddMouseButtonEvent(0,false);compactFrame();compactFrame();
    require(open,"Compact menu opens");
    ImGui::ActivateItemByID(frameId);compactFrame();require(viewer.frameRequested,"Frame scene requests framing");
    ImGui::ActivateItemByID(fullscreenId);compactFrame();require(viewer.fullscreenRequested,"Fullscreen control requests display change");
    ImGui::ActivateItemByID(advancedId);compactFrame();require(viewer.advanced,"Advanced controls can be revealed");
    ImGui::ActivateItemByID(advancedId);compactFrame();require(!viewer.advanced,"Advanced controls can be hidden again");
    ImGui::DestroyContext();
    std::cout<<"PASS: closed default, top-right anchor, click open, scrolling, Escape, outside dismissal, reopen, 640x480 resize, compact viewer, frame/fullscreen requests and advanced toggle\n";
}

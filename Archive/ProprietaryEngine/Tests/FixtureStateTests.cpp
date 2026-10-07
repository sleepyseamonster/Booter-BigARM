#include "Core/FixtureState.h"
#include "Core/SceneCameraControls.h"
#include <cmath>
#include <iostream>
#include <limits>
#include <stdexcept>

void require(bool condition, const char* name) { if (!condition) throw std::runtime_error(name); }
int main() {
    try {
        engine::FixtureState s;
        const auto old = s.eye();
        s.orbit(100, 30, true); s.zoom(100, true);
        require(s.eye() == old, "UI capture must prevent camera changes");
        s.orbit(100, 30, false);
        require(s.eye() != old, "Uncaptured drag must orbit");
        s.orbit(0, 10000, false); s.zoom(10000, false);
        require(s.pitch == 1.25f && s.distance == 2.5f, "Close/pole clamps");
        s.orbit(0, -10000, false); s.zoom(-10000, false);
        require(s.pitch == -0.15f && s.distance == 30.0f, "Far/lower clamps");
        s.yaw = std::numeric_limits<float>::infinity(); s.constrain();
        require(std::isfinite(s.eye()[0]), "Nonfinite camera repaired");
        engine::FixtureState pan;pan.yaw=0;pan.pitch=0;pan.distance=10;pan.fieldOfView=90;
        const auto eye=pan.eye();pan.pan(10,10,100,false);
        require(std::abs(pan.viewOffset[0]+2)<.0001f&&std::abs(pan.viewOffset[1]-2)<.0001f&&pan.viewOffset[2]==0,"Pan follows screen right/up at the target depth");
        require(pan.yaw==0&&pan.pitch==0&&pan.distance==10&&std::abs(pan.eye()[0]-eye[0]+2)<.0001f,"Pan translates without rotating or zooming");
        const auto translated=pan.viewOffset;pan.pan(100,100,100,true);pan.pan(100,100,0,false);require(pan.viewOffset==translated,"UI capture and zero viewport prevent pan");
        pan.pan(-10,-10,100,false);require(std::abs(pan.viewOffset[0])<.0001f&&std::abs(pan.viewOffset[1])<.0001f,"Reverse pan restores pivot");
        pan.yaw=1.5707963268f;pan.pan(10,0,100,false);require(std::abs(pan.viewOffset[2]-2)<.0001f,"Pan axis follows the orbit camera");
        using engine::SceneDrag;engine::SceneCameraControls controls;
        auto drag=[&](bool left,bool middle,bool right,bool alt,bool space,bool mouse=false,bool keyboard=false,bool focus=true){return controls.update(left,middle,right,alt,space,mouse,keyboard,focus);};
        auto release=[&]{drag(false,false,false,false,false);};
        require(drag(true,false,false,false,false)==SceneDrag::None,"Plain left click is not camera motion");release();
        require(drag(true,false,false,true,false)==SceneDrag::Orbit,"Alt/Option left drag orbits");
        require(drag(true,false,false,true,false,true)==SceneDrag::Orbit,"Owned scene gesture continues across an inspector");
        require(drag(true,false,false,false,false)==SceneDrag::None,"Releasing modifier ends its gesture");release();
        require(drag(true,false,false,false,true)==SceneDrag::Pan,"Space left drag pans");release();
        require(drag(false,true,false,false,false)==SceneDrag::Pan,"Middle drag pans");release();
        require(drag(false,false,true,false,false)==SceneDrag::Orbit,"Right drag retains orbit");release();
        require(drag(true,false,false,true,false,true)==SceneDrag::None&&drag(true,false,false,true,false)==SceneDrag::None,"UI-origin drags cannot turn into scene drags");release();
        require(drag(true,false,false,false,true,false,true)==SceneDrag::None,"Typing Space does not start a camera gesture");release();
        drag(false,true,false,false,false);require(drag(false,true,false,false,false,false,false,false)==SceneDrag::None&&drag(false,true,false,false,false)==SceneDrag::None,"Focus loss cancels and held-button return cannot restart");
        engine::ClipRect r{};
        require(engine::clipRect(-20,-10,250,190,200,100,r) && r.x==0 && r.y==0 && r.width==200 && r.height==100,
                "Scissor must clamp to framebuffer");
        require(!engine::clipRect(20,20,10,10,200,100,r), "Empty scissor rejected");
        require(!engine::clipRect(0,0,10,10,0,100,r), "Zero framebuffer suspended");
        require(!engine::clipRect(NAN,0,10,10,200,100,r), "Nonfinite scissor rejected");
        std::cout << "PASS: input capture and gesture ownership, orbit/pan/recenter math, camera bounds, nonfinite state, clipped/empty/zero framebuffer\n";
        return 0;
    } catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}

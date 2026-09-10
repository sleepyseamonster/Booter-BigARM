#include "Core/FixtureState.h"
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
        engine::ClipRect r{};
        require(engine::clipRect(-20,-10,250,190,200,100,r) && r.x==0 && r.y==0 && r.width==200 && r.height==100,
                "Scissor must clamp to framebuffer");
        require(!engine::clipRect(20,20,10,10,200,100,r), "Empty scissor rejected");
        require(!engine::clipRect(0,0,10,10,0,100,r), "Zero framebuffer suspended");
        require(!engine::clipRect(NAN,0,10,10,200,100,r), "Nonfinite scissor rejected");
        std::cout << "PASS: input capture, orbit, camera bounds, nonfinite state, clipped/empty/zero framebuffer\n";
        return 0;
    } catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}

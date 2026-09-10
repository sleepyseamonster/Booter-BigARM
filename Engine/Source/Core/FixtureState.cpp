#include "Core/FixtureState.h"
#include <algorithm>
#include <cmath>

namespace engine {
FixtureState geometryProofState() {
    FixtureState state;
    state.mesh=1; state.objectScale={2.0f,0.65f,1.1f}; state.objectYaw=0.55f; state.showNormals=true;
    return state;
}
void FixtureState::constrain() {
    if (!std::isfinite(yaw)) yaw = 0.65f;
    if (!std::isfinite(pitch)) pitch = 0.28f;
    if (!std::isfinite(distance)) distance = 7.5f;
    if (!std::isfinite(fieldOfView)) fieldOfView = 55.0f;
    yaw = std::remainder(yaw, 6.2831853f);
    pitch = std::clamp(pitch, -0.15f, 1.25f);
    distance = std::clamp(distance, 2.5f, 30.0f);
    fieldOfView = std::clamp(fieldOfView, 30.0f, 90.0f);
    mesh = std::clamp(mesh, 0, 2);
    exposure=std::isfinite(exposure)?std::clamp(exposure,-4.0f,4.0f):0.0f;
    if (!std::isfinite(objectYaw)) objectYaw = 0;
    for (float& scale : objectScale) scale = std::isfinite(scale)?std::clamp(scale,0.2f,3.0f):1.5f;
}
void FixtureState::orbit(float dx, float dy, bool captured) {
    if (captured) return;
    yaw -= dx * 0.008f;
    pitch += dy * 0.008f;
    constrain();
}
void FixtureState::zoom(float wheel, bool captured) {
    if (captured) return;
    distance -= wheel * 0.5f;
    constrain();
}
std::array<float, 3> FixtureState::eye() const {
    return {distance * std::cos(pitch) * std::sin(yaw),
            0.85f + distance * std::sin(pitch),
            distance * std::cos(pitch) * std::cos(yaw)};
}
bool clipRect(float x1, float y1, float x2, float y2, int width, int height, ClipRect& result) {
    if (width <= 0 || height <= 0 || width > 65535 || height > 65535 ||
        !std::isfinite(x1) || !std::isfinite(y1) || !std::isfinite(x2) || !std::isfinite(y2)) return false;
    const int left = static_cast<int>(std::floor(std::clamp(x1, 0.0f, float(width))));
    const int top = static_cast<int>(std::floor(std::clamp(y1, 0.0f, float(height))));
    const int right = static_cast<int>(std::ceil(std::clamp(x2, 0.0f, float(width))));
    const int bottom = static_cast<int>(std::ceil(std::clamp(y2, 0.0f, float(height))));
    if (right <= left || bottom <= top) return false;
    result = {uint16_t(left), uint16_t(top), uint16_t(right-left), uint16_t(bottom-top)};
    return true;
}
}

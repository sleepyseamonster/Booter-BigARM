#pragma once
#include <array>
#include <cstdint>

namespace engine {
struct FixtureState {
    float yaw = 0.65f;
    float pitch = 0.28f;
    float distance = 7.5f;
    float fieldOfView = 55.0f;
    float objectYaw = 0.0f;
    int mesh = 0;
    std::array<float, 3> objectScale{1.5f, 1.5f, 1.5f};
    bool showNormals = false;
    std::array<float, 3> color{0.68f, 0.39f, 0.22f};
    float lightAzimuth = 0.8f;
    float lightIntensity = 0.85f;
    float exposure = 0.0f;
    bool shadows = true;
    float shadowBias = 0.0015f;
    float roughness = 0.7f;
    float metallic = 0.0f;
    float normalStrength = 1.0f;
    float textureScale = 1.0f;
    float ambient = 0.3f;
    bool surfaceTextures = true;
    void orbit(float dx, float dy, bool captured);
    void zoom(float wheel, bool captured);
    void constrain();
    std::array<float, 3> eye() const;
};
// Repeatable technical fixture settings; not final camera or game tuning.
FixtureState geometryProofState();
struct ClipRect { uint16_t x, y, width, height; };
bool clipRect(float x1, float y1, float x2, float y2, int width, int height, ClipRect& result);
}

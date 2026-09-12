#pragma once
#include <array>

namespace engine {
// Presentation data only. Colors are linear RGB; intensity remains relative.
struct EnvironmentSettings {
    float sunElevation = 0.7328151f;
    std::array<float,3> sunColor{1.0f,0.95f,0.86f};
    std::array<float,3> zenith{0.18f,0.32f,0.58f};
    std::array<float,3> horizon{0.62f,0.70f,0.78f};
    std::array<float,3> ground{0.16f,0.13f,0.10f};
    bool sky = true;
    bool toneMapping = true;
    bool operator==(const EnvironmentSettings&) const = default;
};
}

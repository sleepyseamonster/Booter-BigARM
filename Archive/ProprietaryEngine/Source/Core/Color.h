#pragma once
#include <algorithm>
#include <cmath>
namespace engine {
inline float srgbToLinear(float value) {
    return value<=.04045f ? value/12.92f : std::pow((value+.055f)/1.055f,2.4f);
}
inline float linearToSrgb(float value) {
    value=std::max(value,0.0f);
    return value<=.0031308f ? value*12.92f : 1.055f*std::pow(value,1.0f/2.4f)-.055f;
}
}

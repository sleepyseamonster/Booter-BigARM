#pragma once
#include <array>
#include <cmath>
#include <stdexcept>

namespace engine {
using SunVector=std::array<float,3>;
inline float sunDot(SunVector a,SunVector b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
inline SunVector sunCross(SunVector a,SunVector b){return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
inline SunVector sunUnit(SunVector a){const float n=std::sqrt(sunDot(a,a));if(!std::isfinite(n)||n<1e-6f)throw std::invalid_argument("Invalid sun/camera direction");for(auto& v:a)v/=n;return a;}
struct SunCascade {SunVector center;float radius,texel;};
// Sphere fit keeps the projection extent independent of camera rotation. Snap in
// an origin-anchored light basis, not a camera-relative basis that moves each frame.
inline SunCascade fitSunCascade(SunVector eye,SunVector forward,SunVector light,float fovDegrees,float aspect,float nearDistance,float farDistance,unsigned tile) {
    if(!std::isfinite(nearDistance)||!std::isfinite(farDistance)||!std::isfinite(fovDegrees)||!std::isfinite(aspect)||fovDegrees<=0||fovDegrees>=175||aspect<=0||nearDistance<0||farDistance<=nearDistance||tile<16)
        throw std::invalid_argument("Invalid cascade range");
    for(float v:eye)if(!std::isfinite(v))throw std::invalid_argument("Invalid cascade eye");
    forward=sunUnit(forward);light=sunUnit(light);
    const auto right=sunUnit(sunCross(std::abs(light[1])>.9999f?SunVector{0,0,1}:SunVector{0,1,0},light));
    const auto up=sunCross(light,right);
    const float tangent=std::tan(fovDegrees*.00872664626f),half=(farDistance-nearDistance)*.5f;
    float radius=std::sqrt(half*half+farDistance*farDistance*tangent*tangent*(1+aspect*aspect));
    radius=std::ceil(radius*float(tile)/float(tile-4)*16)/16; // PCF/snap guard.
    const float texel=2*radius/float(tile);
    SunVector center=eye;for(size_t i=0;i<3;++i)center[i]+=forward[i]*(farDistance+nearDistance)*.5f;
    const float x=sunDot(center,right),y=sunDot(center,up);
    const float dx=std::round(x/texel)*texel-x,dy=std::round(y/texel)*texel-y;
    for(size_t i=0;i<3;++i)center[i]+=right[i]*dx+up[i]*dy;
    return {center,radius,texel};
}
}

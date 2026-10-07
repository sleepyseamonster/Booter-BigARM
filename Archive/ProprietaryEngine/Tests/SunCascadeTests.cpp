#include "Core/SunCascade.h"
#include <iostream>
using namespace engine;
int main(){try{
    auto require=[](bool ok){if(!ok)throw std::runtime_error("Cascade invariant failed");};
    const SunVector forward{0,0,-1},eye{0,0,12.05f},light=sunUnit({.6f,.4f,.7f});
    const auto base=fitSunCascade(eye,forward,light,55,1.6f,.1f,24,2048);
    const auto right=sunUnit(sunCross({0,1,0},light));auto shifted=eye;
    for(size_t i=0;i<3;++i)shifted[i]+=right[i]*base.texel*.2f;
    const auto stable=fitSunCascade(shifted,forward,light,55,1.6f,.1f,24,2048);
    for(size_t i=0;i<3;++i)require(std::abs(base.center[i]-stable.center[i])<.0001f);
    const auto rotated=fitSunCascade(eye,{1,0,0},light,55,1.6f,.1f,24,2048);require(base.radius==rotated.radius);
    for(float far:{24.f,128.f})for(float az:{0.f,1.f,2.f}) {
        const SunVector f{std::sin(az),0,-std::cos(az)},r{std::cos(az),0,std::sin(az)},e{-500,12,700};
        const auto fit=fitSunCascade(e,f,light,55,1.6f,.1f,far,2048);
        for(float depth:{.1f,far})for(float x:{-1.f,1.f})for(float y:{-1.f,1.f}) {
            SunVector p;for(size_t i=0;i<3;++i)p[i]=e[i]+f[i]*depth+r[i]*x*depth*std::tan(55*.00872664626f)*1.6f;
            p[1]+=y*depth*std::tan(55*.00872664626f);
            for(size_t i=0;i<3;++i)p[i]-=fit.center[i];require(std::sqrt(sunDot(p,p))<=fit.radius);
        }
    }
    bool rejected=false;try{fitSunCascade(eye,forward,light,55,1,.1f,0,2048);}catch(...){rejected=true;}require(rejected);
    std::cout<<"PASS: camera rotation invariant extent, subtexel stability, frustum coverage and invalid range rejection\n";
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}

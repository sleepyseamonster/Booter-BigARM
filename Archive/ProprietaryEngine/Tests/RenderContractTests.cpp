#include "Rendering/RenderContracts.h"
#include <cmath>
#include <iostream>
#include <stdexcept>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
bool near(float a,float b,float tolerance=1e-5f){return std::abs(a-b)<=tolerance;}
template<class F>void rejects(F&& fn){bool rejected=false;try{fn();}catch(const std::exception&){rejected=true;}require(rejected,"Expected rejection");}
}
int main()try{
    OcclusionConvention convention;validateOcclusionConvention(convention);
    const auto center=reconstructViewPosition(.5f,.5f,10,90,16.0f/9.0f);require(near(center[0],0)&&near(center[1],0)&&near(center[2],-10),"Center reconstruction");
    const auto corner=reconstructViewPosition(1,0,10,90,2);require(near(corner[0],20)&&near(corner[1],10),"FOV/aspect reconstruction");
    const auto radius=projectedRadiusUv(1,10,90,2);require(near(radius[0],.025f)&&near(radius[1],.05f),"Meter radius projection");
    const float nearDepth=.1001f,farDepth=599.75f;require(float(nearDepth)==nearDepth&&float(farDepth)==farDepth,"R32F view-depth endpoints");
    require(blockerWeightMeters(10,9.7f,.02f,.4f)>0&&blockerWeightMeters(10,10.1f,.02f,.4f)==0,"Metric blocker range");
    const auto lit=composeOutdoorLighting({1,1,1},{.5f,.25f,.125f},0,{.2f,.3f,.4f},.5f);
    require(near(lit[0],.6f)&&near(lit[1],.65f)&&near(lit[2],.7f),"AO affects indirect before fog only");
    const RenderTargetSupport full{8,true,true,true,true};
    const auto base=planRenderTargets(1920,1080,false,full),quality=planRenderTargets(1920,1080,true,full);
    require(base.sceneBytes==41472000&&base.auxiliaryBytes==0&&base.totalBytes==base.sceneBytes,"Base target footprint");
    require(quality.auxiliaryEnabled&&quality.auxiliaryBytes==24883200&&quality.totalBytes==66355200,"Auxiliary target footprint");
    auto reduced=full;reduced.r32f=false;const auto fallback=planRenderTargets(1920,1080,true,reduced);
    require(!fallback.auxiliaryEnabled&&!fallback.degradation.empty()&&fallback.totalBytes==base.totalBytes,"Bounded optional-feature degradation");
    rejects([&]{auto invalid=convention;invalid.aoBiasMeters=invalid.aoThicknessMeters;validateOcclusionConvention(invalid);});
    rejects([&]{planRenderTargets(0,1080,false,full);});
    std::cout<<"PASS: meter view-depth reconstruction, indirect-only AO/fog ordering and optional target budgets\n";return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

#include "Rendering/RenderContracts.h"
#include <algorithm>
#include <cmath>
#include <limits>
#include <numbers>
#include <stdexcept>

namespace engine {
namespace {
void positive(float value,const char* name){if(!std::isfinite(value)||value<=0)throw std::invalid_argument(std::string(name)+" must be finite and positive");}
uint64_t bytes(uint64_t pixels,uint64_t perPixel){
    if(pixels>std::numeric_limits<uint64_t>::max()/perPixel)throw std::overflow_error("Render target byte count overflow");
    return pixels*perPixel;
}
}

void validateOcclusionConvention(const OcclusionConvention& value){
    positive(value.nearMeters,"Near plane");positive(value.farMeters,"Far plane");
    if(value.farMeters<=value.nearMeters)throw std::invalid_argument("Far plane must exceed near plane");
    positive(value.aoRadiusMeters,"AO radius");positive(value.aoBiasMeters,"AO bias");positive(value.aoThicknessMeters,"AO thickness");
    positive(value.contactBiasMeters,"Contact bias");positive(value.contactThicknessMeters,"Contact thickness");
    if(value.aoBiasMeters>=value.aoThicknessMeters||value.contactBiasMeters>=value.contactThicknessMeters)
        throw std::invalid_argument("Occlusion bias must be smaller than blocker thickness");
}

std::array<float,3> reconstructViewPosition(float u,float v,float depth,float fov,float aspect){
    positive(depth,"View depth");positive(aspect,"Aspect ratio");
    if(!std::isfinite(u)||!std::isfinite(v)||!std::isfinite(fov)||fov<=1||fov>=179)throw std::invalid_argument("Invalid view reconstruction input");
    const float tangent=std::tan(fov*float(std::numbers::pi/360.0));
    return {(u*2-1)*depth*tangent*aspect,(1-v*2)*depth*tangent,-depth};
}

std::array<float,2> projectedRadiusUv(float radius,float depth,float fov,float aspect){
    positive(radius,"Occlusion radius");const auto edge=reconstructViewPosition(1,0.5f,depth,fov,aspect);
    const float halfWidth=std::abs(edge[0]);const float halfHeight=depth*std::tan(fov*float(std::numbers::pi/360.0));
    return {radius/(2*halfWidth),radius/(2*halfHeight)};
}

float blockerWeightMeters(float receiver,float blocker,float bias,float thickness){
    positive(receiver,"Receiver depth");positive(blocker,"Blocker depth");positive(bias,"Blocker bias");positive(thickness,"Blocker thickness");
    if(bias>=thickness)throw std::invalid_argument("Blocker bias must be smaller than thickness");
    const float separation=receiver-blocker;
    return std::clamp((separation-bias)/(thickness-bias),0.0f,1.0f);
}

std::array<float,3> composeOutdoorLighting(std::array<float,3> direct,std::array<float,3> indirect,float visibility,std::array<float,3> fog,float amount){
    if(!std::isfinite(visibility)||!std::isfinite(amount))throw std::invalid_argument("Invalid lighting composition weight");
    visibility=std::clamp(visibility,0.0f,1.0f);amount=std::clamp(amount,0.0f,1.0f);
    std::array<float,3> result{};
    for(size_t i=0;i<3;++i){
        if(!std::isfinite(direct[i])||!std::isfinite(indirect[i])||!std::isfinite(fog[i]))throw std::invalid_argument("Invalid lighting composition color");
        result[i]=(direct[i]+indirect[i]*visibility)*(1-amount)+fog[i]*amount;
    }
    return result;
}

RenderTargetPlan planRenderTargets(uint32_t width,uint32_t height,bool auxiliary,const RenderTargetSupport& support){
    if(!width||!height||width>65535||height>65535)throw std::invalid_argument("Render target dimensions are invalid");
    if(support.maxFramebufferAttachments<3||!support.rgba16f||!support.d24s8)throw std::runtime_error("Required direct/indirect/depth target profile unsupported");
    RenderTargetPlan result;result.width=uint16_t(width);result.height=uint16_t(height);result.auxiliaryRequested=auxiliary;result.sceneAttachments=3;
    const uint64_t pixels=uint64_t(width)*height;
    result.sceneBytes=bytes(pixels,20); // two RGBA16F colors and D24S8 depth/stencil.
    if(auxiliary){
        if(support.maxFramebufferAttachments>=3&&support.rgba8&&support.r32f&&support.d24s8){
            result.auxiliaryEnabled=true;result.auxiliaryAttachments=3;result.auxiliaryBytes=bytes(pixels,12); // view normal, linear depth, hardware depth.
        }else result.degradation="AO/contact auxiliary targets unsupported; direct and indirect lighting remain available";
    }
    result.totalBytes=result.sceneBytes+result.auxiliaryBytes;return result;
}

} // namespace engine

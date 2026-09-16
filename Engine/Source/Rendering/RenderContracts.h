#pragma once
#include <array>
#include <cstddef>
#include <cstdint>
#include <string>

namespace engine {

struct OcclusionConvention {
    float nearMeters=0.1f;
    float farMeters=600.0f;
    float aoRadiusMeters=1.0f;
    float aoBiasMeters=0.025f;
    float aoThicknessMeters=0.35f;
    float contactBiasMeters=0.02f;
    float contactThicknessMeters=0.15f;
};

void validateOcclusionConvention(const OcclusionConvention&);
std::array<float,3> reconstructViewPosition(float u,float v,float viewDepthMeters,float verticalFovDegrees,float aspect);
std::array<float,2> projectedRadiusUv(float radiusMeters,float viewDepthMeters,float verticalFovDegrees,float aspect);
float blockerWeightMeters(float receiverDepthMeters,float blockerDepthMeters,float biasMeters,float thicknessMeters);
std::array<float,3> composeOutdoorLighting(std::array<float,3> direct,std::array<float,3> indirect,
                                           float aoVisibility,std::array<float,3> fog,float fogAmount);

struct RenderTargetSupport {
    uint32_t maxFramebufferAttachments=0;
    bool rgba16f=false;
    bool rgba8=false;
    bool r32f=false;
    bool d24s8=false;
};

struct RenderTargetPlan {
    uint16_t width=0,height=0;
    bool auxiliaryRequested=false;
    bool auxiliaryEnabled=false;
    uint32_t sceneAttachments=0,auxiliaryAttachments=0;
    uint64_t sceneBytes=0,auxiliaryBytes=0,totalBytes=0;
    std::string degradation;
};

RenderTargetPlan planRenderTargets(uint32_t width,uint32_t height,bool auxiliary,const RenderTargetSupport&);

} // namespace engine

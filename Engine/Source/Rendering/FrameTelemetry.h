#pragma once
#include "Persistence/Document.h"
#include <bgfx/bgfx.h>
#include <array>
#include <chrono>
#include <filesystem>

namespace engine {
enum class FramePhase : size_t {Simulation, Streaming, Draw, Count};
enum class PresentMode {VSync, Immediate};
struct RenderConfiguration {
    PresentMode present=PresentMode::VSync;
    std::filesystem::path trace;
    static RenderConfiguration fromEnvironment();
};
class FrameTelemetry {
public:
    using Clock=std::chrono::steady_clock;
    template<class F> void measure(FramePhase phase,F&& work) {
        if(!enabled_){work();return;}
        const auto start=Clock::now();work();
        phases_[size_t(phase)]+=std::chrono::duration<double,std::milli>(Clock::now()-start).count();
    }
    void enable(bool enabled){enabled_=enabled;count_=next_=0;observed_=0;last_={};phases_={};}
    uint32_t finish(bool technical);
    void save(const RenderConfiguration&,const char* backend);
private:
    struct View {uint16_t id=0;uint32_t gpuFrame=0;double ms=-1;};
    struct Sample {
        uint32_t submittedFrame=0,gpuFrame=0,draws=0;uint16_t width=0,height=0,textures=0;
        int64_t textureBytes=0;double interval=-1,frameCall=0,gpu=-1;
        bool technical=false;std::array<double,size_t(FramePhase::Count)> phases{};
        std::array<View,8> views{};size_t viewCount=0;
    };
    static constexpr size_t capacity=128;
    std::array<Sample,capacity> samples_{};
    std::array<double,size_t(FramePhase::Count)> phases_{};
    size_t count_=0,next_=0;uint64_t observed_=0;bool enabled_=false;
    Clock::time_point last_{};
};
}

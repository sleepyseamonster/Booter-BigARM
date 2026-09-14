#include "Rendering/FrameTelemetry.h"
#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <string>
#include <vector>

namespace engine {
RenderConfiguration RenderConfiguration::fromEnvironment(){
    RenderConfiguration config;
    if(const auto* v=std::getenv("ENGINE_PRESENT")) {
        const std::string mode(v);
        if(mode=="immediate")config.present=PresentMode::Immediate;
        else if(mode!="vsync")throw std::invalid_argument("ENGINE_PRESENT must be vsync or immediate");
    }
    if(const auto* v=std::getenv("ENGINE_FRAME_TRACE")) {
        config.trace=v;
        if(config.trace.empty()||std::filesystem::exists(config.trace)||!std::filesystem::is_directory(config.trace.parent_path()))
            throw std::invalid_argument("ENGINE_FRAME_TRACE requires a new file in an existing directory");
    }
    return config;
}
namespace {
double duration(int64_t begin,int64_t end,int64_t frequency){return frequency>0&&begin>0&&end>begin?double(end-begin)*1000.0/double(frequency):-1;}
Json timing(double value){return value>=0?Json(value):Json(nullptr);}
Json distribution(std::vector<double> values){
    if(values.empty())return nullptr;
    std::sort(values.begin(),values.end());
    return {{"samples",values.size()},{"median_ms",values[(values.size()-1)/2]},{"p95_ms",values[size_t(std::ceil(values.size()*.95))-1]},{"max_ms",values.back()}};
}
}
uint32_t FrameTelemetry::finish(bool technical){
    if(!enabled_)return bgfx::frame();
    const auto begin=Clock::now();const auto frame=bgfx::frame();const auto end=Clock::now();
    auto& sample=samples_[next_];sample={};sample.submittedFrame=frame;sample.technical=technical;
    sample.frameCall=std::chrono::duration<double,std::milli>(end-begin).count();
    sample.interval=last_==Clock::time_point{}?-1:std::chrono::duration<double,std::milli>(end-last_).count();last_=end;
    sample.phases=phases_;phases_={};
    const auto* stats=bgfx::getStats();
    sample.gpuFrame=stats->gpuFrameNum;sample.gpu=duration(stats->gpuTimeBegin,stats->gpuTimeEnd,stats->gpuTimerFreq);
    sample.draws=stats->numDraw;sample.textures=stats->numTextures;sample.textureBytes=stats->textureMemoryUsed;
    sample.width=stats->width;sample.height=stats->height;
    sample.viewCount=std::min(size_t(stats->numViews),sample.views.size());
    for(size_t i=0;i<sample.viewCount;++i){const auto& v=stats->viewStats[i];sample.views[i]={v.view,v.gpuFrameNum,duration(v.gpuTimeBegin,v.gpuTimeEnd,stats->gpuTimerFreq)};}
    ++observed_;count_=std::min(count_+1,capacity);next_=(next_+1)%capacity;return frame;
}
void FrameTelemetry::save(const RenderConfiguration& config,const char* backend){
    if(!enabled_||config.trace.empty())return;
    Json rows=Json::array();std::vector<double> intervals,draw,wait,gpu;
    uint32_t lastGpu=UINT32_MAX;
    for(size_t n=0;n<count_;++n){const auto& s=samples_[(next_+capacity-count_+n)%capacity];
        Json views=Json::array();for(size_t i=0;i<s.viewCount;++i)views.push_back({{"view",s.views[i].id},{"gpu_frame",s.views[i].gpuFrame},{"gpu_ms",timing(s.views[i].ms)}});
        rows.push_back({{"submitted_frame",s.submittedFrame},{"gpu_frame",s.gpuFrame},{"technical_paced",s.technical},{"width",s.width},{"height",s.height},
            {"interval_ms",timing(s.interval)},{"simulation_cpu_ms",s.phases[0]},{"streaming_cpu_ms",s.phases[1]},{"draw_cpu_ms",s.phases[2]},
            {"frame_call_cpu_ms",s.frameCall},{"gpu_ms",timing(s.gpu)},{"draw_calls",s.draws},{"textures",s.textures},{"texture_bytes_estimate",s.textureBytes},{"views",views}});
        if(s.interval>=0)intervals.push_back(s.interval);draw.push_back(s.phases[2]);wait.push_back(s.frameCall);
        if(s.gpu>=0&&s.gpuFrame!=lastGpu){gpu.push_back(s.gpu);lastGpu=s.gpuFrame;}
    }
    writeDocument(config.trace,"engine.frame-trace",{{"backend",backend},{"present",config.present==PresentMode::VSync?"vsync":"immediate"},
        {"observed_frames",observed_},{"retained_frames",count_},{"capacity",capacity},{"interval",distribution(intervals)},
        {"draw_cpu",distribution(draw)},{"frame_call_cpu",distribution(wait)},{"gpu_unique_frames",distribution(gpu)},{"frames",rows},
        {"limits","Last 128 frames, optional profiling overhead; GPU stats are delayed and identified separately. CPU frame-call duration includes API synchronization, not measured display latency. Technical pacing and capture work invalidate interactive benchmark claims. Resource counters are backend estimates."}});
}
}

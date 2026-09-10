#include "Audio/Audio.h"
#include <miniaudio.h>
#include <cmath>
#include <stdexcept>
namespace engine {
namespace {void check(ma_result result){if(result!=MA_SUCCESS)throw std::runtime_error("Audio: "+std::string(ma_result_description(result)));}}
struct Audio::Impl {
    ma_engine engine{};ma_audio_buffer buffer{};ma_sound sound{};
    bool engineReady=false,bufferReady=false,soundReady=false,offline=false;
    uint64_t last=0,count=0;
    std::vector<float> pcm;
    ~Impl(){if(soundReady)ma_sound_uninit(&sound);if(bufferReady)ma_audio_buffer_uninit(&buffer);if(engineReady)ma_engine_uninit(&engine);}
};
Audio::Audio(bool offline):impl_(std::make_unique<Impl>()) {
    auto& p=*impl_;p.offline=offline;
    auto config=ma_engine_config_init();config.noDevice=offline;config.channels=2;config.sampleRate=48000;
    check(ma_engine_init(&config,&p.engine));p.engineReady=true;
    p.pcm.resize(9600);
    for(size_t i=0;i<p.pcm.size();++i){const double t=double(i)/48000;const double envelope=std::min(1.0,t/.01)*(1-double(i)/p.pcm.size());p.pcm[i]=float(.1*envelope*std::sin(2*3.141592653589793*660*t));}
    auto buffer=ma_audio_buffer_config_init(ma_format_f32,1,p.pcm.size(),p.pcm.data(),nullptr);buffer.sampleRate=48000;
    check(ma_audio_buffer_init(&buffer,&p.buffer));p.bufferReady=true;
    check(ma_sound_init_from_data_source(&p.engine,&p.buffer,MA_SOUND_FLAG_NO_SPATIALIZATION,nullptr,&p.sound));p.soundReady=true;
}
Audio::~Audio()=default;
bool Audio::cue(uint64_t sequence) {
    auto& p=*impl_;if(sequence==0||sequence<=p.last)return false;
    check(ma_sound_stop(&p.sound));check(ma_sound_seek_to_pcm_frame(&p.sound,0));check(ma_sound_start(&p.sound));
    p.last=sequence;++p.count;return true;
}
uint64_t Audio::delivered() const{return impl_->count;}
std::vector<float> Audio::render(uint32_t frames) {
    if(!impl_->offline||frames==0||frames>48000)throw std::invalid_argument("Invalid offline audio request");
    std::vector<float> result(size_t(frames)*2);check(ma_engine_read_pcm_frames(&impl_->engine,result.data(),frames,nullptr));return result;
}
}

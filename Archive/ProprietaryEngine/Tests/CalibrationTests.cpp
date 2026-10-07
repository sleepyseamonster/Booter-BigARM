#include "Game/CalibrationRuntime.h"
#include "Audio/Audio.h"
#include <cmath>
#include <iostream>
using namespace engine;
void require(bool c,const char* m){if(!c)throw std::runtime_error(m);}
int main()try {
    CalibrationRuntime runtime;Actions actions;float yaw=0,pitch=.3f;
    require(runtime.world().find("authored:calibration:proxy").has_value()&&runtime.world().find("authored:calibration:marker").has_value(),"Shared authored identities");
    require(runtime.canInteract(),"Initial marker within range and visible");
    actions.set(Control::E,1);runtime.advance(FixedClock::step,false,actions,yaw,pitch);
    require(runtime.targetActive(),"Interaction changes runtime state");
    auto cues=runtime.takeCues();require(cues.size()==1&&cues[0].sequence==1&&cues[0].source=="authored:calibration:marker","Exactly one identified cue");
    runtime.advance(FixedClock::step*3,false,actions,yaw,pitch);require(runtime.takeCues().empty(),"Held interaction does not duplicate");
    require(runtime.takeCues().empty(),"Draining does not replay cues");
    actions.set(Control::E,0);actions.set(Control::E,1);runtime.advance(FixedClock::step,false,actions,yaw,pitch);
    cues=runtime.takeCues();require(!runtime.targetActive()&&cues.size()==1&&cues[0].sequence==2,"Next edge toggles once");
    const auto ticks=runtime.clock().ticks();runtime.advance(1,true,actions,yaw,pitch);require(runtime.clock().ticks()==ticks,"Paused runtime holds time");
    const auto frame=runtime.present(yaw,pitch,5,1.f/60);require(std::isfinite(frame.eye[0])&&frame.palette==nullptr,"Headless runtime presentation without model");
    Audio audio(true);require(audio.cue(1)&&!audio.cue(1)&&!audio.cue(0)&&audio.delivered()==1,"Audio cue identity deduplicates");
    const auto samples=audio.render(12000);double energy=0;for(float v:samples){require(std::isfinite(v)&&std::abs(v)<=1,"Finite bounded PCM");energy+=v*v;}
    require(energy>1,"Real miniaudio graph emits cue samples");
    const auto tail=audio.render(12000);float peak=0;for(float v:tail)peak=std::max(peak,std::abs(v));require(peak<.0001f,"Finite cue ends");
    require(audio.cue(2)&&audio.delivered()==2,"Next cue restarts bounded voice");
    std::cout<<"PASS: shared calibration state, one-shot interaction/event drain, pause, camera and offline miniaudio cue; energy="<<energy<<'\n';return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

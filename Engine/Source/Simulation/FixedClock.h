#pragma once
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <limits>
#include <stdexcept>
namespace engine {
class FixedClock {
public:
    static constexpr double step=1.0/60.0;
    static constexpr unsigned maxCatchUp=4;
    template<class Tick> unsigned advance(double seconds,bool paused,Tick&& tick) {
        if(!std::isfinite(seconds) || seconds<0) throw std::invalid_argument("Invalid frame duration");
        if(paused) {remainder_=0;return 0;}
        // Admit at most four ticks plus the existing fractional tick. Account for
        // discarded time explicitly; pressure/travel never use hidden wall time.
        const double admitted=std::min(seconds,step*maxCatchUp);
        dropped_+=seconds-admitted;
        if(seconds>admitted) ++overloadedFrames_;
        remainder_+=admitted;
        unsigned count=0;
        while(count<maxCatchUp && remainder_+1e-12>=step) {
            if(ticks_==std::numeric_limits<uint64_t>::max()) throw std::overflow_error("Simulation tick exhausted");
            tick(step,ticks_+1);
            remainder_=std::max(0.0,remainder_-step);++ticks_;++count;
        }
        return count;
    }
    double alpha() const {return std::clamp(remainder_/step,0.0,1.0);}
    double seconds() const {return double(ticks_)*step;}
    double droppedSeconds() const {return dropped_;}
    uint64_t ticks() const {return ticks_;}
    uint64_t overloadedFrames() const {return overloadedFrames_;}
private:
    double remainder_=0,dropped_=0;
    uint64_t ticks_=0,overloadedFrames_=0;
};
}

#include "Core/WorldIdentity.h"
#include <cmath>
#include <limits>
#include <stdexcept>

namespace engine {
WorldPosition WorldPosition::normalized(double span) const {
    if (!std::isfinite(span) || span<=0) throw std::invalid_argument("Invalid region span");
    WorldPosition result=*this;
    for (double value:local) if (!std::isfinite(value)) throw std::invalid_argument("Nonfinite world position");
    auto axis=[&](int index,int64_t& address) {
        double offset=std::floor(local[index]/span);
        // Division can underflow to negative zero at a boundary; it still belongs to the prior region.
        if (local[index]<0 && offset==0) offset=-1;
        // Bound before integer conversion; 2^63 is exactly representable, INT64_MAX is not.
        if (!std::isfinite(offset) || offset < -0x1p63 || offset >= 0x1p63)
            throw std::overflow_error("Region offset overflow");
        const int64_t delta=static_cast<int64_t>(offset);
        if ((delta>0 && address>INT64_MAX-delta) || (delta<0 && address<INT64_MIN-delta))
            throw std::overflow_error("Region address overflow");
        address+=delta;
        result.local[index]=std::fmod(local[index],span);
        if (result.local[index]<0) result.local[index]+=span;
        // Very small negative values may round to span. Keep the prior region's last representable point.
        if (result.local[index]>=span) result.local[index]=std::nextafter(span,0.0);
    };
    axis(0,result.region.x); axis(2,result.region.z);
    return result;
}
std::array<float,3> WorldPosition::relativeTo(const WorldPosition& origin,double span,double maximum) const {
    if (!std::isfinite(maximum) || maximum<=0 || maximum>std::numeric_limits<float>::max())
        throw std::invalid_argument("Invalid local working radius");
    const auto a=normalized(span), b=origin.normalized(span);
    auto difference=[](int64_t x,int64_t y) {
        // Subtract unsigned magnitudes before conversion, preserving nearby large addresses.
        return x>=y ? double(uint64_t(x)-uint64_t(y)) : -double(uint64_t(y)-uint64_t(x));
    };
    const std::array<double,3> delta{difference(a.region.x,b.region.x)*span+a.local[0]-b.local[0],
        a.local[1]-b.local[1],difference(a.region.z,b.region.z)*span+a.local[2]-b.local[2]};
    std::array<float,3> result{};
    for (int i=0;i<3;++i) {
        if (!std::isfinite(delta[i]) || std::abs(delta[i])>maximum) throw std::out_of_range("Position outside local working radius");
        result[i]=static_cast<float>(delta[i]);
    }
    return result;
}
std::string GeneratedId::text() const {
    if (generator.empty() || generator.size()>64 || generator.find_first_not_of("abcdefghijklmnopqrstuvwxyz0123456789_-")!=std::string::npos)
        throw std::invalid_argument("Invalid generator namespace");
    if (!generatorVersion) throw std::invalid_argument("Generator version must be positive");
    return "generated:v1:"+generator+":"+std::to_string(seed)+":"+std::to_string(generatorVersion)+":"+
        std::to_string(region.x)+":"+std::to_string(region.z)+":"+std::to_string(member);
}
}

#pragma once
#include "World/Rocks/RockGenerator.h"
namespace engine {
// Native adaptation of the approved Unity workbench's primitive field. Input is
// physical authoring data; no Unity type or runtime asset is required.
class CalibratedRockVolume {
public:
    CalibratedRockVolume(const RockVolume&,float edgeDamage,uint32_t seedDelta);
    float evaluate(std::array<float,3>)const;
    std::array<float,3> minimum{},maximum{};
private:
    RockVolume volume_;
    std::array<std::array<float,3>,3> axes_{};
    std::array<std::array<float,4>,4> cuts_{};
    float round_=0,taperX_=0,taperZ_=0,wedgeSlope_=0,wedgeOffset_=0,topX_=0,topZ_=0,distanceScale_=0;
};
void relaxCalibratedRock(ModelData&,float amount,float cellSize);
}

#pragma once
#include "Core/WorldIdentity.h"
#include "Persistence/Document.h"
#include <vector>
namespace engine {
struct TraversalProfile {std::string id;float radius=.35f,height=1.8f,maxSlope=45,maxStep=.35f;bool operator==(const TraversalProfile&)const=default;};
struct TraversalSample {float clearance=0,headroom=0,slope=0,step=0;};
struct Exclusion {std::string id;WorldPosition center;float radius=1;bool operator==(const Exclusion&)const=default;};
struct ReservedRoute {std::string id;WorldPosition start,end;float halfWidth=1;uint8_t agents=3;bool operator==(const ReservedRoute&)const=default;};
struct PlacementConstraints {
    // Calibration dimensions only, not final Booter/BigARM art or traversal tuning.
    std::array<TraversalProfile,2> profiles{{{"player",.35f,1.8f,45,.35f},{"companion",1.1f,2.4f,25,.25f}}};
    std::vector<Exclusion> exclusions;
    std::vector<ReservedRoute> routes;
    bool operator==(const PlacementConstraints&)const=default;
};
bool traversable(const TraversalProfile&,const TraversalSample&);
void validateConstraints(const PlacementConstraints&);
bool placementAllowed(const PlacementConstraints&,WorldPosition center,float footprintRadius);
void saveConstraints(const std::filesystem::path&,const PlacementConstraints&);
PlacementConstraints loadConstraints(const std::filesystem::path&);
}

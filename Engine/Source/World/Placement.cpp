#include "World/Placement.h"
#include <algorithm>
#include <cmath>
#include <set>
namespace engine {
namespace {
void range(float value,float low,float high){if(!std::isfinite(value)||value<low||value>high)throw std::invalid_argument("Placement value outside supported range");}
void profile(const TraversalProfile& p){if(p.id.empty()||p.id.size()>128)throw std::invalid_argument("Invalid traversal profile ID");range(p.radius,.1f,4);range(p.height,.2f,8);range(p.maxSlope,0,80);range(p.maxStep,0,2);}
WorldPosition planar(WorldPosition p){p.local[1]=0;return p.normalized(256);}
Json position(const WorldPosition& p){const auto n=p.normalized(256);return {{"region",{n.region.x,n.region.z}},{"local",n.local}};}
WorldPosition position(const Json& p){if(p.size()!=2||!p.at("region").is_array()||p.at("region").size()!=2||!p.at("local").is_array()||p.at("local").size()!=3)throw std::runtime_error("Invalid placement position");for(const auto& x:p.at("region"))if(!x.is_number_integer()||x>INT64_MAX||x<INT64_MIN)throw std::runtime_error("Invalid region coordinate");return WorldPosition{{p.at("region")[0].get<int64_t>(),p.at("region")[1].get<int64_t>()},p.at("local").get<std::array<double,3>>()}.normalized(256);}
}
bool traversable(const TraversalProfile& p,const TraversalSample& s){profile(p);range(s.clearance,0,10000);range(s.headroom,0,10000);range(s.slope,0,90);range(s.step,0,10000);return s.clearance>=p.radius&&s.headroom>=p.height&&s.slope<=p.maxSlope&&s.step<=p.maxStep;}
void validateConstraints(const PlacementConstraints& c) {
    for(const auto& p:c.profiles)profile(p);if(c.profiles[0].id==c.profiles[1].id)throw std::invalid_argument("Duplicate agent profile ID");
    if(c.exclusions.size()>256||c.routes.size()>256)throw std::invalid_argument("Constraint capacity exceeded");
    std::set<std::string> ids;
    auto identity=[&](const std::string& id){if(id.empty()||id.size()>128||!ids.insert(id).second)throw std::invalid_argument("Invalid/duplicate constraint ID");};
    for(const auto& e:c.exclusions){identity(e.id);e.center.normalized(256);range(e.radius,.1f,64);}
    for(const auto& r:c.routes){identity(r.id);range(r.halfWidth,0,64);if(!r.agents||r.agents>3)throw std::invalid_argument("Invalid route agent mask");const auto d=planar(r.end).relativeTo(planar(r.start),256,512);const auto length=std::hypot(d[0],d[2]);if(length<.01f||length>512)throw std::invalid_argument("Route segment must be 0.01 to 512 metres");}
}
bool placementAllowed(const PlacementConstraints& c,WorldPosition center,float footprint) {
    validateConstraints(c);range(footprint,0,64);center=planar(center);
    for(const auto& e:c.exclusions) {
        try{const auto d=planar(e.center).relativeTo(center,256,2048);if(std::hypot(d[0],d[2])<=e.radius+footprint)return false;}
        catch(const std::out_of_range&){} // Farther than every permitted footprint/exclusion radius.
    }
    for(const auto& r:c.routes) {
        try {
            const auto a=planar(r.start).relativeTo(center,256,2048),delta=planar(r.end).relativeTo(planar(r.start),256,512);
            const float t=std::clamp(-(a[0]*delta[0]+a[2]*delta[2])/(delta[0]*delta[0]+delta[2]*delta[2]),0.f,1.f);
            float agent=0;for(size_t i=0;i<2;++i)if(r.agents&(1<<i))agent=std::max(agent,c.profiles[i].radius);
            if(std::hypot(a[0]+t*delta[0],a[2]+t*delta[2])<=r.halfWidth+agent+footprint)return false;
        }catch(const std::out_of_range&){} // A <=512 m segment cannot reach from outside this 2048 m local window.
    }
    return true;
}
void saveConstraints(const std::filesystem::path& file,const PlacementConstraints& c) {
    validateConstraints(c);Json profiles=Json::array(),exclusions=Json::array(),routes=Json::array();
    for(const auto& p:c.profiles)profiles.push_back({{"id",p.id},{"radius",p.radius},{"height",p.height},{"max_slope",p.maxSlope},{"max_step",p.maxStep}});
    for(const auto& e:c.exclusions)exclusions.push_back({{"id",e.id},{"center",position(e.center)},{"radius",e.radius}});
    for(const auto& r:c.routes)routes.push_back({{"id",r.id},{"start",position(r.start)},{"end",position(r.end)},{"half_width",r.halfWidth},{"agents",r.agents}});
    writeDocument(file,"engine.placement-constraints",{{"profiles",profiles},{"exclusions",exclusions},{"routes",routes}});
}
PlacementConstraints loadConstraints(const std::filesystem::path& file) {
    const auto p=readDocument(file,"engine.placement-constraints");if(p.size()!=3||!p.at("profiles").is_array()||p.at("profiles").size()!=2||!p.at("exclusions").is_array()||p.at("exclusions").size()>256||!p.at("routes").is_array()||p.at("routes").size()>256)throw std::runtime_error("Unsupported constraint document");
    PlacementConstraints c;size_t i=0;for(const auto& a:p.at("profiles")){if(a.size()!=5)throw std::runtime_error("Invalid profile fields");c.profiles[i++]={a.at("id").get<std::string>(),a.at("radius").get<float>(),a.at("height").get<float>(),a.at("max_slope").get<float>(),a.at("max_step").get<float>()};}
    for(const auto& e:p.at("exclusions")){if(e.size()!=3)throw std::runtime_error("Invalid exclusion fields");c.exclusions.push_back({e.at("id").get<std::string>(),position(e.at("center")),e.at("radius").get<float>()});}
    for(const auto& r:p.at("routes")){if(r.size()!=5||!r.at("agents").is_number_unsigned()||r.at("agents")<1||r.at("agents")>3)throw std::runtime_error("Invalid route fields");c.routes.push_back({r.at("id").get<std::string>(),position(r.at("start")),position(r.at("end")),r.at("half_width").get<float>(),r.at("agents").get<uint8_t>()});}
    validateConstraints(c);return c;
}
}

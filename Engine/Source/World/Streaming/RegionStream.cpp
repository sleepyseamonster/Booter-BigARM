#include "World/Streaming/RegionStream.h"
#include <algorithm>
#include <cmath>
#include <set>
namespace engine {
namespace {
RegionKey key(Region r){return {r.x,r.z};}
std::string owner(Region r){return "region:"+std::to_string(r.x)+":"+std::to_string(r.z);}
uint64_t difference(int64_t a,int64_t b){return a>=b?uint64_t(a)-uint64_t(b):uint64_t(b)-uint64_t(a);}
uint64_t distance(Region a,Region b){return std::max(difference(a.x,b.x),difference(a.z,b.z));}
RegionContent generate(const TerrainRecipe& terrain,const RockRecipe& recipe,const PlacementConstraints& constraints,Region region,const std::array<RockAsset,4>& rocks,const WorldDeltas& deltas,const JobCancellation& token){
    RegionContent c;c.terrain=generateTerrain(terrain,region,recipe,constraints,[&]{return token.cancelled();});
    std::erase_if(c.terrain.rocks,[&](const auto& p){return deltas.removed(region,p.id.member);});
    size_t count=c.terrain.collision.size();for(const auto& p:c.terrain.rocks)count+=rocks[p.id.member%4].collision.size();c.collision.reserve(count);c.collision.insert(c.collision.end(),c.terrain.collision.begin(),c.terrain.collision.end());
    for(const auto& p:c.terrain.rocks){if(token.cancelled())throw std::runtime_error("Region generation cancelled");const float angle=p.yaw*.01745329252f,s=std::sin(angle),co=std::cos(angle);
        for(const auto& v:rocks[p.id.member%4].collision)c.collision.push_back({float(p.position.local[0])+co*v[0]+s*v[2],float(p.position.local[1])+v[1],float(p.position.local[2])-s*v[0]+co*v[2]});
    }return c;
}
}
RegionStream::RegionStream(TerrainRecipe terrain,RockRecipe recipe,PlacementConstraints constraints,Attach attach,Detach detach,WorldDeltas deltas)
    :deltas_(std::move(deltas)),terrain_(terrain),recipe_(recipe),constraints_(std::move(constraints)),rocks_(std::make_shared<std::array<RockAsset,4>>()),attach_(std::move(attach)),detach_(std::move(detach)),jobs_([](const auto& c){return c.bytes();}){
    validateDeltas(deltas_);validateTerrainRecipe(terrain_);validateRecipe(recipe_);validateConstraints(constraints_);if(!attach_||!detach_)throw std::invalid_argument("Region adapters required");
    for(size_t i=0;i<4;++i)(*rocks_)[i]=buildRockAsset(recipe_,{terrain_.seed,recipe_.version,{},i,"rock"});
    // Maximum 256 placements, plus terrain, with temporary construction headroom.
    reservation_=2*(512*1024+256*(*rocks_)[0].collision.size()*sizeof(std::array<float,3>));
    if(6144+256*(*rocks_)[0].collision.size()>300000)throw std::invalid_argument("Streaming rock detail exceeds collider triangle capacity");
    if(reservation_>16*1024*1024)throw std::invalid_argument("Streaming rock detail exceeds first-pass staging profile");
}
RegionStream::~RegionStream(){jobs_.shutdown();for(const auto& [_,slot]:slots_)if(slot.content)detach_(slot.region);}
void RegionStream::update(const std::vector<StreamAnchor>& anchors){
    if(anchors.size()>2)throw std::invalid_argument("First-pass streaming supports two anchors");
    std::vector<std::pair<Region,unsigned>> centers;
    for(const auto& a:anchors){const auto r=a.position.normalized(256).region;if(r.x<INT64_MIN+4||r.x>INT64_MAX-4||r.z<INT64_MIN+4||r.z>INT64_MAX-4)throw std::out_of_range("Streaming anchor outside coordinate halo");centers.push_back({r,a.priority});}
    struct Wanted {Region region;unsigned priority;uint64_t distance;};std::map<RegionKey,Wanted> wanted;
    for(const auto& [center,priority]:centers)for(int z=-1;z<=1;++z)for(int x=-1;x<=1;++x){Region r{center.x+x,center.z+z};Wanted next{r,priority,uint64_t(std::max(std::abs(x),std::abs(z)))};auto [it,inserted]=wanted.emplace(key(r),next);if(!inserted&&std::pair(next.priority,next.distance)<std::pair(it->second.priority,it->second.distance))it->second=next;}
    std::vector<Wanted> ordered;for(const auto& [_,w]:wanted)ordered.push_back(w);std::sort(ordered.begin(),ordered.end(),[](const auto& a,const auto& b){return std::tuple(a.priority,a.distance,a.region.x,a.region.z)<std::tuple(b.priority,b.distance,b.region.x,b.region.z);});
    auto retire=[&](auto it){jobs_.cancel(owner(it->second.region));if(it->second.content){detach_(it->second.region);residentBytes_-=it->second.content->bytes();}++retired_;return slots_.erase(it);};
    for(auto it=slots_.begin();it!=slots_.end();){it->second.desired=wanted.contains(it->first);bool keep=it->second.desired;for(const auto& c:centers)keep=keep||distance(c.first,it->second.region)<=2;if(!keep)it=retire(it);else ++it;}
    for(const auto& w:ordered){const auto existing=slots_.find(key(w.region));if(existing!=slots_.end()&&existing->second.ticket)continue;
        if(existing==slots_.end()&&slots_.size()>=25){auto old=std::find_if(slots_.begin(),slots_.end(),[](const auto& e){return !e.second.desired;});if(old==slots_.end())break;retire(old);}
        const auto region=w.region;const auto terrain=terrain_;const auto recipe=recipe_;const auto constraints=constraints_;const auto rocks=rocks_;const auto deltas=deltas_;
        if(epoch_==UINT64_MAX)throw std::overflow_error("Region epoch exhausted");
        auto ticket=jobs_.submit(owner(region),++epoch_,reservation_,[=](const auto& token){return generate(terrain,recipe,constraints,region,*rocks,deltas,token);});
        if(ticket){if(existing!=slots_.end())existing->second.ticket=*ticket;else slots_.emplace(key(region),RegionSlot{region,*ticket,true,{},{},{}});}
    }
    // One adoption per displayed frame bounds expensive collider creation as well as uploads.
    UploadAdmission budget{512*1024,16*1024*1024};auto completed=jobs_.takeReady(budget);if(!completed)return;
    auto it=std::find_if(slots_.begin(),slots_.end(),[&](const auto& e){return e.second.ticket==completed->ticket;});if(it==slots_.end())return;
    auto& slot=it->second;if(!completed->error.empty()){slot.error=completed->error;return;}
    auto content=std::make_shared<RegionContent>(std::move(*completed->value));
    const size_t previousBytes=slot.content?slot.content->bytes():0;
    if(content->bytes()>64*1024*1024-(residentBytes_-previousBytes)){slot.error="Region CPU residency budget exhausted";return;}
    try{slot.ready=attach_(*content);slot.content=content;residentBytes_=residentBytes_-previousBytes+content->bytes();}
    catch(const std::exception& e){if(!slot.content){detach_(slot.region);slot.ready={};}slot.error=e.what();}
}
bool RegionStream::removeRock(const GeneratedId& id){
    if(id.seed!=terrain_.seed||id.generatorVersion!=recipe_.version||id.generator!="rock"||id.member>255)throw std::invalid_argument("Rock identity does not belong to this world");
    if(deltas_.removed(id.region,id.member))return false;
    auto it=slots_.find(key(id.region));if(it==slots_.end()||!it->second.content)return false;
    const auto& rocks=it->second.content->terrain.rocks;if(std::none_of(rocks.begin(),rocks.end(),[&](const auto& p){return p.id==id;}))return false;
    auto next=deltas_;next.removedRocks[key(id.region)].insert(id.member);validateDeltas(next);deltas_=std::move(next);
    jobs_.cancel(owner(id.region));it->second.ticket=0;it->second.error.clear();return true;
}
bool RegionStream::collisionReady(WorldPosition from,WorldPosition to,float radius)const{
    if(!std::isfinite(radius)||radius<0||radius>4)return false;
    const auto a=from.normalized(256),b=to.normalized(256);if(distance(a.region,b.region)>1)return false;
    // A fixed-tick move is short; include the capsule and contact/step margin on both endpoints.
    for(auto p:{a,b})for(float z:{-radius,radius})for(float x:{-radius,radius}){auto q=p;q.local[0]+=x;q.local[2]+=z;auto it=slots_.find(key(q.normalized(256).region));if(it==slots_.end()||!it->second.ready.collision)return false;}
    return true;
}
}

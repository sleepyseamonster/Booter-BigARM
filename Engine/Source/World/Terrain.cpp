#include "World/Terrain.h"
#include <algorithm>
#include <cmath>
namespace engine {
namespace {
uint64_t mix(uint64_t x){x+=0x9e3779b97f4a7c15ULL;x=(x^(x>>30))*0xbf58476d1ce4e5b9ULL;x=(x^(x>>27))*0x94d049bb133111ebULL;return x^(x>>31);}
uint64_t hash(uint64_t seed,Region r,uint64_t member=0){return mix(seed^mix(uint64_t(r.x))^mix(uint64_t(r.z)+0x759ac33ULL)^mix(member));}
double smooth(double t){t=std::clamp(t,0.,1.);return t*t*(3-2*t);}
void supported(Region r){constexpr int64_t limit=INT64_MAX-2;if(r.x < -limit||r.x>limit||r.z < -limit||r.z>limit)throw std::out_of_range("Terrain region needs a one-region sampling halo");}
double roadDistance(double value){return std::min(value,256-value);}
float height(const TerrainRecipe& recipe,WorldPosition position){
    const auto p=position.normalized(256);supported(p.region);
    const double x=p.local[0]/256,z=p.local[2]/256;
    auto corner=[&](int dx,int dz){return (double(hash(recipe.seed,{p.region.x+dx,p.region.z+dz})&65535)/65535.*2-1)*recipe.amplitudeMm*.001;};
    const auto a=std::lerp(corner(0,0),corner(1,0),smooth(x));
    const auto b=std::lerp(corner(0,1),corner(1,1),smooth(x));
    // Shared flat grid corridors and smooth shoulders meet identically across region boundaries.
    const double mask=smooth((roadDistance(p.local[0])-8)/24)*smooth((roadDistance(p.local[2])-8)/24);
    return float(std::lerp(a,b,smooth(z))*mask);
}
}
void validateTerrainRecipe(const TerrainRecipe& r){if(r.version!=1||r.amplitudeMm>12000)throw std::invalid_argument("Unsupported terrain recipe version or amplitude");}
void saveTerrainRecipe(const std::filesystem::path& file,const TerrainRecipe& r){validateTerrainRecipe(r);writeDocument(file,"engine.terrain-recipe",{{"seed",r.seed},{"version",r.version},{"amplitude_mm",r.amplitudeMm}});}
TerrainRecipe loadTerrainRecipe(const std::filesystem::path& file){const auto p=readDocument(file,"engine.terrain-recipe");if(p.size()!=3)throw std::runtime_error("Invalid terrain recipe fields");for(const auto* key:{"seed","version","amplitude_mm"})if(!p.at(key).is_number_unsigned())throw std::runtime_error("Terrain recipe requires unsigned integers");if(p.at("version")!=1||p.at("amplitude_mm")>12000)throw std::runtime_error("Unsupported terrain recipe");TerrainRecipe r{p.at("seed").get<uint64_t>(),1,p.at("amplitude_mm").get<uint32_t>()};validateTerrainRecipe(r);return r;}
TerrainSample terrainSample(const TerrainRecipe& r,WorldPosition p){
    validateTerrainRecipe(r);p=p.normalized(256);
    const double x=std::floor(p.local[0]/8)*8,z=std::floor(p.local[2]/8)*8;
    const float u=float((p.local[0]-x)/8),v=float((p.local[2]-z)/8);
    auto h=[&](double dx,double dz){return height(r,{p.region,{x+dx,0,z+dz}});};
    const float a=h(0,0),b=h(8,0),c=h(0,8),d=h(8,8);
    const float dx=(u+v<=1?b-a:d-c)/8,dz=(u+v<=1?c-a:d-b)/8;
    const float value=u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
    const float length=std::sqrt(dx*dx+1+dz*dz);return {value,{-dx/length,1/length,-dz/length}};
}
size_t TerrainPatch::bytes()const{
    size_t result=sizeof(TerrainPatch)+mesh.vertices.capacity()*sizeof(SkinVertex)+mesh.indices.capacity()*sizeof(uint32_t)+collision.capacity()*sizeof(std::array<float,3>)+rocks.capacity()*sizeof(RockPlacement)+landmarks.capacity()*sizeof(LandmarkAnchor)+routes.capacity()*sizeof(ReservedRoute);
    for(const auto& r:rocks)result+=r.id.generator.capacity();for(const auto& a:landmarks)result+=a.id.capacity();for(const auto& r:routes)result+=r.id.capacity();return result;
}
TerrainPatch generateTerrain(const TerrainRecipe& recipe,Region region,const RockRecipe& rock,const PlacementConstraints& constraints,const std::function<bool()>& cancelled){
    validateTerrainRecipe(recipe);validateRecipe(rock);validateConstraints(constraints);supported(region);
    TerrainPatch patch;patch.region=region;patch.id={recipe.seed,recipe.version,region,0,"terrain"};
    auto check=[&]{if(cancelled())throw std::runtime_error("Terrain generation cancelled");};
    auto& mesh=patch.mesh;mesh.nodes.push_back({});mesh.joints={0};mesh.inverseBind={{{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}}};mesh.metallic=0;mesh.vertices.reserve(33*33);mesh.indices.reserve(32*32*6);
    for(int z=0;z<=32;++z){check();for(int x=0;x<=32;++x){const WorldPosition point{region,{double(x*8),0,double(z*8)}};auto sample=terrainSample(recipe,point);auto left=point,right=point;left.local[0]-=.25;right.local[0]+=.25;const float dx=(height(recipe,right)-height(recipe,left))/.5f;left=point;right=point;left.local[2]-=.25;right.local[2]+=.25;const float dz=(height(recipe,right)-height(recipe,left))/.5f;const float length=std::sqrt(dx*dx+1+dz*dz);sample.normal={-dx/length,1/length,-dz/length};SkinVertex v;v.position={float(x*8),sample.height,float(z*8)};v.normal=sample.normal;v.uv={float(x)/32,float(z)/32};const auto n=v.normal;const float tangentLength=std::hypot(n[1],n[0]);v.tangent={n[1]/tangentLength,-n[0]/tangentLength,0,1};mesh.vertices.push_back(v);}}
    for(uint32_t z=0;z<32;++z)for(uint32_t x=0;x<32;++x){const uint32_t a=z*33+x,b=a+1,c=a+33,d=c+1;mesh.indices.insert(mesh.indices.end(),{a,c,b,b,c,d});}
    patch.collision.reserve(mesh.indices.size());for(auto index:mesh.indices)patch.collision.push_back(mesh.vertices[index].position);
    // Each patch owns its south/west edge; neighbors provide north/east continuation.
    patch.routes.push_back({patch.id.text()+":west",{region,{0,0,0}},{region,{0,0,256}},6,3});
    patch.routes.push_back({patch.id.text()+":south",{region,{0,0,0}},{region,{256,0,0}},6,3});
    const float footprint=float(std::max(rock.radiiMm[0],rock.radiiMm[2]))*.001f*1.4f;
    if(footprint>4)throw std::invalid_argument("Terrain placement currently supports rock footprints up to four metres");
    patch.rocks.reserve(256);
    // Stable cell slots do not renumber when a neighbor candidate is rejected.
    for(uint64_t slot=0;slot<256;++slot){check();const auto random=hash(recipe.seed,region,slot);if((random&3)==0)continue;
        const double x=(slot%16)*16+8+(int((random>>8)&1023)-512)/128.;
        const double z=(slot/16)*16+8+(int((random>>20)&1023)-512)/128.;
        if(roadDistance(x)<=8+footprint||roadDistance(z)<=8+footprint)continue;
        WorldPosition p{region,{x,0,z}};if(!placementAllowed(constraints,p,footprint))continue;
        const auto sample=terrainSample(recipe,p);const float slope=std::acos(std::clamp(sample.normal[1],-1.f,1.f))*57.2957795f;
        if(slope>constraints.profiles[0].maxSlope)continue;
        p.local[1]=sample.height;patch.rocks.push_back({{recipe.seed,rock.version,region,slot,"rock"},p,float((random>>32)%360),footprint});
    }
    for(const auto& a:constraints.exclusions){const auto p=a.center.normalized(256);if(p.region==region)patch.landmarks.push_back({a.id,p,a.radius});}
    validateModel(mesh);return patch;
}
}

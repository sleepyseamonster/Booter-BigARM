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
// Integer lattice addresses avoid converting large regions into imprecise global floats.
double field(uint64_t seed,WorldPosition p,int cells){
    p=p.normalized(256);const double x=p.local[0]*cells/256,z=p.local[2]*cells/256;
    const auto ix=uint64_t(std::floor(x)),iz=uint64_t(std::floor(z));
    auto value=[&](uint64_t dx,uint64_t dz){const auto a=uint64_t(p.region.x)*uint64_t(cells)+ix+dx,b=uint64_t(p.region.z)*uint64_t(cells)+iz+dz;
        return double(mix(seed^mix(a)^mix(b+0x759ac33ULL))&65535)/65535.*2-1;};
    return std::lerp(std::lerp(value(0,0),value(1,0),smooth(x-std::floor(x))),std::lerp(value(0,1),value(1,1),smooth(x-std::floor(x))),smooth(z-std::floor(z)));
}
float height(const TerrainRecipe& recipe,WorldPosition position){
    const auto p=position.normalized(256);supported(p.region);
    if(recipe.version>=2){
        const double broad=field(recipe.seed,p,1),ridges=field(recipe.seed+17,p,2),detail=field(recipe.seed+73,p,8);
        // Broad relief, softened shelves, and small broken-ground variation.
        return float(recipe.amplitudeMm*.001*(.65*broad+.26*ridges+.09*detail));
    }
    const double x=p.local[0]/256,z=p.local[2]/256;
    auto corner=[&](int dx,int dz){return (double(hash(recipe.seed,{p.region.x+dx,p.region.z+dz})&65535)/65535.*2-1)*recipe.amplitudeMm*.001;};
    const auto a=std::lerp(corner(0,0),corner(1,0),smooth(x));
    const auto b=std::lerp(corner(0,1),corner(1,1),smooth(x));
    // Shared flat grid corridors and smooth shoulders meet identically across region boundaries.
    const double mask=smooth((roadDistance(p.local[0])-8)/24)*smooth((roadDistance(p.local[2])-8)/24);
    return float(std::lerp(a,b,smooth(z))*mask);
}
}
void validateTerrainRecipe(const TerrainRecipe& r){if((r.version<1||r.version>3)||r.amplitudeMm>(r.version==1?12000u:24000u))throw std::invalid_argument("Unsupported terrain recipe version or amplitude");}
void saveTerrainRecipe(const std::filesystem::path& file,const TerrainRecipe& r){validateTerrainRecipe(r);writeDocument(file,"engine.terrain-recipe",{{"seed",r.seed},{"version",r.version},{"amplitude_mm",r.amplitudeMm}});}
TerrainRecipe loadTerrainRecipe(const std::filesystem::path& file){
    const auto p=readDocument(file,"engine.terrain-recipe");if(p.size()!=3)throw std::runtime_error("Invalid terrain recipe fields");
    for(const auto* key:{"seed","version","amplitude_mm"})if(!p.at(key).is_number_unsigned())throw std::runtime_error("Terrain recipe requires unsigned integers");
    if(p.at("version")>3||p.at("amplitude_mm")>24000)throw std::runtime_error("Unsupported terrain recipe");
    TerrainRecipe r{p.at("seed").get<uint64_t>(),p.at("version").get<uint32_t>(),p.at("amplitude_mm").get<uint32_t>()};validateTerrainRecipe(r);return r;
}
uint32_t terrainCells(const TerrainRecipe& r){validateTerrainRecipe(r);return r.version>=2?64:32;}
uint32_t terrainRockSlots(const TerrainRecipe& r){validateTerrainRecipe(r);return r.version==3?8:(r.version==2?32:256);}
TerrainSample terrainSample(const TerrainRecipe& r,WorldPosition p){
    validateTerrainRecipe(r);p=p.normalized(256);
    const double step=256./terrainCells(r);const double x=std::floor(p.local[0]/step)*step,z=std::floor(p.local[2]/step)*step;
    const float u=float((p.local[0]-x)/step),v=float((p.local[2]-z)/step);
    auto h=[&](double dx,double dz){return height(r,{p.region,{x+dx,0,z+dz}});};
    const float a=h(0,0),b=h(step,0),c=h(0,step),d=h(step,step);
    const float dx=float((u+v<=1?b-a:d-c)/step),dz=float((u+v<=1?c-a:d-b)/step);
    const float value=u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
    const float length=std::sqrt(dx*dx+1+dz*dz);return {value,{-dx/length,1/length,-dz/length}};
}
size_t TerrainPatch::bytes()const{
    size_t result=sizeof(TerrainPatch)+mesh.vertices.capacity()*sizeof(SkinVertex)+mesh.indices.capacity()*sizeof(uint32_t)+collision.capacity()*sizeof(std::array<float,3>)+rocks.capacity()*sizeof(RockPlacement)+landmarks.capacity()*sizeof(LandmarkAnchor)+routes.capacity()*sizeof(ReservedRoute);
    for(const auto& r:rocks){result+=r.id.generator.capacity()+r.members.capacity()*sizeof(RockFormationMember);for(const auto& m:r.members)result+=m.id.generator.capacity();}for(const auto& a:landmarks)result+=a.id.capacity();for(const auto& r:routes)result+=r.id.capacity();return result;
}
TerrainPatch generateTerrain(const TerrainRecipe& recipe,Region region,const RockRecipe& rock,const PlacementConstraints& constraints,const std::function<bool()>& cancelled){
    validateTerrainRecipe(recipe);validateRecipe(rock);validateConstraints(constraints);supported(region);
    if(rock.version==5)throw std::invalid_argument("V5 authored rocks currently require the rock workbench; streaming uses v1-v4 recipes");
    if(rock.version==4&&rock.formation&&recipe.version!=3)throw std::invalid_argument("Streamed formation groups require terrain version 3");
    TerrainPatch patch;patch.region=region;patch.id={recipe.seed,recipe.version,region,0,"terrain"};
    auto check=[&]{if(cancelled())throw std::runtime_error("Terrain generation cancelled");};
    auto& mesh=patch.mesh;mesh.nodes.push_back({});mesh.joints={0};mesh.inverseBind={{{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}}};mesh.metallic=0;mesh.roughness=recipe.version>=2?.92f:1.f;
    const uint32_t cells=terrainCells(recipe),row=cells+1;const float step=256.f/cells;
    mesh.vertices.reserve(row*row);mesh.indices.reserve(cells*cells*6);
    for(uint32_t z=0;z<=cells;++z){check();for(uint32_t x=0;x<=cells;++x){
        const WorldPosition point{region,{double(x*step),0,double(z*step)}};auto sample=terrainSample(recipe,point);
        auto left=point,right=point;left.local[0]-=.25;right.local[0]+=.25;const float dx=(height(recipe,right)-height(recipe,left))/.5f;
        left=point;right=point;left.local[2]-=.25;right.local[2]+=.25;const float dz=(height(recipe,right)-height(recipe,left))/.5f;
        const float length=std::sqrt(dx*dx+1+dz*dz);sample.normal={-dx/length,1/length,-dz/length};
        SkinVertex v;v.position={x*step,sample.height,z*step};v.normal=sample.normal;v.uv={float(x)/cells,float(z)/cells};
        const auto n=v.normal;const float tangentLength=std::hypot(n[1],n[0]);v.tangent={n[1]/tangentLength,-n[0]/tangentLength,0,1};mesh.vertices.push_back(v);
    }}
    for(uint32_t z=0;z<cells;++z)for(uint32_t x=0;x<cells;++x){const uint32_t a=z*row+x,b=a+1,c=a+row,d=c+1;mesh.indices.insert(mesh.indices.end(),{a,c,b,b,c,d});}
    patch.collision.reserve(mesh.indices.size());for(auto index:mesh.indices)patch.collision.push_back(mesh.vertices[index].position);
    // Each patch owns its south/west edge; neighbors provide north/east continuation.
    if(recipe.version==1){patch.routes.push_back({patch.id.text()+":west",{region,{0,0,0}},{region,{0,0,256}},6,3});
    patch.routes.push_back({patch.id.text()+":south",{region,{0,0,0}},{region,{256,0,0}},6,3});
    }else patch.routes=constraints.routes;
    if(rock.version==3&&rock.formation)throw std::invalid_argument("Formation recipes are authoring assets; live terrain placement requires a single rock");
    const float footprint=rock.version>=3?std::hypot(float(rock.radiiMm[0]),float(rock.radiiMm[2]))*.001f:float(std::max(rock.radiiMm[0],rock.radiiMm[2]))*.001f*1.4f;
    if(footprint>4)throw std::invalid_argument("Terrain placement currently supports rock footprints up to four metres");
    patch.rocks.reserve(256);
    // Stable cell slots do not renumber when a neighbor candidate is rejected.
    for(uint64_t slot=0;slot<terrainRockSlots(recipe);++slot){check();const auto random=hash(recipe.seed,region,slot);if((random&3)==0)continue;
        double x=recipe.version==1?(slot%16)*16+8+(int((random>>8)&1023)-512)/128.:(slot%8)*32+16+(int((random>>8)&1023)-512)/64.;
        double z=recipe.version==1?(slot/16)*16+8+(int((random>>20)&1023)-512)/128.:(slot/8)*64+32+(int((random>>20)&1023)-512)/32.;
        if(recipe.version==3){x=(slot%4)*64+32+(int((random>>8)&1023)-512)/64.;z=(slot/4)*128+32+(int((random>>20)&1023)-512)/48.;}
        std::vector<RockFormationMember> members;float groupFootprint=footprint;
        const GeneratedId groupId{recipe.seed,rock.version,region,slot,"rock"};
        if(rock.version==4&&rock.formation){
            members=planRockFormation(rock,groupId,[](float,float){return 0.f;});groupFootprint=0;
            for(auto& m:members){const float c=std::cos(float((random>>32)%360)*.01745329252f),s=std::sin(float((random>>32)%360)*.01745329252f),mx=m.offset[0],mz=m.offset[2];m.offset[0]=c*mx+s*mz;m.offset[2]=-s*mx+c*mz;m.yaw+=float((random>>32)%360)*.01745329252f;
                groupFootprint=std::max(groupFootprint,std::hypot(m.offset[0],m.offset[2])+footprint*std::max(m.axes[0],m.axes[2])*m.scale);}
            if(groupFootprint>32)throw std::invalid_argument("Formation footprint exceeds the streaming profile");
        }
        if(recipe.version==1&&(roadDistance(x)<=8+footprint||roadDistance(z)<=8+footprint))continue;
        WorldPosition p{region,{x,0,z}};if(!placementAllowed(constraints,p,groupFootprint))continue;
        const auto sample=terrainSample(recipe,p);const float slope=std::acos(std::clamp(sample.normal[1],-1.f,1.f))*57.2957795f;
        if(slope>constraints.profiles[0].maxSlope)continue;
        p.local[1]=sample.height;patch.rocks.push_back({{recipe.seed,rock.version,region,slot,"rock"},p,float((random>>32)%360),groupFootprint,std::move(members)});
    }
    for(const auto& a:constraints.exclusions){const auto p=a.center.normalized(256);if(p.region==region)patch.landmarks.push_back({a.id,p,a.radius});}
    validateModel(mesh);return patch;
}
}

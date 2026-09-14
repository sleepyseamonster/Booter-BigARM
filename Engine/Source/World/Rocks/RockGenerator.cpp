#include "World/Rocks/RockGenerator.h"
#include "Persistence/Document.h"
#include <algorithm>
#include <bit>
#include <cmath>
#include <numeric>
#include <map>
namespace engine {
namespace {
uint64_t mix(uint64_t v){v+=0x9e3779b97f4a7c15ULL;v=(v^(v>>30))*0xbf58476d1ce4e5b9ULL;v=(v^(v>>27))*0x94d049bb133111ebULL;return v^(v>>31);}
using V=std::array<float,3>;
V subtract(V a,V b){return {a[0]-b[0],a[1]-b[1],a[2]-b[2]};}
V cross(V a,V b){return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
float dot(V a,V b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
V unit(V v){const float len=std::sqrt(dot(v,v));if(len<1e-8f)throw std::runtime_error("Degenerate rock triangle");for(auto& x:v)x/=len;return v;}
uint32_t integer(const Json& p,const char* key,uint32_t maximum){const auto& n=p.at(key);if(!n.is_number_unsigned()||n>maximum)throw std::runtime_error("Invalid integer rock parameter");return n.get<uint32_t>();}
}
void validateRecipe(const RockRecipe& r) {
    if((r.version<1||r.version>7)||r.subdivisions>4||r.distortionPermille>250||r.bandPermille>150||r.bands<1||r.bands>32)throw std::invalid_argument("Unsupported rock recipe version or parameters");
    if(r.version>=3){
        if(r.massCount<1||r.massCount>12||r.compaction>1000||r.asymmetry>1000||r.fractures>1000||r.edgeDamage>1000||r.formation>(r.version>=4?5u:3u)||r.members<1||r.members>(r.version>=4?16u:8u)||r.spacingMm<500||r.spacingMm>12000||r.volumes.size()>24)throw std::invalid_argument("Invalid fused rock controls");
        for(auto value:{r.material.grit,r.material.shale,r.material.cracks,r.material.dust,r.material.variation,r.material.worn})if(value>1000)throw std::invalid_argument("Invalid rock material control");
        std::vector<uint32_t> ids;bool additive=r.volumes.empty();
        for(const auto& v:r.volumes){
            if(std::find(ids.begin(),ids.end(),v.id)!=ids.end())throw std::invalid_argument("Duplicate rock volume identity");ids.push_back(v.id);additive|=!v.subtractive;
            if(!std::isfinite(v.yaw)||std::abs(v.yaw)>6.284f)throw std::invalid_argument("Invalid volume rotation");
            for(size_t i=0;i<3;++i)if(!std::isfinite(v.center[i])||std::abs(v.center[i])>(r.version==7?8:4)||!std::isfinite(v.halfSize[i])||v.halfSize[i]<(r.version==7?.001f:.03f)||v.halfSize[i]>3)throw std::invalid_argument("Invalid source volume bounds");
        }
        if(!additive)throw std::invalid_argument("A rock needs an additive source volume");
    }
    if(r.profile>(r.version==7?8u:4u)||r.memberEdits.size()>16)throw std::invalid_argument("Invalid rock profile or member edits");
    if(r.version<5&&(r.profile||!r.memberEdits.empty()))throw std::invalid_argument("Authored profiles and members require generator v5");
    std::vector<uint32_t> slots;
    for(const auto& e:r.memberEdits){
        if(e.slot>=r.members||e.variant>3||std::find(slots.begin(),slots.end(),e.slot)!=slots.end())throw std::invalid_argument("Invalid or duplicate formation member slot");
        slots.push_back(e.slot);
        if(!std::isfinite(e.yaw)||std::abs(e.yaw)>6.284f)throw std::invalid_argument("Invalid member rotation");
        for(size_t a=0;a<3;++a)if(!std::isfinite(e.translation[a])||std::abs(e.translation[a])>20||!std::isfinite(e.axes[a])||e.axes[a]<.1f||e.axes[a]>3)throw std::invalid_argument("Invalid member transform");
    }
    for(const auto& v:r.volumes){
        if(v.primitive>(r.version==7?3u:2u)||!std::isfinite(v.pitch)||!std::isfinite(v.roll)||std::abs(v.pitch)>.8f||std::abs(v.roll)>.8f||!std::isfinite(v.taper)||v.taper<0||v.taper>.8f)throw std::invalid_argument("Invalid volume profile");
        if(r.version<5&&(v.primitive||v.pitch||v.roll||v.taper))throw std::invalid_argument("Shaped volumes require generator v5");
        if(r.version<6&&(v.orientation!=std::array<float,4>{0,0,0,1}||v.shapeSeed))throw std::invalid_argument("Captured orientations and shape seeds require generator v6");
    }
    if(r.version<6&&(r.fusion!=.0657f||r.relaxation!=.45f||r.authoringScale!=1||r.samplingMm!=50||r.calibrationSeed||r.material.sideShale!=360||r.material.topShale!=420||r.material.geologyMm||r.material.dustColor!=RockMaterial{}.dustColor))throw std::invalid_argument("Calibrated controls require generator v6");
    if(r.version>=6){
        if(r.seed>UINT32_MAX||r.calibrationSeed>UINT32_MAX)throw std::invalid_argument("Calibrated seeds must fit the source 32-bit seed range");
        if((r.version==6&&r.volumes.empty())||r.formation||!r.memberEdits.empty())throw std::invalid_argument("Calibrated rocks require captured volumes and single-rock layout");
        if(!std::isfinite(r.fusion)||r.fusion<0||r.fusion>.2f||!std::isfinite(r.relaxation)||r.relaxation<0||r.relaxation>1||!std::isfinite(r.authoringScale)||r.authoringScale<.1f||r.authoringScale>10||r.samplingMm<5||r.samplingMm>100)throw std::invalid_argument("Invalid calibrated authoring controls");
        if(r.material.sideShale>1000||r.material.topShale>1000||r.material.geologyMm<450||r.material.geologyMm>3000)throw std::invalid_argument("Invalid calibrated surface controls");
        for(float value:r.material.dustColor)if(!std::isfinite(value)||value<0||value>1)throw std::invalid_argument("Invalid dust color");
        for(const auto& v:r.volumes){float norm=0;for(float q:v.orientation){if(!std::isfinite(q))throw std::invalid_argument("Invalid source orientation");norm+=q*q;}if(std::abs(norm-1)>.002f)throw std::invalid_argument("Source orientation must be a unit quaternion");}
    }
    if(r.version<7&&r.single!=SingleRockSettings{})throw std::invalid_argument("Single-rock planning requires generator v7");
    if(r.version==7&&(!std::isfinite(r.single.width)||r.single.width<.075f||r.single.width>1.2f||!std::isfinite(r.single.bodyLength)||r.single.bodyLength<.05f||r.single.bodyLength>3))throw std::invalid_argument("Invalid single-rock body dimensions");
    for(auto radius:r.radiiMm)if(radius<100||radius>20000)throw std::invalid_argument("Rock radii must be 100 to 20000 mm");
}
void saveRockRecipe(const std::filesystem::path& path,const RockRecipe& r) {
    validateRecipe(r);Json p={{"generator_version",r.version},{"seed",r.seed},{"subdivisions",r.subdivisions},{"radii_mm",r.radiiMm},{"distortion_permille",r.distortionPermille},{"band_permille",r.bandPermille},{"bands",r.bands}};
    if(r.version>=3){
        p["shape"]={{"masses",r.massCount},{"compaction",r.compaction},{"asymmetry",r.asymmetry},{"fractures",r.fractures},{"edge_damage",r.edgeDamage}};
        p["formation"]={{"kind",r.formation},{"members",r.members},{"spacing_mm",r.spacingMm}};
        p["material"]={{"family",rockMaterialFamily},{"grit",r.material.grit},{"shale",r.material.shale},{"cracks",r.material.cracks},{"dust",r.material.dust},{"variation",r.material.variation},{"worn",r.material.worn}};
        p["volumes"]=Json::array();for(const auto& v:r.volumes)p["volumes"].push_back({{"id",v.id},{"center",v.center},{"half_size",v.halfSize},{"yaw",v.yaw},{"subtractive",v.subtractive}});
    }
    if(r.version>=5){
        p["profile"]=r.profile;p["member_edits"]=Json::array();
        for(const auto& e:r.memberEdits)p["member_edits"].push_back({{"slot",e.slot},{"variant",e.variant},{"translation",e.translation},{"axes",e.axes},{"yaw",e.yaw},{"ground_only",e.groundOnly}});
        for(size_t i=0;i<r.volumes.size();++i){const auto& v=r.volumes[i];auto& j=p["volumes"][i];j["primitive"]=v.primitive;j["pitch"]=v.pitch;j["roll"]=v.roll;j["taper"]=v.taper;}
    }
    if(r.version>=6){
        p["calibration"]={{"source_seed",r.calibrationSeed},{"fusion",r.fusion},{"relaxation",r.relaxation},{"scale",r.authoringScale},{"sampling_mm",r.samplingMm}};
        auto& m=p["material"];m["side_shale"]=r.material.sideShale;m["top_shale"]=r.material.topShale;m["geology_mm"]=r.material.geologyMm;m["dust_color_linear"]=r.material.dustColor;
        for(size_t i=0;i<r.volumes.size();++i){p["volumes"][i]["orientation"]=r.volumes[i].orientation;p["volumes"][i]["shape_seed"]=r.volumes[i].shapeSeed;}
    }
    if(r.version==7)p["single"]={{"width",r.single.width},{"body_length",r.single.bodyLength},{"random_dimensions",r.single.randomDimensions},{"dark_auto_profile",r.single.darkAutoProfile}};
    writeDocument(path,"engine.rock-recipe",p);
}
RockRecipe loadRockRecipe(const std::filesystem::path& path) {
    const auto p=readDocument(path,"engine.rock-recipe");
    RockRecipe r;r.version=integer(p,"generator_version",7);
    if(p.size()!=(r.version==7?15:(r.version==6?14:(r.version>=5?13:(r.version>=3?11:7))))||!p.at("seed").is_number_unsigned()||!p.at("radii_mm").is_array()||p.at("radii_mm").size()!=3)throw std::runtime_error("Invalid rock recipe fields");
    r.seed=p.at("seed").get<uint64_t>();r.subdivisions=integer(p,"subdivisions",4);r.distortionPermille=integer(p,"distortion_permille",250);r.bandPermille=integer(p,"band_permille",150);r.bands=integer(p,"bands",32);
    for(size_t i=0;i<3;++i){const auto& v=p.at("radii_mm")[i];if(!v.is_number_unsigned()||v>20000)throw std::runtime_error("Invalid rock radius");r.radiiMm[i]=v.get<uint32_t>();}
    if(r.version>=3){
        const auto& q=p.at("shape");const auto& f=p.at("formation");const auto& m=p.at("material");
        if(q.size()!=5||f.size()!=3||m.size()!=(r.version>=6?11:7)||m.at("family")!=rockMaterialFamily||!p.at("volumes").is_array()||p.at("volumes").size()>24)throw std::runtime_error("Invalid v3 recipe fields");
        r.massCount=integer(q,"masses",12);r.compaction=integer(q,"compaction",1000);r.asymmetry=integer(q,"asymmetry",1000);r.fractures=integer(q,"fractures",1000);r.edgeDamage=integer(q,"edge_damage",1000);
        r.formation=integer(f,"kind",r.version>=4?5:3);r.members=integer(f,"members",r.version>=4?16:8);r.spacingMm=integer(f,"spacing_mm",12000);
        r.material={integer(m,"grit",1000),integer(m,"shale",1000),integer(m,"cracks",1000),integer(m,"dust",1000),integer(m,"variation",1000),integer(m,"worn",1000)};
        for(const auto& v:p.at("volumes")){
            if(v.size()!=(r.version>=6?11:(r.version>=5?9:5))||!v.at("subtractive").is_boolean()||!v.at("yaw").is_number())throw std::runtime_error("Invalid source volume");
            RockVolume volume;volume.id=integer(v,"id",UINT32_MAX);volume.center=v.at("center").get<std::array<float,3>>();volume.halfSize=v.at("half_size").get<std::array<float,3>>();volume.yaw=v.at("yaw").get<float>();volume.subtractive=v.at("subtractive");if(r.version>=5){volume.primitive=integer(v,"primitive",r.version==7?3:2);volume.pitch=v.at("pitch").get<float>();volume.roll=v.at("roll").get<float>();volume.taper=v.at("taper").get<float>();}if(r.version>=6){volume.orientation=v.at("orientation").get<std::array<float,4>>();volume.shapeSeed=integer(v,"shape_seed",UINT32_MAX);}r.volumes.push_back(volume);
        }
    }
    if(r.version>=5){
        r.profile=integer(p,"profile",r.version==7?8:4);const auto& edits=p.at("member_edits");
        if(!edits.is_array()||edits.size()>16)throw std::runtime_error("Invalid member edit array");
        for(const auto& j:edits){
            if(j.size()!=6||!j.at("ground_only").is_boolean())throw std::runtime_error("Invalid member edit fields");
            RockMemberEdit e;e.slot=integer(j,"slot",15);e.variant=integer(j,"variant",3);e.translation=j.at("translation").get<std::array<float,3>>();e.axes=j.at("axes").get<std::array<float,3>>();e.yaw=j.at("yaw").get<float>();e.groundOnly=j.at("ground_only");r.memberEdits.push_back(e);
        }
    }
    if(r.version>=6){
        const auto& c=p.at("calibration");if(c.size()!=5||!c.at("source_seed").is_number_unsigned())throw std::runtime_error("Invalid calibration fields");
        r.calibrationSeed=c.at("source_seed").get<uint64_t>();r.fusion=c.at("fusion").get<float>();r.relaxation=c.at("relaxation").get<float>();r.authoringScale=c.at("scale").get<float>();r.samplingMm=integer(c,"sampling_mm",100);
        const auto& m=p.at("material");r.material.sideShale=integer(m,"side_shale",1000);r.material.topShale=integer(m,"top_shale",1000);r.material.geologyMm=integer(m,"geology_mm",3000);r.material.dustColor=m.at("dust_color_linear").get<std::array<float,3>>();
    }
    if(r.version==7){const auto& q=p.at("single");if(q.size()!=4||!q.at("random_dimensions").is_boolean()||!q.at("dark_auto_profile").is_boolean())throw std::runtime_error("Invalid single-rock plan fields");r.single={q.at("width").get<float>(),q.at("body_length").get<float>(),q.at("random_dimensions").get<bool>(),q.at("dark_auto_profile").get<bool>()};}
    validateRecipe(r);return r;
}
void saveRockResult(const std::filesystem::path& directory,const RockResult& rock,const RockRecipe& recipe) {
    validateRecipe(recipe);saveModel(directory,rock.mesh);saveRockRecipe(directory/"recipe.json",recipe);
    if(recipe.version>=3)writeDocument(directory/"material.json","engine.rock-material",{{"family",rockMaterialFamily},{"base_textures",{"surface/textures/rocks/workbench/layered/rockworkbenchside_albedo","surface/textures/rocks/workbench/layered/rockworkbenchside_normal","surface/textures/rocks/workbench/layered/rockworkbenchside_surface"}},{"layer_textures",rockLayerTextureIds},{"controls",{{"grit",recipe.material.grit},{"shale",recipe.material.shale},{"cracks",recipe.material.cracks},{"dust",recipe.material.dust},{"variation",recipe.material.variation},{"worn",recipe.material.worn}}}});
    if(recipe.version>=6){
        auto material=readDocument(directory/"material.json","engine.rock-material");auto& c=material["controls"];
        c["side_shale"]=recipe.material.sideShale;c["top_shale"]=recipe.material.topShale;c["geology_mm"]=recipe.material.geologyMm;c["dust_color_linear"]=recipe.material.dustColor;
        writeDocument(directory/"material.json","engine.rock-material",material);
    }
    writeDocument(directory/"rock.json","engine.rock-result",{{"id",rock.id},{"minimum",rock.minimum},{"maximum",rock.maximum},{"footprint_radius",rock.footprintRadius},{"triangle_surfaces",rock.surfaces},{"member_ids",rock.memberIds},{"model","model.json"}});
}
RockResult generateRock(const RockRecipe& r,const GeneratedId& identity) {
    validateRecipe(r);if(identity.generator!="rock"||identity.generatorVersion!=r.version)throw std::invalid_argument("Rock generator identity/version mismatch");
    if(r.version>=3)return r.formation?generateRockFormation(r,identity,[](float,float){return 0.f;}):generateVolumeRock(r,identity);
    RockResult result;result.id=identity.text();auto& mesh=result.mesh;
    mesh.nodes.push_back({});mesh.joints={0};mesh.inverseBind={{{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}}};mesh.baseColor={.32f,.26f,.2f,1};mesh.roughness=.9f;mesh.metallic=0;
    uint64_t seed=mix(r.seed);for(uint64_t value:{identity.seed,uint64_t(identity.region.x),uint64_t(identity.region.z),identity.member})seed=mix(seed^mix(value));
    const V radii{r.radiiMm[0]/1000.f,r.radiiMm[1]/1000.f,r.radiiMm[2]/1000.f};
    // V2 uses broad seeded fracture planes and coherent relief instead of independent
    // vertex noise. The field is sampled identically at every detail level.
    auto random=[&](uint64_t index){return float(mix(seed+index)>>40)/float(0xffffff);};
    std::array<V,10> planes{};std::array<float,10> distances{};
    if(r.version==2)for(size_t i=0;i<planes.size();++i){
        const float angle=random(i*3+40)*6.2831853f;
        planes[i]=unit({std::cos(angle),random(i*3+41)*1.6f-.8f,std::sin(angle)});
        distances[i]=.82f+random(i*3+42)*.24f;
    }
    auto point=[&](int x,int y,int z) {
        // Reduced integer directions keep shared octant edges and coarser vertices identical.
        const int divisor=std::gcd(std::gcd(std::abs(x),std::abs(y)),std::abs(z));x/=divisor;y/=divisor;z/=divisor;
        V v=unit({float(x),float(y),float(z)});
        if(r.version==2){
            // A rounded block gives broad shoulders, rather than a pointed octahedron.
            const float power=3.4f;
            float radius=std::pow(std::pow(std::abs(v[0]),power)+std::pow(std::abs(v[1]),power)+std::pow(std::abs(v[2]),power),-1.f/power);
            for(size_t i=0;i<planes.size();++i){const float projection=dot(v,planes[i]);if(projection>0)radius=std::min(radius,distances[i]/projection);}
            const float phase=random(11)*6.2831853f;
            const float broad=std::sin(v[0]*3.2f+v[1]*1.7f+phase)*std::cos(v[2]*2.8f-v[1]*1.3f+phase*.7f);
            const float detail=std::sin(v[0]*8.1f-v[2]*5.7f+phase)*std::sin(v[1]*6.3f+v[2]*4.1f);
            const float strata=std::sin((v[1]+.12f*v[0]-.08f*v[2])*float(r.bands)*3.14159265f+phase);
            radius*=1+(r.distortionPermille/1000.f)*(broad*.65f+detail*.12f)+(r.bandPermille/1000.f)*strata*.45f;
            V p{v[0]*radius,v[1]*radius,v[2]*radius};
            // Lean and taper the mass slightly; the underside is anchored after sampling.
            p[0]+=.10f*p[1]*std::sin(phase);p[2]+=.07f*p[1]*std::cos(phase);
            return V{p[0]*radii[0],p[1]*radii[1]+radii[1],p[2]*radii[2]};
        }
        const uint64_t random=mix(seed^mix(uint64_t(x))^std::rotl(mix(uint64_t(y)),21)^std::rotl(mix(uint64_t(z)),42));
        const float noise=float(random>>40)/float(0xffffff)*2-1;
        const float weight=1-v[1]*v[1];
        const float radius=1+weight*(r.distortionPermille/1000.f*noise+r.bandPermille/1000.f*std::sin(v[1]*float(r.bands)*3.141592653589793f));
        return V{v[0]*radii[0]*radius,v[1]*radii[1]*radius+radii[1],v[2]*radii[2]*radius};
    };
    bool first=true;
    auto triangle=[&](V a,V b,V c) {
        V normal=cross(subtract(b,a),subtract(c,a));const V radial{a[0]+b[0]+c[0],a[1]+b[1]+c[1]-3*radii[1],a[2]+b[2]+c[2]};
        if(dot(normal,radial)<0){std::swap(b,c);normal=cross(subtract(b,a),subtract(c,a));}
        normal=unit(normal);const V tangent=unit(std::abs(normal[1])<.9f?cross({0,1,0},normal):cross({1,0,0},normal));
        result.surfaces.push_back(normal[1]>.65f?1:(normal[1]<-.5f?2:0));
        for(const auto& p:{a,b,c}) {
            SkinVertex vertex;vertex.position=p;vertex.normal=normal;vertex.tangent={tangent[0],tangent[1],tangent[2],1};vertex.uv={p[0]/(2*radii[0])+.5f,p[2]/(2*radii[2])+.5f};
            mesh.indices.push_back(uint32_t(mesh.vertices.size()));mesh.vertices.push_back(vertex);
            if(first){result.minimum=p;result.maximum=p;first=false;}
            for(size_t i=0;i<3;++i){result.minimum[i]=std::min(result.minimum[i],p[i]);result.maximum[i]=std::max(result.maximum[i],p[i]);}
            result.footprintRadius=std::max(result.footprintRadius,std::hypot(p[0],p[2]));
        }
    };
    const int n=1<<r.subdivisions;mesh.vertices.reserve(size_t(24*n*n));mesh.indices.reserve(size_t(24*n*n));
    for(int sx:{-1,1})for(int sy:{-1,1})for(int sz:{-1,1}) {
        auto p=[&](int i,int j){return point(sx*i,sy*j,sz*(n-i-j));};
        for(int i=0;i<n;++i)for(int j=0;j<n-i;++j){triangle(p(i,j),p(i+1,j),p(i,j+1));if(i+j<n-1)triangle(p(i+1,j),p(i+1,j+1),p(i,j+1));}
    }
    if(r.version==2){
        const float base=result.minimum[1];
        for(auto& vertex:mesh.vertices)vertex.position[1]-=base;
        result.minimum[1]=0;result.maximum[1]-=base;
        // Share normals at exactly shared positions, retaining hard fracture creases.
        std::map<V,std::vector<V>> incident;
        for(size_t i=0;i<mesh.vertices.size();i+=3){
            const auto area=cross(subtract(mesh.vertices[i+1].position,mesh.vertices[i].position),subtract(mesh.vertices[i+2].position,mesh.vertices[i].position));
            for(size_t j=0;j<3;++j)incident[mesh.vertices[i+j].position].push_back(area);
        }
        for(auto& vertex:mesh.vertices){
            V sum{};for(const auto& area:incident.at(vertex.position))if(dot(unit(area),vertex.normal)>.72f)for(size_t j=0;j<3;++j)sum[j]+=area[j];
            vertex.normal=unit(sum);const auto n=vertex.normal;
            const auto tangent=unit(std::abs(n[1])<.9f?cross({0,1,0},n):cross({1,0,0},n));
            vertex.tangent={tangent[0],tangent[1],tangent[2],1};
        }
    }
    validateModel(mesh);return result;
}
}

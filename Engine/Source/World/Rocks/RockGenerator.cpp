#include "World/Rocks/RockGenerator.h"
#include "Persistence/Document.h"
#include <algorithm>
#include <bit>
#include <cmath>
#include <numeric>
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
    if(r.version!=1||r.subdivisions>4||r.distortionPermille>250||r.bandPermille>150||r.bands<1||r.bands>32)throw std::invalid_argument("Unsupported rock recipe version or parameters");
    for(auto radius:r.radiiMm)if(radius<100||radius>20000)throw std::invalid_argument("Rock radii must be 100 to 20000 mm");
}
void saveRockRecipe(const std::filesystem::path& path,const RockRecipe& r) {
    validateRecipe(r);writeDocument(path,"engine.rock-recipe",{{"generator_version",r.version},{"seed",r.seed},{"subdivisions",r.subdivisions},{"radii_mm",r.radiiMm},{"distortion_permille",r.distortionPermille},{"band_permille",r.bandPermille},{"bands",r.bands}});
}
RockRecipe loadRockRecipe(const std::filesystem::path& path) {
    const auto p=readDocument(path,"engine.rock-recipe");if(p.size()!=7||!p.at("seed").is_number_unsigned()||!p.at("radii_mm").is_array()||p.at("radii_mm").size()!=3)throw std::runtime_error("Invalid rock recipe fields");
    RockRecipe r;r.seed=p.at("seed").get<uint64_t>();r.version=integer(p,"generator_version",1);r.subdivisions=integer(p,"subdivisions",4);r.distortionPermille=integer(p,"distortion_permille",250);r.bandPermille=integer(p,"band_permille",150);r.bands=integer(p,"bands",32);
    for(size_t i=0;i<3;++i){const auto& v=p.at("radii_mm")[i];if(!v.is_number_unsigned()||v>20000)throw std::runtime_error("Invalid rock radius");r.radiiMm[i]=v.get<uint32_t>();}
    validateRecipe(r);return r;
}
void saveRockResult(const std::filesystem::path& directory,const RockResult& rock,const RockRecipe& recipe) {
    validateRecipe(recipe);saveModel(directory,rock.mesh);saveRockRecipe(directory/"recipe.json",recipe);
    writeDocument(directory/"rock.json","engine.rock-result",{{"id",rock.id},{"minimum",rock.minimum},{"maximum",rock.maximum},{"footprint_radius",rock.footprintRadius},{"triangle_surfaces",rock.surfaces},{"model","model.json"}});
}
RockResult generateRock(const RockRecipe& r,const GeneratedId& identity) {
    validateRecipe(r);if(identity.generator!="rock"||identity.generatorVersion!=r.version)throw std::invalid_argument("Rock generator identity/version mismatch");
    RockResult result;result.id=identity.text();auto& mesh=result.mesh;
    mesh.nodes.push_back({});mesh.joints={0};mesh.inverseBind={{{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}}};mesh.baseColor={.32f,.26f,.2f,1};mesh.roughness=.9f;mesh.metallic=0;
    uint64_t seed=mix(r.seed);for(uint64_t value:{identity.seed,uint64_t(identity.region.x),uint64_t(identity.region.z),identity.member})seed=mix(seed^mix(value));
    const V radii{r.radiiMm[0]/1000.f,r.radiiMm[1]/1000.f,r.radiiMm[2]/1000.f};
    auto point=[&](int x,int y,int z) {
        // Reduced integer directions keep shared octant edges and coarser vertices identical.
        const int divisor=std::gcd(std::gcd(std::abs(x),std::abs(y)),std::abs(z));x/=divisor;y/=divisor;z/=divisor;
        V v=unit({float(x),float(y),float(z)});
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
    validateModel(mesh);return result;
}
}

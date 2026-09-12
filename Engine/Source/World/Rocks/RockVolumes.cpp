#include "World/Rocks/RockGenerator.h"
#include "World/Rocks/CalibratedRock.h"
#include <algorithm>
#include <cmath>
#include <map>
#include <set>
#include <stdexcept>
namespace engine {
namespace {
using V=std::array<float,3>;
V add(V a,V b){for(int i=0;i<3;++i)a[i]+=b[i];return a;}
V sub(V a,V b){for(int i=0;i<3;++i)a[i]-=b[i];return a;}
V mul(V a,float s){for(auto& x:a)x*=s;return a;}
float dot(V a,V b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
V cross(V a,V b){return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
V unit(V a){const auto length=std::sqrt(dot(a,a));return length>1e-12f?mul(a,1/length):V{0,1,0};}
uint64_t mix(uint64_t v){v+=0x9e3779b97f4a7c15ULL;v=(v^(v>>30))*0xbf58476d1ce4e5b9ULL;v=(v^(v>>27))*0x94d049bb133111ebULL;return v^(v>>31);}
uint64_t rockSeed(const RockRecipe& r,const GeneratedId& id){return mix(r.seed^mix(id.seed)^mix(uint64_t(id.region.x))^mix(uint64_t(id.region.z)+17)^mix(id.member));}
float rnd(uint64_t seed,uint64_t n){return float(mix(seed+n)>>40)/float(0xffffff);}
float smoothMin(float a,float b,float k){if(k<=0)return std::min(a,b);const float h=std::max(k-std::abs(a-b),0.f)/k;return std::min(a,b)-h*h*k*.25f;}
float boxField(const RockVolume& v,V p,float bevel,bool shaped){
    p=sub(p,v.center);const float c=std::cos(v.yaw),s=std::sin(v.yaw);p={c*p[0]-s*p[2],p[1],s*p[0]+c*p[2]};
    if(shaped){
        const float cp=std::cos(v.pitch),sp=std::sin(v.pitch);p={p[0],cp*p[1]+sp*p[2],-sp*p[1]+cp*p[2]};
        const float cr=std::cos(v.roll),sr=std::sin(v.roll);p={cr*p[0]+sr*p[1],-sr*p[0]+cr*p[1],p[2]};
    }
    V half=v.halfSize;
    if(shaped&&v.primitive==1){const float taper=1-v.taper*std::clamp((p[1]/half[1]+1)*.5f,0.f,1.f);half[0]*=taper;half[2]*=taper;}
    const float b=std::min(bevel,*std::min_element(v.halfSize.begin(),v.halfSize.end())*.7f);
    V q;for(int i=0;i<3;++i)q[i]=std::abs(p[i])-half[i]+b;
    V positive;for(int i=0;i<3;++i)positive[i]=std::max(q[i],0.f);
    float value=std::sqrt(dot(positive,positive))+std::min(std::max({q[0],q[1],q[2]}),0.f)-b;
    if(shaped&&v.primitive==2)value=std::max(value,(p[1]+p[0]*v.taper*half[1]/half[0]-half[1]*(1-v.taper*.45f))/std::sqrt(1.f+(v.taper*half[1]/half[0])*(v.taper*half[1]/half[0])));
    // Clip four corners to preserve broad fractured faces instead of rounded boxes.
    if(shaped&&!v.subtractive)value=std::max(value,(std::abs(p[0])/half[0]+std::abs(p[2])/half[2]-1.65f)*std::min(half[0],half[2])*.7071f);
    return value;
}
}
std::vector<RockVolume> planRockVolumes(const RockRecipe& r,const GeneratedId& id){
    validateRecipe(r);if(!r.volumes.empty())return r.volumes;
    const auto seed=rockSeed(r,id);std::vector<RockVolume> plan;
    plan.push_back({0,{0,0,0},{.70f,.76f,.65f},(rnd(seed,1)-.5f)*.3f,false});
    const float asym=r.asymmetry/1000.f,compact=r.compaction/1000.f;
    for(uint32_t i=1;i<r.massCount;++i){
        const float angle=6.2831853f*(float(i)/r.massCount)+rnd(seed,i*7)*.6f;
        const float reach=.65f-compact*.28f;
        V center{std::cos(angle)*reach+asym*.22f,(rnd(seed,i*7+1)-.5f)*.85f,std::sin(angle)*reach};
        const float size=.3f+rnd(seed,i*7+2)*.25f;
        plan.push_back({i,center,{size,size*(.75f+rnd(seed,i*7+3)*.7f),size},(rnd(seed,i*7+4)-.5f)*1.2f,false});
    }
    const uint32_t cuts=r.fractures?1+r.fractures/400:0;
    for(uint32_t i=0;i<cuts;++i){
        // Cuts enter from an exposed shoulder, preserving a substantial core.
        const float yaw=rnd(seed,200+i*5)*6.28f;
        plan.push_back({100+i,{std::cos(yaw)*.58f,.36f+rnd(seed,201+i*5)*.25f,std::sin(yaw)*.58f},
            {.035f+r.fractures*.00007f,.55f,.85f},yaw,false});plan.back().subtractive=true;
    }
    if(r.version==5){
        const uint32_t profile=r.profile?r.profile:1+uint32_t(mix(seed+707)%4);
        const V proportions=profile==2?V{1.35f,.38f,.95f}:(profile==4?V{.57f,1.38f,.60f}:(profile==3?V{.88f,.84f,.86f}:V{1.05f,.88f,1}));
        for(auto& v:plan){
            for(size_t a=0;a<3;++a){v.center[a]*=proportions[a];v.halfSize[a]=std::max(.03f,v.halfSize[a]*proportions[a]);}
            v.pitch=(rnd(seed,400+v.id)-.5f)*.35f;v.roll=(rnd(seed,500+v.id)-.5f)*.30f;
            if(!v.subtractive){v.primitive=(profile==2?2u:(profile==4?1u:(v.id%2?2u:1u)));v.taper=profile==4?.65f:(profile==3?.48f:(profile==2?.65f:.22f));}
        }
    }
    return plan;
}
RockResult generateVolumeRock(const RockRecipe& r,const GeneratedId& id){
    const auto volumes=planRockVolumes(r,id);const auto seed=rockSeed(r,id);
    std::vector<CalibratedRockVolume> calibrated;
    if(r.version==6)for(const auto& v:volumes)calibrated.emplace_back(v,r.edgeDamage*.001f,uint32_t(r.seed^r.calibrationSeed));
    const float smoothing=r.version==6?r.fusion:.025f+r.compaction*.00014f,bevel=.02f+r.edgeDamage*.00011f;
    auto field=[&](V p){
        float value=1e6f;
        if(r.version==6){
            for(size_t i=0;i<volumes.size();++i)if(!volumes[i].subtractive)value=smoothMin(value,calibrated[i].evaluate(p),smoothing);
            for(size_t i=0;i<volumes.size();++i)if(volumes[i].subtractive)value=std::max(value,-calibrated[i].evaluate(p));
            return value;
        }
        for(const auto& v:volumes)if(!v.subtractive)value=smoothMin(value,boxField(v,p,bevel,r.version==5),smoothing);
        const float phase=rnd(seed,900)*6.28f;
        value+=r.distortionPermille*.00020f*std::sin(p[0]*4.1f+phase)*std::sin(p[1]*3.7f-p[2]*2.4f)
             +r.bandPermille*.00016f*std::sin((p[1]+p[0]*.12f)*float(r.bands)*3.14f+phase);
        for(const auto& v:volumes)if(v.subtractive)value=std::max(value,-boxField(v,p,bevel*.15f,r.version==5));
        return value;
    };
    V lo{1e6f,1e6f,1e6f},hi{-1e6f,-1e6f,-1e6f};
    for(const auto& v:volumes)if(!v.subtractive){const float c=std::abs(std::cos(v.yaw)),s=std::abs(std::sin(v.yaw));V extent{c*v.halfSize[0]+s*v.halfSize[2],v.halfSize[1],s*v.halfSize[0]+c*v.halfSize[2]};
        if(r.version==5){extent={};for(int x:{-1,1})for(int y:{-1,1})for(int z:{-1,1}){
            V q{x*v.halfSize[0],y*v.halfSize[1],z*v.halfSize[2]};
            const float cr=std::cos(v.roll),sr=std::sin(v.roll);q={cr*q[0]-sr*q[1],sr*q[0]+cr*q[1],q[2]};
            const float cp=std::cos(v.pitch),sp=std::sin(v.pitch);q={q[0],cp*q[1]-sp*q[2],sp*q[1]+cp*q[2]};
            const float cy=std::cos(v.yaw),sy=std::sin(v.yaw);q={cy*q[0]+sy*q[2],q[1],-sy*q[0]+cy*q[2]};
            for(size_t a=0;a<3;++a)extent[a]=std::max(extent[a],std::abs(q[a]));
        }}for(int a=0;a<3;++a){lo[a]=std::min(lo[a],v.center[a]-extent[a]);hi[a]=std::max(hi[a],v.center[a]+extent[a]);}}
    if(r.version==6){lo={1e6f,1e6f,1e6f};hi={-1e6f,-1e6f,-1e6f};for(size_t i=0;i<volumes.size();++i)if(!volumes[i].subtractive)for(size_t a=0;a<3;++a){lo[a]=std::min(lo[a],calibrated[i].minimum[a]);hi[a]=std::max(hi[a],calibrated[i].maximum[a]);}}
    // Additive bounds only. Large subtractive tools must not coarsen the grid.
    const float padding=.2f+smoothing*float(volumes.size())*.25f;for(int a=0;a<3;++a){lo[a]-=padding;hi[a]+=padding;}
    const auto span=sub(hi,lo);const float cell=r.version==6?r.samplingMm*.001f*std::pow(2.f,2-int(r.subdivisions)):*std::max_element(span.begin(),span.end())/float((r.version>=4?10:12)+8*r.subdivisions);
    std::array<int,3> cells;V step;for(int a=0;a<3;++a){cells[a]=std::max(4,int(std::ceil(span[a]/cell)));step[a]=span[a]/cells[a];}
    if(uint64_t(cells[0]+1)*uint64_t(cells[1]+1)*uint64_t(cells[2]+1)>2000000)throw std::runtime_error("Rock sampling exceeds authoring budget; increase voxel size or lower detail");
    const int nx=cells[0]+1,ny=cells[1]+1,nz=cells[2]+1;
    std::vector<V> points(size_t(nx*ny*nz));std::vector<float> values(points.size());
    auto index=[&](int x,int y,int z){return uint32_t(x+nx*(y+ny*z));};
    for(int z=0;z<nz;++z)for(int y=0;y<ny;++y)for(int x=0;x<nx;++x){const auto i=index(x,y,z);points[i]={lo[0]+x*step[0],lo[1]+y*step[1],lo[2]+z*step[2]};values[i]=field(points[i]);if(std::abs(values[i])<cell*.05f)values[i]=std::copysign(cell*.05f,values[i]);}
    RockResult result;result.id=id.text();result.memberIds={id.text()};auto& mesh=result.mesh;
    mesh.nodes.push_back({});mesh.joints={0};mesh.inverseBind={{{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}}};mesh.baseColor={.32f,.29f,.26f,1};mesh.roughness=.9f;mesh.metallic=0;
    std::map<std::pair<uint32_t,uint32_t>,uint32_t> edges;
    auto vertex=[&](uint32_t a,uint32_t b){
        if(a>b)std::swap(a,b);const auto key=std::pair{a,b};if(const auto it=edges.find(key);it!=edges.end())return it->second;
        const float t=values[a]/(values[a]-values[b]);SkinVertex v;v.position=add(points[a],mul(sub(points[b],points[a]),t));const auto i=uint32_t(mesh.vertices.size());mesh.vertices.push_back(v);edges[key]=i;return i;
    };
    auto triangle=[&](uint32_t a,uint32_t b,uint32_t c,V outside){
        const auto n=cross(sub(mesh.vertices[b].position,mesh.vertices[a].position),sub(mesh.vertices[c].position,mesh.vertices[a].position));
        if(dot(n,n)<1e-20f)throw std::runtime_error("Degenerate volume-grid intersection");if(dot(n,outside)<0)std::swap(b,c);mesh.indices.insert(mesh.indices.end(),{a,b,c});
    };
    constexpr int corners[8][3]={{0,0,0},{1,0,0},{1,1,0},{0,1,0},{0,0,1},{1,0,1},{1,1,1},{0,1,1}};
    constexpr int tetra[6][4]={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
    for(int z=0;z<cells[2];++z)for(int y=0;y<cells[1];++y)for(int x=0;x<cells[0];++x){
        uint32_t ids[8];for(int a=0;a<8;++a)ids[a]=index(x+corners[a][0],y+corners[a][1],z+corners[a][2]);
        for(const auto& t:tetra){uint32_t in[4],out[4];int ni=0,no=0;V ci{},co{};for(int a:t){const auto v=ids[a];if(values[v]<0){in[ni++]=v;ci=add(ci,points[v]);}else{out[no++]=v;co=add(co,points[v]);}}
            if(ni==0||ni==4)continue;const auto outward=sub(mul(co,1.f/no),mul(ci,1.f/ni));
            if(ni==1)triangle(vertex(in[0],out[0]),vertex(in[0],out[1]),vertex(in[0],out[2]),outward);
            else if(ni==3)triangle(vertex(out[0],in[0]),vertex(out[0],in[1]),vertex(out[0],in[2]),outward);
            else {const auto a=vertex(in[0],out[0]),b=vertex(in[0],out[1]),c=vertex(in[1],out[0]),d=vertex(in[1],out[1]);triangle(a,b,d,outward);triangle(a,d,c,outward);}
        }
    }
    if(mesh.indices.empty()||mesh.indices.size()>300000)throw std::runtime_error("Source volumes leave no visible rock, or exceed the mesh budget");
    // A single rock retains its largest connected body; fracture chips are not floating objects.
    std::vector<uint32_t> parent(mesh.vertices.size());for(uint32_t i=0;i<parent.size();++i)parent[i]=i;
    auto root=[&](uint32_t i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;};
    for(size_t i=0;i<mesh.indices.size();i+=3){const auto a=root(mesh.indices[i]);parent[root(mesh.indices[i+1])]=a;parent[root(mesh.indices[i+2])]=a;}
    std::map<uint32_t,size_t> counts;for(size_t i=0;i<mesh.indices.size();i+=3)++counts[root(mesh.indices[i])];
    uint32_t largest=counts.begin()->first;for(const auto& [component,count]:counts)if(count>counts[largest])largest=component;
    std::vector<SkinVertex> kept;std::vector<uint32_t> indices,remap(mesh.vertices.size(),UINT32_MAX);
    for(size_t i=0;i<mesh.indices.size();i+=3)if(root(mesh.indices[i])==largest)for(int a=0;a<3;++a){const auto old=mesh.indices[i+a];if(remap[old]==UINT32_MAX){remap[old]=uint32_t(kept.size());kept.push_back(mesh.vertices[old]);}indices.push_back(remap[old]);}
    mesh.vertices=std::move(kept);mesh.indices=std::move(indices);
    if(r.version==6)relaxCalibratedRock(mesh,r.relaxation,cell);
    // Fit physical dimensions once after meshing; all LODs share the same ground anchor.
    lo={1e6f,1e6f,1e6f};hi={-1e6f,-1e6f,-1e6f};for(const auto& v:mesh.vertices)for(int a=0;a<3;++a){lo[a]=std::min(lo[a],v.position[a]);hi[a]=std::max(hi[a],v.position[a]);}
    float fit=1e6f;for(int a=0;a<3;++a)fit=std::min(fit,float(r.radiiMm[a])*.002f/(hi[a]-lo[a]));
    for(auto& v:mesh.vertices){for(int a=0;a<3;++a){
        if(r.version==6)v.position[a]*=r.authoringScale;
        else if(r.version==5)v.position[a]=(v.position[a]-(a==1?lo[a]:(lo[a]+hi[a])*.5f))*fit;
        else v.position[a]=((v.position[a]-lo[a])/(hi[a]-lo[a])-(a==1?0.f:.5f))*float(r.radiiMm[a])*.002f;
    }v.normal={};v.uv={v.position[0],v.position[2]};}
    for(size_t i=0;i<mesh.indices.size();i+=3){auto& a=mesh.vertices[mesh.indices[i]];auto& b=mesh.vertices[mesh.indices[i+1]];auto& c=mesh.vertices[mesh.indices[i+2]];const auto n=cross(sub(b.position,a.position),sub(c.position,a.position));a.normal=add(a.normal,n);b.normal=add(b.normal,n);c.normal=add(c.normal,n);const auto normal=unit(n);result.surfaces.push_back(normal[1]>.65f?1:(normal[1]<-.5f?2:0));}
    result.minimum={1e6f,1e6f,1e6f};result.maximum={-1e6f,-1e6f,-1e6f};
    for(auto& v:mesh.vertices){v.normal=unit(v.normal);const auto tangent=unit(cross(std::abs(v.normal[1])<.9f?V{0,1,0}:V{1,0,0},v.normal));v.tangent={tangent[0],tangent[1],tangent[2],1};for(int a=0;a<3;++a){result.minimum[a]=std::min(result.minimum[a],v.position[a]);result.maximum[a]=std::max(result.maximum[a],v.position[a]);}result.footprintRadius=std::max(result.footprintRadius,std::hypot(v.position[0],v.position[2]));}
    return result;
}
}

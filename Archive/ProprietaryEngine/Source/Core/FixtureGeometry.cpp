#include "Core/FixtureGeometry.h"
#include <cmath>
#include <algorithm>
#include <stdexcept>

namespace engine {
namespace {
using V = std::array<float, 3>;
V subtract(V a, V b) { return {a[0]-b[0], a[1]-b[1], a[2]-b[2]}; }
V cross(V a, V b) { return {a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]}; }
float dot(V a, V b) { return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]; }
V normalized(V a) {
    const float length=std::sqrt(dot(a,a));
    if (!std::isfinite(length) || length<=0) throw std::runtime_error("Degenerate fixture normal");
    return {a[0]/length,a[1]/length,a[2]/length};
}
FixtureVertex vertex(V p, V n) { return {p[0],p[1],p[2],n[0],n[1],n[2]}; }
void triangle(FixtureMesh& mesh, V a, V b, V c, bool smooth=false) {
    const auto n=normalized(cross(subtract(b,a),subtract(c,a)));
    for (auto p : {a,b,c}) mesh.push_back(vertex(p, smooth?normalized(p):n));
}
V transform(V p, const Matrix4& m) {
    return {m[0]*p[0]+m[4]*p[1]+m[8]*p[2]+m[12],
            m[1]*p[0]+m[5]*p[1]+m[9]*p[2]+m[13],
            m[2]*p[0]+m[6]*p[1]+m[10]*p[2]+m[14]};
}
}
Matrix4 normalMatrix(const Matrix4& m) {
    for (float value : m) if (!std::isfinite(value)) throw std::runtime_error("Nonfinite model transform");
    if (m[3]!=0 || m[7]!=0 || m[11]!=0 || m[15]!=1) throw std::runtime_error("Expected affine model transform");
    const V a{m[0],m[1],m[2]}, b{m[4],m[5],m[6]}, c{m[8],m[9],m[10]};
    const auto x=cross(b,c), y=cross(c,a), z=cross(a,b);
    const float determinant=dot(a,x);
    const float scale=std::sqrt(dot(a,a))*std::sqrt(dot(b,b))*std::sqrt(dot(c,c));
    if (!std::isfinite(scale) || scale<=0 || !std::isfinite(determinant) || std::abs(determinant)<=scale*1e-6f)
        throw std::runtime_error("Singular or ill-conditioned model transform");
    Matrix4 result{x[0]/determinant,x[1]/determinant,x[2]/determinant,0,
                   y[0]/determinant,y[1]/determinant,y[2]/determinant,0,
                   z[0]/determinant,z[1]/determinant,z[2]/determinant,0, 0,0,0,1};
    for (float value : result) if (!std::isfinite(value)) throw std::runtime_error("Normal transform overflow");
    return result;
}
FixtureMesh fixtureCube() {
    const V faces[6][4] = {
        {{-.5f,-.5f,.5f},{.5f,-.5f,.5f},{.5f,.5f,.5f},{-.5f,.5f,.5f}},
        {{.5f,-.5f,-.5f},{-.5f,-.5f,-.5f},{-.5f,.5f,-.5f},{.5f,.5f,-.5f}},
        {{.5f,-.5f,.5f},{.5f,-.5f,-.5f},{.5f,.5f,-.5f},{.5f,.5f,.5f}},
        {{-.5f,-.5f,-.5f},{-.5f,-.5f,.5f},{-.5f,.5f,.5f},{-.5f,.5f,-.5f}},
        {{-.5f,.5f,.5f},{.5f,.5f,.5f},{.5f,.5f,-.5f},{-.5f,.5f,-.5f}},
        {{-.5f,-.5f,-.5f},{.5f,-.5f,-.5f},{.5f,-.5f,.5f},{-.5f,-.5f,.5f}}};
    FixtureMesh mesh;
    for (const auto& f : faces) { triangle(mesh,f[0],f[1],f[2]); triangle(mesh,f[0],f[2],f[3]); }
    return mesh;
}
FixtureMesh fixtureSlopedSolid() {
    // A centered tetrahedron provides flat non-axis-aligned surface normals.
    const V a{-.7f,-.5f,-.5f}, b{.7f,-.5f,-.5f}, c{0,-.5f,.8f}, d{0,.8f,0};
    FixtureMesh mesh;
    triangle(mesh,a,b,c); triangle(mesh,a,d,b); triangle(mesh,b,d,c); triangle(mesh,c,d,a);
    return mesh;
}
FixtureMesh fixtureSphere() {
    constexpr int rings=16, segments=32;
    constexpr float pi=3.14159265358979323846f;
    auto point=[&](int ring,int segment)->V {
        if (ring==0) return {0,.5f,0};
        if (ring==rings) return {0,-.5f,0};
        const float latitude=pi*float(ring)/rings, longitude=2*pi*float(segment%segments)/segments;
        return {.5f*std::sin(latitude)*std::cos(longitude), .5f*std::cos(latitude), .5f*std::sin(latitude)*std::sin(longitude)};
    };
    FixtureMesh mesh;
    for (int r=0;r<rings;++r) for (int s=0;s<segments;++s) {
        const auto a=point(r,s), b=point(r,s+1), c=point(r+1,s), d=point(r+1,s+1);
        if (r>0) triangle(mesh,a,b,c,true);
        if (r<rings-1) triangle(mesh,b,d,c,true);
    }
    return mesh;
}
FixtureMesh fixtureCapsule() {
    constexpr int rings=17,segments=32;
    constexpr float pi=3.14159265358979323846f;
    auto point=[](int ring,int segment)->V {
        if(ring==0) return {0,.9f,0};
        if(ring==rings) return {0,-.9f,0};
        const float latitude=pi*float(ring<=8?ring:ring-1)/16;
        const float longitude=2*pi*float(segment%segments)/segments;
        return {.35f*std::sin(latitude)*std::cos(longitude),.35f*std::cos(latitude)+(ring<=8?.55f:-.55f),.35f*std::sin(latitude)*std::sin(longitude)};
    };
    FixtureMesh mesh;
    for(int r=0;r<rings;++r) for(int s=0;s<segments;++s) {
        const auto a=point(r,s),b=point(r,s+1),c=point(r+1,s),d=point(r+1,s+1);
        if(r>0) triangle(mesh,a,b,c);
        if(r<rings-1) triangle(mesh,b,d,c);
    }
    for(auto& v:mesh) {const auto n=normalized({v.x,v.y-std::clamp(v.y,-.55f,.55f),v.z});v.nx=n[0];v.ny=n[1];v.nz=n[2];}
    return mesh;
}
FixtureMesh bakeFlatReference(const FixtureMesh& mesh,const Matrix4& model) {
    if (mesh.size()%3) throw std::runtime_error("Incomplete reference triangle");
    FixtureMesh baked;
    for (size_t i=0;i<mesh.size();i+=3) {
        const auto& a=mesh[i]; const auto& b=mesh[i+1]; const auto& c=mesh[i+2];
        triangle(baked,transform({a.x,a.y,a.z},model),transform({b.x,b.y,b.z},model),transform({c.x,c.y,c.z},model));
    }
    return baked;
}
}

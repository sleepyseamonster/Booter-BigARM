#include "Core/FixtureGeometry.h"
#include "Core/FixtureState.h"
#include <cmath>
#include <iostream>
#include <stdexcept>

namespace {
using V=std::array<float,3>;
void require(bool value,const char* message) { if (!value) throw std::runtime_error(message); }
V cross(V a,V b) { return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]}; }
float dot(V a,V b) { return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]; }
V direction(const engine::Matrix4& m,V v) {
    return {m[0]*v[0]+m[4]*v[1]+m[8]*v[2],m[1]*v[0]+m[5]*v[1]+m[9]*v[2],m[2]*v[0]+m[6]*v[1]+m[10]*v[2]};
}
void rejects(const engine::Matrix4& m) {
    bool threw=false; try { engine::normalMatrix(m); } catch (const std::runtime_error&) { threw=true; }
    require(threw,"Invalid normal transform must fail explicitly");
}
void checkMesh(const engine::FixtureMesh& mesh,size_t expected,bool sphere=false) {
    require(mesh.size()==expected,"Unexpected fixture vertex count");
    for (size_t i=0;i<mesh.size();i+=3) {
        const auto& a=mesh[i]; const auto& b=mesh[i+1]; const auto& c=mesh[i+2];
        const auto face=cross({b.x-a.x,b.y-a.y,b.z-a.z},{c.x-a.x,c.y-a.y,c.z-a.z});
        require(dot(face,face)>1e-9f,"Fixture contains a degenerate triangle");
        require(dot(face,{a.x+b.x+c.x,a.y+b.y+c.y,a.z+b.z+c.z})>0,"Triangle must face away from interior origin");
        for (const auto* v : {&a,&b,&c}) {
            const V n{v->nx,v->ny,v->nz};
            require(std::abs(dot(n,n)-1)<1e-5f && dot(face,n)>0,"Unit outward normals required");
            if (sphere) {
                require(std::abs(v->x*v->x+v->y*v->y+v->z*v->z-.25f)<1e-5f,"Sphere radius must be one half");
                require(std::abs(v->nx-2*v->x)+std::abs(v->ny-2*v->y)+std::abs(v->nz-2*v->z)<1e-5f,"Sphere normals must be radial");
            }
        }
    }
}
}
int main() {
    try {
        checkMesh(engine::fixtureCube(),36);
        checkMesh(engine::fixtureSlopedSolid(),12);
        checkMesh(engine::fixtureSphere(),32*30*3,true);
        checkMesh(engine::fixtureCapsule(),32*32*3);
        // An affine shear, nonuniform scale and translation exercise more than axis-aligned normals.
        engine::Matrix4 model{2,0,0,0, .7f,3,0,0, .3f,.2f,.5f,0, 8,-2,5,1};
        const auto normal=engine::normalMatrix(model);
        const auto worldTangentA=direction(model,{1,1,0}), worldTangentB=direction(model,{0,1,1});
        const auto worldNormal=direction(normal,{1,-1,1});
        require(std::abs(dot(worldNormal,worldTangentA))<1e-5f && std::abs(dot(worldNormal,worldTangentB))<1e-5f,
            "Transformed normal must remain perpendicular to transformed surface tangents");
        const auto incorrect=direction(model,{1,-1,1});
        require(std::abs(dot(incorrect,worldTangentA))>1,"Regression fixture must distinguish the old position-matrix bug");
        auto translated=model; translated[12]=100; translated[13]=200; translated[14]=-300;
        require(engine::normalMatrix(translated)==normal,"Translation must not affect normals");
        auto mirrored=model; mirrored[0]=-2;
        const auto mirroredNormal=direction(engine::normalMatrix(mirrored),{1,-1,1});
        require(std::abs(dot(mirroredNormal,direction(mirrored,{1,1,0})))<1e-5f,"Negative determinant normal math");
        auto invalid=model; invalid[0]=0; rejects(invalid);
        invalid=model; invalid[3]=1; rejects(invalid);
        invalid=model; invalid[8]=NAN; rejects(invalid);
        invalid=model; invalid[4]=2; invalid[5]=1e-8f; rejects(invalid);
        const auto source=engine::fixtureSlopedSolid(), baked=engine::bakeFlatReference(source,model);
        for (size_t i=0;i<source.size();++i) {
            auto n=direction(normal,{source[i].nx,source[i].ny,source[i].nz});
            const float length=std::sqrt(dot(n,n)); for (auto& v:n) v/=length;
            require(dot(n,{baked[i].nx,baked[i].ny,baked[i].nz})>.99999f,"Independent baked surface normal mismatch");
        }
        engine::FixtureState state; state.mesh=99; state.objectScale={NAN,0,-2}; state.objectYaw=NAN; state.constrain();
        require(state.mesh==2 && state.objectScale[0]==1.5f && state.objectScale[1]==.2f && state.objectScale[2]==.2f && state.objectYaw==0,
            "Inspector must not submit singular/nonfinite transforms or an invalid mesh");
        std::cout << "PASS: outward nondegenerate cube/solid/sphere, radial sphere normals, affine normal perpendicularity, translation, mirrored math, invalid transforms, independent baked normals, inspector bounds\n";
        return 0;
    } catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}

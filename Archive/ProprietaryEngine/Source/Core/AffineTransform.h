#pragma once
#include <array>
#include <cmath>
#include <limits>
#include <stdexcept>
namespace engine {
// Column-major, column vectors, right-handed coordinates; translation is 12..14.
// Derived math uses double. Render inputs must remain finite float-representable values.
using AffineMatrix=std::array<double,16>;
inline AffineMatrix identityAffine(){return {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};}
inline AffineMatrix multiplyAffine(const AffineMatrix& a,const AffineMatrix& b){
    AffineMatrix result{};
    for(size_t c=0;c<4;++c)for(size_t r=0;r<4;++r)for(size_t k=0;k<4;++k)result[c*4+r]+=a[k*4+r]*b[c*4+k];
    return result;
}
inline std::array<double,3> affinePoint(const AffineMatrix& m,std::array<double,3> p){
    return {m[0]*p[0]+m[4]*p[1]+m[8]*p[2]+m[12],m[1]*p[0]+m[5]*p[1]+m[9]*p[2]+m[13],m[2]*p[0]+m[6]*p[1]+m[10]*p[2]+m[14]};
}
inline double affineDeterminant(const AffineMatrix& m){
    return m[0]*(m[5]*m[10]-m[9]*m[6])-m[4]*(m[1]*m[10]-m[9]*m[2])+m[8]*(m[1]*m[6]-m[5]*m[2]);
}
inline AffineMatrix inverseAffine(const AffineMatrix& m){
    const double det=affineDeterminant(m);
    const double volume=std::hypot(m[0],m[1],m[2])*std::hypot(m[4],m[5],m[6])*std::hypot(m[8],m[9],m[10]);
    if(!std::isfinite(det)||!std::isfinite(volume)||volume==0||std::abs(det)<=volume*1e-12)
        throw std::invalid_argument("Singular or ill-conditioned affine transform");
    auto out=identityAffine();
    out[0]=(m[5]*m[10]-m[9]*m[6])/det;out[4]=(m[8]*m[6]-m[4]*m[10])/det;out[8]=(m[4]*m[9]-m[8]*m[5])/det;
    out[1]=(m[9]*m[2]-m[1]*m[10])/det;out[5]=(m[0]*m[10]-m[8]*m[2])/det;out[9]=(m[8]*m[1]-m[0]*m[9])/det;
    out[2]=(m[1]*m[6]-m[5]*m[2])/det;out[6]=(m[4]*m[2]-m[0]*m[6])/det;out[10]=(m[0]*m[5]-m[4]*m[1])/det;
    const auto p=affinePoint(out,{-m[12],-m[13],-m[14]});for(size_t i=0;i<3;++i)out[12+i]=p[i];return out;
}
inline AffineMatrix normalAffine(const AffineMatrix& m){
    const auto inverse=inverseAffine(m);auto out=identityAffine();
    for(size_t c=0;c<3;++c)for(size_t r=0;r<3;++r)out[c*4+r]=inverse[r*4+c];return out;
}
inline void validateRenderAffine(const AffineMatrix& m){
    if(m[3]!=0||m[7]!=0||m[11]!=0||m[15]!=1)throw std::invalid_argument("Expected affine matrix");
    for(double x:m)if(!std::isfinite(x)||std::abs(x)>std::numeric_limits<float>::max())throw std::invalid_argument("Affine transform exceeds finite renderer range");
    for(double x:normalAffine(m))if(!std::isfinite(x)||std::abs(x)>std::numeric_limits<float>::max())throw std::invalid_argument("Affine normal transform exceeds finite renderer range");
}
}

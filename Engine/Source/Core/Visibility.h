#pragma once
#include <array>
#include <cmath>
namespace engine {
inline bool visibleSphere(const float* matrix,std::array<float,3> center,float radius,bool homogeneousDepth){
    // Column-major view-projection; normalized planes are unnecessary for a sphere test.
    for(int plane=0;plane<6;++plane){const int axis=plane/2;const float sign=plane%2?1.f:-1.f;std::array<float,4> p{};
        for(int i=0;i<4;++i)p[i]=(plane==5&&!homogeneousDepth)?matrix[i*4+2]:matrix[i*4+3]+sign*matrix[i*4+axis];
        const float length=std::sqrt(p[0]*p[0]+p[1]*p[1]+p[2]*p[2]);
        if(p[0]*center[0]+p[1]*center[1]+p[2]*center[2]+p[3]<-radius*length)return false;
    }return true;
}
}

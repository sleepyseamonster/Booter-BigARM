#include "World/Terrain.h"
#include <cmath>
namespace engine {
ModelData terrainRenderLod(const TerrainPatch& patch,uint32_t level){
    if(level>2)throw std::invalid_argument("Terrain LOD must be 0, 1 or 2");
    const auto row=uint32_t(std::sqrt(patch.mesh.vertices.size()));
    if(row*row!=patch.mesh.vertices.size()||row<3)throw std::invalid_argument("Terrain LOD requires a regular sample grid");
    const uint32_t cells=row-1,stride=1u<<level,n=cells/stride,r=n+1;
    if(cells%stride)throw std::invalid_argument("Terrain grid does not divide into this LOD");
    ModelData mesh=patch.mesh;mesh.vertices.clear();mesh.indices.clear();
    for(uint32_t z=0;z<=cells;z+=stride)for(uint32_t x=0;x<=cells;x+=stride)mesh.vertices.push_back(patch.mesh.vertices[z*row+x]);
    for(uint32_t z=0;z<n;++z)for(uint32_t x=0;x<n;++x){const uint32_t a=z*r+x,b=a+1,c=a+r,d=c+1;mesh.indices.insert(mesh.indices.end(),{a,c,b,b,c,d});}
    // All LODs have skirts. They cover mixed-resolution edge gaps, with no collider
    // beneath the real surface. Depth bounds the full possible difference in this patch.
    float low=mesh.vertices[0].position[1],high=low;
    for(const auto& v:patch.mesh.vertices){low=std::min(low,v.position[1]);high=std::max(high,v.position[1]);}
    const float depth=high-low+1;
    auto skirt=[&](uint32_t a,uint32_t b){
        auto va=mesh.vertices[a],vb=mesh.vertices[b];va.position[1]-=depth;vb.position[1]-=depth;
        const uint32_t c=uint32_t(mesh.vertices.size());mesh.vertices.push_back(va);mesh.vertices.push_back(vb);
        mesh.indices.insert(mesh.indices.end(),{a,c,b,b,c,c+1});
    };
    for(uint32_t i=0;i<n;++i){skirt(i+1,i);skirt(n*r+i,n*r+i+1);skirt(i*r,(i+1)*r);skirt((i+1)*r+n,i*r+n);}
    validateModel(mesh);return mesh;
}
}

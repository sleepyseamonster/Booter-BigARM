#pragma once
#include "Core/WorldIdentity.h"
#include "Persistence/Document.h"
#include <cstdint>
#include <filesystem>
#include <string>
#include <vector>

namespace engine {
struct TerrainGridContract {
    uint32_t tileMeters=256;
    uint32_t tilesPerSupertile=4;
    uint32_t haloMeters=64;
    bool operator==(const TerrainGridContract&)const=default;
};
struct TerrainTemporalContract {
    uint32_t ancientFormationVersion=1;
    uint32_t dryAgeVersion=1;
    uint64_t liquidWaterCutoffYears=10000;
    uint64_t dryAgeYears=10000;
    bool activeNaturalWater=false;
    bool operator==(const TerrainTemporalContract&)const=default;
};
struct TerrainRevisionSet {
    uint64_t manifest=1;
    uint64_t constraints=1;
    uint64_t sourceFields=1;
    uint64_t stormForcing=1;
    bool operator==(const TerrainRevisionSet&)const=default;
};
enum class TerrainConstraintOperator : uint8_t { Add,Minimum,Maximum,Replace,Protect };
struct TerrainConstraintLayerRef {
    std::string id;
    uint64_t revision=1;
    uint32_t priority=0;
    TerrainConstraintOperator composition=TerrainConstraintOperator::Add;
    bool operator==(const TerrainConstraintLayerRef&)const=default;
};
struct WorldTerrainManifest {
    std::string dataset="technical-terrain-corpus";
    uint64_t worldSeed=1049;
    uint32_t generatorVersion=1;
    TerrainGridContract grid;
    TerrainTemporalContract temporal;
    TerrainRevisionSet revisions;
    std::vector<TerrainConstraintLayerRef> constraintLayers;
    bool operator==(const WorldTerrainManifest&)const=default;
};
void validateTerrainManifest(const WorldTerrainManifest&);
void saveTerrainManifest(const std::filesystem::path&,const WorldTerrainManifest&);
WorldTerrainManifest loadTerrainManifest(const std::filesystem::path&);
std::vector<TerrainConstraintLayerRef> orderedTerrainConstraints(const WorldTerrainManifest&);

struct TerrainTileAddress {int64_t x=0,z=0;uint8_t level=0;bool operator==(const TerrainTileAddress&)const=default;};
TerrainTileAddress terrainSupertile(TerrainTileAddress,uint32_t tilesPerSupertile);

enum class TerrainProduct : uint8_t {
    SourceFields,SurfaceQuery,RenderMesh,Collision,Navigation,Materials,Population,FeatureSurfaces,StormResponse
};
enum class TerrainDependencyChange : uint8_t { Manifest,Constraints,SourceFields,StormForcing };
struct TerrainProductRevision {
    TerrainProduct product=TerrainProduct::SourceFields;
    uint64_t manifest=0,constraints=0,sourceFields=0,stormForcing=0;
    bool operator==(const TerrainProductRevision&)const=default;
};
TerrainProductRevision terrainProductRevision(const WorldTerrainManifest&,TerrainProduct);
std::vector<TerrainProduct> terrainInvalidation(TerrainDependencyChange);
GeneratedId terrainFeatureId(const WorldTerrainManifest&,Region,std::string featureNamespace,uint64_t member);

enum class TerrainSurfaceClass : uint8_t { Bedrock,Shale,Sediment,Talus,Gravel,Sand };
enum class TerrainTraversal : uint32_t {
    None=0,StableSupport=1u<<0,Walk=1u<<1,VaultEdge=1u<<2,Climb=1u<<3,Slide=1u<<4,Loose=1u<<5,Blocked=1u<<6
};
constexpr TerrainTraversal operator|(TerrainTraversal a,TerrainTraversal b){return TerrainTraversal(uint32_t(a)|uint32_t(b));}
constexpr bool hasTraversal(TerrainTraversal value,TerrainTraversal flag){return (uint32_t(value)&uint32_t(flag))!=0;}
struct TerrainSurfaceSemantics {
    TerrainProductRevision revision;
    TerrainSurfaceClass surface=TerrainSurfaceClass::Bedrock;
    uint16_t lithology=0,stratum=0;
    float sedimentDepth=0,talusDepth=0,slopeDegrees=0,curvature=0,support=1,friction=.8f;
    TerrainTraversal traversal=TerrainTraversal::StableSupport|TerrainTraversal::Walk;
};
void validateTerrainSurface(const TerrainSurfaceSemantics&);

struct TerrainCorpusCase {
    std::string id;
    Region address;
    std::string silhouette;
    std::vector<TerrainSurfaceClass> surfaces;
    TerrainTraversal traversal=TerrainTraversal::None;
    bool featureSurface=false;
    bool operator==(const TerrainCorpusCase&)const=default;
};
struct TerrainCorpus {
    bool technicalOnly=true;
    std::vector<TerrainCorpusCase> cases;
    bool operator==(const TerrainCorpus&)const=default;
};
void validateTerrainCorpus(const TerrainCorpus&);
void saveTerrainCorpus(const std::filesystem::path&,const TerrainCorpus&);
TerrainCorpus loadTerrainCorpus(const std::filesystem::path&);

enum class TerrainChangeKind : uint8_t { Rockfall,SedimentMove,StrikeScar,AuthoredCollapse };
struct TerrainChangeDelta {
    GeneratedId event;
    GeneratedId target;
    TerrainChangeKind kind=TerrainChangeKind::Rockfall;
    uint64_t baseGeneratorRevision=0;
    Region region;
    bool accepted=false;
};
void validateTerrainChange(const TerrainChangeDelta&);
}

#include "World/TerrainManifest.h"
#include "World/Terrain.h"
#include <algorithm>
#include <filesystem>
#include <iostream>
#include <set>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
template<class F>void rejects(F&& action){bool rejected=false;try{action();}catch(const std::exception&){rejected=true;}require(rejected,"Expected terrain contract rejection");}
}
int main(int argc,char** argv)try{
    if(argc!=4)throw std::runtime_error("Usage: engine_terrain_manifest_tests manifest corpus output-directory");
    const std::filesystem::path manifestPath=argv[1],corpusPath=argv[2],output=argv[3];std::filesystem::create_directories(output);
    const auto manifest=loadTerrainManifest(manifestPath);const auto corpus=loadTerrainCorpus(corpusPath);
    require(manifest.temporal.liquidWaterCutoffYears==10000&&manifest.temporal.dryAgeYears==10000&&!manifest.temporal.activeNaturalWater,"Waterless temporal contract changed");
    require(manifest.revisions.stormForcing>0&&readDocument(manifestPath,"engine.world-terrain-manifest").dump().find("resonance")==std::string::npos,"Storm seam introduced resonance state");
    saveTerrainManifest(output/"manifest.json",manifest);require(loadTerrainManifest(output/"manifest.json")==manifest,"Terrain manifest roundtrip");
    saveTerrainCorpus(output/"corpus.json",corpus);require(loadTerrainCorpus(output/"corpus.json")==corpus,"Terrain corpus roundtrip");

    const auto ordered=orderedTerrainConstraints(manifest);require(ordered.size()==2&&ordered[0].id=="technical_routes"&&ordered[1].id=="technical_silhouette_guides","Constraint priority composition changed");
    auto reverseManifest=manifest;std::reverse(reverseManifest.constraintLayers.begin(),reverseManifest.constraintLayers.end());require(orderedTerrainConstraints(reverseManifest)==ordered,"Constraint composition depends on file order");
    auto duplicateLayer=manifest;duplicateLayer.constraintLayers.push_back(manifest.constraintLayers.front());rejects([&]{validateTerrainManifest(duplicateLayer);});
    auto invalidLayer=manifest;invalidLayer.constraintLayers.front().composition=TerrainConstraintOperator(255);rejects([&]{validateTerrainManifest(invalidLayer);});
    require(terrainSupertile({-1,-4,0},4)==TerrainTileAddress{-1,-1,1}&&terrainSupertile({4,3,0},4)==TerrainTileAddress{1,0,1},"Signed terrain supertile addressing is not floor-stable");

    const Region far{4000000000000LL,-4000000000000LL};
    const auto feature=terrainFeatureId(manifest,far,"strata",17),repeat=terrainFeatureId(manifest,far,"strata",17),other=terrainFeatureId(manifest,far,"talus",17);
    require(feature==repeat&&feature!=other&&feature.text()=="generated:v1:terrain-strata:1049:1:4000000000000:-4000000000000:17","Large-coordinate terrain feature identity");
    require(terrainInvalidation(TerrainDependencyChange::StormForcing)==std::vector<TerrainProduct>{TerrainProduct::StormResponse},"Storm revision invalidated immutable base terrain");
    for(auto change:{TerrainDependencyChange::Manifest,TerrainDependencyChange::Constraints,TerrainDependencyChange::SourceFields})require(terrainInvalidation(change).size()==9,"Terrain dependency change missed derived products");
    const auto surfaceRevision=terrainProductRevision(manifest,TerrainProduct::SurfaceQuery),stormRevision=terrainProductRevision(manifest,TerrainProduct::StormResponse);
    require(surfaceRevision.stormForcing==0&&stormRevision.stormForcing==manifest.revisions.stormForcing,"Product revision fence mixed storm and base state");

    TerrainSurfaceSemantics ledge;ledge.revision=surfaceRevision;ledge.surface=TerrainSurfaceClass::Shale;ledge.lithology=2;ledge.stratum=7;ledge.slopeDegrees=38;ledge.support=.9f;ledge.friction=.75f;ledge.traversal=TerrainTraversal::StableSupport|TerrainTraversal::Walk|TerrainTraversal::VaultEdge|TerrainTraversal::Climb;validateTerrainSurface(ledge);
    auto contradictory=ledge;contradictory.traversal=TerrainTraversal::Walk|TerrainTraversal::Blocked;rejects([&]{validateTerrainSurface(contradictory);});

    TerrainTraversal interactions=TerrainTraversal::None;std::set<TerrainSurfaceClass> surfaces;size_t featureCases=0;
    for(const auto& item:corpus.cases){interactions=interactions|item.traversal;surfaces.insert(item.surfaces.begin(),item.surfaces.end());featureCases+=item.featureSurface;}
    require(corpus.technicalOnly&&corpus.cases.size()==6&&featureCases>=2,"Representative corpus scope changed");
    require(surfaces.size()==6&&hasTraversal(interactions,TerrainTraversal::Walk)&&hasTraversal(interactions,TerrainTraversal::VaultEdge)&&hasTraversal(interactions,TerrainTraversal::Climb)&&hasTraversal(interactions,TerrainTraversal::Slide)&&hasTraversal(interactions,TerrainTraversal::Loose)&&hasTraversal(interactions,TerrainTraversal::Blocked),"Corpus lost required surface or interaction semantics");

    TerrainChangeDelta delta{{manifest.worldSeed,manifest.generatorVersion,far,3,"terrain-event"},feature,TerrainChangeKind::Rockfall,manifest.revisions.sourceFields,far,true};validateTerrainChange(delta);
    auto wrong=delta;wrong.target.region.x++;rejects([&]{validateTerrainChange(wrong);});

    for(uint32_t version=1;version<=3;++version){TerrainRecipe legacy;legacy.version=version;saveTerrainRecipe(output/("legacy-v"+std::to_string(version)+".json"),legacy);require(loadTerrainRecipe(output/("legacy-v"+std::to_string(version)+".json"))==legacy,"Legacy terrain recipe compatibility changed");}
    const auto payload=readDocument(manifestPath,"engine.world-terrain-manifest");writeDocument(output/"future.json","engine.world-terrain-manifest",payload,2);rejects([&]{loadTerrainManifest(output/"future.json");});
    auto malformed=payload;malformed["revisions"]["manifest"]=-1;writeDocument(output/"malformed.json","engine.world-terrain-manifest",malformed);rejects([&]{loadTerrainManifest(output/"malformed.json");});
    rejects([&]{terrainProductRevision(manifest,TerrainProduct(255));});rejects([&]{terrainInvalidation(TerrainDependencyChange(255));});
    auto active=manifest;active.temporal.activeNaturalWater=true;rejects([&]{validateTerrainManifest(active);});
    auto permanent=corpus;permanent.technicalOnly=false;rejects([&]{validateTerrainCorpus(permanent);});

    writeDocument(output/"result.json","engine.terrain-contract-result",{{"passed",true},{"cases",corpus.cases.size()},{"surface_classes",surfaces.size()},{"feature_domains",featureCases},{"constraint_layers",ordered.size()},{"negative_supertile",{-1,-1}},{"large_address",{far.x,far.z}},{"legacy_versions",3},{"products",9},{"proof_limit","Contracts, identities, revisions and technical corpus only; no production terrain generation, final movement thresholds, visual acceptance or Windows proof."}});
    std::cout<<"PASS: strict terrain manifest/corpus, deterministic constraints and signed addressing, revision invalidation, waterless phases, stable large-address features, traversal semantics, sparse deltas and legacy v1-v3 compatibility\n";return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

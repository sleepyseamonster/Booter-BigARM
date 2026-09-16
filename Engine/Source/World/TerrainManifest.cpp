#include "World/TerrainManifest.h"
#include <algorithm>
#include <cmath>
#include <set>
#include <stdexcept>

namespace engine {
namespace {
void positive(uint64_t value,const char* name){if(!value)throw std::invalid_argument(std::string(name)+" must be positive");}
void bounded(uint32_t value,uint32_t low,uint32_t high,const char* name){if(value<low||value>high)throw std::invalid_argument(std::string(name)+" outside supported range");}
void identifier(const std::string& value,const char* name){
    if(value.empty()||value.size()>64||value.find_first_not_of("abcdefghijklmnopqrstuvwxyz0123456789_-")!=std::string::npos)
        throw std::invalid_argument(std::string("Invalid ")+name);
}
const char* constraintName(TerrainConstraintOperator value){switch(value){case TerrainConstraintOperator::Add:return "add";case TerrainConstraintOperator::Minimum:return "minimum";case TerrainConstraintOperator::Maximum:return "maximum";case TerrainConstraintOperator::Replace:return "replace";case TerrainConstraintOperator::Protect:return "protect";}throw std::invalid_argument("Unknown terrain constraint operator");}
TerrainConstraintOperator constraintOperator(const Json& value){
    if(!value.is_string())throw std::runtime_error("Terrain constraint operator must be a string");const auto name=value.get<std::string>();
    if(name=="add")return TerrainConstraintOperator::Add;if(name=="minimum")return TerrainConstraintOperator::Minimum;if(name=="maximum")return TerrainConstraintOperator::Maximum;if(name=="replace")return TerrainConstraintOperator::Replace;if(name=="protect")return TerrainConstraintOperator::Protect;
    throw std::runtime_error("Unknown terrain constraint operator");
}
const char* surfaceName(TerrainSurfaceClass value){switch(value){case TerrainSurfaceClass::Bedrock:return "bedrock";case TerrainSurfaceClass::Shale:return "shale";case TerrainSurfaceClass::Sediment:return "sediment";case TerrainSurfaceClass::Talus:return "talus";case TerrainSurfaceClass::Gravel:return "gravel";case TerrainSurfaceClass::Sand:return "sand";}throw std::invalid_argument("Unknown terrain surface class");}
TerrainSurfaceClass surface(const Json& value){
    if(!value.is_string())throw std::runtime_error("Terrain surface name must be a string");const auto name=value.get<std::string>();
    if(name=="bedrock")return TerrainSurfaceClass::Bedrock;if(name=="shale")return TerrainSurfaceClass::Shale;
    if(name=="sediment")return TerrainSurfaceClass::Sediment;if(name=="talus")return TerrainSurfaceClass::Talus;
    if(name=="gravel")return TerrainSurfaceClass::Gravel;if(name=="sand")return TerrainSurfaceClass::Sand;
    throw std::runtime_error("Unknown terrain surface class");
}
struct TraversalName{TerrainTraversal value;const char* name;};
constexpr TraversalName traversalNames[]={
    {TerrainTraversal::StableSupport,"stable_support"},{TerrainTraversal::Walk,"walk"},{TerrainTraversal::VaultEdge,"vault_edge"},
    {TerrainTraversal::Climb,"climb"},{TerrainTraversal::Slide,"slide"},{TerrainTraversal::Loose,"loose"},{TerrainTraversal::Blocked,"blocked"}
};
Json traversal(TerrainTraversal flags){Json result=Json::array();for(const auto& item:traversalNames)if(hasTraversal(flags,item.value))result.push_back(item.name);return result;}
TerrainTraversal traversal(const Json& list){
    if(!list.is_array()||list.size()>std::size(traversalNames))throw std::runtime_error("Invalid terrain traversal list");
    TerrainTraversal result=TerrainTraversal::None;std::set<std::string> seen;
    for(const auto& item:list){if(!item.is_string())throw std::runtime_error("Traversal name must be a string");const auto name=item.get<std::string>();if(!seen.insert(name).second)throw std::runtime_error("Duplicate traversal name");
        const auto found=std::find_if(std::begin(traversalNames),std::end(traversalNames),[&](const auto& candidate){return name==candidate.name;});if(found==std::end(traversalNames))throw std::runtime_error("Unknown terrain traversal name");result=result|found->value;}
    return result;
}
Region region(const Json& value){if(!value.is_array()||value.size()!=2)throw std::runtime_error("Terrain corpus address must have two integers");for(const auto& axis:value)if(!axis.is_number_integer())throw std::runtime_error("Terrain corpus address must use signed integers");return {value[0].get<int64_t>(),value[1].get<int64_t>()};}
Json encode(const WorldTerrainManifest& value){
    Json layers=Json::array();for(const auto& layer:value.constraintLayers)layers.push_back({{"id",layer.id},{"revision",layer.revision},{"priority",layer.priority},{"composition",constraintName(layer.composition)}});
    return {{"dataset",value.dataset},{"world_seed",value.worldSeed},{"generator_version",value.generatorVersion},
        {"grid",{{"tile_meters",value.grid.tileMeters},{"tiles_per_supertile",value.grid.tilesPerSupertile},{"halo_meters",value.grid.haloMeters}}},
        {"temporal",{{"ancient_formation_version",value.temporal.ancientFormationVersion},{"dry_age_version",value.temporal.dryAgeVersion},
            {"liquid_water_cutoff_years",value.temporal.liquidWaterCutoffYears},{"dry_age_years",value.temporal.dryAgeYears},{"active_natural_water",value.temporal.activeNaturalWater}}},
        {"revisions",{{"manifest",value.revisions.manifest},{"constraints",value.revisions.constraints},{"source_fields",value.revisions.sourceFields},{"storm_forcing",value.revisions.stormForcing}}},
        {"constraint_layers",layers}};
}
int64_t floorDivide(int64_t value,uint32_t divisor){const auto d=int64_t(divisor);auto quotient=value/d;const auto remainder=value%d;if(remainder<0)--quotient;return quotient;}
}
void validateTerrainManifest(const WorldTerrainManifest& value){
    identifier(value.dataset,"terrain dataset ID");bounded(value.generatorVersion,1,UINT16_MAX,"Terrain generator version");
    bounded(value.grid.tileMeters,32,4096,"Terrain tile size");bounded(value.grid.tilesPerSupertile,1,32,"Terrain supertile span");bounded(value.grid.haloMeters,1,value.grid.tileMeters,"Terrain halo");
    if(value.grid.tileMeters%value.grid.haloMeters)throw std::invalid_argument("Terrain halo must divide tile size");
    bounded(value.temporal.ancientFormationVersion,1,UINT16_MAX,"Ancient formation version");bounded(value.temporal.dryAgeVersion,1,UINT16_MAX,"Dry-age version");
    if(value.temporal.activeNaturalWater)throw std::invalid_argument("Current terrain contract cannot publish active natural water");
    if(value.temporal.liquidWaterCutoffYears<1000||value.temporal.liquidWaterCutoffYears>1000000||value.temporal.dryAgeYears<1000||value.temporal.dryAgeYears>1000000)
        throw std::invalid_argument("Terrain temporal range is unsupported");
    positive(value.revisions.manifest,"Manifest revision");positive(value.revisions.constraints,"Constraint revision");positive(value.revisions.sourceFields,"Source-field revision");positive(value.revisions.stormForcing,"Storm-forcing revision");
    if(value.constraintLayers.size()>64)throw std::invalid_argument("Too many terrain constraint layers");
    std::set<std::string> layerIds;for(const auto& layer:value.constraintLayers){identifier(layer.id,"terrain constraint layer ID");positive(layer.revision,"Terrain constraint layer revision");bounded(layer.priority,0,1000000,"Terrain constraint layer priority");(void)constraintName(layer.composition);if(!layerIds.insert(layer.id).second)throw std::invalid_argument("Duplicate terrain constraint layer ID");}
}
void saveTerrainManifest(const std::filesystem::path& file,const WorldTerrainManifest& value){validateTerrainManifest(value);writeDocument(file,"engine.world-terrain-manifest",encode(value));}
WorldTerrainManifest loadTerrainManifest(const std::filesystem::path& file){
    const auto p=readDocument(file,"engine.world-terrain-manifest");if(p.size()!=7)throw std::runtime_error("Invalid terrain manifest fields");
    const auto& grid=p.at("grid");const auto& temporal=p.at("temporal");const auto& revisions=p.at("revisions");
    if(grid.size()!=3||temporal.size()!=5||revisions.size()!=4)throw std::runtime_error("Invalid terrain manifest section fields");
    for(const auto* value:{&p.at("world_seed"),&p.at("generator_version"),&grid.at("tile_meters"),&grid.at("tiles_per_supertile"),&grid.at("halo_meters"),&temporal.at("ancient_formation_version"),&temporal.at("dry_age_version"),&temporal.at("liquid_water_cutoff_years"),&temporal.at("dry_age_years"),&revisions.at("manifest"),&revisions.at("constraints"),&revisions.at("source_fields"),&revisions.at("storm_forcing")})if(!value->is_number_unsigned())throw std::runtime_error("Terrain manifest integers must be unsigned");
    if(!p.at("dataset").is_string()||!temporal.at("active_natural_water").is_boolean()||!p.at("constraint_layers").is_array())throw std::runtime_error("Invalid terrain manifest value type");
    WorldTerrainManifest value;value.dataset=p.at("dataset").get<std::string>();value.worldSeed=p.at("world_seed").get<uint64_t>();value.generatorVersion=p.at("generator_version").get<uint32_t>();
    value.grid={grid.at("tile_meters").get<uint32_t>(),grid.at("tiles_per_supertile").get<uint32_t>(),grid.at("halo_meters").get<uint32_t>()};
    value.temporal={temporal.at("ancient_formation_version").get<uint32_t>(),temporal.at("dry_age_version").get<uint32_t>(),temporal.at("liquid_water_cutoff_years").get<uint64_t>(),temporal.at("dry_age_years").get<uint64_t>(),temporal.at("active_natural_water").get<bool>()};
    value.revisions={revisions.at("manifest").get<uint64_t>(),revisions.at("constraints").get<uint64_t>(),revisions.at("source_fields").get<uint64_t>(),revisions.at("storm_forcing").get<uint64_t>()};
    for(const auto& row:p.at("constraint_layers")){if(!row.is_object()||row.size()!=4||!row.at("id").is_string()||!row.at("revision").is_number_unsigned()||!row.at("priority").is_number_unsigned())throw std::runtime_error("Invalid terrain constraint layer fields");value.constraintLayers.push_back({row.at("id").get<std::string>(),row.at("revision").get<uint64_t>(),row.at("priority").get<uint32_t>(),constraintOperator(row.at("composition"))});}
    validateTerrainManifest(value);return value;
}
std::vector<TerrainConstraintLayerRef> orderedTerrainConstraints(const WorldTerrainManifest& manifest){validateTerrainManifest(manifest);auto result=manifest.constraintLayers;std::sort(result.begin(),result.end(),[](const auto& a,const auto& b){return a.priority!=b.priority?a.priority<b.priority:a.id<b.id;});return result;}
TerrainTileAddress terrainSupertile(TerrainTileAddress tile,uint32_t tilesPerSupertile){bounded(tilesPerSupertile,1,32,"Terrain supertile span");if(tile.level==UINT8_MAX)throw std::invalid_argument("Terrain tile level overflow");return {floorDivide(tile.x,tilesPerSupertile),floorDivide(tile.z,tilesPerSupertile),uint8_t(tile.level+1)};}
TerrainProductRevision terrainProductRevision(const WorldTerrainManifest& manifest,TerrainProduct product){
    validateTerrainManifest(manifest);switch(product){case TerrainProduct::SourceFields:case TerrainProduct::SurfaceQuery:case TerrainProduct::RenderMesh:case TerrainProduct::Collision:case TerrainProduct::Navigation:case TerrainProduct::Materials:case TerrainProduct::Population:case TerrainProduct::FeatureSurfaces:case TerrainProduct::StormResponse:break;default:throw std::invalid_argument("Unknown terrain product");}
    return {product,manifest.revisions.manifest,manifest.revisions.constraints,manifest.revisions.sourceFields,product==TerrainProduct::StormResponse?manifest.revisions.stormForcing:0};
}
std::vector<TerrainProduct> terrainInvalidation(TerrainDependencyChange change){
    if(change==TerrainDependencyChange::StormForcing)return {TerrainProduct::StormResponse};
    if(change!=TerrainDependencyChange::Manifest&&change!=TerrainDependencyChange::Constraints&&change!=TerrainDependencyChange::SourceFields)throw std::invalid_argument("Unknown terrain dependency change");
    return {TerrainProduct::SourceFields,TerrainProduct::SurfaceQuery,TerrainProduct::RenderMesh,TerrainProduct::Collision,TerrainProduct::Navigation,TerrainProduct::Materials,TerrainProduct::Population,TerrainProduct::FeatureSurfaces,TerrainProduct::StormResponse};
}
GeneratedId terrainFeatureId(const WorldTerrainManifest& manifest,Region address,std::string featureNamespace,uint64_t member){validateTerrainManifest(manifest);identifier(featureNamespace,"terrain feature namespace");return {manifest.worldSeed,manifest.generatorVersion,address,member,"terrain-"+featureNamespace};}
void validateTerrainSurface(const TerrainSurfaceSemantics& value){
    (void)surfaceName(value.surface);
    if(value.revision.product!=TerrainProduct::SurfaceQuery||!value.revision.manifest||!value.revision.constraints||!value.revision.sourceFields||value.revision.stormForcing)
        throw std::invalid_argument("Terrain surface needs a source-revision-fenced surface-query stamp");
    const uint32_t flags=uint32_t(value.traversal);if(flags&~uint32_t(TerrainTraversal::StableSupport|TerrainTraversal::Walk|TerrainTraversal::VaultEdge|TerrainTraversal::Climb|TerrainTraversal::Slide|TerrainTraversal::Loose|TerrainTraversal::Blocked))throw std::invalid_argument("Unknown terrain traversal flags");
    for(float scalar:{value.sedimentDepth,value.talusDepth,value.slopeDegrees,value.curvature,value.support,value.friction})if(!std::isfinite(scalar))throw std::invalid_argument("Nonfinite terrain surface value");
    if(value.sedimentDepth<0||value.talusDepth<0||value.slopeDegrees<0||value.slopeDegrees>90||value.support<0||value.support>1||value.friction<0||value.friction>4)throw std::invalid_argument("Terrain surface value outside supported range");
    if(hasTraversal(value.traversal,TerrainTraversal::Walk)&&hasTraversal(value.traversal,TerrainTraversal::Blocked))throw std::invalid_argument("Terrain cannot be walkable and blocked");
}
void validateTerrainCorpus(const TerrainCorpus& corpus){
    if(!corpus.technicalOnly)throw std::invalid_argument("Representative terrain corpus cannot declare permanent world content");
    if(corpus.cases.size()<5||corpus.cases.size()>32)throw std::invalid_argument("Representative terrain corpus requires 5 to 32 cases");
    std::set<std::string> ids;for(const auto& item:corpus.cases){identifier(item.id,"terrain corpus case ID");if(!ids.insert(item.id).second||item.silhouette.empty()||item.silhouette.size()>128||item.surfaces.empty()||item.surfaces.size()>8)throw std::invalid_argument("Invalid terrain corpus case");
        std::set<TerrainSurfaceClass> surfaces;for(auto value:item.surfaces){(void)surfaceName(value);if(!surfaces.insert(value).second)throw std::invalid_argument("Duplicate corpus surface class");}
        if(item.traversal==TerrainTraversal::None)throw std::invalid_argument("Corpus case needs traversal semantics");}
}
void saveTerrainCorpus(const std::filesystem::path& file,const TerrainCorpus& corpus){
    validateTerrainCorpus(corpus);Json cases=Json::array();for(const auto& item:corpus.cases){Json surfaces=Json::array();for(auto value:item.surfaces)surfaces.push_back(surfaceName(value));cases.push_back({{"id",item.id},{"address",{item.address.x,item.address.z}},{"silhouette",item.silhouette},{"surfaces",surfaces},{"traversal",traversal(item.traversal)},{"feature_surface",item.featureSurface}});}writeDocument(file,"engine.terrain-corpus",{{"technical_only",corpus.technicalOnly},{"cases",cases}});
}
TerrainCorpus loadTerrainCorpus(const std::filesystem::path& file){
    const auto p=readDocument(file,"engine.terrain-corpus");if(p.size()!=2||!p.at("technical_only").is_boolean()||!p.at("cases").is_array())throw std::runtime_error("Invalid terrain corpus document");TerrainCorpus corpus;corpus.technicalOnly=p.at("technical_only").get<bool>();
    for(const auto& row:p.at("cases")){if(!row.is_object()||row.size()!=6||!row.at("surfaces").is_array())throw std::runtime_error("Invalid terrain corpus case fields");TerrainCorpusCase item;item.id=row.at("id").get<std::string>();item.address=region(row.at("address"));item.silhouette=row.at("silhouette").get<std::string>();for(const auto& value:row.at("surfaces"))item.surfaces.push_back(surface(value));item.traversal=traversal(row.at("traversal"));item.featureSurface=row.at("feature_surface").get<bool>();corpus.cases.push_back(std::move(item));}
    validateTerrainCorpus(corpus);return corpus;
}
void validateTerrainChange(const TerrainChangeDelta& value){
    switch(value.kind){case TerrainChangeKind::Rockfall:case TerrainChangeKind::SedimentMove:case TerrainChangeKind::StrikeScar:case TerrainChangeKind::AuthoredCollapse:break;default:throw std::invalid_argument("Unknown terrain change kind");}
    if(value.event.generator!="terrain-event"||value.event.region!=value.region||value.target.region!=value.region||!value.baseGeneratorRevision)throw std::invalid_argument("Invalid terrain change identity or revision");
    value.event.text();value.target.text();
}
}

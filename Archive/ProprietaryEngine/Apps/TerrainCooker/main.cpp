#include "World/Terrain.h"
#include "Core/BoundedJobs.h"
#include <charconv>
#include <chrono>
#include <iostream>
namespace {int64_t integer(const char* text){int64_t result;std::string s=text;const auto p=std::from_chars(s.data(),s.data()+s.size(),result);if(p.ec!=std::errc{}||p.ptr!=s.data()+s.size())throw std::runtime_error("Invalid region coordinate");return result;}}
int main(int argc,char** argv)try{
    if(argc!=7)throw std::runtime_error("Usage: engine_terrain_cook terrain-recipe rock-recipe constraints new-output-directory region-x region-z");
    const auto recipe=engine::loadTerrainRecipe(argv[1]);const auto rock=engine::loadRockRecipe(argv[2]);const auto constraints=engine::loadConstraints(argv[3]);const engine::Region region{integer(argv[5]),integer(argv[6])};
    engine::BoundedJobs<engine::TerrainPatch> jobs([](const auto& p){return p.bytes();});
    jobs.submit("terrain-cook",1,2*1024*1024,[=](const auto& token){return engine::generateTerrain(recipe,region,rock,constraints,[&]{return token.cancelled();});});
    engine::UploadAdmission admission;std::optional<engine::BoundedJobs<engine::TerrainPatch>::Completion> result;
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(30);
    while(!(result=jobs.takeReady(admission))){if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("Terrain cook timed out");std::this_thread::sleep_for(std::chrono::milliseconds(1));}
    if(!result->error.empty())throw std::runtime_error(result->error);const auto& patch=*result->value;
    const std::filesystem::path out=argv[4];engine::saveModel(out,patch.mesh);engine::saveTerrainRecipe(out/"terrain-recipe.json",recipe);engine::saveRockRecipe(out/"rock-recipe.json",rock);engine::saveConstraints(out/"constraints.json",constraints);
    engine::Json rocks=engine::Json::array(),anchors=engine::Json::array(),routes=engine::Json::array();
    for(const auto& r:patch.rocks)rocks.push_back({{"id",r.id.text()},{"member",r.id.member},{"local",r.position.local},{"yaw",r.yaw},{"footprint",r.footprint}});
    for(const auto& a:patch.landmarks)anchors.push_back({{"id",a.id},{"local",a.position.local},{"radius",a.radius}});
    for(const auto& r:patch.routes)routes.push_back({{"id",r.id},{"start",r.start.local},{"end",r.end.local},{"half_width",r.halfWidth},{"agents",r.agents}});
    engine::writeDocument(out/"terrain.json","engine.terrain-patch",{{"id",patch.id.text()},{"region",{region.x,region.z}},{"model","model.json"},{"rocks",rocks},{"landmarks",anchors},{"routes",routes},{"cpu_bytes",patch.bytes()}});
    std::cout<<"Cooked "<<patch.id.text()<<": "<<patch.mesh.indices.size()/3<<" triangles, "<<patch.rocks.size()<<" rocks, "<<patch.bytes()<<" CPU bytes\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}

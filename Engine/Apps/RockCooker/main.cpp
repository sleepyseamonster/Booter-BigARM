#include "World/Rocks/RockGenerator.h"
#include "Persistence/Document.h"
#include <charconv>
#include <iostream>
namespace {template<class T>T number(const char* text){T result;const std::string value=text;const auto parsed=std::from_chars(value.data(),value.data()+value.size(),result);if(parsed.ec!=std::errc{}||parsed.ptr!=value.data()+value.size())throw std::runtime_error("Invalid integer identity argument");return result;}}
int main(int argc,char** argv)try {
    if(argc!=3&&argc!=7)throw std::runtime_error("Usage: engine_rock_cook recipe.json new-output-directory [world-seed region-x region-z member]");
    const auto recipe=engine::loadRockRecipe(argv[1]);engine::GeneratedId id{recipe.seed,recipe.version,{},0,"rock"};
    if(argc==7){id.seed=number<uint64_t>(argv[3]);id.region={number<int64_t>(argv[4]),number<int64_t>(argv[5])};id.member=number<uint64_t>(argv[6]);}
    const auto rock=engine::generateRock(recipe,id);const std::filesystem::path out=argv[2];engine::saveModel(out,rock.mesh);engine::saveRockRecipe(out/"recipe.json",recipe);
    engine::writeDocument(out/"rock.json","engine.rock-result",{{"id",rock.id},{"minimum",rock.minimum},{"maximum",rock.maximum},{"footprint_radius",rock.footprintRadius},{"triangle_surfaces",rock.surfaces},{"model","model.json"}});
    std::cout<<"Cooked "<<rock.id<<": "<<rock.mesh.indices.size()/3<<" triangles, footprint "<<rock.footprintRadius<<" m\n";return 0;
}catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}

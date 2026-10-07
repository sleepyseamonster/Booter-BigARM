#include "Persistence/WorldSave.h"
#include "Assets/TextureData.h"
#include <algorithm>
#include <charconv>
#include <fstream>
#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#else
#include <fcntl.h>
#include <sys/file.h>
#include <unistd.h>
#endif
namespace engine {
namespace {
class WriterLease {
#ifdef _WIN32
    HANDLE handle_=INVALID_HANDLE_VALUE;
#else
    int handle_=-1;
#endif
public:
    explicit WriterLease(const std::filesystem::path& root){
        const auto path=root/"world.lock";
#ifdef _WIN32
        handle_=CreateFileW(path.c_str(),GENERIC_READ|GENERIC_WRITE,0,nullptr,OPEN_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
        if(handle_==INVALID_HANDLE_VALUE)throw std::runtime_error("World profile has an active writer");
#else
        handle_=::open(path.c_str(),O_RDWR|O_CREAT,0600);if(handle_<0)throw std::runtime_error("Cannot open world writer lease");
        if(::flock(handle_,LOCK_EX|LOCK_NB)!=0){::close(handle_);handle_=-1;throw std::runtime_error("World profile has an active writer");}
#endif
    }
    ~WriterLease(){
#ifdef _WIN32
        if(handle_!=INVALID_HANDLE_VALUE)CloseHandle(handle_);
#else
        if(handle_>=0)::close(handle_);
#endif
    }
};
constexpr const char* files[]={"terrain.json","rock.json","constraints.json","player.json","deltas.json"};
struct Inventory {std::vector<std::pair<uint64_t,std::filesystem::path>> committed;uint64_t maximum=0;bool staging=false;};
Inventory inventory(const std::filesystem::path& profile){
    Inventory result;if(!std::filesystem::exists(profile))return result;
    if(std::filesystem::exists(profile/"snapshot-a.json")||std::filesystem::exists(profile/"snapshot-b.json"))throw DocumentCompatibilityError("Legacy player-only profile cannot become a world profile implicitly");
    size_t count=0;
    for(const auto& entry:std::filesystem::directory_iterator(profile)){
        const auto name=entry.path().filename().string();if(!name.starts_with("world-gen-"))continue;
        if(++count>64)throw std::runtime_error("World profile generation limit reached; preserve and inspect this profile");
        auto suffix=name.substr(10);const bool staging=suffix.ends_with(".writing");if(staging)suffix.resize(suffix.size()-8);
        uint64_t n=0;const auto parsed=std::from_chars(suffix.data(),suffix.data()+suffix.size(),n);
        if(parsed.ec!=std::errc{}||parsed.ptr!=suffix.data()+suffix.size()||!n||std::to_string(n)!=suffix)throw std::runtime_error("Unrecognized world generation name");
        result.maximum=std::max(result.maximum,n);result.staging|=staging;
        if(!staging)result.committed.emplace_back(n,entry.path());
    }
    std::sort(result.committed.begin(),result.committed.end(),[](const auto& a,const auto& b){return a.first>b.first;});return result;
}
std::pair<size_t,uint32_t> fingerprint(const std::filesystem::path& file){
    std::ifstream in(file,std::ios::binary|std::ios::ate);if(!in)throw std::runtime_error("Missing world generation member");const auto size=in.tellg();if(size<=0||size>std::streamoff(documentLimit))throw std::runtime_error("World generation member exceeds document bound");
    std::vector<uint8_t> bytes(static_cast<size_t>(size));in.seekg(0);in.read(reinterpret_cast<char*>(bytes.data()),size);if(!in)throw std::runtime_error("Cannot read world generation member");return {bytes.size(),textureCrc(bytes)};
}
Json encodeDeltas(const WorldDeltas& d){validateDeltas(d);Json rows=Json::array();for(const auto& [r,m]:d.removedRocks)rows.push_back({{"region",{r.first,r.second}},{"removed_rocks",m}});return {{"regions",rows}};}
Region region(const Json& a){if(!a.is_array()||a.size()!=2)throw std::runtime_error("Invalid saved region");for(const auto& n:a)if(!n.is_number_integer()||n<INT64_MIN||n>INT64_MAX)throw std::runtime_error("Invalid saved region coordinate");return {a[0].get<int64_t>(),a[1].get<int64_t>()};}
WorldDeltas decodeDeltas(const Json& p){
    if(p.size()!=1||!p.at("regions").is_array()||p.at("regions").size()>256)throw std::runtime_error("Invalid world delta document");WorldDeltas d;
    for(const auto& row:p.at("regions")){if(row.size()!=2)throw std::runtime_error("Invalid region delta fields");const auto r=region(row.at("region"));const auto& list=row.at("removed_rocks");if(!list.is_array()||list.empty()||list.size()>256)throw std::runtime_error("Invalid removed-rock list");std::set<uint64_t> members;for(const auto& n:list){if(!n.is_number_unsigned()||n>255||!members.insert(n.get<uint64_t>()).second)throw std::runtime_error("Invalid/duplicate rock delta");}if(!d.removedRocks.emplace(DeltaRegion{r.x,r.z},std::move(members)).second)throw std::runtime_error("Duplicate region delta");}
    validateDeltas(d);return d;
}
WorldSave readGeneration(const std::filesystem::path& directory,uint64_t generation){
    const auto manifest=readDocument(directory/"commit.json","engine.world-generation");
    if(manifest.size()!=4||!manifest.at("generation").is_number_unsigned()||manifest.at("generation")!=generation||!manifest.at("files").is_array()||manifest.at("files").size()!=5)throw std::runtime_error("Invalid world generation manifest");
    if(manifest.at("content_version")!=1)throw DocumentCompatibilityError("Unsupported world content version");
    size_t i=0;for(const auto& row:manifest.at("files")){if(row.size()!=3||row.at("file")!=files[i]||!row.at("bytes").is_number_unsigned()||!row.at("crc").is_number_unsigned())throw std::runtime_error("Invalid generation member manifest");const auto actual=fingerprint(directory/files[i++]);if(row.at("bytes")!=actual.first||row.at("crc")!=actual.second)throw std::runtime_error("World generation member integrity mismatch");}
    WorldSave s;s.origin=region(manifest.at("origin"));s.configuration={loadTerrainRecipe(directory/"terrain.json"),loadRockRecipe(directory/"rock.json"),loadConstraints(directory/"constraints.json")};s.player=loadSnapshotFile(directory/"player.json");s.deltas=decodeDeltas(readDocument(directory/"deltas.json","engine.world-deltas"));return s;
}
WorldSaveRead read(const Inventory& entries,const WorldConfiguration& expected){
    WorldSaveRead result;result.recovered=entries.staging;
    for(const auto& [generation,path]:entries.committed){try{auto value=readGeneration(path,generation);if(value.configuration!=expected)throw DocumentCompatibilityError("Saved world seed, recipes or authored constraints differ; select the matching configuration or a new world profile");result.value=std::move(value);result.generation=generation;return result;}catch(const DocumentCompatibilityError&){throw;}catch(const std::exception&){result.recovered=true;}}
    if(!entries.committed.empty())throw std::runtime_error("No complete valid world generation; existing profile preserved");return result;
}
}
WorldSaveRead loadWorldSave(const std::filesystem::path& profile,const WorldConfiguration& expected){return read(inventory(profile),expected);}
uint64_t saveWorld(const std::filesystem::path& profile,const WorldSave& state,uint64_t expectedGeneration,const std::function<void(unsigned)>& checkpoint){
    validateTerrainRecipe(state.configuration.terrain);validateRecipe(state.configuration.rock);validateConstraints(state.configuration.constraints);validateSnapshot(state.player);validateDeltas(state.deltas);
    std::filesystem::create_directories(profile);WriterLease lease(profile);const auto entries=inventory(profile);const auto previous=read(entries,state.configuration);
    if(previous.generation!=expectedGeneration)throw std::runtime_error("World profile changed in another session; stale save rejected");
    if(entries.maximum==UINT64_MAX)throw std::runtime_error("World save generation exhausted");const auto generation=entries.maximum+1;
    const auto directory=profile/("world-gen-"+std::to_string(generation)+".writing");const auto committed=profile/("world-gen-"+std::to_string(generation));
    if(!std::filesystem::create_directory(directory))throw std::runtime_error("World staging generation already exists");
    unsigned step=0;auto point=[&]{if(checkpoint)checkpoint(++step);};
    saveTerrainRecipe(directory/"terrain.json",state.configuration.terrain);point();saveRockRecipe(directory/"rock.json",state.configuration.rock);point();saveConstraints(directory/"constraints.json",state.configuration.constraints);point();saveSnapshotFile(directory/"player.json",state.player);point();writeDocument(directory/"deltas.json","engine.world-deltas",encodeDeltas(state.deltas));point();
    Json members=Json::array();for(const auto* file:files){const auto [bytes,crc]=fingerprint(directory/file);members.push_back({{"file",file},{"bytes",bytes},{"crc",crc}});}
    writeDocument(directory/"commit.json","engine.world-generation",{{"generation",generation},{"content_version",1},{"origin",{state.origin.x,state.origin.z}},{"files",members}});point();
    std::filesystem::rename(directory,committed); // Only a complete immutable cohort becomes visible.
    // Retain the new generation plus the previous valid generation. Preserve corrupt/staging evidence.
    for(const auto& [n,path]:entries.committed)if(n!=previous.generation){try{readGeneration(path,n);std::error_code ignored;std::filesystem::remove_all(path,ignored);}catch(const std::exception&){} }
    return generation;
}
}

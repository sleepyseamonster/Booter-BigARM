#include "Assets/TextureCatalog.h"
#include "Persistence/Document.h"
#include <set>
#include <stdexcept>
namespace engine {
namespace {
bool hexDigest(const std::string& value) {return value.size()==64 && value.find_first_not_of("0123456789abcdef")==std::string::npos;}
uint32_t integer(const Json& record,const char* field,uint32_t maximum) {
    const auto& value=record.at(field);
    if (!value.is_number_unsigned() || value>maximum) throw std::runtime_error("Invalid texture catalog integer");
    return value.get<uint32_t>();
}
}
std::vector<TextureRecord> loadTextureCatalog(const std::filesystem::path& path) {
    const auto document=readDocument(path,"engine.texture-catalog");
    const auto root=std::filesystem::absolute(path).parent_path();
    if (document.size()!=1 || !document.at("textures").is_array() || document.at("textures").size()>512)
        throw std::runtime_error("Invalid texture catalog");
    std::vector<TextureRecord> result;std::set<std::string> identities;
    for (const auto& row:document.at("textures")) {
        if (!row.is_object() || row.size()!=12) throw std::runtime_error("Unsupported catalog fields");
        TextureRecord r;
        r.id=row.at("id").get<std::string>();r.key=row.at("key").get<std::string>();r.sha256=row.at("sha256").get<std::string>();
        if (r.id.empty() || r.id.size()>181 || r.id.find_first_not_of("abcdefghijklmnopqrstuvwxyz0123456789/_-")!=std::string::npos ||
            !identities.insert(r.id).second || !hexDigest(r.key) || !hexDigest(r.sha256)) throw std::runtime_error("Invalid/duplicate asset identity or content key");
        r.role=textureRole(row.at("role").get<std::string>());r.srgb=row.at("srgb").get<bool>();
        r.file=contentPath(root,row.at("file").get<std::string>());
        r.width=integer(row,"width",2048);r.height=integer(row,"height",2048);r.mips=integer(row,"mips",12);
        r.residentBytes=integer(row,"resident_bytes",uint32_t(textureFileLimit));r.fileBytes=integer(row,"file_bytes",uint32_t(textureFileLimit));
        r.crc32=integer(row,"crc32",UINT32_MAX);
        if (!r.width || !r.height || !r.mips || !r.residentBytes || !r.fileBytes || r.srgb!=(r.role==TextureRole::Color))
            throw std::runtime_error("Invalid catalog dimensions or transfer function");
        result.push_back(std::move(r));
    }
    return result;
}
TextureData loadTexture(const TextureRecord& record) {
    const auto bytes=readTextureFile(record.file);
    if (bytes.size()!=record.fileBytes || textureCrc(bytes)!=record.crc32)
        throw std::runtime_error("Texture integrity mismatch: "+record.id);
    auto result=parseTexture(bytes,record.role);
    if (result.srgb!=record.srgb || result.levels[0].width!=record.width || result.levels[0].height!=record.height ||
        result.levels.size()!=record.mips || result.bytes()!=record.residentBytes)
        throw std::runtime_error("Texture metadata disagrees with payload: "+record.id);
    return result;
}
}

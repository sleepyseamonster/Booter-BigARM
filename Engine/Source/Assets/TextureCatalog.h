#pragma once
#include "Assets/TextureData.h"
#include <filesystem>
#include <string>
#include <vector>
namespace engine {
struct TextureRecord {
    std::string id,key,sha256;
    std::filesystem::path file;
    TextureRole role;
    uint32_t width=0,height=0,mips=0,crc32=0;
    size_t residentBytes=0,fileBytes=0;
    bool srgb=false;
};
std::vector<TextureRecord> loadTextureCatalog(const std::filesystem::path& path);
TextureData loadTexture(const TextureRecord& record);
}

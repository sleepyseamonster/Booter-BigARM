#pragma once
#include <cstdint>
#include <filesystem>
#include <span>
#include <string>
#include <vector>
namespace engine {
enum class TextureRole { Color, Normal, Surface, Height, Mask };
TextureRole textureRole(const std::string& name);
struct ImageLevel { uint32_t width=0,height=0; std::vector<uint8_t> rgba; };
struct TextureData { bool srgb=false; std::vector<ImageLevel> levels; size_t bytes() const; };
inline constexpr size_t textureFileLimit=32*1024*1024;
std::vector<uint8_t> readTextureFile(const std::filesystem::path& path);
TextureData semanticMips(const ImageLevel& base,TextureRole role);
// Strict engine subset: little-endian 2D RGBA8, complete chain, no arrays/cubes/metadata.
TextureData parseTexture(std::span<const uint8_t> bytes,TextureRole role);
uint32_t textureCrc(std::span<const uint8_t> bytes);
}

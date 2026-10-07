#pragma once
#include <filesystem>
#include <nlohmann/json.hpp>
#include <string_view>
#include <stdexcept>

namespace engine {
using Json=nlohmann::json;
struct DocumentCompatibilityError : std::runtime_error {using std::runtime_error::runtime_error;};
inline constexpr size_t documentLimit=1024*1024;
// Strict JSON envelope, bounded bytes/depth, duplicate keys rejected. No implicit migrations.
Json readDocument(const std::filesystem::path& path,std::string_view kind,unsigned version=1);
void writeDocument(const std::filesystem::path& path,std::string_view kind,const Json& payload,unsigned version=1);
// Relative portable content references only; resolves symlinks before checking containment.
std::filesystem::path contentPath(const std::filesystem::path& root,std::string_view relative);
}

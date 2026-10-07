#pragma once
#include <array>
#include <cstdint>
#include <filesystem>
#include <string>
#include <vector>
namespace engine {
using SkinMatrix=std::array<float,16>;
struct SkinVertex {
    std::array<float,3> position{},normal{};
    std::array<float,2> uv{};
    std::array<float,4> tangent{1,0,0,1},weights{1,0,0,0};
    std::array<uint8_t,4> joints{};
};
static_assert(sizeof(SkinVertex)==68);
struct ModelNode {int parent=-1;std::array<float,3> translation{};std::array<float,4> rotation{0,0,0,1};};
struct ModelTrack {uint32_t node=0;bool rotation=false;std::vector<float> times;std::vector<std::array<float,4>> values;};
struct ModelClip {std::string name;float duration=1;std::vector<ModelTrack> tracks;};
struct ModelData {
    std::vector<SkinVertex> vertices;
    std::vector<uint32_t> indices;
    std::vector<ModelNode> nodes;
    std::vector<uint32_t> joints;
    std::vector<SkinMatrix> inverseBind;
    std::vector<ModelClip> clips;
    std::array<float,4> baseColor{1,1,1,1};
    float roughness=1,metallic=1;
};
void validateModel(const ModelData&);
void saveModel(const std::filesystem::path& newDirectory,const ModelData&);
ModelData loadModel(const std::filesystem::path& manifest);
}

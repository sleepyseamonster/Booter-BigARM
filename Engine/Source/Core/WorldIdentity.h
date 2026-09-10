#pragma once
#include <array>
#include <cstdint>
#include <string>

namespace engine {
// Metres, seconds, right-handed Y-up. Region span is versioned world configuration.
struct Region { int64_t x=0, z=0; bool operator==(const Region&) const = default; };
struct WorldPosition {
    Region region;
    std::array<double,3> local{};
    WorldPosition normalized(double regionSpan) const;
    std::array<float,3> relativeTo(const WorldPosition& origin,double regionSpan,double maxDistance) const;
};
// Structural ID remains stable across generation order, unload and process restart.
// It is deliberately distinct from ECS IDs, asset keys and GPU/physics handles.
struct GeneratedId {
    uint64_t seed=0;
    uint32_t generatorVersion=1;
    Region region;
    uint64_t member=0;
    std::string generator="world";
    std::string text() const;
    bool operator==(const GeneratedId&) const = default;
};
}

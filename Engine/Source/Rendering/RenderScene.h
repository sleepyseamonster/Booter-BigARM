#pragma once

#include "Authoring/SceneDocument.h"
#include <map>
#include <memory>
#include <optional>

namespace engine {

class SurfaceMaterialBinding;

enum class BuiltinRenderMesh : std::uint8_t { Cube, SlopedSolid, Sphere, DiagnosticCube };

struct MeshAssetId {
    std::string value;
    bool operator==(const MeshAssetId&) const = default;
};
struct MaterialAssetId {
    std::string value;
    bool operator==(const MaterialAssetId&) const = default;
};

struct RenderMeshAsset {
    MeshAssetId id;
    BuiltinRenderMesh builtin = BuiltinRenderMesh::DiagnosticCube;
    std::array<double, 3> minimum{-0.5, -0.5, -0.5};
    std::array<double, 3> maximum{0.5, 0.5, 0.5};
    bool hasUv0 = false;
    bool hasTangents = false;
    // Keeps imported CPU/GPU resource ownership alive while a snapshot uses it.
    std::shared_ptr<const void> lease;
};

struct RenderMaterialAsset {
    MaterialAssetId id;
    std::array<float, 4> baseColor{0.65F, 0.65F, 0.68F, 1.0F};
    float roughness = 0.75F;
    float metallic = 0.0F;
    bool normalMapped = false;
    std::shared_ptr<const SurfaceMaterialBinding> surface;
    std::shared_ptr<const void> lease;
};

class RenderAssetCatalog {
public:
    RenderAssetCatalog();
    void publish(RenderMeshAsset);
    void publish(RenderMaterialAsset);
    std::shared_ptr<const RenderMeshAsset> mesh(const MeshAssetId&) const;
    std::shared_ptr<const RenderMaterialAsset> material(const MaterialAssetId&) const;
private:
    std::map<std::string, std::shared_ptr<const RenderMeshAsset>> meshes_;
    std::map<std::string, std::shared_ptr<const RenderMaterialAsset>> materials_;
};

struct RenderSceneDraw {
    SceneEntityId entity = 0;
    std::shared_ptr<const RenderMeshAsset> mesh;
    std::shared_ptr<const RenderMaterialAsset> material;
    AffineMatrix world = identityAffine();
    AffineMatrix normal = identityAffine();
    std::array<float, 3> boundsCenter{};
    float boundsRadius = 0.0F;
    bool diagnostic = false;
    std::string diagnosticMessage;
};

struct RenderSceneSnapshot {
    std::string sceneId;
    std::uint64_t revision = 0;
    std::vector<RenderSceneDraw> draws;
    std::vector<std::string> errors;
    std::shared_ptr<const AuthoringSceneDocument> sourceLease;
};

// Extraction uses indexed entities and cached affine matrices from one immutable
// scene revision. Missing bindings become explicit diagnostic cubes when enabled.
RenderSceneSnapshot extractRenderScene(std::shared_ptr<const AuthoringSceneDocument>,
                                       const RenderAssetCatalog&, bool diagnosticPreview = true);

struct PickRay {
    std::array<double, 3> origin{};
    std::array<double, 3> direction{0, 0, -1};
};
struct ScenePick {
    SceneEntityId entity = 0;
    double distance = 0;
};
std::optional<ScenePick> pickRenderScene(const RenderSceneSnapshot&, const PickRay&);

struct RenderViewportState {
    std::string id;
    std::uint16_t sceneView = 0;
    std::uint16_t displayView = 0;
    std::uint16_t width = 1;
    std::uint16_t height = 1;
    std::uint64_t targetGeneration = 1;
};

// CPU ownership contract for viewport-specific cameras/targets/view IDs. The GPU
// owner consumes these states; resize/remove cannot mutate another viewport.
class RenderViewportRegistry {
public:
    void add(RenderViewportState);
    void resize(const std::string& id, std::uint16_t width, std::uint16_t height);
    void remove(const std::string& id);
    const RenderViewportState& get(const std::string& id) const;
private:
    std::map<std::string, RenderViewportState> views_;
};

} // namespace engine

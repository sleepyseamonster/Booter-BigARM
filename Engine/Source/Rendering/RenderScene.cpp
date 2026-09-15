#include "Rendering/RenderScene.h"
#include <algorithm>
#include <cmath>
#include <limits>
#include <stdexcept>

namespace engine {
namespace {

void validId(const std::string& value, const char* kind) {
    if (value.empty() || value.size() > 256 || value.find('\0') != std::string::npos)
        throw std::invalid_argument(std::string("Invalid ") + kind + " asset identity");
}

std::array<float, 3> transformedBounds(const AffineMatrix& world,
                                        const std::array<double, 3>& minimum,
                                        const std::array<double, 3>& maximum,
                                        float& radius) {
    std::array<double, 3> low{std::numeric_limits<double>::max(), std::numeric_limits<double>::max(), std::numeric_limits<double>::max()};
    std::array<double, 3> high{-low[0], -low[1], -low[2]};
    for (unsigned corner = 0; corner < 8; ++corner) {
        const auto point = affinePoint(world, {
            corner & 1 ? maximum[0] : minimum[0],
            corner & 2 ? maximum[1] : minimum[1],
            corner & 4 ? maximum[2] : minimum[2]});
        for (size_t axis = 0; axis < 3; ++axis) {
            low[axis] = std::min(low[axis], point[axis]);
            high[axis] = std::max(high[axis], point[axis]);
        }
    }
    std::array<float, 3> center{};
    double squared = 0;
    for (size_t axis = 0; axis < 3; ++axis) {
        center[axis] = float((low[axis] + high[axis]) * 0.5);
        const double extent = (high[axis] - low[axis]) * 0.5;
        squared += extent * extent;
    }
    radius = float(std::sqrt(squared));
    return center;
}

std::shared_ptr<const RenderMeshAsset> diagnosticMesh(const RenderAssetCatalog& catalog) {
    auto result = catalog.mesh({"engine://diagnostic/cube"});
    if (!result) throw std::logic_error("Diagnostic mesh is missing from render catalog");
    return result;
}
std::shared_ptr<const RenderMaterialAsset> diagnosticMaterial(const RenderAssetCatalog& catalog) {
    auto result = catalog.material({"engine://diagnostic/missing"});
    if (!result) throw std::logic_error("Diagnostic material is missing from render catalog");
    return result;
}

} // namespace

RenderAssetCatalog::RenderAssetCatalog() {
    publish(RenderMeshAsset{{"engine://diagnostic/cube"}, BuiltinRenderMesh::DiagnosticCube});
    publish(RenderMeshAsset{{"engine://mesh/cube"}, BuiltinRenderMesh::Cube});
    publish(RenderMeshAsset{{"engine://mesh/sloped-solid"}, BuiltinRenderMesh::SlopedSolid});
    publish(RenderMeshAsset{{"engine://mesh/sphere"}, BuiltinRenderMesh::Sphere});
    publish(RenderMaterialAsset{{"engine://diagnostic/missing"}, {1.0F, 0.02F, 0.65F, 1.0F}, 0.65F, 0.0F});
    publish(RenderMaterialAsset{{"engine://material/default"}, {0.55F, 0.58F, 0.62F, 1.0F}, 0.8F, 0.0F});
}

void RenderAssetCatalog::publish(RenderMeshAsset asset) {
    validId(asset.id.value, "mesh");
    for (size_t axis = 0; axis < 3; ++axis)
        if (!std::isfinite(asset.minimum[axis]) || !std::isfinite(asset.maximum[axis]) || asset.minimum[axis] >= asset.maximum[axis])
            throw std::invalid_argument("Invalid mesh bounds for " + asset.id.value);
    const auto id=asset.id.value;
    meshes_[id] = std::make_shared<const RenderMeshAsset>(std::move(asset));
}

void RenderAssetCatalog::publish(RenderMaterialAsset asset) {
    validId(asset.id.value, "material");
    if (!std::isfinite(asset.roughness) || asset.roughness < 0 || asset.roughness > 1 ||
        !std::isfinite(asset.metallic) || asset.metallic < 0 || asset.metallic > 1)
        throw std::invalid_argument("Invalid material factors for " + asset.id.value);
    for (float channel : asset.baseColor)
        if (!std::isfinite(channel) || channel < 0 || channel > 1) throw std::invalid_argument("Invalid material color for " + asset.id.value);
    const auto id=asset.id.value;
    materials_[id] = std::make_shared<const RenderMaterialAsset>(std::move(asset));
}

std::shared_ptr<const RenderMeshAsset> RenderAssetCatalog::mesh(const MeshAssetId& id) const {
    const auto found = meshes_.find(id.value);
    return found == meshes_.end() ? nullptr : found->second;
}
std::shared_ptr<const RenderMaterialAsset> RenderAssetCatalog::material(const MaterialAssetId& id) const {
    const auto found = materials_.find(id.value);
    return found == materials_.end() ? nullptr : found->second;
}

RenderSceneSnapshot extractRenderScene(std::shared_ptr<const AuthoringSceneDocument> source,
                                       const RenderAssetCatalog& catalog, bool diagnostics) {
    if (!source) throw std::invalid_argument("Render scene needs a source snapshot");
    RenderSceneSnapshot result{source->sceneId(), source->version(), {}, {}, source};
    result.draws.reserve(source->entityCount());
    for (size_t index = 0; index < source->entityCount(); ++index) {
        const auto& entity = source->entityAt(index);
        if (!entity.visible) continue;
        auto mesh = catalog.mesh({entity.mesh.empty() ? "engine://mesh/cube" : entity.mesh});
        auto material = catalog.material({entity.material.empty() ? "engine://material/default" : entity.material});
        std::string error;
        if (!mesh) error += "Entity " + std::to_string(entity.id) + " mesh not resolved: " + entity.mesh;
        if (!material) {
            if (!error.empty()) error += "; ";
            error += "Entity " + std::to_string(entity.id) + " material not resolved: " + entity.material;
        }
        if (material && material->normalMapped && (!mesh || !mesh->hasUv0 || !mesh->hasTangents)) {
            if (!error.empty()) error += "; ";
            error += "Normal-mapped material requires UV0 and compatible tangents";
        }
        if (!error.empty()) {
            result.errors.push_back(error);
            if (!diagnostics) continue;
            mesh = diagnosticMesh(catalog);
            material = diagnosticMaterial(catalog);
        }
        const auto& world = source->worldMatrix(entity.id);
        validateRenderAffine(world);
        RenderSceneDraw draw;
        draw.entity = entity.id;
        draw.mesh = std::move(mesh);
        draw.material = std::move(material);
        draw.world = world;
        draw.normal = normalAffine(world);
        draw.boundsCenter = transformedBounds(world, draw.mesh->minimum, draw.mesh->maximum, draw.boundsRadius);
        draw.diagnostic = !error.empty();
        draw.diagnosticMessage = std::move(error);
        result.draws.push_back(std::move(draw));
    }
    return result;
}

std::optional<ScenePick> pickRenderScene(const RenderSceneSnapshot& scene, const PickRay& ray) {
    const double directionLength = std::hypot(ray.direction[0], ray.direction[1], ray.direction[2]);
    if (!std::isfinite(directionLength) || directionLength < 1e-12) throw std::invalid_argument("Pick direction is invalid");
    for (double value : ray.origin) if (!std::isfinite(value)) throw std::invalid_argument("Pick origin is invalid");
    const std::array<double, 3> direction{ray.direction[0] / directionLength, ray.direction[1] / directionLength, ray.direction[2] / directionLength};
    std::optional<ScenePick> best;
    for (const auto& draw : scene.draws) {
        const auto inverse = inverseAffine(draw.world);
        const auto localOrigin = affinePoint(inverse, ray.origin);
        const std::array<double, 3> localDirection{
            inverse[0] * direction[0] + inverse[4] * direction[1] + inverse[8] * direction[2],
            inverse[1] * direction[0] + inverse[5] * direction[1] + inverse[9] * direction[2],
            inverse[2] * direction[0] + inverse[6] * direction[1] + inverse[10] * direction[2]};
        double near = 0, far = std::numeric_limits<double>::max();
        bool hit = true;
        for (size_t axis = 0; axis < 3; ++axis) {
            if (std::abs(localDirection[axis]) < 1e-12) {
                if (localOrigin[axis] < draw.mesh->minimum[axis] || localOrigin[axis] > draw.mesh->maximum[axis]) hit = false;
                continue;
            }
            double a = (draw.mesh->minimum[axis] - localOrigin[axis]) / localDirection[axis];
            double b = (draw.mesh->maximum[axis] - localOrigin[axis]) / localDirection[axis];
            if (a > b) std::swap(a, b);
            near = std::max(near, a); far = std::min(far, b);
            if (near > far) hit = false;
        }
        if (hit) {
            const auto local = std::array<double, 3>{localOrigin[0] + localDirection[0] * near,
                                                     localOrigin[1] + localDirection[1] * near,
                                                     localOrigin[2] + localDirection[2] * near};
            const auto world = affinePoint(draw.world, local);
            const double distance = std::hypot(world[0] - ray.origin[0], world[1] - ray.origin[1], world[2] - ray.origin[2]);
            if (!best || distance < best->distance || (distance == best->distance && draw.entity < best->entity)) best = ScenePick{draw.entity, distance};
        }
    }
    return best;
}

void RenderViewportRegistry::add(RenderViewportState view) {
    if (view.id.empty() || !view.width || !view.height || view.sceneView == view.displayView) throw std::invalid_argument("Invalid viewport state");
    for (const auto& [_, existing] : views_)
        if (existing.sceneView == view.sceneView || existing.sceneView == view.displayView ||
            existing.displayView == view.sceneView || existing.displayView == view.displayView)
            throw std::invalid_argument("Viewport view IDs overlap");
    if (!views_.emplace(view.id, std::move(view)).second) throw std::invalid_argument("Viewport identity already exists");
}
void RenderViewportRegistry::resize(const std::string& id, std::uint16_t width, std::uint16_t height) {
    if (!width || !height) throw std::invalid_argument("Viewport dimensions must be nonzero");
    auto& view = views_.at(id);
    if (view.width != width || view.height != height) { view.width = width; view.height = height; ++view.targetGeneration; }
}
void RenderViewportRegistry::remove(const std::string& id) { if (!views_.erase(id)) throw std::out_of_range("Viewport does not exist"); }
const RenderViewportState& RenderViewportRegistry::get(const std::string& id) const { return views_.at(id); }

} // namespace engine

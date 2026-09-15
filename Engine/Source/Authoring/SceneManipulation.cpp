#include "Authoring/SceneManipulation.h"
#include <algorithm>
#include <cmath>
#include <set>

namespace engine {
namespace {

double snapped(double value, double step) {
    if (!std::isfinite(value) || !std::isfinite(step) || step < 0) throw SceneDocumentError("Invalid gizmo delta or snap value");
    return step == 0 ? value : std::round(value / step) * step;
}

AffineMatrix translation(std::array<double, 3> value) {
    auto result = identityAffine();
    for (size_t i = 0; i < 3; ++i) result[12 + i] = value[i];
    return result;
}
AffineMatrix scale(std::array<double, 3> value) {
    auto result = identityAffine();
    for (size_t i = 0; i < 3; ++i) result[i * 4 + i] = value[i];
    return result;
}
AffineMatrix rotation(std::array<double, 4> q, double snap) {
    double length = 0; for (double value : q) length += value * value;
    if (!std::isfinite(length) || length < 1e-20) throw SceneDocumentError("Invalid gizmo rotation");
    length = std::sqrt(length); for (double& value : q) value /= length;
    if (q[3] < 0) for (double& value : q) value = -value;
    if (snap > 0) {
        const double half = std::acos(std::clamp(q[3], -1.0, 1.0));
        const double sine = std::sin(half);
        std::array<double, 3> axis{1, 0, 0};
        if (std::abs(sine) > 1e-12) for (size_t i = 0; i < 3; ++i) axis[i] = q[i] / sine;
        const double next = snapped(half * 2, snap) * 0.5;
        for (size_t i = 0; i < 3; ++i) q[i] = axis[i] * std::sin(next);
        q[3] = std::cos(next);
    }
    const double x=q[0],y=q[1],z=q[2],w=q[3];
    return {1-2*(y*y+z*z),2*(x*y+z*w),2*(x*z-y*w),0,
            2*(x*y-z*w),1-2*(x*x+z*z),2*(y*z+x*w),0,
            2*(x*z+y*w),2*(y*z-x*w),1-2*(x*x+y*y),0,
            0,0,0,1};
}
AffineMatrix around(const std::array<double, 3>& pivot, const AffineMatrix& delta, const AffineMatrix& world) {
    return multiplyAffine(translation(pivot), multiplyAffine(delta, multiplyAffine(translation({-pivot[0],-pivot[1],-pivot[2]}), world)));
}
std::array<double, 3> localTranslation(const AffineMatrix& world, const std::array<double, 3>& value) {
    std::array<double, 3> out{};
    for (size_t column = 0; column < 3; ++column) {
        const double length = std::hypot(world[column*4], world[column*4+1], world[column*4+2]);
        if (length < 1e-12) throw SceneDocumentError("Local gizmo axis is singular", SceneErrorCode::UnsupportedTransform);
        for (size_t row = 0; row < 3; ++row) out[row] += world[column*4+row] * value[column] / length;
    }
    return out;
}

} // namespace

void SceneGizmoSession::begin(std::shared_ptr<const AuthoringSceneDocument> source,
                              std::vector<SceneEntityId> selection, GizmoTool tool,
                              GizmoSpace space, GizmoSnap snap) {
    if (active_) throw SceneDocumentError("A gizmo drag is already active", SceneErrorCode::Closed);
    if (!source || selection.empty() || selection.size() > AuthoringSceneDocument::maxEntities)
        throw SceneDocumentError("Gizmo selection is empty or invalid");
    if (!std::isfinite(snap.translation) || !std::isfinite(snap.rotationRadians) || !std::isfinite(snap.scale) ||
        snap.translation < 0 || snap.rotationRadians < 0 || snap.scale < 0)
        throw SceneDocumentError("Gizmo snapping values are invalid");
    std::sort(selection.begin(), selection.end());
    if (std::adjacent_find(selection.begin(), selection.end()) != selection.end()) throw SceneDocumentError("Gizmo selection contains duplicate IDs");
    source_ = std::move(source); selection_ = std::move(selection); tool_ = tool; space_ = space; snap_ = snap;
    original_.clear(); original_.reserve(selection_.size()); pivot_ = {};
    for (auto id : selection_) {
        if (!source_->find(id)) throw SceneDocumentError("Gizmo entity does not exist");
        original_.push_back(source_->worldMatrix(id));
        for (size_t axis = 0; axis < 3; ++axis) pivot_[axis] += original_.back()[12 + axis];
    }
    for (double& value : pivot_) value /= double(selection_.size());
    preview_ = {source_->sceneId(), source_->version(), {}};
    preview_.worlds.reserve(selection_.size());
    for (size_t i = 0; i < selection_.size(); ++i) preview_.worlds.emplace_back(selection_[i], original_[i]);
    active_ = true;
}

const GizmoPreview& SceneGizmoSession::update(const GizmoDelta& raw) {
    if (!active_) throw SceneDocumentError("Gizmo drag is not active", SceneErrorCode::Closed);
    auto delta = raw;
    for (double& value : delta.translation) value = snapped(value, snap_.translation);
    for (double& value : delta.scale) {
        value = 1 + snapped(value - 1, snap_.scale);
        if (!std::isfinite(value) || std::abs(value) < 1e-6) throw SceneDocumentError("Gizmo scale is singular", SceneErrorCode::UnsupportedTransform);
    }
    const auto rotate = rotation(delta.rotation, snap_.rotationRadians);
    const auto scaling = scale(delta.scale);
    for (size_t i = 0; i < original_.size(); ++i) {
        AffineMatrix world;
        if (tool_ == GizmoTool::Translate) {
            const auto movement = space_ == GizmoSpace::World ? delta.translation : localTranslation(original_[i], delta.translation);
            world = multiplyAffine(translation(movement), original_[i]);
        } else if (tool_ == GizmoTool::Rotate) {
            world = space_ == GizmoSpace::World ? around(pivot_, rotate, original_[i]) : multiplyAffine(original_[i], rotate);
        } else {
            world = space_ == GizmoSpace::World ? around(pivot_, scaling, original_[i]) : multiplyAffine(original_[i], scaling);
        }
        validateRenderAffine(world);
        preview_.worlds[i].second = world;
    }
    return preview_;
}

Json SceneGizmoSession::commitPayload() const {
    if (!active_) throw SceneDocumentError("Gizmo drag is not active", SceneErrorCode::Closed);
    Json transforms = Json::array();
    for (const auto& [entity, world] : preview_.worlds) transforms.push_back({{"entity", entity}, {"world_matrix", world}});
    return {{"operations", Json::array({{{"type", "set_world_transforms"}, {"transforms", std::move(transforms)}}})}};
}

SceneTransactionResult SceneGizmoSession::commitDirect(AuthoringSceneDocument& document) {
    if (!active_) throw SceneDocumentError("Gizmo drag is not active", SceneErrorCode::Closed);
    if (document.sceneId() != preview_.sceneId || document.version() != preview_.baseRevision) {
        cancel();
        throw SceneDocumentError("Scene changed during gizmo drag", SceneErrorCode::Conflict);
    }
    try {
        auto transaction = document.beginTransaction(preview_.baseRevision);
        transaction.setWorldTransforms(preview_.worlds);
        auto result = transaction.commit();
        cancel();
        return result;
    } catch (...) {
        cancel();
        throw;
    }
}

void SceneGizmoSession::cancel() noexcept {
    source_.reset(); selection_.clear(); original_.clear(); preview_ = {}; active_ = false;
}
const GizmoPreview& SceneGizmoSession::preview() const {
    if (!active_) throw SceneDocumentError("Gizmo drag is not active", SceneErrorCode::Closed);
    return preview_;
}

} // namespace engine

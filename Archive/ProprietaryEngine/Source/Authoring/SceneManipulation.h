#pragma once

#include "Authoring/SceneDocument.h"

namespace engine {

enum class GizmoTool { Translate, Rotate, Scale };
enum class GizmoSpace { World, Local };

struct GizmoSnap {
    double translation = 0;
    double rotationRadians = 0;
    double scale = 0;
};

struct GizmoDelta {
    std::array<double, 3> translation{};
    std::array<double, 4> rotation{0, 0, 0, 1}; // xyzw
    std::array<double, 3> scale{1, 1, 1};
};

struct GizmoPreview {
    std::string sceneId;
    std::uint64_t baseRevision = 0;
    std::vector<std::pair<SceneEntityId, AffineMatrix>> worlds;
};

// UI-independent drag state. Updates are absolute relative to begin(), so mouse
// sampling rate cannot accumulate drift. Commit is one domain transaction.
class SceneGizmoSession {
public:
    void begin(std::shared_ptr<const AuthoringSceneDocument>, std::vector<SceneEntityId>,
               GizmoTool, GizmoSpace, GizmoSnap = {});
    const GizmoPreview& update(const GizmoDelta&);
    Json commitPayload() const;
    SceneTransactionResult commitDirect(AuthoringSceneDocument&);
    void cancel() noexcept;
    void focusLost() noexcept { cancel(); }
    bool active() const noexcept { return active_; }
    const GizmoPreview& preview() const;
private:
    std::shared_ptr<const AuthoringSceneDocument> source_;
    std::vector<SceneEntityId> selection_;
    std::vector<AffineMatrix> original_;
    GizmoTool tool_ = GizmoTool::Translate;
    GizmoSpace space_ = GizmoSpace::World;
    GizmoSnap snap_;
    std::array<double, 3> pivot_{};
    GizmoPreview preview_;
    bool active_ = false;
};

} // namespace engine

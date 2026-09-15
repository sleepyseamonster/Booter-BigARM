#pragma once

#include "Persistence/Document.h"

#include <array>
#include <cstdint>
#include <filesystem>
#include <optional>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

namespace engine {

struct SceneDocumentError : std::runtime_error {
    using std::runtime_error::runtime_error;
};

using SceneEntityId = std::uint64_t;

struct SceneTransform {
    std::array<float, 3> translation{0.0F, 0.0F, 0.0F};
    // Quaternion order is x, y, z, w. Keeping rotation as a quaternion avoids
    // Euler-angle ambiguity at the authoring/AI boundary.
    std::array<float, 4> rotation{0.0F, 0.0F, 0.0F, 1.0F};
    std::array<float, 3> scale{1.0F, 1.0F, 1.0F};
    bool operator==(const SceneTransform&) const = default;
};

struct SceneEntity {
    SceneEntityId id = 0;
    std::string name;
    std::optional<SceneEntityId> parent;
    SceneTransform local;
    std::string mesh;
    std::string material;
    std::string collider;
    std::int32_t lod = 0;
    bool visible = true;
    std::vector<std::string> tags;
};

struct SceneTransactionResult {
    bool applied = false;
    std::uint64_t beforeVersion = 0;
    std::uint64_t afterVersion = 0;
    std::vector<SceneEntityId> changedEntities;
    std::string error;
};

// Renderer-independent authoring state. It is intentionally separate from
// runtime ECS state: the Scene View, Game View, persistence, and AI adapter can
// all consume this document without taking a dependency on a graphics API.
class AuthoringSceneDocument {
public:
    static constexpr unsigned schemaVersion = 1;

    explicit AuthoringSceneDocument(std::string sceneId = "authoring");

    const std::string& sceneId() const noexcept { return sceneId_; }
    std::uint64_t version() const noexcept { return version_; }
    std::uint64_t nextEntityId() const noexcept { return nextEntityId_; }

    SceneEntityId createEntity(std::string name, std::optional<SceneEntityId> parent = {});
    bool eraseEntity(SceneEntityId id);
    const SceneEntity* find(SceneEntityId id) const noexcept;
    bool updateEntityMetadata(SceneEntityId id, std::string mesh, std::string material, std::string collider,
                              std::int32_t lod, bool visible, std::vector<std::string> tags);
    std::vector<SceneEntity> entities() const;
    SceneTransform worldTransform(SceneEntityId id) const;

    class Transaction {
    public:
        Transaction(AuthoringSceneDocument& owner, std::uint64_t expectedVersion);
        void setTransform(SceneEntityId id, SceneTransform transform);
        SceneTransactionResult commit();
        void rollback() noexcept;
        bool closed() const noexcept { return closed_; }

    private:
        AuthoringSceneDocument* owner_;
        std::uint64_t expectedVersion_;
        std::vector<std::pair<SceneEntityId, SceneTransform>> edits_;
        bool closed_ = false;
    };

    Transaction beginTransaction(std::uint64_t expectedVersion) { return Transaction(*this, expectedVersion); }
    bool undo(std::uint64_t expectedVersion = 0);
    bool redo(std::uint64_t expectedVersion = 0);
    bool canUndo() const noexcept { return !undo_.empty(); }
    bool canRedo() const noexcept { return !redo_.empty(); }

    Json toJson() const;
    Json inspectionJson(bool includeWorldTransforms = true) const;
    static AuthoringSceneDocument fromJson(const Json& payload);
    void save(const std::filesystem::path& path) const;
    static AuthoringSceneDocument load(const std::filesystem::path& path);

    static Json transformToJson(const SceneTransform& transform);
    static SceneTransform transformFromJson(const Json& value);
    static Json entityToJson(const SceneEntity& entity);

private:
    struct TransformEdit {
        SceneEntityId id;
        SceneTransform before;
        SceneTransform after;
    };
    struct HistoryEntry {
        std::vector<TransformEdit> edits;
    };

    std::string sceneId_;
    std::uint64_t version_ = 1;
    std::uint64_t nextEntityId_ = 1;
    std::vector<SceneEntity> entities_;
    std::vector<HistoryEntry> undo_;
    std::vector<HistoryEntry> redo_;

    static void validateTransform(const SceneTransform& transform);
    static void validateName(const std::string& name);
    static void validateReference(const std::string& reference);
    SceneEntity* findMutable(SceneEntityId id) noexcept;
    SceneTransform worldTransformRecursive(SceneEntityId id, std::vector<SceneEntityId>& stack) const;
    void pushHistory(std::vector<HistoryEntry>& history, HistoryEntry entry);
    bool applyHistory(const HistoryEntry& entry, bool useAfter, std::vector<HistoryEntry>& destination,
                      std::uint64_t expectedVersion);
};

} // namespace engine

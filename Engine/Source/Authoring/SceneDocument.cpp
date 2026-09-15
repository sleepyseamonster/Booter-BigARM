#include "Authoring/SceneDocument.h"

#include <algorithm>
#include <cmath>
#include <limits>
#include <map>
#include <unordered_set>

namespace engine {
namespace {

template <std::size_t N>
Json arrayToJson(const std::array<float, N>& values) {
    Json result = Json::array();
    for (const float value : values) result.push_back(value);
    return result;
}

template <std::size_t N>
std::array<float, N> arrayFromJson(const Json& value, const char* label) {
    if (!value.is_array() || value.size() != N) throw SceneDocumentError(std::string("Invalid ") + label);
    std::array<float, N> result{};
    for (std::size_t index = 0; index < N; ++index) {
        if (!value.at(index).is_number()) throw SceneDocumentError(std::string("Invalid ") + label);
        result[index] = value.at(index).get<float>();
        if (!std::isfinite(result[index])) throw SceneDocumentError(std::string("Non-finite ") + label);
    }
    return result;
}

float dot(const std::array<float, 4>& a, const std::array<float, 4>& b) {
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
}

std::array<float, 4> normalize(std::array<float, 4> value) {
    const float length = std::sqrt(dot(value, value));
    if (!std::isfinite(length) || length < 1.0e-6F) throw SceneDocumentError("Rotation quaternion is invalid");
    for (float& component : value) component /= length;
    return value;
}

std::array<float, 4> multiply(const std::array<float, 4>& a, const std::array<float, 4>& b) {
    return {a[3] * b[0] + a[0] * b[3] + a[1] * b[2] - a[2] * b[1],
            a[3] * b[1] - a[0] * b[2] + a[1] * b[3] + a[2] * b[0],
            a[3] * b[2] + a[0] * b[1] - a[1] * b[0] + a[2] * b[3],
            a[3] * b[3] - a[0] * b[0] - a[1] * b[1] - a[2] * b[2]};
}

std::array<float, 3> rotate(const std::array<float, 4>& q, const std::array<float, 3>& v) {
    const std::array<float, 4> p{v[0], v[1], v[2], 0.0F};
    const std::array<float, 4> inverse{-q[0], -q[1], -q[2], q[3]};
    const auto result = multiply(multiply(q, p), inverse);
    return {result[0], result[1], result[2]};
}

std::array<float, 3> componentMultiply(const std::array<float, 3>& a, const std::array<float, 3>& b) {
    return {a[0] * b[0], a[1] * b[1], a[2] * b[2]};
}

void requireObject(const Json& value, const char* label) {
    if (!value.is_object()) throw SceneDocumentError(std::string("Invalid ") + label);
}

} // namespace

AuthoringSceneDocument::AuthoringSceneDocument(std::string sceneId) : sceneId_(std::move(sceneId)) {
    if (sceneId_.empty() || sceneId_.size() > 128) throw SceneDocumentError("Scene id is invalid");
}

void AuthoringSceneDocument::validateName(const std::string& name) {
    if (name.empty() || name.size() > 128) throw SceneDocumentError("Entity name is invalid");
}

void AuthoringSceneDocument::validateReference(const std::string& reference) {
    if (reference.size() > 256) throw SceneDocumentError("Asset reference is too long");
}

void AuthoringSceneDocument::validateTransform(const SceneTransform& transform) {
    for (const float value : transform.translation)
        if (!std::isfinite(value)) throw SceneDocumentError("Translation contains a non-finite value");
    for (const float value : transform.scale)
        if (!std::isfinite(value) || std::abs(value) < 1.0e-6F)
            throw SceneDocumentError("Scale contains an invalid value");
    (void)normalize(transform.rotation);
}

SceneEntityId AuthoringSceneDocument::createEntity(std::string name, std::optional<SceneEntityId> parent) {
    validateName(name);
    if (parent && (!*parent || !find(*parent))) throw SceneDocumentError("Entity parent does not exist");
    if (nextEntityId_ == std::numeric_limits<SceneEntityId>::max()) throw SceneDocumentError("Entity id exhausted");
    entities_.push_back(SceneEntity{nextEntityId_, std::move(name), parent});
    const auto id = nextEntityId_++;
    ++version_;
    redo_.clear();
    return id;
}

bool AuthoringSceneDocument::eraseEntity(SceneEntityId id) {
    const auto found = std::find_if(entities_.begin(), entities_.end(), [id](const SceneEntity& entity) { return entity.id == id; });
    if (found == entities_.end()) return false;
    if (std::any_of(entities_.begin(), entities_.end(), [id](const SceneEntity& entity) { return entity.parent && *entity.parent == id; }))
        throw SceneDocumentError("Cannot erase an entity that has children");
    entities_.erase(found);
    ++version_;
    redo_.clear();
    return true;
}

const SceneEntity* AuthoringSceneDocument::find(SceneEntityId id) const noexcept {
    const auto found = std::find_if(entities_.begin(), entities_.end(), [id](const SceneEntity& entity) { return entity.id == id; });
    return found == entities_.end() ? nullptr : &*found;
}

SceneEntity* AuthoringSceneDocument::findMutable(SceneEntityId id) noexcept {
    const auto found = std::find_if(entities_.begin(), entities_.end(), [id](const SceneEntity& entity) { return entity.id == id; });
    return found == entities_.end() ? nullptr : &*found;
}

bool AuthoringSceneDocument::updateEntityMetadata(SceneEntityId id, std::string mesh, std::string material,
                                                  std::string collider, std::int32_t lod, bool visible,
                                                  std::vector<std::string> tags) {
    auto* entity = findMutable(id);
    if (!entity) throw SceneDocumentError("Entity does not exist");
    validateReference(mesh); validateReference(material); validateReference(collider);
    if (tags.size() > 64) throw SceneDocumentError("Entity has too many tags");
    for (const auto& tag : tags) if (tag.empty() || tag.size() > 64) throw SceneDocumentError("Entity tag is invalid");
    if (entity->mesh == mesh && entity->material == material && entity->collider == collider && entity->lod == lod &&
        entity->visible == visible && entity->tags == tags) return false;
    entity->mesh = std::move(mesh); entity->material = std::move(material); entity->collider = std::move(collider);
    entity->lod = lod; entity->visible = visible; entity->tags = std::move(tags);
    ++version_;
    redo_.clear();
    return true;
}

std::vector<SceneEntity> AuthoringSceneDocument::entities() const {
    auto result = entities_;
    std::sort(result.begin(), result.end(), [](const SceneEntity& a, const SceneEntity& b) { return a.id < b.id; });
    return result;
}

SceneTransform AuthoringSceneDocument::worldTransformRecursive(SceneEntityId id, std::vector<SceneEntityId>& stack) const {
    if (std::find(stack.begin(), stack.end(), id) != stack.end()) throw SceneDocumentError("Scene hierarchy contains a cycle");
    const auto* entity = find(id);
    if (!entity) throw SceneDocumentError("Entity does not exist");
    stack.push_back(id);
    SceneTransform result = entity->local;
    result.rotation = normalize(result.rotation);
    if (entity->parent) {
        const auto parent = worldTransformRecursive(*entity->parent, stack);
        result.translation = parent.translation;
        const auto offset = rotate(parent.rotation, componentMultiply(parent.scale, entity->local.translation));
        for (std::size_t index = 0; index < 3; ++index) result.translation[index] += offset[index];
        result.scale = componentMultiply(parent.scale, entity->local.scale);
        result.rotation = normalize(multiply(parent.rotation, entity->local.rotation));
    }
    stack.pop_back();
    return result;
}

SceneTransform AuthoringSceneDocument::worldTransform(SceneEntityId id) const {
    std::vector<SceneEntityId> stack;
    return worldTransformRecursive(id, stack);
}

AuthoringSceneDocument::Transaction::Transaction(AuthoringSceneDocument& owner, std::uint64_t expectedVersion)
    : owner_(&owner), expectedVersion_(expectedVersion) {}

void AuthoringSceneDocument::Transaction::setTransform(SceneEntityId id, SceneTransform transform) {
    if (closed_) throw SceneDocumentError("Transaction is closed");
    if (!owner_->find(id)) throw SceneDocumentError("Entity does not exist");
    AuthoringSceneDocument::validateTransform(transform);
    transform.rotation = normalize(transform.rotation);
    const auto found = std::find_if(edits_.begin(), edits_.end(), [id](const auto& edit) { return edit.first == id; });
    if (found != edits_.end()) found->second = transform;
    else edits_.emplace_back(id, transform);
}

SceneTransactionResult AuthoringSceneDocument::Transaction::commit() {
    if (closed_) return {false, owner_->version_, owner_->version_, {}, "Transaction is closed"};
    closed_ = true;
    SceneTransactionResult result{false, owner_->version_, owner_->version_, {}, {}};
    if (expectedVersion_ == 0 || expectedVersion_ != owner_->version_) {
        result.error = "Scene version mismatch";
        return result;
    }
    std::vector<TransformEdit> changed;
    for (const auto& edit : edits_) {
        const auto* entity = owner_->find(edit.first);
        if (!entity) { result.error = "Entity no longer exists"; return result; }
        if (entity->local != edit.second) changed.push_back(TransformEdit{edit.first, entity->local, edit.second});
    }
    if (changed.empty()) {
        result.applied = true;
        result.afterVersion = owner_->version_;
        return result;
    }
    for (const auto& edit : changed) owner_->findMutable(edit.id)->local = edit.after;
    owner_->pushHistory(owner_->undo_, HistoryEntry{changed});
    owner_->redo_.clear();
    ++owner_->version_;
    result.applied = true;
    result.afterVersion = owner_->version_;
    for (const auto& edit : changed) result.changedEntities.push_back(edit.id);
    return result;
}

void AuthoringSceneDocument::Transaction::rollback() noexcept {
    closed_ = true;
    edits_.clear();
}

void AuthoringSceneDocument::pushHistory(std::vector<HistoryEntry>& history, HistoryEntry entry) {
    history.push_back(std::move(entry));
    if (history.size() > 64) history.erase(history.begin());
}

bool AuthoringSceneDocument::applyHistory(const HistoryEntry& entry, bool useAfter, std::vector<HistoryEntry>& destination,
                                          std::uint64_t expectedVersion) {
    if (expectedVersion && expectedVersion != version_) return false;
    for (const auto& edit : entry.edits) {
        if (!find(edit.id)) return false;
    }
    for (const auto& edit : entry.edits) findMutable(edit.id)->local = useAfter ? edit.after : edit.before;
    pushHistory(destination, entry);
    ++version_;
    return true;
}

bool AuthoringSceneDocument::undo(std::uint64_t expectedVersion) {
    if (undo_.empty() || (expectedVersion && expectedVersion != version_)) return false;
    const auto entry = undo_.back();
    if (!applyHistory(entry, false, redo_, expectedVersion)) return false;
    undo_.pop_back();
    return true;
}

bool AuthoringSceneDocument::redo(std::uint64_t expectedVersion) {
    if (redo_.empty() || (expectedVersion && expectedVersion != version_)) return false;
    const auto entry = redo_.back();
    if (!applyHistory(entry, true, undo_, expectedVersion)) return false;
    redo_.pop_back();
    return true;
}

Json AuthoringSceneDocument::transformToJson(const SceneTransform& transform) {
    validateTransform(transform);
    return Json{{"translation", arrayToJson(transform.translation)},
                {"rotation", arrayToJson(transform.rotation)},
                {"scale", arrayToJson(transform.scale)}};
}

SceneTransform AuthoringSceneDocument::transformFromJson(const Json& value) {
    requireObject(value, "transform");
    if (!value.contains("translation") || !value.contains("rotation") || !value.contains("scale"))
        throw SceneDocumentError("Transform is missing a field");
    SceneTransform result{arrayFromJson<3>(value.at("translation"), "translation"),
                          arrayFromJson<4>(value.at("rotation"), "rotation"),
                          arrayFromJson<3>(value.at("scale"), "scale")};
    validateTransform(result);
    result.rotation = normalize(result.rotation);
    return result;
}

Json AuthoringSceneDocument::entityToJson(const SceneEntity& entity) {
    if (!entity.id) throw SceneDocumentError("Entity id is invalid");
    validateName(entity.name);
    validateReference(entity.mesh); validateReference(entity.material); validateReference(entity.collider);
    Json tags = Json::array();
    for (const auto& tag : entity.tags) { if (tag.empty() || tag.size() > 64) throw SceneDocumentError("Entity tag is invalid"); tags.push_back(tag); }
    return Json{{"id", entity.id}, {"name", entity.name}, {"parent", entity.parent ? Json(*entity.parent) : Json(nullptr)},
                {"transform", transformToJson(entity.local)},
                {"components", Json{{"mesh", entity.mesh}, {"material", entity.material}, {"collider", entity.collider},
                                      {"lod", entity.lod}, {"visible", entity.visible}}},
                {"tags", tags}};
}

Json AuthoringSceneDocument::toJson() const {
    Json serializedEntities = Json::array();
    for (const auto& entity : entities()) serializedEntities.push_back(entityToJson(entity));
    return Json{{"scene_id", sceneId_}, {"revision", version_}, {"next_entity_id", nextEntityId_}, {"entities", serializedEntities}};
}

Json AuthoringSceneDocument::inspectionJson(bool includeWorldTransforms) const {
    auto result = toJson();
    if (includeWorldTransforms) {
        for (auto& entity : result.at("entities")) {
            const auto id = entity.at("id").get<SceneEntityId>();
            entity["world_transform"] = transformToJson(worldTransform(id));
        }
    }
    return result;
}

AuthoringSceneDocument AuthoringSceneDocument::fromJson(const Json& payload) {
    requireObject(payload, "scene payload");
    if (!payload.contains("scene_id") || !payload.at("scene_id").is_string() ||
        !payload.contains("revision") || !payload.at("revision").is_number_unsigned() ||
        !payload.contains("next_entity_id") || !payload.at("next_entity_id").is_number_unsigned() ||
        !payload.contains("entities") || !payload.at("entities").is_array())
        throw SceneDocumentError("Scene payload is missing a required field");
    AuthoringSceneDocument result(payload.at("scene_id").get<std::string>());
    result.version_ = payload.at("revision").get<std::uint64_t>();
    result.nextEntityId_ = payload.at("next_entity_id").get<std::uint64_t>();
    if (!result.version_ || !result.nextEntityId_ || payload.at("entities").size() > 65536)
        throw SceneDocumentError("Scene payload bounds are invalid");
    std::unordered_set<SceneEntityId> ids;
    for (const auto& value : payload.at("entities")) {
        requireObject(value, "entity");
        if (!value.contains("id") || !value.at("id").is_number_unsigned() || !value.at("id").get<SceneEntityId>() ||
            !value.contains("name") || !value.at("name").is_string() || !value.contains("transform"))
            throw SceneDocumentError("Entity is missing a required field");
        SceneEntity entity;
        entity.id = value.at("id").get<SceneEntityId>();
        if (!ids.insert(entity.id).second) throw SceneDocumentError("Duplicate entity id");
        entity.name = value.at("name").get<std::string>(); validateName(entity.name);
        if (value.contains("parent") && !value.at("parent").is_null()) {
            if (!value.at("parent").is_number_unsigned() || !value.at("parent").get<SceneEntityId>()) throw SceneDocumentError("Invalid entity parent");
            entity.parent = value.at("parent").get<SceneEntityId>();
        }
        entity.local = transformFromJson(value.at("transform"));
        if (value.contains("components")) {
            requireObject(value.at("components"), "components");
            const auto& components = value.at("components");
            if (components.contains("mesh")) entity.mesh = components.at("mesh").get<std::string>();
            if (components.contains("material")) entity.material = components.at("material").get<std::string>();
            if (components.contains("collider")) entity.collider = components.at("collider").get<std::string>();
            if (components.contains("lod")) entity.lod = components.at("lod").get<std::int32_t>();
            if (components.contains("visible")) entity.visible = components.at("visible").get<bool>();
            validateReference(entity.mesh); validateReference(entity.material); validateReference(entity.collider);
        }
        if (value.contains("tags")) {
            if (!value.at("tags").is_array() || value.at("tags").size() > 64) throw SceneDocumentError("Invalid entity tags");
            for (const auto& tag : value.at("tags")) { const auto text = tag.get<std::string>(); if (text.empty() || text.size() > 64) throw SceneDocumentError("Invalid entity tag"); entity.tags.push_back(text); }
        }
        result.entities_.push_back(std::move(entity));
    }
    for (const auto& entity : result.entities_) {
        if (entity.parent && !result.find(*entity.parent)) throw SceneDocumentError("Entity parent does not exist");
        result.worldTransform(entity.id);
    }
    if (!result.entities_.empty()) {
        const auto largest = std::max_element(result.entities_.begin(), result.entities_.end(),
                                              [](const auto& a, const auto& b) { return a.id < b.id; });
        if (result.nextEntityId_ <= largest->id) throw SceneDocumentError("Next entity id must remain monotonic");
    }
    return result;
}

void AuthoringSceneDocument::save(const std::filesystem::path& path) const {
    writeDocument(path, "engine.authoring_scene", toJson(), schemaVersion);
}

AuthoringSceneDocument AuthoringSceneDocument::load(const std::filesystem::path& path) {
    return fromJson(readDocument(path, "engine.authoring_scene", schemaVersion));
}

} // namespace engine

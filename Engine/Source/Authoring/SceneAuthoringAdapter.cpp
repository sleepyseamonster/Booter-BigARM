#include "Authoring/SceneAuthoringAdapter.h"

#include <stdexcept>

namespace engine {
namespace {

void requireObject(const AuthoringJson& value, const char* label) {
    if (!value.is_object()) throw SceneDocumentError(std::string("Invalid ") + label);
}

void requireCurrentVersion(const AuthoringSceneDocument& document, std::uint64_t expectedVersion, bool required) {
    if (required && expectedVersion == 0) throw SceneDocumentError("A scene version is required for mutation");
    if (expectedVersion && expectedVersion != document.version()) throw SceneDocumentError("Scene version mismatch");
}

SceneEntityId readEntityId(const AuthoringJson& value) {
    if (!value.is_number_unsigned() || !value.get<SceneEntityId>()) throw SceneDocumentError("Entity id is invalid");
    return value.get<SceneEntityId>();
}

} // namespace

SceneAuthoringAdapter::SceneAuthoringAdapter(AuthoringSceneDocument& document, std::size_t maxPending,
                                             std::size_t maxReceipts)
    : document_(document), operations_(maxPending, maxReceipts) {
    registerHandlers();
}

void SceneAuthoringAdapter::registerHandlers() {
    operations_.registerHandler("inspect_scene", [this](const auto& payload, auto expected, auto& resulting) {
        return inspectScene(payload, expected, resulting);
    });
    operations_.registerHandler("inspect_entity", [this](const auto& payload, auto expected, auto& resulting) {
        return inspectEntity(payload, expected, resulting);
    });
    operations_.registerHandler("apply_transaction", [this](const auto& payload, auto expected, auto& resulting) {
        return applyTransaction(payload, expected, resulting);
    });
}

AuthoringJson SceneAuthoringAdapter::inspectScene(const AuthoringJson& payload, std::uint64_t expectedVersion,
                                                 std::uint64_t& resultingVersion) {
    requireObject(payload, "inspect_scene payload");
    requireCurrentVersion(document_, expectedVersion, false);
    const bool includeWorld = !payload.contains("include_world_transforms") || payload.at("include_world_transforms").get<bool>();
    resultingVersion = document_.version();
    return document_.inspectionJson(includeWorld);
}

AuthoringJson SceneAuthoringAdapter::inspectEntity(const AuthoringJson& payload, std::uint64_t expectedVersion,
                                                  std::uint64_t& resultingVersion) {
    requireObject(payload, "inspect_entity payload");
    if (!payload.contains("id")) throw SceneDocumentError("inspect_entity requires id");
    requireCurrentVersion(document_, expectedVersion, false);
    const auto id = readEntityId(payload.at("id"));
    const auto* entity = document_.find(id);
    if (!entity) throw SceneDocumentError("Entity does not exist");
    resultingVersion = document_.version();
    return AuthoringJson{{"scene_version", document_.version()}, {"entity", AuthoringSceneDocument::entityToJson(*entity)},
                         {"world_transform", AuthoringSceneDocument::transformToJson(document_.worldTransform(id))}};
}

AuthoringJson SceneAuthoringAdapter::applyTransaction(const AuthoringJson& payload, std::uint64_t expectedVersion,
                                                      std::uint64_t& resultingVersion) {
    requireObject(payload, "apply_transaction payload");
    requireCurrentVersion(document_, expectedVersion, true);
    if (!payload.contains("operations") || !payload.at("operations").is_array() || payload.at("operations").empty() ||
        payload.at("operations").size() > 128)
        throw SceneDocumentError("apply_transaction requires 1 to 128 operations");
    auto transaction = document_.beginTransaction(expectedVersion);
    for (const auto& operation : payload.at("operations")) {
        requireObject(operation, "scene transaction operation");
        if (!operation.contains("type") || operation.at("type") != "set_transform" || !operation.contains("entity") ||
            !operation.contains("transform"))
            throw SceneDocumentError("Only set_transform is available in this transaction boundary");
        transaction.setTransform(readEntityId(operation.at("entity")), AuthoringSceneDocument::transformFromJson(operation.at("transform")));
    }
    const auto result = transaction.commit();
    if (!result.applied) throw SceneDocumentError(result.error.empty() ? "Scene transaction was rejected" : result.error);
    resultingVersion = result.afterVersion;
    AuthoringJson changed = AuthoringJson::array();
    for (const auto id : result.changedEntities) changed.push_back(id);
    return AuthoringJson{{"before_version", result.beforeVersion}, {"after_version", result.afterVersion},
                         {"changed_entities", changed}};
}

} // namespace engine

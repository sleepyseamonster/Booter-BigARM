#include "Authoring/SceneAuthoringAdapter.h"
#include "Persistence/JsonLifetime.h"

#include <stdexcept>

namespace engine {
namespace {

void requireObject(const AuthoringJson& value, const char* label) {
    if (!value.is_object()) throw SceneDocumentError(std::string("Invalid ") + label);
}

void requireFields(const AuthoringJson& value,std::initializer_list<const char*> allowed){
    for(const auto& entry:value.items())
        if(std::none_of(allowed.begin(),allowed.end(),[&](const char* key){return entry.key()==key;}))
            throw SceneDocumentError("Unknown operation field: "+entry.key());
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
    requireFields(payload,{"include_world_transforms"});
    requireCurrentVersion(document_, expectedVersion, false);
    const bool includeWorld = !payload.contains("include_world_transforms") || payload.at("include_world_transforms").get<bool>();
    resultingVersion = document_.version();
    return document_.inspectionJson(includeWorld);
}

AuthoringJson SceneAuthoringAdapter::inspectEntity(const AuthoringJson& payload, std::uint64_t expectedVersion,
                                                  std::uint64_t& resultingVersion) {
    requireObject(payload, "inspect_entity payload");
    requireFields(payload,{"id"});
    if (!payload.contains("id")) throw SceneDocumentError("inspect_entity requires id");
    requireCurrentVersion(document_, expectedVersion, false);
    const auto id = readEntityId(payload.at("id"));
    const auto* entity = document_.find(id);
    if (!entity) throw SceneDocumentError("Entity does not exist");
    resultingVersion = document_.version();
    PreparedJson out;
    out.value["scene_version"]=document_.version();
    out.value["entity"]=AuthoringSceneDocument::entityToJson(*entity);
    out.value["world_matrix"]=document_.worldMatrix(id);
    return std::move(out.value);
}

AuthoringJson SceneAuthoringAdapter::applyTransaction(const AuthoringJson& payload, std::uint64_t expectedVersion,
                                                      std::uint64_t& resultingVersion) {
    requireObject(payload, "apply_transaction payload");
    requireFields(payload,{"operations"});
    requireCurrentVersion(document_, expectedVersion, true);
    if (!payload.contains("operations") || !payload.at("operations").is_array() || payload.at("operations").empty() ||
        payload.at("operations").size() > 128)
        throw SceneDocumentError("apply_transaction requires 1 to 128 operations");
    auto transaction = document_.beginTransaction(expectedVersion);
    for (const auto& operation : payload.at("operations")) {
        requireObject(operation, "scene transaction operation");
        requireFields(operation,{"type","entity","transform"});
        if (!operation.contains("type") || !operation.at("type").is_string() ||
            operation.at("type").template get_ref<const std::string&>() != "set_transform" || !operation.contains("entity") ||
            !operation.contains("transform"))
            throw SceneDocumentError("Only set_transform is available in this transaction boundary");
        transaction.setTransform(readEntityId(operation.at("entity")), AuthoringSceneDocument::transformFromJson(operation.at("transform")));
    }
    auto prepared = transaction.prepare();
    const auto& result = prepared.result();
    PreparedJson receipt;
    receipt.value["before_version"]=result.beforeVersion;
    receipt.value["after_version"]=result.afterVersion;
    receipt.value["changed_entities"]=result.changedEntities;
    // All result allocation precedes adoption. Moving the JSON return value is nonthrowing.
    if (!prepared.apply()) throw SceneDocumentError("Scene changed during preparation", SceneErrorCode::Conflict);
    resultingVersion = result.afterVersion;
    return std::move(receipt.value);
}

} // namespace engine

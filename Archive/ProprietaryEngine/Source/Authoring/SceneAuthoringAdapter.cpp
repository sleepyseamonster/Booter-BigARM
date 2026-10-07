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
    for(const std::string type:{"apply_transaction","preview_transaction","undo","redo"})
        operations_.registerHandler(type, [this,type](const auto& payload, auto expected, auto& resulting) {
            auto result=execute(document_,type,payload,expected);
            resulting=document_.version();return result;
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

AuthoringJson SceneAuthoringAdapter::execute(AuthoringSceneDocument& document,const std::string& type,
                                             const AuthoringJson& payload,uint64_t expectedVersion) {
    requireObject(payload,"transaction payload");
    requireCurrentVersion(document,expectedVersion,true);
    const bool preview=type=="preview_transaction";
    auto finish=[&](AuthoringSceneDocument::PreparedEdit prepared,const std::vector<SceneEntityId>& created) {
        const auto& result=prepared.result();
        PreparedJson receipt;
        receipt.value["before_version"]=result.beforeVersion;
        receipt.value["after_version"]=result.afterVersion;
        receipt.value["changed_entities"]=result.changedEntities;
        receipt.value["created_entities"]=created;
        receipt.value["preview"]=preview;
        if(receipt.value.dump().size()>60*1024)throw SceneDocumentError("Transaction result too large",SceneErrorCode::Capacity);
        if(!preview&&!prepared.apply())throw SceneDocumentError("Scene changed during preparation",SceneErrorCode::Conflict);
        return std::move(receipt.value);
    };
    if(type=="undo"||type=="redo") {
        requireFields(payload,{});
        return finish(type=="undo"?document.prepareUndo(expectedVersion):document.prepareRedo(expectedVersion),{});
    }
    if(type!="apply_transaction"&&!preview)throw SceneDocumentError("Unsupported authoring command");
    requireFields(payload,{"operations"});
    if(!payload.contains("operations")||!payload.at("operations").is_array()||payload.at("operations").empty()||payload.at("operations").size()>128)
        throw SceneDocumentError("Transaction requires 1 to 128 operations");
    auto transaction=document.beginTransaction(expectedVersion);
    std::vector<SceneEntityId> created;
    for(const auto& op:payload.at("operations")) {
        requireObject(op,"transaction operation");
        const auto& kind=op.at("type").get_ref<const std::string&>();
        auto entity=[&]{return readEntityId(op.at("entity"));};
        auto parent=[&]()->std::optional<SceneEntityId>{return !op.contains("parent")||op.at("parent").is_null()?std::nullopt:std::optional<SceneEntityId>{readEntityId(op.at("parent"))};};
        if(kind=="create") {
            requireFields(op,{"type","name","parent"});
            created.push_back(transaction.createEntity(op.at("name").get<std::string>(),parent()));
        } else if(kind=="duplicate") {
            requireFields(op,{"type","entity"});created.push_back(transaction.duplicate(entity()));
        } else if(kind=="delete") {
            requireFields(op,{"type","entity"});transaction.eraseEntity(entity());
        } else if(kind=="set_transform") {
            requireFields(op,{"type","entity","transform"});
            transaction.setTransform(entity(),AuthoringSceneDocument::transformFromJson(op.at("transform")));
        } else if(kind=="set_world_transforms") {
            requireFields(op,{"type","transforms"});
            if(!op.contains("transforms")||!op.at("transforms").is_array()||op.at("transforms").empty()||op.at("transforms").size()>AuthoringSceneDocument::maxEntities)
                throw SceneDocumentError("World transform edit requires a bounded nonempty selection");
            std::vector<std::pair<SceneEntityId,AffineMatrix>> transforms;
            transforms.reserve(op.at("transforms").size());
            for(const auto& item:op.at("transforms")) {
                requireObject(item,"world transform item");requireFields(item,{"entity","world_matrix"});
                if(!item.contains("entity")||!item.contains("world_matrix")||!item.at("world_matrix").is_array()||item.at("world_matrix").size()!=16)
                    throw SceneDocumentError("World transform item is invalid");
                AffineMatrix matrix{};
                for(size_t i=0;i<matrix.size();++i) {
                    const auto& value=item.at("world_matrix").at(i);
                    if(!value.is_number())throw SceneDocumentError("World matrix values must be numeric");
                    matrix[i]=value.get<double>();
                }
                validateRenderAffine(matrix);
                transforms.emplace_back(readEntityId(item.at("entity")),matrix);
            }
            transaction.setWorldTransforms(transforms);
        } else if(kind=="reparent") {
            requireFields(op,{"type","entity","parent","preserve_world"});
            transaction.reparent(entity(),parent(),op.value("preserve_world",true));
        } else if(kind=="metadata") {
            requireFields(op,{"type","entity","mesh","material","collider","lod","visible","tags"});
            const auto& lod=op.at("lod");
            if(!lod.is_number_integer()||lod.get<double>()<0||lod.get<double>()>31)throw SceneDocumentError("LOD must be an integer in 0..31");
            transaction.setMetadata(entity(),op.at("mesh").get<std::string>(),op.at("material").get<std::string>(),
                op.at("collider").get<std::string>(),lod.get<int32_t>(),op.at("visible").get<bool>(),op.at("tags").get<std::vector<std::string>>());
        } else throw SceneDocumentError("Unsupported transaction operation: "+kind);
    }
    return finish(transaction.prepare(),created);
}

} // namespace engine

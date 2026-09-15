#include "Authoring/SceneAuthoringAdapter.h"

#include <cmath>
#include <filesystem>
#include <iostream>
#include <stdexcept>

using namespace engine;

void require(bool value, const char* message) {
    if (!value) throw std::runtime_error(message);
}

template <class Function>
void rejects(Function&& function) {
    bool caught = false;
    try { function(); } catch (const std::exception&) { caught = true; }
    require(caught, "Expected rejection");
}

SceneTransform transform(float x, float y, float z) {
    SceneTransform value;
    value.translation = {x, y, z};
    return value;
}

int main(int argc, char** argv) {
    try {
        require(argc == 2, "Expected test output directory");
        const std::filesystem::path root = argv[1];
        std::filesystem::create_directories(root);

        AuthoringSceneDocument document("authoring-fixture");
        const auto parent = document.createEntity("Parent");
        const auto child = document.createEntity("Child", parent);
        document.updateEntityMetadata(parent, "content://mesh/parent", "content://material/stone", "", 0, true,
                                      {"authored", "fixture"});
        auto parentTransform = transform(10.0F, 0.0F, 0.0F);
        auto childTransform = transform(0.0F, 2.0F, 0.0F);
        auto seed = document.beginTransaction(document.version());
        seed.setTransform(parent, parentTransform);
        seed.setTransform(child, childTransform);
        const auto seeded = seed.commit();
        require(seeded.applied && seeded.changedEntities.size() == 2, "Initial transform transaction");
        require(std::abs(document.worldTransform(child).translation[0] - 10.0F) < 0.001F &&
                    std::abs(document.worldTransform(child).translation[1] - 2.0F) < 0.001F,
                "Deterministic hierarchy world transform");

        const auto beforeStale = document.worldTransform(child);
        auto stale = document.beginTransaction(document.version() - 1);
        stale.setTransform(child, transform(3.0F, 4.0F, 5.0F));
        const auto staleResult = stale.commit();
        require(!staleResult.applied && document.worldTransform(child) == beforeStale, "Stale transaction is atomic");

        const auto transactionVersion = document.version();
        auto edit = document.beginTransaction(transactionVersion);
        edit.setTransform(child, transform(3.0F, 4.0F, 5.0F));
        const auto committed = edit.commit();
        require(committed.applied && document.version() == transactionVersion + 1, "Committed transform increments revision");
        require(document.undo(document.version()) && document.worldTransform(child) == beforeStale, "Undo restores prior transform");
        require(document.redo(document.version()) && document.worldTransform(child).translation == std::array<float, 3>{13.0F, 4.0F, 5.0F},
                "Redo reapplies transform");
        const auto rollbackVersion = document.version();
        auto rolledBack = document.beginTransaction(rollbackVersion);
        rolledBack.setTransform(child, transform(99.0F, 99.0F, 99.0F));
        rolledBack.rollback();
        require(document.version() == rollbackVersion && document.worldTransform(child).translation == std::array<float, 3>{13.0F, 4.0F, 5.0F},
                "Rollback leaves document unchanged");

        const auto path = root / "authoring-scene.json";
        document.save(path);
        const auto loaded = AuthoringSceneDocument::load(path);
        require(loaded.toJson() == document.toJson(), "Authoring scene persistence roundtrip");
        const Json malformedCycle = Json{{"scene_id", "bad"}, {"revision", 1}, {"next_entity_id", 3},
                                          {"entities", Json::array({
                                              Json{{"id", parent}, {"name", "Parent"}, {"parent", child},
                                                   {"transform", AuthoringSceneDocument::transformToJson(parentTransform)}},
                                              Json{{"id", child}, {"name", "Child"}, {"parent", parent},
                                                   {"transform", AuthoringSceneDocument::transformToJson(childTransform)}}})}};
        rejects([&] { AuthoringSceneDocument::fromJson(malformedCycle); });

        SceneAuthoringAdapter adapter(document);
        const auto inspectId = adapter.enqueue({"inspect_scene", Json{{"include_world_transforms", true}}, 0});
        require(adapter.process(1) == 1, "Adapter processes inspection");
        const auto inspection = adapter.receipt(inspectId);
        require(inspection && inspection->status == AuthoringOperationStatus::Succeeded &&
                    inspection->result.at("entities").size() == 2,
                "AI scene inspection receipt");

        const auto expected = document.version();
        const Json applyPayload = Json{{"operations", Json::array({
            Json{{"type", "set_transform"}, {"entity", child},
                 {"transform", AuthoringSceneDocument::transformToJson(transform(7.0F, 8.0F, 9.0F))}}})}};
        const auto applyId = adapter.enqueue({"apply_transaction", applyPayload, expected});
        adapter.process(1);
        const auto applied = adapter.receipt(applyId);
        require(applied && applied->status == AuthoringOperationStatus::Succeeded &&
                    applied->result.at("changed_entities").at(0).get<SceneEntityId>() == child,
                "AI transform transaction receipt");
        const auto beforeFailedApply = document.toJson();
        const Json failedPayload = Json{{"operations", Json::array({
            Json{{"type", "set_transform"}, {"entity", parent},
                 {"transform", AuthoringSceneDocument::transformToJson(transform(1.0F, 2.0F, 3.0F))}},
            Json{{"type", "unsupported"}, {"entity", child}, {"transform", AuthoringSceneDocument::transformToJson(childTransform)}}})}};
        const auto failedApplyId = adapter.enqueue({"apply_transaction", failedPayload, document.version()});
        adapter.process(1);
        const auto failedApply = adapter.receipt(failedApplyId);
        require(failedApply && failedApply->status == AuthoringOperationStatus::Failed && document.toJson() == beforeFailedApply,
                "AI multi-operation failure is atomic");
        const auto staleApplyId = adapter.enqueue({"apply_transaction", applyPayload, expected});
        adapter.process(1);
        const auto staleApply = adapter.receipt(staleApplyId);
        require(staleApply && staleApply->status == AuthoringOperationStatus::Failed && document.toJson() == beforeFailedApply,
                "AI stale edit is rejected");

        std::cout << "PASS: authoring hierarchy, atomic transform transactions, undo/redo, persistence and AI adapter\n";
    } catch (const std::exception& error) {
        std::cerr << error.what() << '\n';
        return 1;
    }
}

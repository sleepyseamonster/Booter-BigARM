#pragma once

#include "Authoring/SceneDocument.h"
#include "Runtime/AuthoringOperations.h"

namespace engine {

// Local command adapter used by Codex today and transport bridges later. The
// adapter exposes inspection and atomic scene transactions without coupling
// authoring callers to the renderer, windowing layer, or a network protocol.
class SceneAuthoringAdapter {
public:
    explicit SceneAuthoringAdapter(AuthoringSceneDocument& document, std::size_t maxPending = 128,
                                   std::size_t maxReceipts = 512);

    std::uint64_t enqueue(AuthoringOperationRequest request) { return operations_.enqueue(std::move(request)); }
    std::size_t process(std::size_t budget = 8) { return operations_.process(budget); }
    std::optional<AuthoringOperationReceipt> receipt(std::uint64_t id) const { return operations_.receipt(id); }
    std::size_t pending() const { return operations_.pending(); }

private:
    AuthoringSceneDocument& document_;
    AuthoringOperations operations_;
    void registerHandlers();
    AuthoringJson inspectScene(const AuthoringJson& payload, std::uint64_t expectedVersion, std::uint64_t& resultingVersion);
    AuthoringJson inspectEntity(const AuthoringJson& payload, std::uint64_t expectedVersion, std::uint64_t& resultingVersion);
    AuthoringJson applyTransaction(const AuthoringJson& payload, std::uint64_t expectedVersion, std::uint64_t& resultingVersion);
};

} // namespace engine

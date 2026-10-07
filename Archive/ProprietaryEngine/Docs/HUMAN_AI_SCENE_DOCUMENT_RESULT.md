# Human-AI scene document result

**Audit qualification, 2026-09-15:** the [architecture audit](./ENGINE_ARCHITECTURE_AUDIT_2026-09-15.md) reproduced defects in hierarchy composition, allocation-failure atomicity, delete/history behavior and revision/schema/save bounds. The statements below describe the original first-pass intent and basic tests, not complete correctness. The adapter is in-process library code with no app host. R1 corrections now precede the previously proposed snapshot/gizmo work; no fixes are claimed by the audit.

Implemented 2026-09-14 as the first boundary of the [human and AI engine build-out plan](./HUMAN_AI_ENGINE_BUILDOUT_PLAN.md).

The engine now has a renderer-independent authoring domain under `Source/Authoring/`. `AuthoringSceneDocument` stores stable numeric entity IDs, names, parent relationships, local transforms, resolved world transforms, mesh/material/collider references, LOD intent, visibility and bounded authoring tags. Hierarchy evaluation is deterministic and rejects missing parents, cycles, non-finite values and invalid quaternions. Runtime and renderer handles are not serialized.

Transforms use translation, quaternion rotation (x/y/z/w) and scale. World composition is parent scale, rotation and translation followed by the child local transform. The representation is explicit so the future gizmo and runtime snapshot layers can convert it without making UI state authoritative.

`Transaction` is the first shared mutation seam. A caller supplies the document version, queues one or more `setTransform` edits, then commits atomically or rolls back. A stale version or missing entity leaves every entity unchanged. Successful edits increment the document revision and retain bounded undo/redo history. Metadata uses a validated `updateEntityMetadata` owner method; transform changes remain transaction-only.

Documents use the existing strict `engine.authoring_scene` JSON envelope and atomic replacement helper. The payload includes `scene_id`, `revision`, `next_entity_id` and sorted entities, with bounded sizes and validation on load. A rejected candidate cannot replace a valid in-memory or on-disk document.

`SceneAuthoringAdapter` registers three local commands on the existing bounded `AuthoringOperations` queue:

- `inspect_scene` returns the serialized scene and optional world transforms.
- `inspect_entity` returns one entity, its local data and its resolved world transform.
- `apply_transaction` accepts a bounded list of `set_transform` operations and an expected scene version, returning changed IDs and before/after versions.

The adapter processes only when its owner calls `process(budget)`, so AI reasoning, transport and file analysis cannot block the render or simulation loop. Receipts use the existing operation ID, status, resulting version and structured error fields.

## Verification

`cmake --preset core` configured the Mac core build. `cmake --build build/core -j2` completed, and `ctest --test-dir build/core --output-on-failure` passed all 15 configured tests, including the new `authoring_scene_document` suite. The focused suite covers hierarchy propagation, stale transaction rejection, commit/rollback, undo/redo, cycle rejection, persistence round-trip and the inspect/apply adapter receipts.

The next bounded implementation is Phase 3: extract an immutable `RenderSceneSnapshot` from this document and define the minimal Scene View selection/gizmo contract. Existing fixture and UI paths remain compatibility clients until that adapter is proven.

# Engine Implementation Plan — Draft V1

Created 2026-09-10 for the user's request to assess the complete engine, plan implementation, audit the plan and rewrite it. This is the retained first draft, not the active execution plan. The rewritten plan will remain at [FOUNDATION_PLAN.md](../FOUNDATION_PLAN.md).

## Task contract

Done for this planning task: a source-grounded system inventory, researched technical recommendations, a complete sequence through a Windows game candidate, a recorded audit of this draft, and a rewritten plan with concrete deliverables and validation. Work is limited to Engine documentation and directly useful planning verification. No engine runtime implementation, dependency installation, Unity edits, external publication or new game canon occurs in this task.

The current user and [direction](../DIRECTION.md) control scope. Native Engine source and dated evidence control what exists. Preserved game documents supply setting and BigARM constraints; their old top-down, Unity and canyon implementation choices do not control the new architecture. Target performance, final content scale, feel and release date are not yet established.

## Outcome

Build a game-specific C++ engine supporting third-person Booter traversal, a physically persistent BigARM, generated open wasteland, readable low-sun terrain/rocks, durable player changes, authored world constraints and practical content tools. Preserve Windows as product target and Mac as daily development while practical. The rock workbench remains the first procedural content application after the engine foundation.

The engine is complete enough for production when the intended game can be authored, played, saved, profiled, packaged and diagnosed through the same runtime. A rendered rock is one useful checkpoint, not the completion definition.

## Complete capability outline

1. Native platform/build: window, gamepad/keyboard/mouse, focus, resize/DPI, OS paths, reproducible dependency acquisition, Windows shader/build path and distributable package.
2. Runtime: fixed simulation timing, transient entities/components, transforms, stable logical IDs, command/event order, settings, diagnostics and worker-job boundaries.
3. Assets: source provenance, logical IDs, content hashes, import recipes, typed validation, shader/material/texture/mesh/skeleton/animation/audio cooking, cache invalidation, resource sharing and replacement.
4. Rendering: linear/HDR/display contract, directional shadows, PBR materials, normals, ambient/sky, terrain/rock layering, frustum culling, instancing, LOD, atmospheric dust/fog, effects, antialiasing and quality controls.
5. Physics and control: collision/query integration, capsule movement, slopes/steps, gravity, contact state, obstructed third-person camera, authored traversal constraints and per-agent dimensions.
6. Animation: imported skeleton/clip data, sampling/blending, skinning, locomotion states, root-motion ownership and event delivery; later IK and more complex transitions as gameplay requires.
7. Procedural world: versioned recipes, deterministic integer decisions, floating-point tolerances, terrain and rock geometry, material semantic fields, stable placement, shared borders and world-coordinate precision.
8. Streaming: priorities, cancellation, owner epochs, bounded CPU/GPU/collision/nav work, multiple anchors, cache residency and unload/reload.
9. Persistence: versioned world identity and deltas, atomic saves, backup/recovery, schema migration, inventory/entity/companion state and authored document saves.
10. Navigation and AI: local navigation tiles, agent-specific traversability, macro route graph, coarse travel, detailed re-entry, tasks, perception and failure handling.
11. Gameplay: input commands, interaction, salvage/depletion, finite cargo, survival pressure, crafting/progression, threats/combat and authored encounters tied to persistent IDs.
12. Audio and player UI: positional sounds, ambience, buses, event routing, minimal HUD/prompts, controller navigation, settings, localization-ready text and accessibility.
13. Authoring: shared-runtime workbench, recipe documents, undo/redo, asset browser, material inspection, placement constraints, scene/entity inspection and validation.
14. Shipping/quality: clean builds, automated CPU/integration/GPU checks, Windows measurements, profiling, save recovery, logs/crash reports, dependency notices, runtime packaging and release-candidate acceptance.

## Preferred technical direction

Retain C++20/CMake/SDL3/bgfx/bimg/ImGui and current pins. Own world policy, gameplay, resource lifetime, cooking semantics and authoring. Prefer Jolt for physics, EnTT for transient component storage, fastgltf/glTF for import, meshoptimizer for mesh cooking, ozz-animation for sampling/blending, Recast/Detour for local navigation, miniaudio for sound and RmlUi for player UI. Use explicit JSON documents through a mature parser rather than writing a parser. Add dependencies only with their first consuming subsystem and bounded compatibility/license evidence.

Use explicit ordered frame passes and forward opaque rendering initially; defer a general render graph, ray tracing, virtualized geometry and bindless pipelines until measured requirements justify them. Use an engine-owned fixed update and command boundaries; library ECS/physics objects are not durable world identity. Core gameplay starts in C++ with data-driven definitions, not a new scripting VM.

## First sequence

| Milestone | Deliverable | Completion evidence |
|---|---|---|
| M0 Existing baseline | Native fixture, inspector, geometry correctness, transferred source surfaces and texture research | Existing Mac receipts remain bounded historical proof |
| M1 Outdoor and runtime foundation | OR-2–OR-5, resource identities/ownership, texture cooking/loading, shared asset path, settings, useful metrics, Windows build checkpoint | Numerical/GPU color, shadow/material cases, asset reload/lifetime, source-preserving cook, repeatable fixture |
| M2 Traversable calibration space | Fixed update, game input, Jolt collision and player motor, follow/obstruction camera, entity storage, imported animated proxy | Slope/step/query checks, input focus/disconnect behavior, animation/skin orientation, character-scale review |
| M3 Rock authoring | Versioned recipes, deterministic generation, edit/save/undo, mesh/material/LOD and collision preview | Stable inputs/IDs, malformed input handling, near/all-side/far review, regeneration and memory measurements |
| M4 Persistent streamed wasteland | Terrain/rock placement, seam rules, bounded jobs, chunk lifetimes, durable world deltas and save recovery | Border, cancellation, memory plateau, origin shift, unload/restart/depletion and crash-recovery cases |
| M5 Companion and traversal simulation | Agent-specific navigation, route graph, BigARM loaded/coarse travel, task state and physical regroup | Separation/unloaded travel/re-entry/save continuity; no teleport fallback |
| M6 First survival expedition | Salvage/cargo, one pressure mechanic, one complementary companion action, basic threat, HUD/audio and save/load | Coherent outbound/regroup/return loop with durable consequences; user judges feel |
| M7 Production presentation/content | World constraints/landmarks, animation polish, dust/sky/effects, material detail, broader content tools, settings/localization/accessibility | Representative content import, long traversal, visual quality and readability review |
| M8 Performance and shipping candidate | Windows profiling/budgets, quality tiers, clean player package, save migration, diagnostics and notices | Native supported-PC evidence, install/update recovery, package checks, release decision remains with user |

Windows execution is introduced during M1 and repeated later; unavailable hardware is an explicit evidence gap. Content, performance and artistic scope are refined as representative slices exist. Every milestone keeps a runnable application and source-bound evidence rather than accumulating disconnected libraries.

## Execution model

The agent chooses technical details inside the accepted direction, works in coherent batches, validates, updates status and commits only owned files. It asks the user for product/creative decisions or external/destructive actions when actually required. Technical steps should not each become a new user decision. Preserve old evidence, distinguish tool success from game proof and record any failed gate.

## Initial review questions

Does the sequence expose camera/collision and BigARM architectural risk soon enough? Does persistence enter before irreversible IDs or game state are spread across code? Are resource budgets and readiness contracts specific enough? Can a future agent identify an unblocked task without reconstructing this conversation? Are gameplay, art dependencies and platform verification concrete enough to avoid an indefinitely growing engine project?

# Human and AI engine build-out plan

Updated 2026-09-14, America/Phoenix.

This is the focused execution plan for the new operating model: Codex is the primary authoring client, a human uses a small scene viewport for visual judgment and precise gizmo edits, and the same engine runtime powers playtesting and the standalone game. It is subordinate to [FOUNDATION_PLAN.md](./FOUNDATION_PLAN.md), which remains the master whole-engine roadmap, and [STATUS.md](./STATUS.md), which remains the live evidence handoff.

## Outcome

The engine is complete enough for this direction when it can:

1. Store an explicit, versioned authoring scene with stable entities, hierarchy, transforms and reusable components.
2. Accept the same validated scene operations from Codex and the human viewport.
3. Start an isolated play session from an authoring scene or procedural world profile.
4. Show that session in a Game View using the real player camera, input, simulation, collision, streaming, audio and persistence runtime.
5. Render authoring scenes and play sessions from runtime snapshots rather than fixture-only state.
6. Preserve manually authored examples as data that Codex can analyze into procedural constraints without silently turning examples into game canon.

The engine must remain useful with a minimal interface. A large editor, scripting language, visual material graph, multiplayer model or automatic procedural-world authoring system is not required for this outcome.

## Source of truth and boundaries

- Product direction: [DIRECTION.md](./DIRECTION.md).
- Whole-engine dependencies and platform gates: [FOUNDATION_PLAN.md](./FOUNDATION_PLAN.md) and [ENGINE_ROADMAP.json](./ENGINE_ROADMAP.json).
- Current implementation evidence: [STATUS.md](./STATUS.md).
- Existing operation seam: [AuthoringOperations.h](../Source/Runtime/AuthoringOperations.h).
- Existing shared player/runtime seam: [WorldSession.h](../Source/Game/WorldSession.h) and [Apps/Game/main.cpp](../Apps/Game/main.cpp).
- Existing renderer and material ownership: [Renderer.h](../Source/Rendering/Renderer.h), [RenderModel.h](../Source/Rendering/RenderModel.h), and the asset catalog/resource owner.

Unity remains reference-only. Terrain, rocks, generators, canyons and final game geography are consumers of this architecture, not prerequisites for the first authoring/runtime layers. The UI agent owns presentation widgets, but never owns scene truth, simulation truth or persistence.

## Canonical state model

Keep these records separate and explicit:

| Record | Purpose | Can it be changed during play? |
|---|---|---|
| `AuthoringSceneDocument` | Human/Codex-authored entities, hierarchy, transforms, component references and authoring metadata | Only through an explicit authoring transaction; play does not mutate it implicitly |
| `WorldProfile` | Seed, generator versions, procedural recipes and authored constraints for generated worlds | Versioned authoring data; selected when a session starts |
| `PlaySessionSnapshot` | Runtime copy/overlay of an authoring scene or generated world | Yes, through simulation and player commands; disposable by default |
| `RuntimeDelta` | Deliberate gameplay changes such as depletion, damage, inventory or removed objects | Yes; persisted only through an explicit save transaction |
| `RenderSceneSnapshot` | Immutable frame-facing transforms, bounds, LOD/material references and visibility inputs | Rebuilt from authoritative state; never the owner of objects |

Renderer, physics, navigation, audio and UI handles are transient references. They must never become durable IDs or be written into authored documents or saves.

## Ordered implementation phases

### Phase 0 — Freeze the operating contract

Document the distinction between authoring state, world profile, play session, runtime delta and render snapshot. Define units, coordinate frames, stable ID namespaces, version rules, ownership and transaction boundaries. Mark `FixtureState` as diagnostic/view state, not the long-term scene model.

**Close when:** the contracts are written, linked from the master status, and no planned feature requires a second scene authority. Do not build new UI or generators in this phase.

### Phase 1 — Canonical scene document and hierarchy

Add a C++20 scene domain independent of bgfx, ImGui and Unity:

- Stable entity IDs and parent/child relationships.
- Local transform, resolved world transform and deterministic hierarchy evaluation.
- Mesh, material, collider, LOD and visibility component references.
- Authoring metadata/tags for anchors, sockets, exclusions, support, contact and variation.
- Bounded JSON serialization with schema/version, validation and atomic replacement.
- Explicit selection/query results that expose IDs and values without exposing renderer handles.

Use a flat, cache-friendly component store or equivalent owned structure. Do not add a general reflection system or scripting VM. Keep hierarchy mutations deterministic and reject cycles, missing parents, non-finite transforms and stale document versions.

**Proof:** core tests cover IDs, hierarchy order, transform propagation, invalid documents, stale versions, atomic save/reload and preservation of the previous valid document after a rejected candidate.

### Phase 2 — Transform transactions, history and selection

Build the domain operations that both AI and UI will call:

- Inspect scene/entity/component.
- Create, duplicate, delete and restore entities.
- Set translation, rotation and scale in local or world space.
- Parent/reparent with cycle and preserve-world-transform rules.
- Group/ungroup, set visibility and set authoring tags.
- Begin/commit/rollback a transaction.
- Undo/redo committed transactions.

A drag is one transaction, not hundreds of permanent edits. Each receipt contains operation ID, expected version, resulting version, changed entity IDs and structured errors. Large operations are bounded and can be previewed before commit.

**Proof:** duplicate/reparent/undo/redo cases, stale transaction rejection, stable IDs after save/reload, and no partial mutation after a failed multi-entity operation.

### Phase 3 — Human Scene View contract

Give the UI agent a narrow viewport contract over Phase 2:

- Selection by ID and viewport ray/pick result.
- Translate, rotate and scale gizmos.
- World/local/pivot modes and bounded snapping.
- Multi-selection and parent-aware manipulation.
- Camera orbit, pan, zoom and frame selection.
- Minimal outliner and transform readout.
- Clear selection, transaction and validation feedback.

The UI submits domain transactions and reads snapshots. It does not directly edit ECS components, renderer resources or save files. Game View hides authoring gizmos and uses a separate input context.

**Proof:** a manual transform round-trip produces the same document as an equivalent Codex operation; selection/gizmo changes survive save/reload; UI focus does not leak into gameplay input.

### Phase 4 — Play session and Game View

Formalize the existing player/runtime path into a shared play-session service:

- Start from an authoring scene or selected `WorldProfile`.
- Clone immutable authoring data into a `PlaySessionSnapshot`.
- Select seed, content versions, player spawn and camera mode explicitly.
- Run fixed-tick simulation, input, collision, gravity, streaming, animation, audio and save/load through the existing shared runtime.
- Pause, resume, restart and stop without mutating the authoring scene.
- Keep temporary runtime deltas separate; require an explicit save/apply action to persist them.

Expose Game View as a second viewport over this session. The standalone player application and the in-workbench Game View must use the same session and renderer services; one is not a special preview implementation.

**Proof:** start/stop/restart isolation, deterministic initial snapshot, player camera/input ownership, collision and streaming readiness, runtime changes discarded on stop, explicit save preserving only intended deltas, and identical render/resource paths in Workbench and Player.

### Phase 5 — AI command bridge

Connect Codex to the same operations without requiring UI automation. Begin with a local, bounded adapter using JSON requests and receipts; add a transport only when an actual external process requires it.

Required command families:

- `inspect_scene`, `inspect_entity`, `inspect_runtime`.
- `apply_transaction` and `preview_transaction`.
- `save_scene`, `load_scene`, `export_scene`.
- `start_play_session`, `pause_play_session`, `restart_play_session`, `stop_play_session`.
- `capture_frame`, `capture_scene_snapshot`, `collect_runtime_metrics`.

Every request has an operation ID, expected domain/session version, bounded payload, owner, cancellation policy and structured completion receipt. AI reasoning, network activity and file analysis never block the real-time loop. Human UI commands and AI commands share validation, history, persistence and error handling.

**Proof:** the same transform transaction applied through the local adapter and the viewport yields identical serialized state; malformed, stale, unauthorized-domain and cancelled requests leave state unchanged; long-running work reports progress without blocking frames.

### Phase 6 — Runtime snapshot rendering and representative workload

Replace fixture-only submission as the primary path with an extraction step:

`Authoritative scene/session state → RenderSceneSnapshot → visibility/LOD → material/resource bindings → render passes`.

The snapshot contains only frame-safe data: transforms, bounds, mesh/material IDs, LOD choice, skin pose references and visibility flags. Resource owners resolve IDs to GPU handles. Keep the current forward passes and G-buffer/AO/contact infrastructure; do not introduce a general render graph without a demonstrated need.

Create one generic representative authoring/playtest workload containing repeated static meshes, parented groups, multiple material instances, several LODs, one skinned object, a player camera and streamed instances. It is an architecture fixture, not final game content.

**Proof:** snapshot extraction matches authoritative transforms, culling/LOD choices are stable, resources are shared and released, frame-facing data does not mutate mid-frame, and CPU/GPU/resource telemetry is recorded at bounded p50/p95/p99 samples.

### Phase 7 — Authored examples and procedural constraint capture

Add a versioned authored-composition record that can reference a scene document without baking it into the generated world:

- Relative transforms and hierarchy relationships.
- Semantic tags, anchors, sockets, exclusions and support/contact relationships.
- Spacing/orientation/scale ranges and allowed variation.
- Material/archetype references and LOD intent.
- Source scene ID, authoring version and review status.

Codex can analyze several approved examples and produce a `ConstraintRecipe` proposal. The proposal is previewed against new variants, validated, versioned and explicitly promoted. Hand-built examples remain available as references. No automatic rule promotion and no silent world rewrite.

**Proof:** capture/reload preserves relationships and IDs; proposals are reproducible from the same examples; invalid constraints are rejected; generated previews never overwrite source scenes; accepted recipe versions are distinguishable from drafts.

### Phase 8 — Generator and world integration

Only after the authoring and runtime layers work, connect terrain, rocks, ruins and other generators through the `ConstraintRecipe` and `WorldProfile` interfaces. Generated objects receive stable IDs from world identity and generator version. Streaming, collision, navigation and persistence consume the same generated result and readiness states.

Manual set construction remains a source of constraints and reusable authored archetypes. It does not force a permanently authored landscape or prevent later procedural regeneration.

**Proof:** same seed/profile/version produces the same authored IDs and constraints; generated render/collision/nav outputs agree on source version; unload/reload and save/restart preserve intended deltas; rejected or superseded jobs cannot publish stale results.

### Phase 9 — Platform and production hardening

After the shared architecture has a representative workload, perform Windows build/GPU/input proof, quality-tier tuning, package/recovery validation, long-run resource checks and support diagnostics. Do not move to Windows merely because a scene view exists; move when the next required proof depends on the target backend or hardware.

## Runtime and performance rules

- Codex work is asynchronous and budgeted; the fixed simulation/render loop never waits for model reasoning, network calls, asset analysis or disk scans.
- Authoritative state uses versioned transactions and immutable frame snapshots.
- Expensive generation, cooking, collision preparation, asset decode and uploads use bounded queues, cancellation and epochs.
- The renderer may degrade optional detail or delay adoption, but cannot discard authored/runtime deltas or resurrect stale objects.
- Measure a representative workload, not a fixture frame interval. Record backend, drawable size, active instances, LOD/material counts, resource high-water marks and CPU/GPU stage timing.

## Plan audit and rewrite decisions

The initial editor-oriented interpretation was revised because it would have created unnecessary systems and ambiguous ownership:

1. The authoritative object model is now a native scene document, not UI widgets or `FixtureState`.
2. Scene View and Game View are separate clients of one runtime, not two renderers or two world models.
3. Gizmos are precise human input; Codex operations are the primary bulk authoring path. Both use identical transactions and history.
4. Playtesting starts from an isolated snapshot so manual experiments cannot corrupt authored source data.
5. Manual set building precedes procedural inference; examples become constraints only through explicit capture and promotion.
6. Scalable rendering means frame-safe snapshot extraction, visibility, LOD and resource ownership around representative data. It does not mean building a giant editor or general render graph immediately.
7. AI transport is intentionally local and bounded first. A network protocol, scripting VM or service layer is added only when a real integration requires it.
8. Terrain, rock and ruin generators are downstream consumers. Their implementation is not allowed to redefine scene authority or block the generic authoring/playtest loop.

## First implementation boundary

The next coherent engine batch is **Phase 1 plus the minimum of Phase 2**: add `AuthoringSceneDocument`, hierarchy/transform storage, versioned serialization and one atomic `set_transform` transaction with undo/rollback. Stop when Codex and a small test client can inspect and change a scene without touching renderer or UI state. Then integrate the UI gizmo contract and Play Session lifecycle in the following bounded batches.

Do not begin generator-specific constraints, permanent world layouts, a large asset browser or additional visual effects before that boundary is complete. The result must remain a usable Scene View while the Game View/session work is added.

# Engine architecture audit — 2026-09-15

Audited source: `f6dc2ad`, on the development Mac. This is a source and bounded CPU audit, with fresh native tests and selected primary-source checks. It is not a new visual acceptance, Windows, gameplay or performance certification. The root Unity modifications were present before this audit and are outside its scope.

The [bounded lifecycle follow-up](./ENGINE_LIFECYCLE_AUDIT_2026-09-15.md) adds A12–A15: incompatible snapshot preservation, sleeping-body activation after support edits, pause presentation and allocation-free teardown. It retains R1 as the next batch and folds these corrections into R4. Together the audits are sufficient to begin corrections; another broad survey is not a prerequisite.

## Judgment

Keep the current engine and library choices. The intended ownership model is sound: native authoring documents, a simulation owner, bounded jobs, resource owners, explicit persistent identity and separate applications. Several existing implementations follow it well. A rewrite or new renderer library would not address the most urgent defects.

The implementation is a collection of useful connected prototypes plus an unconnected authoring library, rather than a complete generic authoring/playtest engine. Correct the scene authority and render-input defects before building gizmos on top of them. Then connect one neutral authored scene through rendering and an isolated play session. More subsystem features should follow that integration.

The prior scene-document closeout was too broad. Its basic tests pass, but full hierarchy correctness, exception-safe transactions and scalable/saveable documents are not established. The local adapter is a C++ interface exercised by tests; neither application instantiates it and there is no external command entry point for editing a running scene yet.

## Scope and evidence

Reviewed the CMake target graph and test registration; application loops; authoring documents/commands/history; simulation/input/physics ownership; region scheduling/adoption; renderer resource lifetime and passes; model/material/texture paths; animation/audio; document/world persistence; roadmap and current handoffs. Generator algorithms and game content were not audited for artistic quality or parity.

- Fresh build: `cmake --build build/core --target engine_scene_tests engine_core_tests -j2` passed. This rebuilds the directly audited scene/core targets, not every renderer binary.
- Existing native suite: `ctest --test-dir build/core --output-on-failure` passed **15/15 registered tests**.
- New observational probes: [source](../Tests/ArchitectureAuditProbe.cpp), [runner](../Tools/audit_architecture.py), [recorded observations and input hashes](../Evidence/ARCH-2026-09-15/observations.json). Eight issue probes reproduced, plus one bounded hierarchy timing series. `reproduced_issue: true` means a defect was observed; runner exit zero means evidence collection succeeded.
- The dependency checker reports no graph errors, 40 packages and 24 capabilities. It reports P22/P23/P25 as locally ready; that is graph/file-presence evidence, not the current authoring sequence or implementation acceptance.
- [Verification record](../Evidence/ARCH-2026-09-15/verification.json) captures the test inventory and source review fingerprints.
- The workspace document checker reports 44 existing reference-import link/whitespace errors, all under `Research/UnityReference/`; none involve the task documents. Those preserved reference copies remain unchanged. This is a separate documentation-routing gap, not a native engine test failure.
- Existing Metal evidence remains historical and scoped to its original fixtures. No application was launched or changed during this audit.

## Findings requiring correction

Priority P1 means correct before exposing the affected path to regular authoring. P2 means a real correctness, scaling or integration issue to address in the next owning batch. All findings below remain open; this audit changes guidance and adds diagnostic probes, not runtime behavior.

### A01 — P1: hierarchy composition gives incorrect world transforms

Source: [SceneDocument.cpp](../Source/Authoring/SceneDocument.cpp), `worldTransformRecursive`, lines 142–158.

The function multiplies scale components and quaternion rotations separately. Nonuniform parent scale followed by child rotation can produce shear, which this resolved TRS representation discards. This also corrupts deeper child positions. The probe uses parent scale `(2,1,1)`, child rotation 90 degrees about Z, and a grandchild translated one unit along X: expected world position `(0,1,0)`, actual approximately `(0,2,0)`.

Keep local TRS for editing, but compute derived world affine matrices by parent-matrix × local-matrix multiplication. Define whether preserve-world reparenting may retain affine transforms or reject a nonrepresentable local result. Include inverse-transpose normals and determinant-aware mirrored winding in the renderer contract. Do not silently decompose away shear. This follows the transform model in the [glTF specification, transformations](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#transformations).

Close with an independent matrix oracle covering rotated/scaled parent chains, mirrored transforms, nonrepresentable reparent results, and singular/overflow rejection.

### A02 — P1: transaction failure does not reliably preserve state

Source: [SceneDocument.cpp](../Source/Authoring/SceneDocument.cpp), `Transaction::commit` lines 179–205 and `applyHistory` lines 218–227; [SceneAuthoringAdapter.cpp](../Source/Authoring/SceneAuthoringAdapter.cpp), lines 79–85.

Commit writes live transforms before allocating the history entry and changed-ID receipt. Allocation-failure injection reproduced changed data with the old revision and no undo record, and another failure after the revision already advanced. Undo/redo likewise allocate destination history after changing live values. The adapter also constructs JSON after commit, so an exception can be reported as a failed request after the edit took effect.

A separate direct-domain probe queues a valid edit, catches a later invalid-entity error, then successfully commits the valid prefix. The current adapter avoids that case by abandoning its local transaction on exception, but the public transaction does not become aborted. Human and AI callers therefore do not yet have one reliable failure contract.

Prepare validation, affected transforms, inverse history and success receipt before a nonthrowing adoption step. Abort a transaction after a validation failure, or explicitly redesign and document recoverable staging semantics; the plan currently promises all-or-nothing edits. Distinguish an applied operation whose delivery failed from a rejected mutation. Require an exact revision for undo/redo as well as apply when exposed through the command boundary.

Close with allocation-failure probes for commit/undo/redo and receipt creation, plus multi-edit failure tests proving state, revision and history remain coherent.

### A03 — P1: structural edits can permanently block undo

Source: [SceneDocument.cpp](../Source/Authoring/SceneDocument.cpp), `eraseEntity`, `updateEntityMetadata`, `applyHistory` and `undo`.

Create/delete/metadata changes bypass transaction history. Deleting an entity referenced by the latest transform entry leaves `canUndo()` true while `undo()` repeatedly returns false; earlier edits to surviving entities become inaccessible. The probe edits A, edits B, deletes B, then cannot undo A or restore B.

Bring create/delete/restore and metadata into the same transaction/history model before user manipulation. Preserve ID allocation high-water marks through undo and reload; restored entities must not collide with newly created identities. Until structural undo is implemented, an explicit history barrier is safer than leaving poisoned entries, but it does not meet the complete Phase 2 requirement.

Close with edit/delete/undo/redo, subtree delete/restore, duplicate IDs and mixed metadata/transform transactions.

### A04 — P1: accepted authoring state can become invalid or impossible to save

Source: [SceneDocument.cpp](../Source/Authoring/SceneDocument.cpp), validation, mutation and serialization methods; [Document.h](../Source/Persistence/Document.h), `documentLimit`.

Four demonstrated inconsistencies:

- A loaded revision of `UINT64_MAX` is accepted. The next transform wraps it to zero, applies the change, and produces a failed adapter receipt because the version went backwards.
- Individually finite translations can compose to infinity. Commit accepts the result; subsequent inspection throws.
- A fractional LOD value is silently truncated (`1.5` becomes `1`), and unknown payload fields are silently discarded on reserialization.
- Creating 2,500 small entities succeeds, but their actual pretty-printed envelope is **1,431,570 bytes**, beyond the **1,048,576-byte** writer limit. In-memory creation has no count bound; loading separately allows up to 65,536 entities. The accepted data domain is larger than its persistence domain.

Use consistent schema/type/range validation across creation, mutations, load and save; guard version exhaustion before mutation; validate derived matrices before adoption. Choose an explicit unknown-field policy. Match scene capacity to the supported durable format, initially with a bounded document budget and later independently saveable scene partitions if needed. Do not simply remove all size limits.

Close with maximum revisions, finite-composition overflow, strict integer fields, unsupported fields, and largest-admitted-scene round-trip tests that retain the previous valid file on rejection.

### A05 — P2: command budgets do not bound authoring latency or memory

Source: [SceneDocument.cpp](../Source/Authoring/SceneDocument.cpp), `find`, recursive hierarchy resolution and `inspectionJson`; [AuthoringOperations.h](../Source/Runtime/AuthoringOperations.h), `process` and receipt storage.

Inspection copies and sorts entities, then repeatedly traverses parent chains using linear searches and cycle-stack scans. A full deep-chain inspection has cubic worst-case work. One optimized local run measured 128 entities at **1.51 ms**, 512 at **21.56 ms**, and 1,024 at **128.12 ms**. These are synthetic depth probes, not representative game frame benchmarks.

`process(1)` limits operation count, not the duration of that operation. Results have no byte limit or pagination, and up to 512 full receipts can be retained. The queue is an owner-thread container, not a thread-safe external ingress. Copies/moves of `SceneAuthoringAdapter` retain handlers capturing the original `this`; the class should prohibit copying/moving or rebind safely before becoming a session-owned service.

Use ID-to-index lookup, iterative parent-first evaluation cached by revision, bounded depth, paged inspection and aggregate receipt-byte limits. Put external ingress on a bounded handoff queue; execute small commits on the owner and serialize immutable snapshots off the frame-critical path. Bind requests to document identity plus a session/load epoch so loading another same-revision scene cannot accept an old command accidentally.

Close with bounded flat/deep hierarchies, stale-load commands, paged output, ownership/lifetime checks and measured extraction/commit budgets. A small representative timing check is enough; no prolonged stress campaign is needed now.

### A06 — P1: contact prepass does not bind its normal matrix

Source: [Renderer.cpp](../Source/Rendering/Renderer.cpp), `prepassSubmit` lines 371–377; [vs_scene.sc](../Shaders/vs_scene.sc), [vs_skin.sc](../Shaders/vs_skin.sc), [fs_prepass.sc](../Shaders/fs_prepass.sc).

Both prepass vertex programs read `u_normalMatrix`, but `prepassSubmit` sets the model transform and skin palette only. The regular color submission binds the normal matrix. Prepass normals can therefore depend on stale uniform state rather than the submitted object's rotation/scale, and contact visibility consumes those normals.

Bind a correctly derived normal matrix for each prepass draw, preferably through shared per-draw preparation used by color and depth/normal passes. Source establishes the missing input; this audit does not claim a new captured pixel failure. Close with differently rotated and nonuniformly scaled generic objects, draw-order reversal and a prepass-normal image comparison.

### A07 — P2: UV normal mapping needs a correct tangent contract

Source: [vs_scene.sc](../Shaders/vs_scene.sc), line 11; [vs_skin.sc](../Shaders/vs_skin.sc), tangent output; [GltfImport.cpp](../Source/Assets/GltfImport.cpp), lines 92–97.

Tangents are transformed with the inverse-transpose normal matrix. Tangent directions should use the model's linear transform, then be orthogonalized against the transformed normal. For a diagonal tangent in a planar XY surface scaled 2× in X, the correct direction is proportional to `(2,1,0)`; the current path produces `(.5,1,0)`. Mirrored object transforms also need a handedness policy.

When imported tangents are absent, the importer chooses an arbitrary perpendicular to the normal without consulting UVs. That is sufficient for an untextured diagnostic, but is not a UV-derived normal-map basis. Preserve valid supplied tangents; generate a compatible basis from positions/normals/UVs, or reject normal mapping when that prerequisite is missing. The [glTF mesh guidance](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#meshes) recommends MikkTSpace generation for missing tangents.

Close with an asymmetric normal map on rotated/nonuniform/mirrored meshes and explicit absent-tangent behavior.

### A08 — P2: AO/depth and render-target costs need architectural correction

Source: [fs_display.sc](../Shaders/fs_display.sc), AO sampling and `sceneColor *= aoFactor`; [fs_scene.sc](../Shaders/fs_scene.sc), lines 292–303; [Renderer.cpp](../Source/Rendering/Renderer.cpp), `resizeTargets` lines 174–211 and projection setup.

Screen AO multiplies the already combined direct light, ambient light and fog. It therefore darkens direct sunlight and fog as well as ambient illumination. The material AO path applies to ambient; the screen AO path has different semantics. Move scene AO into indirect-light evaluation before fog, or explicitly preserve a separate indirect contribution for composition. [Filament's occlusion model](https://google.github.io/filament/main/filament.html#lighting/occlusion) explains this distinction.

Both scene and contact depth are hardware nonlinear depth copied into RGBA16F, then compared with fixed depth-space thresholds. Precision and physical radius vary with distance, near/far planes and projection. The AO offsets also use normalized screen radius without aspect correction. Define one depth convention, use adequate precision or reconstructed/linear view depth, and express radius/thickness in declared units before increasing sample count. Contact shadows need a thickness policy to avoid treating every foreground depth as an arbitrarily thick blocker.

The renderer allocates five full-resolution RGBA16F targets and two D24S8 targets even when AO/contact shadows are disabled: nominal **48 bytes/pixel**, about **380 MiB at 3840×2160**, plus a nominal **64 MiB** shadow atlas, swapchain/resources and transient overlap during resize. This is format arithmetic, not measured driver allocation. The prepass accounts for 20 bytes/pixel even when unused. Use feature-dependent targets, narrower suitable formats, checked backend limits and an engine-owned allocation ledger. The pinned bgfx `Caps::Limits` exposes texture size, framebuffer attachment and sampler limits; see the [bgfx reference](https://bkaradzic.github.io/bgfx/bgfx.html).

Close with a bounded radius/depth scene, sunlight-only versus ambient-only comparisons, and declared target-byte/cost measurements at the intended viewer resolutions.

### A09 — P2: region failure states can strand desired content

Source: [RegionStream.cpp](../Source/World/Streaming/RegionStream.cpp), `update`; [StreamingScene.cpp](../Source/Rendering/StreamingScene.cpp), attach callback and `adoptCollisions`.

Desired slots with a nonzero ticket are skipped. Completion, admission or attach errors record an error but leave that consumed ticket nonzero. Physics preparation errors similarly mark collision unready with no retry request. A transient allocation/queue/cook failure can leave the player waiting until the region is retired or another edit resets it. Existing readiness guards safely prevent traversal, but safe waiting has no bounded recovery path.

Define pending/ready/failed/retryable states, reset consumed tickets, distinguish permanent data errors from retryable resource errors and provide bounded backoff or an explicit retry command. Carry content revision with collision readiness so recovery cannot mark a newer render cohort ready using older collision. Retain valid prior resources where possible.

Close with one injected attach/cook failure followed by resource recovery, plus cancellation/replacement tests. This is scheduler infrastructure; it requires no new terrain or generator algorithm.

### A10 — P2: persistence is recoverable but still blocks the frame thread

Source: [Apps/Game/main.cpp](../Apps/Game/main.cpp), save lambda and 600-tick autosave; [WorldSession.cpp](../Source/Game/WorldSession.cpp), `save`; [WorldSave.cpp](../Source/Persistence/WorldSave.cpp), `saveWorld`; [Document.cpp](../Source/Persistence/Document.cpp), replacement.

The player performs validation, filesystem inventory, multiple writes/fsyncs, fingerprints and generation cleanup synchronously on its frame thread every 600 ticks and on exit. No fresh latency measurement was taken here, so a particular millisecond stall is not claimed. These blocking calls are nevertheless incompatible with a strict low-latency frame contract.

Retain coherent immutable save cohorts, expected-generation checks, writer leases and last-good recovery. Capture one immutable state at the owner boundary, then enqueue a bounded save job. Report durable completion separately from snapshot acceptance; serialize writes per profile and define shutdown drain/failure handling. Scene saves also need explicit disk-generation conflict/recovery semantics before multiple authoring clients can save the same scene. Do not equate an atomic rename with full power-loss recovery; directory durability and crash recovery are separate hardening work.

Close with slow-I/O injection, concurrent/stale saves, interruption recovery and exact success receipt behavior, without blocking simulation during normal saves.

### A11 — P2: green CTest coverage is narrower than the subsystem claims

Source: [CMakeLists.txt](../CMakeLists.txt), lines 115–198; [SceneDocumentTests.cpp](../Tests/SceneDocumentTests.cpp), cycle case lines 73–80.

There are 24 `engine_*tests` executables but only 15 CTest registrations. Nine are not run by the standard suite, including `engine_snapshot_tests`, `engine_world_save_tests` and `engine_world_session_tests`. Several other omitted executables concern generator work outside this task. Keep their intended manual/technical status explicit; do not register gameplay smoke checks indiscriminately.

The existing cycle test supplies signed JSON integers for `revision`/`next_entity_id`, while `fromJson` requires unsigned values. Its catch-any-exception helper passes before hierarchy validation is reached. This does not mean cycle detection is absent; it means the test does not prove it.

Register the intended headless persistence contracts with correct arguments, label opt-in technical/visual tests, and assert error categories for invalid-input tests. Add independent oracles for A01–A04 instead of only round-trip or same-implementation comparisons. Keep the audit probe separate from passing regression gates until corrections are made.

## Coverage and missing integration

| Area | Existing useful foundation | Gap / next owning boundary |
|---|---|---|
| Core/build | C++20, CMake targets/presets, pinned acquisition, licenses, headless core | `engine_core` now includes authoring/JSON command code; split an `engine_authoring` target as real consumers appear. Common math and units need one tested contract. Windows W01 is still open. |
| Scene authority | Stable per-document numeric IDs, local TRS, component reference strings, transform history | A01–A05 first. No reparent/duplicate/group domain operations, light/camera components or typed asset resolution yet. Define identity as document ID + entity ID + load/session epoch when crossing domains. |
| AI control | Bounded queue, operation IDs, inspection/apply handlers | Library-only: no app owner, live scene endpoint, save/load/export/session commands, conflict preview or operation recovery. A thin local command transport can follow an actual scene host. |
| Human Scene View | Existing orbit/pan/zoom and fixture controls | Selection/picking/gizmos must edit the same scene domain; define preview/commit/cancel, parent-aware multi-selection and local/world transforms. UI agent owns widget implementation. |
| Rendering | Shared bgfx renderer, HDR/display, directional cascades, frustum culling, model buffers | No `RenderSceneSnapshot`; hard-coded marker/ground/rock/character branches, yaw-only `RenderInstance`, borrowed resource pointers, two surface buckets and fixed global view IDs. Generic affine instances, material IDs and viewport contexts must replace those assumptions gradually. |
| Materials/assets | Semantic texture cooking/catalog, role/color checks, generation-checked texture leases, UV/triplanar paths | Scene material strings have no material catalog resolver. `MaterialDefinition` is slot validation centered on a layered rock family. glTF import rejects images/textures and multiple primitives; renderer disables texturing for `mesh == -1`. Support one generic imported textured static/skinned object before claiming an end-to-end material pipeline. |
| Lighting/post | Sun/PCF cascades, hemispheric ambient, tone mapping/exposure, height-fog approximation, AO/contact prototypes | A06–A08. Environment-map lighting/reflections and local lights remain absent. Ambient is explicitly not IBL; a panorama asset is not an environment-light implementation. Temporal AA/occlusion and volumetric fog are later measured enhancements. |
| Simulation/physics/input | Fixed 60 Hz clock with capped catch-up, action focus routing, EnTT identity tokens, Jolt collision/gravity/queries/controller | `CalibrationRuntime` owns fixed proxy/marker content; no generic scene-to-physics component adoption or isolated PlaySession. Expand supported components at the first generic consumer, not via a second physics world model. |
| Jobs/streaming | Capacity/byte reservations, cancellation, owner-thread adoption, unload/reload, collision readiness | A09. Some terrain LOD preparation/uploads/copies still run on the frame thread. Shape byte accounting estimates input/result and is not peak Jolt cook-memory proof. Prior 59 ms cooking evidence predates the worker move. |
| Persistence | Strict envelope, atomic file replacement, coherent world save generations, writer lease and fallback | A04/A10. Authoring scene history/recovery and play-session source isolation are unfinished; default player saves live beside the executable, so a writable per-user data root is needed for installed builds. |
| Animation | ozz sampling/blending, bounded skin palette, CPU/GPU skin path | One limited asset profile, no general animation state/event/root-motion system. Runtime constructs offline ozz data at load and allocates a new palette per evaluation; move conversion to cooking and reuse pose buffers when scaling characters. |
| Audio | miniaudio backend, bounded cue IDs, one generated cue, offline check | No generic spatial emitters/listener/buses or asset-streaming/device-recovery integration. Add at the generic play-session consumer. |
| Navigation/game services | Roadmap and durable library research | Navigation modules are absent. No need to build companion/game rules to repair the generic engine; nav, VFX, richer animation/audio and gameplay integration follow the reusable runtime slice. |
| Performance/production | Shared recent-frame telemetry, GPU identity distinction, VSync policy, historical Metal capture/package receipts | 128-sample window with median/p95/max, limited GPU memory report and no present-day representative authoring/play workload. No Windows execution/GPU proof or input-to-photon measurement; no automatic CI workflows found in the repo. |

## Corrected execution sequence

This ordering refines the [human-AI plan](./HUMAN_AI_ENGINE_BUILDOUT_PLAN.md) under the [master plan](./FOUNDATION_PLAN.md). It does not add a competing full-engine roadmap.

| Batch | Work and stop condition |
|---|---|
| R1 — Correct scene authority | Resolve A01–A04 and the false-positive test in A11. Use affine derived transforms, exception-safe adoption/receipts, coherent history, validated capacity and monotonic revisions. Close on independent CPU proofs and a save/reloadable generic document. |
| R2 — Bound and host authoring | Resolve A05; add cached parent-first extraction, document/session identities, paged reads and a bounded local command host. Add minimum create/delete/metadata/reparent operations needed by the neutral scene. Close when commands inspect/edit/reload the hosted scene with explicit receipts. |
| R3 — Connect renderer and Scene View | Add `RenderSceneSnapshot`, a fixture compatibility adapter, typed mesh/material references, per-view camera/target/input state and selection/gizmo transaction contract. Correct A06/A07 as the shared draw preparation is factored. Close with the same generic scene visible and editable by AI and human input. |
| R4 — Share play lifecycle and durability | Extract an isolated PlaySession used by Workbench Game View and standalone player; source authoring state is immutable during play. Correct A09/A10 and follow-up A12–A15; register relevant persistence tests from A11. Close with incompatible-save preservation, sleeping-body support changes, continuous pause presentation, nonthrowing stop at capacity, one dynamic object, player collision/gravity and durable save receipts. |
| R5 — Establish a measured rendering/material baseline | Resolve A08, finish one imported textured material path, add bounded frame/resource accounting and a representative neutral workload. Record CPU/GPU p50/p95/p99 with useful sample counts. Introduce instancing/material sorting only where measured submission cost warrants it. |
| R6 — Windows and remaining engine capability gates | Run W01 build proof when a Windows runner is available; Mac daily work can continue. Once the common slice works, prioritize W02 GPU/input proof, environment lighting/local lights, spatial audio/animation ownership, navigation and packaging according to the next generic consumer. Keep advanced GI, a general render graph and large editor tooling conditional. |

Do not skip R1 to start gizmo integration. R1 is a correction pass with a clear end, not permission for indefinite cleanup. Reuse the current renderer, Jolt, ozz, miniaudio, persistence and bounded-job owners. Do not cook permanent landscapes or expand generator themes during these batches.

Procedural compatibility remains explicit: authored IDs are scoped to documents; generated IDs retain world seed/version/member identity; resource unload cannot remove authoring truth or saved deltas; constraints remain source data; renderer/physics objects are transient consumers of a versioned scene/session result. This audit creates only diagnostic fixtures, not accepted authored world content.

## Research and proof limits

Primary references checked 2026-09-15: Khronos glTF 2.0 transformations and tangent guidance, Filament indirect-light/occlusion design, and bgfx API/capability documentation (linked at the relevant findings). These support the proposed contracts; they do not require replacing our libraries. Current pinned bgfx headers were also checked for the capability fields rather than assuming online version parity.

The strongest new evidence is A01–A05's CPU observations. A06–A11 additionally use source inspection and explicit call paths; graphics artifacts, real disk-stall duration, streaming retry behavior on an actual device and final Windows suitability require their scoped implementation proofs. Passing the current test suite cannot close those findings by itself.

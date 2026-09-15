# Engine correction and integration plan

2026-09-15. Status: **R1 and R2 implemented; R3 is next**. [R1 proof](./SCENE_AUTHORITY_R1_RESULT.md), [R2 proof and live protocol](./LIVE_AUTHORING_R2_RESULT.md). Plan source baseline: `7eb9abd`. This is the executable correction sequence beneath [FOUNDATION_PLAN.md](./FOUNDATION_PLAN.md), informed by the [architecture audit](./ENGINE_ARCHITECTURE_AUDIT_2026-09-15.md) and [lifecycle follow-up](./ENGINE_LIFECYCLE_AUDIT_2026-09-15.md). It controls the immediate work in the [human–AI build-out](./HUMAN_AI_ENGINE_BUILDOUT_PLAN.md). [STATUS.md](./STATUS.md) records actual delivery; the `active_execution` section of [ENGINE_ROADMAP.json](./ENGINE_ROADMAP.json) indexes these batches. Existing first-pass package evidence remains historical, not proof that these corrections are complete.

## Outcome and boundary

Deliver one dependable generic engine path: author a bounded scene through shared operations, inspect and manipulate it in Scene View, start an isolated Game View session using the standalone player's runtime, save/recover deliberately, and render it through correct material/lighting inputs with measured resource costs. Close all A01–A15 findings with evidence tied to their corrected source. Windows acceptance remains a separate native gate.

Keep C++20, the selected libraries and useful existing owners. Work only under `Engine/`. No rock/terrain algorithms, permanent landscapes, game rules, lore, origin-shift project, broad editor or library replacement. Existing fixture content serves only as a temporary compatibility consumer. The engine lane owns domain/runtime/rendering/operations; the UI lane owns gizmo widgets and layout. Engine interfaces and headless work proceed independently, while visual/gizmo acceptance stays explicit if its consumer is not ready.

This planning task ends with this reviewed plan, complete finding coverage, correct continuation routing and focused planning checks. Runtime implementation starts with R1 in the next implementation task.

[Planning verification](../Evidence/CORRECTION-PLAN-2026-09-15/result.json) records the 12 focused checker tests, complete finding coverage, R1 continuation result and existing reference-document check limitations. It does not close a runtime batch.

## Required architecture decisions

1. **One scene authority.** Retain editable local TRS. Resolve full affine world matrices in deterministic parent-first order, retaining shear introduced by hierarchy. Centralize matrix convention, units, quaternion order and normal/handedness derivation. Preserve-world reparenting rejects a result that cannot be represented as supported local TRS; it never silently discards shear. Physics adoption separately rejects unsupported shear/scale with an entity-specific error instead of silently misaligning collision.
2. **Prepare, then adopt.** Validate a complete candidate, derived transforms, history and receipts before any live mutation. Publish via a nonthrowing owner-thread adoption. A staging validation error aborts the transaction. Undo/redo use the same boundary; create/delete/restore/metadata/reparent must participate. ID high-water marks and revisions never roll back or wrap.
3. **Version every boundary.** Durable scene ID plus entity ID identifies authored data. A load/session epoch distinguishes live incarnations. Every mutation, including undo/redo, requires the current identity/epoch/revision. Generated IDs keep world seed/generator version/member identity. GPU/physics handles never persist.
4. **Frame consumers use snapshots.** Authoring and play state produce immutable render snapshots containing affine transforms, bounds, typed asset IDs, visibility and pose references. Resource leases retain assets until their last snapshot/consumer releases them. No worker touches GPU handles or mutable scene/physics storage. Model reasoning and transport never run in the frame loop.
5. **Play is isolated.** Play starts from an immutable scene revision or supported world profile. Runtime deltas belong to the session; stopping discards unsaved deltas. Saving play state does not edit authoring source. No automatic apply-back. Explicit later authoring transactions are the only route for accepting runtime changes into source.
6. **Receipts describe reality.** Distinguish rejected, queued/preparing, applied, cancelled-before-apply, and durable-save completion. Once adopted, cancellation cannot describe the edit as unapplied. Retrying a request ID in the same live epoch returns its retained receipt or an explicit expired/unknown result, never blindly replays it. Do not claim crash-persistent exactly-once editing before implementing a durable journal; after restart reconcile the saved revision and new epoch.
7. **Local automation first.** R2 hosts one bounded local command endpoint, with a small CLI client callable from Codex. Prefer a user-private Unix-domain socket on Mac and a named pipe on Windows behind a transport adapter; no remote listener or new service dependency. Domain handlers remain transport-independent. Mutations are processed by the owner; slow reads, preparation and serialization work from immutable data off the frame path.

## Sequence and acceptance ownership

These are coherent batches, not a list of tiny tasks requiring user decisions. Commit working internal checkpoints when useful; close a batch only when its full exit condition is met.

Indexed batch completion describes engine-owned delivery. R3's engine contract may close with a headless gizmo client and renderer evidence while the UI owner's actual widget acceptance remains a separately named open gate in STATUS. That permits R4 to proceed, but does not permit claiming the complete human editing workflow has shipped. Likewise native platform completion never follows from Mac-only proof.

| Batch | Depends on | Primary findings | Exit outcome |
|---|---|---|---|
| R1 | Current audited baseline | A01, A02, A03, A04; hierarchy-test part of A11 | Correct, transactional, saveable scene document |
| R2 | R1 | A05 | Bounded live authoring host usable by Codex |
| R3 | R2 | A06, A07 | Generic snapshot rendering and shared Scene View manipulation contract |
| R4 | R3 | A09, A10, A12, A13, A14, A15; registration part of A11 | Isolated play, recoverable streaming and deliberate durable saves |
| R5 | R4 | A08 | Correct material/AO/depth integration and a measured generic workload |
| R6 | R5; native W01/W02 for completion | Platform and release-readiness gaps | Native Windows evidence and a scoped engine handoff |

W01 compiler/build work may proceed whenever a Windows runner is available; it does not wait for R6. W02 starts when the corrected rendering/runtime consumers exist. Missing Windows access must not block independent Mac R1–R5 work, nor be treated as a completed platform gate. No remote job, purchase or publication is authorized by this plan alone.

### R1 — Correct scene authority

Owned areas: `Source/Authoring`, shared CPU math in `Source/Core` if needed, `Source/Persistence`, affected operation receipt code, scene tests and CMake registration. Preserve current public consumers with narrow migrations; do not maintain two independent scene implementations.

Implementation order:

1. Introduce affine hierarchy evaluation with an ID index and deterministic parent order. Use independent matrix expectations, finite-result validation and explicit singular-transform rejection. World-space edit/reparent conversion must prove local-TRS representability. Build the index here so R2 does not repeat a hierarchy rewrite.
2. Extend the transaction candidate to structural edits and metadata. Deleting a subtree records restorable relationships and data. Multi-selection transforms must not apply a parent's delta twice to its selected descendants. Duplicate assigns fresh IDs; group/ungroup are compositions of the same validated operations, not separate mutation paths.
3. Prepare forward/inverse history, bounded changed-ID results and publication storage before adopting. A failed commit/undo/redo preserves values, revision and history. Reserve operation result storage so delivery failure cannot change an applied edit into a rejected edit. Explicitly define an applied edit whose transport delivery was interrupted.
4. Enforce strict types and an explicit schema. Reject unknown fields in the current schema instead of dropping them; migrate known historical schema versions explicitly if a format change is required. Check revision/entity-ID exhaustion before editing, and retain high-water marks through undo/reload. Validate the exact serialized envelope size before accepting a scene candidate.
5. Repair the hierarchy test to use the intended unsigned schema values and assert the hierarchy error category. Convert relevant observational probes into independent corrected regression cases; retain original audit evidence unchanged.

**Proof and stop:** nonuniform/mirrored parent chains; reparent shear rejection; edit/delete/subtree restore/undo/redo; stale or exhausted revisions; failure injection at preparation/publication boundaries; strict numeric/unknown-field rejection; largest-admitted-scene save/reload; rejected saves preserve the last valid file. Run directly affected CPU tests once after the batch, rerunning only for changed code or failures. No renderer or GUI is required. Close when A01–A04 are resolved and the A11 false-positive test is fixed.

### R2 — Bound and host authoring

Owned areas: `Source/Authoring`, `Source/Runtime/AuthoringOperations`, local transport/platform adapter, `Apps/Workbench` host wiring and `Tools` command client. Split `engine_authoring` from `engine_core` when wiring the real host so runtime-only targets do not inherit transport/editor dependencies. Do not split modules without a concrete consumer.

Cache parent-first extraction by revision; avoid recursive linear lookup and repeated full-scene JSON on every frame. Use bounded preparation jobs on immutable revisions, rechecking identity/epoch/revision immediately before adoption. Page inspections against a pinned snapshot revision. Keep adapters noncopyable/nonmovable unless handlers are explicitly rebound. The endpoint advertises its session/document identity and supported operations; callers select the endpoint rather than guessing a running application.

Expose inspect, preview/apply, create/duplicate/delete/reparent/metadata and undo/redo through the shared domain. The CLI returns structured results and supports receipt lookup. Add save/load requests only after R4 supplies conflict/recovery/durable-acknowledgment semantics; do not temporarily expose a blocking live save as the final API. R1 still proves domain save/reload headlessly.

**Proof and stop:** a real CLI-to-host transaction and inspection, identical domain/direct results, stale-load epoch rejection, retry/cancel/expired receipt semantics, bounded queue/page/receipt bytes, flat/deep hierarchy extraction and host shutdown with pending work. A CPU transport harness can prove correctness; a real Workbench host response proves live application wiring. Record preparation versus adoption costs separately. Close A05 and leave the host usable without widgets.

### R3 — Connect renderer and Scene View

Owned areas: `Source/Rendering`, `Shaders`, asset/material resolution, snapshot extraction, Workbench viewport binding and an engine-to-UI contract. Reuse current bgfx resource owners. One renderer owns shared resources; each viewport owns explicit camera/projection/target/view IDs and dimensions. Resize/removal must not overwrite another view's state. Stage resource replacements and release failed candidates safely.

1. Introduce `RenderSceneSnapshot` and typed mesh/material resolution. Missing/unsupported assets return actionable identity-based errors or an explicitly labelled diagnostic preview; they do not silently become production bindings. Replace yaw-only instances with affine transforms and transformed bounds. Keep a small fixture adapter until existing consumers migrate.
2. Factor shared per-draw state so color, shadow and depth/normal passes bind the same transform, skin pose and correct normal matrix. Correct tangents using the linear transform, orthogonalization and determinant-aware handedness. Preserve supplied tangents; generate a compatible UV-derived basis at import or reject normal-map usage when prerequisites are missing.
3. Define selection/picking and gizmo begin/update/commit/cancel over scene identity and revision. Drag updates are ephemeral preview state; one completed drag makes one undo entry. Cancel/focus loss restores the document unchanged. Parent-aware multi-selection, local/world modes and snapping use domain operations. A concurrent AI edit conflicts explicitly; it cannot overwrite an in-progress human edit silently.
4. Deliver the contract to the existing UI-owned documentation surface. The UI owner supplies actual widgets; do not grow a second interface. Engine-side picking and simulated drag requests remain independently testable.

**Proof and stop:** snapshot matrices/bounds match the independent scene oracle; asset leases survive in-flight snapshot use; two camera/view targets and resize remain independent; asymmetric normal-map tests under rotated/nonuniform/mirrored transforms; reversed draw order does not change prepass normals. Validate captures with source/shader/config hashes. Mark engine API proof separately from actual human gizmo/visual acceptance. R4 core work can proceed if the UI consumer is pending, while R3's human acceptance remains explicitly open.

### R4 — Shared play lifecycle, streaming recovery and persistence

Owned areas: shared session/runtime, `Source/Physics`, `Source/World/Streaming` scheduling only, `Source/Persistence`, relevant jobs, Workbench Game View and standalone Game wiring. No new world-generation algorithms.

1. Extract a shared PlaySession with explicit starting/running/paused/stopping/stopped/failed states. Adopt a bounded generic static mesh, dynamic primitive, player collider/camera and existing skinned/audio consumer where supported. Validate component support before starting; partial start failure releases all acquired resources. Both applications consume the same session implementation. Authoring source revision stays untouched.
2. Preserve fractional presentation on pause/focus suspension; resume does not accumulate wall-clock backlog. Game input belongs to the active Game View and is inhibited on focus changes. Session restart gets a fresh epoch, rejecting old commands/completions. User gameplay feel remains a separate review.
3. Make stop idempotent, release-only and nonthrowing. Disconnect callbacks, cancel/join work while dependencies live, release physics/resources, then close the owner. Put any fixture restoration in a separate fallible transition. Stop and failed-start cleanup must succeed at capacity.
4. Add explicit pending/ready/retryable-failed/permanent-failed stream states. Consume/reset finished tickets; retry transient failures with bounded backoff and a retry limit/explicit retry command. Keep prior valid resources while preparing replacements. Require owner/session/content revision on collision adoption and readiness. Cancelled/retired content cannot become ready later.
5. Wake nearby dynamic bodies over old/new collision bounds when support is replaced/removed. Keep unrelated bodies asleep. Streaming retirement either retains support needed by active dynamics or retires/persists those dynamics in the same lifecycle boundary; it must not leave orphan simulated residents by accident.
6. Protect unsupported snapshot versions/kinds before reusing the two-slot path (A12). Capture coherent immutable save state, serialize/write through a bounded per-profile queue, retain writer leases and expected disk-generation checks. Explicit saves receive durable completion separately from snapshot acceptance. Autosaves may coalesce only superseded snapshots of the same profile with observable receipts. Slow/erroring disk work must not block the frame loop.
7. Scene load prepares off-thread, validates, then swaps only after expected identity/revision still match. Failure preserves the current scene. Save requests bind source revision and target disk generation. Preserve incompatible files and last-good generations. Decide and document file/directory durability per platform without claiming power-loss guarantees from rename alone. Flush pending explicit saves through an observable shutdown state before release-only destruction; report failures and retain the last good save.
8. Register the intended headless persistence executables with unique temporary directories and labels. Preserve manual/visual/gameplay test boundaries; do not indiscriminately register all nine omitted executables from A11.

**Proof and stop:** source checksum unchanged across start/pause/restart/stop; stale session results rejected; dynamic collision/gravity/support edits; pause at a fractional tick; failed start/stop at capacity; one transient failure then recovery and one permanent error without endless retry; stale collision completion after replacement/retirement; incompatible/corrupt/interrupted/concurrent/stale save cases; slow-I/O receipt behavior. Close A09–A15 except the already-owned rendering findings, and complete A11 registration. Expose lifecycle/save/load/inspect-runtime operations through R2. No automatic save-to-source or generator changes.

### R5 — Correct materials, AO/depth and resource costs

Owned areas: renderer/shaders, material/asset import, resource budgeting and telemetry. Complete the generic material binding introduced in R3 with one supported imported textured static mesh and one skinned case. Reuse transferred legal textures as optional diagnostics, not hard-coded material families. Asset IDs resolve to validated color/normal/surface roles and color spaces; unsupported UV sets/extensions/multiple-primitive cases reject explicitly until supported.

Choose one documented view-depth/reconstruction convention shared by AO and contact shadows. Express radii, bias and blocker thickness in declared units; account for projection and aspect ratio. Apply screen AO to indirect illumination before fog rather than darkening the completed sunlight/fog image. Preserve a consistent direct/indirect split through material evaluation and composition.

Allocate optional targets only when consumed, select formats with sufficient depth/normal precision, check backend texture/attachment limits and account for live/retiring/resize-overlap allocations. Failure retains valid targets or reports bounded feature degradation. Extend per-view ownership rather than introducing a general render graph. Environment-map lighting/local lights are future capabilities, not a substitute for correcting present paths.

Use a fixed neutral workload: repeated static meshes, a hierarchy, several material instances/LODs, one skinned actor, one dynamic body and synthetic resident/streaming adapters. At a recorded 1920×1080 profile, collect at least 1,000 valid post-warmup timing samples per measured run, bounded in memory or streamed to a file. Record p50/p95/p99, sample counts, hardware/backend, GPU query validity, stage costs, resource high-water marks and post-retirement baseline. This is one bounded baseline/comparison, not an endless benchmark campaign. Use instancing, material sorting or upload changes only where that run identifies an actual cost.

**Proof and stop:** direct-only versus indirect-only AO, fog separation, near/far and aspect/depth precision, known UV/normal/surface response, no stale draw-state dependence, optional-target toggles/resize/failure cleanup and reference culling/LOD comparisons. Report missing GPU timing as unavailable, not zero. Close A08 and the generic material path with recorded CPU/GPU/resource evidence; do not infer input-to-photon latency or Windows FPS from Mac measurements.

### R6 — Platform validation and handoff

Use the existing W01/W02 gates for native compiler/shader and D3D11 GPU/input/resource proof. Build/package from pinned sources, launch from an arbitrary directory and use a writable per-user data root for installed saves/logs. Record dependencies, source/build identity and notices. Add a repository-owned build/check entry point reusable by a future CI runner; running or publishing external CI requires its own task authority.

**Proof and stop:** native W01/W02 evidence against the corrected generic workload, relocatable technical package, save/recovery/path checks, diagnostics and remaining limitations. R6 cannot close on Mac-only compilation or a paper platform plan. W03 and shipping approval remain later product gates. A blocked Windows gate is recorded once; continue independent engine work only if it has a defined next consumer and scope.

## Initial bounds and migration policy

These are adjustable engineering defaults, not final world scale or target-PC promises. Make limits explicit in one configuration/contract and change them only with relevant evidence.

| Boundary | Initial policy |
|---|---|
| Authored document | Retain the 1 MiB actual serialized envelope ceiling; at most 1,024 entities and 64 parent links. All limits apply to create/edit/load/save. Reject oversized candidates before adoption; larger partitioned scenes are a later capability. |
| Live command ingress | Retain the existing 256 KiB request ceiling; bound aggregate pending payload bytes to 8 MiB and request count to 128, whichever fills first. Validate before admission. |
| Inspection/results | At most 64 entities and 64 KiB per page/receipt. Page by pinned scene revision; changed-ID lists may use bounded pages or reject an oversized transaction before applying. |
| Receipt/history retention | At most 512 receipts and 8 MiB serialized receipt bytes; initial undo budget 64 entries and 16 MiB accounted owned payload. Do not evict pending receipts. Expose expiry/history limits; never accept an edit that cannot retain its required inverse/receipt. |
| Work/adoption | Retain existing finite job/staging limits, account for cancelled running work, and measure actual peak allocations when changing cook paths. Bound commit work structurally; a command-count limit alone is insufficient. |
| Performance | Keep 60 Hz fixed simulation and capped catch-up. Measure stage tails under declared load; choose hardware pass/fail frame budgets after a supported target is established. Do not invent a universal FPS guarantee. |

Existing files outside the new admitted domain remain untouched on rejection. Report the exact limit and a non-destructive import/splitting route; do not truncate entities or silently rewrite files. Undo history need not survive a reload in this pass, but document that boundary; durable source data and ID high-water marks must survive. Preserve audit artifacts as historical observations; new regression/evidence records carry corrected-source hashes.

## Complete finding coverage

| Finding | Closure owner and check |
|---|---|
| A01 | R1 affine hierarchy plus independent matrix oracle |
| A02 | R1 candidate/history/receipt preparation and nonthrowing adoption |
| A03 | R1 structural transaction history including subtree restore |
| A04 | R1 strict schema, overflow and exact save-capacity admission |
| A05 | R2 indexed/cached extraction, bounded host/results, identity/epoch |
| A06 | R3 common per-draw normal inputs in every consuming pass |
| A07 | R3 UV-derived tangents, affine tangent/normal/handedness contract |
| A08 | R5 indirect-only AO, shared depth units and feature-dependent target budget |
| A09 | R4 retry/error states and revision-bound readiness |
| A10 | R4 immutable asynchronous saves, conflicts and durable receipts |
| A11 | R1 hierarchy assertion; R4 intended headless persistence registration |
| A12 | R4 incompatible snapshot preservation before profile reuse |
| A13 | R4 support-edit wake/retirement semantics |
| A14 | R4 pause/suspend presentation continuity |
| A15 | R4 release-only nonthrowing teardown and failure cleanup |

## Plan self-audit and execution rules

- **Avoid a second rewrite:** R1 creates the hierarchy index and affine contract; R2 adds caching/hosting over it; R3 consumes it. All consumers migrate to one domain and snapshot path.
- **Resolve proof/dependency ambiguity:** R3 core interfaces unblock R4 without falsely claiming unfinished human widget acceptance. Windows gates can start early and remain open independently. A11 explicitly has two closure checks rather than disappearing between batches.
- **Keep saves honest:** incompatible-version protection precedes profile reuse; R2 does not expose a temporary blocking save API. An applied edit and a durable save are different acknowledgments. Live receipt deduplication does not claim crash-persistent exactly-once behavior.
- **Bound expansion:** no new generator, game mechanic, general ECS rewrite, material graph, scripting VM, GI/IBL or navigation package is required to close A01–A15. Local lights/environment lighting, richer animation/audio and navigation stay in the master capability backlog for the next generic consumer.
- **Procedural compatibility:** authoring constraints remain source data; generated identity and deltas keep their current semantics. Streaming only changes residency/readiness. Session isolation and unload cannot discard authoring truth or persisted deltas. Technical fixtures are not accepted game content.
- **Work deliberately:** one coupled batch active in the shared checkout. Read status and run the plan checker, state the bounded target, implement, run relevant checks, inspect evidence, update the batch state/finding closure, commit exact owned paths, then advance. No push or UI takeover is implied.
- **Stop testing when answered:** use independent regression cases for corrected defects and one appropriate integration check. Broaden only for a new failure, changed interface or unresolved claim. A passing compile is not graphics, hands-on play or Windows acceptance. Never change a probe's expected result simply to make an unchanged defect pass.

Continue with **R3 snapshot rendering and the Scene View manipulation contract** after the completed R1/R2 correction batches. No new general audit or generator task is required.

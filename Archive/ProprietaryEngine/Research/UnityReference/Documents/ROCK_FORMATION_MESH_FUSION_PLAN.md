# Rock Formation Mesh Fusion Plan

Status: superseded for production runtime on 2026-08-14 by `Docs/TOP_DOWN_3D_LANDSCAPE_IMPLEMENTATION_PLAN.md`. The proved Manifold work remains an isolated editor-side asset-baking experiment and historical evidence; chunk streaming and gameplay must not depend on native Boolean fusion. Its checked-in binaries are now enabled only for the macOS Editor and excluded from Standalone players. The placement, identity, topology, and validation findings below remain useful input to the production rock-asset pipeline.

## Editor extraction precision repair — 2026-09-08

The detailed approved rock catalog exposed a native-output conversion edge case: distinct double-precision positions can round to the exact same Unity float position. Exact-position welding then leaves a face with repeated indices and no representable area. Extraction now removes only those collapsed faces before the unchanged closed-manifold and positive-volume checks. The converted float surface is also rechecked through Manifold for validity and exactly one connected component, so a collapsed narrow connection cannot hide a split. It does not use an epsilon weld, remove representable narrow triangles, relax validation, add a fallback, or change source meshes, placement, identity, materials or player code.

Regression coverage includes exact face collapse, preservation of representable slivers, rejection of an open surface, and the seeded `ExtraLarge:4:-4` parent edge that exposed the defect. The existing seeded fusion corpus remains intact. The baked-family regression also follows the canonical approved-recipe resting-pose and decreasing-LOD rules for Boulder, Slab and Nodule; the original 600-vertex cap remains on the older procedural primitives. This is not a new performance budget or a claim of runtime activation.

The 48-formation corpus has a test-local 15-minute timeout instead of Unity's default three minutes. With the detailed approved meshes, the legacy planner fixture plus fusion completed in about 585 seconds during validation (about 39 seconds in measured fusion work), exceeding the runner limit despite completing its geometry assertions. No sample range, formation count, assertion, or global timeout was reduced or disabled. This editor-test cost is not a measured gameplay performance result.

## Objective

Replace the visual construction path for intersecting multi-member physical rock formations so every successful formation is rendered as one connected, watertight exterior mesh without internal overlapping shells. Preserve deterministic placement, stable root/member identity, root-chunk ownership, materials, shadows, dust envelopes, traversal behavior, and one box collider per member.

## Source Of Truth

Use this order when implementation facts or requirements conflict:

1. The user's current implementation request.
2. Root `AGENTS.md` and `Docs/Agents/Gottspan/README.md`.
3. `Docs/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md` and `Docs/WORLD_SYSTEMS_STANDARD.md`.
4. Current production runtime code, especially `TopDown3DRockFormationPlanner.cs`, `TopDown3DNaturalObjectCatalog.cs`, `TopDown3DNaturalObjectDecorator.cs`, and `TopDown3DProceduralWorld.cs`, plus the isolated historical authoring code under `Assets/_Project/Scripts/Editor/TopDown3D/RockFusion/`.
5. Focused EditMode tests and the non-mutating TopDown3D validator.

Current dirty edits in the rock planner, settings, catalog, tests, and feature contract are user-owned current truth. Fusion consumes them and must not revert, retune, or absorb them.

## Owner And Lane

Gottspan owns the repository integration. Production natural-object geometry and streaming remain in `BooterBigArm.TopDown3D.Runtime`; this superseded fusion experiment is isolated in the Editor assembly and cannot own player generation. Babineaux owns later Unity automation when required. Gear Ball owns any later staging, commit, push, or publication operation; those operations are not authorized by this plan.

## Approved Scope

- Vendor pinned Manifold v3.5.2 under Apache-2.0 with recorded provenance and a reproducible macOS universal build.
- Prove a minimal native C ABI and Boolean union before changing the canonical formation path.
- Convert the procedural source triangle soup into deterministic indexed, two-manifold topology.
- Add one native interop owner, one canonical formation mesh builder, and one bounded streamed-work owner.
- Replace only the multi-member visual mesh construction path after focused proof passes.
- Preserve single-member construction as the canonical identity case, not an error fallback.
- Preserve existing compound box collision and all planner/version/identity contracts.
- Advance the physical-rock generation version and replace projection-only child contact acceptance with a deterministic managed positive-volume-overlap witness. Existing identities remain historical; the new version owns the corrected placements.
- Add focused EditMode proof, non-mutating validation, profiling markers, dependency documentation, and truthful feature documentation.
- First proof target is the current macOS desktop development environment.

## Non-Goals

- No tier, branching, density, scale, catalog, or unrelated placement tuning beyond the newly authorized contact-validity correction.
- No SDF/metaball smoothing, connector geometry, overlapping-shell fallback, alternate Boolean backend, or custom CSG implementation.
- No collider replacement, shader/material change, terrain change, save-format change, final shipping-platform decision, package-manager change, broad refactor, UI/UX change, or gameplay smoke test.
- No branch switch, worktree operation, staging, commit, push, pull request, build publication, release, or deployment.

## Canonical Design

1. Normalize each of the sixty cached procedural rock variants by exact-position welding, deterministic first-occurrence indexing, and closed two-manifold validation.
2. Marshal transformed root-local solids through one Manifold C API facade using `MeshGL64` and explicit safe native ownership.
3. Batch-union all members once, force evaluation, require successful status and exactly one decomposed component, then extract one indexed mesh.
4. Recompute deterministic area-weighted normals in double precision with explicit normalization, then compute bounds and Unity index format in managed code.
5. Run native fusion on one serial background worker using immutable managed inputs. Create and register Unity objects only on the main thread.
6. Tag requests with formation identity, formation order, owning chunk coordinate, and a chunk-instance generation token. Unload invalidates pending work; stale outputs are disposed, never applied.
7. Create mesh, renderer, colliders, and traversal obstacle atomically after success. An unexpected multi-member failure skips the whole formation and emits a structured error; it never creates invisible collision or overlapping visual fallback.

## Validated Batches

### Batch 0 — Preflight And Plan Authority

- Refresh Git status, Unity lock state, package/editor/runtime assembly truth, and target-file diffs.
- Materialize this plan and make it the active goal source.
- Stop if current ownership cannot be preserved.

### Batch 1 — Dependency And ABI Proof

- Pin Manifold v3.5.2 to its resolved upstream commit and source checksum.
- Preserve the Apache-2.0 license and add a deterministic native build recipe.
- Build/import the macOS universal C binding with internal parallelism disabled.
- Prove load, allocation/destruction, two-overlapping-cube union, status, component count, extraction, and cleanup without touching the canonical rock path.

### Batch 2 — Source Topology And Pure Fusion

- Add deterministic source topology normalization and validate all cached variants.
- Add safe native handles and the single `UnionMany` facade.
- Add the pure formation mesh builder and focused topology/determinism tests.
- Stop if any source variant is invalid or representative formations are disconnected.

### Batch 3 — Streamed Runtime Integration

- Add the serial worker queue, chunk-instance invalidation, stable completion order, bounded main-thread application, and shutdown cleanup.
- Replace the decorator's old multi-member append path.
- Preserve single-member behavior, naming, renderer/material/shadow settings, compound colliders, traversal component, stable IDs, dust data, and chunk ownership.

### Batch 4 — Validation And Documentation

- Run focused fusion tests, existing natural-object/world EditMode tests, and the non-mutating validator through the background-safe Unity path.
- Audit deterministic regeneration, unload-before-completion, unload/reload, native and Unity resource cleanup, and existing two-millisecond world-work budget.
- Record measured current-mac profiling evidence without claiming shipping-platform acceptance.
- Update feature and third-party documentation to match proved behavior.

## Proof Requirements

- All cached source variants are finite, consistently wound, positive-volume, nondegenerate, and closed two-manifold after deterministic welding.
- Multi-member output is nonempty, finite, positive-volume, nondegenerate, closed two-manifold, and exactly one connected component.
- Repeated identical inputs produce identical canonical vertex/index hashes on the current proof platform.
- Stable formation/member ID construction, root-chunk ownership, hierarchy order, renderer count, material assignment, and collider count are unchanged. The physical generation version advances to 4 while the cosmetic stream remains unchanged.
- No native operation or Unity object access occurs on the worker-main-thread boundary incorrectly; stale chunk results are never applied and all owned resources are released.
- Existing focused tests and validator remain green.
- Native work stays off the main thread; apply work is measured against the existing two-millisecond world-work budget.
- User visual acceptance remains required for seams, silhouette, load presentation, and traversal feel.

## Hard Stops

Stop instead of adding a workaround when:

- A source variant is not a valid manifold after exact welding.
- The corrected planner still accepts a parent-child edge without a deterministic point proven strictly inside both closed source volumes.
- The pinned ABI cannot be loaded or safely owned on the current macOS target.
- Determinism, stale-result rejection, resource cleanup, or focused validation cannot be proved.
- Completion requires changing placement beyond the approved version-4 contact-validity rule, colliders, shader/material behavior, final platform policy, or another owner’s files beyond the narrow agreed integration.
- Completion requires foreground Unity interaction, a player/release build, staging, commit, push, deployment, or publication not authorized in the current thread.

## Implementation Checkpoint — 2026-08-14

Implemented in the canonical runtime path:

- Pinned Manifold v3.5.2 at upstream commit `11235e6b8ebea2dbed8aec4285685aafd3d95667`, preserved its Apache-2.0 license, and added the guarded reproducible macOS build recipe at `Tools/Native/Manifold/build-macos.sh`.
- Added universal arm64/x86_64 `libmanifold.dylib` and `libmanifoldc.dylib` plugins with macOS-only importer metadata and loader-relative linkage.
- Added deterministic source normalization, closed-manifold validation, safe C-ABI ownership, one batch-union facade, deterministic output normals/bounds/hash, a serial fusion worker, chunk-generation invalidation, one-completion-per-frame application, and fail-closed atomic Unity object creation.
- Replaced the old multi-member overlapping-shell renderer path. Single-member triangle-soup behavior and compound member box colliders remain unchanged.
- Added focused source/native/determinism/invalidation/unload-reload/integration tests and a non-mutating dependency validator.

Evidence available without entering the open Unity Editor:

- Upstream Manifold suite: 427 of 427 tests passed in 146.67 seconds.
- Direct C-ABI probe: two overlapping unit cubes produced status 0, one component, 16 vertices, 28 triangles, and volume 1.5.
- Managed C# interop/worker probe: the same union produced valid topology and volume 1.5; the serial worker applied once and drained to zero outstanding requests.
- Unity Roslyn-input compilation: the full touched runtime assembly, editor assembly, and focused EditMode test assembly compile cleanly. New asset metadata has no missing or duplicate GUIDs.
- Plugin SHA-256: `libmanifold.dylib` is `d11e68c985dee63b57dbf54d18f6ec030caf348cd893ee76dad225a40fa1dc70`; `libmanifoldc.dylib` is `e7f3f0fea0482e89d16e5490ae68c2d2e47b4872153501cf764a6b9d87847dbb`.

Unity mirror evidence gathered after this checkpoint was first written:

- An exact temporary mirror of current `Assets`, `Packages`, and `ProjectSettings` imported under Unity 6000.4.0f1 without competing with the GUI-owned live checkout.
- The rock dependency validator passed through its dedicated non-mutating CLI entry point.
- All eight focused fusion test cases passed. The corpus covers all sixty source variants, the native cube ABI, stale-result rejection, unload/reload generation rejection, single-member identity, 48 deterministic multi-member formations with independent proof for every recorded parent edge and exactly one final component, plus the two exact narrow-sliver regressions found by the robustness audit.
- The focused deterministic/root-owned chunk-plan, formation-topology/caps, immediate-parent contact/blend-range, physical-version/cosmetic-isolation, and decorator integration tests all passed.
- The decorator integration produced one renderer per formation, one box collider per member, stable sibling ordering, and a measured maximum main-thread apply of 1.208 milliseconds against the two-millisecond budget.
- The full touched runtime, editor, and test compilation surfaces compile cleanly from Unity's Roslyn inputs.
- The seeded fusion corpus first exposed a valid narrow intersection triangle with cross-product magnitude squared `1.86213349E-18`. The later robustness audit found a smaller valid triangle at `5.596066E-21` and a connected output whose accumulated normal magnitude was only `2.3796124E-06`, below Unity's normalization epsilon. Topology validation now computes triangle cross products and signed volume in double precision, rejects only non-finite or true zero-area triangles, and normal generation accumulates and normalizes explicitly in double precision.
- A disposable mirror-only 17-by-17 chunk sweep validated all 451 multi-member formations against the actual corrected code. Every formation produced finite unit normals, positive-volume one-component closed topology, and the same canonical vertex/index hash on an immediate repeat. The test took 100.99 seconds; measured fusion work was 1,157.30 milliseconds total.
- Version 3 exposed a projection-only parent/child pair with no real volume intersection. An early version-4 candidate then exposed a three-way overlap that produced a numerical component of approximately `-1.0E-09` volume. Input tolerance and ordered-union experiments did not resolve it and were removed.
- The canonical correction is source-side and versioned: an accepted parent edge must have the overlapping-bounds center strictly inside both closed volumes with scale-aware clearance, and conservative projected support circles of non-parent members may not overlap. The native owner remains the single flat Manifold batch union; no connector, component-dropping rule, fallback renderer, tolerance heuristic, or duplicate CSG path remains.

Remaining proof outside this implementation lane:

- A broader natural-object fixture passed all five rock integration checks but retained two unrelated failures in root-spacing precedence and abundance distribution. The general TopDown3D prototype validator also stops on an unrelated Humanoid animation-driver requirement. Those planner/animation lanes must be reconciled by their owners rather than absorbed into mesh fusion; the dedicated rock-fusion validator passes.
- User-owned visual/play acceptance remains required for silhouette, seams, load presentation, and traversal feel in the GUI-owned live checkout.
- Additional shipping platforms require separately pinned native binaries and target-specific validation.

Do not focus or control the GUI Editor, weaken the component/topology tests, add a visual fallback or connector, silently retune placement, or advance the generation version again inside this completed mesh-fusion lane.

## Historical Completion Boundary — Superseded

The buildout is complete when the canonical runtime uses the proved fused mesh path for multi-member formations, focused automated proof and the existing validator pass in the current macOS development environment, documentation is truthful, unrelated dirty work is preserved, and the remaining evidence is limited to user-owned visual/play acceptance or separately authorized platform/build/publication work.

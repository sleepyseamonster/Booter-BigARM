# Minimal cube rock verification plan

Date: October 10, 2026. Status: planned checks, not executed. Applies only to the [minimal implementation plan](MINIMAL_CUBE_ROCK_IMPLEMENTATION_PLAN.md).

The proof must establish that duplicating and moving cubes yields a smooth exterior through ordinary Unity editing, without extra authoring controls. Geometry checks and user appearance acceptance are separate.

## Safe execution

Use Unity `6000.4.0f1` and the existing Editor test assembly. Build an isolated candidate project from the exact task source and pinned packages before running focused tests. Do not reuse another terrain task's validation copy or run batchmode against the live project's lock. Capture images only in an isolated background project, sequentially with tests. Preserve the foreground application.

Proposed test filter: `BooterBigArm.Tests.SmoothRockCubeTests`. This fixture and any new test entry point do not exist yet. Verify command discovery and the compiled fixture before invoking them through the repository's [Unity automation workflow](../../Engineering/UNITY_AUTOMATION.md). Run only the old workbench tests directly affected by the extraction change; inspect their setup/teardown and use the isolated copy. No gameplay smoke tests, broad repair commands or Player builds are part of this Edit Mode slice.

## Proof matrix

| Claim | Fixture and assertion | What it establishes |
| --- | --- | --- |
| Simple authoring | Prefab/component inspection and source review: one cube, no custom panel/buttons/settings or workbench dependency | The author receives the requested small workflow |
| True singleton | Unit cube field at face, edge, corner and interior points; extracted bounds and volume agree within a declared sampling tolerance | Plain box source rather than a weathered primitive |
| Neutral duplication | Duplicate at the identical pose; field and exterior match singleton within declared tolerance; both editable sources retained | Duplication does not swell the rock before movement |
| Actual contact | Face-touch pair, overlap pair, rotated separated pair whose world bounds overlap, and two distinct scenes | Geometric membership instead of bounds-only grouping |
| All touching cases | Exact edge and corner fixtures at several grid offsets; positive-volume connected exterior or a documented failed gate | Touch behavior is tested rather than silently excluded |
| One exterior | Edge-incidence/winding, finite vertices, positive volume and component count; inspect internal contact plane and buried triangle samples | Removal of inner surfaces, not just smooth shading |
| Stable arrangement | Reload fixed recipe, reorder Hierarchy, vary discovery order, move sources across ordering positions, test duplicate identifier repair if used | Reproducibility and absence of ordering-induced pops |
| Multi-cube shape | Three-way stack, chain and U-shaped opening; compare union bounds/volume, neck thickness and intentional gaps | Swelling and gap loss are quantified before appearance acceptance |
| Automatic updates | Duplicate/move/separate/delete/disable with Editor updates; verify source states and current group membership | Current arrangement displayed without user buttons |
| Undo and cleanup | Undo/Redo duplication, movement and deletion; scene close, domain reload, prefab stage exclusion and Edit/Play transitions | No stranded rendering suppression, leaked mesh or cross-context mutation |
| Source selection | Source selected through Hierarchy and Scene picking; Transform handles manipulate that cube | The fused display does not make editing inaccessible |
| Texture continuity | Shared material identity and fixed property block values; matched isolated renders of singleton, pair and stack | No texture reset, scaling jump or artificial painted join seam |
| Bounded work | Record cube count, group span, effective grid, samples, vertices/triangles, rebuild latency and cleanup counts | Measured limits for this study, not a world-population budget |
| Preservation | Before/after hashes for production scene, terrain, shared materials, packages and settings; study diff or live insertion readback | Changes stay within the named task boundary |
| Persistence boundary | Save/reopen an isolated study with sources; ensure transient outputs are absent from serialization and rebuilt. Enter Play Mode and confirm source restoration | Edit Mode recipe persistence and explicit absence of runtime fusion |

Sampling tolerances must be stated before checking fixtures and tied to the fixed extraction spacing. Do not relax them after a failure to obtain green. Smooth fusion intentionally changes the union near joins; zero total volume change is not an acceptance claim. Measure the change and submit matched images for the author's judgment. Thin features narrower than the grid's reliable resolution cannot be accepted solely because a component counter says one.

## Existing regressions

Inspect and execute affected tests from `TopDown3DRockWorkbenchTests`: `OverlappingBoxesBuildOneClosedConnectedSurface`, `SeparatedBoxesReportTwoClosedSurfaceComponents`, `FaceTouchingBoxesSmoothIntoOneSurface`, `RotatedTouchingBoxesSmoothIntoOneSurface`, and `SurfaceRelaxationReducesVoxelScaleRoughnessWithoutLiftingTheRock`. Add the grouped-formation fixture only if its shared extraction call changes. Run the nine-area structural fixture after a saved scene change, adjusting only any formerly exclusive-content assertion that the added cube deliberately supersedes. Never weaken terrain, lighting, generator-count or build-exclusion assertions.

Sculpted-rock fusion is a separate path. Source inspection and unchanged-file checks are enough unless the implementation actually changes its code or assets; then run its affected checks. Do not absorb the existing untracked Fused output into this task.

## Evidence and delivery

Store final receipts and focused test XML under `Docs/Evidence/Art/SmoothCubeRock/` only after tests exist and run. Keep raw logs and intermediate captures in ignored task-local diagnostics; promote only named useful evidence. A receipt should identify the commit/candidate, source hashes, Unity version, fixed internal constants, fixtures and counts, topology outcomes, measured cost, protected hashes, saved versus live placement, and outstanding visual acceptance.

The author reviews a singleton and a small offset stack from the study view. The only creative question is whether the joins look like the desired rock; do not require learning a settings panel. Hands-on manipulation and gameplay smoke testing remain author-owned. If appearance needs revision, change the internal fixed behavior within the authorized task rather than expose more controls.

Closeout reports compilation/import, focused checks, isolated images, live placement, saved placement and author acceptance independently. Do not claim Player readiness or arbitrary high-density performance. Stop when the minimal interaction meets these gates; a failed gate identifies the specific remaining problem rather than prompting adjacent feature work.

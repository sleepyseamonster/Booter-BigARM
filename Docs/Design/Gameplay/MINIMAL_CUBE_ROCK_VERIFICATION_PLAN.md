# Minimal cube rock verification plan

Date: October 10, 2026. Status: scoped checks executed; [delivery evidence](../../Evidence/Art/SmoothCubeRock/README.md) links the original proof and subsequent texture/smooth-join corrections. The latest join run passes 29 cases. Applies only to the [minimal implementation plan](MINIMAL_CUBE_ROCK_IMPLEMENTATION_PLAN.md). Matrix entries describe the proof contract, not unrestricted claims beyond the tested fixtures.

The proof must establish that duplicating and moving cubes yields a smooth exterior through ordinary Unity editing, without extra authoring controls. Geometry checks and user appearance acceptance are separate.

## Safe execution

Use Unity `6000.4.0f1` and the existing Editor test assembly. Build an isolated candidate project from the exact task source and pinned packages before running focused tests. Do not reuse another terrain task's validation copy or run batchmode against the live project's lock. Capture images only in an isolated background project, sequentially with tests. Preserve the foreground application.

Implemented test filter: `BooterBigArm.Tests.SmoothRockCubeTests`, now with 23 cases including volcanic imports and the author's offset join contour. Verify command discovery and the compiled fixture before invoking it through the repository's [Unity automation workflow](../../Engineering/UNITY_AUTOMATION.md). The final join run also executes the six affected old workbench checks named below. Captures use `BooterBigArm.Tests.SmoothRockCubeTests.CaptureReview` or `CaptureJointReview` only in the isolated background project. No gameplay smoke tests, broad repair commands or Player builds are part of this Edit Mode slice.

## Proof matrix

| Claim | Fixture and assertion | What it establishes |
| --- | --- | --- |
| Simple authoring | Prefab/component inspection and source review: one cube, no custom panel/buttons/settings or workbench dependency | The author receives the requested small workflow |
| True singleton | Unit cube field at face, edge, corner and interior points; original singleton render bounds and volume match its box; multi-box extraction uses a separately declared sampling tolerance | Plain box source rather than a weathered primitive |
| Neutral duplication | Duplicate at the identical pose; retain original singleton display, both editing sources and original shading. Move the duplicate through small offsets away from coincidence | No swelling on duplication or discontinuous jump immediately afterward |
| Actual contact | Face-touch pair, overlap pair, rotated separated pair whose world bounds overlap, and two distinct scenes | Geometric membership instead of bounds-only grouping |
| All touching cases | Exact edge and corner fixtures at several grid offsets; positive-volume connected exterior or a documented failed gate | Touch behavior is tested rather than silently excluded |
| One exterior | Edge-incidence/winding, finite vertices, positive volume and component count; inspect internal contact plane and buried triangle samples | Removal of inner surfaces, not just smooth shading |
| Stable arrangement | Reload fixed recipe; permute creation, Hierarchy and discovery order; cross cube positions; keep geometry fixed during permutations | Geometrically order-independent output and continuous movement, not merely ID-stable folds |
| Multi-cube shape | Three-way stack, chain and U-shaped opening; compare union bounds/volume, neck thickness and intentional gaps | Swelling and gap loss are quantified before appearance acceptance |
| Automatic updates | Duplicate/move/separate/delete/disable with Editor updates; dirty a pose during an outstanding job; retire old display immediately, reject stale completions and rebuild unchanged scenes only when needed | Current arrangement displayed without user buttons or misleading old exterior |
| Undo and cleanup | Duplicate an already fused/hidden source, then separate and Undo/Redo it; Save with live preview; scene close, domain reload, worker cancellation, prefab stage exclusion and Edit/Play transitions | No inherited suppression, serialized transient state, leaked mesh/job or cross-context mutation |
| Source selection | Select visible, coincident and fully contained sources through Hierarchy; map exterior Scene hit to a contributing source; keep source and Area transforms from being applied twice | Each editable cube remains reachable without buttons or gameplay colliders |
| Texture continuity | Shared material identity and fixed property block values; matched isolated renders of singleton, pair and stack | No texture reset, scaling jump or artificial painted join seam |
| Bounded work | Include a 3 m face-touch chain and grid offsets; record cube count/span, padded bounds, requested/effective grid, neck thickness, samples, vertices/triangles, total latency, worst main-thread stall and cleanup counts | Sampling policy and Editor responsiveness are tested, not inferred from one job per update |
| Preservation | Before/after hashes for production scene, terrain, shared materials, packages and settings; study diff or live insertion readback | Changes stay within the named task boundary |
| Persistence boundary | Save/reopen an isolated study with sources; ensure transient outputs are absent from serialization and rebuilt. Enter Play Mode and confirm source restoration | Edit Mode recipe persistence and explicit absence of runtime fusion |

Sampling tolerances must be stated before checking fixtures and tied to effective extraction spacing. Do not relax them after a failure to obtain green. Check the extracted exterior against the chosen blended field, not only against the original cubes: blending intentionally changes their union. Surface samples and small offsets on each side of a face must agree with a boundary of that field; inspect internal sheets and bounded-fixture self-intersections independently of edge-incidence checks. Exact closed topology alone does not prove a correct exterior. Measure shape change and submit matched images for the author's judgment. Thin features narrower than reliable grid resolution cannot be accepted solely because a component counter says one.

## Existing regressions

Inspect and execute affected tests from `TopDown3DRockWorkbenchTests`: `OverlappingBoxesBuildOneClosedConnectedSurface`, `SeparatedBoxesReportTwoClosedSurfaceComponents`, `FaceTouchingBoxesSmoothIntoOneSurface`, `RotatedTouchingBoxesSmoothIntoOneSurface`, and `SurfaceRelaxationReducesVoxelScaleRoughnessWithoutLiftingTheRock`. Include the grouped-formation fixture if its shared extraction call changes, even when its wrapper signature does not.

`NineFlatAreasHaveTheSameBaseAndExactlyOneEditableRockEach` hardcodes Area 05's original three children, one basalt at zero position/unit scale, zero terrain heights and absence of stickers. Current artist work can legitimately differ. Establish pre-task baseline results in the isolated candidate first. Do not reset artist poses, painting or terrain to make that historical layout assertion pass. Update only task-obsolete content expectations with explicit checks for the added cube and preserved existing content. Verify terrain heights/weights, lighting, material/layer identity, generator count and build exclusion against the captured current baseline; retain valid original invariants where that baseline still satisfies them. Report unrelated pre-existing failures separately rather than deleting their assertions or calling the whole suite green.

Sculpted-rock fusion is a separate path. Source inspection and unchanged-file checks are enough unless the implementation actually changes its code or assets; then run its affected checks. Do not absorb the existing untracked Fused output into this task.

## Evidence and delivery

Store final receipts and focused test XML under `Docs/Evidence/Art/SmoothCubeRock/` only after tests exist and run. Keep raw logs and intermediate captures in ignored task-local diagnostics; promote only named useful evidence. A receipt should identify the commit/candidate, source hashes, Unity version, fixed internal constants, fixtures and counts, topology outcomes, measured cost, protected hashes, saved versus live placement, and outstanding visual acceptance.

The author reviews a singleton and a small offset stack from the study view. The only creative question is whether the joins look like the desired rock; do not require learning a settings panel. Hands-on manipulation and gameplay smoke testing remain author-owned. If appearance needs revision, change the internal fixed behavior within the authorized task rather than expose more controls.

Closeout reports compilation/import, focused checks, isolated images, live placement, saved placement and author acceptance independently. The proposed Edit Mode boundary is an explicit limitation, not user-approved Play Mode behavior. Do not claim Player readiness or arbitrary high-density performance. All-contact geometry, order independence and clone/save restoration are required gates; recording their failure does not count as completion. Stop when the minimal interaction meets these gates; a failed gate identifies the specific remaining problem rather than prompting adjacent feature work.

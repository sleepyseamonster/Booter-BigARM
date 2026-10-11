# Minimal cube rock plan audit

Date: October 10, 2026. Audit baseline: `f751eae26` on `main`. Owner: Gottspan. Scope: review and repair of the [implementation plan](MINIMAL_CUBE_ROCK_IMPLEMENTATION_PLAN.md) and [verification plan](MINIMAL_CUBE_ROCK_VERIFICATION_PLAN.md). No Unity implementation, scene save, test execution or package change occurred.

The intended interaction remains appropriate: duplicate textured cubes, move them, and automatically blend touching groups without extra controls. The original plan overstated readiness. Its geometry choice and sampling policy were still unresolved, and several editing/persistence gates were too vague. The plans now separate a small geometry experiment from conditional authoring integration.

## Findings and repairs

| Priority | Finding | Evidence and consequence | Repair |
| --- | --- | --- | --- |
| P1 | Fixed sampling contradicted the existing extractor | `TopDown3DRockWorkbenchMesher` expands bounds and coarsens spacing to fit 96 cells. At the proposed constants a 3 m chain needs 131 cells, so effective spacing becomes about 0.034 m instead of 0.025 m. Thin joins may disappear at some grid offsets | Choose sampling and blend together; require effective-grid and contact-neck proof over a 3 m fixture before integration |
| P1 | Hidden IDs hid rather than solved order-dependent geometry | The researched polynomial operation is nonassociative. Stable IDs reproduce one fold, but identical cube poses created in another order can yield another shape; IDs add clone/Undo state | Remove source-order IDs from the proposed marker. Require geometric order independence and continuous near-coincident movement; operator selection is a Batch 1 gate |
| P1 | One group per update did not bound Editor stalls | Existing extraction samples the whole grid and normalizes/orients the mesh synchronously. Limiting job count does not bound a single job's cost | Use a bounded numeric worker or prove a small synchronous path's worst stall; keep Unity objects on the main thread and test cancellation/stale completion |
| P1 | Source duplication and Save lacked transient-visibility proof | Existing preview patterns use `forceRenderingOff` and per-instance restoration dictionaries. Whether suppression copies or survives serialization in the proposed mechanism must be checked in the pinned Editor; a new instance has no old dictionary record | Explicit clone/save/reload restoration contract and tests before placement; do not treat inherited suppression as intended visibility |
| P2 | Change detection only rejected stale completions | The old displayed mesh could still remain at an obsolete pose while rebuilding | Retire stale display and restore sources as soon as inputs change |
| P2 | Singleton-to-duplicate appearance could change | Switching a built-in singleton cube to extracted geometry on duplication can alter silhouette and normals, even with a deduplicated field | Use original source geometry when there is one distinct box; test motion away from coincidence |
| P2 | Historical scene assertions could reject valid author edits | `NineAreaRockStudyTests` hardcodes three center children, zero basalt position, flat heights and no stickers. The live baseline already differs from its original center arrangement | Baseline the isolated candidate first; protect current authored contents and terrain, update only obsolete task-specific expectations, report unrelated failures |
| P2 | Geometry proof could reject the intended blended exterior or miss internal sheets | Comparing only with original cubes conflates exact union with blending; edge incidence and volume are insufficient for self-intersection or internal boundary correctness | Validate against the selected blended field, local boundary sides and relevant bounded-fixture intersection checks |
| P2 | Selection and transform ownership were underspecified | A fused hit has no unique source at every point; contained/coincident cubes may have no exposed surface. World-space mesh under a transformed parent can double-transform | Define exterior-hit mapping and ordinary Hierarchy access; use an unscaled preview root; remove accidental primitive/gameplay colliders |
| P2 | Edit Mode only was presented as an author decision | The author requested a study interaction but did not explicitly choose appearance changes on entering Play Mode | Label it as the proposed slice boundary and disclose it before integration; revise persistence if Play Mode fusion is requested |

## Evidence and limits

Inspected the current extractor's bounds, 96-cell cap, sampling and result construction; the exact-position topology validator; sculpted preview hiding and selection; shader origin/seed/size inputs; current assembly names; and the nine-area structural test. The source files are linked in the [research report](../../Research/Art/SMOOTH_CUBE_ROCK_FUSION_RESEARCH.md).

[Unity's forceRenderingOff documentation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Renderer-forceRenderingOff.html) confirms rendering suppression without changing `enabled`. It does not establish clone or serialization behavior for the proposed service; those are targeted proof requirements, not confirmed bugs in an unimplemented cube system.

A direct arithmetic check gives padded span `3 + 2 * (0.1 + 0.025 * 1.25) = 3.2625 m`; the cap gives `3.2625 / 96 = 0.033984375 m`. This is an extraction-policy calculation, not a failed Unity fixture or measured neck radius. The blend's equal-input offset `k/4 = 0.025 m` indicates the need to investigate sub-cell contact geometry; it does not itself prove disconnection.

The audit preserves existing dirty BigARM material and untracked Fused assets. Documentation verification checks links, task scope and whitespace. No new geometry or lifecycle behavior is claimed proved.

## Readiness

Batch 0 and a bounded geometry experiment are ready to scope under a future implementation instruction. The full buildout is conditional: first select a blend/sampling policy that passes contact, ordering and near-coincident fixtures, then prove responsive clone/save-safe editing. The corrected plan avoids adding user controls as a workaround for these technical gaps.

No subagent, production dressing, runtime mesher, export panel, LOD system or new user-facing setting is required by this audit. Additional features remain author-requested work.

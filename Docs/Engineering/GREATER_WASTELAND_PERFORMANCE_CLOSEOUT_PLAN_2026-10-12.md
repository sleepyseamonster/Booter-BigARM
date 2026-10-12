# Greater Wasteland performance lane: audit and closeout plan

October 12, 2026. Owner: Gottspan, with Babineaux validation and Gear Ball local publication. This is the author's requested closeout reassessment. It supersedes open-ended optimization next steps in the implementation log; it does not retroactively accept unverified gameplay or change the original resident-terrain requirements.

## Decision and scope

Freeze further performance tuning. Close the implemented terrain/hitch foundation and bounded population integration through a finite evidence and acceptance pass. Fix only a demonstrated regression or a missing requirement necessary for this scope. Do not start another optimization merely because a timing maximum can be lower.

The original objective remains: resident terrain query/render separation, runtime rendering window, targeted hitch fixes, deterministic population foundation and bounded sector integration, preserving native terrain, collision, gameplay and saved state. Whole-world population rollout was not delivered and must not be reported as delivered. Production seed/catalog selection, save migration, dense world rollout, 64/128 m presentation experiments and GPU/terrain alternatives are separately proposed follow-on work; the author must agree to their disposition before final scope signoff. They are not silently completed or removed from the architecture roadmap.

“Rebase” is applied here to the plan/evidence baseline. Work remains on main tracking origin/main; no Git history rewrite, push, package change or asset move is part of this closeout.

## Current baseline

- Latest implementation checkpoint: `4e66cb0f2`; 103 focused EditMode cases passed. Current source/artifact hashes were rechecked against the [microstep receipt](../Evidence/Performance/population-microsteps-implementation-2026-10-12.json).
- Live production scene contains 8,960 serialized Terrain and 8,960 TerrainCollider components. SHA-256: `2747f07f81eed430cc66e339bf6e287a903b8a2bd41ff5af549d13447bf4fc89`. It matches the [terrain owner's reconciliation](../Evidence/DeathValley/NORTH_ROW4_STEP05_COMMITTED_SCENE_RECONCILIATION_2026-10-12.json). This matching scene hash is not a new exhaustive native-data validation or Player measurement.
- Earlier full-world timings used the frozen 8,704-tile snapshot. Keep that scope visible. Reference population timings use sixteen native tiles; they do not benchmark the entire populated world.
- Production terrain rendering and hitch fixes are runtime code. Procedural population is opt-in reference integration, with real controller preparation bindings; it is not installed in Greater Wasteland or its production save path.
- Unrelated dirty camp/BigARM materials and Fused art remain outside the lane. No changes to those assets are needed for this plan.

## Requirement audit

| Original requirement/gate | Current evidence | Status for closeout |
| --- | --- | --- |
| Fixed terrain/data/collision remain resident | Query/window tests, retained collider counts, current scene inventory/owner receipt | Implemented; preserve and reconcile current content before final signoff |
| Queries work when rendering is disabled; holes/seams/data mismatch reject | Focused native-query cases included in latest suite | Proven within fixture/reference scope |
| Runtime render window, immediate arrivals, bounded departures, restoration, camera/shadow coverage | Code and lifecycle tests; [corrected camera sweep](../Evidence/Performance/camera-transition-resolved-receipt-2026-10-12.json) | Implemented; author visual acceptance and current-scene gameplay remain open |
| Failed route retry and one route build per query | Route/pathfinder regression cases in latest suite | Implemented; actual moving-gameplay hitch magnitude not established |
| Ordered checkpoint writer, immutable captured revisions, backups/recovery and shutdown | Checkpoint/persistence cases in latest suite | Implemented; normal gameplay save behavior still needs author acceptance |
| Deterministic IDs, neighbour rules, authored masks, sparse removals, recreation/version rejection | Identity/state/planner/presentation cases, independent reference vector, exact public/planned receipt comparison | Proven for the bounded contract; production migration not delivered |
| Shared art/LOD, bounded scheduling, independent actor collision and reference integration | Saved actor scene, readiness/synchronization tests, allocation controls, [spatial demand pair](../Evidence/Performance/population-spatial-demand-receipt-2026-10-12.json) | Implemented in reference lane; author composition/traversal acceptance open |
| Timings, memory, startup, transitions and available CPU/GPU evidence | Raw offscreen traces, unit clocks, memory watchdogs and camera/absent-view/restore transitions | Diagnostic evidence available; GPU/upload-specific and presented gameplay evidence unavailable |
| 60 FPS at 1080p without harming appearance/gameplay | No accepted normal presented-gameplay run yet | Unproven; do not convert manual timings to FPS |
| Conditional GPU/proxy/terrain/database experiments | No need established by this lane's diagnostic results | Future decisions, not automatic prerequisites |

The latest reference change reduces normal residency from 390 to 182 instances, retains 81 under absent camera demand, and keeps native collision enabled. One same-binary pair measured 1.33 versus 1.91 ms mean normal updates. Current microsteps perform at most one hash per operation; the largest sampled planner step was 4.47 ms and total startup population work 7.11 ms. These are useful diagnostics, not an absolute time guarantee or accepted gameplay FPS. Initial readiness did not improve consistently. Do not spend further closeout time chasing these cold maxima without a user-visible failure.

## Finite closeout sequence

1. **Freeze and reconcile the evidence package.** This plan, current inventory/hash, the existing implementation log and corrected architecture baseline form the candidate. Check task-owned source/metadata/evidence linkage, current Unity import/compile state and applicable existing owner validation. Reuse accepted matching evidence rather than repeating tests unchanged. If a source or asset changed after its relevant proof, run only the affected gate. Finish one current-content compatibility pass; no repeated A/B captures or new experimental branches.
2. **Resolve acceptance through the author.** Author-owned normal gameplay should cover terrain/distant hills and shadows, camera movement, Booter traversal, offscreen BigARM/route preparation, save/resume, and the bounded actor reference's population appearance. Record actual observations and any presented frame-time evidence separately from diagnostics. Do not launch/focus Unity, create gameplay smoke tests, or claim acceptance from a PNG. If the author is not ready, leave this gate pending rather than continue tuning around it.
3. **Close or make one targeted repair pass.** For a demonstrated regression, fix the smallest relevant cause and rerun its affected checks. Otherwise record acceptance, remaining measured limitations and the author's disposition of follow-on architecture items, commit only lane-owned closeout files and mark the implementation goal complete only when its retained requirements are proven. No push/publication follows automatically.

## Stop rules and deferred work

No routine micro-optimization, density expansion, shader/quality reduction, package change, renderer migration, terrain merging/streaming, database installation or production population/save conversion during closeout. No promise to eliminate every isolated cold-start or scheduling outlier. No new crash investigation unless the author requests it or validation produces a concrete failure; always-loaded native memory remains a known limitation.

A bounded reference lane can close without pretending the full procedural world has shipped. The proposed future world-construction lane covers production world identity/catalog/migration, authored POI/route/camp constraints, spatial presentation partition selection, dense population and representative checkpoint growth. If the author considers any of these necessary to the original lane's closeout, retain it as an explicit missing requirement and revise the finite plan before proceeding; do not silently relabel success.

Current result: **implementation has substantial verified progress; final closeout is not yet proven**. The next actions are reconciliation and acceptance, not another tuning loop.

## Reconciliation outcome — October 12

Step 1 is reconciled against the [closeout receipt](../Evidence/Performance/performance-closeout-reconciliation-2026-10-12.json). All 206 production runtime sources present in the tested copy match, except the documented candidate-only batch cursor guard; paired metadata match. The latest 103-case test/evidence hashes are valid. The live 8,960 terrain/collider scene hash matches its owner receipt. The background Editor initially held stale assemblies; a script-only compilation request resolved that, and read-only reflection now sees current planner/receipt types with zero current console errors/warnings. No scene was opened/saved/focused and no Player or test suite was rerun.

Remaining gate: author acceptance and explicit disposition of follow-on work. Keep tuning frozen. This reconciliation is not a fresh exhaustive native-data validation or a presented-gameplay/visual acceptance result.

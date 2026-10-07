# Batch 7 geological rock evolution evidence

Date: 2026-08-24

## Production authority change

- `TopDown3DNaturalObjectPlanner` no longer calls the legacy cell-density physical-rock planner.
- `WorldRockFormationPlanner` now owns absolute-space landform and formation reservations, structural orientation, composition goals, member genealogy, stable formation/member IDs, and novelty fingerprints.
- Chunk coordinates select half-open owner areas only. They do not seed or shape formations.
- Formation and member identity uses `WorldVersionDomain.Decoration`; saved runtime deltas can address either without serializing geometry.
- The adapter preserves the existing baked mesh families, three LODs, collider data, material surface choice, and natural-object decoration consumer.
- Planning has a dedicated `TopDown3D.World.PlanGeologicalRocks` profiler marker.
- Absolute formation identity and ownership survive local-origin rebasing. Only realized Unity positions are localized.
- Site reservations, approaches, BigARM reserved routes, and the protected spawn area reject physical formation roots and members.
- The proof profile remains explicitly non-canon. No coordinate bounds, projection, wrapping, region names, or lore were introduced.

## Composition and repetition controls

- Two independent reservation scales create broad density rhythm: 96 m landform anchors and 44 m formations.
- Anchor reservations protect breathing room from smaller formations.
- Four technical composition goals vary parent graphs, scale descent, structural alignment, member count, aspect, and direction.
- Content-derived novelty fingerprints exclude world IDs and location so repeated silhouettes can be measured instead of hidden by unique identifiers.
- The bounded proof transect requires more than 92% unique novelty fingerprints and at least three composition goals across both reservation scales.

## Verification receipts

- `WorldRockFormationPlannerTests`: 5/5 passed (`/tmp/booter-batch7-rocks-final.xml`).
  - deterministic formation/member identity and genealogy
  - exclusive boundary ownership
  - route/site reservation protection
  - silhouette fingerprint diversity and density rhythm
  - baked-family realization, rebase/reload identity, and non-parent collision separation
- Complete World Creator namespace: 70/70 passed (`/tmp/booter-batch7-worldcreator.xml`).
- Topology-v2 world-generator regression: 9/9 passed (`/tmp/booter-batch7-worldgen.xml`).
- Focused natural chunk-plan determinism/root ownership: 1/1 passed (`/tmp/booter-batch7-natural-focused.xml`).
- Production authority validator: passed (`/tmp/booter-batch7-validator.log`).
- Exact final Development StandaloneOSX build: succeeded (`/tmp/BooterBigArm-Batch7-Final.app`, `/tmp/booter-batch7-development-build-final.log`).
- The complete legacy `TopDown3DNaturalObjectTests` fixture was stopped after five minutes at sustained 100% CPU without a test receipt. This reproduces the pre-existing Batch 6 full-fixture behavior; it is not counted as proof.

## Remaining approval gates

- Fixed-camera visual review remains user-owned. Automated fingerprints prove content diversity, not artistic acceptance.
- Target-Windows GPU/player profiling remains required in Batch 10. Current editor/headless macOS checks are not target-hardware proof.

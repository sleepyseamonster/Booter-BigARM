# Batch 6 Topology-v2 Production Cutover — 2026-08-24

## Outcome

The production terrain path now uses one World Creator authority. Near collision terrain and non-colliding middle/far terrain are compiled from `IWorldQueryService` through the representation scheduler. The former `TopDown3DWorldGenerator` scalar/noise algorithm has been removed; its retained class is only a compatibility adapter for existing planners and ground queries.

The approved temporary Fractured Transect profile is loaded through `Resources/WorldCreator/ProductionWorldCreatorProfile`. The profile, coordinate adapter, province IDs, strata IDs, and influence IDs remain explicitly marked `proof.non-canon` and define no globe bounds, projection, wrapping, thematic notation, named regions, or lore.

The pre-cutover restoration point is commit `dc93ebc`.

## Production authority

- world identity: seed plus independent version manifest;
- active topology: version 2;
- coordinate seam: `NonCanonTechnicalCoordinateModel`, replaceable by the user-authored globe model later;
- query authority: bounded-cache `UnboundedHybridWorldQueryService`;
- near representation: 25-by-25 vertices per 18 m tile, with collision;
- middle representation: 9-by-9 vertices per 72 m tile, no collision;
- far representation: 5-by-5 vertices per 288 m tile, no collision;
- main-thread integration: four scheduler candidates and a 1 ms drain budget per frame, followed by bounded Unity mesh realization;
- precision: absolute keys survive local-origin rebasing; scene roots other than the generated-world root shift when the 1,536 m technical threshold is crossed;
- cache/pool limits: 256 representation entries, 96 MiB, 96 canyon plans, 24 terrain windows, and four retained buffer sets per resolution;
- materials: the existing terrain shader receives provisional semantic vertex packing from the canonical surface query; Batch 8 owns the final semantic material contract.

## Save compatibility decision

Prototype game-state snapshots are now format version 2 and include World Creator topology version 2. Load rejects version-1 snapshots, topology mismatches, and wrong-world seeds before applying inventory or companion data. No migration is provided during this development phase, as approved. Batch 9 will expand this into the full saved-place and persisted-delta manifest.

## Verification receipts

- World Creator and production-path EditMode selection: 65 passed, 0 failed (`/tmp/booter-batch6-worldcreator-final.xml`).
- Topology-v2 generator/cutover selection: 9 passed, 0 failed (`/tmp/booter-batch6-worldgen-final.xml`).
- Deposited-dust downstream regression: 9 passed, 0 failed in 98.24 s (`/tmp/booter-batch6-dust.xml`).
- Natural-object deterministic chunk-plan check: 1 passed, 0 failed (`/tmp/booter-batch6-natural-focused.xml`).
- Production-scene World Creator validator passed inside the 65-test selection. It checks the Resources profile, topology version, strict save format, scene wiring, scheduler use, and absence of the retired scalar algorithm from live near/middle/far terrain.
- Development StandaloneOSX build succeeded at `/tmp/BooterBigArm-Batch6.app` (`/tmp/booter-batch6-development-build-final.log`).
- A background, no-graphics Development Player run loaded the production scene and streamed the topology-v2 world without a World Creator exception (`/tmp/booter-batch6-development-player.log`). After warm-up, its last stable samples reported roughly 56.4-56.8 FPS, 17.1-17.7 ms average frame time, 18.9-19.7 ms p95, 21.0-21.5 ms p99, 10 MiB managed memory, 34-35 near chunks, and no pending terrain. These are macOS headless diagnostics, not Windows-target or GPU proof.

## Known non-cutover findings

- The broader foundation fixture remains 18/21 because of three pre-existing dirty-worktree failures: pending BigARM packing/save scene wiring, a dust-atmosphere expectation, and an exact character-material comparison. None is counted as Batch 6 proof.
- The full natural-object fixture remained CPU-heavy and was stopped after five minutes without a receipt. Batch 7 must move formation planning onto the World Creator planning/streaming lane and re-establish bounded proof before rock evolution is accepted.
- The Development Player logged the existing missing `Universal Render Pipeline/Particles/Unlit` footstep-dust shader. It is outside terrain-authority cutover scope and must not be mistaken for a World Creator failure.
- The headless player cannot prove GPU time, fixed-camera quality, Windows target performance, subjective traversal, or absence of visible LOD transitions.

## Remaining gates

Fixed-camera before/after review cannot be claimed because no valid pre-cutover camera receipt was captured before the old runtime authority was replaced. Commit `dc93ebc` preserves a reversible baseline if that comparison is later required. User visual acceptance and the target-class Windows Development Player profile remain mandatory final gates; they are not silently converted into automated proof.

# Batch 8 semantic material and dust evidence

Date: 2026-08-24

## Authority and rollback seam

- `WorldSurfaceMaterialService` is the renderer-independent authority for strata exposure, erosion, wind exposure, shelter, deposit, weathering, sediment, structural direction, prevailing wind, surface height, and slope.
- It resolves those signals from the deferred coordinate context plus canonical surface semantics. It introduces no water, plants, coordinate bounds, projection, wrapping, named regions, or lore.
- `WorldRepresentationCompiler` stores the canonical material sample beside each surface sample for every tier.
- `WorldTerrainMaterialPackingAdapter` is the only production RGBA packing seam for the existing terrain shader:
  - R: semantic deposit
  - G: erosion/sediment
  - B: strata exposure
  - A: weathering
- The existing shader, textures, transitions, parallax/normal treatment, far-detail branch, and playtest-fast branch remain the rollback-compatible realization layer.
- Representation keys and source fingerprints explicitly include the material version so cached geometry/material results cannot survive a material-contract version change.
- The independent material version remains `1`, consistent with the approved Batch 6 manifest; Batch 8 establishes its first production semantic contract rather than inventing an unapproved compatibility bump.

## Dust integration

- Production deposited dust derives its base coverage and height from semantic deposit, sediment, shelter, wind exposure, erosion, and slope.
- Geological formations supply bounded lee-side obstruction wakes; the legacy cell-density planner is no longer called.
- Prevailing wind is sampled from coordinate context at each surface location rather than reconstructed from the monolithic settings asset.
- Dust plans carry authoritative surface height so mesh realization does not reinterpret absolute chunk coordinates through a rebased local frame.
- Existing public local-noise and synthetic-wake helpers remain only as compatibility/test surfaces; production `BuildPlan` routes through the semantic path.

## Verification receipts

- Semantic material/dust suite: 6/6 passed (`/tmp/booter-batch8-materials-final.xml`).
  - deterministic bounded semantic samples
  - exact boundary continuity
  - exposed-strata versus depositional-ground causal response
  - identical near/mid/far corner material contracts
  - representation invalidation by material version
  - dust boundary and origin-rebase stability
- Complete World Creator namespace: 76/76 passed (`/tmp/booter-batch8-worldcreator-final.xml`).
- Deposited-dust regression suite: 9/9 passed (`/tmp/booter-batch8-dust.xml`).
- Topology-v2 world-generator regression: 9/9 passed (`/tmp/booter-batch8-worldgen.xml`).
- Production authority validator: passed (`/tmp/booter-batch8-validator.log`).
- Exact Development StandaloneOSX build: succeeded (`/tmp/BooterBigArm-Batch8.app`, `/tmp/booter-batch8-development-build.log`).
- Headless Development Player diagnostic (`/tmp/booter-batch8-development-player.log`):
  - steady state approximately 56.7-57.1 FPS
  - average frame time approximately 17.52-17.62 ms
  - p95 approximately 19.10-19.38 ms
  - p99 approximately 20.77-21.24 ms
  - managed memory 15 MiB
  - 30 loaded near chunks, no pending terrain, 32 renderers, 30 colliders

The player diagnostic used the reversible stress profile with runtime decoration disabled and no graphics device. GPU time was unavailable. It is CPU/streaming evidence, not fixed-lighting, fixed-camera, target-Windows GPU, or artistic acceptance proof. The existing unrelated missing footstep-particle shader error remained present.

## Remaining acceptance gates

- Fixed-camera/fixed-lighting visual comparison remains user-owned for Batch 10.
- Target-Windows GPU and player profiling remains required for final performance acceptance.

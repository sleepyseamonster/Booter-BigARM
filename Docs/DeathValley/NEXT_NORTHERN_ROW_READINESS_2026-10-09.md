# Next northern row readiness — October 9, 2026

Scope: preflight and baseline only, requested by the author before beginning the row. The full-row interpretation is six sections, built west to east. Acquisition, Blender authoring and production integration await the author's instruction to begin.

## Starting checkpoint

The controlling saved checkpoint is now the [Greater Wasteland reconciliation](../Evidence/Badwater/GREATER_WASTELAND_RECONCILIATION_2026-10-09.md) and [baseline JSON](../Evidence/Badwater/GREATER_WASTELAND_BASELINE_2026-10-09.json), superseding the handoff's unresolved sculpt/hierarchy state. The current local-only source policy also supersedes old source/LFS staging guidance.

- Inspected HEAD: `57589ac8e706b3956f2e4bf1bfee562e24da7d04`, on `main` tracking `origin/main`.
- Saved scene SHA-256: `09a8bf19b4ebf679a9baeb8a33bc26c9751e04e9d1d840c8ea3252720ee92bb8`.
- Latest source manifest SHA-256: `f23132a0fd1724fa0f806252960af3b0630c77af8e73104a015b7808601a20eb`.
- Fresh file comparison: all 4,608 TerrainData assets and their metadata match the sealed baseline, as do scene and manifest: 9,218 successful comparisons, zero mismatches.
- Production Unity connection: PID 13084, port 7800, Unity 6000.4.0f1, exact D: checkout. Initial live read found Greater Wasteland clean, outside Play Mode, not compiling, with 4,608 terrains.
- Blender background version check: 5.2.2 LTS, hash `d13f752e3b9c`. Repository Python `pip check` passed.

Local diagnostics are under `Logs/NextNorthernRowPreflight20261009/`. The existing full terrain validator passed with collision enabled: 4,608 terrains, 9,072 exact joins, 288 exterior slots and 115,200 collision samples. Maximum native readback deviation was 0.0274926424 m; maximum collider deviation was 0.0000610351563 m, both within unchanged tolerances. Every 257-square normalized grid was captured under the fresh local-only `SourceData/Terrain/DeathValley/NextNorthernRowPreflight20261009/Baseline01/` owner. Its 4,608 unique geographic cells exactly fill the accepted 96 × 48 rectangle. See the [fresh baseline receipt](../Evidence/DeathValley/NEXT_NORTHERN_ROW_BASELINE_2026-10-09.json) for inventory identities and proof limits. All 4,608 are retained for the proposed next row; the validator's retained/new fields describe its historical preceding batch.

The separate fixed-terrain dressing study wrote and committed its own files during preflight, including agent guidance and an isolated reference scene. Its closeout advanced HEAD to `30c7b5e536aecc827a31b69aa5c9e2c7a9071451`; these are separately owned commits. The final live read still found Greater Wasteland clean, outside Play Mode, with zero dirty TerrainData assets. Recheck HEAD, saved scene, dirty assets and storage at the actual implementation start.

## Proposed row contract

Use the exact geometry and per-step counts in the [handoff](NEXT_NORTHERN_ROW_HANDOFF_2026-10-09.md). Strip bounds are `[499920,4018488,524496,4022584]` in EPSG:26911; fixed Unity origin remains `[522448,4008248]`. Six additions produce 6,144 terrains in a fully occupied 96 × 64 grid. The first addition has only a south anchor, adds 496 joins and yields 320 exterior slots; later additions also require their west predecessor.

Create a distinct `north_row2` contract and new adapters. Preserve the old five-step contracts and historical proofs. Recheck the organized section/sector hierarchy; integration must append a section under the current single connectivity owner while preserving existing scene documents and geographic transforms. Never flatten the accepted hierarchy to satisfy older helpers.

Geographic identity stays deterministic through EPSG, lower-left coordinates, tile size and source version. Terrain remains authored and always loaded; no terrain generation, streaming, unload/reload or persisted runtime-delta format is needed for this row. Stable terrain identities and existing authored constraints must survive integration. Coordinate future dressing coverage separately; this preflight neither extends dressing placement nor redesigns gameplay.

## Source reconnaissance

A fresh official USGS TNM catalog query across the complete strip returned eight 1 m DEM products in `CA_FEMAR9Southeast_D24`: x49–x52, each with y402 and y403. The x49 sliver at the western end must be accounted for, as must the northing 4020000 ownership transition. The existing old-row product-selection code cannot be reused unchanged.

The established USGS NAIP ImageServer responded successfully; its envelope query returned 28 intersecting records without a service error or transfer-limit warning. Responses are retained in the local diagnostic folder. These catalog intersections establish reconnaissance only: no new DEM raster windows or imagery exports were acquired, and no complete finite-sample, datum, ownership-overlap or height-range proof is claimed.

The first implementation gate is bounded source acquisition and audit across all six footprints: exact 4,097-square native DEM samples, both-axis ownership, XML/datum/CRS, overlap differences, NoData, and aligned 2,048-square RGB imagery. Measure the complete elevation range before authoring. Stop if any accepted sample exceeds the unchanged −100 to 1,700 m encoding; do not clamp or rescale existing terrain. Fresh per-section baseline and anchor proofs still precede preparation.

## Capacity gate

Measured existing production Assets: 10.385 GiB; Library: 5.809 GiB. Existing isolated validation folders occupy 112.230 GiB collectively; this is an inventory, not authorization to remove them. D: started near 40.25 GiB free; the final read after study growth and the fresh full baseline was 36.33 GiB free.

The latest completed section provides a planning reference: 1.243 GiB of Unity-owned files, 0.139 GiB acquisition, 0.882 GiB preparation and 1.288 GiB baseline. New six-section sizes will differ with mountain complexity and source overlap. An initial full isolated copy with cache is approximately 16.2 GiB before growth. Six old-style isolated copies would exceed current free capacity before source or authoring work.

Do not claim a full-row capacity pass yet. Plan one bounded new isolated validation project, record each accepted predecessor before reuse, preserve required evidence and budget final runtime growth, Git objects, local source versions, native combined reviews and full baseline captures. Compress proven task-owned captures only with exact roundtrip/hash proof. Never delete existing validation projects, retired-source history, or the C: rollback to force this gate. Obtain a measured peak budget and working margin before expensive authoring; additional free D: space may be necessary.

## Authority and closeout

New Blender files and everything under `SourceData/Terrain/` remain local-only under the [source publication policy](../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Commit small receipts, code and approved native Unity assets only. New source retirement needs separate authority and versioned retirement support.

No foreground activation, scene save, terrain repair, package change, gameplay smoke test, push or source cleanup was performed by this readiness task. The baseline is ready to support source auditing. Authoring and integration remain gated on complete source/range proof and a passing peak storage budget. No new row section was begun.

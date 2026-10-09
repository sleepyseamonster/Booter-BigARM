# Further west and northwest pair

The user authorized another two Blender-built sections farther west on October 8, 2026. Current production is the accepted 2,048-terrain footprint from [the preceding batch](./NEXT_WEST_PAIR_IMPLEMENTATION_2026-10-08.md). All authoring, acquisition, validation and temporary build outputs for this batch stay on D:.

| Section | EPSG:26911 bounds in metres | Chunks |
| --- | --- | --- |
| `west_far` | `[504016,4006200,508112,4010296]` | 256 |
| `northwest_far` | `[504016,4010296,508112,4014392]` | 256 |
| Complete target | `[504016,4006200,524496,4014392]` | 2,560 total |

Preserve origin `[522448,4008248]` and the existing geographic-key contract. The resulting 80 × 32 grid has 5,008 internal seams and 224 exterior neighbor slots. All 2,048 retained assets, GUIDs, full height grids and authored scene objects are protected. Generation, streaming/unload/reload, generated-object identities and persisted runtime deltas are not implemented by this fixed authored terrain addition.

1. Verify fresh official USGS 1 m windows and bare-earth NAVD88 metadata for `x50y401`/`x50y402`, which nominally cover the entire new stripe. Require finite native grids, strict 2 m decimation and a verified height range; never clip to the retained encoding.
2. Acquire aligned NAIP aerial imagery with service/catalog snapshots and hashes. Preserve raw inputs; match macro-color and anchor new-side color to the accepted west_next/northwest_next edges.
3. Capture and seal the saved 2,048-terrain baseline in fresh isolated `D:/BooterBigArmValidation/TerrainFarWestPair20261008-01`. Verify all four existing terrain roots and pinned package/build files.
4. Prepare only new-side border constraints at easting 508112 within the established precision budget. Build, save, reopen and inspect a Blender junction pilot, both complete 256-mesh sources and a 2,560-mesh combined review using Blender 5.2.2 LTS in background mode with auto-execution disabled.
5. Export verified reopened Blender heights and imagery; require 5,008 exact normalized joins. Create native Unity TerrainData, layers and reusable section prefabs in an isolated candidate outside enabled Build Settings.
6. Validate full native readback, retained hashes/GUIDs, exact seams, reciprocal neighbors and 64,000 direct collider samples. Run relevant offline and focused EditMode checks, preserve old scene documents except two appended terrain-parent children, transfer only verified additions and refresh coverage/map data.
7. Verify production import/readback and metadata, document evidence, and commit only task-owned files on main. No push, package changes, gameplay smoke tests, window focus or procedural systems are authorized by this batch.

The production Editor is closed at preflight; use hidden batch validation rather than launching a foreground Editor. Unrelated BigARM material and QualitySettings edits remain separately owned. Before every production transfer, compare the saved scene and all protected hashes with the baseline so concurrent changes cannot be overwritten.

All stages are complete. See [the implementation receipt](./FAR_WEST_PAIR_IMPLEMENTATION_2026-10-08.md) for source, Blender, scene preservation, transfer and production validation evidence. Final checks passed 81 Unity tests, 57 offline tests, 5,008 exact joins and 64,000 direct collider samples. Production import/validation and map rendering used hidden batch processes and left the Editor closed.

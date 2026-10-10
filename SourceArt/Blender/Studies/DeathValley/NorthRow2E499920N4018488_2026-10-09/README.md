# Northern row section 1: editable geographic sources

Batch: `NorthRow2E499920N4018488_2026-10-09`. Bounds: `[499920, 4018488, 504016, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has one retained south border. The combined review covers exactly 4864 occupied tiles with the remaining upper-east sections absent.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 4 | 2 m | `b43da081bebd5218f587f080a1bdcb886ea62b051051e8b2fcb9837539b9c1c8` |
| `north_row2_e499920_n4018488_01.blend` | 256 | 2 m | `b38fdba3c6a92bdd0521bb4a1ce8993a08d53c5ad5ee47e64186033fa26424dc` |
| `combined01.blend` | 4864 | 4 m | `5c538ecd5a3c6d6d757a8b8b9e1bdee3c91389affc95dc46278ca61a53dc4dc8` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP01_AUTHORING_2026-10-09.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E499920N4018488_2026-10-09/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E499920N4018488_2026-10-09/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

# Northern row section 5: editable geographic sources

Batch: `NorthRow2E516304N4018488_2026-10-10`. Bounds: `[516304, 4018488, 520400, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has retained south and west borders plus their corner. The combined review covers exactly 5888 occupied tiles with the remaining upper-east sections absent.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 8 | 2 m | `6f1a1abae74abbf58b6d1528a72a448aa848bae5b86c1bb8f03500c12de0e651` |
| `north_row2_e516304_n4018488_01.blend` | 256 | 2 m | `6df38b2037c8424818568bfda0e6c03fde8d4299e16c5670bfdf8a1f3c380b84` |
| `combined01.blend` | 5888 | 4 m | `dbf666bbbe7551edca7ec1be1d4238e9e8b914689556ecf917b2531cf0edd055` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP05_AUTHORING_2026-10-10.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E516304N4018488_2026-10-10/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E516304N4018488_2026-10-10/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

# Northern row section 3: editable geographic sources

Batch: `NorthRow2E508112N4018488_2026-10-09`. Bounds: `[508112, 4018488, 512208, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has retained south and west borders plus their corner. The combined review covers exactly 5376 occupied tiles with the remaining upper-east sections absent.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 8 | 2 m | `4ed1690f40498d70a1ea4fd0801db0b9969bad8be8fa1248cc76a8557cbd196c` |
| `north_row2_e508112_n4018488_01.blend` | 256 | 2 m | `96ad5fa75d3c1427a22c57aace4539df9815f863fbc28d6ef021560f5c227c52` |
| `combined01.blend` | 5376 | 4 m | `f0852236941323be398ce682673c45315f3605861332ba53b4d54c624af56de5` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP03_AUTHORING_2026-10-09.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E508112N4018488_2026-10-09/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E508112N4018488_2026-10-09/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

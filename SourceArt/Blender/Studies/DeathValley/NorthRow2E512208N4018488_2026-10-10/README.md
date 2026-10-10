# Northern row section 4: editable geographic sources

Batch: `NorthRow2E512208N4018488_2026-10-10`. Bounds: `[512208, 4018488, 516304, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has retained south and west borders plus their corner. The combined review covers exactly 5632 occupied tiles with the remaining upper-east sections absent.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 8 | 2 m | `27a31ea6352fd4bc44520c6ffa471f926152eadd6f50ffbae6b4ea3a55240609` |
| `north_row2_e512208_n4018488_01.blend` | 256 | 2 m | `5a50e74152243a1b87674bf04a8a3d5be12fa9e918887b09f73128e771278085` |
| `combined01.blend` | 5632 | 4 m | `c259ee80fca05a7debecea8ccec157d33f7b1c338a8f35efdd8d98ed7fcd64f4` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP04_AUTHORING_2026-10-10.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E512208N4018488_2026-10-10/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E512208N4018488_2026-10-10/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

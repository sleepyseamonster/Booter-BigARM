# Northern row section 6: editable geographic sources

Batch: `NorthRow2E520400N4018488_2026-10-10`. Bounds: `[520400, 4018488, 524496, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has retained south and west borders plus their corner. The combined review covers exactly 6144 occupied tiles in the complete 96-by-64 rectangle.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 8 | 2 m | `c423ccbbf679b67f03c83365e84f0472a907d8332abc5a370013aee018ff57cd` |
| `north_row2_e520400_n4018488_01.blend` | 256 | 2 m | `5aac31bd708f9d358891fbc4681d21e856e3993318cd09affc51ee54c9f43997` |
| `combined01.blend` | 6144 | 4 m | `43102f83f362affb48723731154063625234e1c564a175305e7791cc0dea3169` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP06_AUTHORING_2026-10-10.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E520400N4018488_2026-10-10/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E520400N4018488_2026-10-10/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

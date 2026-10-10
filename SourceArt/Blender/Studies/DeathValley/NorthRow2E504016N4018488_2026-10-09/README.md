# Northern row section 2: editable geographic sources

Batch: `NorthRow2E504016N4018488_2026-10-09`. Bounds: `[504016, 4018488, 508112, 4022584]` in EPSG:26911. Fixed projected origin: `[522448,4008248]`; metre units and elevation are preserved. These are native Blender 5.2.2 LTS sources. Terrain uses 2 m interior authoring controls with full 1 m borders; the combined review uses 4 m interiors with full borders.

All three files were saved and independently reopened. Mesh/source correspondence, positions, transforms, units, geographic identities, upward normals, packed imagery and exact shared borders passed. Five views were inspected: pilot junction, section overhead/oblique and combined overhead/oblique. The pilot has retained south and west borders plus their corner. The combined review covers exactly 5120 occupied tiles with the remaining upper-east sections absent.

| File | Meshes | Interior spacing | SHA-256 |
| --- | ---: | ---: | --- |
| `Pilot01.blend` | 8 | 2 m | `060d629b7db93a0925d2e88cc039d8867942d29d1b77f0ad1ccd52d6260efe04` |
| `north_row2_e504016_n4018488_01.blend` | 256 | 2 m | `da3b64140fb81814aec8ac71889b460019d4ab6d1064a4e50625e2f62eb53725` |
| `combined01.blend` | 5120 | 4 m | `67742bfbbc58bdd884fa2bcfb13ce397788472027adbb23bbac3880065193bc0` |

The [authoring checkpoint](../../../../../Docs/Evidence/DeathValley/NORTH_ROW2_STEP02_AUTHORING_2026-10-09.json) records preparation, hashes and inspected views. Numeric grids, imagery and reopen exports are local under `SourceData/Terrain/DeathValley/NorthRow2E504016N4018488_2026-10-09/Prepared01/`. Runtime owner: `Assets/_Project/Art/Terrain/NorthRow2E504016N4018488_2026-10-09/`. Native integration acceptance and its saved-versus-live limits are controlled by the [execution plan](../../../../../Docs/DeathValley/NORTH_ROW2_EXECUTION_PLAN_2026-10-09.md), rather than inferred from this source review.

The `.blend` files and terrain source data remain local-only. Do not stage, publish, replace or retire them automatically. Existing retired historical sources must not be restored at their old paths. Aerial imagery is geographic reference; close-range material and hands-on gameplay acceptance remain separate. No terrain generation, streaming or runtime-delta schema change is introduced by these fixed terrain sources.

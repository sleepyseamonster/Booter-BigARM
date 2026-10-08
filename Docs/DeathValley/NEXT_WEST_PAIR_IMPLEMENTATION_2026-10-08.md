# Next western terrain pair implementation

Greater Wasteland now includes the next west and northwest slices beyond the previously accepted six sections. The complete authored terrain is a 64 × 32 grid of 2,048 native Unity terrains, covering 16.384 × 8.192 km. Both additions followed verified GIS elevation/imagery → native Blender construction and reopening → guarded export → isolated native Unity creation and validation → production transfer and live readback.

| Section | EPSG:26911 bounds in metres | New chunks |
| --- | --- | --- |
| `west_next` | `[508112, 4006200, 512208, 4010296]` | 256 |
| `northwest_next` | `[508112, 4010296, 512208, 4014392]` | 256 |
| Entire production footprint | `[508112, 4006200, 524496, 4014392]` | 2,048 total |

```text
Next northwest | Northwest outer | Northwest | North
Next west      | West outer      | West      | Original
```

The origin remains `[522448, 4008248]`; geographic identities retain `epsg26911/e{east}/n{north}/size256`. All terrain stays fixed, authored and always loaded. Generation, streaming/unload/reload, generated-object identity and persisted runtime deltas are outside this batch. Existing authored gameplay objects, controls and build routing remain accounted for through protected assets and scene-document preservation.

## Sources and Blender proof

Accepted acquisition, saved baseline and preparation are under `SourceData/Terrain/DeathValley/NextWestPair2026-10-08/Acquisition01/`, `Baseline01/` and `Prepared01/`. The four official USGS `CA_FEMAR9Southeast_D24` bare-earth 1 m products are `x50y401`, `x50y402`, `x51y401` and `x51y402`, using EPSG:26911 and NAVD88 elevations in metres. Catalog, bounded source windows, XML metadata, ownership rules and hashes are retained. Full finite 4,097 × 4,097 section grids pass strict 2 m decimation; heights range from −84.648949 to 136.224686 m and remain inside the existing −100 to 1,700 m encoding range.

Source ownership is explicit at easting 510000 and northing 4010000. Product overlap differences are recorded, with maximum 0.220205 m; raw source data is not silently averaged. Two 2 m RGB aerial sections were acquired from the established USGS NAIP service with service/catalog responses and hashes. Color follows the existing macro-color match and new-side feathering, with exact retained RGB edge anchors. Imagery remains geographic reference rather than final close-range game material art.

The saved 1,536-terrain baseline has 2,992 exact existing joins and 17,489 protected file hashes. New geometry alone adopts the retained east-border readback at easting 512208. Maximum preparation adjustment is 0.027458191 m, within the derived 0.042057667 m precision budget. Immutable measured sources remain separate from these authored border constraints.

Editable Blender sources are in `SourceArt/Blender/Studies/DeathValley/NextWestPair2026-10-08/`: `west_next01.blend`, `northwest_next01.blend`, `combined01.blend`, and the verified junction pilot `Pilot01.blend`. Blender 5.2.2 LTS built and reopened both 256-mesh sources and the full 2,048-mesh combined review. Individual sources use 2 m interiors and complete 1 m borders; the combined review uses 4 m interiors with full borders. Mesh sample error and shared edge error are zero, normals point upward, units are metres, transforms/origin pass and imagery is packed. Pilot, overhead and oblique renders were inspected. Reopened mesh proofs and their height exports are sealed beside the prepared manifest.

The strict batch tools are `acquire_next_west_pair.py`, `prepare_next_west_pair.py`, `build_next_west_pair_scene.py` and `export_next_west_pair_unity.py` in `Tools/Art/Blender/death_valley/`; the native launcher remains `run_expansion_blender.py`. All accepted source, export, validation and temporary build paths are on D:. Build subprocess `TEMP`/`TMP` points to `D:/BooterBigArmTools/Temp`. The initial C: session-storage failure cleared after free space returned; this task did not delete the protected rollback checkout.

## Unity integration and verification

The isolated validation project is `D:/BooterBigArmValidation/TerrainNextWestPair20261008-01`. `BadwaterNextWestPair` source/baseline/builder/validator classes retain strict eight-section ownership and reject missing, duplicate, escaped or changed protected inputs. The candidate scene is `Assets/_Project/Scenes/Reference/TerrainNextWestPairCandidate.unity`, outside enabled Build Settings. New TerrainData, TerrainLayers, source exports, textures and two reusable section prefabs live under `Assets/_Project/Art/Terrain/NextWestPair2026-10-08/`.

Candidate, preserved isolated production and live production readbacks all pass:

- 2,048 terrains, including 1,536 exact retained full grids and 512 additions.
- 4,000 exact seams, reciprocal neighbors, 192 null exterior neighbor slots and one connectivity owner.
- 51,200 direct TerrainCollider samples; maximum collider error 0.000030518 m.
- Maximum new native height readback error 0.027492642 m, inside the established precision budget.
- Existing TerrainData identities, metadata and protected file bytes preserved.

Scene preservation keeps 8,231 prior native YAML documents exact; the sole changed original document is terrain parent `1259644692` with two appended children. There are 2,564 new native documents. The original scene GUID and sole enabled production build entry remain unchanged. Transfer checked 8,216 new asset files byte-for-byte, plus owner metadata and the reference scene. Detailed transfer evidence is under `D:/Arc & Dust/Transfers/NextWestPair20261008/`.

Final focused EditMode checks passed 66/66 against the integrated scene and refreshed eight-section map data. Offline checks passed 53/53. Coverage verified all 2,048 source records and 4,000 normalized joins; terrain doctor checked 4,111 records with zero issues. Atlas tile selection now uses unique geographic keys and its viewport fits the full terrain union with 512 m padding. Unity map overview, oblique and top previews were inspected without opening or focusing a window.

Production scene SHA-256 is `66902ca300b62260b81347622e0f8a2e4fc6f3eedb1914c9370be7889d921966`; export manifest SHA-256 is `c04881f28b0f7126f0fa706c20cbaa46fb12af3d3477bba5b01515206879325f`. Current receipts, test XML and images are in `Docs/Evidence/DeathValley/NextWestPair2026-10-08/`. The live production scene is loaded, clean and outside Play Mode.

The live Editor reported 2,501,744,360 allocated bytes and 3,231,936,512 reserved bytes before validation, compared with a separate 1,536-terrain reading of 2,014,534,867 and 2,865,991,680 bytes. These are authoring-state readings, not a controlled Player benchmark. The additions increase area and terrain count by one third. Player performance and hands-on traversal remain user-owned; no gameplay smoke test was run.

Fresh compilation and terrain validation pass. Live console history retains two missing-metadata messages from immutable URP package test files and two bridge five-second response timeouts during long import/validation operations; those operations completed and produced their verified outputs. Package versions and caches were not patched, and user console history was preserved. Separately owned BigARM material and QualitySettings edits remain outside the terrain batch. No push or external publication is authorized by this implementation.

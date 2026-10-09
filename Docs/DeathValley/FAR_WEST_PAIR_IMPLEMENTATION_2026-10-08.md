# Further western terrain pair implementation

Greater Wasteland now includes two more Blender-authored sections west of the accepted eight-section footprint. Production has 2,560 native Unity terrains in an 80 × 32 grid, covering 20.480 × 8.192 km. The origin remains `[522448,4008248]` in EPSG:26911.

| Section | Projected bounds in metres | Additions |
| --- | --- | --- |
| `west_far` | `[504016,4006200,508112,4010296]` | 256 |
| `northwest_far` | `[504016,4010296,508112,4014392]` | 256 |
| Full footprint | `[504016,4006200,524496,4014392]` | 2,560 total |

The route remains verified GIS elevation/imagery → native Blender construction and reopening → guarded heights/imagery export → isolated native Unity creation and integration → verified production transfer. Terrain remains authored and always loaded. Generation, streaming/unload/reload, generated-object identity and persisted runtime deltas remain deferred. Existing geographic identities and authored gameplay constraints are preserved.

## Inputs and editable sources

Immutable source acquisition, saved baseline and preparation are under `SourceData/Terrain/DeathValley/FarWestPair2026-10-08/Acquisition01/`, `Baseline01/` and `Prepared01/`. Fresh bounded windows and XML metadata from official USGS `x50y401` and `x50y402` products establish complete 1 m bare-earth coverage, EPSG:26911 and NAVD88 metre elevations. Both 4,097 × 4,097 section grids are finite and their 2 m controls match strict native decimation. Prepared source heights range from 2.788204 to 639.239380 m, inside the existing −100 to 1,700 m encoding range. Recorded ownership at northing 4010000 avoids silently averaging overlapping products.

Two aligned 2 m NAIP RGB sections have retained service/catalog responses and hashes. Geographic color follows the existing macro-color source, with feathering confined to the additions and exact color anchors to `west_next`/`northwest_next`. Imagery is reference color, not final close-range game material art.

The saved native baseline seals all 2,048 retained full grids, 4,000 exact retained joins and 25,707 protected file hashes across four existing terrain roots and pinned package/build files. New-side height constraints join at easting 508112; maximum adjustment is 0.027481079 m, below the established 0.042057667 m budget. Raw measurements remain separate and immutable.

Editable sources are `west_far01.blend`, `northwest_far01.blend`, `combined01.blend` and `Pilot01.blend` under `SourceArt/Blender/Studies/DeathValley/FarWestPair2026-10-08/`. Blender 5.2.2 LTS built, saved and reopened the pilot, both 256-mesh sections and the complete 2,560-mesh review. Individual interiors use 2 m controls and complete 1 m borders; combined interiors use 4 m spacing with full borders. Mesh/source and edge errors are zero, normals point upward, units/origin/transforms pass and imagery is packed. Junction, overhead and oblique views were inspected.

New strict adapters are `acquire_far_west_pair.py`, `prepare_far_west_pair.py`, `build_far_west_pair_scene.py` and `export_far_west_pair_unity.py` in `Tools/Art/Blender/death_valley/`. They preserve all earlier accepted sources and use fresh destinations. All task artifacts are on D:; subprocess temporary files use `D:/BooterBigArmTools/Temp`.

## Native Unity handoff

The isolated project is `D:/BooterBigArmValidation/TerrainFarWestPair20261008-01`. New runtime data, textures, TerrainLayers, source exports and two reusable section prefabs are under `Assets/_Project/Art/Terrain/FarWestPair2026-10-08/`. Candidate `Assets/_Project/Scenes/Reference/TerrainFarWestPairCandidate.unity` remains outside enabled Build Settings. The `BadwaterFarWestPair` source/baseline/builder/validator classes enforce the strict ten-section contract and exact protected bytes rather than relaxing earlier validators.

Candidate, preserved isolated production and the production checkout pass 2,560 terrains, 2,048 exact retained grids/GUIDs, 5,008 exact seams, reciprocal neighbors, 224 exterior null neighbor slots, one connectivity owner and 64,000 direct TerrainCollider samples. Maximum native height error is 0.027492642 m; collider error is 0.000030518 m, both inside the established precision gates.

All 10,795 unaffected prior scene documents remain byte-exact. The sole changed original document is terrain parent `1259644692`, with two appended children; 2,564 new native documents hold the additions. The scene GUID and enabled build routing remain unchanged. Transfer verified 9,240 new asset files byte-for-byte, plus owner metadata and the candidate scene. Detailed transfer evidence is in `D:/Arc & Dust/Transfers/FarWestPair20261008/`.

Final focused EditMode tests passed 81/81 against the integrated scene and refreshed map data; offline checks passed 57/57. Terrain doctor checked 5,135 records with zero issues. Current production readback, preservation receipts, test XML and images are recorded under `Docs/Evidence/DeathValley/FarWestPair2026-10-08/`. Coverage and the Unity developer map include the full ten-section footprint, preserving geographic-key selection and the complete atlas viewport. Result counts and metadata checks are sealed in `completion_receipt.json`.

Production scene SHA-256 is `d1bfba16e85ba87c80cdcb68a3f864db1996dbd68573770a5d6cb20edac10516`; export manifest SHA-256 is `8557e66f72230c29ebbe7db14028ad027928a0b5cfb3ba0376d18893b31fa488`.

The Editor was closed at preflight. Validation and preview rendering used hidden batch processes and left it closed, preserving the user's foreground app. No gameplay smoke test or Player FPS benchmark was run. The additions increase terrain count and area by 25%; hands-on traversal and Player performance remain user-owned. Package versions, inactive archives, actor/input setup and separately owned BigARM material/QualitySettings edits were not changed by this terrain batch. No push or external publication is authorized.

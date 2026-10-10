# Third northern row — October 10, 2026

The user authorized all six next northern sections. Execute west to east across EPSG:26911 bounds `[499920,4022584,524496,4026680]`. Each section is 4096 m square, containing sixteen 1024 m sectors and 256 terrain tiles of 256 m. This is a new third-row owner; previous row contracts and sealed proofs remain unchanged.

## Accepted predecessor

Baseline commit `3917fbfb3bc23765d6892cc3ebd00a1f76407a73` has 6144 tiles, 12128 reciprocal joins, 320 exterior slots, 384 sectors and 24 sections. Saved production bounds are `[499920,4006200,524496,4022584]`, Unity origin `[522448,4008248]`, and height encoding is minimum -100 m plus 1800 m range. The saved scene and latest second-row source manifest match their native validation proof. The live root Editor is Unity 6000.4.0f1 on port 7800 with the clean dressing study active and production unloaded.

## Geographic and runtime contract

Stable identity remains `epsg26911/e{east}/n{north}/size256`. The approved fixed terrain remains always loaded; generation and streaming are deferred. This extension changes neither generated-object identity nor persisted runtime-delta formats. Authored constraints are the measured DEM/NAIP inputs, exact accepted south and west anchors, and unchanged new-side adjustment and native validation tolerances. Existing actors, camera, input, UI, build scene and scene GUID must be preserved.

| Step | Easting | South anchor | Total tiles | Joins | Exterior slots |
| --- | ---: | --- | ---: | ---: | ---: |
| 1 | 499920 | north_row2_e499920_n4018488 | 6400 | 12624 | 352 |
| 2 | 504016 | north_row2_e504016_n4018488 | 6656 | 13136 | 352 |
| 3 | 508112 | north_row2_e508112_n4018488 | 6912 | 13648 | 352 |
| 4 | 512208 | north_row2_e512208_n4018488 | 7168 | 14160 | 352 |
| 5 | 516304 | north_row2_e516304_n4018488 | 7424 | 14672 | 352 |
| 6 | 520400 | north_row2_e520400_n4018488 | 7680 | 15184 | 352 |

Step 1 has only a south anchor. Each later west anchor is the immediately preceding accepted third-row section. Final bounds are `[499920,4006200,524496,4026680]`, 96 by 80 tiles, 30 sections, 480 sectors and 192000 collider samples.

## Ordered acceptance gates

1. Acquire and reopen all six measured 1 m DEM grids and aligned 2 m NAIP sections. Audit all finite samples, strict 2 m decimation, source hashes, product overlap and half-open ownership. Four products `x49y403` through `x52y403` cover this strip, including the 80 m western sliver. Elevations outside -100..1700 m require an explicit author decision before height encoding or production changes.
2. Capture a fresh protected predecessor inventory and reuse all 6144 immutable native references. Supplement retained authoring banks with the actual quantized sixth second-row section. Measure storage before creating a new isolated validation project; preserve all existing projects and source files.
3. Add distinct third-row source, builder, validator, export and coverage adapters. Parse each distinct source owner once in scene detection to avoid the previous repeated per-tile JSON parsing cost. Preserve historical contracts and all tolerance gates.
4. For each section: prepare against accepted anchors; build, save, reopen and inspect pilot, section and combined Blender stages; inspect all five required views before export.
5. Export, import and validate candidate in isolation; preserve every preceding scene document except the shared parent's appended child; require exactly 1314 new documents per section. Validate full native readback, reciprocal joins, direct collider samples, canonical playable setup, focused contract/map checks, metadata and refreshed coverage. Inspect three developer-map views. Recheck live root state and predecessor bytes immediately before transfer.
6. Commit each verified task-owned acceptance batch on main. Final audit covers all six sections, source lineage, immutable predecessor payloads, current coverage/doctor, package/build pins, storage and retired-source absence.

All app work remains background-only. Gameplay smoke tests remain user-owned. SourceData/Terrain and native Blender binaries stay local-only. No source cleanup or external publication is authorized.

## Startup state

The geographic contract and five focused positive/negative checks pass. The [baseline evidence](../Evidence/DeathValley/NORTH_ROW3_BASELINE_2026-10-10.json) verifies all 6144 retained geographic cells and all 18432 TerrainData, metadata and immutable native-reference hashes. The [whole-strip source audit](../Evidence/DeathValley/NORTH_ROW3_SOURCE_AUDIT_2026-10-10.json) reopened all 100691969 unique native samples and six aligned imagery sections, with strict decimation and exact measured section joins. Elevations range from -83.6060943604 to 1158.3819580078 m, within the unchanged encoding.

DEM inputs are in local `SourceData/Terrain/DeathValley/NorthRow3Survey2026-10-10/Acquisition01/DEM`; accepted imagery is in separately versioned `ImageryAcquisition02`, after transient HTTP 504 responses. The initial incomplete imagery attempt is preserved. SourceAudit01 seals both manifests and exact inputs.

The retained broad 40 m colour palette ends at N4024200, partway into this strip. Third-row preparation holds the northernmost covered palette row beyond that extent for low-frequency colour calibration, recording the original reference hash and exact padding. Detailed NAIP and geometry remain independently measured over the entire strip. This prevents unfilled palette pixels from creating a dark band and requires native visual review before export.

The fresh protected predecessor inventory passed with 124305 files, and the sixth accepted second-row native model was added without changing earlier inputs. Western-section preparation passed with maximum new-side adjustment 0.0274658203 m within the unchanged 0.0420576669 m budget, exact height/RGB joins and no west anchor or corner exception. The four-mesh pilot and 256-mesh section saved/reopened with zero sample/border error; their junction and overhead/oblique views were inspected. The 6400-mesh combined review also saved/reopened with zero sample/border error, positive normals and packed imagery. All five required views were inspected, including the correct western protrusion and unbuilt upper-east strip. The [first authoring checkpoint](../Evidence/DeathValley/NORTH_ROW3_STEP01_AUTHORING_2026-10-10.json) seals all three sources (609964745 bytes) and their reopened proofs. Guarded export is in progress; new native Unity terrain and production acceptance remain pending. Six pure Unity contract EditMode cases passed in a new physical isolated project at `D:/BooterBigArmValidation/TerrainNorthRow3_20261010_01`. The [startup receipt](../Evidence/DeathValley/NORTH_ROW3_STARTUP_2026-10-10.json) records these bounded gates. No third-row production terrain has been imported or accepted yet. D: began with approximately 88.5 GiB free; the [startup capacity gate](../Evidence/DeathValley/NORTH_ROW3_STARTUP_CAPACITY_2026-10-10.json) passed, and the physical project/cache copy completed with approximately 68.3 GiB free. Continue measuring before expensive outputs.


Western-section Unity candidate readback passed: 6400 terrains, 12624 joins, 352 exterior slots, 160000 collision samples and 400 sectors. All 6144 retained full native grids, GUIDs and payloads matched exactly. New-side native error was 0.0274926424 m and sampled collider error 0.0000610352 m within unchanged limits. All34row/map EditMode cases passed. Tested editor tooling is committed as `1d7278b79`.

The section was integrated into the isolated saved production scene, then preservation restored32077old documents exactly, changed only the common parent's appended child and kept1314new documents. Independent saved-production validation is in progress. Root production remains at6144 until native validation, canonical setup and fresh live-state guards pass.


Section1 is accepted. Its [closeout](../Evidence/DeathValley/NorthRow3E499920N4022584_2026-10-10/closeout.json) seals native saved-production validation, canonical setup, unique metadata for1554assets/folders, current6400-file coverage with12624exactjoins, all34current-catalog tests and three inspected developer-map views. Root transfer preserved sceneGUID and32077old documents, appending one parent child and1314new documents. Saved sceneSHA is `1114a8944d78825c7e43f7d2982013950c917035dbeaaa839e55574ebbd12125`.

The catalog test inventory was extended by the six exact third-row counts and matching4026680north bound; both failed pre-correction reports remain preserved, and the full corrected34-case run passed. The metadata refresh completed despite the bridge's earlier5-second deadline; current console diagnostics identify only two existing immutable URP test-file messages and that bridge timeout, with no compilation or terrain errors. No console or package files were cleared or changed. Future bridge calls pass the longer command timeout positionally.

Five sections remain. Section2 must capture this6400-tile accepted native predecessor and use section1's actual quantized west anchor plus its accepted second-row south anchor.


Section2 authoring passed with a fresh6400-tile predecessor and127396protected files. Maximum new-side adjustment0.0274734497m is within the unchanged0.0420576669m budget; height/RGB joins are exact, and the measured southwest RGB difference is[0,0,0] with no corner exception. The eight-mesh pilot,256-mesh section and6656-mesh combined review saved/reopened with exact samples/borders, positive normals and packed images. All five views were inspected. The [section2 authoring checkpoint](../Evidence/DeathValley/NORTH_ROW3_STEP02_AUTHORING_2026-10-10.json) seals the local-only sources. New native Unity acceptance is pending.


Section2 is accepted. Its [closeout](../Evidence/DeathValley/NorthRow3E504016N4022584_2026-10-10/closeout.json) seals6656native terrains,13136exactjoins,352exterior slots,166400collision samples,416sectors, canonical setup,1554unique metadata assets/folders, complete current coverage and34passing current-catalog tests. New native error0.0274926424m and collider error0.0000610352m remain within unchanged limits. Transfer preserved33391old documents, appending one common-parent child and1314new documents; saved sceneSHA `080994b036202e9eb2cdf6e84f7060bc4ffe059ad07c697e163590dcdf03695e`.

The user's unsaved dressing study was not saved, switched or altered: all loaded scene-object/component serialized fingerprints matched before/after transfer. No loaded task-owned terrain asset was dirty. Later root Editor queries encountered stale compilation telemetry after metadata import; the user session was preserved, and the three map views were rendered in the hidden physical validation Editor instead, using the exact updated catalog. All three were inspected. This does not claim later root-main-thread readiness. Four sections remain.


Section3 authoring passed against a fresh6656-tile predecessor and130487protected files. Maximum new-side adjustment0.0274658203m is within the unchanged0.0420576669m budget, height/RGB joins are exact, and southwestRGB[0,0,0] needs no exception. The eight-mesh pilot,256-mesh basin-floor section and6912-mesh combined review saved/reopened with zero sample/border error, positive normals and packed images. All five required views were inspected; the three-section upper strip preserves the unbuilt eastern half. The [section3 authoring checkpoint](../Evidence/DeathValley/NORTH_ROW3_STEP03_AUTHORING_2026-10-10.json) seals the local-only sources. Unity acceptance is pending.

Section3 candidate validation passed for 6912 native terrains, 13648 joins, 352 exterior slots, 172800 collider samples and 432 sectors. All 6656 retained native grids and owned payloads matched their accepted references. Maximum new native error was 0.0274674967 m; maximum sampled collider error was 0.0000610352 m. All 34 terrain-contract and map tests passed. Isolated production integration preserved 34705 existing documents exactly, changed only the common parent's appended child, and added 1314 documents. Independent saved-production validation remains in progress; root production still contains the two accepted sections.

The user reloaded the root Editor, restoring fresh main-thread bridge responses. A fresh read-only guard verified production unloaded, no compilation errors, no dirty task-owned terrain assets and Editor outside Play Mode. Unrelated study edits and concurrent basalt-study commits remain user-owned and are preserved.


Section3 is accepted. Its [closeout](../Evidence/DeathValley/NorthRow3E508112N4022584_2026-10-10/closeout.json) seals 6912 native terrains, 13648 exact joins, 352 exterior slots, 172800 collider samples and 432 sectors. Both 34-case test runs passed, all 1554 new asset/folder metadata entries passed global GUID and native payload checks, and all three refreshed developer-map views were inspected. Coverage verifies all 6912 height and colour files with zero hash mismatches and zero encoded edge differences. The saved scene SHA is `2e7e30186f7d3f9eead09329a7f49b394dee4ccee15f58a7aa00cae1e2ce4019`. Transfer preserved 34705 existing documents and the exact serialized state of the unsaved study. Three sections remain; section4 must capture this accepted native predecessor and its quantized west anchor before authoring.


Section4 authoring passed against the committed 6912-tile predecessor and 133578 protected files. New-side adjustment 0.0274581909 m stays within the unchanged 0.0420576669 m budget; height and RGB joins are exact with no corner exception. The eight-mesh pilot, 256-mesh section and 7168-mesh combined world saved/reopened with zero sample/border error and positive normals. All five views were inspected, preserving basin-floor channels, alluvial fans, eastern ridges and the unbuilt two-section upper-east footprint. The [section4 authoring checkpoint](../Evidence/DeathValley/NORTH_ROW3_STEP04_AUTHORING_2026-10-10.json) seals three local-only Blender sources totalling 672019545 bytes. Native Unity acceptance remains pending.

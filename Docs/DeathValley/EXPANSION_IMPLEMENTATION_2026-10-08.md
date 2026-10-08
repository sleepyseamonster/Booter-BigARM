# Greater Wasteland expansion implementation

Greater Wasteland now contains 1,024 native Unity Terrain chunks: the retained southeast section and new west, north and northwest sections, each with 256 chunks. Production and developer-map coverage use the 8,192 m square `[516304,4006200,524496,4014392]` in EPSG:26911. The original Unity origin `[522448,4008248]` remains unchanged.

The [audited plan](./PLAN_WEST_NORTH_NORTHWEST_2026-10-07.md) and [handoff](./HANDOFF_WEST_NORTH_NORTHWEST_2026-10-07.md) control the footprint and Blender-first pipeline. This is implementation, source, native terrain and packaging proof. Appearance, actor traversal and representative performance remain user acceptance.

## Sources and authoring

Accepted acquisition and preparation live under `SourceData/Terrain/DeathValley/WestNorthNorthwest2026-10-07/Acquisition01` and `Prepared03`. Four official bare-earth USGS 1 m products use NAVD88 metres. Each new section retains finite 4097 × 4097 native samples and strictly decimated 2049 × 2049 control samples. New native terrains use 2 m controls and complete 1 m render borders. Imagery is geographic NAIP resampled to 2 m; band four is NIR. New-only color matching is recorded separately from immutable imagery.

Accepted Blender files are `west01.blend`, `north01.blend`, `northwest01.blend`, `CombinedReview01.blend` and `Pilot04.blend` under `SourceArt/Blender/Studies/DeathValley/WestNorthNorthwest2026-10-07`. Native save/reopen checks prove counts, geographic placement, samples, normals, packed resources and exact height seams. The combined review uses 4 m interiors with full 1 m borders; individual sections use 2 m interiors. Unity exports derive from reopened meshes.

The [source receipt](../Evidence/DeathValley/WestNorthNorthwest2026-10-07/source_authoring_receipt.json) hashes accepted artifacts and accounts for earlier local candidates. The exact authoring script matching the original build receipt is archived in `Prepared03/build_expansion_scene_at_authoring.py`; the active script subsequently gained render framing based on mesh bounds. Archived C: paths remain provenance; the exporter verifies corresponding resources in the current D: checkout.

## Production preservation and verification

The production scene GUID remains `4de5dd018ee194314a00fd369e2d3eeb`. Existing 256 TerrainData payloads, metadata, placements, heights and resources are unchanged. Of 1,822 original scene documents, 1,821 are byte-identical. Only the terrain parent transform adds three section children. The 3,846 new documents contain 768 terrain objects and three section parents. Save-time ambient and camera changes from existing edit-mode scripts were removed through guarded serialization preservation.

The full native grid passes 1,984 exact shared height edges, reciprocal neighbor ownership, 128 null outer edges and 25,600 collider samples. Maximum new native readback error is 0.027492643 m within the measured encoding budget; maximum sampled collider error is 0.000030518 m. Retained full readback is exact. All 6,034 active metadata GUIDs are unique.

Current protection checks cover 2,066 files. The two package hashes were explicitly reconciled to separately committed Unity CLI bridge work `0941ee8`, with original hashes retained in the source manifest. Terrain protection was not relaxed. The scene remains the only enabled production build scene, with fixed authored terrain and one connectivity owner.

Geographic keys provide deterministic chunk identity; original GUIDs preserve asset identity. Streaming, unload/reload generation and generated-object persistence are not applicable to this fixed, always-loaded authored terrain. No generator, streaming service or runtime-delta save integration was added.

## Developer map and tests

The canonical catalog and cyan grid now describe all 1,024 built chunks. Playable Area framing uses the union while the `badwater` study region retains its original bounds. Geographic keys distinguish repeated local labels. The aerial view, pan/zoom/orbit and existing controls remain available; overview imagery retains its original resolution and provenance.

Fresh focused Unity EditMode tests passed 41/41 on `D:/BooterBigArmValidation/TerrainExpansionFinal20261008-01`. The selected methods cover expansion contracts, saved-scene reload, retained terrain, map coordinates/framing and build-output guards. No actor/input or Play-mode smoke test was run. Full offline discovery passed 46/46; historical 64-chunk proposal tests use the explicit retained-baseline fixture and reject areas that are already built.

Fresh native readback is in `D:/BooterBigArmValidation/expansion-final-readback-20261008.json`. Test XML/logs and build logs use the same dated prefix under `D:/BooterBigArmValidation`. Inspected [top-down map](../Evidence/DeathValley/WestNorthNorthwest2026-10-07/unity-chunks-top.png), [oblique map](../Evidence/DeathValley/WestNorthNorthwest2026-10-07/unity-chunks.png) and [Blender overview](../Evidence/DeathValley/WestNorthNorthwest2026-10-07/combined_oblique_framed.png) show the full footprint. Interface renders are retained alongside them.

`ScenePlayabilityAuditTests` still has 256-count gameplay-smoke assumptions. Its maintenance and actor/input execution belong to separately authorized gameplay validation; it was excluded here. The read-only `RepositoryPlayabilityAudit` routes terrain checks through the generalized canonical validators without a new count assumption.

## Reproduction commands

Use PowerShell from the canonical checkout. Every Blender proof/export output and build directory must be fresh. Batchmode targets an isolated snapshot, never the production project owned by the connected Editor.

```powershell
$root = 'D:\Arc & Dust\Project'
$python = Join-Path $root '.venv\Scripts\python.exe'
$blender = 'D:\BooterBigArmTools\Blender522\blender-5.2.2-windows-x64\blender.exe'
$prepared = Join-Path $root 'SourceData\Terrain\DeathValley\WestNorthNorthwest2026-10-07\Prepared03\manifest.json'
$source = Join-Path $root 'SourceArt\Blender\Studies\DeathValley\WestNorthNorthwest2026-10-07\west01.blend'
& $blender --background --disable-autoexec --python-exit-code 1 $source --python "$root\Tools\Art\Blender\death_valley\verify_game_slice_scene.py" -- --manifest $prepared --section west --output 'D:\BooterBigArmValidation\FreshBlenderProof\west.json'
& $python "$root\Tools\Art\Blender\death_valley\export_badwater_unity_source.py" --manifest $prepared --output 'D:\BooterBigArmValidation\FreshExpansionExport'
& $python "$root\Docs\DeathValley\audit_coverage.py"
& $python "$root\Docs\DeathValley\terrain_tools.py" doctor
& $python -m unittest discover -s "$root\Docs\DeathValley" -p 'test_*.py' -v
```

The canonical `build_game_slice_scene.py` and `verify_game_slice_scene.py` dispatch explicit `--section` requests to the expansion implementation; their retained single-section contracts remain intact. Fresh native build/reopen dispatch was exercised with a four-chunk pilot. The reusable hidden launcher is `run_expansion_blender.py` with `--mode build|verify|render`, explicit section, manifest, source, fresh output and log.

```powershell
$editor = Join-Path $env:LOCALAPPDATA 'Unity\Editors\6000.4.0f1\Editor\Unity.exe'
$snapshot = 'D:\BooterBigArmValidation\TerrainExpansionFinal20261008-01'
$filter = 'BooterBigArm.Tests.BadwaterTerrainExpansionTests;BooterBigArm.Tests.DeathValleyExpandedMapTests;BooterBigArm.Tests.DeathValleyMapTests;BooterBigArm.Tests.BadwaterDevelopmentBuildTests;BooterBigArm.Tests.BadwaterTerrainSeamTests.CanonicalValidators_AcceptSavedSceneAndAllSourceDerivedVertices;BooterBigArm.Tests.BadwaterTerrainSeamTests.ContextGrid_PreservesEverySourceSampleAndInterpolatesNewVertices;BooterBigArm.Tests.BadwaterTerrainSeamTests.SharedBorders_CloseEdgesAndFourWayCornerWithoutChangingInteriors;BooterBigArm.Tests.BadwaterTerrainSeamTests.SavedScene_PreservesMeasuredDemSamplesAndTileBounds;BooterBigArm.Tests.BadwaterTerrainSeamTests.ReloadedScene_RestoresAllNeighborsAndExactBorders'
& $editor -projectPath $snapshot -batchmode -nographics -runTests -testPlatform EditMode -testFilter $filter -testResults 'D:\BooterBigArmValidation\FreshExpansionTests.xml' -logFile 'D:\BooterBigArmValidation\FreshExpansionTests.log'
& $editor -projectPath $snapshot -batchmode -nographics -quit -executeMethod BooterBigArm.Editor.BadwaterTerrainExpansionValidator.ValidateFromCli -terrainExpansionScene Assets/_Project/Scenes/Production/GreaterWasteland.unity -terrainExpansionReport 'D:\BooterBigArmValidation\FreshProductionProof.json' -logFile 'D:\BooterBigArmValidation\FreshProductionProof.log'
& $editor -projectPath $snapshot -batchmode -quit -executeMethod BooterBigArm.Editor.DeathValleyMapWindow.RenderFromCli -logFile 'D:\BooterBigArmValidation\FreshMapRender.log'
& $editor -projectPath $snapshot -batchmode -nographics -quit -executeMethod BooterBigArm.Editor.BadwaterDevelopmentBuild.BuildFromCli -buildOutput 'D:\Arc & Dust\Builds\FreshExpansion\ArcAndDust.exe' -logFile 'D:\BooterBigArmValidation\FreshExpansionBuild.log'
```

Launch processes hidden through `Start-Process -WindowStyle Hidden` for agent automation. `BuildCandidateFromCli` and `IntegrateSavedSceneFromCli` are restricted to isolated copies. Live integration uses `IntegrateVerifiedSections` through the connected Editor, followed by guarded preservation against the saved original scene and fresh readback. Asset transfers include original metadata and hashes; the detailed transfer receipt is `D:/Arc & Dust/Transfers/ExpansionIntegration20261008/transfer.json`.

## Development Player and user acceptance

The fresh Windows Development Player is `D:/Arc & Dust/Builds/GreaterWastelandExpansion20261008-01/ArcAndDust.exe`. Unity reported 517,474,701 bytes and 3 minutes 12.241 seconds. It was not launched. The [build receipt](../Evidence/DeathValley/WestNorthNorthwest2026-10-07/development_build_receipt.json) hashes all output files and verifies the task-owned scene, terrain, metadata and code against the packaged snapshot. Unity serialized isolated shader-prefilter/resource membership and connection metadata; production settings were not changed.

For user acceptance, move Booter and BigARM across the west–retained, north–retained, northwest–west and northwest–north interfaces. Check the shared junction at projected `[520400,4010296]`, Unity `[-2048,2048]`, and ground near all outer edges inside the footprint. Look for height jumps, gaps, imagery joins and grounding/following problems. Profile the full 1,024-terrain scene with its normal content and rendering settings. Packaging does not establish performance or actor traversal acceptance.

# Death Valley coverage and expansion

Death Valley is the user's current geographic expansion focus. In the coordinated scene-promotion task, the user named the existing playable Badwater terrain Greater Wasteland, selected it as the primary scene, and explicitly deferred procedural generation. This package records retained coverage, verifies rebuild inputs, and tracks wider Unity coverage. Use [the terrain slice workflow](./TERRAIN_SLICE_WORKFLOW.md) and each batch's strict source/builder/validation contract for new sections. It does not promote Blender studies to accepted game art or implement a new production World Creator.

## Current terrain coverage - October 9, 2026

Eastward northern-row section 1 is built in Blender and integrated into saved Greater Wasteland. Current production has 14 sections and 3,584 occupied chunks: the retained 96 by 32 rectangle plus a 32 by 16 northern cap. The AABB is `[499920,4006200,524496,4018488]`; 1,024 upper-east cells remain unbuilt. The fixed origin is unchanged. See [the latest implementation receipt](./NORTH_ROW_E504016_IMPLEMENTATION_2026-10-09.md) and [the ordered eastward plan](./NORTH_ROW_EASTWARD_PLAN_2026-10-09.md). The saved scene and transferred asset bytes pass native readback, collision and exact seam checks. Live Editor reload remains pending at its external-change dialog.

The refreshed atlas contains all 3,584 occupied chunks using unique geographic keys and actual footprint framing. Planned sections beyond step 1 are not represented as built terrain.

The original 256-chunk descriptions and pending-stage tables below retain the initial study's scope and historical sequencing. The current coverage catalog and newer implementation receipts control current production facts.

## Inspect the coverage

Open [coverage_atlas.html](./coverage_atlas.html) for the official park outline, regional/detail footprints and all 3,584 clickable Unity terrain chunks. Selection shows bounds, resolution, source availability and provenance. The blue grid identifies built terrain; unbuilt upper-east cells remain outside it. This north-up coverage diagram is distinct from a terrain render.

- [coverage_catalog.json](./coverage_catalog.json) records footprints, file hashes, source identities, geographic chunk keys, scene references and verification limits.
- [unity_tiles.csv](./unity_tiles.csv) supplies the tile inventory in a flat table.
- [regional_source_tiles.csv](./regional_source_tiles.csv) lists the 42 configured 32 km regional source footprints and expected builder names; detail cutouts may replace geometry, so individual Blender object presence remains unverified.
- [coverage_overview.svg](./coverage_overview.svg) and [coverage_badwater.svg](./coverage_badwater.svg) are portable static maps.
- [nps_deva_boundary.json](./nps_deva_boundary.json) retains the official NPS polygon response, request, date and response hash.
- [local_preflight.json](./local_preflight.json) records targeted Windows tool and external-data location checks.

Each large Badwater quarter is 2.048 × 2.048 km with 64 chunks. The total is 4.096 × 4.096 km with 256 chunks of 256 × 256 m. Rows run north to south and columns west to east. The four 1 m source chunks are `r12_c01`, `r12_c02`, `r13_c01`, and `r13_c02`; the other 252 retain 2 m source detail. All Unity rendering grids are 257 × 257 after the Windows seam repair. Interpolation does not add surveyed relief.

The expanded regional rectangle is 192 × 224 km and contains surrounding terrain beyond the park. Its 42 coarse source-tile footprints must not be mistaken for 42 detailed Unity regions. Blender footprints come from versioned configurations and retained build records. Materialized file presence and hashes do not establish fresh Blender scene readability.

## Ownership during repository organization

This task owns this new package, the narrow cross-platform change to `BadwaterDevelopmentBuild.cs`, and its new focused test/metadata pair. Existing agent packages, shared indexes, organization receipts, `.gitattributes`, archives and transferred Blender saves remain with their existing owners. No file move, deduplication, metadata regeneration, branch switch or package update is part of this task.

Gottspan coordinates coverage, integration and closeout. Blender owns terrain source inspection and custom geometry; Babineaux owns Unity import, scene wiring and validation. Gear Ball owns the scoped local commit. The user owns geographic and creative decisions, interactive traversal and visual acceptance. No additional agent was started for this package.

Initial organization and scene-promotion coordination is complete. The user ended further agent coordination for the subsequent audit; current work proceeds independently while preserving unrelated changes. Before every later batch, refresh status, paths, Unity process ownership and task boundaries. Resolve ambiguous canonical files instead of choosing an arbitrary duplicate. The current scene is `Assets/_Project/Scenes/Production/GreaterWasteland.unity`. The inventory locates the terrain scene through preserved GUID `4de5dd018ee194314a00fd369e2d3eeb`, including after its rename to Greater Wasteland. Do not batchmode the project already open in Unity; use a separate project copy and hidden background processes. Preserve the user's foreground application.

## Recovery findings

The six canonical Death Valley `.blend` files are materialized. The imported Badwater heights and colors are intact: 256 height hashes and 256 color hashes match their recorded manifest, all 480 shared source edges agree at common samples, and all 256 TerrainData GUIDs are referenced by the saved scene. These are source and serialized-reference checks, not a new Unity height readback or Player profile.

The initial Windows scan found no `.tif`, `.tiff`, `.npz`, `.npy`, `.las` or `.laz` files in `Docs`, `Assets` or incoming transfer roots. Bounded west-candidate rasters and a prepared grid have since been preserved under `SourceData/Terrain/DeathValley/WestCandidate2026-10-06/`; the current inventory includes that source-data area. The original external `/Users/worldbuilder/Desktop/Death Valley Terrain Data` folder is not confirmed restored. Three specifically checked Windows equivalents were absent; this was not an exhaustive drive search. On October 7 the user provided the original Mac data folder. Its [organized snapshot](../../SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/README.md) now preserves the regional/Mosaic data, expanded overview and detail grids, geology, imagery, masks, acquisition records, and original single/four-slice inputs. This restores the original study data rather than relying on reverse extraction from Unity exports. It does not establish uniformly fine coverage for new footprints.

Unity 6000.4.0f1, Windows standalone build support, and the pinned GIS Python environment are present. Cached Blender 5.2.2 executables were located during the October 7 transfer audit but failed Windows side-by-side startup. Saved-schema inspection verified the nine retained Blender versions and packed resources; a native application reopen remains unavailable. GIS preparation and source validation work independently.

## Approved expansion and prior source proof

The user selected three full 256-chunk sections immediately west, north and northwest of the existing section, forming an 8.192 km square with 1,024 chunks total. The accepted pipeline builds and verifies terrain in Blender before exporting and importing native Unity Terrain. Use the [execution handoff](./HANDOFF_WEST_NORTH_NORTHWEST_2026-10-07.md) for exact bounds, source requirements, seams and integration gates.

The earlier west-quarter candidate covers `[518352,4006200,520400,4008248]`, a 2.048 km square with only 64 chunks. Its verified inputs remain useful partial source evidence; they do not establish complete coverage for the newly selected three-section expansion. Existing candidate records/atlas labels describe that earlier proposal until implementation updates the catalog.

[usgs_west_catalog.json](./usgs_west_catalog.json) records the TNM product query. Catalog intersection alone does not prove coverage. [west_raster_headers.json](./west_raster_headers.json) verifies the two candidate 1 m products' actual raster CRS, spacing and bounds. [west_source_assessment.json](./west_source_assessment.json) records bounded sample acquisition, vertical metadata, complete sample coverage, and agreement with the retained Badwater border.

The candidate preparation has 1,050,625 finite samples at 2 m spacing, derived by preparing native 1 m heights and strict decimation. Direct 2 m reprojection filtered the surface and was rejected. The final shared-edge maximum difference from retained encoded heights is 0.013733 m, within the 0.027467 m encoding-step tolerance. Source overlap differs by up to 0.033844 m; a recorded source-owner split at easting 520000 m makes composition reproducible. No claim of byte-identical recovery or independent survey accuracy follows.

The raw windows, metadata and candidate grid are now preserved with hashes under [SourceData/Terrain/DeathValley/WestCandidate2026-10-06](../../SourceData/Terrain/DeathValley/WestCandidate2026-10-06/manifest.json). Original diagnostic copies remain under ignored `Logs/DeathValleyInventory/west-source-proof/`. Each audit checks all five rebuild-artifact hashes and resolves the preserved copies through their archive manifest; missing files are reported separately from historical verification, and changed files reject the audit. The assessment must describe the same bounds, CRS, sample coverage and passing border tolerance before it can inform the catalog. Keep each new source acquisition in its own versioned source-data directory before terrain integration. No candidate terrain or texture has been imported into Unity.

## Terrain approach

| Approach | Fit and cost | Decision |
| --- | --- | --- |
| Native Unity Terrain from prepared grids | Continues the existing 256 tiles, Terrain colliders, source encoding and neighbor repair. Heightfields cannot express overhangs. Larger coverage still needs measured asset ownership/loading. | Baseline for the first adjoining batch |
| Tiled mesh terrain | Can express custom topology and coordinate with existing production mesh representations. Requires explicit LOD, seam, collision and material integration beyond the current geographic scene. | Retain as an alternative if the Terrain baseline reveals a specific gap |
| Hybrid Terrain plus source-linked meshes | Preserves measured ground while adding separately authored cliff/ledge geometry where needed. Carries additional asset/collider and identity budgets. | Candidate for later local refinement; no custom geometry needed for the basin-floor proposal |

Prepared GIS grids are the shared measured source. For this expansion, the user explicitly requires Blender terrain construction, reopening and seam validation before Unity export/import. Preserve the native Unity Terrain representation and keep the three-section build bounded; full-region uniformly fine terrain is not part of this task.

Before scaling, preserve dataset/version plus absolute projected bounds as source identity; existing `rNN_cNN` identifiers are local addresses, not globally unique keys. The atlas geographic keys are inventory identifiers, not a new runtime save schema. Keep immutable measured heights separate from authored terrain adjustments and persisted player/simulation deltas. Define ready collision for both actors, near/far agreement, unload/reload and precision-safe placement before implementing terrain streaming. Procedural generation and generated-content integration are deferred by the user's current direction. Their future identity and persistence contracts remain design considerations, not work authorized by this terrain batch. Do not introduce a competing world manager.

## Execution stages

| Stage | Output and current state | Gate |
| --- | --- | --- |
| 0. Ownership and preflight | Separate task package; current organization changes left intact; main branch retained; active Unity session identified | Revalidate before each batch and before a scoped commit |
| 1. Coverage inventory | Atlas, eight footprints, six source files, 256 tile records, real NPS park polygon and reference/hash checks complete | Refresh paths after moves; file presence remains distinct from scene readability |
| 2. Recoverability | Targeted tool/data assessment complete; original full GIS snapshot unlocated; small west candidate source verified | The west-quarter archive is preserved; a full new footprint still needs complete source coverage; Blender executable status must be checked when native review is needed |
| 3. Footprint and method | Three 256-chunk sections selected: west, north and northwest; Blender-first construction followed by native Unity Terrain import | Use the execution handoff; complete new source coverage and build validation remain pending |
| 4. Windows baseline | Canonical profiling builder extended for Windows with guarded fresh output; 8/8 focused checks passed; Greater Wasteland Development Player packaged successfully | Windows Player measurements and user traversal/visual review are required before performance decisions or expansion integration |
| 5. Three adjoining sections | Pending | Generalize source/build/export and validation tools; build and verify Blender candidates; validate Unity imports and every join; preserve existing GUIDs and terrain |
| 6. Expanded comparison | Pending | Same views/quality and Player measurements; joins, collision and actor access pass; provisional art is tracked separately |
| 7. Repeat and refine | Pending | Repeat bounded batches; introduce loading infrastructure for measured cost or justified larger scale, not object count alone |
| 8. Procedural world integration | Explicitly deferred | Requires a separately approved generation design; this terrain task adds no procedural systems |

Expansion must leave the current scene usable when preparation, acquisition or validation fails. New batches are validated before replacing references. Back up task-owned candidates and keep previous accepted source versions. Stop for incomplete source coverage, changed source metadata, incompatible seam/coordinate conventions, unresolved ownership or failed validation. Fix blocking terrain/collision defects before integration; do not require final rock or material art to finish geographic preparation.

## Reproduce the audit

From the checkout root with the pinned Python environment:

```powershell
& ./.venv/Scripts/python.exe Docs/DeathValley/audit_coverage.py
```

This refreshes only the generated catalog, CSV, HTML and SVGs in this package. Canonical Blender sources are discovered under `SourceArt/Blender/Studies/`, authoring configs/tools under `Tools/Art/Blender/death_valley/`, and imported assets under `Assets/`; ambiguous matches fail instead of choosing a copy. To acquire a new official boundary snapshot, add `--refresh-boundary`; service output can change, so preserve the prior record before replacing it. Both cached and newly acquired park geometry are validated. A refreshed boundary is published only after the terrain/source checks pass. The tool refuses ambiguous canonical matches, incompatible source provenance or focus tiers, and invalid or changed tile sources.

The optional [verify_west_source.py](./verify_west_source.py) reads bounded remote elevation windows and writes only this package's assessment plus its local ignored diagnostic directory. It verifies source identity independently of catalog ordering, and refuses to reuse any existing output directory, including an incomplete acquisition. It is not a terrain importer or a game-world generator. Before repeating an acquisition, pass `--output Logs/DeathValleyInventory/<new-directory>` rather than overwriting accepted source history. Completed proof files from earlier runs remain preserved.

Run the focused offline regressions without Unity or network access:

```powershell
& ./.venv/Scripts/python.exe -m unittest discover -s Docs/DeathValley -p 'test_*.py' -v
```

For a fresh Windows baseline build, use the existing `BooterBigArm.Editor.BadwaterDevelopmentBuild.BuildFromCli` method in a separately copied project with `-buildTarget StandaloneWindows64` and `-buildOutput` pointing to a new directory's `.exe`. Keep the process hidden and never launch batchmode against the live project's open editor. Build output and diagnostic logs stay outside Assets. The current local executable is `Builds/DeathValleyBaseline-20261006/GreaterWastelandDevelopment.exe`; it is ignored and does not transfer with Git.

## Unity developer map

Open **Booter & BigARM → Death Valley → Developer Map** in Unity. The window is a full-width, simplified 3D aerial map. Left or middle drag pans; right drag orbits; scroll zooms around the cursor. **Entire Map** frames the regional landscape; **Playable Area** frames Greater Wasteland. The blue grid marks its 256 built terrain chunks. Everything outside that grid shows surrounding landscape awaiting terrain expansion. No inventory sidebar, study boundaries, proposed regions, source controls or segment-selection UI is shown.

The map always displays the recovered Blender aerial imagery and starts in an oblique, orbitable orthographic view with no height exaggeration. The [active source descriptor](./visualizer_data/developer_map.json) keeps the saved terrain heights and all four packed image layers. The window creates only temporary preview objects and does not change gameplay terrain or Build Settings. Reopen the window after refreshing source data or the coverage catalog.

The [recovery script](./recover_visualizer_blender.py) reads the compressed Blender file without executing or modifying it. Its bounded [binary reader](./saved_blend_reader.py) resolves Blender's saved DNA schema, ID-scoped data pointers, mesh positions, saved bounds/origin and packed image links. The [recovery proof](./visualizer_data/blender_recovered/recovery_proof.json) records the source hash, 76 terrain meshes, 1,077,281 complete height samples, layer ownership and exact image hashes. Recovery refuses existing output or backup receipts. The earlier dynamic-service grid and its [original descriptor](./visualizer_data/usgs_overview_snapshot.json) remain preserved separately.

Refresh the inventory with `audit_coverage.py` after file moves or accepted terrain changes. The [preparation script](./prepare_visualizer.py) remains available for a new [USGS dynamic elevation service](https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer) snapshot; it refuses existing output directories. Preserve prior sources and keep exactly one active `developer_map.json` under Docs. This map is a development navigation tool, not survey, collision, Player-performance or traversal evidence.

The tool uses existing projected inventory identities and authored bounds. Runtime generation, stable generated objects, streaming/unload/reload and persisted deltas are not applicable to this editor-only preview; it does not implement those systems. Greater Wasteland assets and enabled Build Settings remain unchanged.

[Verification receipt](./developer_map_verification.json) records focused Unity tests and rendered plan-view previews. The navigation tests verify camera-relative panning, cursor-anchored zoom, and complete framing at top-down and oblique angles. Panning and zooming keep the orbit target on its horizontal reference plane. Source recovery proof is retained from the prior validated batch. Live pointer interaction remains user-owned review.

## Everyday terrain tools

[terrain_tools.py](./terrain_tools.py) provides three offline, read-only commands. It uses the last coverage catalog and requires only Python's standard library. Paths derive from this checkout; after repository moves, refresh the inventory before using its recorded paths. Exit codes are 0 for a completed lookup/plan or healthy check, 1 for health issues, and 2 for invalid inputs. Add `--json` before the command for full records suitable for another tool or a saved review artifact.

```powershell
# Check current scene, six Blender files, 512 chunk sources, configs and west inputs.
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py doctor

# Find the chunk beneath a projected point (EPSG:26911 easting/northing).
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py locate 520700 4007100

# Translate Unity X/Z using the current verified scene origin; no origin is guessed.
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py locate -1748 -1148 --unity --origin 522448 4008248

# Inspect the west proposal, including every geographic chunk key and retained join.
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py --json plan west
```

Coordinate lookup returns all sample owners on a shared edge or corner. Study footprints remain separate from imported Unity chunks and proposed batches. The planner also accepts `north` and `east`, rejects overlap, disconnected footprints and grid misalignment, and estimates raw uint16 height payloads at `--spacing 1` or `--spacing 2`. These are inventory keys and preparation estimates; runtime memory and performance require measurements. The health check reports intentional scene edits as changed snapshot bytes until reviewed and refreshed; it does not repair or move files. File hashes do not establish Blender readability or Player acceptance.

These tools operate on authored geographic constraints and inventory identities. They add no runtime world identity, object generation, streaming, unload/reload or persisted deltas; those concerns remain outside this offline tooling scope. Expansion proposals remain pending the baseline review and selection gates above.

## Sources and validation boundaries

The [existing scene record](../../SourceArt/Blender/Studies/DeathValley/UNITY_BADWATER_SCENE.md), [expanded viewer record](../../SourceArt/Blender/Studies/DeathValley/EXPANDED_VIEWER.md), [Windows seam repair](../Evidence/Badwater/terrain-seam-repair-2026-10-06.md), [gameplay terrain plan](../Design/Gameplay/BADWATER_GAMEPLAY_TERRAIN_PLAN.md), and [world systems standard](../Engineering/WORLD_SYSTEMS_STANDARD.md) remain the governing references for their facts and contracts.

USGS provides the [TNM product API](https://www.usgs.gov/faqs/there-api-accessing-national-map-data) and [3DEP elevation products](https://www.usgs.gov/3d-elevation-program/about-3dep-products-services). Retain each product's metadata, datum, source resolution and acquisition hashes. Geographic imagery is color/reference evidence, not an accepted close-range game material; imagery provenance and shipping terms are separate from elevation rights.

Source hashes, raster samples, browser checks, Unity compilation, focused tests, Player packaging and user review establish different claims. See [VERIFICATION.md](./VERIFICATION.md) for current proof. No gameplay smoke test is part of this task.

The next implementation gate is the [baseline review checklist](./REVIEW_CHECKLIST.md), followed by the selected adjoining terrain batch. The Windows executable is ready for user-owned review; adding source-verified terrain remains pending that gate.

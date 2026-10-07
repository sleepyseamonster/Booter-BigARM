# Death Valley coverage and expansion

Death Valley is the user's current geographic expansion focus. In the coordinated scene-promotion task, the user named the existing playable Badwater terrain Greater Wasteland, selected it as the primary scene, and explicitly deferred procedural generation. This package records retained coverage, verifies rebuild inputs, and tracks wider Unity coverage. It does not promote Blender studies to accepted game art or implement a new production World Creator.

## Inspect the coverage

Open [coverage_atlas.html](./coverage_atlas.html) for the official park outline, regional and detail footprints, the four Badwater quarters, and all 256 clickable Unity terrain chunks. Selecting a footprint shows its size, coordinates, resolution and current source availability; the complete source record is available on demand. The map is a north-up projected coverage diagram, not a terrain render. Brown dashed footprints are proposed expansion batches and are not built or selected.

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

Initial organization and scene-promotion coordination is complete. The user ended further agent coordination for the subsequent audit; current work proceeds independently while preserving unrelated changes. Before every later batch, refresh status, paths, Unity process ownership and task boundaries. Resolve ambiguous canonical files instead of choosing an arbitrary duplicate. The inventory locates the terrain scene through preserved GUID `4de5dd018ee194314a00fd369e2d3eeb`, including after its rename to Greater Wasteland. Do not batchmode the project already open in Unity; use a separate project copy and hidden background processes. Preserve the user's foreground application.

## Recovery findings

The six canonical Death Valley `.blend` files are materialized. The imported Badwater heights and colors are intact: 256 height hashes and 256 color hashes match their recorded manifest, all 480 shared source edges agree at common samples, and all 256 TerrainData GUIDs are referenced by the saved scene. These are source and serialized-reference checks, not a new Unity height readback or Player profile.

No `.tif`, `.tiff`, `.npz`, `.npy`, `.las` or `.laz` files were found in the scanned `Docs`, `Assets` or incoming transfer roots. The original external `/Users/worldbuilder/Desktop/Death Valley Terrain Data` folder is not confirmed restored. Three specifically checked Windows equivalents were absent; this was not an exhaustive drive search. Original full-precision region grids, geology grids, prepared masks and acquisition snapshots therefore remain recovery items. Retained Unity exports can rebuild the existing bounded terrain but cannot recover all original 1 m samples or the full regional data snapshot.

Blender was not found on PATH, in the checked install folders or uninstall registry entries. Unity 6000.4.0f1, Windows standalone build support, and the pinned GIS Python environment are present. Blender installation or locating an existing executable is needed for fresh `.blend` readability checks, but does not block the coverage audit or direct GIS-to-Unity preparation.

## First expansion proposal

The proposed first batch is a 2.048 km square immediately west of the existing southwest quarter, bounds `[518352,4006200,520400,4008248]` in EPSG:26911. It would add 64 aligned chunks across basin-floor terrain while sharing one complete edge with the existing scene. North and east alternatives are shown in the atlas. The west proposal remains provisional pending the user's geographic preference and the existing-scene baseline.

[usgs_west_catalog.json](./usgs_west_catalog.json) records the TNM product query. Catalog intersection alone does not prove coverage. [west_raster_headers.json](./west_raster_headers.json) verifies the two candidate 1 m products' actual raster CRS, spacing and bounds. [west_source_assessment.json](./west_source_assessment.json) records bounded sample acquisition, vertical metadata, complete sample coverage, and agreement with the retained Badwater border.

The candidate preparation has 1,050,625 finite samples at 2 m spacing, derived by preparing native 1 m heights and strict decimation. Direct 2 m reprojection filtered the surface and was rejected. The final shared-edge maximum difference from retained encoded heights is 0.013733 m, within the 0.027467 m encoding-step tolerance. Source overlap differs by up to 0.033844 m; a recorded source-owner split at easting 520000 m makes composition reproducible. No claim of byte-identical recovery or independent survey accuracy follows.

The raw windows, metadata and candidate grid are local diagnostic acquisition artifacts under ignored `Logs/DeathValleyInventory/west-source-proof/`, not durable production source storage. Their paths and hashes are recorded, but those paths will not exist in a fresh clone. Each audit checks all five artifact hashes; missing local files are reported separately from the historical verification, and changed files reject the audit. The assessment must describe the same bounds, CRS, sample coverage and passing border tolerance before it can inform the catalog. Choose external source storage and preserve the windows there before terrain integration. No candidate terrain or texture has been imported into Unity.

## Terrain approach

| Approach | Fit and cost | Decision |
| --- | --- | --- |
| Native Unity Terrain from prepared grids | Continues the existing 256 tiles, Terrain colliders, source encoding and neighbor repair. Heightfields cannot express overhangs. Larger coverage still needs measured asset ownership/loading. | Baseline for the first adjoining batch |
| Tiled mesh terrain | Can express custom topology and coordinate with existing production mesh representations. Requires explicit LOD, seam, collision and material integration beyond the current geographic scene. | Retain as an alternative if the Terrain baseline reveals a specific gap |
| Hybrid Terrain plus source-linked meshes | Preserves measured ground while adding separately authored cliff/ledge geometry where needed. Carries additional asset/collider and identity budgets. | Candidate for later local refinement; no custom geometry needed for the basin-floor proposal |

Prepared GIS grids are the shared source for Blender and Unity. Blender is an inspection and authoring surface, not a prerequisite conversion stage for every heightfield. Do not build the entire region as a uniformly fine or permanently loaded mesh.

Before scaling, preserve dataset/version plus absolute projected bounds as source identity; existing `rNN_cNN` identifiers are local addresses, not globally unique keys. The atlas geographic keys are inventory identifiers, not a new runtime save schema. Keep immutable measured heights separate from authored terrain adjustments and persisted player/simulation deltas. Define ready collision for both actors, near/far agreement, unload/reload and precision-safe placement before implementing terrain streaming. Procedural generation and generated-content integration are deferred by the user's current direction. Their future identity and persistence contracts remain design considerations, not work authorized by this terrain batch. Do not introduce a competing world manager.

## Execution stages

| Stage | Output and current state | Gate |
| --- | --- | --- |
| 0. Ownership and preflight | Separate task package; current organization changes left intact; main branch retained; active Unity session identified | Revalidate before each batch and before a scoped commit |
| 1. Coverage inventory | Atlas, eight footprints, six source files, 256 tile records, real NPS park polygon and reference/hash checks complete | Refresh paths after moves; file presence remains distinct from scene readability |
| 2. Recoverability | Targeted tool/data assessment complete; original full GIS snapshot unlocated; small west candidate source verified | Source archive storage and Blender executable still need resolution for their respective uses |
| 3. First batch and method | West candidate bounded and assessed; native Terrain baseline recommended; alternatives recorded | User preference may change the footprint; Greater Wasteland promotion is separately owned; full playable extent still needs geographic review |
| 4. Windows baseline | Canonical profiling builder extended for Windows with guarded fresh output; 8/8 focused checks passed; Greater Wasteland Development Player packaged successfully | Windows Player measurements and user traversal/visual review are required before performance decisions or expansion integration |
| 5. One adjoining batch | Pending | Generalize hardcoded exporter bounds, prepare versioned height/color assets, validate separately, preserve GUIDs, then integrate only verified task-owned assets |
| 6. Expanded comparison | Pending | Same views/quality and Player measurements; joins, collision and actor access pass; provisional art is tracked separately |
| 7. Repeat and refine | Pending | Repeat bounded batches; introduce loading infrastructure for measured cost or justified larger scale, not object count alone |
| 8. Procedural world integration | Explicitly deferred | Requires a separately approved generation design; this terrain task adds no procedural systems |

Expansion must leave the current scene usable when preparation, acquisition or validation fails. New batches are validated before replacing references. Back up task-owned candidates and keep previous accepted source versions. Stop for incomplete source coverage, changed source metadata, incompatible seam/coordinate conventions, unresolved ownership or failed validation. Fix blocking terrain/collision defects before integration; do not require final rock or material art to finish geographic preparation.

## Reproduce the audit

From the checkout root with the pinned Python environment:

```powershell
& ./.venv/Scripts/python.exe Docs/DeathValley/audit_coverage.py
```

This refreshes only the generated catalog, CSV, HTML and SVGs in this package. To acquire a new official boundary snapshot, add `--refresh-boundary`; service output can change, so preserve the prior record before replacing it. Both cached and newly acquired park geometry are validated. A refreshed boundary is published only after the terrain/source checks pass. The tool refuses ambiguous canonical matches, incompatible source provenance or focus tiers, and invalid or changed tile sources.

The optional [verify_west_source.py](./verify_west_source.py) reads bounded remote elevation windows and writes only this package's assessment plus its local ignored diagnostic directory. It verifies source identity independently of catalog ordering, and refuses to reuse any existing output directory, including an incomplete acquisition. It is not a terrain importer or a game-world generator. Before repeating an acquisition, pass `--output Logs/DeathValleyInventory/<new-directory>` rather than overwriting accepted source history. Completed proof files from earlier runs remain preserved.

Run the focused offline regressions without Unity or network access:

```powershell
& ./.venv/Scripts/python.exe -m unittest discover -s Docs/DeathValley -p 'test_*.py' -v
```

For a fresh Windows baseline build, use the existing `BooterBigArm.Editor.BadwaterDevelopmentBuild.BuildFromCli` method in a separately copied project with `-buildTarget StandaloneWindows64` and `-buildOutput` pointing to a new directory's `.exe`. Keep the process hidden and never launch batchmode against the live project's open editor. Build output and diagnostic logs stay outside Assets. The current local executable is `Builds/DeathValleyBaseline-20261006/GreaterWastelandDevelopment.exe`; it is ignored and does not transfer with Git.

## Sources and validation boundaries

The [existing scene record](../Agents/Blender/studies/DeathValley/UNITY_BADWATER_SCENE.md), [expanded viewer record](../Agents/Blender/studies/DeathValley/EXPANDED_VIEWER.md), [Windows seam repair](../Evidence/Badwater/terrain-seam-repair-2026-10-06.md), [gameplay terrain plan](../BADWATER_GAMEPLAY_TERRAIN_PLAN.md), and [world systems standard](../WORLD_SYSTEMS_STANDARD.md) remain the governing references for their facts and contracts.

USGS provides the [TNM product API](https://www.usgs.gov/faqs/there-api-accessing-national-map-data) and [3DEP elevation products](https://www.usgs.gov/3d-elevation-program/about-3dep-products-services). Retain each product's metadata, datum, source resolution and acquisition hashes. Geographic imagery is color/reference evidence, not an accepted close-range game material; imagery provenance and shipping terms are separate from elevation rights.

Source hashes, raster samples, browser checks, Unity compilation, focused tests, Player packaging and user review establish different claims. See [VERIFICATION.md](./VERIFICATION.md) for current proof. No gameplay smoke test is part of this task.

The next implementation gate is the [baseline review checklist](./REVIEW_CHECKLIST.md), followed by the selected adjoining terrain batch. The Windows executable is ready for user-owned review; adding source-verified terrain remains pending that gate.

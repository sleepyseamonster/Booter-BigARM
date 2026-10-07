# Death Valley west, north and northwest expansion handoff

## Objective and accepted pipeline

Build three new terrain sections adjoining the current Greater Wasteland section: **west, north and northwest**. Each new section has 256 chunks. Acquire or reuse correct elevation and imagery, build the terrain in Blender, prove that it joins the existing terrain, then export and import it into Unity.

The user explicitly selected **GIS data → Blender terrain construction and review → exported height data/imagery → Unity Terrain** because they like the existing Blender results. This supersedes the earlier suggestion to bypass Blender. Keep Blender as a real build and verification stage. Continue native Unity Terrain for the playable ground; changing it to an FBX mesh landscape is not part of this brief.

This is an implementation brief for the next agent when the user assigns it. The handoff-writing task itself does not acquire new data, build terrain or create another chat. The requested footprint and pipeline are settled; do not ask the user to choose the direction or reconfirm Blender-first work.

## Exact footprint

Working CRS is **EPSG:26911**, horizontal units metres. Bounds below are `[east_min, north_min, east_max, north_max]`.

| Section | Projected bounds | Unity X bounds | Unity Z bounds | Chunks |
| --- | --- | --- | --- | --- |
| Existing southeast section | `[520400,4006200,524496,4010296]` | `[-2048,2048]` | `[-2048,2048]` | 256, preserved |
| New west section | `[516304,4006200,520400,4010296]` | `[-6144,-2048]` | `[-2048,2048]` | 256 |
| New north section | `[520400,4010296,524496,4014392]` | `[-2048,2048]` | `[2048,6144]` | 256 |
| New northwest section | `[516304,4010296,520400,4014392]` | `[-6144,-2048]` | `[2048,6144]` | 256 |

```text
               NORTH
      ┌─────────────┬─────────────┐
      │ Northwest  │ North       │
      │ new 256    │ new 256     │
      ├─────────────┼─────────────┤
      │ West       │ Existing    │
      │ new 256    │ preserved256│
      └─────────────┴─────────────┘
```

Each section is **4,096 × 4,096 m**, arranged as 16 × 16 chunks of **256 × 256 m**. The combined area is **8,192 × 8,192 m**, bounds `[516304,4006200,524496,4014392]`, with **1,024 chunks**: 768 new and 256 existing. Section interiors must not overlap; shared edge vertices are intentional.

Preserve the shared Unity origin **east 522448, north 4008248**. Use `Unity X = east − 522448`, `Unity Z = north − 4008248`; world elevations remain metres. Do not recenter the game world on the enlarged rectangle. For Blender, use X east, Y north, Z elevation with the same documented origin, and explicitly verify the export axis conversion.

Local row IDs increase north-to-south; columns increase west-to-east. A local `r00_c00` is not a globally unique identity. Use a section prefix plus projected chunk identity, such as `epsg26911/e516304/n4006200/size256`, and recorded source version. Terrain transforms use the chunk's southwest corner.

For section bounds `[xmin,ymin,xmax,ymax]` and local indices `row,col` in `0..15`, the chunk southwest corner is `east = xmin + 256*col`, `north = ymax − 256*(row+1)`. Apply the shared Unity origin to that corner; use `[east,north,east+256,north+256]` as the chunk bounds.

## First reads and current workspace

Read root [AGENTS.md](../../AGENTS.md), [local workspace guide](../LOCAL_WORKSPACE.md), and the relevant Gottspan, Blender, Babineaux and Gear Ball role entry points under `Docs/Agents/`. Then read [terrain workflow](./TERRAIN_SLICE_WORKFLOW.md), [current verification](./VERIFICATION.md), and [original Mac snapshot README](../../SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/README.md).

Work on `main` tracking `origin/main`; inspect live Git state before changes. The last terrain-data restoration commit is `5e08165`. At handoff creation, `Assets/_Project/Materials/TopDown3D/Greybox_BigARM.mat` is an unrelated dirty file; preserve it. Later live state takes precedence over this snapshot. Do not coordinate with other chats unless the user newly authorizes it; do not include their work in this batch.

Unity is pinned to **6000.4.0f1**, URP **17.4.0**, with the sole production 3D renderer at index 0. Primary scene and only enabled build scene:

`Assets/_Project/Scenes/Production/GreaterWasteland.unity`

Preserve its GUID **`4de5dd018ee194314a00fd369e2d3eeb`**, existing terrain GUIDs, gameplay setup, player controls, UI, materials and references. This is terrain expansion, not gameplay redesign. The disabled generated-world reference and all archives remain inactive.

Preserve the user's foreground application. Use hidden background Blender/Unity processes. Never start Unity batchmode against a project already open in Unity; validate in a separate project copy. Do not create or run gameplay smoke tests. Hands-on traversal and final visual acceptance belong to the user. Local commits are required after verification; pushes, PRs, branch/worktree changes and releases are not authorized by this brief.

## What has been recovered and verified

- [MacSnapshot2026-10-07](../../SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/README.md) restores the original regional/Mosaic, `expanded_v2`, `game_slice_v1`, and `four_slices_v1` data: DEMs, imagery, geology, masks, grids, source windows, metadata and reports. The transfer receipt inventories all 3,603 delivered files; 573 are source/study files. The Mac `.venv` is excluded from tracking; use the pinned Windows `.venv`.
- Original four-slice manifest and geographic-color hashes match the current Unity provenance. Fresh four-slice verification passed: 256 chunks, unchanged original southwest source, identical RAW borders, maximum quantization error approximately 0.013733 m.
- Original regional, corridor, pilot and canyon checks passed. All 491 archived source references resolved. Distinct Blender studies and dated backups were preserved under SourceArt; working sources were not overwritten.
- [WestCandidate2026-10-06](../../SourceData/Terrain/DeathValley/WestCandidate2026-10-06/manifest.json) contains a separately verified **64-chunk** west-quarter proposal. It covers `[518352,4006200,520400,4008248]`, not the entire requested west section or the northern sections.
- Existing 256 Unity chunks retain 2 m source detail except four 1 m focus tiles: `r12_c01`, `r12_c02`, `r13_c01`, `r13_c02`. Unity rendering uses 257 × 257 grids. Keep existing source/render heights intact.

Original manifests retain Mac absolute paths and original hashes. Use [terrain_archive.py](./terrain_archive.py) to create temporary Windows manifest views under `Logs/DeathValleyTransfer/`; do not rewrite archived metadata. Preserve original manifest identity separately from the hash of a rebased local view.

The restored broad overview is not a fine gameplay DEM. The developer map uses a 200 m sample lattice displayed on an 800 m mesh. Restored native rasters also include bounded windows; possession of them does not prove complete coverage for all three new sections.

## Tools and known implementation gaps

Authoring tools live under `Tools/Art/Blender/death_valley/`. Useful starting points are `inspect_dem_catalog.py`, `prepare_game_slice.py`, `prepare_four_slices.py`, `build_game_slice_scene.py`, `verify_game_slice.py`, `verify_four_slices.py`, `verify_game_slice_scene.py`, `render_four_slices_preview.py`, `export_badwater_unity_source.py`, `fetch_landsat_reference.py` and the imagery-composition tools.

The original preparation scripts assume particular source products and a previous southwest slice. The exporter rejects geographic bounds other than the existing section. `BadwaterUnitySceneBuilder.cs` is a preserved reference outside active Editor compilation and contains fixed paths, origin, height range and focus tiles. Do not run it against production or copy it unchanged as the expansion implementation.

Active production validators and installers currently assume 256 terrains. Audit and generalize `BadwaterPlayableSceneBuilder`, `BadwaterTerrainSeamRepair`, `BadwaterTerrainReadbackAudit`, relevant tests, and `audit_coverage.py` for multiple section manifests. `BadwaterTerrainConnectivity` only reconnects terrains beneath its own parent: four separate section parents must not leave cross-section neighbors disconnected. Extend the existing lane rather than adding a competing world manager.

The source-data/archive tools have 27 passing offline regressions. The simplified developer map has 16 passing focused Unity tests and inspected 3D previews. These are baseline evidence, not proof that the expansion exists.

Initial repository-root checks:

```powershell
git status --short --branch
& ./.venv/Scripts/python.exe Docs/DeathValley/audit_coverage.py
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py doctor
& ./.venv/Scripts/python.exe -m unittest discover -s Docs/DeathValley -p 'test_*.py' -v
```

Inventory refresh writes only the research catalog/atlas outputs. Original-data validation commands and safe Windows manifest conversion are in the Mac snapshot README. Current `terrain_tools.py plan west` reports the old 64-chunk proposal; it is not the manifest for this three-section task.

## Implementation sequence

### 1. Freeze the existing baseline and restore native Blender execution

Capture hashes, terrain transforms, size/resolution, height readback and GUID references for all 256 existing chunks. Record the west and north boundary samples and the existing northwest corner before any candidate work. Keep retained source grids and existing production TerrainData as the baseline.

Locate and verify a working Blender runtime compatible with the saved studies; **5.2.2 LTS** is the preferred baseline. Cached executables under `%LOCALAPPDATA%/BooterBigArm/Tools/Blender/` failed Windows side-by-side startup during restoration. File presence is not execution proof. Repair or replace the runtime safely and prove background startup before building. Binary extraction alone does not satisfy the user's Blender-build stage. Source preparation can continue independently while resolving runtime availability.

### 2. Establish complete source coverage

Inspect restored files first, then acquire only missing elevation/imagery coverage for the union and necessary sampling halo. Use the recorded USGS 3DEP/TNM, NAIP and Landsat sources and metadata. Confirm actual raster extents, source resolution, CRS, vertical datum, surface type and NoData. Do not infer coverage from a catalog hit, a product name or the size of the regional preview.

Prepare a common aligned source model for all three new sections. A 4,096 m section contains 4,097 × 4,097 vertices at 1 m spacing, or 2,049 × 2,049 at 2 m. Native 1 m preparation followed by strict 2 m decimation preserves common samples; direct coarse resampling previously failed border agreement. Use bounded/blockwise acquisition where required and deterministic product ownership in overlaps. Record source URLs, requests, dates, datum, transforms, hashes and coverage in new versioned source directories. Report unsupported fine coverage instead of presenting interpolated measurements as native detail.

Keep existing accepted source samples authoritative at retained boundaries. If a new snapshot disagrees, quantify the difference and preserve raw measurements. Use a documented candidate transition only where justified; do not reshape existing terrain silently or distort a broad area merely to conceal an alignment/datum error.

### 3. Build all three sections in Blender

Generalize the current build tools around section manifests. Create separate, clearly named new-section collections with 256 aligned chunk objects each. Reuse the existing terrain style and geographic imagery approach. Include a protected reference representation of the existing section in a combined review file so all four sections can be inspected together. Work in new `.blend` files under SourceArt; do not overwrite `BadwaterGameSlice.blend` or the regional studies.

Use measured elevation as the terrain base and aligned imagery as geographic color. Separate any authored adjustment from raw source data. Do not blindly copy the old four focus-tile coordinates into new sections; record any new detail areas and their source support. Pack or explicitly retain resources so the review scene can reopen on this Windows machine.

Save, reopen and verify the Blender files with embedded-script execution disabled. Check mesh coordinates against prepared grids, section transforms, elevations, bounds, chunk counts, normals and image orientation. Render consistent overview, oblique and seam close-ups of the combined area. Do not claim visual quality solely from a script report.

### 4. Prove all joins before Unity import

There are four section interfaces, each 4,096 m long:

- New west ↔ existing section at easting **520400**.
- New north ↔ existing section at northing **4010296**.
- New northwest ↔ new west at northing **4010296**.
- New northwest ↔ new north at easting **520400**.

Each interface joins 16 chunk pairs. All four sections meet at **`[520400,4010296]`**; verify this shared vertex and the junction around it explicitly. Also verify internal tile borders. A fully connected 32 × 32 grid has **1,984 adjacent chunk-edge pairs**, including the existing 480.

Check common source samples, Blender mesh boundaries, image alignment/color transitions, final Unity render samples and collider readback as separate gates. Adjacent new exports sharing one prepared grid should agree on common samples. For retained joins, compare decoded world heights and report tolerances derived from source/encoding precision; do not widen tolerance to hide an incorrect transform or datum. The current encoding step is approximately 0.02747 m and the prior west-quarter comparison differed by approximately 0.01373 m; these are references, not an automatic acceptance result for the new sections.

### 5. Export and import isolated Unity candidates

Export documented, source-aligned heights and imagery from the verified Blender preparation, then create native Unity Terrain using an active, parameterized Editor builder. Preserve little-endian encoding and row conventions; north-first source arrays must become north-positive Unity Z. Use new asset folders, GUIDs, section identities and a candidate scene outside enabled Build Settings.

The existing uint16 encoding spans **−100 to 1,700 m** with Terrain Y base −100 and vertical size 1,800. Check actual new elevations before choosing a range. Reject clipping. If a new range is needed, support it in exporters, importers and validators, and compare seams in world metres rather than raw integers. Existing terrain must retain its current heights and references.

Run fresh Unity import/compile checks, focused EditMode tests and candidate readback validation. Verify all 768 new terrains/colliders, exact dimensions/placement, internal and cross-section neighbors, material alignment, absence of missing scripts, and unchanged baseline terrain. Aerial imagery remains geographic reference; this task does not require final close-range rock dressing or new gameplay systems.

### 6. Integrate the verified expansion and update coverage

After source, Blender and Unity candidate gates pass, integrate task-owned new assets into Greater Wasteland while preserving its scene GUID and gameplay setup. Generalized validation must require exactly 1,024 terrains and corresponding colliders, all expected geographic keys, complete neighbor relationships, and no duplicate/overlapping interiors.

Refresh the coverage catalog/atlas and developer map data so the cyan grid includes all four playable sections. Keep the developer UI simple: aerial 3D view, pan/zoom/orbit, blue playable grid, and `Entire Map` / `Playable Area` buttons. Do not reintroduce the inventory sidebar or proposed/study overlays. The Playable Area button and catalog must frame the entire enlarged footprint rather than the old section alone.

Package a fresh Development Player when relevant to final integration proof and report measured/remaining validation separately. Do not launch gameplay smoke tests. Provide seam render evidence and a user-owned traversal checklist for both actors across the new area and all four-way junctions.

## Architectural boundaries and stop conditions

Geographic bounds, source versions and section-qualified chunk identities provide deterministic authored terrain identity. Keep authored constraints explicit and raw elevations immutable. Streaming/unload/reload and persisted runtime deltas are not implemented in this fixed-terrain expansion; procedural generation remains deferred. Do not activate prototype generators, generated props, procedural saves or random terrain, and do not introduce a new save/object identity scheme to complete this job.

Stop a dependent stage for incomplete coverage, source hash/datum changes, unresolved overlap ownership, mismatched joins, clipped heights, failed native Blender builds, failed Unity checks or conflicting edits. Preserve the current playable scene and earlier passing candidates. Continue independent source/documentation work where possible. Ask only for a concrete unresolved decision or authorization, not for already-settled footprint/pipeline choices.

## Deliverables and closeout

Deliver versioned source/batch manifests and acquired inputs; reproducible preparation/build/export commands; three verified Blender sections plus combined review; source/Blender/Unity seam reports and renders; validated new Terrain assets; the integrated 1,024-chunk scene with preserved baseline; refreshed cyan coverage; and a concise handoff naming exact commands, checks, commits and remaining user review.

Commit verified task-owned files in coherent batches. Preserve unrelated dirty files and existing source versions. Do not push. Completion requires actual built/reopened Blender terrain and validated Unity integration; preparation scripts or a cyan map overlay alone do not establish a playable expansion.

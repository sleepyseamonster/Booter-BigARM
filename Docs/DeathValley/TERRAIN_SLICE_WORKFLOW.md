# Building another Death Valley terrain slice

The user-selected production route is **verified GIS elevation and imagery â†’ Blender terrain construction and seam review â†’ exported heights/imagery â†’ native Unity Terrain**. Blender is a required build and verification stage for this expansion. Native Unity Terrain remains the playable ground representation. The [west/north/northwest handoff](./HANDOFF_WEST_NORTH_NORTHWEST_2026-10-07.md) supplies the accepted footprint and complete execution brief.

This guide documents the existing contracts and the next implementation boundary. The current tools reproduce or validate specific existing footprints; there is not yet a general command that builds and integrates an arbitrary new 256-chunk slice.

The original-stage boundaries below are historical. Current production contains 2,048 chunks after the [next west/northwest pair](./NEXT_WEST_PAIR_IMPLEMENTATION_2026-10-08.md). Its acquisition, preparation, native Blender adapter and guarded exporter are `acquire_next_west_pair.py`, `prepare_next_west_pair.py`, `build_next_west_pair_scene.py` and `export_next_west_pair_unity.py` under `Tools/Art/Blender/death_valley/`. Unity uses the `BadwaterNextWestPair` source, isolated builder, baseline audit and validator. These retain strict eight-section identities and do not authorize arbitrary footprints or source reuse without fresh coverage and join proof. All accepted task artifacts and temporary build files are on D:.

The subsequent [further western pair](./FAR_WEST_PAIR_IMPLEMENTATION_2026-10-08.md) extends current production to 2,560 chunks. Its four `far_west_pair` Python adapters and `BadwaterFarWestPair` Unity classes preserve strict ten-section bounds and the complete 2,048-terrain baseline. The preceding paragraph describes the retained eight-section batch, not current total coverage.

The [western ridge pair](./WEST_RIDGE_PAIR_IMPLEMENTATION_2026-10-08.md), completed October 9, extends current production to 3,072 chunks. Its `west_ridge_pair` Python adapters and `BadwaterWestRidgePair` Unity classes preserve the complete 2,560-terrain baseline and strict twelve-section bounds. Earlier batch paragraphs above are retained implementation history.

The [single northern ridge cap](./NORTH_RIDGE_IMPLEMENTATION_2026-10-09.md) adds `north_ridge` at `[499920,4014392,504016,4018488]`. Its saved production integration passes 3,328 chunks, preserving all 3,072 prior terrains. The `north_ridge` Python adapters and `BadwaterNorthRidge` Unity contracts enforce the exact union of the retained 96 Ã— 32 rectangle and the new 16 Ã— 16 northern cap. The AABB `[499920,4006200,524496,4018488]` is a framing boundary; 1,280 upper-east cells are unbuilt. Saved production readback and the refreshed atlas pass; the open Editor awaits its external-change Reload dialog.

## Size and coordinates

One new 256-chunk section consists of **16 Ã— 16 chunks**, each **256 Ã— 256 metres**, covering **4,096 Ã— 4,096 metres**. A 64-chunk quarter is 2,048 Ã— 2,048 metres. These are different batch sizes.

| Contract | Required value |
| --- | --- |
| Projected coordinates | EPSG:26911, metres; east and north |
| Bounds order | `[east_min, north_min, east_max, north_max]` |
| Original section bounds | `[520400,4006200,524496,4010296]` |
| Latest integrated candidate bounds | `[499920,4006200,524496,4018488]`; sparse occupied union, not a filled rectangle |
| Shared Unity origin | East `522448`, north `4008248` |
| Unity placement | `X = east âˆ’ 522448`; `Z = north âˆ’ 4008248`; elevations remain metres |
| Chunk alignment | Offset from existing minimum easting/northing must be a multiple of 256 m |
| Inventory identity | Projected lower-left coordinate, chunk size and source version |
| Tile addressing | Local rows north-to-south and columns west-to-east; `rNN_cNN` alone is not globally unique |

Keep the shared Unity origin when adding a neighboring section. Recentring each section independently would place it over the current scene. Each Terrain transform uses its chunk's southwest corner; the developer map displays those same projected bounds.

**Approved west section:** `[516304,4006200,520400,4010296]`. North and northwest sections are also approved; their bounds are listed in the handoff. Complete fine source coverage and seam validation remain to be established. The existing verified west candidate covers only `[518352,4006200,520400,4008248]`, one 64-chunk quarter. Its source proof does not establish coverage for the remaining 192 chunks.

## Source preparation

1. Use the approved west, north and northwest footprints in the handoff and confirm grid alignment without interior overlap. Record section-qualified batch names, bounds, source spacing, intended detail areas and source versions.
2. Identify actual USGS DEM products covering the whole footprint. Verify raster CRS, resolution, complete finite coverage, surface type, vertical datum, metadata and hashes. Catalog intersection by itself does not prove coverage. Preserve source windows and metadata under a new versioned `SourceData/Terrain/DeathValley/` directory.
3. Prepare one consistent measured grid, including the shared boundary samples. For a 4,096 m square, a 1 m vertex grid has 4,097 Ã— 4,097 samples; a 2 m grid has 2,049 Ã— 2,049 samples. Native 1 m preparation followed by strict 2 m decimation preserves common samples. Direct coarse resampling previously changed the retained border and failed verification.
4. Split the grid into 256 tiles with shared edge vertices. At 2 m spacing each source tile is 129 Ã— 129 samples; at 1 m it is 257 Ã— 257. The current Unity render convention is 257 Ã— 257. Interpolated render vertices do not create new measured detail.
5. Prepare aligned imagery separately. Preserve source images, projected bounds and hashes. Aerial imagery provides geographic context; close-range gameplay surfaces require their own material work.

The developer overview uses saved Blender terrain sampled at 200 m and displays an 800 m mesh. It is a coverage and navigation reference, not a source of fine gameplay heights. The original native grids and acquisition files have since been restored in the [Mac data snapshot](../../SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/README.md); use those recorded inputs where they cover the new footprint. Its packed imagery can guide inspection, but new detailed terrain requires adequate original or newly acquired DEM coverage.

## Height encoding and joins

Keep full precision measured elevations before encoding or authoring changes. The existing source tiles use little-endian uint16 with north-first rows and the range **âˆ’100 to 1,700 m**. Their quantization step is approximately **0.02747 m**. A mountain section extending above that range needs an explicitly supported range; silently clipping or reusing the current normalization would produce incorrect terrain.

Validate every internal shared edge and the entire join to Greater Wasteland in world elevation units. Matching raw integers only proves matching heights when both tiles use the same encoding range. Decode north-first rows into Unity's north-positive Z convention. Mixed-resolution borders must agree on the final render samples and on collider readback.

The verified west quarter's retained-border difference was approximately 0.01373 m, inside the existing encoding-step tolerance. That is proof for that source snapshot and boundary only. A new footprint or source version requires its own check. Record source ownership where DEM products overlap so future rebuilds make the same choice.

## Unity creation and integration

Build and reopen the new terrain in Blender first, validating source correspondence and all existing/new joins. Export documented height and image assets from that verified build. Create a Unity candidate under its own `Assets/_Project/Art/Terrain/<batch>/` asset folder and use a separate validation scene outside enabled Build Settings. Generate TerrainData, source assets, materials and colliders with new GUIDs. Preserve the existing terrain's assets and GUIDs.

Validate dimensions, geographic placement, source hashes, height readback, shared borders, neighbor links and collision before adding the candidate to `Assets/_Project/Scenes/Production/GreaterWasteland.unity`. Integration must retain the current player setup, controls and scene GUID. Current batch validators enforce explicit accepted footprints and counts; a new batch requires its own bounded contract rather than relaxing an earlier validator. For the northern cap, require 6,512 exact joins, 288 exterior neighbor slots and 83,200 collider samples across the actual occupied union.

After integration, refresh the coverage catalog so the blue grid represents the new playable footprint, and perform user-owned traversal and visual review. Compare Player measurements before accepting additional loading costs. Streaming, procedural generation and generated-world save integration remain deferred; this workflow adds authored terrain and geographic inventory identities, not a second runtime world manager.

## Current tools and limits

| Entry point | Available use | Boundary |
| --- | --- | --- |
| [audit_coverage.py](./audit_coverage.py) | Refresh catalog, source hashes, scene references and atlas for accepted batches | Requires the latest production scene/manifest proof; northern-cap coverage rejects holes, shifted caps and filled-AABB assumptions |
| [terrain_tools.py](./terrain_tools.py) | Health check, projected/Unity point lookup, candidate planning | `plan west/north/east` describes 64-chunk proposals, not new 256-chunk builders |
| [verify_west_source.py](./verify_west_source.py) | Acquire and verify the known west quarter | Fixed footprint and known source products; not a general acquisition command |
| [prepare_game_slice.py](../../Tools/Art/Blender/death_valley/prepare_game_slice.py) | Native DEM preparation reference | Existing single-source study assumptions and export conventions |
| [prepare_four_slices.py](../../Tools/Art/Blender/death_valley/prepare_four_slices.py) | Existing four-quarter preparation reference | Specific source pair, previous southwest manifest and source-owner join |
| [export_badwater_unity_source.py](../../Tools/Art/Blender/death_valley/export_badwater_unity_source.py) | Existing Badwater source handoff reference | Rejects other geographic bounds; requires its specific color grid |
| [BadwaterUnitySceneBuilder.cs](../../Tools/Art/Blender/death_valley/BadwaterUnitySceneBuilder.cs) | Preserved terrain-creation reference | Outside active Editor compilation; fixed paths, origin, height range and detail tiles; not a production expansion command |
| [BadwaterPlayableSceneBuilder.cs](../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterPlayableSceneBuilder.cs) | Existing gameplay installation and validation | Writes the production scene in build mode and expects 256 existing Terrain objects; does not create new terrain |
| [BadwaterDevelopmentBuild.cs](../../Assets/_Project/Scripts/Editor/TopDown3D/BadwaterDevelopmentBuild.cs) | Package the current scene for review | Does not acquire, create or integrate terrain |

Do not run the preserved scene builder against production as an expansion shortcut. The repeatable next-slice implementation needs a parameterized source/batch manifest, complete-footprint acquisition, a guarded exporter and active candidate builder, and batch-aware validators and coverage refresh. Review their isolated outputs before production integration.

## Commands available now

Run from the repository root:

```powershell
& ./.venv/Scripts/python.exe Docs/DeathValley/audit_coverage.py
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py doctor
& ./.venv/Scripts/python.exe Docs/DeathValley/terrain_tools.py --json plan west
& ./.venv/Scripts/python.exe -m unittest discover -s Docs/DeathValley -p 'test_*.py' -v
```

The first command refreshes only the research inventory outputs. The second checks current files. The third prints the existing 64-chunk west proposal without selecting or importing it. The last command runs offline tooling/source regressions, not gameplay tests.

The west source files are preserved in [SourceData/Terrain/DeathValley/WestCandidate2026-10-06](../../SourceData/Terrain/DeathValley/WestCandidate2026-10-06/manifest.json); availability checks prefer these hash-matched durable copies over ignored Logs. The [original Mac study archive](../../SourceData/Terrain/DeathValley/MacSnapshot2026-10-07/README.md) is now restored, including its native single/four-slice source grids. It still does not imply full-region 1 m coverage; validate sources for each new footprint. Saved Blender sources live under [SourceArt/Blender/Studies/DeathValley](../../SourceArt/Blender/Studies/DeathValley/README.md), and original authoring tools live under `Tools/Art/Blender/death_valley/`. Dated receipts retain original paths; relocation resolution preserves their source identity.

The original three-section handoff and later western pairs are completed batch history. The current northern-cap closeout requires final saved production readback and inventory refresh after the isolated integration passes. Further geographic additions require a new approved footprint, fresh source coverage and the same Blender-first verification gates.

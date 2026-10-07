# Expanded Death Valley and Badwater Blender viewer

**Build date:** 2026-10-05. **File:** [DeathValleyExploreExpanded.blend](DeathValleyExploreExpanded.blend). The prior [DeathValleyExplore.blend](DeathValleyExplore.blend) is preserved.

## Open and navigate

Open the expanded `.blend` in Blender 5.2.2 LTS. It saves a free User Perspective view over Badwater Basin, with a yellow candidate detail boundary. Middle mouse orbits, Shift + middle mouse pans, the wheel zooms, and **Home** frames the full regional map. The Outliner has named `DeathValley_RegionalTerrain_200m`, `BadwaterCorridor40m`, `BadwaterPilot20m`, `BadwaterCanyonPatch10m`, and review-marker collections. Toggle the finer collections if you want to compare the broad terrain. There are no camera objects or game assets.

The expanded file was opened and verified in the single Blender window for regional review. It remains preserved on disk alongside the earlier file. The current close-range review scene is [BadwaterGameSlice.blend](BadwaterGameSlice.blend).

## What is built

| Layer | EPSG:26911 bounds, metres | Resolution and color reference | Role |
| --- | --- | --- | --- |
| Expanded region | `[399400, 3920200, 591400, 4144200]` | 200 m DEM; full-extent natural-color Landsat reference | 192 × 224 km, six by seven full 32 km source tiles |
| Badwater candidate | `[495400, 3984200, 543400, 4024200]` | 40 m DEM; 20 m NAIP color with Landsat gap fill | 48 × 40 km cross-valley basin, canyon, cliff and eastern-range context |
| Basin-edge pilot | `[515400, 4004200, 523400, 4012200]` | 20 m DEM; 10 m NAIP color with Landsat gap fill | 8 × 8 km terrain comparison across playa and eastern bedrock |
| Eastern canyon patch | `[520400, 4006200, 522400, 4008200]` | 10 m DEM; 2 m NAIP color with Landsat gap fill and broad color match | 2 × 2 km terrain and image-detail example, without rock geometry |

The regional source grid has 42 complete tiles. Coarse faces under detail are omitted to avoid overlapping surfaces, and the one fully replaced coarse object is omitted from the final Outliner. The scene has 76 mesh objects, **4,754,400 evaluated terrain triangles**, four packed images, and no camera. The `.blend` is about 85 MB. The yellow outline and Badwater marker are geographic review guides, not game objects. The screenshot arrow's projected target is approximate and lies inside the candidate corridor; the exact footprint remains adjustable after visual review.

## Data and verification

The new source snapshot and recorded service responses live in `/Users/worldbuilder/Desktop/Death Valley Terrain Data/expanded_v2/`. Preserve that directory to rebuild this exact study. The four prepared manifests and their raw DEM/NAIP responses are under its overview, `corridor_40m`, `pilot_20m`, and `canyon_patch_10m` directories. `native_dem_catalog.json` records intersecting USGS 1/3 arc-second and 1 m products. Catalog overlap is **not** a claim of 1 m coverage at every point; the scene uses 40, 20, and 10 m sampled terrain. The broad service can mix source products, so retain its response and datum caveat rather than claiming survey precision.

- USGS 3DEP elevation: [dynamic service](https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer). The full-tile grid is 961 × 1121 vertices, and every old/new overlapping elevation sample matched exactly in this snapshot.
- USGS NAIP: [image service](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer). Valid NAIP coverage was 99.97% in the corridor, 99.90% in the pilot, and 98.58% in the canyon patch. A recorded Landsat export fills gaps. The 2 m patch's broad color was matched to the parent image after the first render revealed a visible rectangle.
- Regional geology: [USGS MF-2381](https://pubs.usgs.gov/mf/2002/mf-2381/) was rasterized categorically on each new grid. It is metadata/authoring reference; the visible color remains geographic imagery.
- The full-tile elevation, geology, and packed color hashes passed `verify_pipeline.py` in terrain-only mode for each level. Compared with bilinear parent terrain, interior elevation residual RMS was 11.62 m at 40 m, 3.57 m at 20 m, and 1.11 m at 10 m. These differences show added source relief; they do not establish absolute accuracy.
- Each finer outer edge is fitted to its parent height over a 100 m transition, and image color blends inward separately. The full region, Badwater corridor, oblique corridor, and canyon patch were rendered from temporary review cameras without saving cameras into the `.blend`. The final canyon render no longer shows the initial rectangular color seam.

Blender background mode reopened the final `.blend` and rendered the four previews successfully. The expanded file was also opened in the single Blender GUI window for regional review. Free GUI navigation has not been timed. If orbit or pan feels slow, compare the collection visibility first; no streaming loader is installed.

## Rebuild entry points

The versioned bounds are in [`region_full_tiles.json`](../../../../Docs/Tools/Art/Blender/death_valley/region_full_tiles.json), [`badwater_corridor.json`](../../../../Docs/Tools/Art/Blender/death_valley/badwater_corridor.json), [`badwater_transition_pilot.json`](../../../../Docs/Tools/Art/Blender/death_valley/badwater_transition_pilot.json), and [`badwater_canyon_patch.json`](../../../../Docs/Tools/Art/Blender/death_valley/badwater_canyon_patch.json). `prepare_region.py` and `prepare_corridor.py` acquire each DEM/NAIP grid; `rasterize_geology.py` supplies the categorical overlay; `fetch_landsat_reference.py` and `make_imagery_composite.py` fill image coverage; `match_detail_color.py` corrects the patch's broad color. `measure_detail.py` records added relief and raw edge mismatch. `build_explore_scene.py` reads the four manifests and color images into one new scene; `render_explore_previews.py` generates read-only review images. The [plan](../../../../Docs/Docs/Agents/Blender/DEATH_VALLEY_EXPANSION_PLAN.md) lists the sequence and proof gates.

These are geographic study textures. They retain aerial shadows, roads and capture differences, and are not accepted game materials. Rock faces, stones, displacement, clutter, collision, gameplay, and Unity import were not added.

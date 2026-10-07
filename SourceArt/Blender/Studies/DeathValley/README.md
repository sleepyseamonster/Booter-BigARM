# Death Valley terrain exploration and future Unity handoff

**State (2026-10-05):** The [expanded Blender viewer](EXPANDED_VIEWER.md) now covers full regional tiles and a provisional Badwater detail corridor. It is geographic study terrain, not a finished Unity game world or accepted production art.

The [Badwater four-slice study](BADWATER_GAME_SLICE.md) is a separate 4.096 × 4.096 km Blender scene with measured 1 m elevation in a 512 m focus, 2 m surrounding chunks, provisional surface materials and candidate 16-bit height exports. A [separate playable Unity terrain scene](UNITY_BADWATER_SCENE.md) now contains the same 256 named terrain tiles, geographic color reference, Booter, Legger, the production camera, HUD, and lighting. Runtime terrain streaming and broader gameplay-world integration remain to be built.

## What is here

| Artifact | Use | Limit |
| --- | --- | --- |
| `BadwaterGameSlice.blend` | **Open this to inspect the four adjoining Badwater terrain slices.** 256 aligned 256 m chunks, including four 1 m focus chunks, with editable art and geographic material slots. The saved viewport shows the full area; Numpad 0 enters the basin-eye camera. | Source heights and seams are verified; materials are provisional. This is not yet a streamed or playable Unity world. See the [build record](BADWATER_GAME_SLICE.md). |
| `DeathValleyExploreExpanded.blend` | **Open this for the new terrain review.** One free Blender viewport contains the full 192 × 224 km region, a 48 × 40 km Badwater corridor at 40 m, an 8 × 8 km 20 m basin-edge pilot, and a 2 × 2 km 10 m canyon margin patch. It opens over Badwater. | The candidate boundary needs visual review. All meshes are loaded together; there is no automatic chunk streaming. No game cameras, assets, or rock geometry. See the [build record](EXPANDED_VIEWER.md). |
| `DeathValleyExplore.blend` | **Open this for free exploration.** It contains the whole 176.2 × 215.4 km regional terrain at 200 m spacing, split into 42 Blender mesh objects. It opens in the ordinary 3D viewport over Racetrack Playa, with natural-color Landsat surface reference. | All 42 objects are loaded; this scene does not stream chunks. The 200 m DEM shows the playa basin and surrounding topography, not individual stones or trails. No close tiles, game assets, or cameras are present. |
| `DeathValleyTerrainMaster.blend` | Earlier multi-resolution technique study with the regional mesh, Mosaic 10 m terrain and 1 m patch | Preserved for later detail decisions; it is not the current exploration scene. |
| `DeathValleyRegionalStudy.blend`, `DeathValleyMosaicDetail.blend` | Earlier separate studies and the experimental rock/scatter setup | Kept for provenance and a later rock pass; these are not the current review scene. |
| `rock_exports/*.fbx` | Individual centered rock prototypes with UVs, contact pivots and metre scale | Procedural Blender shaders do not transfer; assign reviewed Unity materials after import |
| `rock_maps/*` | Five families with 512 px base color, OpenGL +Y normal, roughness and height starter maps | Procedural art studies, not scanned rock or baked high-poly detail; test normal orientation in the Unity material path |
| `scan_materials/rocks_ground_09/*` | 1K scanned rocky ground diffuse, OpenGL normal, roughness and displacement from Poly Haven; source URL and provider MD5 are in its manifest | The displacement map is retained but not connected. The material requires game art review and would need a Unity shader setup. |
| `previews/*` | Fixed view regional, 10 m Mosaic, 1 m elevated, and 1 m ground render evidence | Artistic quality remains provisional |

The large raw and prepared GIS data live in `/Users/worldbuilder/Desktop/Death Valley Terrain Data/`. They stay outside Unity imports and Git. The complete acquisition responses and SHA-256 hashes are in that folder's `manifest.json`, `prepared/detail/manifest.json`, `prepared/geology_manifest.json`, and `prepared/rock_maps/manifest.json`. Keep that folder to rebuild this particular source snapshot. Service output can change on a later fetch, so compare hashes rather than assuming a future download is byte-identical.

## Geographic scope and provenance

The [NPS park boundary layer](https://services1.arcgis.com/fBc8EJBxQRMcHlei/ArcGIS/rest/services/National_Park_Service_Boundaries/FeatureServer/0) returned `DEVA` bounds `409313,3938827,565540,4134109` in EPSG:26911. The study bounds are `[399400,3928800,575600,4144200]`, about 10 km beyond the park extent. This covers the park bounding rectangle and flanking terrain, not the future Red Rock Canyon, Valley of Fire, or Grand Canyon extensions.

- Elevation: [USGS 3DEP ImageServer](https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer), sampled at 200 m for the region and 10 m for Mosaic. The 1 m patch uses TNM product `USGS_1M_11_x48y405_CA_FEMAR9Southeast_D24` from the [TNM API](https://tnmaccess.nationalmap.gov/api/v1/products). Elevations remain in metres; the [3DEP metadata](https://data.usgs.gov/datacatalog/data/USGS%3A3a81321b-c153-416f-98b7-cc8e5f0e17c3) describes data sources and datum, but the dynamic service can blend component products. Check each product's vertical metadata before survey-precision use.
- Imagery: [USGS NAIP](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer) covers 88.8% of the broad rectangle and nearly all of Mosaic. The master uses the [Landsat natural-color service](https://landsat.arcgis.com/arcgis/rest/services/Landsat/MS/ImageServer) for the entire regional surface so missing NAIP areas and capture seams do not form a large discontinuity. Mosaic retains higher resolution NAIP. This is geographically aligned reference color, not a finished ground material. The external data folder retains the Landsat source snapshot and composite experiment with hashes.
- Geology: [USGS MF-2381](https://pubs.usgs.gov/mf/2002/mf-2381/) categorical polygons, source NAD27 UTM 11 reprojected to NAD83 UTM 11. The published map is 1:250,000 scale and cannot specify metre-scale rock contacts. It covers 67.5% of the broader study; unit code `0` is unknown. Mosaic coverage is complete.
- Mosaic setting: [NPS Mosaic Canyon description](https://www.nps.gov/deva/planyourvisit/mosaic-canyon.htm) informs the marble/dolomite and breccia visual direction. The first 1 m patch is a technique study within the 2 km tile.

No Google Maps/Earth imagery, mesh, or scraped tiles are used. USGS elevation/NAIP data, Landsat natural-color reference, and CC0 surface maps avoid a paid-tool dependency. The Landsat image service credits Esri, USGS, and NASA; review its exact use terms before shipping any image-derived texture. Before shipping a game asset, check the exact source item's metadata and retain attribution in the project record.

The close ground material is [Poly Haven Rocks Ground 09](https://polyhaven.com/a/rocks_ground_09), a CC0 asset. Its file metadata came through the [Poly Haven public API](https://github.com/Poly-Haven/Public-API); the API terms call for a small **Powered by Poly Haven** credit when building on that live API. The `.blend` packs the image files, and portable copies remain in `scan_materials/rocks_ground_09/`.

## Review in Blender first

Open `DeathValleyExploreExpanded.blend` for the new review. It starts in **User Perspective** over Badwater Basin with a yellow candidate detail boundary. Orbit with middle mouse, pan with Shift + middle mouse, and zoom with the wheel. Press **Home** while the pointer is over the viewport to frame the entire regional terrain. To jump to Badwater, Racetrack, or the screenshot target, select its marker in the Outliner and press Numpad **Period** (Frame Selected). The viewport clip distance is 500 km. Four geographic color images are packed into the file; the scene has no camera object. The older `DeathValleyExplore.blend` remains available and unchanged.

This phase stays in Blender until the user has seen and assessed its scale and art direction. Do not move these studies into Unity merely because exports exist.

## Terrain batching for the full landscape

The expanded scene fills the partial east and south tiles with real source data. The 200 m regional grid is now 192 × 224 km, split into 42 complete source tiles; it remains one loaded scene. The selective Badwater layers use 40, 20, and 10 m terrain without raising the whole region to that resolution. Underlying broad faces are hidden where fine terrain appears, avoiding overlapping surfaces. A loader is only worth adding if the user finds the measured viewport cost too high. The preserved Mosaic study remains a separate earlier technique test.

## Rebuild

The scripts are in [`tools/death_valley/`](../../../../Docs/Tools/Art/Blender/death_valley) and the Python requirements are listed there. They ran in a local Python 3 virtual environment with rasterio 1.5.2, pyproj 3.8.0, numpy 2.5.3, Pillow 12.3.0 and pyshp 2.3.1; Blender is 5.2.2 LTS. Adjust the local Blender executable path as needed. From the repository root:

```sh
python Tools/Art/Blender/death_valley/prepare_region.py --config Tools/Art/Blender/death_valley/region.json --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Tools/Art/Blender/death_valley/rasterize_geology.py --zip '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_MF2381_geology_nobase.zip' --manifest '/Users/worldbuilder/Desktop/Death Valley Terrain Data/manifest.json' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Tools/Art/Blender/death_valley/prepare_detail.py --source '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_1M_11_x48y405_CA_FEMAR9Southeast_D24.tif' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/detail'
python Tools/Art/Blender/death_valley/make_rock_maps.py --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/rock_maps'
```

Run `build_explore_scene.py` in Blender background mode with `--manifest`, `--overview-color '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/overview/landsat_natural_fallback.png'`, and `--output` to rebuild the **older** free-navigation scene. For the expanded viewer, follow the four manifest and color paths in its [build record](EXPANDED_VIEWER.md) and pass the optional corridor, pilot, and patch arguments to the same builder. `verify_pipeline.py --terrain-only --color` checks each new geographic grid and color hash. The builder packs the derived images into the saved `.blend`.

For the scanned ground, pass `--scan-manifest SourceArt/Blender/Studies/DeathValley/scan_materials/rocks_ground_09/manifest.json` to `build_detail_study.py`. `fetch_polyhaven_material.py` can recreate those files from the saved API file listing under the external raw data folder; it validates the provider MD5 values.

## Art grammar to transfer, version 0.1

1. **Large forms:** Basin floor and opposing ranges should be read from elevation and broad geological province. Preserve mountain silhouettes and long fans; a 200 m grid is sufficient only for distant shape.
2. **Intermediate forms:** The 10 m tile contributes canyon entrances, wash shapes, ridge rhythms, and approach routes. The 1 m patch contributes local ground breaks. Do not use either as the full procedural world's permanent authored layout.
3. **Rock families:** Sedimentary contacts favor slabs and pale fractured faces; basement regions favor angular dark blocks; talus is placed below steep terrain; wash beds reduce large slabs and can retain small debris. These are artistic tendencies, not a literal local geology simulator. Five prototype meshes cost 38–76 triangles each before LOD or collision policy.
4. **Surface:** Use regional categorical geology for macro palette, terrain slope/position for landform blending, and tiling maps or shader detail for close frequency. Aerial color is reference only because baked shadows and capture season are unsuitable for game lighting.
5. **Spacing and routes:** Keep traversable washes and basin pockets comparatively open. Concentrate rock mass at cliff bases and selective focal clusters. Hand-authored landmarks and access constraints should anchor the generator, with meaningful alternate approaches for Booter and BigARM.
6. **Variation:** A second composition can alter ridge orientation, canyon spacing, fan size, geology weights, rock scale and density, routes, and overlook placement while preserving the causal relationship from rock source to talus and wash deposition. Version this rule catalog separately from raw geographic cache data.

## Unity integration contract

The owning **World Creator plan/query path** must choose geology, broad landforms, route truth and stable feature IDs from world seed, absolute position, and independent generator versions. Chunks may instantiate near/mid/far representations and unload them, but may not invent different rock units or route affordances. Store player alterations as persisted deltas, not generated mesh instances. Authored landmarks may constrain or override the grammar through the established plan authority. The geographic UTM origin is study metadata only; use precision-safe game coordinates.

After Blender visual review, Babineaux's Unity batch should import a small selected rock/map set and a bounded, non-canon proof influence, after reviewing the current dirty Unity changes and the production camera/runtime profile. It must verify FBX axes, metre scale, pivot, UVs, normal convention, material appearance, collider cost, deterministic placement, chunk edge agreement, unload/reload identity and deltas, Booter/BigARM routes, and Development Player frame/memory budgets. The full regional Blender mesh is not a Unity import candidate. Hands-on traversal and final visual acceptance belong to the user. No Unity proof has been run from this study.

## Measured checks and current visual assessment

- Expanded viewer: 42 full 32 km source tiles before detail cutouts; 76 mesh objects and 4,754,400 evaluated terrain triangles after layered replacement and omission of one fully covered coarse object; four packed color images; no camera. The four read-only preview renders show the full map, Badwater corridor, oblique terrain, and canyon patch. GUI pan/orbit cost in the new file still needs user visual review.
- Exploration scene: 42 regional tiles and 1,897,674 terrain triangles, all visible in a single free Blender viewport. The saved view shows Racetrack Playa; **Home** was tested to frame the entire terrain. The earlier master has two stitched Mosaic tiles and four 1 m patch tiles, with 2,480,454 evaluated terrain triangles; those details are not loaded in the exploration scene.
- Geometry Nodes preview: 140 slabs, 100 cliff blocks, 240 dark boulders, 450 talus shards and 700 pebbles, 1,630 total, seeded from local coordinates and filtered by slope, geology, talus/wash hints. This count is not a runtime density recommendation.
- Blender's read-only `inspect_blend.py` found UVs and closed rock meshes with no nonmanifold edges or loose vertices. The terrain has intentional open boundary edges; Geometry Nodes point carriers intentionally have loose vertices.
- The pipeline verifier passed for the regional and Mosaic grids, categorical geology, 1 m patch, five FBX exports and 20 generated maps. The 10 m Mosaic sample near the NPS trailhead differed by roughly 0.32 m from a separate USGS EPQS point response. This is a local check, not a blanket accuracy guarantee.
- The map view shows the complete regional rectangle and major range/basin structure. The scanned close ground and irregular/rotated rock instances improved the 1 m view, but many rock faces remain visually simple and the macro composition remains a blockout. Treat it as a technique and scale proof, not finished wasteland art.

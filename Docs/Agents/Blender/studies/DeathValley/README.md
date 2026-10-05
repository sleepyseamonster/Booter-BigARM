# Death Valley terrain master and future Unity handoff

**State (2026-10-05):** Reproducible geographic and Blender prototype. It is not a finished Unity game world or accepted production art.

## What is here

| Artifact | Use | Limit |
| --- | --- | --- |
| `DeathValleyTerrainMaster.blend` | **Open this single project file for terrain review.** It contains the full regional 200 m terrain, 10 m Mosaic Canyon terrain, and a 500 × 500 m 1 m patch. Natural-color Landsat covers the full regional surface; higher resolution USGS NAIP colors Mosaic. The Mosaic surfaces add a subtle CC0 scanned ground normal and roughness. Three camera markers cover the map, elevated Mosaic, and 1 m patch. | Satellite and aerial color contain baked lighting and roads. They are geographic study textures, not finished game materials. Do not import the entire overview into Unity as one mesh. |
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

Open `DeathValleyTerrainMaster.blend`. Frame **1** selects the regional map camera, frame **2** the 10 m Mosaic elevated camera, and frame **3** the 1 m patch camera. Press Numpad **0** to enter the camera if Blender opens in a free view. Use Material Preview or Rendered shading to see the terrain textures. The regional, 10 m, and 1 m surfaces are separate named collections, but they share one scene and one geographic origin. The 1 m patch is terrain only; rocks and scatter are deferred. The imagery and scanned ground maps are packed into the file.

This phase stays in Blender until the user has seen and assessed its scale and art direction. Do not move these studies into Unity merely because exports exist.

## Terrain batching for the full landscape

The master scene already covers the park bounding rectangle and surrounding ranges at 200 m spacing. Add detail **by area of interest**, not by raising the entire park to 1 m. Each new batch should use an aligned metric grid, keep a small border that blends to its parent grid, and enter the master file as a named collection with a review camera. Mosaic demonstrates the first 2 km terrain batch at 10 m and a selected 500 m patch at 1 m. This keeps the valley and mountains navigable in Blender while allowing close terrain to be refined one place at a time. The next batches should be chosen after reviewing the current map and camera view, using the places the game needs rather than a uniform tiling of all 38,000 km².

## Rebuild

The scripts are in [`tools/death_valley/`](../../tools/death_valley/) and the Python requirements are listed there. They ran in a local Python 3 virtual environment with rasterio 1.5.2, pyproj 3.8.0, numpy 2.5.3, Pillow 12.3.0 and pyshp 2.3.1; Blender is 5.2.2 LTS. Adjust the local Blender executable path as needed. From the repository root:

```sh
python Docs/Agents/Blender/tools/death_valley/prepare_region.py --config Docs/Agents/Blender/tools/death_valley/region.json --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Docs/Agents/Blender/tools/death_valley/rasterize_geology.py --zip '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_MF2381_geology_nobase.zip' --manifest '/Users/worldbuilder/Desktop/Death Valley Terrain Data/manifest.json' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Docs/Agents/Blender/tools/death_valley/prepare_detail.py --source '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_1M_11_x48y405_CA_FEMAR9Southeast_D24.tif' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/detail'
python Docs/Agents/Blender/tools/death_valley/make_rock_maps.py --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/rock_maps'
```

Then run `build_region_study.py` in Blender background mode with `--manifest`, `--detail-manifest`, `--scan-manifest`, `--overview-color '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/overview/landsat_natural_fallback.png'`, and `--output` to rebuild the terrain master. The optional detail and scan arguments can be omitted for a lighter regional study. `verify_pipeline.py` checks the underlying arrays, geology codes, source hashes, FBX files, and maps. `build_region_study.py` packs its imagery and scan maps into the saved `.blend`. The separate `build_detail_study.py` remains available for the later rock pass.

For the scanned ground, pass `--scan-manifest Docs/Agents/Blender/studies/DeathValley/scan_materials/rocks_ground_09/manifest.json` to `build_detail_study.py`. `fetch_polyhaven_material.py` can recreate those files from the saved API file listing under the external raw data folder; it validates the provider MD5 values.

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

- Terrain master: 42 overview tiles, two stitched Mosaic tiles, and four 1 m patch tiles. The saved file has 2,480,454 evaluated terrain triangles, UVs on every tile, and no loose vertices. The 1 m tile edges share source samples from the same 501 × 501 raster. The earlier separate detail study remains available for the later rock pass.
- Geometry Nodes preview: 140 slabs, 100 cliff blocks, 240 dark boulders, 450 talus shards and 700 pebbles, 1,630 total, seeded from local coordinates and filtered by slope, geology, talus/wash hints. This count is not a runtime density recommendation.
- Blender's read-only `inspect_blend.py` found UVs and closed rock meshes with no nonmanifold edges or loose vertices. The terrain has intentional open boundary edges; Geometry Nodes point carriers intentionally have loose vertices.
- The pipeline verifier passed for the regional and Mosaic grids, categorical geology, 1 m patch, five FBX exports and 20 generated maps. The 10 m Mosaic sample near the NPS trailhead differed by roughly 0.32 m from a separate USGS EPQS point response. This is a local check, not a blanket accuracy guarantee.
- The map view shows the complete regional rectangle and major range/basin structure. The scanned close ground and irregular/rotated rock instances improved the 1 m view, but many rock faces remain visually simple and the macro composition remains a blockout. Treat it as a technique and scale proof, not finished wasteland art.

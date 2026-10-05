# Death Valley regional terrain study and Unity handoff

**State (2026-10-05):** Reproducible geographic and Blender prototype. It is not a finished Unity game world or accepted production art.

## What is here

| Artifact | Use | Limit |
| --- | --- | --- |
| `DeathValleyRegionalStudy.blend` | 200 m Death Valley regional relief and separate 10 m Mosaic Canyon tile; fixed map and elevated cameras; packed NAIP reference imagery in alternate material slots | Visual research mesh only; do not import the 1.90 million-triangle overview into Unity as one mesh |
| `DeathValleyMosaicDetail.blend` | 500 × 500 m 1 m DEM patch, five low-cost rock prototypes, and Geometry Nodes placement preview | The Blender node graph is not Unity placement authority |
| `rock_exports/*.fbx` | Individual centered rock prototypes with UVs, contact pivots and metre scale | Procedural Blender shaders do not transfer; assign reviewed Unity materials after import |
| `rock_maps/*` | Five families with 512 px base color, OpenGL +Y normal, roughness and height starter maps | Procedural art studies, not scanned rock or baked high-poly detail; test normal orientation in the Unity material path |
| `previews/*` | Fixed view regional, 10 m Mosaic, 1 m elevated, and 1 m ground render evidence | Artistic quality remains provisional |

The large raw and prepared GIS data live in `/Users/worldbuilder/Desktop/Death Valley Terrain Data/`. They stay outside Unity imports and Git. The complete acquisition responses and SHA-256 hashes are in that folder's `manifest.json`, `prepared/detail/manifest.json`, `prepared/geology_manifest.json`, and `prepared/rock_maps/manifest.json`. Keep that folder to rebuild this particular source snapshot. Service output can change on a later fetch, so compare hashes rather than assuming a future download is byte-identical.

## Geographic scope and provenance

The [NPS park boundary layer](https://services1.arcgis.com/fBc8EJBxQRMcHlei/ArcGIS/rest/services/National_Park_Service_Boundaries/FeatureServer/0) returned `DEVA` bounds `409313,3938827,565540,4134109` in EPSG:26911. The study bounds are `[399400,3928800,575600,4144200]`, about 10 km beyond the park extent. This covers the park bounding rectangle and flanking terrain, not the future Red Rock Canyon, Valley of Fire, or Grand Canyon extensions.

- Elevation: [USGS 3DEP ImageServer](https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer), sampled at 200 m for the region and 10 m for Mosaic. The 1 m patch uses TNM product `USGS_1M_11_x48y405_CA_FEMAR9Southeast_D24` from the [TNM API](https://tnmaccess.nationalmap.gov/api/v1/products). Elevations remain in metres; the [3DEP metadata](https://data.usgs.gov/datacatalog/data/USGS%3A3a81321b-c153-416f-98b7-cc8e5f0e17c3) describes data sources and datum, but the dynamic service can blend component products. Check each product's vertical metadata before survey-precision use.
- Imagery: [USGS NAIP imagery only](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer), used for reference and palette, not as the finished ground material. Valid alpha coverage is 88.8% on the broad study and nearly 100% at Mosaic. Missing imagery receives a neutral relief color in previews.
- Geology: [USGS MF-2381](https://pubs.usgs.gov/mf/2002/mf-2381/) categorical polygons, source NAD27 UTM 11 reprojected to NAD83 UTM 11. The published map is 1:250,000 scale and cannot specify metre-scale rock contacts. It covers 67.5% of the broader study; unit code `0` is unknown. Mosaic coverage is complete.
- Mosaic setting: [NPS Mosaic Canyon description](https://www.nps.gov/deva/planyourvisit/mosaic-canyon.htm) informs the marble/dolomite and breccia visual direction. The first 1 m patch is a technique study within the 2 km tile.

No Google Maps/Earth imagery, mesh, or scraped tiles are used. USGS public-domain source data and our procedural maps avoid a paid-tool dependency. Before shipping a game asset, check the exact source item's metadata and retain attribution in the project record.

## Rebuild

The scripts are in [`tools/death_valley/`](../../tools/death_valley/) and the Python requirements are listed there. They ran in a local Python 3 virtual environment with rasterio 1.5.2, pyproj 3.8.0, numpy 2.5.3, Pillow 12.3.0 and pyshp 2.3.1; Blender is 5.2.2 LTS. Adjust the local Blender executable path as needed. From the repository root:

```sh
python Docs/Agents/Blender/tools/death_valley/prepare_region.py --config Docs/Agents/Blender/tools/death_valley/region.json --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Docs/Agents/Blender/tools/death_valley/rasterize_geology.py --zip '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_MF2381_geology_nobase.zip' --manifest '/Users/worldbuilder/Desktop/Death Valley Terrain Data/manifest.json' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data'
python Docs/Agents/Blender/tools/death_valley/prepare_detail.py --source '/Users/worldbuilder/Desktop/Death Valley Terrain Data/raw/USGS_1M_11_x48y405_CA_FEMAR9Southeast_D24.tif' --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/detail'
python Docs/Agents/Blender/tools/death_valley/make_rock_maps.py --output '/Users/worldbuilder/Desktop/Death Valley Terrain Data/prepared/rock_maps'
```

Then run `build_region_study.py` and `build_detail_study.py` in Blender background mode with the manifest and output arguments shown in each script. `verify_pipeline.py` checks the resulting arrays, geology codes, source hashes, FBX files, and maps. `build_region_study.py` packs its two image references into the saved `.blend`. `build_detail_study.py` saves the editable scene before exporting the centered FBX prototypes.

## Art grammar to transfer, version 0.1

1. **Large forms:** Basin floor and opposing ranges should be read from elevation and broad geological province. Preserve mountain silhouettes and long fans; a 200 m grid is sufficient only for distant shape.
2. **Intermediate forms:** The 10 m tile contributes canyon entrances, wash shapes, ridge rhythms, and approach routes. The 1 m patch contributes local ground breaks. Do not use either as the full procedural world's permanent authored layout.
3. **Rock families:** Sedimentary contacts favor slabs and pale fractured faces; basement regions favor angular dark blocks; talus is placed below steep terrain; wash beds reduce large slabs and can retain small debris. These are artistic tendencies, not a literal local geology simulator. Five prototype meshes cost 46–76 triangles each before LOD or collision policy.
4. **Surface:** Use regional categorical geology for macro palette, terrain slope/position for landform blending, and tiling maps or shader detail for close frequency. Aerial color is reference only because baked shadows and capture season are unsuitable for game lighting.
5. **Spacing and routes:** Keep traversable washes and basin pockets comparatively open. Concentrate rock mass at cliff bases and selective focal clusters. Hand-authored landmarks and access constraints should anchor the generator, with meaningful alternate approaches for Booter and BigARM.
6. **Variation:** A second composition can alter ridge orientation, canyon spacing, fan size, geology weights, rock scale and density, routes, and overlook placement while preserving the causal relationship from rock source to talus and wash deposition. Version this rule catalog separately from raw geographic cache data.

## Unity integration contract

The owning **World Creator plan/query path** must choose geology, broad landforms, route truth and stable feature IDs from world seed, absolute position, and independent generator versions. Chunks may instantiate near/mid/far representations and unload them, but may not invent different rock units or route affordances. Store player alterations as persisted deltas, not generated mesh instances. Authored landmarks may constrain or override the grammar through the established plan authority. The geographic UTM origin is study metadata only; use precision-safe game coordinates.

Babineaux's next batch should import a small selected rock/map set and a bounded, non-canon proof influence, after reviewing the current dirty Unity changes and the production camera/runtime profile. It must verify FBX axes, metre scale, pivot, UVs, normal convention, material appearance, collider cost, deterministic placement, chunk edge agreement, unload/reload identity and deltas, Booter/BigARM routes, and Development Player frame/memory budgets. The full regional Blender mesh is not a Unity import candidate. Hands-on traversal and final visual acceptance belong to the user. No Unity proof has been run from this study.

## Measured checks and current visual assessment

- Regional Blender study: 42 overview tiles, 1,897,674 overview triangles; one Mosaic tile, 80,000 triangles. Mosaic 1 m study: four tiles, 500,000 source terrain triangles. The four 1 m tile edges use shared vertices from the same 501 × 501 raster.
- Geometry Nodes preview: 140 slabs, 100 cliff blocks, 240 dark boulders, 450 talus shards and 700 pebbles, 1,630 total, seeded from local coordinates and filtered by slope, geology, talus/wash hints. This count is not a runtime density recommendation.
- Blender's read-only `inspect_blend.py` found UVs and closed rock meshes with no nonmanifold edges or loose vertices. The terrain has intentional open boundary edges; Geometry Nodes point carriers intentionally have loose vertices.
- The pipeline verifier passed for the regional and Mosaic grids, categorical geology, 1 m patch, five FBX exports and 20 generated maps. The 10 m Mosaic sample near the NPS trailhead differed by roughly 0.32 m from a separate USGS EPQS point response. This is a local check, not a blanket accuracy guarantee.
- The map view shows the complete regional rectangle and major range/basin structure. The 1 m ground render exposes prototype limits: repetitive simple rock silhouettes, little contact shadow, and a largely flat dark surface. Treat it as a technique and scale proof, not finished wasteland art.

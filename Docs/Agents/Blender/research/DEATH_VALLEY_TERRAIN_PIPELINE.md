# Death Valley terrain and rock pipeline — research proposal

**Date:** 2026-10-05

**Status:** Research and proposed workflow. No geographic data, Blender scene, script, or Unity asset has been produced by this document. The user owns the final area, visual direction, and any paid-tool decision.

## Goal and project boundary

Use Death Valley and its surrounding ranges as a *geographic and geological base* for a convincing wasteland, then transform it to serve the game. Exact one-to-one reconstruction is not the goal. Start with one playable-scale area and a wider visual horizon. Red Rock Canyon, Valley of Fire, and the Grand Canyon are later regional references or separate provinces, not one initial Blender mesh.

Blender owns the source study, reusable rocks, material prototypes, masks, and review renders. Unity's [World Creator architecture](../../../Design/DeferredGeneration/WORLD_CREATOR_ARCHITECTURE_PLAN.md) owns deterministic absolute-coordinate queries, chunk realization, near/middle/far representations, authored constraints, stable object IDs, and saved runtime changes. A finite Death Valley dataset can inform its rules and supply samples or caches; it does not replace the effectively unbounded world model.

The existing [badlands study](../studies/HANDOFF_2026-10-01.md) is paused and dirty. Do not overwrite or regenerate it for this work. Its latest unimplemented feedback—fewer triangles, larger planar fracture faces, less clay-like rock—should guide a separate candidate rock kit.

## Recommended tool stack

| Job | Default | Optional / reason |
| --- | --- | --- |
| Elevation and orthophoto | [USGS 3DEP DEM](https://data.usgs.gov/datacatalog/data/USGS%3A3a81321b-c153-416f-98b7-cc8e5f0e17c3) and [USGS NAIP](https://www.usgs.gov/centers/eros/science/usgs-eros-archive-aerial-photography-national-agriculture-imagery-program-naip), both public-domain US government sources | [USGS 1 m DEM](https://www.usgs.gov/faqs/what-coverage-3d-elevation-program-3dep-dems) where local coverage exists; check before choosing a hero site |
| Geologic regions | [USGS Death Valley geologic map GIS files](https://geo-nsdi.er.usgs.gov/metadata/map-mf/2381/a/metadata.html) | Hand-painted corrections where map scale is too coarse for game art |
| Coordinate, raster, and vector preparation | [QGIS](https://docs.qgis.org/3.44/en/docs/user_manual/processing_algs/gdal/index.html) with [GDAL](https://gdal.org/en/stable/programs/gdalwarp.html) | BlenderGIS can help browse a location, but QGIS/GDAL should create reproducible aligned source rasters |
| Visual authoring | Blender terrain tiles, material nodes, Geometry Nodes, high/low rock meshes and baking | [Poly Haven CC0](https://polyhaven.com/license) rock/material sources; original photos and photogrammetry |
| Large-scale erosion or voxel cliffs | Begin with Blender and GIS; add a dedicated terrain tool only if a tested feature warrants it | [World Machine Dragontail Peak](https://www.world-machine.com/version/dragontail) has voxel/heightfield workflows and macOS support; evaluate license before production use |
| Unity handoff | Raster height/masks, a versioned material/rock catalog, optimized meshes and texture maps | A few authored landmark transforms; Babineaux owns actual Unity import and runtime validation |

**Cost and rights.** Blender, QGIS/GDAL, USGS data, and CC0 assets form the low-cost baseline. [World Machine Basic is explicitly noncommercial and capped at 1025 × 1025 output](https://www.world-machine.com/download.php); its paid tiers differ in commercial and tiled export features. Its current [purchase page](https://www.world-machine.com/purchase.php?page=pro%3B124921) should be checked before budgeting. [Google's geo guidelines](https://about.google/brand-resource-center/products-and-services/geo-guidelines/) and [Maps Platform terms](https://cloud.google.com/maps-platform/terms) make captured Maps/Earth imagery or 3D tiles a poor source for reconstructing shippable terrain. Use Google views for visual study only within their permitted use, not as export geometry or extracted textures. Record the source and license of every input asset.

## Geographic extent and resolution

1. **Scout area:** 40–60 km across, including the valley floor and an adjacent range or two. Select the exact UTM bounding box after visual inspection. The broad terrain is for composition and horizon silhouette.
2. **Hero area:** roughly 2 × 2 km in a chosen transition from playa/alluvial fan to rocky mountain edge. This is a proposed starting size, not an approved playable-map boundary.
3. **Detail sites:** a few 100–500 m patches for rock faces, trails, washes, and debris. Their close-range surface comes from authored geometry and materials as much as DEM sampling.

The 3DEP contiguous-US 10 m DEM is a practical baseline. A 50 × 50 km grid at 10 m contains about **25 million cells**; a regular terrain mesh would approach **50 million triangles** before rocks. A 50 m overview of the same area is about **one million cells**. A 2 × 2 km tile at 10 m is about **40,000 cells**. These are order-of-magnitude comparisons, not measured Blender or Unity budgets. Use several levels of detail, not one mesh. A 1 m DEM, if available, belongs on a small detail site.

USGS 3DEP elevation is in metres and commonly specifies NAVD88 vertical datum while its 10 m products are distributed in geographic NAD83 coordinates; inspect the chosen download's metadata. Death Valley is in UTM Zone 11 North, so **EPSG:26911 (NAD83 / UTM 11N)** is a proposed metric working CRS. Reproject all layers to one grid. Store a fixed projected origin in a manifest and subtract it when building Blender meshes to avoid huge floating-point scene coordinates. Preserve the original horizontal and vertical datum facts. Do not normalize each tile independently or alter vertical scale without a documented transform: both would damage height continuity and source traceability.

## Reproducible build stages

### 1. Acquire and audit source data

Download DEM tiles, NAIP orthophotos, and geologic polygons from the official USGS sources. For each file record URL, dataset identifier/date, license, original CRS, vertical datum, pixel size, NoData value, chosen extent, and SHA-256. Inspect hillshade and color mosaics in QGIS before writing Blender code. Aerial imagery supplies color and broad surface cues; it is not close-up rock geometry and can contain lighting, vegetation, and seasonal color.

### 2. Prepare one aligned GIS grid

Use [`gdalbuildvrt`](https://gdal.org/en/stable/programs/gdalbuildvrt.html) to mosaic source tiles without immediately duplicating all pixels. Use [`gdalwarp`](https://gdal.org/en/stable/programs/gdalwarp.html) to project elevation to the metric working CRS with an explicit extent, target resolution, aligned pixels (`-tap`), NoData handling, and an appropriate elevation resampling method. Warp imagery separately on the **same georeferenced extent** at the desired image resolution. Rasterize geology to categorical masks without interpolating category IDs. Keep float elevation; do not use colorized hillshade as height.

Derive a small set of authoring signals: slope, aspect, and ruggedness from [QGIS terrain analysis](https://docs.qgis.org/3.44/en/docs/user_manual/processing_algs/qgis/rasterterrainanalysis.html); geologic unit and aerial-color regions; and hand-checked wash, fan, playa, and cliff masks. Curvature or flow accumulation may help later, but the exact tool and thresholds need a tested GIS prototype. Create 50 m overview and 10 m hero products from the *same* base and pixel alignment. Shared tile edges must sample the same source heights. Spot-check known elevation points and inspect NoData seams.

### 3. Build a bounded Blender study

Import the overview as a lightweight terrain mesh and the hero area as separate tiles. Do not subdivide the broad region for photographic detail. Maintain collections for `SourceTerrain`, `HeroTerrain`, `RockLibrary`, `ScatterPreview`, `Landmarks`, and `ReviewCameras`. Record the geographic origin and source hashes as scene metadata. Use metres, consistent axes, unapplied source transforms where possible, and predictable contact pivots. Set cameras at the game's elevated view plus ground-level inspections to catch both silhouette and close surface problems.

Begin with an honest terrain base, then sculpt or add **bounded feature meshes** for cliffs, undercuts, ledges, road cuts, cave mouths, and silhouette-critical breaks that a heightfield cannot express. Keep the original DEM copy and a reversible authored delta layer. A terrain mesh with micro-displacement everywhere is an expensive way to produce details that belong in material maps or instanced rocks.

### 4. Build materials at three scales

- **Macro:** NAIP color and geology inform broad palette, light/dark region placement, and weathering masks. Correct baked shadows or color casts before using imagery as color reference; do not simply drape a photo and call it close-range albedo.
- **Middle:** Blend a compact set of material families—playa/salt, fine silt, alluvial gravel, stratified sedimentary rock, darker volcanic rock—by geology, slope, elevation, washes, and artist-painted corrections. These families are proposed art vocabulary, not an exact geologic legend.
- **Micro:** Tileable PBR base color, normal, roughness, and optional height/ambient-occlusion maps supply grains, chips, cracks, and dusty transitions. [Blender's box projection](https://docs.blender.org/manual/en/4.2/render/shader_nodes/textures/image.html) is useful for previewing steep faces with fewer UV seams. Blender node networks will not become Unity shaders automatically: bake or export texture maps and specify equivalent Unity blending rules. [Bump and displacement](https://docs.blender.org/manual/en/latest/render/shader_nodes/displacement/displacement.html) change different things; use mesh geometry when silhouette or collision must change, and normal/bump detail for smaller visual relief.

### 5. Author a rock and debris kit

Build a few *families* rather than a unique mesh for every stone: planar stratified slabs/ledges, fractured cliff pieces, angular volcanic boulders, scree/talus shards, and small pebbles. Establish believable dimensions and a common material response per family. Spend polygons on silhouette, contact, and major breaks. Keep broad faces planar with selective chipped edges and directional stratification. Avoid pervasive rounded noise or voxel smoothing, which made the current badlands study read like clay.

For high-detail hero sources, sculpt or photograph the rock, then make an efficient low mesh, UV it, and [bake](https://docs.blender.org/manual/en/5.0/render/cycles/baking.html) normals, ambient occlusion, and other needed maps. A tiling material plus decals/vertex color may cost less than a unique full texture set for each variation. Test LODs and per-instance material count at the game's camera distance. [Apple Object Capture](https://developer.apple.com/documentation/realitykit/realitykit-object-capture/) is an optional route for self-photographed rocks on a compatible Mac; captured geometry still needs cleanup, retopology, UVs, and provenance. Adobe Painter is optional for texture painting and [baking](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/baking/baking), not a baseline dependency.

### 6. Preview placement in Blender, realize it in Unity

Use [Geometry Nodes point distribution](https://docs.blender.org/manual/en/5.0/modeling/geometry_nodes/point/distribute_points_on_faces.html) and [instances](https://docs.blender.org/manual/en/latest/modeling/geometry_nodes/instances.html) for review: cliff foot produces talus; fan surfaces produce graded gravel; bedrock margins carry slabs; washes keep a distinct size/density profile; playa stays relatively sparse. Couple scatter masks to geology, slope, drainage, surface normals, and artist exclusions. Orient and partly bury assets so they contact the ground. Random yaw and size variation should be bounded by each rock family. Keep instances un-realized unless producing a small specific export.

Export rock assets and georeferenced **density/region/exclusion masks**, not millions of placed Blender objects. A few distinctive landmarks may export as explicit authored anchors. Unity can plan generated placements from seed, version, absolute coordinates, and feature ownership; give each mutable instance a stable ID. Chunk streaming rebuilds the visual objects, while saves hold only runtime deltas such as removal or modification. If the game later remixes geography, the same kit and masks can constrain new region plans instead of locking play to the real map.

## Small tools worth building

The installed background Blender was checked as **5.2.2 LTS** with NumPy 2.3.4. QGIS/GDAL command-line tools were **not found locally during this research**, so the first build action would be installing or locating them and verifying their versions. Avoid adding a dependency on a BlenderGIS release until it has been tested against this Blender version.

1. **`prepare_region.py` (external Python + GDAL):** accept a manifest and one explicit projected bounding box; make aligned DEM, overview, orthophoto, geology, and mask outputs. Validate CRS, pixel alignment, NoData, shared edges, source hashes, and elevation range. Write an output manifest; never silently rescale heights.
2. **`build_region_study.py` (Blender Python):** read prepared rasters/arrays and the manifest; build a fresh, named `.blend` with local-origin terrain tiles, overview, material placeholders, cameras, and source metadata. Make it idempotent to a new output path, not an in-place rewrite of the paused badlands study. A small custom importer is likely cheaper to maintain than depending on a BlenderGIS importer for the production build.
3. **One reusable Geometry Nodes scatter group:** named inputs for masks, density, seed, rock family, slope and scale limits. Save it inside the study and export its parameter schema for Unity rather than trying to translate the node graph automatically.
4. **`export_rock_kit.py` (Blender Python):** export only named approved meshes, UV/material maps, origin, dimensions, evaluated triangle counts, LOD grouping, and a machine-readable catalog. Run the existing [model-to-Unity checks](../sops/MODEL_TO_UNITY.md). Babineaux owns Unity import verification.

Build these incrementally after choosing the first bounded study region and obtaining real data. The **first coded vertical slice** should be one aligned DEM tile, one Blender terrain tile, one cliff/rock material, and a small scatter mask. That proves coordinate fidelity and visual value before a large automation investment.

## Verification and decision gates

- **GIS:** source/license manifest complete; all layers align; DEM metres, datum, range, NoData and adjoining edges checked; source elevation spot checks documented.
- **Blender:** inspect wide and game-camera views plus close rock views; compare terrain contours to source hillshade; count *evaluated* triangles and texture memory; review geology/material transitions and scatter contact. A script report alone does not establish rock realism.
- **Unity handoff:** Babineaux verifies orientation, scale, import materials, normal maps, collision, LOD, elevated-camera readability, chunk boundary behavior, and a representative density/performance slice when that integration is authorized. No gameplay smoke test is implied by this research.
- **Creative:** the user chooses the first site and decides how much of the real geography remains recognizable, the palette, and whether the dark volcanic rock direction applies beyond the existing badlands study.

**Recommended next decision:** pick a roughly 2 × 2 km Death Valley hero area at a valley-to-mountain transition, with a 40–60 km horizon around it. Then test one georeferenced tile end to end with the free stack. Keep broader Nevada/Arizona landmarks as later references until this slice looks convincing and meets source and runtime budgets.

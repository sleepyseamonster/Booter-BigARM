# Death Valley landscape build plan

**Status:** Proposed execution plan, 2026-10-05. No data download, tool installation, Blender build, or Unity integration has been performed for this plan.

## Outcome and stop condition

Build one convincing, reusable Death Valley-inspired landscape slice for the active Unity TopDown3D game. The first deliverable is a roughly **2 × 2 km valley-to-mountain hero area** in a separate Blender study, with a lower-detail **40–60 km contextual horizon**, rock and ground material families, and placement masks. Transfer a bounded set of approved assets and data to the deterministic Unity world only after Blender visual review.

The first implementation cycle is done when (1) the geographic data can be rebuilt from a documented source manifest, (2) the Blender study visibly reads as valley, fan, wash, bedrock, and mountain at both game and ground-level views, (3) its rock kit has measured geometry and texture costs, and (4) one representative Unity chunked slice has proven scale, appearance, seams, stable placement, unload/reload, and relevant performance. The larger Death Valley region and Nevada/Arizona landmarks are later decisions, not first-cycle completion criteria.

## Authority and boundaries

- **Creative authority:** The user selects the final site, desired geographic recognizability, geology simplification, palette, and which features become game canon. The site and 2 × 2 km size below are proposed starting parameters.
- **World rules:** [WORLD_BASIS.md](../../WORLD_BASIS.md) controls the active 3D game's barren ecology, ancient dry channels, perpetual twilight, and the Broken World's tone. Do not quietly introduce active rivers, vegetation, or ordinary Earth daylight into the game version.
- **Technical authority:** [World Creator architecture](../../WORLD_CREATOR_ARCHITECTURE_PLAN.md) and [world systems standard](../../WORLD_SYSTEMS_STANDARD.md) control deterministic identity, chunk streaming, authored constraints, near/middle/far representations, and persisted deltas. Real terrain is reference data and optional bounded cache, not a finite replacement for the effectively infinite world.
- **Blender lane:** [Blender research](./research/DEATH_VALLEY_TERRAIN_PIPELINE.md) supplies data and technique choices. The existing [badlands study](./studies/HANDOFF_2026-10-01.md) remains paused and dirty; create a separate Death Valley study. Source art and Blender tools are owned here; approved production art goes under `Assets/_Project/Art/`.
- **Unity lane:** Babineaux handles import, scene/prefab/editor work and Unity proof under Gottspan's integration order. No normal repository task should focus the user's Unity window.
- **Exclusions:** Google Maps/Earth-derived mesh or extracted textures, one giant full-resolution mesh, purchased tools as prerequisites, Unity gameplay smoke tests, and unrelated Unity/Blender cleanup.

## Sequence and acceptance gates

| Stage | Build work and concrete output | Gate to continue |
| --- | --- | --- |
| **0. Site and source preflight** | Compare 2–3 Death Valley candidate valley-to-range windows using official USGS elevation, imagery and geology. Check 1 m DEM availability, source dates, rights, and local QGIS/GDAL versions. Record the selected projected bounding box, source links, resolution, local origin, and storage estimate in a source manifest. | User chooses the site/art direction from map and shaded-relief previews. Data are actually available and licensed for the intended use. |
| **1. One aligned GIS tile** | Create `prepare_region.py` or a minimal reproducible GDAL command set. Produce a float 10 m hero DEM, 50 m context DEM, imagery reference, geology categories, slope/ruggedness, and initial wash/fan/cliff masks on one aligned metric grid. Keep raw data outside Unity imports; record hashes, CRS, vertical datum, NoData, transform and output dimensions. | Re-running produces matching grids; adjacent edges agree, NoData is handled, geologic categories do not interpolate, and sample elevations match source values within the documented resampling tolerance. |
| **2. Blender terrain study** | Create `build_region_study.py` for Blender 5.2.2, reading the prepared manifest. Write a *new* local-origin `.blend` with context and hero terrain collections, source metadata, fixed review cameras, and an untouched DEM base plus authored terrain deltas. Build one representative tile first, then the bounded hero area. | Terrain scale, orientation and silhouettes match source hillshade; no visible tile cracks; game-view and ground-view renders are reviewed. Record evaluated vertices/triangles, RAM and render time on this machine. |
| **3. Terrain forms and surface** | Add limited authored cliff/ledge meshes where the heightfield cannot represent vertical shape. Prototype macro palette from imagery, regional blends from geology/landforms, and tileable micro PBR material maps. Preserve a barren Broken World interpretation instead of directly draping Earth imagery. | At least one valley-floor-to-rock transition looks coherent from the elevated game camera and close view; material families and cliff silhouettes remain readable under a twilight lighting test. Geometry is reserved for silhouette/collision-relevant relief. |
| **4. Rock kit and debris** | Build a small kit: planar stratified slabs, fractured cliff pieces, angular dark boulders, talus, and low-cost pebble variants. Make one high/low baked hero example; use reusable tiling maps where unique bakes add little. Build one Geometry Nodes scatter group driven by geology, slope, wash/fan/cliff masks, exclusions, family, seed and density. | Fixed-view comparisons show broad planar faces rather than clay-like lumps. Record per-mesh evaluated triangles, UV/normal checks, texture sizes, LODs and representative scatter counts; review contact, burial and repetition. |
| **5. Unity handoff slice** | Export only approved meshes/maps plus georeferenced region/density/exclusion masks, origin transform and a versioned catalog. Babineaux imports a *bounded representative slice* through the existing Unity World Creator path. Preserve source/export separation and paired `.meta` files. | Unity import scale/pivot/orientation/materials and elevated-camera look pass. Seed and absolute coordinates reproduce placement; neighboring chunks meet; unload/reload restores generated objects; mutable changes retain stable IDs and saved deltas. Measure chunk build, render, collider and texture costs on the target proof configuration. |
| **6. Expansion decision** | Compare results with the user's visual target and measured costs. Choose whether to expand Death Valley, add another geologic province, deepen the rock kit, or trial a paid terrain tool for a specific unmet need. | Expand only after the first slice demonstrates clear visual gain and manageable runtime/data cost. Record any changed geographic scale or art rule before rebuilding. |

Stages 0–2 should be built as one **data-to-Blender vertical slice** before investing in a broad script framework. Stages 3–4 may iterate together visually. Stage 5 is a separate Unity integration batch; the Blender study alone is not gameplay proof.

## Minimal pipeline contracts

The GIS output manifest should include: source IDs/URLs and hashes, license, acquisition date, geographic/projected CRS, vertical datum, projected bounds, local Blender origin, grid width/height/resolution, NoData, resampling method, mask legend, and generator version. Height must remain in metres; any artistic vertical scale or displacement must be an explicit versioned layer. All tiles sample a shared grid and border convention.

The Blender exporter should emit: named mesh family and LOD, dimensions in metres, contact pivot, evaluated triangle count, UV/material map list, export transform, source `.blend` identity, mask grid identity, and catalog version. Blender Geometry Nodes are a **placement preview**; do not export millions of realized rocks or assume Unity can consume Blender's shader/Geometry Nodes graph.

The Unity adapter should bind the catalog and masks to **seed + generator version + absolute location**, with larger-scale authored geology and landmarks constraining placement. The owning chunk may instantiate and unload visuals; feature IDs must remain stable across those transitions. Store player/simulation changes as deltas rather than saving generated mesh instances. The same source data may be sampled for a bounded regional reference or used to parameterize broader generated provinces; this choice is made during Stage 5, not hardcoded in Blender.

## Budget and risk controls

- **Size:** A 50 × 50 km 10 m mesh approaches 50 million triangles before decoration. Keep the context coarse and tile the hero area. Treat all suggested resolutions as initial measurements, not permanent budgets.
- **Data fidelity:** 10 m elevation cannot provide boulder, crack, undercut, or traversable ledge detail. Use authored feature meshes, surface maps, and instanced rocks; check 1 m coverage only for selected detail sites.
- **Visual fidelity:** Aerial photos can contain shadows and color bias. Use them for palette and masks, with artist correction and close-range PBR textures. Real Death Valley geology is reference, not a demand for literal replication.
- **Runtime:** Measure the actual Unity target with the game camera and chunk streamer before multiplying the region. Material count, texture memory, collision, instance density and build time matter alongside source triangles.
- **Dependency cost:** Blender + QGIS/GDAL + USGS + CC0 is the default. A World Machine or Adobe purchase requires a specific demonstrated gap, license review, and user decision. BlenderGIS is optional only after version compatibility is verified.
- **Preservation:** Keep large raw geodata and generated caches out of incidental Unity imports and Git until storage and provenance policy is chosen. Do not alter the paused badlands `.blend` or other dirty Unity files to execute this plan.

## Immediate first batch

Start with Stage 0: identify candidate windows at a valley-to-range transition, verify available USGS elevation/imagery/geology and local GIS tooling, and produce a short visual site comparison with exact bounding boxes. Once the user selects a site, build only one georeferenced terrain tile and inspect it in Blender before writing the rest of the pipeline.

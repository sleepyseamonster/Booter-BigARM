# Legger original-led chassis rebuild

October 16, 2026. **Applied to the same native source.** The author requested an audit of the original frame and chassis followed by a rebuild to recover its silhouette. The [audit and implementation brief](../../../Design/Art/LEGGER_CHASSIS_REBUILD.md) records the observed features, inferred landmarks and constraints. The source remains one complete assembled scene with separately editable major parts. Creative acceptance remains with the author.

## Saved source and modeled result

Active source: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **745,433,568 bytes**, SHA-256 **`788e4de73bac5a52b9853e0071b1df23b9433edc296b981468e2d991c2cd7f93`**. [Application](application.json) binds the update from the [three-section leg checkpoint](../LeggerLegRework20261015/README.md), hash `32e97e86658e12e5655b072c1de40863fdf4ef992ebcc43ed9c7f27fbc989b1d`. Exact recovery remains at `C:/Users/kbkno/.codex/art-cache/LeggerChassis20261016/Before.blend`.

The D: thin-provisioned storage pool has limited allocation capacity. The update reused the existing file allocation after independent C: recovery/candidate hash checks, then flushed and verified the result; it was not an atomic replacement. Blender lossless native-file compression reduces the file by 99,771,732 bytes despite retaining sources and adding maps. Post-load fingerprints verify geometry and packed-image preservation. This is a storage saving, not a runtime memory measurement.

Thirteen existing structural/drive meshes were rebuilt and two major forward yokes added:

- Two broad tapered cast diagonals descend from the front neck toward the footrest junction, replacing the old deep continuous front/bottom cradle. Upper rails, rear supports and fitted crossmembers provide a coherent assembly.
- Compact forward yokes have shaped bearing covers and tapered lower guards. Hip mounts fit the accepted axes without moving the legs.
- Tall tapered footrest webs follow the original's side silhouette while retaining the existing foot pads. Seat rails follow the unchanged saddle pan; the steering bearing stays fixed.
- Five drive/cooling housings are narrower and more inboard/rearward, restoring visible space around the upper legs. A shaped rear cooling cover has swept fixed ribs.
- Curved service arcs hang beneath the rear body. Inspection caught insufficient hose-end seating; closed connectors and revised terminations were added before baking. Cargo rack geometry remains intact and receives the revised Frame atlas.

There are **75 active major meshes, 42,552 quad control faces and 136,928 evaluated triangles**, across the existing eleven module roots. This adds 16,872 evaluated triangles over the accepted leg checkpoint, principally for the rebuilt cast forms, curved routing and supports. [Module budget](module_budget.json), [native checks](native_qa.json) and [canonical inspection](native_canonical.json) identify the exact result. Active stored faces are all quads; no boundary/nonmanifold edges, loose vertices or degenerate base faces were detected. Spacing follows curvature, rather than forcing equal-sized squares across every surface. Modifiers/rendering produce triangles.

## Original audit and fit

[Inventory](audit_inventory.json) and [view calibration](audit_views.json) record fourteen fresh original/before views. The authority is retained `Legger_DetailBake_Source`, 2,667,021 evaluated triangles, at its saved transform. Side/oblique/rear/top/underside views establish the large forms; half-side and central-slab camera cuts reveal obscured structure. Those cuts neither modify the native mesh nor establish real part boundaries. The fused generated reference has no verified skeleton or mechanical construction.

The original suggests a cast diagonal around 29° downward/rearward in the side plane, compact forward bearings, upright footrest webs and curved rear routing. Approximate covered landmarks carry 20–30 mm reading uncertainty; they guide the reconstruction, not exact engineering or skeleton recovery. Retained body panels still differ from the original and partly conceal the forward yokes. This pass adapts the chassis to those panels and the accepted rest stance rather than recreating the original asymmetric pose.

All **58 unaffected active meshes** retain full geometry/UV/material-assignment/world fingerprints, including **all 30 accepted leg meshes**, the saddle, controls, body and cargo. New paired yokes, hip mounts, footrests and drive pods have zero stored-vertex reflection error. Checks verify the prior hip/knee/hock/ankle targets, the steering bearing axis/end planes and the existing pad corners. Overall height remains **2.2244136 m** without rescaling. Bounds are approximately **0.866 × 1.64386 × 2.22441 m**; rear housing curvature changes the rear extent by a few millimetres. Eleven module-root movement checks pass in memory and are restored before inspection completes. These checks do not certify articulation or rider clearance.

## Textures and preserved sources

[Material manifest](materials.json) records **ten new packed maps**: 4096² base color, +Y tangent normal, roughness, metallic and AO for Frame and Drive. **Forty-five retained active maps** give 55 across eleven sets. Base color is sRGB; data maps are Non-Color. AO is an isolated-module auxiliary, not lighting multiplied into albedo. Visible parts have only their final render UV atlas. The existing ivory/dark-metal/brown-leather palette and restrained wear continue; no new setting material or crafting rule is introduced.

[Preservation](source_preservation.json) accounts for **all 816 pre-pass mesh objects** using exact raw vertex, face/material, UV, bevel/sharp attribute and world-matrix fingerprints, and **all 136 pre-pass packed image payloads** byte for byte. Thirteen superseded objects and two pre-atlas rack copies remain under `REFERENCE - Before original-led chassis rebuild`. Seventeen authored procedural surface copies remain under `REFERENCE - Original-led chassis procedural surfaces`. Earlier dense and baked references remain hidden. The complete file contains 146 packed images, including historical resources; 55 belong to the active assembly.

[Texture QA](texture_qa.json) reports zero detected control-interior overlap, full sampled evaluated-interior coverage, no empty sampled albedo and over 99.9% normalized sampled tangent normals in both revised modules. No collapsed evaluated UV triangles were detected in these two sets. Raster inspection excludes sub-four-pixel faces/triangles and eroded faces with no surviving interior: 431 Frame control faces and thousands of small evaluated bevel triangles were skipped. It does not certify every seam/mip or revise the historical UV limits of untouched sets.

## Visual verification and handoff

Thirteen matched geometry/PBR views and four technical boards/contact sheets were inspected, including bare structure, front/rear/top/underside, fitted body, full vehicle, clay and hip interface. [Render comparison](render_comparison.json) records equal alpha silhouettes; maximum per-view mean RGB bake difference is approximately **0.612/255**, with larger isolated edge differences. This verifies the bake's appearance in those captures, not exact equivalence to the original generated surface. The saved D: native file is independently reopened, checked and rendered; its matched assembled result is recorded in that comparison.

[Image identities](image_manifest.json) bind 45 local captures/boards. Main comparison: `C:/Users/kbkno/.codex/art-cache/LeggerChassis20261016/Legger_Chassis_Comparison.png`; bare-structure comparison: `Legger_Chassis_Structure.png` in the same cache. [Local recipe/checkpoint identities](local_recipe_manifest.json) identify the retained construction, atlas, bake and review sources. Original audit inspection did not save its visibility or clipping overrides.

All modeling and verification used hidden Blender 5.2.2 LTS processes with embedded auto-execution disabled. The user's foreground app and open Blender window were not changed. **Use File > Revert before saving an already open Blender window**, or reopen the same native path, to load this saved update.

No rig, animation, engineering/Boolean-union certification, motion/clearance test, collider, LOD, Unity import or runtime performance test was added. Runtime texture consolidation/downsampling and deformation remain later work. World identity, streaming/unload/reload, generated-object identity and persisted deltas are inapplicable to this local source-art task; physical scale, fitted interfaces, source retention and scene organization remain accounted for. Native files, textures and render pixels remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

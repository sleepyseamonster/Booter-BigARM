# Legger sci-fi form refinement

**Subsequent checkpoints:** the [three-section leg rework](../LeggerLegRework20261015/README.md) replaces this pass's two-link geometry and numeric targets while retaining matching stance and height. The later [original-led chassis rebuild](../LeggerChassis20261016/README.md) identifies the current native source, retaining those legs and reconstructing Frame/Drive around them. This receipt's hashes, counts and preservation statements describe the earlier sci-fi checkpoint.

October 15, 2026. **Applied and verified in the existing authoring file.** The author requested implementation of the [form overhaul brief](../../../Design/Art/LEGGER_SCI_FI_OVERHAUL_AUDIT.md), bringing the overly simple modular model closer to the retained original futuristic design. This pass rebuilds 53 major meshes, rebakes seven affected texture modules and preserves the [neutral leg alignment](../LeggerLegAlignment20261015/README.md). Creative acceptance remains with the author.

## Saved source and organization

The sole active source is `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **810,807,494 bytes**, SHA-256 **`fcb639d849c25866508a05b9546175140f44fadaa79a1ff177bc499dc08d1b1f`**. [Application](application.json) records the guarded atomic replacement of neutral baseline `fa71944d8d6292448418810cce37c89b4997cf565b23192dabd656f251a20538`. An exact recovery of that baseline remains local at `C:/Users/kbkno/.codex/art-cache/LeggerSciFi20261015/Before.blend`. Local construction, atlas and textured checkpoints remain in that cache. No reference or recovery source was deleted.

There is still one scene, `Legger`, and one view layer, `LEGGER - COMPLETE`. Collection `LEGGER - ASSEMBLED MODEL` opens with the complete fitted vehicle, 67 major meshes and eleven independently movable module roots. Small fixed fittings are grouped into their parent meshes. Hidden references remain outside that visible hierarchy. No exploded arrangement replaces the saved default.

All 67 stored control meshes are closed manifold quad meshes, with no detected loose vertices or degenerate base faces and unit object scale. Quads follow curved strips, loft sections and joint profiles; their sizes vary with curvature rather than forcing equal squares across flat surfaces. Evaluated bevels and rendering introduce triangles. [Native checks](native_qa.json), [canonical object inspection](native_canonical.json) and [module budget](module_budget.json) bind these statements to the saved hash.

| Module | Major meshes | Control quads | Evaluated triangles |
| --- | ---: | ---: | ---: |
| Frame/chassis | 10 | 5,320 | 17,328 |
| Drive/cooling | 5 | 2,246 | 7,064 |
| Left leg | 12 | 7,536 | 22,606 |
| Right leg | 12 | 7,536 | 22,606 |
| Body armor | 6 | 4,196 | 14,564 |
| Saddle | 2 | 2,362 | 6,124 |
| Steering/controls | 8 | 1,798 | 6,500 |
| Four retained cargo cases | 12 | 5,308 | 26,872 |
| **Total** | **67** | **36,302** | **123,664** |

The prior neutral baseline had 86,072 evaluated triangles. This pass spends geometry on visible curvature, layered joint profiles, wrapped armor, saddle shaping and feet. It exceeds the brief's provisional 120k upper envelope by 3,664 triangles, chiefly in the legs and rider interfaces. That is an authoring tradeoff for the requested shape restoration, not measured Unity performance. No further decimation was applied.

## Modeled changes

- Body cheeks and shrouds now have crowned cross-sections, curved perimeter returns and tapered ends. The front control pod follows a shaped central spine with recessed service relief.
- Chassis members, cradle, hip mounts and drive housings use tapered cast forms and fitted transitions. Layered cooling housings replace simple blocks. The two existing cargo racks retain their geometry and receive the revised frame atlas.
- Both leg chains have wrapped armor shells, dished concentric joint covers, shaped mechanical cores, separate actuator barrels/rods and arched split-toe feet with heel supports. Matching left/right design is fitted around the neutral joint targets.
- The saddle has a scooped seat, raised bolsters, curved pan and baked ribs/stitching. The swept handlebar, branching stem, levers and compact instrument wedge read as one rider interface. The small display pattern is in base color; no emissive map was added.
- The twelve cargo meshes retain geometry, UVs, materials and world transforms. All twenty cargo map payloads remain byte-identical. The ivory/dark-metal/brown-leather palette and restrained wear continue; better geometry does not mean a glossy factory finish.

Joint centers remain hips `(±0.225, -0.300, 1.320)`, knees `(±0.260, -0.245, 0.800)` and ankles `(±0.275, -0.300, 0.170)` metres. Feet are grounded side by side. There is no global scale change. Actual evaluated bounds are approximately **0.866 × 1.446 × 2.2244 m**; the remodeled instrument top is about 1.6 mm below the old maximum. The source remains at the author's approximately 2.23 m game scale. No armature, pose or animation was introduced.

## Texture and preservation proof

[Material manifest](materials.json) records thirty-five new packed maps: base color, tangent-space normal, roughness, metallic and AO for Frame, Drive, LeftLeg, RightLeg, BodyArmor, Saddle and Steering. The first five modules use 4K atlases; saddle and steering use 2K. Four retained cargo sets contribute twenty maps, giving **55 active packed maps across eleven PBR materials**. Base color uses sRGB; data maps use Non-Color. AO is an isolated-module auxiliary map and is not multiplied into albedo. Surfaces were authored procedurally and baked against the new forms; these are actual asset textures, not concept images or an indiscriminate projection of noisy original geometry. All visible parts have exactly one render UV layer.

[Source preservation](source_preservation.json) accounts for **all 626 pre-overhaul mesh objects** using raw vertex, polygon/material, UV, bevel/sharp attribute and world-transform fingerprints. All 86 original packed-image payloads are byte-identical. The old 53 rebuilt meshes and two pre-atlas rack copies are in `REFERENCE - Neutral baseline before sci-fi overhaul`; 55 authored surface sources are in `REFERENCE - Sci-fi procedural surfaces`. Earlier dense and baked references also remain hidden. The complete native file therefore contains 121 packed images including historical resources; only 55 maps belong to the visible assembly. The preservation proof does not certify a watertight Boolean union or mechanical operation of disconnected fitted parts.

[Texture QA](texture_qa.json) finds full coverage at inspected evaluated UV raster interiors, no empty interior albedo texels and over 99.9% normalized sampled tangent normals in each revised module. Six modules have zero detected control-interior overlap; steering has approximately **0.0012%** raster overlap, within the stated shared-edge tolerance. Tiny generated bevel/cap triangles have collapsed UV area, at most **0.000298% of a module's evaluated surface area**. Those residuals are recorded, not claimed absent. Sampling excludes triangles under four pixels and faces with no surviving eroded interior; this does not certify every seam, every mip level or runtime texture import. Clean up remaining microscopic cap UVs when preparing the runtime mesh.

## Visual and native verification

Twelve candidate PBR views were opened and inspected: assembled, front, side, rear, elevated, body, legs, cockpit, isolated chassis, underside, feet and clay assembly. Six geometry/procedural views preceded baking. [Render comparison](render_comparison.json) records identical alpha silhouettes between geometry/procedural and PBR captures at matched cameras, with mean RGB differences below 0.33 on an 8-bit scale and 99th-percentile differences of two. Individual pixels differ more at texture/shading edges; this is not exact material equivalence.

The actual saved native file was reopened in a fresh hidden Blender 5.2.2 LTS process. Its assembled render matches the reviewed textured candidate with maximum RGB difference **1/255** and identical alpha. Native QA rechecked prior-source retention, all active topology, roots, neutral joints, ground contact, UV0, materials, map payloads, units and bounds. Reference collections are hidden in viewport and render. Render cameras, studio lights and material overrides existed only in the review processes; they were not saved into the authoring scene.

[Image manifest](image_manifest.json) identifies twenty-one local PNGs under `Logs/LeggerSciFi20261015/`, including `Legger_SciFi_Refinement.png` (matched neutral before/after) and `Legger_SciFi_Details.png` (six detail views). The before image comes from the verified neutral-alignment checkpoint; the original dense source remains the design reference, whose staggered stance was deliberately not restored. [Construction summary](construction.json) and [local recipe identities](local_recipe_manifest.json) identify the local implementation and recovery chain. Review images, recipes, maps and native sources remain local/ignored; this receipt publishes text evidence only under the [local-only source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

The user Blender session was not focused, reloaded or closed. To inspect the updated disk source in an already open Blender window, use **File > Revert before saving**; its in-memory scene can still be the earlier version.

This is a source-art refinement checkpoint. Rider fit, articulation clearance, rigging, collision, LODs, channel packing, material/draw-call consolidation, texture memory, Unity import and Greater Wasteland readability remain unvalidated. No Unity assets/settings or gameplay were changed, and no gameplay smoke test ran. World identity, streaming/unload/reload, generated-object identity and persisted runtime deltas are inapplicable because this work modifies local source art without runtime integration. Authored scale, neutral joints, cargo and palette constraints were explicitly preserved. The author can now review this concrete pass before deciding on further modeling or reduction.

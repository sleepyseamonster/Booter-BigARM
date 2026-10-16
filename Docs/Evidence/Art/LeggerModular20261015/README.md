# Legger modular assembly and textures

October 15, 2026. The author requested an independently modeled chassis, extended the work to the legs, saddle and handlebars, then authorized a complete modular model with separate textures, an audited plan and one organized scene. The [plan and audit](../../../Design/Art/LEGGER_MODULAR_MODEL_PLAN.md) govern this checkpoint. Small fixed fittings are grouped into their parent parts.

Subsequent author review: the rebuild is too simplistic outside the crates and needs an overhaul toward the original futuristic design. The [form audit](../LeggerSciFiAudit20261015/README.md) closes this technical lane and records the replacement modeling direction. The counts and verification below remain facts about the unchanged technical baseline; they do not establish final visual acceptance.

The author then requested aligned legs for rigging. The [neutral alignment receipt](../LeggerLegAlignment20261015/README.md) identifies the current source and supersedes this checkpoint's staggered-pose coordinates while retaining its topology budget and packed maps.

## Current authoring file

`SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` remains the sole active native file. Scene `Legger` and its only view layer, `LEGGER - COMPLETE`, open on the complete assembled vehicle. Collection `LEGGER - ASSEMBLED MODEL` contains the active parts. `90 - References - hidden` retains prior geometry and procedural bake sources outside the intended export hierarchy. No exploded arrangement or isolated chassis replaces the assembled default.

| Visible module | Major meshes | Quad control faces | Evaluated triangles |
| --- | ---: | ---: | ---: |
| Chassis and frame | 10 | 6,682 | 19,252 |
| Drive and cooling | 5 | 1,296 | 6,544 |
| Left leg | 12 | 2,506 | 11,444 |
| Right leg | 12 | 2,398 | 11,068 |
| Body armor | 6 | 324 | 2,888 |
| Saddle | 2 | 1,486 | 3,452 |
| Steering and controls | 8 | 1,396 | 4,552 |
| Two main cases | 6 | 3,676 | 17,752 |
| Two auxiliary cases | 6 | 1,632 | 9,120 |
| **Total** | **67** | **21,396** | **86,072** |

The preceding accepted source contained 343 visible meshes and 88,420 evaluated triangles. This pass reduces visible object count while improving the structure and retaining a similar triangle budget. All stored visible control faces are quads; evaluated bevels and rendering introduce triangles. Counts include both bevel passes. Per-edge weights preserve separate treatment for major corners and small fittings without maintaining a standalone object for each fitting.

## Construction and organization

The frame uses closed side loops, crossmembers and a seat subframe. Cargo racks sit inboard of the existing cases and support their undersides; engine isolators, steering support, hip mounts and footrests have clearer attachment logic. Drive core, spine housing, cooling housing and side pods remain separately editable.

Both legs retain the existing hip, knee and ankle centers and staggered stance. Their cores, outer/inner shell groups, joint housings, actuator barrels/rods and foot assemblies are separate major meshes. Armor uses regular quad rows and thickness; cores taper through the limb, and hydraulic lines terminate into the assembly. Feet retain ground contact. The saddle has a regularly sampled cushion, trim and separate pan; its height and rising rear profile remain consistent with the prior seat. The steering stem meets the bar, with separate grips, levers, gauge and front control pod. Stitching, grip grooves and gauge markings are texture detail.

The six useful main body fairings retain their prior shapes, with their fixed fasteners grouped into them. Each cargo case now consists of body, lid and strap/fitting groups. Its previous atlas/maps are retained. Lid origins sit on the hinge axis. Each module has a named root; all eleven assembly roots pass an independent translation check. These origins and separations support future rigging but are not a completed rig, joint hierarchy or motion validation.

## Textures and UVs

Seven new module sets cover frame, drive, left leg, right leg, body armor, saddle and steering. The first five use 4K authoring atlases; saddle and steering use 2K. Four existing cargo sets remain: 4K main cases and 2K auxiliary cases. Every set has base color, tangent-space normal (+Y), roughness, metallic and AO, for **55 packed images across 11 PBR materials**. All parts in a module have distinct regions in its atlas and remain editable as separate meshes. AO is an auxiliary map rather than multiplied lighting in albedo.

Each visible mesh has exactly one UV layer: its final atlas as UV0. Procedural coordinate layers remain in hidden source copies. Source versus baked render comparisons and an additional UV0-cleanup render verify that removing those unused layers preserves appearance. Base color uses sRGB; data maps use Non-Color. All 20 retained cargo map payloads are byte-identical to the preceding cargo checkpoint.

The new maps bake the clean authored materials and details rather than projecting unwanted generated lumps onto the rebuilt forms. The approved ivory, dark metal and brown leather palette is retained. Authoring atlas sizes and 67 material-bearing meshes are not a measured runtime memory or draw-call budget. Runtime channel packing, compression, LODs and import/material validation belong to the Unity handoff.

## Verification

[Application receipt](application.json) identifies the exact before/after native hashes and atomic update of the same file. [Native checks](native_qa.json) verify one scene/view layer, all 67 parts visible, quad control topology, per-part manifold closure, no detected loose vertices or degenerate faces, unit scale, joint positions, ground contact, independent root movement, UV0 layout and packed images. Approximate visible bounds are 0.866 x 1.5965 x 2.226 m. Height and ground scale remain unchanged; the old rack's longitudinal curve overshoot is removed.

All 504 mesh objects from the accepted baseline remain accounted for in the retained original/reference copies, with matching stored vertices/polygons, material assignments and evaluated world transforms. Reference collections are temporarily enabled during read-only comparison so excluded-object transform caches are not mistaken for actual transform changes; flags are restored afterward. This is preservation of reference data, not a claim that the new active forms are silhouette-identical to the earlier generated source.

[Canonical inspection](canonical_inspection.json) independently matches the active topology/counts while excluding references. [Texture checks](texture_qa.json) record zero rasterized control-UV interior overlap, complete evaluated UV-interior bake coverage, normalized tangent normals and map ranges/hashes. Bevels remove unused corners of control polygons, so both control-cage and evaluated coverage are recorded; only evaluated surface coverage determines acceptance. No evaluated triangles have zero UV area in these seven atlases. Raster tests exclude shared-edge pixels and subpixel faces and do not certify every future mip level.

[Material inventory](materials.json) names the packed sets. [Render comparison](render_comparison.json) records matched authored/baked views and native saved-file reproduction. Fixed Cycles/OPTIX renders were inspected assembled, front/back/side, bare chassis/underside, legs, saddle/controls and with actual control-edge overlays. Images and working recipes remain local under `Logs/LeggerChassis20261015/`; the review board is `modular_review.png` there. Source recovery checkpoints remain in the task's local C: cache.

Per-part closure does not prove a watertight union between assembled pieces, mechanical strength, collision clearance through animation or functional engineering. No rig, animation, collider, export, Unity asset/scene, package, project setting or gameplay behavior changed. No gameplay smoke test ran. The author retains final visual acceptance.

Deterministic world identity, streaming/unload/reload, generated-object identity and persisted runtime deltas are inapplicable to this local source-art edit. Existing pose, scale, case placement and joint/rider interfaces are the authored constraints. Native source and recovery files remain local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Only task-owned plan/evidence documents are committed; no push is authorized.

All processing used hidden background Blender 5.2.2 LTS processes. No application was focused or refreshed. In an already-open Blender window, use **File > Revert before saving** to load the complete saved assembly.

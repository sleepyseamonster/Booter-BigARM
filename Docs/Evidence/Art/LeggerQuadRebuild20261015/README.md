# Legger clean quad rebuild

October 15, 2026. Owner: Blender under Gottspan coordination. The author requested rigid-looking cargo cases with leather straps and better chassis/frame geometry, authorizing a rebuild from scratch. The author clarified that “polished” means mesh quality rather than a glossy finish, requested clean quads where practical, and explicitly retained the ivory/dark-metal/brown-leather palette with restrained wear.

## Current source and scope

`SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` remains the single current file in the authoring folder. Its visible asset is now a newly constructed interpretation of the same motorcycle-on-legs design, rather than a further decimation or exact retopology of the generated surface. This is the first clean construction review candidate; its shape/detail choices are not final creative approval or game canon.

| New visible geometry | Count |
| --- | ---: |
| Editable control faces | 9,018 |
| Quad control faces | 9,018 (100%) |
| Triangles/ngons in control meshes | 0 / 0 |
| Evaluated triangles with editable bevels | 96,804 |
| Separate mesh parts | 343 |
| Approximate dimensions | 0.866 x 1.615 x 2.226 m |

The new mesh collection is `LEGGER_REBUILD - clean quad geometry`, parented to `Legger_Rebuild_ROOT`. Its categories are Chassis, Armor, Legs, Cargo, Leather, Hardware and Controls. Every new part has unit scale and a geometric-center origin. Overall height remains approximately the author's 2.23 m size, with the ground at z = 0. This construction changes some proportions and outlines deliberately, particularly the rigid cargo cases, fairings and feet; it is not silhouette-identical to the generated source.

The preceding 155,999-triangle asset remains intact inside `REFERENCE - previous baked Legger - hidden`. Its original packed materials/maps remain on those reference objects. The earlier 2,667,021-triangle detailed bake source also remains hidden. Native inspection compares every retained vertex and polygon with the inspected pre-rebuild file. These references are hidden, nonselectable and excluded from rendering and intended export. Export the new collection only.

## Construction

- Cargo uses planar box control cages, distinct lids and gaskets, face inserts, corner guards, hinges, grab handles and separate latch hardware. Four cases retain the paired side-cargo arrangement.
- Leather straps are closed, thick quad ribbons wrapping around the cases, with separate buckle frames, tongues and tail pieces. Stitching is procedural material relief/color rather than extra stitch geometry.
- Chassis rails and racks use consistent tube profiles; drive casings, joint bearings, yokes, housings, cooling fins and fairings are separate manufactured forms. Their interfaces and mounting logic are more legible than the merged source surface.
- Armor and saddle use regular quad grids/lofts. Leg shells cover both outer and inner faces, with separate rigid cores, actuator barrels/rods, bearing covers, hoses, ankle hardware and toe/heel parts.
- Box faces stay economical quads. Circular caps use square-to-disc quad grids; tube ends use small quad cap fans. All are intended as rigid components. Quad counts do not certify deformation loops or guarantee an ideal subdivision surface at every cap.
- Bevel and weighted-normal modifiers remain editable. Evaluated bevel geometry contains the usual additional faces and render triangulation; the 100% quad statement applies to the stored control meshes.

The retained generated normal maps are not projected onto the new flat cases/fairings, because that would reintroduce the unwanted lumpy relief. The new asset currently uses self-contained Blender authoring shaders with controlled roughness, small grain and saddle/strap detail. The palette is retained; a final localized weathering/painted surface pass, final UV atlases and runtime texture baking remain subsequent work. These preview shaders are not Unity materials and do not establish compatibility with the accepted [Sable surface-treatment direction](../../../Agents/Sable/memory/DECISIONS.md).

## Verification and limits

[Mesh summary](mesh_summary.json) records the exact candidate hash, group totals, dimensions, control topology, evaluated triangle count and structural checks. Every new component is closed/manifold individually, with zero detected degenerate faces or loose vertices. This does not prove clearance through animation, absence of every intersection between assembled parts, mechanical functionality, collision suitability or a watertight union of the complete assembly.

The repository's read-only `Tools/Art/Blender/inspect_blend.py` also ran against the applied source; [its scoped summary](canonical_inspection.json) independently matches the 343 new components, their 18,036 control triangles/96,804 evaluated triangles, and zero boundary/non-manifold/loose counts. Hidden reference geometry is excluded from those active-asset totals.

[Construction inventory](construction_inventory.json) names every new part and records its quad count, evaluated triangles and modifier types. [Application receipt](application.json) records the inspected source hash and atomic replacement of the same file. [Native reopen](native_reopen.json) verifies the actual applied file, visible quad meshes, retained geometry and hidden-reference state. It was rendered again from disk; [saved-render comparison](saved_render_comparison.json) records the bounded pixel comparison with the prepared three-quarter render.

Matched original/rebuild renders were inspected in full side, front, rear and three-quarter views and cargo/chassis/leg close-ups. Additional clay overlays draw the base control edges rather than the renderer's triangle edges. This review checks construction and presentation; it is not a zero-loss detail comparison with the generated original. The initial faceting on the main fairings and exposed inner leg beams was corrected before the applied checkpoint.

Blender 5.2.2 LTS / Cycles OPTIX rendered the study with 32 samples, denoising and fixed studio lighting at 1500 x 1200. The comparison board is local at `Logs/LeggerRebuild20261015/Review/rebuild_board.png`; shaded close-ups and `quad_cargo.png`, `quad_chassis.png`, `quad_legs.png` remain alongside it. Working scripts and raw structural records are in that ignored task cache. The procedural authoring materials need baking/consolidation before a runtime handoff. There is no rig, animation, LOD, collider, runtime draw-call budget, Unity import or gameplay validation in this checkpoint.

The native source and backups remain local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Only small textual evidence is committed. No Unity asset, package, setting or gameplay file was changed. World identity, streaming/unload/reload, generated-object identity, authored world-placement constraints and persisted runtime deltas are inapplicable because this operation changes only a local source model, not world generation, actors, placement or saved state.

No existing GUI window was focused or reloaded. Use **File > Revert before saving** from an already-open Blender window to load the new saved construction.

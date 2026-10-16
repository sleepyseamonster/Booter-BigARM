# Legger separate cargo cases

October 15, 2026. The author requested separately modeled and textured crates/cases in the existing Legger scene, using reasonably uniform quads and a game-conscious polygon count. The approved ivory, dark-metal and brown-leather palette with restrained wear remains the controlling direction. This is a cargo-only continuation of the [clean construction checkpoint](../LeggerQuadRebuild20261015/README.md).

## Saved source and editing

The single current authoring file remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`. Four independent assemblies live under `LEGGER_REBUILD - clean quad geometry > Cargo - separate textured cases`. Select a `Legger_Cargo_L_Main_ROOT`, `Legger_Cargo_R_Main_ROOT`, `Legger_Cargo_L_Aux_ROOT` or `Legger_Cargo_R_Aux_ROOT` to move a whole case. Each root retains separately editable body, lid, straps, buckle/latch hardware, guards and hinges; the main cases also retain grab handles. These are multipart authoring assemblies, not four joined runtime draw calls.

| Per assembly | Large case (each) | Auxiliary case (each) |
| --- | ---: | ---: |
| Editable quad control faces | 1,838 | 816 |
| Evaluated triangles including bevels | 8,876 | 4,560 |
| Separate mesh parts | 41 | 29 |
| Dedicated atlas resolution | 4096 x 4096 | 2048 x 2048 |

All four cases together have 5,308 quad control faces and 26,872 evaluated triangles, down from 35,256 evaluated triangles in the preceding construction checkpoint (23.8% fewer). The complete visible Legger now has 12,282 control quads and 88,420 evaluated triangles. The quad statement applies to stored editable meshes; bevel evaluation and game rendering introduce triangles. These counts are an authoring budget, not measured Unity performance or a certification of a complete game-ready character.

## Geometry and surfaces

Large body, lid and inset surfaces have regular quad grids. Main body grid edges are approximately 42.5-46.7 mm; auxiliary body edges are approximately 44.4-48.3 mm. Strap rows follow the existing wrap contour with straight-run spacing at most 34 mm, retaining thickness and shorter rows around bends. Small hardware stays economical rather than receiving unnecessary subdivisions. Two bevel segments retain the large corner outlines, with one segment on tiny fittings and editable weighted normals.

Each case has one dedicated baked PBR material, shared by all pieces in that case, with five packed images: base color, tangent-space normal (+Y), roughness, metallic and ambient occlusion. Base color uses sRGB; all data maps use Non-Color. AO is provided as an auxiliary map and is not multiplied into albedo. Twenty new maps are packed into the native file; no external image file is required to display these cargo materials. Four independent atlases allow later per-case edits.

Surface detail includes leather grain and stitching, broad low-contrast tonal variation, material roughness and restrained wear around fittings/corners. Fine details are in the baked maps; larger straps, buckle openings, corner guards and handles remain geometry for their silhouette. The maps were baked from the clean procedural cargo surfaces, rather than projecting the old generated case lumps onto the new planar shapes. A procedural source copy of every cargo part is retained in `REFERENCE - Cargo procedural bake sources - hidden`, excluded from display/render/export. The rest of the robot retains its prior authoring materials; this task does not claim a whole-robot texture finish.

## Verification

[Application receipt](application.json) identifies the exact before/after hashes and same-file atomic update. [Native reopen checks](native_qa.json) verify the applied file, all-quad/manifold cargo meshes, no detected loose vertices or degenerate faces, packed map completeness, unit scale and independent case-root movement. The 224 pre-existing non-cargo mesh objects retain their exact stored vertex/polygon data, world matrices and material assignments. Overall dimensions remain approximately 0.866 x 1.615 x 2.226 m with ground at z = 0. A placement error in an intermediate unsaved candidate was rejected during visual review; the applied candidate passes both placement and independent-root checks.

[Texture checks](texture_qa.json) record coverage, rasterized control-UV overlap, map ranges, hashes and normal lengths. Shared-edge pixel aliases and subpixel faces limit raster overlap checks; these checks do not prove every evaluated-bevel UV seam or future mip level. Atlases use Smart UV layout with a 0.006 island-margin setting and 12-pixel bake dilation. [Canonical inspection](canonical_inspection.json) independently records active mesh totals and excludes the hidden reference meshes from the active counts.

Fixed Cycles/OPTIX studio renders were inspected in the assembled robot, isolated front/back/top and auxiliary views, alongside a base-quad wire overlay and matched procedural-source/baked-material view. [Render comparison](render_comparison.json) records the observed bake difference and native saved-file reproduction. Raw images and the preview board remain local in `Logs/LeggerCargo20261015/Review/`. The review helpers and original recovery checkpoint are local caches; Blender sources remain local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

## Boundaries and handoff

No Unity asset, scene, setting, package, rig, animation, collider or export was changed. Runtime mesh joining/draw calls, LODs, import normals, material conversion and actual Unity performance remain a later integration step. Intersections between assembled fittings and collision/mechanical operation are not certified by per-component manifold checks. No gameplay smoke test ran. Visual acceptance remains with the author.

Deterministic world identity, streaming/unload/reload, generated-object identity, authored world-placement systems and persisted runtime deltas are inapplicable because this change only edits local source-model cargo; it creates no runtime actor, world placement or saved-state integration. Existing case placement and model scale are preserved authored constraints.

All processing ran in hidden background Blender 5.2.2 LTS processes. No application window was focused or refreshed. In an already-open Blender window, use **File > Revert before saving** to load this disk version.

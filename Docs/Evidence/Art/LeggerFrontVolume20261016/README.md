# Legger front body volume continuation

October 16, 2026. The author marked the original's upper front housing and lower front body, requesting another pass to fill out those areas. This continues the [front silhouette and panel correction](../LeggerFront20261016/README.md). The dense retained `Legger_DetailBake_Source` remains the visual authority. The author's marked comparison is preserved locally as `Marked_Areas.png`; its identity is included in the image manifest.

## Saved source and reconstruction

Active source: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **799,090,806 bytes**, SHA-256 **`90757f7efa6811fbfd9c82c93b08a98d254f60d13d15e447e5eeab03f2db6304`**. [Application](application.json) records guarded staging, fsync and atomic replacement. Independent preceding source: `C:/Users/kbkno/.codex/art-cache/LeggerFrontVolume20261016/Before.blend`, SHA-256 `cb63818312443226502d02ca7ed76498e985d4b556205a3b60d8f122b5a26071`. The first volume candidate remains as `Checkpoint01.blend`. Native sources, maps and raw review pixels remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

The visible model retains **76 meshes, 45,762 all-quad control faces, 146,264 evaluated triangles, 55 active packed maps, eleven assembly roots, one scene and one view layer**. No active vertices, edges or faces were added. Four existing meshes were reshaped:

| Part | Change |
| --- | --- |
| Front control pod | Recontour the existing 22 eight-sided rings into an enclosed neck and tapered lower body behind the ivory arch, reaching the frame/yoke region. Rebuild the existing 16 × 11 × 2 closed head grid into a deeper wrapped housing with a flat center and chamfered shoulders. Existing bezel/lens geometry stays fixed. |
| Left/right front fairings | Extend the existing ivory tips from approximately 1.680 m to 1.645 m; thicken the inward backing shell to meet the new central body while retaining the exposed ivory surface. |
| Front tank cowling | Deepen only the forward lower region to close the transition into the neck. Retain the upper roof, width and saddle-end termination. |

The original encloses substantially more front body than our preceding thin reconstruction. The first checkpoint filled the void but made the center too broad; the final pass narrows that new center by 16 percent and replaces the rounded head cross-section with deliberate planar/chamfered sections. The resulting forms reconstruct the original's massing and panel relationships rather than its fused microgeometry.

[Horizontal section measurements](volume_sections.json) isolate the connected control components. Central-body depth at Z = 1.830 m changes from **52.5 mm to 379.7 mm**. Upper-head depth at Z = 2.000 m changes from **83.3 mm to 149.3 mm**. These are exact intersections of control edges with the stated horizontal plane. Auxiliary original fused-surface slices include nearby parts; their coordinate envelopes support the visual audit but do not prove semantic segmentation, solid-volume accuracy or final fit.

## Baked surface detail

The eight Steering meshes share a copied `Legger_FrontVolume_Steering_PBR` material. Four new packed 2048-square maps revise base color, roughness, metallic and tangent-space normal only in the existing central-body/head UV regions. The isolated CPU bake adds paired side access-cover rings, two upper-head rings and a fine front access seam. Relief is modeled in the bake shader at 1.2 mm for the rings and 0.65 mm for the seam; no ring, cover or fastener geometry is added. Existing normal grain feeds the new bump bake. AO is retained. This is procedural surface reconstruction, not a claim to recover every detail from the original texture.

Base color remains sRGB; data maps remain Non-Color. [Decoded PNG checks](texture_patch_qa.json) find zero channel difference across **3,217,977 pixels outside the body/head UVs plus an 8-pixel guard**, in every revised map. The decoded normal vectors have positive tangent Z and median length approximately one. UV coordinates are unchanged; reshaping changes local texel density, and retained AO is not a fresh geometric occlusion bake.

## Preservation and verification

[Preservation](source_preservation.json) verifies all **1,010 preceding mesh versions** and **194 preceding packed image payloads** by fingerprints/bytes. Eleven hidden pre-pass copies preserve the four changed forms and eight material users, with one overlap. Native totals including hidden history are **1,021 mesh objects and 198 packed images**. Seventy-two active meshes retain exact vertex coordinates; sixty-five retain complete mesh/material-name/world fingerprints. All active connectivity, UV coordinates, modifier settings and transforms stay unchanged. All thirty leg meshes, joint targets, rider pocket, handlebars/grips/levers, foot contacts, frame/yokes, cargo and recreated hoses stay fixed.

[Native QA](native_qa.json) and [canonical inspection](native_canonical.json) passed after independent reopening. All visible controls are closed, nondegenerate all-quads without detected loose vertices or boundary/nonmanifold edges. Every connected component of the four changed meshes has positive signed volume. Fairings mirror within three micrometres; all eleven assembly roots pass independent movement checks. These are static authoring checks, not complete solid-intersection, rig, animated clearance, dressed-rider or Unity proof.

[Views](views.json) bind 34 renders: sixteen original/before views, eight first-checkpoint views, eight final-candidate views and two reopened-native views. All three sheets, selected full-size side/oblique details and the original/before/revised comparison board were inspected. [Matched render comparison](render_comparison.json) checks the independently reopened front oblique and complete assembly against the candidate. [Image identities](image_manifest.json) bind 39 local images, including the author's marked areas and `Legger_FrontVolume_Comparison.png`. [Visual review](visual_review.json), [local recipes](local_recipe_manifest.json) and [verification](verification.json) bind provenance. Cache: `C:/Users/kbkno/.codex/art-cache/LeggerFrontVolume20261016/`.

Work used hidden Blender 5.2.2 LTS workers with auto-execution disabled. No foreground activation or GUI reload/save occurred. Reopen the saved source before saving from an older loaded session. No Unity asset, metadata, package, export, rig, collider or gameplay test changed. World identity, streaming/unload/reload, generated runtime identity and persisted deltas are inapplicable to source art; original design, budget, scale, pose and rider constraints apply. Final creative acceptance remains with the author.

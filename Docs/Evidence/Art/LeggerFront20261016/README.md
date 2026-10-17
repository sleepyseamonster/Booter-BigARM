# Legger original-led front silhouette and panels

October 16, 2026. The author requested comparison with the original and correction of the vehicle front silhouette and panel shapes. This pass follows the [hose recreation](../LeggerHoses20261016/README.md) in the same assembled scene. The retained dense `Legger_DetailBake_Source` controls the design reference. The result reconstructs its major shapes through the existing quad surfaces; it is not an exact segmentation of the generated source or final artistic acceptance.

## Saved source and changes

Active source: `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **797,234,968 bytes**, SHA-256 **`cb63818312443226502d02ca7ed76498e985d4b556205a3b60d8f122b5a26071`**. [Application](application.json) records guarded staging, fsync and atomic replacement. Independent preceding source: `C:/Users/kbkno/.codex/art-cache/LeggerFront20261016/Before.blend`, SHA-256 `49a8a0e4322fc388ea2cfbb3d3ede9b94323109aa41e1573020d0fd5f88a6a17`. The first reviewed candidate is retained as `Checkpoint01.blend`. Native files, maps and raw review pixels remain local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

The model retains **76 visible meshes, 45,762 all-quad control faces, 146,264 evaluated triangles, 55 active packed maps, eleven assembly roots, one scene and one view layer**. No vertices, edges or polygons were added to the active model. [Construction](construction.json) records the four reshaped mesh parts and their section profile:

| Part | Original-led correction |
| --- | --- |
| Left and right front fairings | Broader swept ivory cheeks and descending pointed tips; lower the oversized crown and recover the open inner arch. Inset dark backing beneath the ivory surface. Existing slot and lower trim follow the revised panel. |
| Front tank cowling | Broaden the rear upper shoulder and dark roof into a tapered trapezoid while retaining the lower waist and saddle-end termination. |
| Front control pod | Replace the rounded capsule with a narrower angular central spine, external dark head casing and lower split lens/bezel. Advance and deepen the head and fold its existing columns into a center recess with paired shoulder ribs. |

Front/side/top, oblique, low-angle, rider-facing, clay and complete-assembly captures were compared. The former fairing had a concrete layering fault: its dark backing extended outside the ivory face and obscured much of it. The revised ivory surface now defines the broad side silhouette. The first candidate improved the cheek but left the head reading as a flat plate; the second geometry pass corrected that depth and shoulder profile. The original [color/section probe](original_audit.json) is auxiliary: its bright warm-pixel threshold includes other nearby parts and is not a semantic panel segmentation or exact fit certification.

## Surface treatment and preservation

The eight Steering parts share a copied `Legger_Front_Steering_PBR` material. Three new packed 2048-square base-color, roughness and metallic maps patch only the repurposed head/lens UV regions. The isolated CPU EMIT bake starts from the old atlas pixels with clearing disabled. An earlier GPU trial reported insufficient memory and is excluded from final proof. Normal and AO maps are retained; no rebake of normal detail or automatic fidelity guarantee is claimed.

[Decoded texture comparison](texture_patch_qa.json) finds **zero channel difference across 3,861,098 pixels outside the patch plus an 8-pixel guard**, in each revised map. Base color remains sRGB; roughness/metallic remain Non-Color. Existing grain, seams and restrained wear remain available. Recontouring changes local texture density, and very small design details remain simplified.

[Preservation](source_preservation.json) verifies all **999 preceding mesh versions** and **191 preceding packed image payloads** by their fingerprints/bytes. Eleven hidden pre-pass copies preserve the four changed forms and the eight material users (one overlaps). Native totals including hidden history become **1,010 mesh objects and 194 packed images**. Seventy-two active parts have exact vertex coordinates, and sixty-five retain complete mesh/material-name/world fingerprints. All active connectivity, UV coordinates, modifiers and transforms are unchanged. All thirty leg meshes, joint targets, rider pocket, bar/grip/lever geometry, foot contacts, cargo, chassis and recreated hoses remain unchanged.

## Verification and review

[Native QA](native_qa.json) and [canonical inspection](native_canonical.json) passed after independently reopening the saved source. Active controls remain closed, nondegenerate all-quads without detected loose vertices or boundary/nonmanifold edges. Every connected control component in the four changed meshes has positive signed volume. The two front fairings mirror within three micrometres; all eleven assembly roots pass independent movement checks. These are static authoring checks, not complete Boolean self-intersection, engineering-load, rig, animation-sweep, dressed-rider or Unity validation.

[Views](views.json) bind **34 renders**: sixteen original/before views, eight first-checkpoint views, eight final-candidate views and two reopened-native views. Labeled sheets, selected full-size details, and `Legger_Front_Comparison.png` were visually inspected. [Matched render comparison](render_comparison.json) checks the independently reopened front oblique and full assembly against the candidate. [Image identities](image_manifest.json) bind 38 local images; [visual review](visual_review.json), [local recipes](local_recipe_manifest.json), and [verification](verification.json) bind decisions and provenance. Review cache: `C:/Users/kbkno/.codex/art-cache/LeggerFront20261016/`.

Work used hidden Blender 5.2.2 LTS workers with auto-execution disabled. No foreground activation or GUI reload/save occurred. Reopen the saved source before saving from an older loaded Blender session. No Unity asset, metadata, package, export, rig, collider or gameplay test changed. Deterministic world identity, streaming/unload/reload, generated runtime identity and persisted deltas are inapplicable because this task changes source art only; original design, scale, budget, pose and rider constraints apply.

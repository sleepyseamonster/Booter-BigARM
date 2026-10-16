# Legger neutral leg alignment

**Subsequent checkpoints:** the [sci-fi pass](../LeggerSciFi20261015/README.md) initially preserved every neutral target below. The later [three-section leg rework](../LeggerLegRework20261015/README.md) identifies the current native source and deliberately replaces those old two-link numeric targets with hip/knee/hock/ankle positions. Matching left/right geometry, equal segment lengths, shared bends and forward feet remain the rest-stance requirement. This receipt's hashes, counts and unchanged-body statements describe the earlier alignment-only checkpoint.

October 15, 2026. The author requested that both legs line up to make rigging easier. This supersedes preservation of the previous staggered leg pose and its joint coordinates. The [sci-fi form audit](../LeggerSciFiAudit20261015/README.md) is complete; the broader shape overhaul remains separate from this applied alignment pass.

## Saved result

The same source, `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, now has matching left/right hip, knee and ankle positions, common thigh/shin lengths, forward-facing feet side by side, and a small matching knee pre-bend. The feet sit directly below the hips in side projection. Joint heights, overall height, ground plane, body, saddle, controls and cargo remain unchanged. A 55 mm knee offset supplies a deliberate bend direction while keeping the stance almost straight.

| Joint | Left position, metres | Right position, metres |
| --- | --- | --- |
| Hip | (-0.225, -0.300, 1.320) | (0.225, -0.300, 1.320) |
| Knee | (-0.260, -0.245, 0.800) | (0.260, -0.245, 0.800) |
| Ankle | (-0.275, -0.300, 0.170) | (0.275, -0.300, 0.170) |

Thighs are approximately 0.5241 m and shins 0.6326 m between their centers on both sides. Previously the staggered chains had unequal segment lengths. The control geometry was refitted along each segment axis to common neutral lengths, preserving thickness directions, existing UV coordinates, material assignments and separate barrel/rod parts. Joint housings and feet translate rigidly. Both hip mounts were refitted to the new bearings while retaining their chassis anchors; their bearing bosses remain round. This is a source-mesh rest-stance preparation, not posing an existing armature.

The change affects 24 leg meshes and two hip mounts. The other 41 active meshes have matching geometry/material/UV/world-transform fingerprints. Active counts remain **67 major meshes, 21,396 quad control faces and 86,072 evaluated triangles**. Existing 11 materials and 55 packed active maps are retained; no texture was generated or rebaked. Atlas coordinates stay fixed, so surface patterns follow the refitted geometry; this does not certify equal physical texel density. The planned form overhaul will require appropriate new UV/bake work.

The complete model still opens in scene `Legger`, view layer `LEGGER - COMPLETE`. Leg roots record their neutral joint targets, and chassis hip-position/inspection metadata now agrees with the saved scene. Bounds are approximately **0.866 × 1.448 × 2.226 m**; depth decreases because the feet are no longer staggered. There is no global scale change.

## Identity and verification

[Application receipt](application.json) records an atomic update of the same authoring file from SHA-256 `c5eadd0501631bbace05551e1453c43a14db55df1365b07a77755cbf80b51be5` to **`fa71944d8d6292448418810cce37c89b4997cf565b23192dabd656f251a20538`**. The final file is 747,815,765 bytes. A verified pre-alignment recovery remains local at `C:/Users/kbkno/.codex/art-cache/LeggerChassis20261015/TexturedModular.blend`. No extra active `.blend` file was added to the source directory.

[Alignment proof](alignment.json) records old/new coordinates, changed/unchanged meshes, segment lengths and bounds. [Native reopen checks](native_qa.json) verify the actual saved file: one scene/view layer, complete visible assembly, unit scale, closed manifold quad control meshes with no detected loose vertices or degenerate faces, unchanged evaluated budget, grounded feet, matching joint targets, independent movement of all eleven assembly roots, UV0 layout and packed-image payloads. All 504 meshes from the previously accepted source remain accounted for in original/reference copies. All 20 cargo map payloads remain byte-identical to their accepted checkpoint.

Five actual candidate renders were opened and inspected: assembled three-quarter, front, side, isolated legs and a hip-mount close view. A three-view board was also inspected. [Image manifest](image_manifest.json) identifies six local PNG files under `Logs/LeggerLegAlignment20261015/`; `Legger_Neutral_Alignment.png` is the board. The final metadata cleanup changed only authoring notes; a combined digest verifies identical active geometry, UVs, material assignments, assembly transforms and all packed image payloads against the rendered candidate (`e003bacf20cc9f44ffa01c37000d7e2d09af2dc0a1f1d4106d552c9243563699`). It did not alter the reviewed appearance.

Processing and reopen checks used hidden Blender 5.2.2 LTS processes. Review used Cycles/OPTIX, 32 samples and fixed studio lighting at 1400 × 1400. No application was focused or refreshed. An already-open Blender window may still show its previous in-memory version: use **File > Revert before saving** to load the saved alignment.

This pass does not add an armature, skinning, joint constraints, collision, animation, export or Unity integration, and does not prove motion clearances. It does not apply the planned sci-fi shape overhaul. No gameplay smoke test ran. World identity, streaming, generated-object identity and persisted runtime deltas are inapplicable because only the local source-model rest stance changed. Authored constraints are the scale, joint heights, ground contact, rider/body/cargo arrangement and separate major parts. Native files, recovery sources and working renders remain local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

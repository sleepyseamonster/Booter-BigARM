# Legger detail refinement

The author's subsequent request for improved manufactured forms and clean quads produced the [clean quad rebuild](../LeggerQuadRebuild20261015/README.md), which now describes the visible asset in the same authoring file. The geometry and hashes below remain the preceding baked reference, retained hidden inside that file.

October 15, 2026. Owner: Blender under Gottspan coordination. The author asked whether the preceding bake covered the whole robot and authorized continuing the texture/geometry work. The [preceding checkpoint](../LeggerFullDetailBake20261015/README.md) already covered the frame, both legs and the cargo/saddle. This continuation reduces the frame and legs again while retaining the accepted cargo geometry.

## Applied checkpoint

The current local-only source remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`. No second model file is added to its authoring folder.

| Active object | Previous triangles | Current triangles | Packed maps |
| --- | ---: | ---: | ---: |
| `Legger_Frame_Baked` | 99,999 | 89,999 | 7 |
| `Legger_LeftLeg_Baked` | 30,000 | 27,000 | 7 |
| `Legger_RightLeg_Baked` | 30,000 | 27,000 | 7 |
| `Legger_CargoSeat_Baked` | 12,000 | 12,000 | 7 |
| Active total | 171,999 | 155,999 | 28 |

This removes 16,000 active triangles, approximately 9.30% overall. The hidden 2,667,021-triangle `Legger_DetailBake_Source` remains inside the same file for rebaking and is excluded from the active count and intended export. Export only the four named active objects; exclude the detailed source and scale mannequin. Artist transforms, scale and pivot are unchanged. No rig, animation loops, collider, LOD, runtime texture budget or Unity readiness is claimed.

## Geometry and surface work

A uniform 10% trial removed too much geometry around cargo hardware and case edges. It was not applied. The accepted cargo mesh and UV coordinates remain exactly unchanged. Frame and leg reduction additionally protects sensitive surface features: vertices farther than 1.5 mm from the trial surface, and vertices of affected triangle centers, seed extra pinned supports. Corresponding reverse checks also add support near changed target surfaces. Pins expand by one edge ring; the original boundary/non-manifold support ring stays pinned. Frame converged in three trials, left leg in three, and right leg in two, while reaching their reduction budgets.

The [preparation receipt](geometry_preparation.json) records exact preservation of pinned coordinates and the maximum tested distances for every adaptive trial. Its tests inspect all vertices and triangle centers in both directions for each changed region. They do not establish an exhaustive continuous-surface or Hausdorff bound. [Independent geometry checks](geometry_qa.json) include seeded surface samples, topology counts, finite UV coordinates in 0–1, world bounds and transform equality. The asset retains inherited open/non-manifold edges; this pass does not certify a repaired or watertight mesh.

All four regions receive freshly baked Base Color, Roughness, Metallic, tangent Normal, object Normal, AO and Height maps from the original detailed source and its material. Frame and leg atlases are refreshed for their new geometry; the cargo atlas is retained. Frame color and tangent-normal masters are 8192 square; the other 26 maps are 4096 square. The cargo's three new auxiliary maps increase the active map total from 25 to 28.

The four PBR channels feed the visible shader. AO, object normal and height are retained as unconnected authoring/export references. Color is sRGB; the data maps are Non-Color; tangent Normal Map nodes explicitly use their region's atlas. Every active map is packed in the file. These are authoring masters, and their size/atlas packing still needs a deliberate runtime-budget pass.

Blender 5.2.2 LTS / Cycles OPTIX bakes each region in isolation to fit GPU memory. Settings remain cage extrusion 0.018, maximum ray distance 0.07, and 16-pixel Extend padding. Color/data passes use one sample; the AO node uses 16 samples at distance 0.035. The method and official references are recorded in the preceding checkpoint.

Height remains approximate auxiliary data: 0.5 is neutral and one encoded unit represents 0.04 m. Non-finite, uncovered or out-of-atlas samples, projection displacement over 9 cm and normal displacement over 2 cm become neutral. The 32-bit RGB EXRs use lossless ZIP compression; saved decoded float32 values were checked for exact equality. Height is not connected to displacement. See [height validation](height_validation.json) and [projection coverage](coverage_report.json). One of 1,764,564 tested frame 4K core texels lacks white projection coverage; the frame 8K and three other 4K atlases have no uncovered tested core texels. Coverage tests one-pixel-eroded UV interiors and does not certify gutters, mips or every continuous point.

## Verification and preservation

[Mesh/material receipt](final_report.json) records counts, transforms, map dimensions, connections and packed payload hashes. [Eight-view comparison](silhouette_report.json) covers full front, side and rear views plus frame, cargo, legs, feet and underside close-ups under matched studio lighting. These are offline Blender comparisons, not gameplay-camera or Unity validation. Frame/seat and cargo close-ups remain close to the preceding checkpoint. A small triangular shading difference remains on the inner surface of the rear foot in the feet close-up, alongside minor edge changes. This is a review candidate, not zero-loss certification; textures cannot restore removed physical silhouette, parallax or cast shadows exactly.

[Application hashes](application.json) establish that the inspected source was unchanged before atomic replacement of the same file. [Native reopen](native_reopen.json) verifies the applied file, its 28 packed maps, shader/UV/transform state, hidden source and unchanged cargo geometry/UVs. Raw maps, scripts and review images remain in ignored `Logs/LeggerRefine10_20261015/Protected/`; the comparison is `Review/comparison.png`. The rejected initial trial remains separate in its parent cache for diagnosis.

The applied file was rendered again after reopening; [saved-render comparison](saved_render_comparison.json) records the pixel difference from the prepared side view. The left-leg job initially stopped when temporary EXR buffers exhausted available disk space. Regenerable intermediate scenes from the preceding bake cache were removed, the incomplete normal-position buffer was regenerated, and the job resumed using hash-guarded completed maps. Position/normal projection buffers are discarded after their height extraction succeeds. The current authoring file and inspected pre-pass copy were preserved throughout.

The Blender source and caches remain local-only under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md). Only small evidence records are committed. No Unity files or gameplay tests are part of this task. World identity, streaming/unload/reload, generated-object identity, authored placement constraints and saved runtime deltas are inapplicable because this local source-art operation changes no world objects, placement, generation or save data.

No GUI was focused or reloaded. If the file is already open in Blender, use **File > Revert before saving** to load the applied checkpoint rather than overwriting it with the older in-memory scene.

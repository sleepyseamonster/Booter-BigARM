# Legger three-section leg rework

October 15–16, 2026; completed October 16. **Applied and reopened successfully in the existing native scene.** Following the [original leg audit](../../../Design/Art/LEGGER_LEG_GEOMETRY_AUDIT.md), the author requested implementation to improve the legs. This pass restores the short upper section, alternating bends, narrower shafts, continuous ivory armor and broad arched feet while keeping a matched left/right rest stance. Creative acceptance remains with the author.

## Saved source

The sole active authoring file remains `SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend`, **845,205,300 bytes**, SHA-256 **`32e97e86658e12e5655b072c1de40863fdf4ef992ebcc43ed9c7f27fbc989b1d`**. [Application](application.json) binds the update from the [sci-fi baseline](../LeggerSciFi20261015/README.md), hash `fcb639d849c25866508a05b9546175140f44fadaa79a1ff177bc499dc08d1b1f`. Exact recovery remains at `C:/Users/kbkno/.codex/art-cache/LeggerLegRework20261015/Before.blend`; the verified finished candidate and intermediate checkpoints also remain on C:.

The thin-provisioned D: Storage Spaces pool refused a second complete allocation despite NTFS reporting sufficient free space. Failed temporary copies were removed and the baseline restored and hash-checked. The final update reused the existing file allocation, with independently verified C: recovery/candidate, flush and final SHA validation. It was not an atomic replacement. No references or recovery content were discarded; intermediate task review images were copied and hash-verified to C: to reduce D: allocation pressure.

Scene `Legger`, view layer `LEGGER - COMPLETE`, opens as the complete fitted vehicle under `LEGGER - ASSEMBLED MODEL`: **73 editable major meshes**, eleven independently movable module roots, **35,934 quad control faces and 120,056 evaluated triangles**. The previous pass had 123,664 evaluated triangles. [Native checks](native_qa.json), [canonical inspection](native_canonical.json) and [module budget](module_budget.json) describe this exact saved source. All active stored faces are quads, with closed manifold components, unit scale and no detected loose vertices or degenerate base faces. Loop spacing follows curvature; these are not equal-sized squares everywhere. Modifiers/rendering produce triangles.

## Rebuilt forms and targets

Thirty-two meshes were rebuilt: fifteen major parts per leg and two chassis hip mounts. Each leg has hip, upper bend/knee, hock and ankle housings; three separate frames and wrapped shells; middle/lower actuator barrels and rods; and a foot. Small fixed supports are grouped with their parent mesh. One right template was reflected into the left; all sixteen paired parts have zero stored-vertex reflection error.

| Joint | Left X / right X | Y | Z |
| --- | ---: | ---: | ---: |
| Hip | −0.175 / +0.175 | −0.255 | 1.460 |
| Upper bend / knee | −0.205 / +0.205 | −0.395 | 1.205 |
| Hock | −0.255 / +0.255 | −0.210 | 0.680 |
| Ankle | −0.285 / +0.285 | −0.500 | 0.235 |

Coordinates are metres, forward −Y, Z up. Both chains have equal 3D center lengths of approximately **0.2924 / 0.5589 / 0.5320 m** and side-plane slopes **−28.77° / +19.41° / −33.09°** from downward vertical. The old two-link neutral pivot numbers are deliberately replaced; the author's requirement for matching legs, shared bend directions and forward feet is preserved. Root, scene and chassis hardpoint metadata now agree with the new targets. These are newly authored joints guided by uncertain source landmarks, not recovered original rig axes.

Ivory shells wrap around the front/outer sides of tapered dark cores, leaving rear mechanical recesses and actuator channels. A shaped upper-bend shield, distinct hock housing and compact ankle keep the joint hierarchy readable. Feet have long arched forward forks, rear supports, lateral pads and a central cradle cover. Each foot measures **0.680 m along Y × 0.296 m across X**, with matching placement and grounded pads.

Measured evaluated midpoint assembly widths are **0.1314 m middle / 0.0934 m lower**, close to the audit's original R estimates of 0.1342/0.0987 m. Side-plane depths are 0.1585/0.1504 m; the lower section remains deeper than the original's estimated 0.1231 m because of its mechanical layout. These corridor/triangle-plane envelopes are not exact plate widths or mechanical clearance proof. The original asymmetric pose was not copied.

Overall height remains **2.2244136 m**, without global rescaling. New bounds are approximately **0.866 × 1.6405 × 2.2244 m**; the broader feet restore fore/aft extent. Body, drive, saddle, cockpit and cargo geometry retain their prior forms. Eight unchanged frame parts receive the revised frame atlas alongside the refitted mounts.

## Textures and preservation

[Materials](materials.json) records **15 new packed maps**, five each for Frame, LeftLeg and RightLeg at 4096²: base color, tangent-space +Y normal, roughness, metallic and AO. Forty existing active maps remain, giving **55 active maps across eleven PBR sets**. Base color is sRGB; data maps are Non-Color. AO is an isolated-module auxiliary map, not lighting multiplied into albedo. The ivory/dark-metal/brown-leather palette and restrained wear continue. Visible parts each have one render UV atlas.

[Source preservation](source_preservation.json) accounts for **all 736 pre-pass mesh objects** by exact raw geometry, face/material, UV, bevel/sharp attribute and world-matrix fingerprints, and **all 121 pre-pass packed image payloads** byte for byte. Thirty-three active preexisting meshes retain their full fingerprints; eight further frame meshes retain geometry/world placement and have hidden pre-atlas copies. Twenty-six former leg/mount objects and those eight copies are archived under `REFERENCE - Before three-link leg rework`. Forty new procedural surface sources remain under `REFERENCE - Three-link procedural surfaces`. Earlier dense and baked references remain hidden. The complete file contains 136 packed images, including historical resources; only 55 belong to the visible assembly.

[Texture QA](texture_qa.json) finds zero detected control-interior overlap, full sampled evaluated-interior coverage, no empty interior albedo and over 99.9% normalized sampled tangent normals in all three revised sets. No collapsed evaluated UV triangles were detected in those sets. Raster checks skip triangles under four pixels or without an eroded interior; they do not certify every seam/mip. The other eight sets were retained and not newly certified free of their previously recorded microscopic UV defects.

## Verification and scope

Twelve matched geometry/PBR views and comparison/detail boards were visually inspected. [Render comparison](render_comparison.json) records identical alpha silhouettes across those views; mean RGB bake differences remain below 0.45/255, with larger isolated edge differences. The final D: source was independently reopened and rendered: its assembled image differs from the candidate by at most 1/255 RGB and zero alpha. [Image identities](image_manifest.json) and [local recipe/checkpoint identities](local_recipe_manifest.json) bind the local artifacts. Raw renders are in `Logs/LeggerLegRework20261015/`, including the three `Legger_ThreeLink_*.png` boards.

All work used hidden Blender 5.2.2 LTS processes with embedded auto-execution disabled. The user's GUI was not focused or reloaded. An already open Blender window may still contain the previous model: **use File > Revert before saving that window**, or reopen the same source path.

No armature, animation, motion/clearance validation, collider, LOD, Unity import or runtime performance test was added. Fitted closed components are not a Boolean union or engineering certification. Runtime texture packing/downsampling and deformation remain later handoff work. World identity, streaming/unload/reload, generated-object identity and persisted runtime deltas are not applicable because this task only changes a local source asset, with no placement or gameplay system. Authored scale, scene organization and source retention remain accounted for. Native files, textures and render pixels stay local under the [source policy](../../../Operations/LOCAL_ONLY_SOURCE_POLICY.md).

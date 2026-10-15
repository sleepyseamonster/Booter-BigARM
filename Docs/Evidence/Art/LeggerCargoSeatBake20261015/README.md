# Legger cargo and saddle detail bake

This checkpoint was subsequently extended by the [full frame/leg detail bake](../LeggerFullDetailBake20261015/README.md), which now describes the current authoring file. Counts and hashes below record the preceding cargo-only checkpoint.

October 15, 2026. The author requested replacing dense detail geometry on cargo cases, straps, buckles and the saddle with newly generated baked textures, using the supplied close-up as the target area. This changes the current local Blender file in place; it does not start another 10% whole-model decimation pass.

## Current authoring asset

`SourceArt/Blender/Studies/Legger/Legger_Topology_Iteration04.blend` remains the sole model file in its authoring folder. It contains:

| Object | Role | Triangles |
| --- | --- | ---: |
| `Legger_Body` | Untargeted robot surfaces, original material/UVs | 2,084,137 |
| `Legger_CargoSeat_Baked` | Reduced cargo and saddle surfaces with new PBR material | 12,000 |
| `Legger_DetailBake_Source` | Complete pre-bake robot source, hidden and excluded from rendering/export | 2,667,021 |

Active robot geometry is 2,096,137 triangles. The affected surfaces originally had 582,884 triangles; the new surfaces remove 97.94127% of their triangle count. Major forms, projecting parts and some boundary/support geometry remain in the mesh. Small relief is represented in the baked maps. This is a bounded surface optimization, not a completed game-ready or animation-ready whole robot.

The complete detailed robot is retained inside the same file in `BAKE_REFERENCE - hidden - not for export`. It increases authoring-file size without being part of the active robot. Export only the named body and baked-surface objects; exclude the bake reference and scale-reference mannequin.

## Modeling and bake method

The input was one connected mesh. Cargo regions were identified by world-space face centers with `abs(x) > 0.20`, `y > 0.12`, `z > 1.35`; saddle/seat regions use `abs(x) < 0.15`, `-0.28 < y < 0.70`, `z > 1.89`. The region preview was inspected before reduction. Boundary and untargeted vertices receive zero collapse weight, and only interior region vertices can collapse. Original vertex-index attributes establish an exact comparison with the retained source: all untargeted vertex positions match exactly. Those temporary attributes and reduction groups are removed from the final meshes.

A fresh `CargoSeat_Atlas` UV layer uses Smart UV Project with a 78 degree angle, 0.0015 island margin and area weight 0. The final layout occupies approximately 27.50% of the atlas; the many small preserved boundary patches limit packing efficiency. This is an improvement over the first test's 11.62%, not an optimal production UV layout.

New 4096-square Base Color (sRGB), Roughness, Metallic and tangent-space Normal (Non-Color) maps are baked from the source geometry and original packed material. These are newly generated technical texture maps derived from the model, not an unrelated visual redesign or AI-painted replacement. The normal map is connected through a Tangent Normal Map node using `CargoSeat_Atlas`; the other maps feed the matching Principled inputs. All four maps are packed in the current `.blend`; external cache PNGs are not required to open it.

Blender 5.2.2 LTS / Cycles OPTIX on the installed RTX 3060 Ti performs selected-to-active baking with one sample, 0.018 extrusion, 0.07 maximum ray distance, and final 16-pixel Extend padding. A white-emission coverage bake checks projection coverage. Final coverage is 100% of 4,149,601 tested one-pixel-eroded UV interior texels. The padding step, rather than invented texture painting, resolves the tiny missed areas exposed by the tighter-padding trial. The coverage test does not prove perfect projection at every seam, mip level or view.

References: [Blender baking manual](https://docs.blender.org/manual/en/latest/render/cycles/baking.html) and [Blender collapse implementation](https://github.com/blender/blender/blob/main/source/blender/bmesh/tools/bmesh_decimate_collapse.cc). The installed modifier behavior and resulting mesh were checked locally; a current web source alone is not version proof.

## Verification and application

See [final mesh/material report](final_report.json), [application hashes](application.json), [native reopen](native_reopen.json), and [four-view silhouette measurements](silhouette_report.json).

Source and baked materials were rendered under matched studio lighting in side, rear, saddle and cargo close-up views. The final packed asset was reopened and rendered again in side/cargo views. Review retains recognizable case profiles, orange straps, hardware, seams, wear and saddle ribs. Texture maps do not recreate physical protrusions, self-occlusion or cast shadows exactly. Close-view silhouette overlap spans approximately 99.40–99.77%; the small silhouette changes are greater than in the earlier conservative decimation-only passes. This is not a zero-loss certification, and close inspection of handles, case corners and grazing views remains appropriate.

The prepared asset was saved and reopened with four packed 4K material maps, matching world transforms, the original body UVs, the new atlas, and the reference hidden from rendering and viewport selection. Application compares the current file's SHA-256 with the inspected source, copies the verified prepared file to a sibling temporary path, verifies the copy, and replaces the current file atomically. Raw evidence and local execution scripts remain under ignored `Logs/LeggerBake20261015/`; the final preview is `Review_Final/comparison.png`.

No existing GUI session was reloaded, focused or changed. The already-open Blender window still holds its old scene: use **File > Revert before saving from that window** to load the updated file. Blender sources, packed maps and cache outputs remain local-only; only these small receipts are committed. No Unity import, runtime prefab, collision, rig or gameplay test is claimed.

World identity, streaming/unload/reload, generated-object identity, authored placement constraints and persisted runtime deltas remain inapplicable: this local source-mesh/material operation does not modify scene placement, actors, terrain, generation or save data. The world transform, author scale and ground origin are retained.

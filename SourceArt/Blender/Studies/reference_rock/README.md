# Fractured reference rock — 2026-10-01

User brief: model the supplied rock in the existing scene with minimal faces; use textures and materials for fractures, cracks, and chips, and simplify the silhouette.

## Delivered asset

- `ReferenceRock_FracturedSlate_01`: **74 triangles, 39 source vertices, one material**, one UV set, no modifiers. Closed mesh, no degenerate faces or nonfinite coordinates.
- Approximately 2.85 × 1.60 × 3.32 metres; bottom pivot at the origin. Tall left crest, descending shoulder, and two broad silhouette breaks. Small chips and fissures are surface shading only.
- Three 2048 × 2048 maps: base color (sRGB), tangent normal (OpenGL), and roughness (linear). Packed into the Blender asset and embedded through the GLB material. No displacement or runtime procedural shader is required.
- Added to [the existing study](../BrokenWorldBadlandsStudy.blend) at approximately X=0, Y=-6, grounded against terrain samples, with the new object selected for review.

| File | Purpose |
| --- | --- |
| [ReferenceSlate.blend](ReferenceSlate.blend) | Standalone editable source |
| [ReferenceSlate.fbx](ReferenceSlate.fbx) | Static mesh export; maps supplied in `textures/` |
| [ReferenceSlate.glb](ReferenceSlate.glb) | Portable mesh with embedded PBR material |
| [In-scene view](ReferenceSlate_InStudy.png) | Render from the saved working scene |
| [Neutral beauty](ReferenceSlate_Beauty.png) / [geometry](ReferenceSlate_Geometry.png) | Material and silhouette review |
| [Back](ReferenceSlate_Back.png) / [elevated](ReferenceSlate_Elevated.png) | Other inspection angles |
| [Validation report](validation_report.json) | Saved source, export, and scene checks |

Both exports were reimported in Blender and retain 74 triangles, dimensions, one UV layer, and one material. GLB splits vertices at UV/normal boundaries (196 imported vertices); its raw boundary-edge count reflects those splits, not holes in the source mesh. FBX reimports with 39 mesh vertices; an engine may split them during import.

Preservation checks against a copy of the live scene made before this addition found all 879 existing objects unchanged in the checked transforms, geometry, material assignments, visibility flags, and light/camera settings. Exactly one object was added. The saved scene was rendered and visually inspected.

## Material provenance and limits

`Reference.png` is the user's supplied image. `textures/FracturedRock_SurfaceSource.png` was generated with the imagegen skill using that image as a material reference. Blender box-projected this image for authoring, then baked a UV atlas. Roughness and normal relief are estimates derived from image values, not calibrated scan data. The generated image may retain apparent lighting. The final asset has no dependency on a procedural box-projection shader.

Generation prompt:

> Create a square seamless game material BASE COLOR texture based on the gray fractured rock in the reference. Output ONLY an edge-to-edge orthographic flat rock surface texture, no object silhouette, no background, no text. Neutral even diffuse illumination, remove directional cast shadows and specular highlights. Broad irregular charcoal-gray slate stone plates, angular intersecting hairline and medium cracks, chipped pale gray edges, subtle warm beige dust trapped in cracks, small mineral flecks. Reference material character and fractured planar geology, with believable fine photographic detail. About 6 to 10 broad fracture plates across the texture, finer fissures within them. No orange veins, no clay, no rounded cobblestones. Seamless repeat left/right and top/bottom. This image will be mapped onto a low-poly rock and combined with separate normal/roughness maps.

Unity import and runtime acceptance are not included. For URP, assign base color as sRGB and import the normal texture as a normal map; convert roughness to smoothness (`1 - roughness`) and pack it for the chosen shader. Verify tangent orientation, texture compression, and visual scale in the target renderer. No collider or LOD asset was authored. Texture resolution can be reduced after checking the actual camera distance.

## Reproduction and scope

`build_reference_rock.py` runs in a separate background Blender process and regenerates this standalone asset, maps, exports, and isolated review images. It resets its process to an empty scene: **never run it inside the live study**. `add_reference_rock_to_study.py` appends the asset once to the live study and guards against duplicates; save through the live editor. `render_in_study.py` creates a temporary review camera without saving. `verify_reference_rock.py` reopens the source and exports, optionally comparing `--baseline` and `--study` files.

This is a source-art delivery under `Docs/`; no Unity production assets were changed. Deterministic world identity, streaming, generated-object IDs, placement rules, and persisted deltas remain with Unity World Creator and are not applicable to this static source-mesh addition. Existing formation-rock lumpiness remains unresolved; this new rock does not replace those formations. The broader landscape goal remains paused.

The saved packed study is approximately 109 MB. It needs a compressed Blender save or an approved large-file storage path before GitHub publication. The user's live Blender session changed after verification, so no further save was attempted. This delivery is committed locally only.

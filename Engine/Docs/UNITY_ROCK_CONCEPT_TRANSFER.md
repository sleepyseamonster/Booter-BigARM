# Unity rock generator concept transfer

**Implementation update:** the [native v3 first pass](./ROCK_TRANSFER_RESULT.md) now supplies layered materials, editable fused volumes, and basic formations. The comparison below records the pre-transfer baseline.

Source audit: 2026-09-10. The user requested carrying the Unity generator's functionality and textures into the native engine. This comparison reads the existing project; it does not run or modify Unity. The source references below describe implementation, not proof that every feature was active in the last Unity scene.

## Finding

The Unity workbench builds rocks from editable additive and subtractive volumes, with seeded mass layouts and an implicit mesher. The current native v2 generator samples a fractured, rounded radial body. Both are deterministic procedural generators, but the native implementation does not yet reproduce the Unity volume-authoring model or its full layered material response.

| Concept | Unity source | Native state and transfer action |
| --- | --- | --- |
| Shape controls | `TopDown3DRockWorkbenchAuthoring.cs`, `TopDown3DRockWorkbenchBaseRockGenerator.cs`: independent width/height, lopsidedness, compaction, major fractures, edge damage and seeded source volumes | Seed/radii/distortion/bands exist. Add versioned volume plans and matching artist controls without changing existing v1/v2 recipes. |
| Fused geometry | `TopDown3DRockWorkbenchMesher.cs`: smooth union of additive fields, subtraction, grouped fields, tetrahedral polygonization, relaxation and orientation checks | Native radial sampling cannot express the same editable internal cuts or multi-mass topology. Port the field/meshing concepts into a separate generator version; route accepted meshes through existing collision/export. |
| Surface appearance | `BrokenWorldRockWorkbenchPBR.shader`: base/top/grit/underside texture sets, crack/halo/mineral masks, dust, patch variation, worn shine, formation seams | Native `fs_scene.sc` uses one triplanar color/normal/surface set. Add layered material recipes and equivalent surface selection/blending. Preserve map meaning and actual serialized bindings. |
| Formation planning | `TopDown3DRockWorkbenchFormationGenerator.cs`: connected outcrops, scattered layouts, piles, ridge pillars, hoodoos, support/terrain seating and member seeds | The native single-rock recipe is not a formation recipe. Bring deterministic member planning and grounding after the individual rock/material path is working. |
| Preview and output | Unity preview/editor/baker tools turn plans into meshes/material assignments | Keep the existing native apply/undo/save, LOD, collision and export loop; extend that loop rather than introducing a second workbench. |

Unity source paths are under `Assets/_Project/Scripts/Editor/TopDown3D/`, `Assets/_Project/Scripts/Runtime/TopDown3D/`, and `Assets/_Project/Shaders/TopDown3D/`. Native anchors: [generator](../Source/World/Rocks/RockGenerator.cpp), [recipe](../Source/World/Rocks/RockGenerator.h), [shader](../Shaders/fs_scene.sc), [workbench](../Source/Tools/RockWorkbench.cpp).

## Texture preservation is complete for the audited family

Fresh `python3 Tools/verify_surface_assets.py --sources` passed: 38 texture images, five source-art files, 101,692,582 bytes, 15 material records, 186 resolved serialized texture bindings, and 102 unchanged source records. A separate recursive image inventory of `Assets/_Project/Art/Environment/Rocks/` and `Assets/_Project/Art/Environment/Ground/SandDirt/` found 43 files, all present in the transfer manifest; none were missing.

The [surface library](../Assets/SurfaceLibrary/README.md) preserves originals, [provenance/import settings](../Assets/SurfaceLibrary/manifest.json), and [material bindings](../Assets/SurfaceLibrary/material-reference.json). Do not generate replacements or infer bindings from filenames. The observed material family reuses side albedo in several color slots while using different normal/surface maps. Packed surface R/G/B means AO/roughness/height; crack R/G/B means crack/halo/mineral deposit. These are not generic ORM maps.

Copied assets are not full shader parity. The native shader already uses triplanar color, normal and AO/roughness data, but top/underside/grit blending, crack-mask interpretation and dust/patch response still need implementation. Height files are preserved; that alone does not create displacement. Source PSDs remain editable sources rather than runtime textures.

## Implementation sequence

1. Extend the native material recipe/bindings to express the existing layered rock family, then port top/side/underside, grit, crack and dust behavior into the native shader. Use a fixed rock and light for a bounded visual comparison.
2. Add a new versioned rock recipe with seeded additive masses and subtractive cuts. Port volume evaluation and meshing, with width/height, compaction, asymmetry and fracture controls connected to the existing workbench. Keep existing recipe output reproducible.
3. Connect generated meshes to existing LOD, collision and export; check one accepted example end to end before expanding styles.
4. Add deterministic formation member plans and terrain seating for open Greater Wasteland spaces. Canyons remain deferred.

Before implementation, preserve world seed and generator-version identity, stable member IDs, authored constraints, regeneration after chunk reload and persisted runtime deltas. Texture resource handles must remain transient; recipes refer to stable material/asset IDs. Source audit and texture preservation create no world objects or saved gameplay changes. Matching concepts does not promise identical Unity/C++ random sequences or pixel-identical output under different lighting.

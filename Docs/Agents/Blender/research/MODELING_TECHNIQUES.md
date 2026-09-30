# Modeling techniques for Booter & BigARM

Research baseline: 2026-09-30. These are method choices to test against actual game assets, not universal polygon budgets or a locked art style.

## Select the method from the asset's job

| Asset need | First approach | Check before accepting |
| --- | --- | --- |
| Readable rocks, debris, and bulky props at elevated camera distance | Block out major masses first; use controlled low-poly facets or limited bevels | Silhouette, exposed faces, underside, repeated-instance cost |
| Manufactured hard-surface parts | Bevel important edges non-destructively; control sharp edges and face normals | Highlights at game distance, shading splits, evaluated triangle count |
| Organic or damaged forms needing fine surface detail | Sculpt a high-resolution source, make a simpler game mesh, then bake detail | Bake artifacts, texture resolution, silhouette loss |
| Repeated modular kit | Agree on units, pivot, mating edges, and material set before detail | Seam fit, rotation variants, material consistency |

Blender's Bevel modifier is non-destructive and can cooperate with weighted normals. Use it where edge highlights improve readability, then inspect the evaluated mesh because modifiers can increase geometry. [Blender Bevel modifier](https://docs.blender.org/manual/en/5.1/modeling/modifiers/generate/bevel.html)

Decimation is useful for dense sculpted or subdivided results; it is not automatically helpful for carefully built economical meshes. Preserve silhouette, UVs, and bake quality when reducing geometry. [Blender Decimate modifier](https://docs.blender.org/manual/en/4.3/modeling/modifiers/generate/decimate.html)

Mark seams to guide UV unwrapping; iterate seam placement against distortion and visible texture joins. Keep enough padding for mipmaps when baking texture maps. [Blender UV seams](https://docs.blender.org/manual/en/5.2/modeling/meshes/uv/unwrapping/seams.html), [Blender baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html)

For high-to-low baking, use a UV-mapped target and selected-to-active setup, inspect ray distance or a cage, and verify the baked normal map in Unity. [Blender baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html), [Unity normal maps](https://docs.unity3d.com/6000.1/Documentation/Manual/StandardShaderMaterialParameterNormalMap.html)

FBX is a practical exchange format for Unity. Axis conversion, scale, transforms, smoothing, and modifier application are export decisions that must be proven with one imported sample before setting a project convention. Keep the `.blend` as the editable source. [Blender FBX export](https://docs.blender.org/manual/en/5.3/files/import_export/fbx_legacy.html), [Unity model import settings](https://docs.unity3d.com/6000.0/Documentation/Manual/FBXImporter-Model.html)

## Game-specific quality target

Build shape language around the approved asset brief and `Docs/WORLD_BASIS.md`. For terrain decoration, inspect from the actual elevated camera and from multiple rotations. A model that looks good in Blender close-up can become visual noise when scattered across streamed chunks. Define triangle, material, texture, and draw-call budgets from the intended instance density and measured Unity evidence, not from a generic asset rule.

For a procedural world, a model is reusable visual data. The world system supplies deterministic placement and stable identity; the model should support authored constraints such as footprint, orientation, collision intention, and variation roles. The source mesh itself does not own chunk streaming or save deltas.

## Learning loop

For each first-of-kind asset: record the brief, create a small source sample, inspect front/side/top and game-camera silhouettes, run mesh checks, export once, inspect in Unity, note failures, then refine this page or the SOP with measured evidence. Do not record an untested technique as proven.

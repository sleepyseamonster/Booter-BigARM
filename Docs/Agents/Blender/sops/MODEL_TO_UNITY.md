# Model to Unity SOP

1. **Preflight.** Read the asset brief, root rules, relevant live Unity scale/import references, and Git status. Identify task-owned files. State the five procedural integration concerns in the brief before implementation.
2. **Block out.** Set units and a meaningful ground/contact pivot. Compare dimensions with a live Unity reference. Establish a recognizable silhouette at the intended elevated camera distance before surface detail.
3. **Build.** Choose topology for the deformation or static use. Use modifiers deliberately. Inspect front, side, top, and underside; check normals, loose geometry, self-intersections where relevant, and visible shading. Non-manifold edges may be intentional for open surfaces, so review rather than auto-fix them.
4. **Surface.** Assign a restrained material set. Make UVs where textures or bakes need them; check stretching and padding. Use high-to-low baking only when visual gain justifies its source and texture cost. Save editable sources before applying destructive steps.
5. **Inspect.** Run `tools/inspect_blend.py` on a saved `.blend` if Blender is available. Review the JSON and inspect the model visually. This script is advisory: it does not detect every geometric or artistic defect.
6. **Export.** Export only named game meshes from a clean source collection. Record Blender version, format, transform/axis/scale settings, evaluated triangle count, texture dependencies, and export path. Keep source and runtime export in separate stable folders under `Assets/_Project/Art/`.
7. **Unity handoff.** Ask Babineaux to verify actual import scale, pivot, orientation, materials, normals, collision, prefab use, and elevated camera readability. For repeated procedural assets, validate density and streaming cost in the intended world lane when that work is authorized.
8. **Closeout.** Report source/export paths, check results, visual evidence, Unity proof or its absence, and any remaining art decision. Preserve unrelated dirty files and stage only task-owned files for a verified commit.

Do not import an asset into the Unity production scene merely to complete a Blender task. Do not focus the user's Unity window during normal repository work.

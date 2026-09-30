# Broken World badlands environment study

This is a **provisional Blender scene**, built for visual review before Unity integration. It has one bounded, mostly flat badland, shallow dry washes, modest rough rises, clustered rock formations, scattered stones, and sand/gravel/rock ground layers. It uses the repository's existing ground and rock albedo textures. No terrain, placement, material, or save authority in the Unity project changes.

## Files

- `build_badlands_study.py` creates and renders the scene deterministically in Blender 5.2.2 LTS.
- `BrokenWorldBadlandsStudy.blend` is the editable result with source textures packed.
- `BrokenWorldBadlandsStudy.png` is the first camera review image.
- `build_badlands_rock_detail_study.py` builds a temporary rock-detail source in the system temporary directory.
- `apply_rock_detail_to_open_study.py` applies that source to the currently open study without replacing the scene or its edited lights and camera.
- `BrokenWorldBadlandsRockDetailStudy.png` and `BrokenWorldBadlandsRockDetailCloseup.png` show the rock-detail pass and a formation close-up.
- `apply_badlands_realism_to_open_study.py` applies a ground, placement, and composition pass to the same open `.blend` without resetting the user's edited sun.
- `BrokenWorldBadlandsRealismStudy.png` is a review render from the saved working file after that pass.

Run from the repository root with the Steam Blender executable on this macOS host:

```sh
"$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" --background --python-exit-code 1 --python Docs/Agents/Blender/studies/build_badlands_study.py

# Rebuild the temporary rock-detail source and review images:
"$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" --background --python-exit-code 1 --python Docs/Agents/Blender/studies/build_badlands_rock_detail_study.py
```

The source script rebuilds its output files and is not an in-place editor for user changes made inside the `.blend`. Save manual revisions under a new name or update the source script deliberately.

The user-facing working file remains `BrokenWorldBadlandsStudy.blend`. The rock-detail pass was applied to that same open file, preserving its edited sun and view. It adds denser rock meshes, broken silhouettes, shaped tops, smoother side normals, box-projected albedo, fine bump detail, and a soft fill light. The separate review render also tries slightly deeper rock grounding; the in-place update preserves the user's rock transforms. These meshes have not been optimized or imported for Unity.

The next in-place pass expanded the terrain from 90 m to 180 m, smoothed its surface, reduced the ground material's orange saturation, mixed in broad gravel variation, settled 145 existing rocks, varied five buttress positions and heights, and added 100 small talus fragments near formations. It added an interior review camera while retaining the original elevated camera. The live scene was saved in the same `.blend`. The render still shows a finite study boundary at its upper corners, and the ground remains a provisional material study rather than a finished texture set.

## Review gate

Assess terrain shape, paths, rock silhouette, texture scale, repetition, and value/color separation from the elevated game-like camera. This render is a composition study, not a measured Unity match. Approved components should be split into reusable meshes and texture assets and checked through Babineaux's Unity import path. Production World Creator remains the sole terrain and placement authority.

Procedural integration: the study uses a fixed seed only for reproducibility. It has no runtime world identity, chunk unload/reload behavior, stable generated-object IDs, placement constraints, or persisted deltas. Those remain owned by existing Unity systems and must be addressed during any approved asset integration.

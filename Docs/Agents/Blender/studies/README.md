# Broken World badlands environment study

This is a **provisional Blender scene**, built for visual review before Unity integration. It has one bounded, mostly flat badland, shallow dry washes, modest rough rises, clustered rock formations, scattered stones, and sand/gravel/rock ground layers. It uses the repository's existing ground and rock albedo textures. No terrain, placement, material, or save authority in the Unity project changes.

## Files

- `build_badlands_study.py` creates and renders the scene deterministically in Blender 5.2.2 LTS.
- `BrokenWorldBadlandsStudy.blend` is the editable result with source textures packed.
- `BrokenWorldBadlandsStudy.png` is the first camera review image.

Run from the repository root with the Steam Blender executable on this macOS host:

```sh
"$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" --background --python-exit-code 1 --python Docs/Agents/Blender/studies/build_badlands_study.py
```

The source script rebuilds its output files and is not an in-place editor for user changes made inside the `.blend`. Save manual revisions under a new name or update the source script deliberately.

## Review gate

Assess terrain shape, paths, rock silhouette, texture scale, repetition, and value/color separation from the elevated game-like camera. This render is a composition study, not a measured Unity match. Approved components should be split into reusable meshes and texture assets and checked through Babineaux's Unity import path. Production World Creator remains the sole terrain and placement authority.

Procedural integration: the study uses a fixed seed only for reproducibility. It has no runtime world identity, chunk unload/reload behavior, stable generated-object IDs, placement constraints, or persisted deltas. Those remain owned by existing Unity systems and must be addressed during any approved asset integration.

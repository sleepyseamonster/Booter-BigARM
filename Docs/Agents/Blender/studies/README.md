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
- `expand_badlands_authoring_area.py` extends the terrain in the currently open working file while copying every existing center vertex and its ground mask color.
- `BrokenWorldBadlandsExpandedOverview.png` is the full-area review render from the saved working file.
- `references/BadlandsPaletteAndDebrisReference.png` is the user-provided visual reference for color, rough rock, and debris buildup. Its tall buttes and deep canyons are not the current shape target.
- `references/BadlandsWideHorizonPanorama.png` is the user's second reference and exact source for the panoramic World environment.
- `apply_reference_debris_to_open_study.py` adds the reference-inspired rubble pass once to the already open working file.
- `apply_wide_horizon_panorama_to_open_study.py` maps the second reference into the World of the already open working file and adds a wide terrain review camera.
- `BrokenWorldBadlandsReferenceDebrisClose.png` is a close render from that saved working file.
- `BrokenWorldBadlandsWideHorizon.png` is the wide review render from the saved file.

Run from the repository root with the Steam Blender executable on this macOS host:

```sh
"$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" --background --python-exit-code 1 --python Docs/Agents/Blender/studies/build_badlands_study.py

# Rebuild the temporary rock-detail source and review images:
"$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" --background --python-exit-code 1 --python Docs/Agents/Blender/studies/build_badlands_rock_detail_study.py
```

The source script rebuilds its output files and is not an in-place editor for user changes made inside the `.blend`. Save manual revisions under a new name or update the source script deliberately.

The user-facing working file remains `BrokenWorldBadlandsStudy.blend`. The rock-detail pass was applied to that same open file, preserving its edited sun and view. It adds denser rock meshes, broken silhouettes, shaped tops, smoother side normals, box-projected albedo, fine bump detail, and a soft fill light. The separate review render also tries slightly deeper rock grounding; the in-place update preserves the user's rock transforms. These meshes have not been optimized or imported for Unity.

The next in-place pass expanded the terrain from 90 m to 180 m, smoothed its surface, reduced the ground material's orange saturation, mixed in broad gravel variation, settled 145 existing rocks, varied five buttress positions and heights, and added 100 small talus fragments near formations. It added an interior review camera while retaining the original elevated camera. The live scene was saved in the same `.blend`. The render still shows a finite study boundary at its upper corners, and the ground remains a provisional material study rather than a finished texture set.

The current working scene is now 360 m × 360 m, four times the previous area. The authored 180 m × 180 m center retains its 0.75 m mesh spacing and all 58,081 original vertex positions; the new 90 m wide band on every side uses 2.5 m spacing and low, broad relief. All 245 pre-existing rocks and talus pieces retain their transforms. The original cameras remain, and `Expanded authoring overview camera` is available for reviewing the whole canvas. The expanded ground is deliberately sparse for future visual authoring. The saved mesh has 97,969 vertices and 194,688 terrain triangles, compared with 58,081 vertices and 115,200 terrain triangles before expansion. Its finite edge is visible in the full-area render; this is still a bounded Blender study, not a Unity world or streamed terrain.

The reference pass was applied and saved in that same open `.blend`. It adds a roughly 2.3 m high layered shelf and 1,920 dark, rust, and dusty-shale fragments clustered around five existing formations and the new shelf. The denser fans gather at the rock bases and thin across the mostly flat ground. The new meshes, close review camera, and local warm bounce light are in `Reference study - rubble aprons`, which can be hidden for comparison. The terrain, earlier rocks, cameras, and lights remain in place. `Rubble and rock close review camera` shows this pass; switch to `Expanded authoring overview camera` for the whole area. The new rock shapes are a provisional style study and still read more faceted than the photo reference.

The wide-horizon image is now the main reference for **most** terrain: open, low-relief rust ground; shallow ridges and washes; sparse larger rocks with local patches of smaller debris; a distant hazy horizon; and a warm clouded sky. The first image remains the closer reference for fractured rock and talus buildup. The high buttes visible in both references are distant backdrop cues only at this stage, not a request for tall local formations or deep canyons.

The ground texture pass was applied to the **same open `BrokenWorldBadlandsStudy.blend`**. `apply_layered_ground_to_open_study.py` copies the previous terrain material to `StudyGround_LayeredBadlandsReferences` and keeps the previous material available for comparison. It layers three generated study textures from `textures/`: `BadlandsCompactedRustSoil.png` for the widespread soil, `BadlandsEmbeddedShaleGravel.png` for irregular exposed-rock islands, and `BadlandsDarkScree.png` for the finer rubble at formation feet. A terrain point attribute gives the six low formations broad dusty/pebbly aprons, while procedural noise breaks their edges. The existing swept-sand image adds thin dust variation and the existing rocky height image adds subtle bump. Color images are packed into the `.blend`; mirrored image extension avoids sharp repeat seams, although repetition can still be visible. `Layered ground texture review camera` is active for close inspection. These generated images are visual-study color sources, not a calibrated seamless PBR set or a Unity-ready terrain material.

The exact second PNG is packed into the same `.blend` as an equirectangular World environment. `Wide horizon terrain review camera` is active; the earlier cameras remain available. This is a 1774 × 887 **8-bit PNG**, so it is a low-dynamic-range panoramic environment, not a true HDR radiance image. The existing sun and area lights still supply stronger lighting. The rendered review shows a visible join where the finite study terrain meets the image's baked landscape; the image also cannot supply missing views or HDR highlight values. These are limitations to resolve before treating it as a seamless game sky or lighting source.

## Review gate

Assess terrain shape, paths, rock silhouette, texture scale, repetition, and value/color separation from the elevated game-like camera. This render is a composition study, not a measured Unity match. Approved components should be split into reusable meshes and texture assets and checked through Babineaux's Unity import path. Production World Creator remains the sole terrain and placement authority.

Procedural integration: the study uses a fixed seed only for reproducibility. It has no runtime world identity, chunk unload/reload behavior, stable generated-object IDs, placement constraints, or persisted deltas. Those remain owned by existing Unity systems and must be addressed during any approved asset integration.

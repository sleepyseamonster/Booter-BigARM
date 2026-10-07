# Unity single-rock shape parity in the native engine

2026-09-14. Generator v7 ports the original single-rock composition rules and
surface sampling into native C++. Open the
[single-rock viewer](../out/single-rock-workbench/Launch-Rock-Generator.command).
It starts with a freshly generated Golden Rock and includes ten editable
[single-rock recipes](../Assets/SingleRockPresets/README.md). Normal launch uses
the current scene-only viewer; `--test-controls rock` exposes the existing
manual authoring panel when needed. AI authoring can edit validated recipes and
call the native generator/cooker without widgets.

The earlier v6 transfer reproduced three captured source layouts. V7 also
generates new layouts using the original seeded random sequence, mass roles,
profile proportions, overlap, fractures, dimension fit, resting rotation and
burial. Changing a seed now changes composition as it did in the Unity single
rock workbench. Existing v1–v6 recipe behavior remains versioned separately.

The pipeline is:

1. Plan a few weathered solid masses and optional subtractive cuts.
2. Fuse their signed-distance fields on a temporary voxel sampling grid, using
   the original additive bounds, padding, resolution limit and zero handling.
3. Extract the surface with the original six-tetrahedra cell decomposition;
   retain components and apply the original relaxation settings.
4. Produce a native mesh, layered material, three LODs and collision input.

The voxel grid exists only during generation. Runtime rendering uses ordinary
triangles. Native numeric helpers preserve seeded/evaluation behavior; there is
no runtime Unity or C# bridge. The small C# shim exists exclusively in the
[comparison tests](../Tests/ReferenceRock/README.md), where it runs the preserved
original algorithm bodies. Floating-point contraction is disabled for the
planner to retain expression ordering near opposing wedge rotations.

## Verification

The [receipt](../Evidence/SINGLE-ROCK-PARITY/result.json) records this pass.

| Check | Result |
|---|---|
| Reconstruct all three saved Unity family source poses | Maximum component error 1.04308e-7 |
| Original C# algorithms versus native C++, 21 geometry cases | Same source identities, vertex/triangle counts and oriented connectivity |
| Largest corresponding vertex difference | 0.017414 mm; acceptance threshold 0.1 mm |
| 189 orthographic silhouette comparisons | Minimum overlap 99.989716%; threshold 99.95% |
| Native mesh and persistence checks | Closed winding, exact source capture, seed changes, recipe roundtrip, export, Undo and three LODs pass |
| Jolt cooking and mesh surface comparison | All 21 bodies cook; 825 hits across 1,029 probes match mesh heights within 0.2 mm |
| Existing physics and v3/v4/v5/v6 rock checks | Pass |
| Metal viewer and installed launcher | Five captures each, Apply/Undo checks pass, zero GPU errors; simulation not advanced |

The renderer retains the source mesh, including thin triangles. Collision
prevalidation now rejects actual zero-area triangles instead of an arbitrary
minimum area. Jolt's existing sanitizer may discard triangles that collapse in
its quantized collision storage. Surface-query comparisons verify the resulting
collision on the bounded case set.

The [silhouette sheet](../out/single-rock-parity/silhouettes-verified/silhouette-comparison.png)
shows native, original-algorithm and overlay views. Full comparison output lives
under `out/single-rock-parity/`. The package manifest binds native binaries,
shaders, textures, recipes and source hashes. Original Unity source files were
read only; Unity Editor was not run. This proves bounded shape/geometry parity,
not identical Unity lighting/material pixels or Windows execution.

## Procedural and authoring contract

World seed, generator version, region and slot continue to identify generated
objects independently of renderer/physics handles. A v7 recipe deliberately
uses its 32-bit recipe seed for the Unity-compatible shape sequence. Placement
code may choose that seed from world identity; v7 is not enabled in streamed
terrain by this authoring change. Transient meshes and collision bodies can be
rebuilt on unload/reload from the same accepted recipe.

Dimensions, silhouette, fusion, relaxation, source overrides and material
settings are authored constraints. Saving/exporting persists the accepted
recipe; Undo restores it. Runtime gameplay deltas remain owned by the existing
persistence system, not by this temporary grid. No new damage/deformation delta
format or whole-formation fusion is introduced.

Remaining scope is artistic acceptance across more user-selected examples,
Windows numerical/runtime proof, streamed placement adoption, and surrounding
ground/clutter. Those are separate from this single-rock shape pass.

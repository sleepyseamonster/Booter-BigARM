# Unity Golden Rock in the native engine

2026-09-11. The saved approved Unity boulder family now has a native C++ v6
workbench path. Open [the Golden Rock workbench](../out/golden-rock-workbench/Launch-Rock-Generator.command).
It starts with Golden Rock and provides two other saved approved variants.

The source is `ApprovedBoulderFamilyRecipe.asset`, with three two-mass layouts.
The [native library and settings](../Assets/GoldenRockPresets/README.md) preserve
six source positions, full quaternion rotations, physical scales and shape seeds.
The [source receipt](../Assets/GoldenRockPresets/source-receipt.json.txt) records
the original file hash. Unity source files and the running older engine instance
were preserved.

## How the setup builds a rock

1. Load the approved core and support masses, already in their saved resting pose.
2. Evaluate the source's rounded/tapered weathered-block field and four seeded
   corner cuts for each mass. Smoothly fuse their fields at 0.0657 m.
3. Sample the combined exterior at 50 mm for Detail 2 using the native shared
   triangulator. Keep the main connected body and relax its surface at 0.45,
   preserving enclosed volume and the sampled minimum height.
4. Apply the original layered textures and saved grit, shale, crack, variation,
   geology-scale and worn controls through the native renderer.
5. Use that accepted mesh for collision and export; save the complete native
   recipe, including source poses and surface settings.

The saved material controls are grit 0.22, underside shale 0.20, side/top shale 0,
geology scale 2.4 m, surface variation 0.346, cracks 0.36 and worn shine 0.099.
These saved values supersede the older prose values. The inspection preset uses
the current native sky/sun lighting and normal strength 1.08. Its shadow bias is reduced to 0.0003 for the smaller
physical rocks; the comparison reduces the detached-shadow appearance without
changing the captured mesh. Contact dressing remains separate work.

## Authoring and procedural contracts

The right panel exposes Overall scale, Fusion, Relaxation, Voxel size, Edge damage,
source mass size/position/angle and material controls. Apply commits a draft to
preview/collision; Undo/Redo restores accepted states. Save and Export require
an applied draft. Original library presets are preserved by the package launcher.

V6 versions the changed field and physical-space behavior. Old v1–v5 recipes retain
their paths; captured fields cannot silently serialize into older versions.
Existing generated IDs include world seed, generator version, region and stable
slot. Rebuilding the same captured recipe is deterministic. The recipe supplies
its authored constraints; GPU and physics handles remain transient.

This work is an authoring transfer. Live chunk placement/unload/reload and persisted
world deltas remain on the existing v4 path, whose regression passes. Terrain and
stream entry points explicitly reject v5/v6 until their shared shape cache and
placement bounds support these recipes. Authoring regeneration alone is not proof
of live world integration.

## Verification

- Native tests pass all three recipes and their LODs: closed consistently wound
  meshes, unit normals, saved poses/materials, exact seed bits, deterministic
  regeneration, scaled physical dimensions, relaxation volume/contact preservation,
  collision agreement, Undo/Redo, export and invalid-input/budget rejection.
  [Receipt](../out/golden-rock-review/native-final/result.json).
- Existing six-preset v5 authoring, v3 fused-rock and v4 streamed formation/removal/
  reload checks pass. The first v3 invocation used a one-LOD streaming fixture;
  rerunning with its intended three-LOD authoring preset passes.
- Workbench, player and cooker build on the current Mac. Metal captures cover the
  three saved variants and multiple surrounding views. The UI capture checks full
  and small window layouts plus actual ImGui Next seed, Apply and Undo actions;
  all assertions pass with zero GPU errors and no simulation advancement.
  [UI receipt](../out/golden-rock-review/golden-metal/result.json).
- The packaged executable renders using its bundled recipes, textures and shaders.
  [Package receipt](../out/golden-rock-review/package-final/result.json).
- A second read-only import produces identical recipe files. No Unity process,
  gameplay smoke test, dependency addition or Windows test was used.

## Limits of this transfer

The three compositions are captured approved variants. Seed changes vary primitive
chips within a captured layout; Unity's complete random source-layout, width/yaw/
burial planner is not ported. Source field/pose transfer does not imply triangle
or pixel equality: native grid sampling, triangulation, normals, BRDF, lighting and
color treatment differ. Unity's per-seed charcoal tint formula is not included.

The surrounding approved terrain/sand/clutter scene, contact dressing and live
v6 world placement are not part of this package. Physical mouse/gamepad use,
Windows and final visual acceptance remain unverified.

![Golden Rock rendered by the native engine](../out/golden-rock-review/package-final/rock.png)

# Native rock and formation authoring

2026-09-11. The Unity workbench's broad silhouette grammar and individual formation
editing now have a native C++ path. This extends the existing v4 transfer; it does
not claim complete Unity parity or finished geological art.

Open [the v5 workbench](../out/rock-workbench-v5/Launch-Rock-Generator.command).
The existing v4 application and its working files are preserved. The new package
has its own UserData and six [editable examples](../Assets/AuthoringRockPresets/README.md).

## Use the workbench

1. Choose Rock preset: four single-rock silhouettes, a braced outcrop or an authored
   pile. Change the silhouette or seed, then Apply recipe and Frame rock / formation.
2. Fit envelope is the maximum half-size in millimeters. V5 fits uniformly within
   that envelope, retaining a slab's low proportions or a shard's vertical shape.
3. Source volumes → Edit generated volumes exposes the mass/cut plan. Adjust center,
   half-size, yaw, pitch, roll, primitive and taper. Explicit volumes take precedence
   over the seeded silhouette controls; Return to seeded plan clears them.
4. Formation → expand a member to move X/Z, lift, resize, turn, choose a variant or
   seat on ground only. Auto silhouettes choose boulder/slab/chunk/shard by role;
   a fixed silhouette uses four seeded versions of that same shape family.
5. Apply updates rendering and collision together. Undo/Redo restores accepted
   recipes. Save and Export require an applied draft; errors retain the last valid
   preview. Save writes the displayed working file. Export requires a new directory.

The panel stays within the viewport after resizing and scrolls to lower controls.
It can be collapsed using its title arrow. Orbit/pan controls and the Engine menu
retain their existing behavior. Changes are explicitly applied, rather than
rebuilding expensive meshes during every slider movement.

## Translation and procedural contract

Read-only sources were `TopDown3DRockWorkbenchBaseRockGenerator.cs` (profile/core
proportions and primitive selection) and `TopDown3DRockWorkbenchMesher.cs` (field
composition), plus the [retained methods audit](../Research/UnityReference/ENVIRONMENT_METHODS_AUDIT.md).
The native generator adapts their concepts to its own scalar-field mesher. It has
no Unity assembly, asset, scene or Editor dependency.

- Generator v5 owns the changed geometry contract. V1–v4 retain their generation
  paths and serialized fields; new fields cannot silently save into older versions.
- World seed, region and stable member slots still determine identities. Member
  transforms do not renumber siblings. Artist edits are offsets from the seeded
  layout, so changing the seed/layout intentionally changes the underlying plan.
- Member placement uses accepted geometry for ground/host seating. Authored lift
  is added after seating; ground-only disables host support. An explicitly moved
  member that misses its host reports an error instead of silently snapping back.
- Renderer handles and physics bodies are transient. The accepted mesh supplies
  collision and export; recipe save includes primitive and member edits.
- Chunk streaming, placement exclusions and persisted world-removal deltas are not
  changed by this authoring pass. V5 is explicitly rejected by terrain/stream entry
  points until their shared variant cache and full influence bounds support it.
  The existing v4 streaming/removal/unload-reload regression passes. No claim of v5
  live world integration follows from deterministic authoring regeneration.

## Verification

The focused native authoring check passes all six presets, closed consistently
wound LOD meshes, distinct slab/boulder/shard proportions, finite unit normals,
explicit-volume capture, tilted primitive and recipe roundtrips, per-member edits,
identity preservation, collision/mesh equality, export, Undo/Redo and invalid data
rejection. [Native receipt](../out/rock-authoring-v5-review/native-final/result.json).

V3 fused-rock regression, all four v4 composition layouts with streamed collision,
whole-group exclusions and persisted removal/reload, and `rock_adapters_commands`
pass. Workbench, cooker and player build with the existing Mac toolchain. No new
dependency or gameplay smoke test was added.

Metal captures cover all six examples with original materials. The UI capture
checks full/small window layout and activates the actual Dear ImGui Next seed,
Apply and Undo buttons through its navigation queue. All three assertions pass;
no simulation advances. [UI receipt](../out/rock-authoring-v5-review/ui-final/result.json).
Physical mouse/gamepad use and Windows remain unverified.

![Native braced formation](../out/rock-authoring-v5-review/ui-final/rock.png)

## Remaining gaps

Formations remain assembled static members with visible intersections and limited
contact shading. Sand deposits, contact-derived gravel/debris, formation-wide
fusion/seam channels, viewport gizmos and v5 streaming are not included. Four
silhouette families are implemented; Unity's full profile catalog is not ported.
Final visual acceptance remains with the user.

## Reproduce

From the repository root, build `engine_workbench`, `engine_rock_authoring_tests`,
`engine_rock_transfer_tests`, `engine_formation_parity_tests`, and
`engine_rock_workbench_tests` in `Engine/build/foundation`. Run:

```sh
Engine/build/foundation/engine_rock_authoring_tests Engine/Assets/AuthoringRockPresets Engine/out/my-authoring-check
```

The output directory must be new. `--capture-rock <new-directory>` renders and
exports the selected `--rock`; add `--capture-rock-ui` for viewport resize, member
panel and draft/apply/undo verification. Supply the complete cooked `--catalog`
and an `--inspection` preset. These modes use a non-focusable window.

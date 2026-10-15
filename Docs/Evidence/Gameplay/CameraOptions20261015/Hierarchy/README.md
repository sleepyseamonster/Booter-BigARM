# Separate camera authoring rigs — 2026-10-15

CampLoopTesting now has `Camera / Camera Options / Elevated Camera Rig` and `Third Person Camera Rig`, plus an authored Starting View selector. Third Person is selected. Both rigs retain their own editable Inspector settings, runtime switch controls and Edit Mode previews. One existing Main Camera/AudioListener renders both views. GreaterWasteland, packages, input bindings and build settings are unchanged.

The elevated settings were copied field-for-field from the existing 22 m rig. Legacy scene values remain fallback data; the Main Camera Inspector routes framing edits to the separate rig objects. Third-person framing still uses its existing profile, with a scene-owned starting FOV. Initial scene selection takes precedence over the saved local camera mode; subsequent menu Apply and temporary Play mode Inspector switches remain supported. Runtime selection never rewrites Starting View.

## Executed proof

- `final-tests.xml`: **85 passed, 0 failed, 0 skipped**, in the existing marked isolated candidate `D:\BooterBigArmValidation\CameraOptions20261015-01`, Unity 6000.4.0f1, background batchmode with D3D11. Filters cover camera options/menu/runtime/clearance, fixed-terrain coverage, menu/UI integration, riding lifecycle/ownership/input, and the three existing movement/orbit/look-ahead foundation cases. The new runtime case uses an empty synthetic scene and device-isolated input, not gameplay traversal.
- `initial-tests.xml`: 40 passed, 1 failed. The new hierarchy visibility assertion compared an enum with numeric zero. It now compares with `HideFlags.None`; the complete 85-case run supersedes this test assertion failure.
- `CameraHierarchyAuthoring.ValidateCandidateFromCli` exited **0**. `hierarchy-validation.json` records two configured rigs, preserved elevated settings, one Camera/AudioListener, both Inspector preview paths, unchanged output identity, and cleanup of temporary preview solvers/components. The validator opens only the isolated candidate scene and does not save it.
- `validated-source-manifest.json`: all 12 affected scene/code/test files match the candidate after line-ending normalization. The scene hash includes the preserved unrelated working edits; those edits are excluded from this commit.
- `scene-merge-proof.json`: nine new Unity records and two bounded existing-record changes (parent child reference and Main Camera selector reference); all other 1,389 pre-existing records were preserved. The live scene received the same objects with Undo support. Source persistence used those bounded changes rather than saving the author's unrelated unsaved scene state.

The authoring Editor confirmed both visible rig names, valid bindings, the 22 m / 5 m settings, Third Person as Starting View and one Camera. A later optional live-preview dispatch timed out during refresh; the console snapshot still reported compilation in progress with no compiler errors. No forced reload or discard was attempted. The isolated preview/compilation proof above passed independently.

No gameplay smoke test, Player build, foreground activation, terrain/save-world change or production promotion was performed. Camera authoring changes do not affect world identity, generation, chunk unload/reload, authored terrain constraints, generated-object IDs or persisted runtime deltas. Hands-on camera feel remains author-owned.

See the [authoring guide](../../../../Engineering/CAMERA_OPTIONS.md) for hierarchy and selector use.

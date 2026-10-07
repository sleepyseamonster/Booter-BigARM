# Legacy 2D Boundary

`Assets/_Project/Legacy2D/` is the preservation boundary for the former 2D top-down prototype. TopDown3D is the primary production product.

## Ownership

- `Assets/_Project/Scenes/TopDown3D/GreaterWasteland.unity` is the only enabled production build scene. `TopDown3DPrototype.unity` is a disabled generated-world reference.
- New production art, prefabs, scenes, gameplay code, settings, UI, and VFX belong in the normal type-first roots under `Assets/_Project/`.
- Legacy-only art, prefabs, scenes, runtime code, editor tooling, item assets, profiles, templates, world settings, and renderer data belong under `Assets/_Project/Legacy2D/`.
- Shared input and project-wide URP assets stay outside the legacy boundary only when both lanes genuinely depend on them.
- The historical isometric lab remains a separate comparison lane. It is not the primary product and is not part of the 2D legacy folder.

## Safety Rules

- Preserve all asset GUIDs and paired `.meta` files during maintenance.
- Keep the legacy scenes disabled in Build Settings.
- Keep legacy cameras explicitly assigned to renderer index 0. The production 3D renderer remains the project default at index 1.
- Do not add new production dependencies on `Assets/_Project/Legacy2D/`.
- Do not delete the legacy boundary, remove its packages, or migrate its saved data without separate user authority and proof.

## Assembly Boundaries

- `BooterBigArm.TopDown3D.Runtime` owns production 3D runtime code.
- `BooterBigArm.Runtime` owns preserved legacy 2D runtime code.
- `BooterBigArm.Legacy2D.Editor` owns legacy-only editor automation.
- `BooterBigArm.Isometric.Runtime` isolates the historical isometric runtime from both production and legacy source folders.

## Procedural-Generation Contract

Greater Wasteland currently uses fixed authored terrain by the user's 2026-10-06 direction. Procedural generation and streaming implementation are deferred pending redesign. Preserve future compatibility where relevant without activating those systems.

## Planned Complete Segregation

The [professional layout plan](./Operations/Repository/LAYOUT_PLAN_2026-10-06.md) selects a future archive outside Unity's `Assets/` tree. This boundary remains the current physical location until extraction gates pass. The active editor/tests still reference legacy assemblies, the isometric lab uses legacy assets, and production URP settings reference the legacy renderer and default volume profile. Those dependencies must be separated before moving the folder. Preserve GUIDs, archive restoration dependencies, and the active rendering behavior throughout that migration.

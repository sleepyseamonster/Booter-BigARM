# Legacy 2D Boundary

`Assets/_Project/Legacy2D/` is the preservation boundary for the former 2D top-down prototype. TopDown3D is the primary production product.

## Ownership

- `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity` is the first and only enabled production build scene.
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

This migration changes ownership and defaults, not the accepted procedural-generation-first architecture. Every new production system must still address deterministic world identity, chunk unload and reload, stable generated-object identity, authored constraints, persisted runtime deltas, and focused deterministic proof where applicable.

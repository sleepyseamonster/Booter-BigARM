# Perspective Top-Down 3D Foundation Plan

**Status:** Initial foundation accepted; traversal hardening and right-stick camera orbit implemented, awaiting user-owned feel tuning
**Owner:** Gottspan under the user's creative and product authority
**Supersedes for new work:** the orthographic/isometric camera direction in `ISOMETRIC_DIRECTION_BRIEF.md`
**Preserves:** the existing 2D prototype and the completed isometric conversion lab as comparison evidence

## Product Direction

Booter & BigARM is moving toward a perspective, top-down 3D game. The world, characters, physics, terrain, lighting, and runtime assets are fully 3D. The camera remains elevated and deliberately constrained so the game retains the readability and navigation feel of a top-down game without using orthographic or isometric projection.

The first implementation foundation prioritizes camera feel, player movement, gamepad control, procedural 3D world generation, and a smaller simple-follow BigARM. Harvesting, item balance, save migration, production assets, and the final companion design are deliberately deferred.

## Definition Of Done For This Batch

This foundation batch is complete when the repository contains a separate `TopDown3DPrototype` scene, excluded from Build Settings, that demonstrates:

- a perspective camera with elevated, constrained right-stick yaw/pitch orbit, stable follow damping, and obstruction handling;
- normalized camera-relative player movement using a 3D Rigidbody and walkable-slope grounding;
- one centralized owner for Gameplay input with gamepad and keyboard bindings for movement, sprint, and BigARM recall plus gamepad right-stick camera look;
- deterministic 3D mesh terrain generated from seed plus chunk coordinates;
- runtime chunk loading and unloading around Booter without cracks between neighboring chunks;
- a smaller BigARM with simple idle, follow, avoidance, stuck-recovery, and recall behavior;
- structural and deterministic tests, conversion-preservation validation, clean Unity compilation, and recorded playtest limitations.

## In Scope

- A new runtime assembly for perspective top-down 3D systems, isolated from legacy Tilemap dependencies.
- Project-owned greybox materials and settings.
- A generated development scene with distinct GUIDs and no Build Settings changes.
- Gamepad-friendly bindings and deadzone treatment using the existing Input System asset.
- Focused editor validation and tests.
- Documentation that records the revised direction and current proof state.

## Out Of Scope

- Harvesting, inventory, survival, canister, or economy redesign.
- Save-file migration or writing conversion state to the legacy save slot.
- Final BigARM abilities, harvesting autonomy, combat, scouting, or production navigation.
- Production models, rigs, animation, VFX, audio, UI art, purchases, or external assets.
- Package installation, default-renderer changes, Build Settings changes, player-build cutover, release, or legacy deletion.
- Final biome, canyon, landmark, structure, resource, or encounter generation.

## Architecture

### Camera

- Perspective projection is authoritative for new work.
- The camera uses the existing `Gameplay/Look` right-stick binding as angular velocity: horizontal input orbits, vertical input adjusts pitch inside a top-down-safe range, and releasing the stick holds the current view.
- The Input System's gamepad-stick deadzone is authoritative; the camera does not stack a second deadzone processor.
- Camera distance, pitch, field of view, damping, and target offset remain serialized tuning values.
- Camera obstruction pulls the camera toward its target without changing gameplay movement intent.

### Input And Movement

- A single input router owns the Gameplay action map in the new scene.
- Gameplay systems consume higher-level movement, sprint, and recall state from that router.
- Player input is projected through the camera basis onto XZ, clamped to unit magnitude, and applied through Rigidbody physics.
- The motor exposes position, velocity, grounded state, sprint state, facing, and teleport behavior needed by later shared seams.

### Procedural World

- A settings asset owns immutable generation and streaming parameters.
- Terrain height is a deterministic function of world seed and world-space sample coordinates.
- Every chunk builds an independent mesh and collider from the same border samples, producing matching seams.
- Border vertices use world-sampled normals, so neighboring chunks match in both geometry and terrain lighting.
- Chunk identity is an XZ coordinate. Y is elevation.
- Initial loading creates a safe local collider ring immediately, then builds farther chunks within a per-frame budget; unloading uses a padded radius to avoid boundary thrash.
- Initial spawn selection rejects terrain above the configured walkable slope, and prop placement rejects the spawn exclusion zone, steep surfaces, overlaps, and chunk-edge conflicts.
- Generated runtime objects remain separate from authored scene content and are not save data.

### BigARM

- BigARM remains larger than Booter but is substantially smaller than the original conversion-spike placeholder.
- The foundation AI owns only follow spacing, local obstacle avoidance, ground placement, stuck recovery, and recall.
- More complex decisions remain behind the later companion-design gate.

### Lighting And Shadows

- `PerpetualTwilightSun` is the perspective lane's authoritative global-light owner.
- The sun remains low on one side of the world and moves through a slow, continuous brighter-sunset/deeper-twilight loop rather than a conventional overhead day and dark night.
- Direct-light color and intensity, flat ambient fill, fog, and the procedural runtime sky move together so the world retains an orange twilight read without flattening the shadows.
- The directional light uses high-quality soft shadows backed by a 4096 main-light atlas, four camera-tuned cascades, conservative cascade culling, and a 60-unit shadow distance. Its narrow elevation and azimuth bands keep shadows long and readable without sudden direction reversals.
- Later gameplay may read `PerpetualTwilightSun.Active`, `DirectionToSun`, `LightTravelDirection`, and `Brightness01`; it should not create a competing clock or infer sun state from presentation-only colors.
- The current generated perspective scene adopts the system at runtime. Future scene rebuilds serialize the component through `TopDown3DPrototypeBuilder`.

## Execution Sequence

1. Add the isolated runtime assembly and implement input, movement, camera, deterministic terrain, streaming, and BigARM follow systems.
2. Add a guarded editor builder for the new scene and settings/material assets.
3. Add structural, input-binding, determinism, and seam-continuity tests.
4. Import through Unity, build the new scene, and run focused non-smoke validation.
5. Record the automated and visual proof boundary, reconcile docs, and commit only verified task-owned files. Hands-on movement, controller, camera-feel, and companion acceptance belong to the user unless the user explicitly asks Codex to perform them.
6. After initial user acceptance, harden traversal with walkable safe spawn selection, staged streaming, unload hysteresis, collision-safe props, normal-continuous seams, and right-stick camera orbit.

## Stop Conditions

Stop before any implementation would require package installation, Build Settings changes, replacement of a protected scene, modification of user-owned ground art, production asset sourcing, save migration, or expansion into deferred gameplay mechanics.

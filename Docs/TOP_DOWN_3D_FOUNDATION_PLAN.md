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
- a smaller BigARM with route-based natural follow, acceleration, avoidance, stuck recovery, physical catch-up, and no teleport fallback;
- layered dust atmosphere with distance haze, close suspended dust, restrained post-processing, deterministic world-scale pockets separated by clear air, and explicit local-zone overrides;
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
- BigARM is a companion and synergistic part of Booter's mechanics, not a mobile base, habitat, safe zone, or crafting hub.
- The first natural-follow slice follows Booter's recent route with a forgiving follow band, acceleration/deceleration, turn-weighted movement, local avoidance, stuck recovery, and faster physical catch-up when called.
- Calls and large separation never relocate BigARM. Missing streamed terrain produces an explicit waiting state until the later world-scale traversal seam is designed.
- More complex autonomous tasks and unloaded-world traversal remain behind the later companion-design gate in `BIGARM_COMPANION_STANDARD.md`.

### Lighting And Shadows

- `PerpetualTwilightSun` is the perspective lane's authoritative global-light, ambient-fill, and sky owner.
- The sun remains low on one side of the world and moves through a slow, continuous brighter-sunset/deeper-twilight loop rather than a conventional overhead day and dark night.
- Direct-light color and intensity, flat ambient fill, and the procedural runtime sky move together so the world retains an orange twilight read without flattening the shadows.
- The directional light uses high-quality soft shadows backed by a 4096 main-light atlas, four camera-tuned cascades, conservative cascade culling, and a 60-unit shadow distance. Its narrow elevation and azimuth bands keep shadows long and readable without sudden direction reversals.
- Later gameplay may read `PerpetualTwilightSun.Active`, `DirectionToSun`, `LightTravelDirection`, and `Brightness01`; it should not create a competing clock or infer sun state from presentation-only colors.
- The current generated perspective scene adopts the system at runtime. Future scene rebuilds serialize the component through `TopDown3DPrototypeBuilder`.

### Dust Atmosphere

- Current runtime posture: the global dust haze is parked while its implementation is retained. `TopDown3DDustAtmosphere.DefaultGlobalHazeEnabled` is `false`, so the controller does not become `Active`, its scene-load bootstrap does not install it, and volumetric haze, dust-responsive post-processing, motes, and veils remain absent from play by default. Ground-deposited drifts and footstep kick-up remain independent and active.
- When re-enabled, `TopDown3DDustAtmosphere` remains the perspective lane's single fog, close-haze, and atmosphere post-processing owner; the twilight sun supplies brightness state but does not write competing fog values.
- A half-resolution URP RenderGraph pass raymarches the canonical dust field before transparents, reconstructs world positions from camera depth, and uses the actual main-light color, direction, and shadow attenuation for sunset scattering. Legacy `RenderSettings` fog stays disabled while this atmosphere is active so there is no second extinction path.
- Sun-facing dust remains warmer and brighter through directional scattering, but a soft 1.65 phase ceiling prevents the low sun from washing the entire camera view into an unreadable flat veil. This contrast protection belongs to the dust optics path, not the camera rig.
- The renderer consumes a snapped 64-by-64 world-XZ density texture generated by `TopDown3DDustAtmosphere` from the same seeded pocket field and `TopDown3DDustZone` sampling used by gameplay. Beer-Lambert extinction preserves the 26-to-27-unit lighter-pocket and 17-to-18-unit dense-pocket half-visibility contract; outside a pocket the procedural extinction is zero.
- A depth-aware full-resolution composite limits halos at terrain and character edges. Two camera-following particle layers remain a secondary close-haze treatment after the volumetric pass: tiny low-poly suspended motes use URP's shadow-receiving lit particle shader to catch the perpetual sunset, while broad veils remain soft and unlit so they do not become glowing cards. Both layers keep an explicit nonzero clear-air emission floor; pockets increase their density but never gate whether drifting dust is visible.
- A shared iron-oxide rust palette tints the volumetric haze, close particles, authored dust zones, bloom, and dust-responsive color filtering. The restrained runtime Volume returns to neutral values in clear air and avoids depth of field so gameplay focus and silhouette readability remain intact.
- A seeded world-space cellular field places softly blended, irregular dust pockets independently of loaded chunks. Default pocket centers use 144-unit cells, their 54-to-72-unit radii span roughly six to eight 18-unit chunks, and an 18-unit edge band eases between dust and clear air. Sampling absolute XZ plus the immutable world seed makes the result stable across chunk seams, loading order, unloading, and negative coordinates.
- The low-frequency regional field now varies density inside each pocket rather than imposing an always-on dust floor. `TopDown3DDustZone` remains the smooth local override for authored hazards, shelters, storms, or later gameplay effects without requiring a second visual path.
- Pocket interiors also carry a deterministic, strongly shaped 0.038-frequency clump field, producing readable thick and thin rust-dust patches at roughly a 26-unit scale. Because this modulation is folded into `SampleAtPosition`, volumetric scattering, visibility, exposure, and close-particle response agree on each clump instead of layering a camera-only noise effect.
- `TopDown3DFootstepDust` converts actual grounded travel distance into alternating rust-dust puffs, so walking and sprinting retain a stable cadence without imported animation events. Every grounded step produces a clearly readable puff, including in clear-air regions; sampling the same world-space pocket field as the volumetric atmosphere lets thick pocket interiors increase particle count, opacity, size, and lifetime without controlling whether the effect exists.
- Later gameplay must treat `TopDown3DDustAtmosphere.Active == null` as the parked/no-global-haze state. When the atmosphere is deliberately re-enabled, it may read `CurrentDustIntensity`, `DustExposure01`, and `ApproximateVisibilityDistance`; it should not reverse-engineer mechanic state from `RenderSettings` or particle emission.
- While the default-off posture is active, existing perspective scenes do not receive a controller from the runtime bootstrap. Guarded scene rebuilds may preserve the serialized atmosphere and tuning regions through `TopDown3DPrototypeBuilder`; they remain inert until the retained `globalHazeEnabled` gate is deliberately enabled again.
- The retained renderer feature remains installed exactly once on the protected non-default 3D renderer but no-ops while no atmosphere is `Active`. The default 2D renderer, pipeline-wide depth setting, Build Settings, and sun clock remain unchanged.

## Execution Sequence

1. Add the isolated runtime assembly and implement input, movement, camera, deterministic terrain, streaming, and BigARM follow systems.
2. Add a guarded editor builder for the new scene and settings/material assets.
3. Add structural, input-binding, determinism, and seam-continuity tests.
4. Import through Unity, build the new scene, and run focused non-smoke validation.
5. Record the automated and visual proof boundary, reconcile docs, and commit only verified task-owned files. Hands-on movement, controller, camera-feel, and companion acceptance belong to the user unless the user explicitly asks Codex to perform them.
6. After initial user acceptance, harden traversal with walkable safe spawn selection, staged streaming, unload hysteresis, collision-safe props, normal-continuous seams, and right-stick camera orbit.

## Stop Conditions

Stop before any implementation would require package installation, Build Settings changes, replacement of a protected scene, modification of user-owned ground art, production asset sourcing, save migration, or expansion into deferred gameplay mechanics.

# Top-Down 3D Landscape Implementation Plan

Status: Representative family implemented and Unity-validated on 2026-08-14; fixed-camera visual acceptance and Development Player profiling remain pending.

## Objective

Transform the production `TopDown3DPrototype` procedural world into a realistic-stylized Broken World landscape matching the supplied red-desert references: broad eroded basins, readable drainage and traversal corridors, mesas and scarps, layered sediment, convincing rock and cliff families, atmospheric depth, and perpetual-twilight lighting.

The first completion target is one representative, deterministic production landscape family. It must be built through the canonical streamed-world path, remain safe to extend, and establish a coherent geology-to-rendering data flow rather than a demonstration scene or parallel generator.

## Authority and owner lane

- The user is the product and creative authority and approves final visual direction, platform targets, purchases, release work, and destructive decisions.
- Gottspan owns repo-wide coordination and this landscape/world-generation implementation lane.
- Babineaux owns later visible Unity Editor interaction or automation handoff when such work is explicitly requested or becomes necessary.
- Gear Ball owns staging, commits, pushes, pull requests, and publication. This plan grants none of those actions.
- Existing unrelated dirty files and assets remain user-owned. Landscape files already modified by the active landscape/rock work may be changed only when required by this plan and only after their current contents are inspected.

## Sources of truth, in order

1. The six landscape reference images supplied in the approving task and the user's stated realistic-stylized direction.
2. `AGENTS.md` and `Docs/Agents/Gottspan/README.md`.
3. `Docs/WORLD_BASIS.md` for setting, tone, and world rules.
4. `Docs/WORLD_SYSTEMS_STANDARD.md` for deterministic world identity, chunking, stable generated-object identity, and persisted deltas.
5. `Docs/DECISION_LOG.md` for the accepted TopDown3D production cutover.
6. The live production scene, build settings, scripts, shaders, materials, settings assets, and tests.
7. `Docs/PERFORMANCE_AUDIT_AND_OPTIMIZATION.md`, `Docs/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md`, and `Docs/ROCK_FORMATION_MESH_FUSION_PLAN.md` where they do not conflict with the higher authorities above.

`Docs/PROJECT_STATUS.md` is not authoritative for the current production-scene choice where it conflicts with live Build Settings and the accepted decision log.

## Approved scope

- The production TopDown3D procedural generator and world settings.
- Deterministic regional geology, drainage, basin, mesa, ridge, scarp, talus, and traversal-corridor rules.
- A shared terrain surface sample used by terrain meshes, normals, materials, rocks, cliffs, landmarks, and natural-object placement.
- Terrain PBR data and the existing Broken World terrain shader/material authority.
- A production rock, outcrop, cliff, and landmark asset/baking pipeline whose runtime does not require native CSG.
- Geology-driven placement through the existing natural-object catalog/planner authority.
- Near, middle, and far landscape representation sufficient for the fixed production camera to read a large world.
- Perpetual-twilight sun, ambient response, atmospheric perspective, and haze through the existing atmosphere authority.
- Required editor baking, deterministic tests, structural validation, performance instrumentation, and durable documentation.
- One representative landscape family before biome or content breadth.

## Non-goals

- Legacy2D changes.
- Movement, camera controls, HUD, input, survival, combat, harvesting, ruins, encounters, or broad biome content.
- Changes to the intended camera distance, FOV, pitch, or orbit. Far-clip and landscape visibility tuning are allowed when required for distant terrain.
- Save migration or a change of world perspective.
- BigARM behavior or unloaded-world traversal simulation.
- Runtime hydraulic simulation, voxel terrain, caves, or package additions.
- External purchases, commissions, or silently importing third-party content.
- A second terrain generator, compatibility mode, fallback terrain, duplicate material authority, or demo-only workaround.
- User-owned gameplay smoke testing.
- Branch switching, worktrees, staging, commit, push, pull request, release, deployment, or publication.

## Canonical architecture decision

Create an instance-owned `TopDown3DWorldGenerator` as the sole terrain and surface authority. It consumes deterministic world identity (seed plus an explicit terrain-generation version), a geology profile, and world coordinates. It returns `TopDown3DWorldSurfaceSample`, including at minimum:

- height and stable regional feature identity;
- sand, gravel, bedrock, and deposited-material weights;
- flow/drainage influence, curvature, and talus influence;
- lithology/weathering values;
- traversal-corridor influence.

The production terrain mesh, normals, shader data, rocks, cliffs, dust, vegetation/natural objects, hero landmarks, and distant terrain must derive from that same sample or its stable feature identity. After the atomic production cutover, remove the static `TopDown3DHeightSampler` and separate escarpment height authority rather than retaining a legacy path.

The first regional scale is 288 metres (16 current 18-metre chunks) with a deterministic halo so adjacent regions agree. The terrain stack is: broad basin/uplift form, warped ridge and mesa fields, drainage carving, terraces/weathering, talus deposition, low-relief detail, then surface-material weights.

Keep current collidable streamed chunks for the near field. Add coarser middle/far terrain derived from the same generator, targeting a visually useful horizon of roughly 1.1 km subject to profiling. Hero landmarks share stable identities between near and far representations.

Extend the existing terrain shader/material authority to consume baked/generated geological weights and physically plausible albedo, normal, roughness, and AO data. Do not create a competing terrain shader path.

Produce the rock/cliff family as deterministic editor-baked meshes/prefabs with LODs and catalog entries. Existing Manifold fusion may be used as an editor-only authoring aid if it proves robust enough for bounded inputs, but shipped/runtime world generation must not depend on native CSG or disable the world when that native plugin is unavailable.

## Implementation batches

### Batch 0 — Planning and authority baseline

- Record this implementation plan as the checkpoint source.
- Reconcile stale status text that conflicts with the accepted TopDown3D production decision.
- Record the terrain-generation version and the migration/cutover decision.
- Preserve current dirty work and identify plan-owned paths before every edit.

Proof: repository status and source citations; no conflicting production-scene authority remains in touched documentation.

### Batch 1 — Visual benchmark and asset rules

- Define a fixed-camera landscape benchmark using the production camera without changing its intended composition.
- Document measurable scale bands, texel-density goals, terrain/rock material channel conventions, LOD transitions, and reference-image success criteria.
- Inventory existing terrain textures, rock materials, mesh families, and third-party provenance.

Proof: a durable benchmark/asset specification tied to the production scene and current content.

### Batch 2 — Pure deterministic geology kernel

- Add serializable geology settings/profile data with safe defaults.
- Add `TopDown3DWorldGenerator` and `TopDown3DWorldSurfaceSample` as pure runtime code with no scene or native-plugin dependency.
- Generate stable regional basin/uplift, ridge/mesa, drainage, terrace, talus, low-relief, surface-weight, and corridor data with seam-safe coordinate sampling.
- Add deterministic, seam, range, continuity, and feature-identity tests.

Proof: isolated compile plus Unity EditMode tests when the editor is available; repeated seeds and cross-border samples agree.

### Batch 3 — Atomic production terrain-authority cutover

- Make `TopDown3DProceduralWorld` own one configured generator instance.
- Route chunk vertices, normals, spawn heights, and all existing height consumers through it.
- Bump the terrain-generation version because generated terrain identity changes.
- Remove the static height sampler and separate escarpment height authority after all callers move.
- Remove the native-Manifold startup gate from world availability; native fusion remains editor authoring only.

Proof: no production calls to the removed authorities; structural validator; isolated compile; deterministic chunk-border tests; Unity scene/test proof when available.

### Batch 4 — Geological terrain shading

- Carry compact geology/surface weights through the existing chunk mesh format.
- Update `BrokenWorldTerrainBlend` and its production material to blend by generated surface data with distance-aware detail.
- Establish correctly authored, neutral-lit PBR channel assets and import settings; avoid baked lighting in base colour.
- Preserve one terrain material/shader authority.

Proof: shader compile/import, channel and material validation, fixed-camera near/mid screenshots, and no visible chunk seams.

### Batch 5 — Middle and far landscape

- Add camera-relative coarse terrain rings/clipmap derived from the same generator.
- Prevent overlap/z-fighting with near chunks and preserve deterministic hero-feature identities.
- Extend camera far clip only as required; preserve distance, FOV, pitch, and orbit.
- Budget regeneration and avoid per-frame managed allocations.

Proof: structural and deterministic tests, transition screenshots, profiler markers, and a Development Player profile on target-class hardware when available.

### Batch 6 — Production rock and cliff family

- Author/bake representative boulder, slab, outcrop, scarp/cliff, talus, and hero-spire families with silhouette variation, erosion logic, pivots, collision, LODs, and triplanar PBR materials.
- Make the natural-object catalog the sole runtime mesh/prefab authority.
- Move mesh fusion fully out of player/runtime generation and keep only proven editor-bake tooling.

Proof: bake validator, mesh/collider/LOD/material checks, catalog completeness, deterministic placement tests, and fixed-camera silhouette/material review.

### Batch 7 — Geology-driven placement

- Replace independent placement heuristics with rules based on surface sample, regional feature identity, drainage, talus, lithology, and corridors.
- Preserve stable generated-object identity and chunk unload/reload determinism.
- Keep authored exclusion and traversal constraints explicit.

Proof: deterministic planner tests, reload identity checks, corridor-clearance checks, and representative density screenshots.

### Batch 8 — Twilight lighting and atmosphere

- Tune the existing perpetual-twilight and dust-atmosphere authorities for warm directional light, cool/neutral fill, readable rock planes, and depth-separated haze.
- Keep atmosphere distance-aware and compatible with far terrain; do not add a competing fog authority.

Proof: fixed-camera near/mid/far comparison images and stable frame/performance evidence.

### Batch 9 — Integrated proof and handoff

- Run the owned EditMode suites and structural validator after the Unity editor lock is released.
- Capture deterministic seed comparisons, chunk seams, near/mid/far views, rock family views, and profiler evidence.
- Self-audit canonical ownership, duplicate paths, serialization/versioning, persisted-delta implications, and documentation accuracy.

Proof: source/static checks plus Unity import, EditMode, visual, and Development Player evidence kept distinct. Hands-on gameplay smoke testing remains user-owned.

## Validation and proof boundary

Repository inspection, source checks, isolated compilation, and pure deterministic tests may be completed without the Unity GUI. They are not Unity import, EditMode, scene, visual, gameplay, or player-performance proof.

While `Temp/UnityLockfile` exists, do not start a competing Unity batchmode process and do not focus or manipulate the editor. Unity-dependent proof is deferred until the existing editor closes or the user explicitly requests an appropriate visible/editor handoff.

Any final performance claim requires a controlled Development Player profile on agreed target-class hardware. Editor profiling is diagnostic only. Any final visual claim requires fixed-camera captures from the production scene and user acceptance.

## Implementation checkpoint — 2026-08-14

Completed in the canonical production path:

- Added terrain-generation version 2, a serializable geology profile, `TopDown3DWorldGenerator`, and `TopDown3DWorldSurfaceSample` as the shared deterministic authority.
- Cut near terrain, normals, spawn selection, dust deposition, natural-object planning, rock-root planning, and material weights to that authority; removed the old static height and separate escarpment production paths.
- Extended the existing terrain shader/material authority to generated surface weights and distance-faded detail, and enabled streaming mipmap metadata for the owned terrain and rock textures.
- Added deterministic non-colliding middle/far rings from the same generator and extended only camera far visibility, preserving the 25 m distance, FOV, pitch, orbit, input, and gameplay perspective.
- Removed the native-Manifold startup requirement and runtime fusion call from streamed-world construction. The retained fusion implementation now lives in the Editor assembly, and both native binaries are excluded from Standalone players.
- Drove rock and clutter density from bedrock, gravel, talus, deposit, weathering, drainage, and corridor data.
- Changed ambient response from a single flat value to warm/cool/ground trilight under the existing perpetual-twilight authority. The separate dust atmosphere remains deliberately dormant pending fixed-camera and performance proof.
- Added focused generator and camera coverage tests, updated affected tests and validator contracts, and imported/compiled the current runtime, Editor, shaders, assets, and Editor-test assembly in Unity 6000.4.0f1.
- Baked and catalogued nine archetypes (pebble, shard, slab, boulder, nodule, outcrop, cliff, talus, and hero spire), three stable variants per archetype, and three progressively simplified LODs per variant: 27 complete families and 81 persistent mesh assets.
- Cut physical LOD rendering, collider bounds, cosmetic combined meshes, directional support, world bounds, overlap witnesses, and variant selection to the catalog. Deleted `TopDown3DNaturalMeshLibrary`; missing baked families now fail closed instead of invoking a fallback constructor.
- Added baked three-level LOD geometry and one simple box collider per physical formation member while preserving one traversal marker on the formation root. The 2026-08-17 performance correction consolidates member rendering to one `LODGroup` per formation while retaining every member collider, transform, stable identity, and baked shape. The prior obsolete same-object collider requirement remains removed; the updated hierarchy has source/compiler proof and still requires execution of the focused EditMode contract.
- Advanced natural-object generation to version 3 and physical-rock generation to version 5 for the abundance-response and baked-geometry cutovers. Corrected the abundance shaping call so sparse regions can actually reach the documented zero-density floor while retaining deterministic rock-rich islands.
- Split landscape-only validation from unrelated character/HUD scene validation without weakening either contract. The production landscape validator now checks the geological world/material path, terrain and triplanar rock assets, catalog completeness, all LOD meshes, pivots, collider bounds, readability, and player exclusion for the authoring-only native plugins.

Recorded Unity proof:

- Deterministic rock bake completed successfully in Unity and serialized all 27 catalog entries and 81 mesh assets.
- The canonical production landscape validator passed in Unity batchmode.
- The isolated editor-fusion dependency validator passed with both Manifold binaries Editor-only.
- The owned landscape EditMode selection passed 27 of 27 tests: world generator, natural-object planning/decorating, rock baker/catalog, editor fusion, and the scoped performance contract.
- A broader Editor-test run also exposed unrelated pre-existing HUD/Survival EditMode scene-construction failures plus character material/animation validation drift. Those failures are outside this landscape lane and are not represented as landscape proof.

Not yet visually or hardware proved:

- Fixed elevated top-down captures, near/mid/far transition review, chunk/ring seam inspection, rock silhouette and material review, and user visual acceptance.
- Controlled Development Player profiling on target-class hardware, including steady-state frame percentiles, streaming spikes, render/GPU time, allocations, and memory.
- Hands-on traversal and collider acceptance remain user-owned gameplay proof.
- Rock materials currently use owned triplanar albedo, scalar roughness, geometry normals, and the restrained teal luster mask. Dedicated rock micro-normal/roughness/AO maps remain an asset-quality option pending the fixed-camera review; no claim of final material fidelity is made.
- Visual calibration of terrain amplitudes, surface thresholds, far-ring rebuild cadence, lighting, and haze. Source defaults are hypotheses until viewed from the production camera.

Visual calibration update — 2026-08-15:

- User review confirmed that sparse visible bedrock is correct, but the physical formation hierarchy was far too sparse near the player compared with the six reference landscapes.
- Decoupled formation admission from the sparse visible-bedrock material mask while retaining deterministic regional abundance, geology, slope, corridor, spacing, and spawn-exclusion constraints.
- Advanced physical-rock generation to version 6 and raised the six root-tier rates to establish frequent low/medium formations, regular large and extra-large outcrops, and occasional readable massive/towering silhouettes.
- Removed the oversized cross-tier spacing halo: peers retain full separation, while smaller formations may cluster outside a larger formation's non-overlapping envelope to build the reference-style boulder and outcrop skirts around monuments.
- Added focused linear-admission and representative starting-region density contracts. Fixed-camera visual acceptance and player-performance proof remain required after this calibration.

Current safe stop: repository, Unity import, bake, validator, and owned EditMode work are complete for the representative family. The next meaningful step is fixed-camera visual review through an explicitly requested Babineaux/Unity presentation pass, followed by any approved visual tuning and a controlled Development Player profile. Do not broaden into more biomes or claim final visual/performance quality before those gates.

## Implementation stop condition

Stop when one representative landscape family is integrated through the canonical production TopDown3D path; the old terrain/runtime-fusion authorities are removed; deterministic and structural checks pass; available fixed-camera and performance evidence is recorded; and all remaining Unity-, hardware-, or user-owned proof is explicitly bounded.

Stop earlier if the next action changes an unapproved system or owner lane, requires a package/purchase/publication/destructive action, would overwrite unrelated dirty work, depends on unsafe concurrent Unity control, or reveals that this plan is incomplete or contradictory. Do not broaden into additional biomes or content families until the user accepts the representative visual direction.

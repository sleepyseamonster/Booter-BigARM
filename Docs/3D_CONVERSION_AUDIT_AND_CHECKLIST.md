# 2.5D Isometric-Style Conversion Audit And Checklist

This document is the working audit and gated conversion plan for moving Booter & BigARM from its current top-down 2D pixel-art prototype to a top-down, isometric-style 2.5D game rendered with 3D environments, characters, props, lighting, and effects.

The intended direction is **top-down, isometric-style 2.5D**: the runtime world is made from 3D assets and uses 3D rendering and collision, while a fixed or tightly constrained isometric-style camera and primarily planar traversal preserve readability. Screen-space UI can remain 2D. This does not imply a free-orbit third-person camera or unrestricted vertical traversal.

## Status And Authority

- Audit date: 2026-08-12.
- Audit basis: read-only inspection of the live repository.
- Direction authority: the user has selected a top-down, isometric-style 2.5D game using 3D world assets.
- Exploration authority: the user has authorized creating and refining this audit/checklist.
- Implementation authority in this pass: documentation only.
- Final conversion status: not yet accepted as a destructive cutover.
- The current 2D prototype remains the working implementation until a separate 3D vertical slice passes its acceptance gate.
- No asset purchase, package installation, destructive asset removal, broad project-setting change, or release is authorized by this plan.

## Definition Of Done For The Conversion Program

The conversion is complete only when:

- Booter, BigARM, terrain, resources, props, structures, hazards, and encounters use approved 3D runtime assets.
- The camera, movement plane, aiming model, elevation rules, and occlusion behavior have been explicitly accepted.
- The survival traversal loop works in the 3D scene: leave BigARM, travel, gather or salvage, experience resource pressure, and return.
- Deterministic chunk generation and streaming work with 3D world output.
- BigARM can navigate and perform its approved prototype tasks without unacceptable collision or pathing failures.
- Current save/load, inventory, survival, harvesting, and command behaviors are either migrated or intentionally superseded.
- Performance meets an agreed target on the agreed minimum hardware and target platform.
- Required runtime/editor tests, non-mutating validation, build checks, and focused playtests pass.
- Canonical docs describe the accepted 3D implementation rather than the retired 2D baseline.
- Legacy 2D assets and code are removed only through a separately reviewed, reference-safe cleanup after the 3D implementation is proven.

## Scope Of This Audit

### In scope

- Current renderer, package, scene, code, art, prefab, save, tooling, test, and documentation dependencies.
- Reuse-versus-replace classification.
- Product and technical decisions required before conversion.
- A phased implementation checklist with stop conditions and proof gates.
- A production-oriented 3D asset checklist.
- Risks, sequencing, and cutover safeguards.

### Out of scope

- Editing Unity scenes, prefabs, renderer assets, packages, project settings, or gameplay code.
- Selecting or purchasing third-party assets.
- Declaring provisional camera, verticality, art-style, or performance ideas to be final design canon.
- Deleting or moving the current 2D implementation.
- Promising dates, budgets, platforms, or release scope.

## Sources Of Truth

Use this precedence when the conversion work begins:

1. The user's current creative and product direction.
2. `AGENTS.md` and Gottspan's repo-management contract.
3. `Docs/WORLD_BASIS.md` for the world fantasy, tone, and survival rules.
4. This audit for conversion sequencing and gates.
5. Accepted architecture, movement, input, world, art, and rendering standards after they are revised for 3D.
6. Live Unity assets, packages, settings, scenes, and code for implementation fact.
7. Provisional research notes and experiments.

The world fantasy does not need to be reinvented to change presentation. The current sentence that declares a 2D pixel-art game must be revised only after the conversion direction passes its acceptance gate.

## Executive Audit Result

The project should **not** be restarted in a blank Unity repository.

The safe path is to keep the current project and create a parallel isometric-style 3D prototype scene, renderer path, movement implementation, and world-output implementation. This preserves useful game-state systems and the existing prototype as a comparison and recovery point.

The conversion is feasible because the project is still a prototype and several important systems are data- or state-oriented. It is nevertheless a major presentation and world-runtime change because the most coupled systems are central ones: player movement, BigARM locomotion, world generation, camera presentation, scene construction, and asset production.

## Current-State Audit

### Repository safety

- Branch at audit time: `main`, seven commits ahead of `origin/main`.
- The worktree already contains user-owned changes in prototype ground art and Babineaux/Unity automation documentation and tooling.
- Those changes are not conversion-owned and must not be staged, reverted, moved, or rewritten by conversion work.
- The existing 2D scene and asset GUIDs must remain intact during the exploratory 3D lane.

### Engine, packages, and renderer

- Unity editor version: `6000.4.0f1`.
- URP package version: `17.4.0`.
- Cinemachine `3.1.5` and Input System `1.19.0` are installed and reusable.
- Unity's 3D physics and Terrain modules are available.
- The current URP asset has one renderer entry, and that entry is `Renderer2D.asset`.
- The current renderer uses 2D lighting, transparency sorting, and sprite-oriented materials.
- A 3D renderer path does not currently exist in project-owned settings.
- No approved runtime navigation solution has been selected. Do not add a navigation package merely because 3D is planned; select it against the BigARM and world-streaming requirements.

### Scenes and prefabs

`PrototypeScene.unity` is structurally 2D. Its serialized content includes:

- 2 `Rigidbody2D` components.
- 1 `BoxCollider2D` and 5 `CircleCollider2D` components.
- 7 `SpriteRenderer` components.
- 8 `Tilemap` components across 6 `Grid` components.
- A pixel-perfect main camera and a Cinemachine pixel-perfect extension.
- Sprite/tilemap lighting and depth-ordering assumptions.

The existing prototype prefabs are sprite-based resource, boulder, and tall-prop placeholders. They should remain available as 2D reference content during the 3D spike rather than being converted in place.

### Runtime code

- Runtime source contains 46 C# files and approximately 6,317 lines.
- Nine runtime files directly use the main low-level 2D physics, tilemap, or sprite-rendering APIs.
- Additional systems reference 2D-bound classes such as `PlayerMotor2D` or `PrototypeWorldGenerator`, so direct API counts understate the dependency surface.
- `PrototypeWorldGenerator.cs` is approximately 1,843 lines and is strongly coupled to tilemaps, rule tiles, sprite layers, and sprite props.
- `PrototypeBigArmAiController.cs` is approximately 578 lines and mixes reusable task/state behavior with `Rigidbody2D`, `Collider2D`, `SpriteRenderer`, and planar `Vector2` locomotion.
- The runtime assembly directly references `Unity.2D.Tilemap.Extras`.

### Editor tooling

- Prototype editor tooling contains approximately 3,242 lines.
- `PrototypeSceneBootstrapper` creates 2D rigidbodies, 2D colliders, sprites, grids, tilemaps, an orthographic pixel-perfect camera, 2D lighting, and 2D renderer-dependent scene content.
- Current bootstrap/repair operations mutate project content and are not safe substitutes for validation.
- There is no committed non-mutating Unity validation entry point.

### Art and asset readiness

The project-owned asset inventory currently contains:

- 26 PNG files.
- 5 PSD files.
- 11 prefabs, all serving the current sprite prototype.
- No FBX, OBJ, Blend, DAE, glTF, or GLB model files.
- No established project-owned 3D material, rig, animation, LOD, or model-import pipeline.

This means production 3D content must be created, commissioned, or purchased later. It is not a blocker for a greybox slice because primitives and explicitly temporary assets are sufficient for the first proof gate.

### Systems that can be preserved

The following are substantially reusable, although names or references may still need cleanup:

- Input action asset, action-map separation, and most of `PlayerInputAdapter`.
- Item definitions, item database, inventory data, inventory capacity, and save DTOs.
- Survival-resource rules and most survival calculations.
- Save service, JSON persistence location, save versioning approach, and three-axis position DTO fields.
- World seed, generation-version, and chunk-identity concepts.
- Harvest yields, item-receiver/tool-source contracts, depletion state, and harvest save data.
- BigARM command intent, task concepts, storage state, threat concept, and save DTO.
- HUD information architecture and debug intent, although presentation should be revisited for 3D readability.

### Systems that need adaptation

- `PrototypeCameraTargetController`: remap look offsets to the XZ movement plane and the accepted camera basis.
- `PrototypeSurvivalState`: depend on a movement/traversal abstraction instead of `PlayerMotor2D`.
- `PrototypeHarvestInteractor`: replace `Physics2D.OverlapCircleAll` with an accepted 3D targeting method and verify camera-relative intent.
- Harvest nodes, world pickups, and dust-canister presentation: replace sprite/collider assumptions while preserving state behavior.
- Save/load coordination: replace concrete 2D runtime dependencies, define XZ/elevation semantics, and decide prototype-save compatibility.
- BigARM command and debug surfaces: replace concrete motor/controller references with accepted runtime seams.
- World settings and prop catalog: separate generation rules from sprite/tile output and introduce 3D prefab/material data deliberately.

### Systems that should be replaced or rebuilt alongside the old path

- `PlayerMotor2D` with a separately named 3D motor behind a narrow traversal contract.
- BigARM's 2D locomotion, collision, sensing, visibility toggling, and later its pathing solution.
- Tilemap/sprite output inside `PrototypeWorldGenerator` with a 3D chunk-generation backend.
- `PrototypeSpriteDepthSorter`; 3D depth and occlusion should replace sprite sorting.
- Pixel-perfect camera extensions and 2D lighting in the 3D scene.
- The 2D prototype scene bootstrap path for the new scene. Do not silently repurpose the existing bootstrapper.
- Runtime scene and world prefabs used by the 3D path.
- Production world, character, creature, resource, and structure assets.

### Save compatibility finding

- Current player and BigARM save DTOs already store `Vector3` positions.
- Current world identity stores two-dimensional chunk coordinates named X/Y. A 3D top-down world will probably interpret the horizontal plane as X/Z.
- Current save version is 6 and world generation version is 1.
- The conversion must not silently reinterpret old chunk coordinates or world output.
- Before cutover, explicitly choose one of these policies: migrate prototype saves, import only selected state, or declare pre-conversion prototype saves incompatible. Any incompatibility must be intentional and documented.

### Validation finding

- The Unity Test Framework package exists, but there are no committed runtime or editor test sources or test assemblies.
- Interactive movement feel, camera readability, scale, and BigARM behavior currently lack repeatable 3D proof.
- Conversion work should establish targeted tests and a non-mutating validation command before the old implementation is retired.

### Isometric-style implications

The selected presentation narrows several technical decisions but introduces specific proof requirements:

- Current movement applies the input vector directly on the 2D world plane. Isometric 3D movement must project input through the accepted camera basis onto the XZ traversal plane so pressing up always moves visually up-screen.
- Current camera look-ahead offsets directly on XY. Isometric look-ahead must use the same camera-relative basis as movement and must not change elevation accidentally.
- Current world chunks use X/Y coordinates. The 3D horizontal world should explicitly use X/Z semantics internally or provide an unambiguous compatibility mapping.
- True orthographic isometric projection and mild perspective isometric-style projection should be compared. Orthographic preserves scale; mild perspective may improve depth and physical presence. The checklist does not silently choose between them.
- A fixed yaw makes asset composition and procedural landmark readability more predictable, but objects can consistently hide the same areas. Occlusion handling is therefore a core system, not a polish task.
- Walls, canyon lips, BigARM, ruins, and large hunt targets need an accepted visibility treatment such as camera-side fading, cutaways, selective transparency, silhouettes, or carefully constrained placement.
- Three-dimensional depth buffering replaces sprite sorting, but transparent materials, decals, particles, and UI markers still need explicit render-order and visibility rules.
- Continuous 3D character rotation can replace sprite-direction sets, but locomotion blend trees, aim direction, interaction facing, and animation interruption still need to match camera-relative controls.
- The fixed view should influence model detail placement, silhouettes, pivots, colliders, landmark profiles, lighting direction, and LOD evaluation. Assets should be judged from the game camera, not only in a model viewer.

### Documentation impact

The following current documents encode 2D, pixel-art, tilemap, or `Rigidbody2D` assumptions and will require review if the 3D direction is accepted:

- `AGENTS.md`
- `Docs/WORLD_BASIS.md`
- `Docs/AGENT_AND_UNITY_PRACTICES.md`
- `Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md`
- `Docs/IMPLEMENTATION_SEQUENCE.md`
- `Docs/MOVEMENT_CAMERA_STANDARD.md`
- `Docs/URP_2D_STANDARD.md`
- `Docs/ART_ANIMATION_STARTER.md`
- `Docs/PROJECT_BASELINE.md`
- `Docs/PROJECT_STATUS.md`
- `Docs/RESEARCH_PLAN.md`
- 2D/tilemap world-generation reference documents
- Gottspan and Babineaux project-memory or runtime-routing notes that describe the current implementation

Historical 2D reference documents may remain as historical/provisional evidence if clearly labeled. Canonical standards must not continue to direct new work down a retired 2D path.

## Accepted Direction And Recommended First-Slice Baseline

The isometric-style direction is accepted for planning. Exact technical values remain recommendations for later testing, not accepted canon:

- Use 3D meshes, materials, colliders, lights, shadows, and effects for the playable world.
- Keep screen-space HUD and menus as normal 2D UI.
- Use a fixed or tightly constrained top-down isometric-style camera; do not begin with free orbit.
- Keep camera yaw stable so navigation, composition, asset silhouettes, and procgen readability can be designed for a known view.
- Test strict orthographic projection against a mild perspective projection before locking the lens. Both can produce an isometric-style view, but they create different scale, depth, occlusion, and asset-composition behavior.
- Keep first-slice movement on an XZ traversal plane with modest ramps or terrain variation.
- Defer caves, stacked floors, climbing, jumping, and complex vertical navigation until the base slice is readable.
- Use primitives and simple temporary materials before production assets.
- Target stylized, strong-silhouette art before high-detail realism; the top-down camera does not reward invisible detail.
- Preserve the deliberate, weighty movement and survival tone from `WORLD_BASIS.md`.

## Decision Gates

### Gate 0 — Product definition

Owner: user, supported by Gottspan.

- [x] Confirm the overall direction: top-down, isometric-style 2.5D using 3D world assets.
- [ ] Confirm that screen-space HUD and menus remain 2D UI.
- [ ] Compare orthographic projection with mild perspective projection while keeping the same isometric-style framing.
- [ ] Choose the final fixed yaw, pitch, field of view or orthographic size, framing distance, and zoom limits.
- [ ] Decide whether optional 90-degree camera rotation is ever desirable. Recommended first-slice default: no rotation.
- [ ] Decide whether elevation is visual-only, modestly traversable, or a major gameplay system.
- [ ] Decide whether aiming/facing follows movement, right-stick direction, cursor position, lock-on, or a combination.
- [ ] Decide whether movement is camera-relative at all times. Recommended baseline: yes, with up-input mapping to projected camera-forward.
- [ ] Decide the visibility treatment for camera-side cliffs, walls, roofs, BigARM, and large enemies.
- [ ] Establish the intended art family: low-poly stylized, mid-poly stylized, hand-painted, realistic, or another named reference.
- [ ] Identify target platform(s), minimum hardware, target resolution, and frame-rate goal before production optimization.
- [ ] Establish whether models will be made internally, commissioned, purchased, or mixed.
- [ ] Record that purchases and external-account actions require separate approval.

Exit proof: a short accepted direction brief with no unresolved choice that would invalidate the first technical slice.

### Gate 1 — Protected technical spike

- [ ] Create a new project-owned 3D renderer asset without replacing `Renderer2D.asset`.
- [ ] Add the new renderer to the URP asset in a way that preserves the old scene's renderer behavior.
- [ ] Create a new, separately named isometric 2.5D prototype scene; do not overwrite `PrototypeScene.unity`.
- [ ] Keep both current build scenes and their enabled/disabled state unchanged until an explicit build-settings task.
- [ ] Create a greybox Booter using a capsule or similarly obvious placeholder.
- [ ] Implement a separate experimental 3D motor; do not rename or mutate `PlayerMotor2D` in place.
- [ ] Compare a 3D `Rigidbody` motor and any alternative only if the accepted movement requirements justify the extra spike.
- [ ] Add a fixed isometric-style Cinemachine camera without pixel-perfect components.
- [ ] Prove camera-relative movement, facing, and interaction direction with both gamepad and keyboard/mouse.
- [ ] Verify equal movement speed on screen-cardinal and diagonal input.
- [ ] Verify movement and facing remain correct at every allowed camera zoom and any allowed camera rotation.
- [ ] Add one ground plane, several walls/rocks, one ramp if elevation is in scope, and basic directional lighting/fog.
- [ ] Place deliberate camera-side occluders and prove the selected fade, cutaway, silhouette, or placement rule.
- [ ] Add one 3D collider-based harvest node and one pickup using placeholder geometry.
- [ ] Add a placeholder BigARM object with only basic follow/recall behavior.
- [ ] Capture comparable screenshots/video and playtest notes for the 2D and 3D scenes.

Exit proof: the 2.5D slice launches independently, is controllable, maintains a consistent isometric-style composition, remains readable around occluders, and demonstrates enough of Booter/BigARM scale to support a user go/no-go decision.

Stop condition: do not begin broad conversion or source production assets if the slice has not been accepted.

### Gate 2 — Architecture and validation seam

- [ ] Define a narrow player traversal interface that exposes position, velocity, teleport, movement state, and sprint state without naming a physics dimension.
- [ ] Move survival, save/load, BigARM commands, and debug consumers away from concrete `PlayerMotor2D` dependencies.
- [ ] Define a world-runtime interface for seed, generation version, chunk identity, reset, and streaming status.
- [ ] Separate deterministic generation decisions from 2D tilemap or 3D prefab/mesh output.
- [ ] Decide whether 2D and 3D implementations coexist temporarily in the same runtime assembly or use explicit assembly boundaries.
- [ ] Remove the runtime assembly's direct Tilemap Extras dependency only after the 2D generator no longer needs that assembly reference or has moved behind a legacy boundary.
- [ ] Add runtime and editor test assemblies with only the tests required for the conversion seams.
- [ ] Add a non-mutating Unity validation entry point.
- [ ] Verify compilation and serialization in both the preserved 2D scene and new 3D scene.

Exit proof: shared systems can operate against an accepted traversal/world contract, focused tests pass, and the legacy scene still opens without missing scripts.

### Gate 3 — Current-loop greybox parity

- [ ] Booter can walk, sprint, accelerate, decelerate, collide, and teleport correctly in 3D.
- [ ] Movement remains weighty and deliberate rather than slippery or arcade-like.
- [ ] The camera follows, looks ahead, zooms as accepted, and handles occluders without hiding Booter or important threats.
- [ ] World-space prompts, selection markers, health bars, and interaction indicators remain legible and correctly anchored from the isometric view.
- [ ] Survival drain, low-reserve movement effects, safe-zone detection, and recovery work.
- [ ] Inventory, item definitions, carry rules, HUD feedback, and item saving work.
- [ ] Harvest targeting, progress, tool requirements, yields, depletion, and persistence work.
- [ ] World pickups and dust-canister behavior work with 3D colliders and meshes.
- [ ] BigARM storage, recall, follow, return, scout placeholder, protection placeholder, hidden/away state, and saving work as accepted.
- [ ] Save/load round-trips player and BigARM elevation and horizontal position without axis mistakes.
- [ ] Input maps remain correctly isolated across Gameplay, UI, and System states.

Exit proof: the existing prototype survival-traversal loop is playable in a finite greybox 3D area and passes focused save/load and interaction checks.

### Gate 4 — 3D world-generation prototype

- [ ] Choose the first 3D world representation: modular chunk prefabs, generated meshes, Unity Terrain tiles, a hybrid, or another documented approach.
- [ ] Test that choice against canyon topology, streaming, collision, navigation, authoring cost, and deterministic regeneration.
- [ ] Preserve stable world seed, generation version, and chunk keys.
- [ ] Define horizontal chunk coordinates explicitly as X/Z if that is the accepted world plane.
- [ ] Separate macro layout, traversal corridors, canyon walls, dressing, landmarks, resources, and runtime deltas into testable generation stages.
- [ ] Generate the same chunk result from the same inputs across repeated runs.
- [ ] Load and unload chunks without visible stalls beyond the agreed budget.
- [ ] Prevent cracks, collider gaps, navigation breaks, and duplicate props across chunk boundaries.
- [ ] Preserve player-created/depleted state independently from regenerated visual output.
- [ ] Establish pooling or another lifecycle strategy before high-volume prefab spawning.
- [ ] Test camera sightlines and landmark readability across generated chunks.
- [ ] Prevent procedural placement from creating persistent camera-side blind corridors or fully hidden interaction spaces.
- [ ] Test BigARM navigation across chunk boundaries before scaling the world radius.
- [ ] Increment generation version when output compatibility changes.

Exit proof: a multi-chunk 3D canyon test is deterministic, streamable, navigable, save-aware, and readable from the accepted camera.

### Gate 5 — Production asset pipeline

Do not start this gate with a broad asset shopping or commissioning pass. First write and accept an asset brief containing scale, style, camera-distance requirements, topology, rig, texture, shader, collider, LOD, pivot, naming, and licensing requirements.

#### Technical asset standard

- [ ] Establish one Unity unit-to-world scale convention.
- [ ] Establish forward/up axes, pivots, origin placement, and prefab-root rules.
- [ ] Establish triangle or vertex budgets by asset class and expected screen coverage.
- [ ] Establish texture resolution and packing rules by asset class.
- [ ] Establish URP shader/material standards and the limited approved material families.
- [ ] Establish color, value, silhouette, and ground-contact rules for top-down readability.
- [ ] Evaluate every asset class from the accepted isometric camera angle and expected on-screen size.
- [ ] Put identifying detail on top-facing and camera-facing surfaces without making rotation-dependent assets visually incorrect.
- [ ] Establish collider policy: primitive, compound, simplified mesh, or special case.
- [ ] Establish LOD and culling expectations for repeated or distant content.
- [ ] Establish rig naming, humanoid/generic choice, root-motion policy, and animation import rules.
- [ ] Establish prefab nesting, variant, source-file, and optimized-runtime-asset conventions.
- [ ] Establish license and provenance tracking for third-party assets.
- [ ] Create an import-validation checklist before accepting production batches.

#### Booter

- [ ] Greybox scale model.
- [ ] Approved production model and material set.
- [ ] Approved rig.
- [ ] Idle, locomotion, sprint, turn/facing, interaction, gathering, mining, damage, downed/death, and other accepted core clips.
- [ ] Equipment attachment points and silhouette tests.
- [ ] Camera-distance readability and occlusion tests.

#### BigARM

- [ ] Greybox volume proving safe-zone, storage, and navigation footprint.
- [ ] Production model separated into the parts required for animation or upgrades.
- [ ] Locomotion, idle, turn, deploy/open, crafting/storage interaction, damage, recovery, and accepted companion-state animations.
- [ ] Simplified collision and navigation footprint.
- [ ] LODs and visibility tests appropriate to its scale.

#### World kit

- [ ] Traversable ground and canyon-floor modules.
- [ ] Canyon walls, cliffs, ledges, ramps, chokepoints, and boundary silhouettes.
- [ ] Rocks, boulders, debris, scrap, and ground dressing with controlled variation.
- [ ] Ruins, broken war machines, buried megastructure pieces, settlements, and outpost kit.
- [ ] Algae-vat and survival-economy props.
- [ ] Harvest nodes, pickups, tools, weapons, containers, and crafting props.
- [ ] Landmark kit that remains identifiable from the game camera.
- [ ] Decals and material variation that do not obscure traversal boundaries.

#### Creatures, threats, and hunts

- [ ] Small threat greyboxes before production models.
- [ ] Approved insectoid and war-machine visual language.
- [ ] Rigs, locomotion, telegraphs, attacks, hit reactions, death states, and LODs appropriate to each accepted threat.
- [ ] Hunt-scale silhouette and camera-framing tests before detailed production.

#### Lighting, VFX, and audio presentation

- [ ] Orange-sky and rust-world lighting study.
- [ ] Shadow, fog, depth, and exposure rules that preserve playability.
- [ ] Dust, impact, harvesting, machine, damage, algae, safe-zone, and accepted weatherless atmospheric effects.
- [ ] VFX budgets and pooling rules.
- [ ] Reconnect or create positional audio and surface-response rules after collider/material categories stabilize.

Exit proof: one complete representative asset family passes import, appearance, animation, collision, LOD, performance, and licensing checks before mass production.

### Gate 6 — Production world and gameplay integration

- [ ] Replace greybox content incrementally by asset family, not through one large scene rewrite.
- [ ] Keep generated placement rules independent from individual art filenames.
- [ ] Validate collision and navigation whenever an environment family changes.
- [ ] Validate interaction prompts and target selection against production silhouettes.
- [ ] Add camera occlusion handling appropriate to cliffs, BigARM, ruins, and hunt-scale targets.
- [ ] Verify transparent/fading occluders do not expose broken backsides, missing interiors, shadow artifacts, or interaction ambiguity.
- [ ] Add shadows and lighting in measured layers while tracking CPU/GPU cost.
- [ ] Reassess minimal HUD contrast and legibility against the 3D background.
- [ ] Establish animation state ownership and interruption rules for gameplay actions.
- [ ] Verify pooled object reset behavior for inventory pickups, harvest nodes, threats, and VFX.
- [ ] Add representative stress scenes for chunk density, BigARM, enemies, particles, and UI.

Exit proof: representative production-quality content supports the full accepted loop without breaking readability, determinism, saving, navigation, or performance targets.

### Gate 7 — Quality, performance, and compatibility

- [ ] Define CPU, GPU, memory, loading, and frame-time budgets for the target hardware.
- [ ] Profile the player build, not only the Unity Editor.
- [ ] Profile representative dense chunks and worst-case camera views.
- [ ] Verify batching, instancing, material counts, shadow cost, overdraw, LOD transitions, culling, and texture residency.
- [ ] Verify no frame spikes from chunk creation, destruction, collider cooking, navigation updates, or save capture.
- [ ] Test all supported control schemes and device switching.
- [ ] Test camera readability at all supported resolutions and aspect ratios.
- [ ] Test color/value readability, subtitle/UI scale, reduced camera motion options, and other accepted accessibility needs.
- [ ] Test fresh saves, migrated/imported saves if supported, corrupted saves, and generation-version mismatches.
- [ ] Run focused runtime/editor tests, non-mutating validation, and player-build smoke checks.
- [ ] Record structured playtests for movement feel, survival pressure, orientation, BigARM value, and occlusion failures.

Exit proof: agreed automated checks and playtest questions pass on an exact candidate, with remaining limitations documented.

### Gate 8 — Canon and project cutover

This gate requires fresh user approval because it changes the project's controlling baseline.

- [ ] Record the accepted 3D presentation decision and rationale in `Docs/DECISION_LOG.md`.
- [ ] Update `Docs/WORLD_BASIS.md` without altering unrelated world canon.
- [ ] Update `AGENTS.md` current snapshot and asset-structure guidance where necessary.
- [ ] Replace or supersede 2D movement, camera, rendering, art, and world-generation standards.
- [ ] Update project baseline, status, roadmap, implementation sequence, research plan, and docs index.
- [ ] Update Gottspan and Babineaux routing/memory only with verified durable facts.
- [ ] Select the 3D scene as the primary build scene through an explicitly authorized Unity task.
- [ ] Verify project build settings, renderer selection, quality settings, and player build from the exact cutover candidate.
- [ ] Mark legacy 2D docs and tools historical or superseded before considering deletion.
- [ ] Produce a reference-safe inventory of legacy assets/code and all GUID consumers.
- [ ] Obtain explicit approval for any deletion, move, package removal, or assembly cleanup.
- [ ] Remove legacy content in narrow, recoverable batches with compilation and missing-reference checks after each batch.

Exit proof: the accepted 3D scene is the proven project baseline, canonical docs agree, the project builds, and any legacy retirement was separately approved and verified.

## Risk Register

| Risk | Level | Why it matters | Required control |
| --- | --- | --- | --- |
| 3D procgen scope expansion | High | Canyon meshes, collision, seams, streaming, landmarks, and runtime deltas interact | Prove one multi-chunk representation before production breadth |
| Camera occlusion and readability | High | Cliffs, BigARM, and large enemies can hide the player or threats | Fixed first camera, occlusion prototype, repeated playtests |
| BigARM navigation | High | Large footprint plus streamed procedural terrain can produce blocked or invalid paths | Greybox footprint early; test chunk boundaries before art |
| Asset-production cost | High | The repo has no 3D model/rig/material pipeline yet | Asset brief and one representative family before bulk work |
| Save/world compatibility | High | X/Y chunk semantics and generation output change | Explicit policy, version bump, tests, no silent reinterpretation |
| Performance regression | High | Meshes, shadows, colliders, navigation, and VFX add new budgets | Target hardware and player-build profiling from the first representative slice |
| In-place conversion damage | High | Replacing serialized 2D assets can break references and destroy comparison proof | Parallel scene/renderer/code lane until cutover |
| Architecture duplication | Medium | Permanent 2D/3D forks could double maintenance | Temporary coexistence with explicit contracts and retirement gate |
| Documentation drift | Medium | Existing standards strongly direct 2D work | Track impacted docs and update only after acceptance |
| Visual identity drift | Medium | Generic asset packs could weaken the Broken World's distinctive tone | Art direction, silhouette rules, controlled palette, provenance review |

## Suggested Work Batches

These batches are ordered. Completion of a batch does not automatically authorize the next one.

1. **Direction brief** — resolve Gate 0 choices and acceptance questions.
2. **Protected isometric 2.5D technical spike** — new renderer, new scene, primitives, movement, fixed camera, one interaction, placeholder BigARM.
3. **Go/no-go review** — compare 2D and 3D feel, readability, workload, and art implications.
4. **Architecture and validation seam** — dimension-neutral contracts, tests, and non-mutating validation.
5. **Finite-area loop parity** — survival, inventory, harvesting, BigARM, and saves in a bounded greybox.
6. **3D world-generation proof** — deterministic multi-chunk canyon representation and streaming.
7. **Representative production asset family** — accepted import and performance pipeline.
8. **Production integration** — controlled replacement of greybox families and gameplay expansion.
9. **Candidate validation and playtest** — exact-build performance, regression, and feel proof.
10. **Accepted cutover** — canonical doc updates, build-scene switch, and separately authorized legacy retirement.

## Effort Shape

These are relative planning sizes, not schedule commitments:

- Small to medium: renderer coexistence, basic 3D camera, greybox lighting, single-node interaction conversion.
- Medium: 3D player motor, camera-relative controls, save-axis verification, production import rules.
- Large: finite-area loop parity, BigARM navigation and behavior conversion, representative character/animation pipeline.
- Extra large: deterministic infinite 3D canyon generation, streaming, production environment breadth, hunt-scale content, and final optimization.

The asset pipeline and 3D world generator are likely to dominate total conversion effort. The initial technical spike should remain deliberately small so those costs are measured before the project commits to them.

## Immediate Next-Step Checklist

No Unity implementation should begin until the user reviews or delegates the Gate 0 choices. Once those are accepted, the first authorized implementation task should be limited to Gate 1 and should define:

- [ ] Exact task-owned files and new asset paths.
- [ ] Existing dirty files that remain user-owned and untouched.
- [ ] The new renderer and scene names.
- [ ] The temporary motor and camera experiment boundary.
- [ ] The one interaction and BigARM behavior included.
- [ ] Input devices and camera/readability scenarios to test.
- [ ] Proof artifacts required for the go/no-go review.
- [ ] A hard stop before broad conversion, purchases, package changes, or legacy cleanup.

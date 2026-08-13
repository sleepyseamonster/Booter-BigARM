# 2.5D Isometric-Style Conversion Master Plan

This is the canonical audit, execution plan, and gated checklist for moving Booter & BigARM from its current top-down 2D pixel-art prototype to a top-down, isometric-style 2.5D game rendered with 3D environments, characters, props, lighting, and effects. `Docs/ROADMAP.md` remains the overall gameplay roadmap; this document owns the conversion program and its acceptance gates.

Use [`3D_CONVERSION_START_READINESS.md`](./3D_CONVERSION_START_READINESS.md) as the live Level A/Level B inventory for closing the current CP-06 blockers, preparing the CP-07 task contract, and later authorizing production 3D asset work.

The intended direction is **top-down, isometric-style 2.5D**: the runtime world is made from 3D assets and uses 3D rendering and collision, while a fixed or tightly constrained isometric-style camera and primarily planar traversal preserve readability. Screen-space UI can remain 2D. This does not imply a free-orbit third-person camera or unrestricted vertical traversal.

## Status And Authority

- Audit date: 2026-08-12.
- Audit basis: read-only inspection of the live repository.
- Direction authority: the user has selected a top-down, isometric-style 2.5D game using 3D world assets.
- Program owner: Gottspan owns conversion planning, task design, architecture coordination, integration order, validation expectations, status reconciliation, and evidence-based closeout.
- Product owner: the user retains final creative direction, acceptance, priority, purchasing, account, destructive-cleanup, release, and public-commitment authority.
- Planning authority: the user has authorized continued audit and creation of the full conversion plan.
- Implementation authority: CP-01 through CP-05 protected-spike work is authorized; CP-06 remains a user acceptance gate before shared-system conversion.
- Final conversion status: not yet accepted as a destructive cutover.
- Program state: CP-00 through CP-03 are complete. CP-04 and CP-05 are implemented and evidence-ready; the program is intentionally stopped at CP-06 for the user's proceed, revise, or stop decision.
- Second-pass readiness state: Amber. The spike remains useful, but the defects and missing proof in `3D_CONVERSION_START_READINESS.md` must close before CP-06 can be accepted.
- The current 2D prototype remains the working implementation until a separate 3D vertical slice passes its acceptance gate.
- No asset purchase, package installation, destructive asset removal, broad project-setting change, or release is authorized by this plan.

## Program Ownership And Control

| Role | Owns | Does not own unilaterally |
| --- | --- | --- |
| User | Creative direction, visual target, product acceptance, priority, spending, releases, and destructive decisions | Day-to-day repo sequencing or technical evidence collection |
| Gottspan | Master plan, conversion backlog, task briefs, architecture boundaries, dependency order, file ownership, integration, validation, docs, Git hygiene, and final handoffs | Purchases, external accounts, release, destructive cleanup, or turning provisional creative choices into canon |
| Babineaux | Approved Unity Editor and automation work under a Gottspan brief, including serialized-asset handling and Unity-side proof | Competing project-management path, branch/worktree operations, or unbriefed scene/settings changes |
| Task-scoped specialists | Bounded implementation or read-only research with explicit file ownership and proof requirements | Integration, scope expansion, product decisions, staging, commits, or destructive operations unless explicitly granted |

Program rules:

- Gottspan is the single integration owner even when specialists contribute.
- Every implementation batch must define done, scope, out-of-scope work, source of truth, owned files, proof, and stop conditions.
- The current 2D path remains a protected reference until the user accepts the 2.5D vertical slice and later authorizes cutover.
- Parallel work may not edit the same scene, prefab, renderer asset, project setting, or coupled script at the same time.
- A completed batch authorizes no package install, purchase, build-settings switch, legacy deletion, push, release, or next batch by implication.

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

The world fantasy does not need to be reinvented to change presentation. The current sentence that declares a 2D pixel-art game should be revised at Gate 8 only after the 2.5D implementation and cutover candidate have been accepted.

## Executive Audit Result

The project should **not** be restarted in a blank Unity repository.

The safe path is to keep the current project and create a parallel isometric-style 3D prototype scene, renderer path, movement implementation, and world-output implementation. This preserves useful game-state systems and the existing prototype as a comparison and recovery point.

The conversion is feasible because the project is still a prototype and several important systems are data- or state-oriented. It is nevertheless a major presentation and world-runtime change because the most coupled systems are central ones: player movement, BigARM locomotion, world generation, camera presentation, scene construction, and asset production.

## Current-State Audit

### Repository safety

- Planning audit branch: `main`; local commits are ahead of `origin/main`, which is repository state rather than push or release authority.
- The worktree contains user-owned changes in prototype ground art.
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
- All quality levels reference the same URP asset, so changing that asset can affect every quality tier.
- The project uses linear color space. Depth texture and opaque texture are disabled in the URP asset and must be enabled only if a proven 3D feature requires them.
- No approved runtime navigation solution has been selected. Do not add a navigation package merely because 3D is planned; select it against the BigARM and world-streaming requirements.
- The AI Navigation package is not installed. Default NavMesh project settings contain only the standard Humanoid agent definition, which is not proof of a suitable BigARM navigation setup.
- Addressables is not installed. The world/asset pipeline must first prove whether direct references and pooling are sufficient or whether content-scale and loading requirements justify an Addressables batch.

### Scenes and prefabs

`PrototypeScene.unity` is structurally 2D. Its serialized content includes:

- 2 `Rigidbody2D` components.
- 1 `BoxCollider2D` and 5 `CircleCollider2D` components.
- 7 `SpriteRenderer` components.
- 8 `Tilemap` components across 6 `Grid` components.
- A pixel-perfect main camera and a Cinemachine pixel-perfect extension.
- Sprite/tilemap lighting and depth-ordering assumptions.

`SampleScene.unity` is also an orthographic 2D scene with a Global Light 2D. Both scenes are enabled in Build Settings, and `BuildAutomation` builds every enabled scene. A conversion lab must not silently enter player builds.

The existing prototype prefabs are sprite-based resource, boulder, and tall-prop placeholders. They should remain available as 2D reference content during the 3D spike rather than being converted in place.

### Runtime code

- Runtime source contains 46 C# files and approximately 6,317 lines.
- Nine runtime files directly use the main low-level 2D physics, tilemap, or sprite-rendering APIs.
- Additional systems reference 2D-bound classes such as `PlayerMotor2D` or `PrototypeWorldGenerator`, so direct API counts understate the dependency surface.
- `PrototypeWorldGenerator.cs` is approximately 1,843 lines and is strongly coupled to tilemaps, rule tiles, sprite layers, and sprite props.
- `PrototypeBigArmAiController.cs` is approximately 578 lines and mixes reusable task/state behavior with `Rigidbody2D`, `Collider2D`, `SpriteRenderer`, and planar `Vector2` locomotion.
- The runtime assembly directly references `Unity.2D.Tilemap.Extras`.
- The input asset has reusable `Gameplay`, `System`, and `UI` maps, but it also contains Touch, Joystick, and XR schemes plus currently unused `Attack` and `Jump` actions. Conversion planning must not silently treat all of them as supported conversion scope.
- Current gameplay components independently resolve and enable action maps; map presence alone does not prove centralized mode switching or that gameplay is suppressed while prototype UI is open.

### Editor tooling

- Prototype editor tooling contains approximately 3,242 lines.
- `PrototypeSceneBootstrapper` creates 2D rigidbodies, 2D colliders, sprites, grids, tilemaps, an orthographic pixel-perfect camera, 2D lighting, and 2D renderer-dependent scene content.
- Its full scene-build command replaces `PrototypeScene.unity` and rewrites Build Settings to enable only `PrototypeScene` and `SampleScene`; it must never be used as a conversion-scene builder or harmless repair/validation step.
- Current bootstrap/repair operations mutate project content and are not safe substitutes for validation.
- CP-00 began without a non-mutating Unity validation entry point. CP-02 added `ConversionBaselineValidator`; the second-pass audit found that its camera-to-renderer relationship check still needs hardening before CP-07.
- `BuildAutomation` is dimension-neutral and reusable, but its enabled-scene behavior makes Build Settings a controlled cutover surface.

### Art and asset readiness

The project-owned asset inventory currently contains:

- 26 PNG files.
- 5 PSD files.
- 11 prefabs, all serving the current sprite prototype.
- No FBX, OBJ, Blend, DAE, glTF, or GLB model files.
- No established project-owned 3D material, rig, animation, LOD, or model-import pipeline.

This means production 3D content must be created, commissioned, or purchased later. It is not a blocker for a greybox slice because primitives and explicitly temporary assets are sufficient for the first proof gate.

### Physics, layers, and navigation readiness

- The 3D physics manager is on default gravity with automatic simulation, trigger queries enabled, and an all-open collision matrix.
- The project has no conversion-specific layers for BigARM, interactables, pickups, threats, occluders, or streamed world geometry.
- The only project-specific named physics layer is `Player`; existing custom sorting layers are 2D presentation state, not a 3D collision policy.
- A 3D layer and collision matrix must be designed from actual interaction requirements, not added speculatively.
- The default NavMesh Humanoid agent has a 0.5 radius and 2-unit height. BigARM needs its own measured footprint if NavMesh is accepted.
- Dynamic or streamed-world navigation has not been proven. It is a Gate 4 design problem, not an assumed package checkbox.

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
- `PrototypeSaveService.Save` writes directly to the destination with `File.WriteAllText`; it has no temporary-file swap or backup. Load failures are caught, but save failures are not converted into a result object.
- The conversion must not silently reinterpret old chunk coordinates or world output.
- Before cutover, explicitly choose one of these policies: migrate prototype saves, import only selected state, or declare pre-conversion prototype saves incompatible. Any incompatibility must be intentional and documented.
- Save hardening is not required to prove the first isometric spike, but it must be addressed or explicitly risk-accepted before the converted path becomes the production baseline.

### Validation finding

- The Unity Test Framework package and a focused conversion Editor test assembly now exist. There is not yet a dedicated runtime/PlayMode conversion suite.
- The current eight-test suite covers movement-basis math, baseline structure, direct harvest yield, and direct BigARM recall positioning; it does not yet cover the complete runtime interaction matrix.
- The second-pass audit found that the green suite does not fail on the unsupported kinematic-velocity warning produced by direct BigARM recall. Repair the behavior and warning policy before CP-07.

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

## System Migration Matrix

`Preserve` means the current behavior or data contract should remain. `Adapt` means retain the owner but change dimensional or presentation dependencies. `Split` means separate reusable rules from 2D execution. `Replace` means build a parallel 3D owner. `Retire later` never authorizes deletion before Gate 8.

| Area | Current owner or asset | Audit finding | Disposition | Conversion target and proof |
| --- | --- | --- | --- | --- |
| Input asset | `InputSystem_Actions.inputactions` | Correct map separation; broader schemes/actions than proven product scope | Preserve and audit | Gamepad plus keyboard/mouse work in the accepted isometric basis; unsupported schemes are explicitly classified |
| Input adapter | `PlayerInputAdapter` | Move/look/facing intent is reusable, but facing is represented as `Vector2` | Adapt | Expose screen intent separately from world-space facing; no camera or physics ownership in input code |
| Player movement | `PlayerMotor2D` | Entire physics execution is `Rigidbody2D`/XY/sprite-sorting bound | Replace alongside | 3D motor proves weight, collision, slopes if accepted, equal diagonal speed, teleport, and save restore |
| Camera target | `PrototypeCameraTargetController` | XY look-ahead and player transform coupling | Adapt or replace | Camera-relative XZ look-ahead with fixed isometric composition and no elevation drift |
| Camera rig | Main Camera plus Cinemachine pixel-perfect setup | Orthographic 2D renderer and pixel-perfect extensions | Replace alongside | Isometric rig compares orthographic and mild perspective, then locks accepted lens and occlusion behavior |
| Survival | `PrototypeSurvivalState` and save/HUD | Rules are reusable; concrete `PlayerMotor2D` reference is not | Adapt | Dimension-neutral traversal/sprint input; safe-zone distance and reserve round-trip verified in 3D |
| Items and inventory | Item defs, database, inventory, slots, summaries, save data | No 2D rendering or physics dependency | Preserve | Existing capacity, mass, transfer, and serialization behavior passes focused regression tests |
| Inventory UI | `PrototypeInventoryHud` and UI controller | Immediate-mode screen UI is dimension-neutral but prototype-only | Preserve for spike, replace later | Spike keeps readable UI; production pass moves to approved Canvas/UI path without changing inventory rules |
| Harvest rules | Yields, entries, kind, item receiver/tool source, node save data | Primarily plain data and interfaces | Preserve | Same deterministic yield and depletion behavior under a 3D presentation |
| Harvest node | `PrototypeHarvestNode` | State and yield logic are mixed with `Collider2D`, sprite pickup output, and sprite visibility | Split | Reusable harvestable state plus 3D collider/presentation/drop spawning; node IDs remain stable |
| Harvest targeting | `PrototypeHarvestInteractor` | Uses radial `Physics2D` query and nearest-object selection | Replace query path | Accepted 3D sphere/capsule/cone or aim query respects camera-relative facing, occlusion, masks, and target priority |
| World pickup | `PrototypeWorldItemPickup` | Sprite renderer, `CircleCollider2D`, and 2D trigger assumptions | Replace presentation | 3D mesh/collider/trigger and pooled lifecycle preserve item/amount/pickup-delay semantics |
| Dust canister | Canister, controller, and save data | Save DTO is 3D-ready; controller deployment is planar `Vector2`; object is sprite based | Split and adapt | 3D deploy/pickup placement, collision, fill feedback, and saved elevation work without changing inventory contract |
| Save service | `PrototypeSaveService` | JSON/path owner is dimension-neutral, but writes directly to the destination without an atomic swap or backup | Preserve initially; harden before production | Fresh save/load and failure tests pass; production candidate has an accepted write-integrity policy |
| Save DTOs | Save schema, player, BigARM, survival, inventory, harvest, canister | Player/BigARM already use `Vector3`; chunk identity uses X/Y semantics | Adapt and version | Explicit XZ mapping, schema version change only when layout/meaning changes, no silent old-world reinterpretation |
| Save coordination | `PrototypeSaveLoadController` | Concrete references to 2D player/world/BigARM owners | Adapt behind serialized-safe seams | Both preserved 2D and conversion scene can capture/apply their own runtime owners during coexistence |
| World identity | `PrototypeWorldIdentity` | Seed/version concept is reusable; chunk property names imply XY | Adapt | Explicit horizontal coordinate contract and migration policy; deterministic identity tests |
| World generation | `PrototypeWorldGenerator` | Streaming, deterministic hashes, selection, painting, fallback texture creation, props, and renderer setup are one 1,843-line class | Split, then replace output | Pure chunk plan/sampler separated from lifecycle and 3D realization; deterministic snapshot plus multi-chunk streaming proof |
| Ground masks | `RuleGroundMasks` | Specific to 2D Rule Tile adjacency | Retire later | No 3D dependency; retain only while the legacy generator remains supported |
| World settings/catalog | `PrototypeWorldSettings` and prop catalog | Weighted GameObject selection can be reused; categories, size, placement, collision, and biome semantics are too thin for production 3D | Adapt | Authored 3D spawn definitions include footprint, rotation, slope, spacing, landmark, collision, and LOD rules as needed |
| BigARM task logic | `PrototypeBigArmAiController` | Task concepts are valuable, but decision, perception, movement, presentation, and hiding are one 578-line 2D controller | Split | Separate task state/decision, perception, navigation, execution, presentation, and persistence with understandable transitions |
| BigARM commands | Command adapter and threat signal | Intent is reusable; player velocity/position assumptions are 2D | Adapt | Camera/world-facing threat placement and commands operate through accepted player/BigARM seams |
| BigARM navigation | Direct `Rigidbody2D.MovePosition` | No pathfinding despite design requirement | Replace | Measured BigARM agent traverses accepted streamed topology, replans at intervals, and fails safely when no path exists |
| Depth sorting | `PrototypeSpriteDepthSorter` and `SortingGroup` setup | Entirely sprite-specific | Retire later | Depth buffer plus explicit transparent/VFX/world-space UI rules; no sprite-sort dependency in the conversion scene |
| Debug/system controls | Debug overlay and system input adapter | Useful intent; concrete 2D motor/generator fields | Adapt | Dimension-neutral diagnostics show coordinates, chunk state, save status, and relevant 3D movement data |
| Runtime assembly | `BooterBigArm.Runtime.asmdef` | Direct Tilemap Extras reference couples the only runtime assembly to 2D | Preserve during coexistence, then split or clean | 3D work does not require Tilemap Extras; legacy dependency removed only after reference-safe retirement |
| 2D bootstrap/inspectors | `PrototypeSceneBootstrapper` and generator/settings inspectors | Large, mutating, and deeply 2D-specific | Preserve as legacy; do not repurpose | New conversion tooling is separate, minimal, idempotent where practical, and never used as validation by accident |
| Build automation | `BuildAutomation` | Generic and reusable; builds every enabled scene | Preserve and guard | Conversion lab remains outside enabled player builds until explicit build-settings authority; exact scene list validated |
| Renderer/settings | URP asset, `Renderer2D.asset`, quality settings | One renderer entry shared by all quality levels | Add parallel path | New 3D renderer is added without changing default index; conversion camera selects it explicitly until cutover |
| Scenes/prefabs | Two enabled 2D scenes and eleven 2D prefabs | No existing 3D runtime content | Preserve legacy; add parallel | New scene and prefabs have distinct GUIDs and folders; no in-place component swaps |
| Art/materials/animation | Sprite/PSD assets; empty project Materials/Audio/VFX roots | No 3D production pipeline or models | Build new pipeline | One representative asset family passes source, import, rig, material, collider, LOD, camera, performance, and license checks |
| Tests/validation | Test package only | No test assemblies or non-mutating Unity validator | Add before shared refactors | Focused EditMode/PlayMode suites, structural validator, compile/import, interactive checks, and build smoke tests are distinct |
| Documentation | Canonical standards and snapshots | Strongly encode 2D/pixel/tile assumptions | Stage updates | Planning/status update now; implementation standards only when corresponding evidence exists; final canon at cutover |

## Target Conversion Architecture

This is the architectural destination Gottspan will protect while allowing individual implementations to evolve through evidence.

### Coordinate and input contract

- Unity world uses X/Z as the horizontal traversal plane and Y as elevation.
- The input layer continues to expose two-dimensional player intent.
- A single camera-basis owner converts screen intent into normalized planar world direction.
- Movement, facing, aiming, camera look-ahead, interaction queries, threat pings, and BigARM commands use that same accepted basis instead of duplicating conversions.
- Diagonal input is clamped before speed is applied.
- Optional camera rotation, if ever accepted, changes the shared basis rather than rewriting gameplay systems.

### Scene and renderer topology

- The preserved 2D scenes continue to use renderer index `-1`, which resolves to the existing default 2D renderer during coexistence.
- A separately created 3D renderer is added as a non-default renderer in the existing URP asset.
- The conversion camera explicitly selects the 3D renderer until final cutover.
- The conversion lab is a separate scene and initially stays out of Build Settings.
- The finite vertical slice becomes a separate scene or a deliberate evolution of the lab only after the technical spike is accepted.
- Production scene, UI/bootstrap scene, test scenes, and stress scenes remain role-specific rather than accumulating into one monolith.

Proposed paths, subject to the owning implementation brief:

- `Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity`
- `Assets/_Project/Settings/Rendering/URP/Renderer3D.asset`
- `Assets/_Project/Scripts/Runtime/Traversal/`
- `Assets/_Project/Scripts/Runtime/Camera/`
- `Assets/_Project/Scripts/Runtime/World/`
- `Assets/_Project/Scripts/Runtime/BigArm/`
- `Assets/_Project/Scripts/Editor/Validation/`
- `Assets/_Project/Tests/Runtime/`
- `Assets/_Project/Tests/Editor/`
- `Assets/_Project/Art/3D/`, `Materials/`, `Prefabs/`, `VFX/`, and `Audio/` using stable production-oriented subfolders once the asset standard is approved

### Runtime ownership layers

1. **Authored definitions** — item defs, world rules, asset catalogs, tuning, and templates in project assets.
2. **Pure rules and state** — inventory, survival, yields, save DTOs, generation inputs/results, and BigARM task state without renderer or physics ownership.
3. **Runtime orchestration** — scene-resolved owners coordinate input, traversal, interactions, saving, chunks, and BigARM.
4. **Physical execution** — 3D movement, colliders, queries, navigation, and streaming lifecycle.
5. **Presentation** — models, animation, materials, lighting, VFX, audio, UI, occlusion, and camera composition.

Rules/state must not depend on presentation. Presentation may observe rules/state but must not become the save-file owner.

### Parity versus repair rule

Conversion batches preserve accepted behavior; they do not automatically authorize unrelated gameplay redesign or cleanup. If an existing issue blocks the conversion proof—such as input ownership, save integrity, stable IDs, or BigARM pathing—it becomes an explicit task in the owning conversion batch. Other defects or design changes remain separate backlog work so “conversion” does not become an unlimited refactor.

### Serialized seam rule

Unity does not serialize ordinary interface fields as direct Inspector object references. The architecture batch must choose a Unity-safe seam—such as an abstract `MonoBehaviour` base, a concrete facade, or an explicitly resolved component interface—rather than scattering `MonoBehaviour` fields plus unchecked casts. The selected seam must keep the legacy scene serializable during coexistence.

### World-generation pipeline

The current monolithic generator should be decomposed in this order:

1. Stable world and chunk coordinate types.
2. Pure deterministic sampling and hash functions.
3. A chunk plan describing terrain class, traversal, height/elevation intent, landmarks, resources, props, and stable IDs.
4. A 2D legacy realization adapter only for as long as comparison is useful.
5. A 3D realization layer that creates or retrieves meshes/prefabs, colliders, navigation data, and presentation.
6. A chunk lifecycle owner for queueing, loading, activation, pooling, unloading, and cancellation.
7. A runtime-delta store for depletion, destruction, placement, discovery, and other persistent changes.

The first 3D world proof should not attempt the final infinite canyon algorithm. It should prove that the representation can express a canyon route, load adjacent chunks without seams, preserve stable IDs, and support Booter and BigARM.

### BigARM architecture

- **Perception:** nearby threats, resources, player state, route availability, and task context.
- **Decision:** priority, hysteresis/cooldowns, interruption rules, and understandable task state.
- **Navigation:** path request, agent footprint, route following, replanning interval, stuck detection, and failure handling.
- **Execution:** follow, return, harvest, protect placeholder, scout placeholder, and away-state transitions.
- **Presentation:** model, animation, audio, storage access, markers, visibility, and occlusion.
- **Persistence:** position, inventory, task-relevant state, and away-state information approved for saving.

The conversion must not copy the current monolithic controller into a 3D class and merely change `Vector2` to `Vector3`.

### Save and compatibility architecture

- Keep the current JSON service and versioned schema approach.
- Increment the save schema only when serialized layout or meaning changes; increment generation version when deterministic world output changes.
- Store world-space positions as `Vector3`, but define X/Z horizontal semantics explicitly.
- Give generated and authored persistent objects stable IDs that do not depend on transient instance order.
- Choose and test one policy before public cutover: full prototype-save migration, selected-state import, or explicit incompatibility.
- Never load a 2D world identity as a 3D world by silently remapping axes or generation version.

### Validation architecture

- **Structural:** repo health, diff checks, GUID/reference inspection, asmdef boundaries, forbidden folders, and enabled-scene checks.
- **EditMode:** camera-basis math, coordinate conversion, deterministic chunk plans, stable IDs, inventory/survival regressions, save migration, and asset validators.
- **PlayMode:** movement/collision, interaction targeting, pickup lifecycle, safe zone, save round-trip, BigARM state transitions, chunk lifecycle, and occlusion behavior where automatable.
- **Interactive:** camera feel, weight, silhouettes, occlusion, landmark recognition, animation, lighting, and controller feel.
- **Build/performance:** exact enabled scenes, player-build smoke test, target hardware profiling, memory, frame time, streaming spikes, and worst-case views.

Each check records what it proves. Compile success is not camera proof; a player build is not proof of save migration or game feel.

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

## Decision Register

Open decisions are not blockers to finishing this plan. They become gates only before the batch that would make them expensive to reverse.

| ID | Decision | Owner | Recommended starting position | Required before |
| --- | --- | --- | --- | --- |
| D-01 | Screen-space HUD and menus | User | Keep conventional 2D UI; use world-space markers only where they improve spatial understanding | Vertical-slice UI work |
| D-02 | Orthographic versus mild perspective | User after comparison | Start with fixed orthographic isometric framing and compare one mild-perspective variant | Gate 1 acceptance |
| D-03 | Camera rotation | User | Fixed yaw; no player rotation in the first slice | Gate 1 build |
| D-04 | Elevation scope | User | Modest slopes/ramps only; defer jumping, climbing, stacked floors, and caves | Movement and world-representation selection |
| D-05 | Facing and aiming | User | Movement-facing for basic interaction; preserve right-stick/mouse aim intent for later combat evaluation | Interaction slice |
| D-06 | Art family | User | Stylized, strong silhouettes, limited material families, and detail designed for game-camera distance | Asset brief |
| D-07 | Target platform and performance floor | User | Do not assume beyond the current desktop development environment | Representative asset and optimization budgets |
| D-08 | Asset sourcing | User | Greybox internally first; commission/purchase only against an accepted asset brief | Any external asset action |
| D-09 | Supported input schemes | User with Gottspan recommendation | Gamepad primary and keyboard/mouse first-class; treat Touch, Joystick, and XR as uncommitted | Input acceptance matrix |
| D-10 | Player 3D movement mechanism | Gottspan based on spike evidence | Test a constrained 3D `Rigidbody` first; compare alternatives only against explicit failure criteria | Shared traversal seam |
| D-11 | 3D world representation | Gottspan with user readability acceptance | Compare modular chunk meshes/prefabs and hybrid approaches; do not assume Terrain is the answer | Gate 4 implementation |
| D-12 | BigARM navigation solution | Gottspan; package install remains gated | Select only after BigARM footprint and streamed topology exist | Navigation implementation |
| D-13 | Prototype-save policy | User informed by Gottspan | Prefer explicit selected-state import or incompatibility over a fragile full world migration unless saves have real user value | Cutover candidate |
| D-14 | Final cutover and legacy cleanup | User | Preserve the 2D baseline until exact-candidate proof is accepted | Gate 8 |

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

- [ ] Capture the preserved 2D baseline commit, scene list, renderer default, relevant screenshots, and known unverified behavior before Unity writes begin. Commit, hashes, hierarchy, and renderer state are recorded; the second-pass audit found the 2D screenshot is still missing.
- [x] Create a new project-owned 3D renderer asset without replacing `Renderer2D.asset`.
- [x] Add the new renderer to the URP asset in a way that preserves the old scene's renderer behavior.
- [x] Create a new, separately named isometric 2.5D prototype scene; do not overwrite `PrototypeScene.unity`.
- [x] Keep both current build scenes and their enabled/disabled state unchanged until an explicit build-settings task.
- [x] Create a greybox Booter using a capsule or similarly obvious placeholder.
- [x] Implement a separate experimental 3D motor; do not rename or mutate `PlayerMotor2D` in place.
- [x] Use the 3D `Rigidbody` motor for the spike; no accepted requirement justified a second motor experiment.
- [x] Add a fixed isometric-style Cinemachine camera without pixel-perfect components.
- [ ] Prove camera-relative movement, facing, and interaction direction with both gamepad and keyboard/mouse.
- [x] Keep Touch, Joystick, XR, `Attack`, and `Jump` outside the first slice unless D-09 or another accepted decision brings them into scope.
- [x] Verify equal movement speed on screen-cardinal and diagonal input through focused camera-basis tests.
- [x] Verify movement and facing remain correct at the spike's fixed yaw in both permitted projection modes; no zoom or rotation is allowed yet.
- [x] Add one ground plane, several walls/rocks, one traversable ramp, and basic directional lighting/fog.
- [x] Place deliberate camera-side occluders and prove temporary binary hide/reveal behavior.
- [x] Add one 3D collider-based harvest node, world-space marker, and one pickup using placeholder geometry.
- [x] Use temporary explicit layer masks without prematurely declaring the final collision matrix.
- [x] Add a placeholder BigARM object with basic camera-relative follow/recall behavior.
- [x] Capture comparable projection screenshots and structured live-play notes for the 3D scene.

Keyboard/mouse passed live. The gamepad half of the remaining unchecked item requires a physical-device playtest before Gate 1 can be accepted.

Exit proof: the 2.5D slice launches independently, is controllable, maintains a consistent isometric-style composition, remains readable around occluders, and demonstrates enough of Booter/BigARM scale to support a user go/no-go decision.

Stop condition: do not begin broad conversion or source production assets if the slice has not been accepted.

### Gate 2 — Architecture and validation seam

- [ ] Define a narrow player traversal interface that exposes position, velocity, teleport, movement state, and sprint state without naming a physics dimension.
- [ ] Select a Unity-serializable implementation of that seam and prove Inspector references survive reload.
- [ ] Move survival, save/load, BigARM commands, and debug consumers away from concrete `PlayerMotor2D` dependencies.
- [ ] Define a world-runtime interface for seed, generation version, chunk identity, reset, and streaming status.
- [ ] Separate deterministic generation decisions from 2D tilemap or 3D prefab/mesh output.
- [ ] Decide whether 2D and 3D implementations coexist temporarily in the same runtime assembly or use explicit assembly boundaries.
- [ ] Remove the runtime assembly's direct Tilemap Extras dependency only after the 2D generator no longer needs that assembly reference or has moved behind a legacy boundary.
- [ ] Add runtime and editor test assemblies with only the tests required for the conversion seams.
- [x] Add a non-mutating Unity validation entry point. The CP-02 preservation validator exists; extend it when the accepted seams require more coverage.
- [ ] Add an enabled-scene/renderer assignment check so the conversion lab cannot enter builds or change the legacy renderer accidentally.
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
- [ ] Select or reject an added navigation package only after the streamed-world experiment defines the real baking/update requirements.
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
- [ ] Establish the DCC source and Unity interchange format. Do not depend on direct `.blend` import unless the build/import environment is intentionally prepared for it.
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

- [x] Record the approved conversion direction and Gottspan program ownership in `Docs/DECISION_LOG.md`.
- [ ] Record the user's final acceptance of the exact 2.5D cutover candidate.
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
| Generator extraction regression | High | Deterministic decisions and tile rendering are interleaved in a 1,843-line owner | Characterization tests before extraction; pure sorted chunk-plan snapshots |
| BigARM controller clone | High | Changing only vectors/physics would preserve the current monolith and block real navigation | Split decision, perception, navigation, execution, presentation, and persistence |
| Build contamination | Medium | Build automation includes every enabled scene and both enabled scenes are currently 2D | Keep lab disabled/out of Build Settings; validate exact enabled scenes before every build |
| Layer/collision drift | Medium | 3D physics currently uses an all-open matrix and lacks conversion layers | Add only evidence-backed layers; test trigger and collision ownership |
| Input scope drift | Medium | The asset contains Touch, Joystick, XR, Jump, and Attack beyond the proven conversion slice | D-09 support decision; explicit test matrix; no inferred platform promise |
| Package lock-in | Medium | Navigation or asset-format packages could constrain streamed-world design | Prove requirements first; install only through an approved package batch |
| Documentation drift | Medium | Existing standards strongly direct 2D work | Track impacted docs and update only after acceptance |
| Visual identity drift | Medium | Generic asset packs could weaken the Broken World's distinctive tone | Art direction, silhouette rules, controlled palette, provenance review |

## Master Work Breakdown

The batches below are the controlled execution order. Gates above define acceptance; batches define the implementation units used to reach them. Completion of a batch does not automatically authorize the next batch.

| ID | Batch | Depends on | Principal deliverables | Exit proof | Status |
| --- | --- | --- | --- | --- | --- |
| CP-00 | Full audit and master plan | User's conversion direction | Repository audit, migration matrix, architecture target, gates, risk register, work breakdown, and ownership | Planning docs agree; structural checks pass; only planning files changed | Complete |
| CP-01 | Direction brief | CP-00 | Decisions D-01 through D-09 resolved only to the depth needed for the spike; acceptance questions attached | No unresolved choice invalidates CP-03 through CP-05; final creative locks remain gated | Complete — `ISOMETRIC_DIRECTION_BRIEF.md` |
| CP-02 | Preservation baseline and validation floor | CP-01 | Exact legacy baseline, scene/renderer/build-settings inventory, minimal non-mutating validator, initial test asmdefs, and conversion task paths | Validator reports legacy state; focused tests run; unrelated dirty files preserved | Complete — validator pass, 8 focused tests pass |
| CP-03 | Parallel renderer and conversion lab | CP-02 | Non-default 3D renderer, separate isometric lab scene, primitive environment, directional light, restrained volume, explicit camera renderer | Legacy scene retains 2D renderer; lab renders 3D; Build Settings unchanged; compile/import clean | Complete — preservation hashes and live renderer proof recorded |
| CP-04 | Player movement and camera comparison | CP-03 | Temporary 3D player motor, shared camera-basis math, orthographic/mild-perspective comparison, fixed framing, zoom limits | Gamepad and keyboard/mouse movement is readable, speed-correct, collision-aware, and accepted for feel | Evidence ready — keyboard/test/ramp pass; gamepad and user feel/lens acceptance pending |
| CP-05 | Occlusion, interaction, and BigARM scale spike | CP-04 | Camera-side occluder experiment, one harvest node, one pickup, placeholder BigARM follow/recall volume, world-space marker test | Booter and interactions stay readable; BigARM scale is viable; no broad conversion started | Evidence ready — live interaction/visibility/scale proof; user acceptance pending |
| CP-06 | Spike go/no-go | CP-03 through CP-05 | Comparison captures, playtest notes, measured risks, accepted/rejected experiments, revised effort assessment | User explicitly chooses proceed, revise, or stop; accepted choices enter decision log | Awaiting user decision — see protected spike report |
| CP-07 | Shared runtime seams | CP-06 proceed | Serialized-safe traversal/world seams, shared coordinate contract, consumer decoupling, legacy adapters, focused regression tests | Both legacy and conversion scenes serialize/compile; shared state works through accepted seams | Pending Gate 1 acceptance |
| CP-08 | Finite-area loop parity | CP-07 | Survival, inventory, harvesting, pickups, canister, BigARM storage/commands, HUD, and save/load in bounded 3D greybox | Leave–gather–pressure–return loop passes scripted checks and structured playtest | Pending Gate 2 |
| CP-09 | Save and identity migration | CP-07 and CP-08 | Explicit XZ chunk identity, schema/generation version policy, stable IDs, chosen legacy-save policy, migration/incompatibility handling | Fresh and supported legacy cases behave exactly as documented; axis/version tests pass | Pending product policy |
| CP-10 | Pure deterministic world plan | CP-07 and CP-09 | Extracted sampler/hash logic, chunk-plan data, traversal/elevation/landmark/resource fields, deterministic snapshots | Same inputs produce the same sorted plan; no renderer or scene dependency in plan tests | Pending Gate 2 |
| CP-11 | 3D chunk realization and streaming | CP-10 | Selected world representation, multi-chunk lifecycle, pooling, seam control, colliders, stable object IDs, runtime deltas | Multi-chunk canyon proof streams without cracks/duplicates and restores persistent changes | Pending world-representation decision |
| CP-12 | BigARM navigation and split architecture | CP-11 plus accepted BigARM footprint | Perception/decision/navigation/execution/presentation split, chosen navigation solution, streamed-chunk route lifecycle, stuck/failure handling | Follow/return/harvest routes work across chunks; replanning is bounded; failures are understandable | Pending navigation decision/package authority |
| CP-13 | 3D asset standard and representative family | CP-04 through CP-06; D-06 through D-08 | Scale/style/import/rig/material/collider/LOD/license standard plus one representative family | Assets pass game-camera, import, animation, collision, performance, and provenance checks | Pending creative and sourcing decisions |
| CP-14 | Booter and BigARM production assets | CP-12 and CP-13 | Approved models, rigs, materials, core animations, attachment points, colliders, LODs, and prefab integration | Core loop works with production representatives and accepted animation/scale/readability | Pending asset authority |
| CP-15 | World, resource, structure, threat, VFX, and audio integration | CP-11 and CP-13 | Controlled asset-family replacement, landmarks, resources, representative threats, lighting, occlusion, VFX, audio | Representative production slice preserves traversal readability, determinism, navigation, and loop behavior | Pending asset/content authority |
| CP-16 | Performance, accessibility, and compatibility hardening | Representative CP-14/CP-15 content; D-07 | Player-build profiling, budgets, LOD/culling/material/shadow tuning, input matrix, resolution/aspect tests, save robustness | Exact candidate meets accepted budgets and focused test/playtest matrix | Pending target-platform decision |
| CP-17 | 2.5D project cutover | CP-08 through CP-16 and user acceptance | Primary build-scene change, default renderer decision, canonical doc supersession, project status/baseline update, exact build proof | User accepts exact candidate; project builds and docs agree on the new baseline | Requires fresh user authority |
| CP-18 | Legacy 2D retirement | CP-17 plus reference inventory | Separately reviewed removals or archival moves in small GUID-safe batches; package/asmdef cleanup if justified | No missing references; build/tests pass after every batch; deleted content and recovery are reported | Requires explicit destructive authority |

### Critical dependency path

The shortest safe path to a real conversion decision is:

`CP-01 -> CP-02 -> CP-03 -> CP-04 -> CP-05 -> CP-06`

The longest technical path after a proceed decision is:

`CP-07 -> CP-09 -> CP-10 -> CP-11 -> CP-12 -> CP-15 -> CP-16 -> CP-17`

Asset work can begin at CP-13 after the camera/style brief is accepted, in parallel with later world-system work, but it must not outrun scale, camera-distance, collision, or world-representation decisions.

### Batch proof packet

Every conversion batch closes with:

- Exact starting and ending commit/state.
- Task-owned paths and preserved user-owned paths.
- Diff and serialized-asset review.
- Checks run and what each check proves.
- Unity GUI evidence when visual, physics, camera, animation, or Inspector behavior is in scope.
- Remaining risks, rejected alternatives, and follow-up gates.
- One coherent commit when the batch is verified.
- No push, package action, purchase, next-batch work, cutover, or cleanup without its own authority.

## Effort Shape

These are relative planning sizes, not schedule commitments:

- Small to medium: renderer coexistence, basic 3D camera, greybox lighting, single-node interaction conversion.
- Medium: 3D player motor, camera-relative controls, save-axis verification, production import rules.
- Large: finite-area loop parity, BigARM navigation and behavior conversion, representative character/animation pipeline.
- Extra large: deterministic infinite 3D canyon generation, streaming, production environment breadth, hunt-scale content, and final optimization.

The asset pipeline and 3D world generator are likely to dominate total conversion effort. The initial technical spike should remain deliberately small so those costs are measured before the project commits to them.

## Immediate Next-Step Checklist

The protected spike exists, but the second-pass audit prevents treating it as accepted. Use `3D_CONVERSION_START_READINESS.md` in this order:

1. Close A-01 through A-17 for editor safety, implementation corrections, and evidence integrity.
2. Resolve A-18 through A-26 with the user and record the CP-06 proceed, revise, or stop decision.
3. If the user chooses proceed, finish A-27 through A-35 as the exact CP-07 task contract.
4. Keep Level B production-asset work gated behind its creative, platform, technical-standard, sourcing, and representative-family requirements.

Unity implementation remains limited to protected-spike corrections until CP-06 is accepted. Do not begin shared conversion, purchases, package changes, Build Settings changes, cutover, or legacy cleanup by implication.

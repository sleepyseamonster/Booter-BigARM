# Ironstone Mining And Inventory Implementation Plan

Status: Built; Ironstone gates green, with one unrelated HUD regression recorded

Planning owner: Gottspan

Implementation lane: TopDown3D production runtime, editor assembly, project-owned assets, focused EditMode tests, and validation

Last audited: 2026-08-16

Implementation checkpoint (2026-08-16): Batches 0-5 are built. The production lane now contains the strict item/resource catalogs, 16-slot transactional inventory and snapshots, deterministic global-cell resource topology in the single canonical chunk plan, world-owned resource deltas, streamed node presentation, deterministic line-of-sight interaction, an exactly-once conditional gather commit, owner-bound motor constraint, authored in-place Humanoid gather clip, passive feedback HUD, explicit input modes, and a separate 4-by-4 interactive inventory canvas. Fresh focused results are inventory 8/8, resources 8/8, interaction 7/7, inventory UI 3/3, locomotion 10/10, and natural objects 15/15. The current scene rebuild and Ironstone-expanded production validator pass, and fixed-view active/depleted, prompt, receipt, and inventory evidence exists. The existing action-D-pad regression remains 7/8 due only to its unrelated custom triangle-winding assertion at triangle 263; the feature did not change that geometry. No gameplay smoke test, Development Player profile, art approval, or application-restart save proof is claimed.

## 1. Objective

Add a deterministic, harvestable Ironstone resource to the production TopDown3D procedural world. An Ironstone outcrop has a stable generated identity, survives chunk unload/reload through saved runtime deltas, and presents one deliberate gather interaction. Booter performs an in-place gathering animation; only a successfully completed action adds Ironstone Ore directly and atomically to the player's inventory. No ore pickup is spawned on the ground.

Deliver the production inventory seam needed by that loop: authoritative item definitions, fixed-capacity stack storage, transactional mutations, versioned snapshots, and a gamepad/keyboard/mouse inventory UI that uses the existing Input System architecture.

## 2. Planning Frame

### 2.1 Owner and authority

- The user remains product and creative authority and must approve this plan and its listed product defaults before implementation.
- Gottspan owns repo-wide coordination, scope control, integration order, and final evidence review.
- The TopDown3D runtime lane owns gameplay state and behavior. New production code remains in `BooterBigArm.TopDown3D.Runtime`; it must not depend on Legacy2D or Isometric prototype code.
- Babineaux owns later Unity bridge/automation work only if that lane is explicitly invoked. This plan does not authorize visible Unity interaction.
- Gear Ball owns commits, branches, pushes, pull requests, and publication. None are authorized by this planning task.

### 2.2 Source of truth, in priority order

1. `AGENTS.md` and `Docs/Agents/Gottspan/README.md` for authority and working boundaries.
2. `Docs/WORLD_BASIS.md` for setting and core-loop intent.
3. `Docs/WORLD_SYSTEMS_STANDARD.md` for deterministic identity, streaming, mutable deltas, and versioning.
4. `Docs/GROUND_CLUTTER_AND_NATURAL_OBJECT_SYSTEM.md` for the existing rock/catalog/planner/decoration contract and its explicit boundary between reconstructed cosmetics and future persistent interactive objects.
5. `Docs/INPUT_ARCHITECTURE_STANDARD.md` for Gameplay/UI/System map ownership and gamepad-first navigation.
6. `Docs/SURVIVAL_SYSTEM_STANDARD.md` for the existing player-owned, versioned-snapshot pattern.
7. Current production code and assets under `Assets/_Project/`, particularly the paths in Section 7.
8. `Docs/LEGACY_2D_BOUNDARY.md`, which makes Legacy2D reference-only for this work.

`Docs/PROTOTYPE_UI_PLAN.md` is historical guidance, not current UI authority: it describes an immediate-mode prototype while the live TopDown3D scene uses screen-space uGUI.

### 2.3 Approved planning scope

The user's request authorizes an implementation-ready plan for:

- Ironstone resource-node placement in procedural generation.
- Stable generated-node identity and chunk unload/reload continuity.
- A player gathering interaction and animation.
- Direct inventory reward after successful completion, with no ground pickup.
- A robust production inventory data model and UI.
- The minimum supporting input, locomotion lock, animation, scene-installation, validation, test, and documentation changes needed for that path.

### 2.4 Non-goals

- No gameplay implementation in this planning task.
- No crafting, recipes, equipment, tool durability, pickaxe model, tool-gating, combat rewards, merchants, containers, chests, BigARM storage, or encumbrance behavior.
- No item use, consume, equip, discard, world-drop, loot-bag, or drag-to-world behavior.
- No generic generated-entity framework and no conversion of every decorative rock into a component-bearing interactable.
- No regeneration, migration, or dependency on preserved Legacy2D inventory/harvest code.
- No application-wide save-file service in this slice. The slice owns versioned inventory/resource snapshots and streamed-world continuity; a later canonical save owner may serialize both snapshots together.
- No automatic node respawn, calendar-driven regeneration, multiplayer authority, networking, or background simulation.
- No package upgrade, render-pipeline change, collision-layer redesign, or broad project-setting change.
- No branch switch, commit, push, pull request, release, purchase, or external write.
- No claim of player feel, visual approval, build performance, or application-restart persistence from static/EditMode proof alone.

### 2.5 Planning stop condition

Planning stops when one canonical approach identifies the owning data, runtime, generation, interaction, inventory, UI, asset, test, and validation seams; sequences their implementation; states conflicts and defaults; and defines pass/fail evidence. That condition is met by this document. The next move is user approval or requested revision, not implementation.

## 3. Audited Current Repo Truth

### 3.1 Procedural world and natural objects

- `TopDown3DProceduralWorld` owns streamed chunks and destroys their GameObjects outside the unload radius.
- `TopDown3DNaturalObjectPlanner.BuildChunkPlan` deterministically plans cosmetic placements and physical formations from global cells.
- Physical rock formations already expose deterministic stable IDs and feed visible rocks, colliders, exclusion, and dust behavior.
- The production catalog already contains Ironstone-named decorative/physical rock families. Reusing their baked mesh families is appropriate; treating those definitions themselves as harvestable would make visually identical ordinary rocks ambiguous.
- Cosmetic placement IDs identify definitions, not unique instances, and cosmetic placements are intentionally reconstructed rather than persisted. They are not a valid mining-node identity source.
- `TopDown3DGeneratedChunk.GenerationToken` is a runtime generation counter, not world identity, and must not enter save keys.
- Terrain surface samples already expose the authored constraints useful for resource admission: feature, sand/gravel/bedrock/deposit weights, flow, curvature, talus, lithology, weathering, and traversal corridor.
- Existing natural-object/catalog/planner/decorator/world-settings/procedural-world files are currently modified by a separate landscape overlay. Implementation must re-read and integrate with their then-current shape; it must not overwrite or revert that work.

### 3.2 Input, locomotion, animation, and UI

- `TopDown3DInputRouter` currently resolves movement, look, sprint, and BigARM recall from the Gameplay map. It does not expose Interact or inventory mode switching.
- The input asset already has Gameplay/Interact, Gameplay/OpenInventory, and a full UI map. The live TopDown3D scene does not yet have the single EventSystem/InputSystemUIInputModule needed by an interactive uGUI screen.
- `TopDown3DPlayerMotor` reads input during its movement lifecycle. A gathering action therefore needs an explicit motor-owned locomotion-suppression seam; disabling the component is not a safe substitute.
- `TopDown3DPlayerAnimationDriver` owns a fixed Playables locomotion graph with no action override. The current player rig is Humanoid with root motion disabled.
- No production gather/pick animation clip or Ironstone Ore icon currently exists.
- `TopDown3DGameHudCanvas` is a passive overlay and intentionally removes its GraphicRaycaster. The inventory must use a separate interactive canvas rather than weakening that passive-HUD contract.
- The input asset's current Gameplay/OpenInventory action cannot by itself close an inventory after Gameplay is disabled. The canonical TopDown3D toggle therefore belongs in a System action that remains enabled across Gameplay and UI modes; the preserved Gameplay action remains untouched for legacy consumers.

### 3.3 Inventory and persistence

- There is no production TopDown3D inventory, item catalog, resource-node state store, or whole-game save owner.
- Survival demonstrates an appropriate local pattern: player-owned runtime state plus a versioned serializable snapshot, independent of chunk lifetime.
- Legacy2D inventory/harvesting is reference-only and is unsuitable as a production dependency: it contains ground-drop fallback behavior, loaded-node save scanning, weak duplicate-ID handling, and capacity/transaction semantics that do not meet this plan.
- A resource delta store must live above chunk GameObjects. Chunk destruction cannot destroy the authoritative depleted state.

### 3.4 Workspace and proof constraints

- The worktree is already broadly dirty and `main` is ahead of its upstream. All pre-existing changes are user-owned.
- A Unity editor lockfile is present. No batchmode validation may run against the open project. Unity proof must wait until the editor is closed or proceed through a separately authorized background-safe bridge path.
- The current task authorizes planning only. This document is the only planned repo change for the task.

## 4. Product Defaults Requiring Approval With This Plan

These defaults eliminate step-by-step design questions during implementation. Approving the plan approves them; changing one should revise the affected batches before code begins.

| Decision | Proposed default | Reason |
| --- | --- | --- |
| Meaning of "pick animation" | A tool-less, in-place hand-gather/mining motion; no pickaxe object | Meets the requested feedback without introducing equipment/tool scope. |
| First node economy | One successful action, one Ironstone Ore, then depleted; all values authored and tunable | Smallest complete loop with deterministic state and no hidden respawn rules. |
| Respawn | None | Avoids inventing time/calendar authority. |
| Depleted presentation | Rock remains as world geometry/collision, switches to a required depleted material state, and cannot be targeted | Preserves traversal/dust silhouette and makes persistence visible. |
| Initial inventory | 16 fixed slots; Ironstone Ore stacks to 99 | Clear bounded UI and predictable capacity. Both are authored values. |
| Mass | Item mass metadata exists, but carry-mass limiting is disabled and hidden | Preserves a future survival seam without changing current movement/economy. |
| Inventory time behavior | World time continues; Gameplay input is suppressed while the modal UI is open | Avoids creating an unauthorized global pause/time owner. |
| Resource visuals | Reuse an existing production Ironstone baked-mesh family through the canonical natural-object catalog; require distinct active/depleted materials and an ore icon | Avoids a second mesh authority and prevents active nodes from being indistinguishable from clutter. |
| Application restart | Snapshot-ready only; no disk save/load command in this slice | There is no canonical whole-game save owner to extend safely. |

If the intended animation includes a visible pickaxe, stop before implementation: that choice adds tool art, hand attachment, animation contact alignment, and equipment/tool-gating decisions that are outside this plan.

## 5. Candidate Approaches

### Approach A: Convert existing physical formations into harvestable rocks

**Source boundary:** add mutable state/components to selected `TopDown3DRockFormationPlanner` results and decorate them through the existing physical-rock path.

**Benefits:** smallest apparent generation change; immediately reuses stable formation IDs, meshes, colliders, and current placement constraints.

**Risks and breakage:** couples resource persistence to `PhysicalRockGenerationVersion`; changes intended rock density when a node is tuned; makes ordinary and harvestable Ironstone visually ambiguous; risks changing collision/dust/spawn-exclusion behavior on depletion; makes future physical-rock replanning orphan harvested deltas.

**Validation path:** extend rock planner and fusion tests, then prove resource deltas across formation-version changes. That proof is expensive because the ownership boundary itself is wrong.

**Decision:** reject. Reuse the catalog's visual family and physical-envelope constraints, not physical formations as the mutable resource authority.

### Approach B: Add a dedicated interactive-resource planner to the canonical chunk plan

**Source boundary:** add a resource definition/catalog, resource planner, stable delta store, and resource decorator; extend the one canonical natural-object chunk plan so `TopDown3DProceduralWorld` obtains a single immutable plan and passes its resource placements to the decorator. Reuse a referenced baked mesh family from the natural-object catalog.

**Benefits:** independent resource topology/version; clear active/depleted visual contract; sparse components only where interaction is needed; persistent deltas remain independent of chunk lifetime and physical-rock versioning; future resource types can use the same bounded contract without a universal entity framework.

**Risks and breakage:** touches the currently dirty generation integration seam; admission must explicitly avoid formations, traversal corridors, player spawn, and chunk-border duplication; a second call to the natural planner would create duplicate planning authority.

**Validation path:** pure deterministic planner tests, chunk-boundary ownership tests, formation/corridor exclusion tests, state snapshot tests, then focused Unity scene validation.

**Decision:** recommend.

### Approach C: Build a generic generated-entity/save framework first

**Source boundary:** replace or wrap natural objects with a general entity catalog, planner, lifecycle registry, interaction system, and save registry for resources, salvage, encounters, and landmarks.

**Benefits:** broad future flexibility and one conceptual model for many generated gameplay entities.

**Risks and breakage:** pulls unknown future requirements into the first resource; expands the blast radius across procgen, save/load, and content authoring; risks a competing authority beside the mature natural-object/formation path; delays the playable loop.

**Validation path:** repository-wide architecture and migration proof, well beyond the requested slice.

**Decision:** reject now. Cherry-pick only the narrow reusable pieces: stable definition IDs, stable instance IDs, versioned deltas, and a small interaction contract.

### Approach D: Port the Legacy2D inventory and harvest systems

**Source boundary:** move or reference `Legacy2D` item, inventory, harvest, and save classes from TopDown3D.

**Benefits:** superficially fast and already contains related concepts.

**Risks and breakage:** violates the legacy boundary; brings 2D/prototype assumptions, pickup fallback, loaded-node persistence, and insufficient transaction/catalog guarantees into production; creates two sources of truth during migration.

**Validation path:** would require replacing rather than trusting most legacy behavior.

**Decision:** reject. Legacy behavior may inform test cases only; production types and assets are new and namespaced to TopDown3D.

## 6. Recommended Architecture

Approach B wins because it preserves the canonical terrain/natural-object path while giving gameplay resources their own identity, mutable state, and version boundary. It incorporates Approach A's mesh/collider reuse and Approach C's stable contracts without inheriting either approach's coupling or scope.

### 6.1 Authority map

| Concern | Single authority |
| --- | --- |
| Static item data | `TopDown3DItemCatalog` containing immutable `TopDown3DItemDefinition` assets |
| Mutable player inventory | `TopDown3DPlayerInventory`, delegating rules to plain `TopDown3DInventoryState` |
| Resource topology/version | `TopDown3DResourceCatalog` plus `ResourceGenerationVersion` in `TopDown3DWorldSettings` |
| Per-chunk resource placement | `TopDown3DResourceNodePlanner`, whose results are included in the canonical natural-object chunk plan |
| Runtime harvested deltas | world-root `TopDown3DResourceWorldState`, keyed by stable resource ID |
| Chunk presentation | `TopDown3DResourceNodeDecorator` and per-instance `TopDown3DIronstoneNode` view/controller |
| Target selection/action timing | player-owned `TopDown3DInteractionController` and `TopDown3DPlayerActionController` |
| Locomotion suppression | `TopDown3DPlayerMotor`; callers request/release the explicit action constraint |
| Animation presentation | `TopDown3DPlayerAnimationDriver`; action state overrides locomotion but never owns reward commitment |
| Gameplay/UI/System mode | `TopDown3DInputRouter` |
| Interactive inventory screen | a separate `TopDown3DInventoryCanvas` with the scene's sole EventSystem/InputSystemUIInputModule |
| Save-file serialization | deliberately unowned/out of scope; this slice only exposes versioned capture/apply snapshots |

No subsystem may scan the scene to reconstruct authoritative inventory or harvested state. No node, UI view, animation event, or chunk GameObject owns economy truth.

### 6.2 Data contracts

#### Items

`TopDown3DItemDefinition` is a ScriptableObject with:

- stable non-empty `ItemId` (`resource.ironstone_ore` for this slice);
- display name (`Ironstone Ore`), description, category, icon;
- integer `MaxStack` greater than zero (99 initially);
- non-negative mass metadata (authored but not capacity-enforced in this slice).

`TopDown3DItemCatalog` is the only runtime lookup authority. Duplicate/empty IDs, duplicate asset references, null definitions, invalid stack limits, or missing required icons fail validation; there is no last-write-wins or synthesized fallback definition.

#### Inventory

`TopDown3DInventoryState` is a plain serializable rules object with fixed slot count and stable slot order. Empty slots store no item ID and zero quantity. Public mutations return a structured result and are transactional:

- `CanAdd` and all-or-nothing `TryAdd` for one or multiple item amounts;
- `TryRemove`;
- `TryMoveOrSwap` and `TryMerge`;
- capture/apply `TopDown3DInventorySnapshot` with an explicit schema version.

Rules must fill compatible stacks before empty slots, never exceed max stack or slot count, reject unknown item IDs, reject non-positive requests, and leave the state byte-for-byte equivalent on failure. A successful transaction emits one change notification after commit, never one per touched slot.

The rules layer supports exact mutations needed by future UI, but the first UI exposes viewing, selection, merge/reorder/swap, and close. Split, use, equip, discard, transfer, and containers remain out of scope.

#### Resources

`TopDown3DResourceDefinition` is a ScriptableObject with:

- stable `ResourceId` (`resource.ironstone_node`);
- referenced production natural-object mesh-family ID;
- required active and depleted materials;
- required item definition and fixed yield (1);
- max uses (1), interaction range, action duration, prompt text;
- deterministic placement parameters and surface/admission thresholds.

`TopDown3DResourceCatalog` is the sole resource-definition lookup. Missing mesh references, materials, item references, invalid yields/uses/durations, and duplicate IDs fail closed.

`TopDown3DResourceNodePlacement` is immutable plan data: stable instance ID, definition ID, owner chunk, world position, rotation, scale, and surface metadata required for decoration. It contains no mutable harvested state.

The stable instance ID is derived only from stable world inputs, for example:

`resource:{worldSeed}:{resourceGenerationVersion}:{resourceId}:{globalCellX}:{globalCellZ}:{candidateIndex}`

It must not include chunk load order, frame/time, `GetInstanceID`, scene path, `GenerationToken`, catalog array index, or physical-rock generation version. The planner assigns each accepted global cell to exactly one owner chunk so border cells cannot duplicate.

`TopDown3DResourceWorldState` stores only deltas from pristine generation, keyed by stable instance ID. For this slice the delta is remaining uses/depleted status. It provides versioned `TopDown3DResourceWorldSnapshot` capture/apply, ignores no unknown records silently, rejects duplicate IDs, and remains alive when chunks unload.

### 6.3 Deterministic placement and authored constraints

`TopDown3DResourceNodePlanner` uses global-cell candidates and a dedicated resource seed salt/version. It samples `TopDown3DWorldSurfaceSample` and admits Ironstone only when all configured constraints pass:

- correct lithology/deposit/bedrock relationship for Ironstone;
- acceptable slope, talus, weathering, and surface-normal thresholds;
- outside traversal corridors and the protected player-start region;
- outside physical-formation envelopes, other interactive-resource envelopes, and configured world-feature exclusions;
- within the owner chunk after deterministic border ownership;
- below an authored sparse density cap per chunk.

The canonical chunk plan gains `InteractiveResourcePlacements`. `TopDown3DProceduralWorld` builds the plan once per chunk generation and passes that same immutable instance to natural-object, resource, collision/exclusion, and any applicable dust consumers. An implementation that calls `BuildChunkPlan` separately for each consumer fails review because it can drift and duplicates authority.

`ResourceGenerationVersion` changes only when resource topology/identity intentionally changes. Changing item balance, action duration, materials, or UI does not bump it. Terrain, natural-object, and physical-rock generation versions remain independently owned.

### 6.4 Chunk lifecycle

On decorate:

1. Resolve each placement's resource definition and referenced baked mesh; fail closed in validation if missing.
2. Read the stable ID's delta from `TopDown3DResourceWorldState`.
3. Instantiate a chunk-child presentation with mesh, collider, active/depleted material, and `TopDown3DIronstoneNode`.
4. Configure the node from immutable placement/definition data plus current delta. It does not copy or own the authoritative delta.

On successful harvest, the resource state is updated immediately before the next chunk lifecycle transition. On unload, only presentation is destroyed. On reload, the same placement and stable ID are reconstructed and the delta reapplied. Depleted nodes remain visible/non-targetable and keep geometry/collision; no reward GameObject is spawned.

### 6.5 Interaction and action transaction

`TopDown3DInteractionController` performs an allocation-free bounded target query around Booter, filters for the narrow production interactable contract, applies range/facing/line-of-sight checks, and scores deterministically with stable ID as the final tie-break. It cannot select through terrain or rocks. The current target drives a passive HUD prompt.

Pressing Interact requests a gather from `TopDown3DPlayerActionController`:

1. Reject if another action is active, the node is depleted, the target is no longer valid, or the complete yield cannot fit.
2. Face the target and ask the motor to enter its explicit action constraint. The motor cancels incompatible traversal and safely suppresses planar intent without being disabled.
3. Ask the animation driver to play the required in-place Humanoid gather clip for the authored action duration.
4. During the action, cancel without reward if the node becomes invalid, its chunk unloads, the player leaves the permitted action envelope, inventory/UI mode supersedes the action, or the controller is disabled.
5. At controller-owned completion, revalidate target state and inventory capacity.
6. Commit the all-or-nothing inventory add, then update the non-failing in-memory resource delta in the same main-thread transaction boundary. Mark the transaction committed so duplicate callbacks/disable paths cannot award twice.
7. Apply the depleted presentation, release the motor constraint, return animation to locomotion, and show a passive `+1 Ironstone Ore` receipt.

Animation events may provide cosmetic contact cues only. They never own the inventory grant. If the inventory cannot accept the full reward, the action does not start or does not commit; the node remains unchanged and the HUD reports `Inventory full`. There is no partial reward and no ground-drop fallback.

### 6.6 Animation integration

- Import one project-owned, in-place Humanoid gather clip under the TopDown3D art/animation lane with root motion disabled and reference-safe `.meta` continuity.
- Extend `TopDown3DPlayerAnimationDriver` with an explicit action override state layered above its current locomotion choice. Do not replace the existing Playables graph with an Animator-controller migration in this slice.
- Match playback speed to the authored duration within a validated safe range. Locomotion resumes cleanly on complete/cancel/disable.
- The action controller is the timing and reward authority; the driver only reports/presents animation state.

### 6.7 Input and inventory UI

Add a `System/ToggleInventory` action with Tab and Gamepad North bindings. Keep the existing Gameplay/OpenInventory action unchanged for legacy compatibility but do not bind TopDown3D inventory behavior to it. `TopDown3DInputRouter` owns three explicit modes:

- Gameplay: Gameplay and System enabled; UI disabled.
- Inventory: UI and System enabled; Gameplay disabled; cached gameplay intent cleared.
- Disabled: all maps disabled during teardown.

Interact continues to use Gameplay/Interact (E and Gamepad West). UI/Cancel and System/ToggleInventory close the inventory. Mode transitions are idempotent and restore the last valid UI selection. The world is not paused.

Keep `TopDown3DGameHudCanvas` passive. Install a separate inventory canvas above it with a GraphicRaycaster, safe-area root, opaque/readable panel, 4-by-4 slot grid, selected-item detail, slots-used summary, and mass hidden while disabled. Install exactly one EventSystem with `InputSystemUIInputModule`; never create competing EventSystems. The interaction prompt and reward/full messages stay on the passive HUD.

Gamepad and keyboard navigation are first-class. Mouse selection is supported by uGUI. Opening, selection movement, reorder/swap/merge commands, cancel, and close must work without a pointer. No drag/drop-only operation is permitted.

### 6.8 Snapshot boundary

This slice exposes:

- `TopDown3DInventorySnapshot` with schema version, capacity, and ordered slots;
- `TopDown3DResourceWorldSnapshot` with schema/resource-generation versions and stable-ID deltas;
- deterministic capture/apply APIs and tests.

It does not create a second partial disk-save authority. A future whole-game save owner must capture inventory, resources, survival, player position, world seed/version data, and other required state as one coordinated save. Until then, chunk continuity and in-memory snapshot round trips are provable; closing/relaunching the application is not.

## 7. Planned Source Boundary

Exact names may adjust only to match established naming discovered at implementation start; responsibilities and ownership may not drift.

### 7.1 New runtime code

Under `Assets/_Project/Scripts/Runtime/TopDown3D/`:

- `Items/TopDown3DItemDefinition.cs`
- `Items/TopDown3DItemCatalog.cs`
- `Items/TopDown3DItemAmount.cs`
- `Inventory/TopDown3DInventorySlot.cs`
- `Inventory/TopDown3DInventorySnapshot.cs`
- `Inventory/TopDown3DInventoryState.cs`
- `Inventory/TopDown3DPlayerInventory.cs`
- `Resources/TopDown3DResourceDefinition.cs`
- `Resources/TopDown3DResourceCatalog.cs`
- `Resources/TopDown3DResourceNodePlanner.cs`
- `Resources/TopDown3DResourceWorldSnapshot.cs`
- `Resources/TopDown3DResourceWorldState.cs`
- `Resources/TopDown3DResourceNodeDecorator.cs`
- `Resources/TopDown3DIronstoneNode.cs`
- `Interaction/ITopDown3DInteractable.cs`
- `Interaction/TopDown3DInteractionController.cs`
- `Interaction/TopDown3DPlayerActionController.cs`
- `UI/TopDown3DInventoryCanvas.cs`
- `UI/TopDown3DInventorySlotView.cs`
- `UI/TopDown3DInventoryUiController.cs`
- `UI/TopDown3DInteractionFeedbackHud.cs`
- `UI/TopDown3DInventoryUiSceneInstaller.cs`

Every new Unity asset/code file receives its normal Unity-generated `.meta`; no existing `.meta` is regenerated.

### 7.2 Existing production code to extend narrowly

- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DWorldSettings.cs`: resource catalog reference and independent resource generation version/tuning.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DNaturalObjectPlanner.cs`: add immutable resource placements to the canonical chunk-plan result and coordinate one build, without moving resource rules into the natural-object planner.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DNaturalObjectDecorator.cs`: accept/reuse the already-built plan rather than rebuilding it; remain natural-object presentation owner.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DProceduralWorld.cs`: create the world-level resource state, build/pass one plan, invoke the resource decorator, and preserve state across chunk destruction.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs`: typed Interact/system toggle access and authoritative map-mode transitions.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs`: explicit action constraint with safe enter/exit behavior.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.cs`: gather action override in the existing Playables architecture.
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DGameHudCanvas.cs`: extend the existing `TopDown3DGameHudBootstrap` to install the passive interaction-feedback HUD; do not add a raycaster or interactive controls to this canvas.
- `Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DPrototypeBuilder.cs`: provision exact catalogs/assets/components/Input System references for repeatable scene construction.
- `Assets/_Project/Scripts/Editor/Validation/TopDown3DPrototypeValidator.cs`: enforce the new scene, asset, input, and reference contracts.
- `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`: add System/ToggleInventory only; preserve existing action IDs/bindings.
- `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`: serialized production wiring created through the approved builder/editor path.

The generation files above are dirty at planning time. Before implementation edits, compare their current content and diffs, assign task-owned hunks, and stop if the active overlay has changed their ownership contract incompatibly.

### 7.3 New project assets

- `Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset`
- `Assets/_Project/Settings/Items/Item_IronstoneOre.asset`
- `Assets/_Project/Settings/World/TopDown3DResourceCatalog.asset`
- `Assets/_Project/Settings/World/Resource_IronstoneNode.asset`
- `Assets/_Project/Materials/TopDown3D/Resources/IronstoneNodeActive.mat`
- `Assets/_Project/Materials/TopDown3D/Resources/IronstoneNodeDepleted.mat`
- `Assets/_Project/UI/Items/IronstoneOre.png`
- one approved in-place Humanoid gather animation under the existing project animation structure.

The resource definition references an existing catalog mesh family by stable ID. It does not copy baked meshes or introduce a prefab/mesh fallback.

### 7.4 Tests and docs

- `Assets/_Project/Tests/Editor/TopDown3DInventoryTests.cs`
- `Assets/_Project/Tests/Editor/TopDown3DResourceNodeTests.cs`
- `Assets/_Project/Tests/Editor/TopDown3DInteractionTests.cs`
- `Assets/_Project/Tests/Editor/TopDown3DInventoryUiTests.cs`
- extend `TopDown3DNaturalObjectTests.cs`, `TopDown3DPlayerLocomotionTests.cs`, and `TopDown3DActionDpadHudTests.cs` only where their owned contracts change.
- add a focused system standard/closeout update after behavior is proven; do not rewrite historical Legacy2D docs.

No new assembly is required: existing runtime/editor-test assemblies already carry the relevant Input System and uGUI references. Re-evaluate only if compilation proves an actual boundary need.

## 8. Implementation Sequence And Gates

Each batch must be independently reviewable. Do not proceed through a red gate by adding fallback behavior.

### Batch 0: Rebase the audit against live workspace truth

1. Re-read `AGENTS.md`, Gottspan instructions, source standards, current `git status`, and `Temp/UnityLockfile`.
2. Diff every existing file listed in Section 7.2 and record which hunks are task-owned versus pre-existing.
3. Confirm current catalog stable IDs, chunk-plan shape, input action IDs, runtime/test assembly references, scene installer pattern, and animation rig/import settings.
4. Confirm required visual/animation source assets are available or can be authored in-scope without a purchase/import license decision.

**Gate:** stop for user/owner direction if product defaults were not approved, a visible pickaxe is required, a licensed asset decision is needed, generation ownership has materially changed, or task edits cannot be isolated from the dirty overlay.

### Batch 1: Build and prove item/inventory rules without UI

1. Create item definition/catalog and strict validation.
2. Implement fixed-slot transactional inventory rules and structured mutation results.
3. Add player inventory owner and versioned capture/apply snapshot.
4. Author Ironstone Ore definition/catalog entry.
5. Add exhaustive pure/EditMode tests before scene wiring.

**Gate:** all inventory tests pass with no mutation on any failed operation and no Legacy2D reference.

### Batch 2: Build and prove deterministic resource topology/state

1. Create resource definition/catalog and independent generation version.
2. Implement global-cell planner with geology, slope, corridor, spawn, formation, self-spacing, border-owner, and density constraints.
3. Extend the canonical chunk-plan data so it contains resource placements once.
4. Implement delta store and versioned snapshot, then resource presentation/decorator.
5. Integrate the single-plan build and world-root state into the procedural world.

**Gate:** repeated-plan, cross-border, version-isolation, overlap, unload/reload, and snapshot tests pass. Existing natural-object/rock tests remain green. No consumer independently replans the chunk.

### Batch 3: Build and prove interaction transaction and animation

1. Add narrow interactable targeting with deterministic scoring and line of sight.
2. Add motor action constraint and regress locomotion/traversal teardown.
3. Import/configure the approved gather clip and extend the existing Playables driver with action override.
4. Implement the action state machine and exactly-once inventory/resource commit.
5. Add prompt, success receipt, full-inventory, and cancellation feedback.

**Gate:** full inventory, invalid target, unload, range exit, disable, repeated input, and duplicate completion all award zero; one valid completion awards exactly one and depletes exactly once. Root motion stays disabled and locomotion returns after every exit path.

### Batch 4: Build and prove input modes and inventory UI

1. Add System/ToggleInventory without changing existing action IDs or legacy action behavior.
2. Extend the input router's explicit map modes and clear stale gameplay intent during transitions.
3. Install one interactive inventory canvas/EventSystem above the passive HUD.
4. Bind the UI to inventory change events and commands; do not let views mutate slots directly.
5. Prove gamepad, keyboard, mouse, safe-area, selection restore, grid reorder/swap/merge, and repeated open/close behavior in focused tests.

**Gate:** exactly one EventSystem exists; Gameplay input is inactive in inventory mode; UI and System remain usable; no GraphicRaycaster is added to the passive game HUD; opening the inventory does not pause world time.

### Batch 5: Builder, validator, Unity proof, and closeout

1. Update the builder to author exact components/assets/references idempotently; update the validator to fail on missing/duplicate authority.
2. After the Unity lock is absent, allow Unity to import/compile in a background-safe manner, run the focused EditMode suite, then the production validator according to `Docs/UNITY_AUTOMATION.md`.
3. Inspect Console/import logs and serialized diffs. Verify no unrelated dirty file was staged, reverted, regenerated, or normalized.
4. Capture fixed-view visual evidence for active/depleted legibility, interaction prompt, receipt, and inventory layout.
5. Hand off keyboard/gamepad feel and visual acceptance to the user. Do not claim that hands-on proof was automated.
6. Update the system documentation with actual proven behavior and remaining save-file boundary.

**Gate:** local/Unity gates are green, visual evidence exists, user-owned smoke items are clearly separated, and the task-owned diff contains no workaround, pickup fallback, duplicate catalog/planner/state owner, or Legacy2D dependency.

## 9. Verification Matrix

### 9.1 Static and pure/EditMode requirements

| Area | Required proof |
| --- | --- |
| Catalogs | Empty/duplicate IDs, null references, invalid limits, missing icon/material/mesh/clip all fail; valid lookup is stable. |
| Inventory | Stack fill order, fixed capacity, exact max stack, add/remove, move/swap/merge, atomic batch add, unknown ID, invalid amounts, unchanged-on-failure, one event per transaction. |
| Inventory snapshot | Versioned round trip preserves exact slot order/quantity; repeated item IDs across valid stacks remain legal, while bad versions, unknown IDs, malformed empty/non-empty records, stack overflow, and capacity mismatch fail without partial apply. |
| Resource determinism | Same seed/settings/version yields byte-equivalent placements and IDs; load order does not matter; different resource version changes only resource topology. |
| Spatial admission | No duplicates across adjacent chunk borders; no corridor/spawn/formation/resource-envelope overlaps; density cap holds. |
| Resource state | Pristine nodes need no delta; harvest writes immediately; unload/reload and snapshot apply restore depleted state; bad/duplicate records fail without partial apply. |
| Transaction | Full inventory, cancellation, invalidation, unload, repeated Interact, duplicate completion, and disable paths cannot grant or consume; valid completion grants/consumes once. |
| Motor/animation | Action constraint stops input safely, traversal cannot leak through, root motion remains off, cancel/complete returns locomotion, action driver cannot award items. |
| Input/UI | Exact bindings exist, mode transitions are idempotent, stale movement clears, one EventSystem, controller navigation and cancel work, passive HUD remains raycaster-free. |
| Architecture | Runtime assembly has no Legacy2D/Isometric production dependency; no scene-scan save authority; no second chunk planner, item database, resource state store, or ground pickup. |

### 9.2 Unity-owned proof

Only after the open-editor lock is cleared or the approved bridge lane is used:

- Unity import and compile succeed with zero relevant errors.
- Focused inventory, resource, interaction, UI, natural-object, locomotion, and HUD EditMode suites pass.
- `TopDown3DPrototypeValidator` passes for the production scene and exact required assets.
- Scene/builder rerun is idempotent and preserves GUID/reference continuity.
- Console and import logs contain no new missing-script, serialization, shader/material, Input System, or Playables errors.

### 9.3 Visual, gameplay, and performance proof

Automated/local proof does not establish feel. The user-owned hands-on checklist is:

- active Ironstone reads as special but remains grounded in the existing rock language;
- depleted state is clear without looking like the rock vanished;
- target choice, range, line of sight, facing, action duration, and animation contact feel credible;
- no ore appears on the ground; success and full-inventory feedback are legible;
- inventory is readable and fully navigable on gamepad and keyboard; mouse support does not weaken focus navigation;
- UI does not collide with survival/action HUD at target aspect ratios;
- node density and chunk streaming do not produce obvious hitches.

Any player-performance claim requires a Development Player profile with representative streaming/density. EditMode allocation assertions and editor observation are not player-performance proof.

## 10. Risk Register And Failure Containment

| Risk | Containment |
| --- | --- |
| Dirty landscape overlay overlaps generation files | Re-audit live diffs in Batch 0, edit narrow hunks, never revert/normalize user changes, stop on incompatible ownership. |
| Resource IDs orphan after unrelated rock tuning | Dedicated resource version and global-cell key; exclude physical-rock version/catalog index. |
| Multiple consumers rebuild different plans | One immutable chunk plan built once and passed to consumers; validator/test assertion. |
| Ordinary Ironstone clutter looks harvestable | Required distinct active/depleted material language and prompt targeting; do not convert clutter definitions. |
| Reward duplicates on animation/input/unload races | Controller-owned state machine, committed flag, main-thread atomic transaction, animation events cosmetic only. |
| Full inventory creates ground loot or destroys node | Preflight/revalidate, all-or-nothing add, unchanged node, explicit feedback; no pickup type in the architecture. |
| Chunk unload loses depletion | World-root delta store updated at commit, node is presentation only, reload test. |
| UI input traps or duplicate EventSystems | Input router owns modes, System toggle remains live, UI Cancel closes, validator requires exactly one EventSystem. |
| Global time behavior changes unexpectedly | Do not touch `Time.timeScale`; only suppress Gameplay map/motor input. |
| Inventory overbuild expands into crafting/equipment | Fixed public contract and non-goals; reserve data fields only where currently useful, no speculative subsystem. |
| Missing gather art pressures fallback | Fail at Batch 0/asset validation; do not substitute vault/attack animation or silent instant reward. |
| Open Unity makes validation unsafe | Do not run batchmode while lock exists; defer Unity proof or use separately authorized bridge workflow. |

## 11. Implementation Completion And Stop Condition

Implementation is complete only when all of the following are true:

- one strict Ironstone Ore item definition and one strict Ironstone resource definition are canonical;
- sparse Ironstone nodes are deterministically included in the single canonical chunk plan with stable IDs and authored exclusions;
- harvested state survives chunk unload/reload and passes versioned snapshot round-trip proof;
- Booter performs the approved in-place gather animation and valid completion commits exactly one direct inventory reward;
- no ground pickup/fallback path exists and full inventory leaves the node untouched;
- the fixed-slot transactional inventory and 4-by-4 modal UI work with gamepad, keyboard, and mouse under authoritative input modes;
- prompt, receipt, full, active, and depleted states are represented;
- focused tests, Unity compile/import, production validator, serialized-diff review, and architecture checks are green;
- visual/gameplay/performance claims are labeled according to evidence, and user-owned hands-on acceptance remains explicit where not yet performed;
- docs describe the implemented contract and the outstanding whole-game save boundary.

Then stop. Do not continue into disk save, respawn, crafting, pickaxe/equipment, item dropping, containers, BigARM behavior, generic resource expansion, performance redesign, publication, or release without new authority. If implementation reaches a licensed-asset choice, an incompatible dirty-overlay conflict, a request for visible Unity control, or a need to create a canonical whole-game save owner, stop and route that decision to the user/appropriate owner.

## 12. Exact Proof Boundary At Handoff

The strongest autonomous implementation handoff this plan authorizes is: repository/static review plus Unity import/compile, focused EditMode tests, validator output, serialized-diff inspection, and captured fixed-view visual evidence after safe access to Unity is available.

That evidence can prove deterministic planning, transactional data rules, snapshot round trips, scene wiring, reference validity, and scripted lifecycle behavior. It cannot by itself prove subjective animation feel, controller comfort, final art approval, Development Player performance, or persistence across application restart. Those remain explicitly unproven until the user performs hands-on smoke testing, a player profile is captured, and a separately approved whole-game save owner serializes the provided snapshots.

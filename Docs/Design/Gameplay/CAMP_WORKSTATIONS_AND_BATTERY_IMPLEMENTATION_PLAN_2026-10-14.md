# Camp workstations and battery implementation plan

Date: October 14, 2026. Status: planning complete; ready for review and subsequent implementation authorization. This plan builds on the [workstation audit](CAMP_WORKSTATIONS_AND_BATTERY_AUDIT_2026-10-14.md). It selects a bounded camp-loop prototype and resolves routine technical choices. The recipe and charging values below are proposed prototype defaults, not accepted world canon.

## Objective and observable success

In the existing `CampLoopTesting` scene, Booter can open a stocked camp chest, craft one empty battery at a workbench, deploy and retrieve a dust canister with its contents intact, insert that canister and battery into a refinery, and retrieve a charged Arc Energy battery. Each canister, ingredient and battery has one owner at every step. Failed operations preserve contents, and a deliberate camp-loop reset restores one complete starting baseline.

The workbench supports item and equipment recipes through data, with only the battery recipe furnished initially. The refinery visibly accepts a canister and battery. The chest contains the exact items and quantities required by the selected recipe. These three services are physically local to the camp.

The implementation endpoint is an integrated, structurally validated lab loop with passing focused tests, preserved production behavior and a clear author handoff for appearance and feel. Production rollout and durable save migration are separately bounded follow-on work.

## Authority and source order

The author owns product decisions and implementation approval. Gottspan owns integration, source boundaries and closeout; Babineaux owns background Unity import, scene authoring and validation; Gear Ball owns verified local commits and any separately authorized publication. No Blender authoring or external lore retrieval is needed for primitive station visuals. These are responsibility lanes, not instructions to launch agents.

Source order is the current user request, [root agreement](../../../AGENTS.md), [world basis](../../WORLD_BASIS.md), [fixed-world architecture](../../Engineering/GREATER_WASTELAND_WORLD_ARCHITECTURE_BASELINE_2026-10-11.md), current inventory/camp standards, live code/assets, then the audit and older plans. [Local workspace routing](../../LOCAL_WORKSPACE.md) and [Unity automation](../../Engineering/UNITY_AUTOMATION.md) govern Windows execution. The world is fixed resident terrain for this scope; retained generated-world instructions do not authorize terrain generation or streaming.

Planning changes only this document and its audit routing link. Later implementation must stop on overlapping active writers, unclear scene ownership, unsafe Unity access, failed preservation checks, or a need to expand into production migration, packages, purchases, publication or gameplay redesign.

## Revalidated repository facts

The planning checkout is `main` tracking `origin/main`, initially at audit commit `3eca6e38c`. Concurrent animation work advanced HEAD during inspection. Existing dirty animation-driver, crouch, binding, foot-projection, animation-status and fused-rock paths remain outside this task. In particular, changes to `TopDown3DPlayerActionController.cs` must coexist with the current animation target dispatch; do not replace it with an older copy.

Unity is pinned to `6000.4.0f1`, Input System to `1.19.0`, URP to `17.4.0`, and Test Framework to `1.6.0`. Runtime assembly is `BooterBigArm.TopDown3D.Runtime`; Editor assembly is `BooterBigArm.Editor`; focused tests compile into `BooterBigArm.Editor.Tests`. No `Temp/UnityLockfile` or `Unity.exe` process was found during this planning check. Batch 0 must recheck both; this is not future launch permission or proof of compilation.

The production four-item catalog, 100-unit harvester capacity, 10-second collection interval, resource-only cache and session-only lab remain applicable. Concurrent untracked salvage work now supplies `Settings/Studies/CampLoopSalvage/LabItemCatalog.asset`, three salvage definitions, `CampLoopSalvageSession` and a wreck with three extraction points. Its catalog is bound to kit/cargo/cache and must be extended in place after that owner's batch is sealed, rather than replaced. Its metal plate, wire bundle and drive assembly are not this plan's battery ingredients: the existing salvage ledger currently requires every awarded part to remain present until its own reset. Consuming them in crafting would break that contract. Additional caller inspection found two critical couplings:

- `TransferSlot` reconstructs rewards from item ID/quantity, and Auto-Pack produces inventory snapshots. Both would lose canister contents unless changed with the slot contract.
- Cargo's total mass comes from inventory, while left/right mass is calculated separately from definitions. Both must use the same payload-aware unit mass to avoid contradictory load readings.

The Animation Playground reset registry is sequential and applies to registered animation participants. It is not an atomic economy reset service. Its twelve station IDs remain reserved for animation fixtures; the camp workbench and refinery must not become AP13/AP14 or masquerade as animation preview targets.

## Prototype defaults and preserved behavior

| Choice | Planned default |
| --- | --- |
| Battery assembly | 4 `resource.ironstone_ore` -> 1 `equipment.arc_battery_empty`. Abstract ore processing for this prototype. |
| Starter chest | One recipe batch: exactly 4 Ironstone Ore. Seed from the recipe reference, not a duplicated quantity list. |
| Refining | 10 dust from the docked canister + 1 empty battery -> 1 `equipment.arc_battery_charged`. |
| Timing | Explicit instant batch commit, followed by brief feedback. No job queue, fuel or processing timer. |
| Battery carry/stack | Quantity-one field supply; Either carry preference; maximum stack 1 for empty and charged states. Provisional mass 0.8 for both. It occupies a supply pocket, not a seventh equipment slot. |
| Canister carry | Empty canister may travel in Booter's kit, cargo or chest. Nonempty canister travels in Legger cargo, chest or refinery; Booter's kit rejects it. |
| Canister mass | Existing 0.8 tool mass plus 0.05 per dust unit. Fractional collection time adds no mass. |
| Collection | Preserve capacity 100, rate 10 simulation seconds/unit, cap 32 placements and no offline progress. Carried/docked canisters do not collect. |
| Craft source/output | Read the explicitly linked local chest only; output goes to Booter's kit. No automatic consumption from distant or unselected cargo. |
| Refinery sources | Explicitly selected Booter kit, reachable Legger cargo or linked local chest, subject to legal item/content policy. |
| Reset | Separate **Reset Camp Loop** action; ordinary Animation Playground reset does not replenish camp supplies. |

These defaults form one coherent implementation proposal. Author revisions to material types, quantities, canister carry policy or timed processing must update the recipe, tests and baseline before implementation. No additional economy is implied. The richer salvage recipe from the audit remains a future alternative.

Preserve terrain, twelve Ironstone nodes, seven camp rocks, camp/return routes, actor positions at scene start, kit capacity, twelve cargo mounts, movement/camera settings, survival behavior, existing input bindings, private animation assets, build routing and all Unity GUIDs. Do not modify `GreaterWasteland.unity`, its production catalog/prefabs, existing checkpoint files or excluded source archives. Shared C# extensions must retain existing behavior when the explicit camp-loop feature is absent.

## Candidate approaches

| Approach | Authority and affected boundary | Benefit | Cost and risk | Proof |
| --- | --- | --- | --- | --- |
| A. Lab opt-in typed canister payloads | Existing inventory remains owner while carried; deployed registry and refinery become owners after transfer. Extend slot/snapshot/transaction/transfer/packing code; activate through serialized lab configuration and a lab catalog. | Fits real chest/cargo UI, exact contents and current transaction architecture; confines content rollout to the requested scene. | Shared APIs must preserve payloads consistently and preserve production v1 behavior. Requires opt-in snapshot capability and tests for absence of the feature. | Payload conservation, ownership transitions, existing inventory regression and lab scene validation. |
| B. Immediate production cutover | Same inventory authority, plus authored-world save service as coordinated durable owner. Change production items, scene installation and save migrations together. | Avoids a later content cutover and directly gives production the new loop. | Broadest reference and save compatibility risk; existing four-stack tools need lossless expansion with capacity failures. Requires additional authority and acceptance beyond the requested camp-loop plan. | Migration fixtures, coordinated restart snapshots, production validators and full affected regressions. |
| C. Canister ledger with inventory references | A dedicated world/session ledger owns canister contents; slots, placements and sockets reference ledger IDs. Existing inventory remains capacity owner. | Keeps mutable contents out of ordinary item payloads; canister lifecycle is concentrated in one service. | Every transfer, pack and reset must still preserve references. Ledger and occupancy can diverge; destruction and snapshotting require joint validation. Adds a second state boundary for only one device type. | Orphan/duplicate reference checks, joint transaction failure tests and coordinated ledger/occupancy round trips. |

Select A. Use a narrow, typed canister payload rather than a generic item-component framework. It gives each carried object a complete record in the authoritative slot and avoids a shadow registry of carried contents. Production save migration from B remains a later gate. Separate loose-dust refining would reuse more current code, but would not deliver the requested filled-canister docking behavior.

## Canonical data and command contracts

**Items and recipes.** New `TopDown3DCraftingRecipe` assets contain stable recipe ID/version, ingredients, output and workstation kind. Reject unknown IDs, duplicate ingredient entries, zero/negative amounts and overflow. The battery recipe yields one empty battery. Extend the existing salvage lab catalog in place with a lab-only harvester definition replacing only that catalog's harvester reference (same stable item ID, max stack 1), and the two battery definitions. Keep all ordinary and salvage definitions, catalog GUID and salvage session binding. Assign capability through scene overrides to all lab inventory owners before first state initialization; never reconfigure live nonempty inventories or mutate the production catalog asset. Remove the ID-only starter-harvester grant through a lab scene override, then let the economy session grant one fully formed empty instance at its initialization barrier. Preserve all other starter items.

**Typed canister record.** `TopDown3DCanisterPayload` contains version, instance ID, integer dust and fractional collection seconds. Capacity comes from the explicitly linked harvester settings; validate contents against it. One canister record occupies a quantity-one slot. Deep copies are mandatory for drafts, snapshots and reset baselines. Reject payloads attached to unrelated IDs, duplicate IDs, malformed progress, quantity greater than one and payload-free canisters in the lab capability. An ordinary item-ID-only add/remove/transfer must reject stateful canisters; explicit instance commands handle them. Initialize the lab's single starter canister with an empty record once.

**Ownership and identity.** A canister instance ID survives inventory -> placement -> cargo -> chest -> socket -> placement. A separate placement ID and sequence continue to identify deployment addresses. Allocate lab instance IDs from authored world identity, session epoch and a monotonic counter; do not use Unity instance IDs. Reset advances the epoch, invalidating stale commands. The same instance must never exist in two inventories, a placement and a socket simultaneously.

**Snapshots.** Preserve ordinary inventory version-1 output and reading when canister capability is disabled. Add an explicit version-2 inventory payload format for enabled owners; preserve versions/capacities of existing production inventories and outer save DTOs. `ApplySnapshot` validates an entire candidate before touching live state. Do not pass lab v2 data into production checkpoints. Stateful placed-canister snapshots need a correspondingly versioned lab format with instance IDs; preserve the existing production placement format. Session reset/domain-reload support must reconstruct all owned records without treating a deserialize callback as a starter grant.

**Transactions.** Extend `TopDown3DInventoryTransaction` with bounded prepared participants for the placed registry and refinery state. Each participant provides captured revision, prepared replacement, lock/revalidate, install and notification. Acquire all locks in a stable order; recheck access/epoch and all revisions; install all validated replacements without callbacks; then publish guarded notifications. Preparation cannot mutate live data. Installation cannot perform fallible asset loading, allocations requiring validation, event callbacks or world effects. If this cannot be guaranteed, add rollback from complete precommit records before exposing state. Presentation exceptions never turn a committed action into a replayable failure. Avoid mutable world changes in a `condition` callback that cannot be rolled back.

**Commands.** Add explicit `Craft(recipeId)`, `DeployCanister(owner, slot, epoch)`, `PickUpCanister(placementId, destination, epoch)`, `InsertCanister(owner, slot, epoch)`, `InsertBattery(owner, slot, epoch)`, `Refine(epoch)` and `Retrieve(socket, destination, epoch)`. Commands return structured success/failure reasons. Use exact selected instance IDs, never an arbitrary first matching canister. Deploy empty tools from Booter's kit and partial canisters from physically reachable cargo; preserve contents and collection remainder. Empty pickup can return to Booter, nonempty pickup to Legger; no fallback that drops contents. Chest or station access must never teleport Legger or permit remote cargo.

**Refinery.** One canister socket and one battery socket are the authoritative station state. Closing its UI leaves inserted objects docked. Refining requires an empty battery and at least ten dust, removes exactly ten and replaces the battery with its charged definition. Below ten dust, it changes nothing. A charged battery blocks another charge. Retrieval respects capacity, content-aware carry policy and cargo mass; failed retrieval leaves the socket occupied. The workbench uses no physical output socket in this slice.

**Storage and mass.** Add an explicit camp-storage policy while preserving existing serialized enum values and resource-cache defaults. Apply content-aware admission to add, transfer, move/swap and restore, not only the UI. Use one slot-mass function in inventory capacity, cargo balance, Auto-Pack and details. Auto-Pack reorders whole canister records; it cannot rebuild them from ID/quantity or merge them. Keep `UnloadResources` resource-only; filled canisters use explicit transfer rather than becoming raw resource stacks.

**UI.** Workbench shows the selected recipe, chest ingredient availability and output capacity. Refinery shows both sockets, dust count, required amount and result. Chest adds a destination selector for Booter/Legger while keeping physical cargo checks. Linked-chest commands recheck `CanAccess` from Booter's actual position; furnish the service cluster so both station and chest remain reachable from the working approach. Reuse current UI action map, binding display names and EventSystem; no new gameplay keybind. Station modal ownership cancels gathering, blocks new lab actions and clears stale motion on close. System-menu suspension, cancel, death, lost station access, disable and unload must release listeners/leases and reject stale commands. Menus do not halt world simulation unless the existing menu owner does so. Physical reach/contact animations can follow; economic commit is never an animation event.

## World lifecycle and reset contract

| Required concern | Resolution |
| --- | --- |
| Deterministic world identity | Existing `greater-wasteland.authored.v1`; explicit recipe/station versions and session epoch. No population reseeding or generated terrain. |
| Stable generated identity | Stations/chest are authored anchors. Player-created placements retain placement identities; canisters retain their own instance identity through every owner. |
| Chunk streaming | Terrain streaming does not apply because terrain is fixed and resident. Future dressing visibility cells must not delete or own gameplay records. |
| Unload and re-entry | Unload releases views, UI and listeners. A fresh lab load establishes one new baseline; domain reload within the same session retains it rather than reseeding. |
| Authored constraints | Sample existing ground and collision, fit station approaches around camp rocks and preserve the return corridor and cargo access. No sculpting shared TerrainData. |
| Persisted deltas | Lab has no checkpoint writes. Production later needs chest seed/depletion, inventories, socket state and placements captured and validated together. No old generated-world save service. |
| Deterministic proof | Repeated baseline initialization yields the same quantities; actions conserve items/dust; two snapshots restore equivalent ownership; reset invalidates prior epoch commands. |
| Runtime budget | Two stations, one existing chest, at most 32 placed harvesters, one station modal and two sockets. Cache explicit references; no new per-frame scene scans, jobs or meshes. Refresh UI on revisions/events. |

`TopDown3DCampLoopEconomySession` owns initialization, epoch and an immutable deep starting snapshot, not another copy of live inventories. It has explicit references to actors, chest, workbench, refinery and placed registry, and requires the lab path plus existing session-only marker. Serialized capability/catalog references resolve before lazy inventory initialization; startup refuses inconsistent configuration without grants. Retain the initialization flag, epoch/counter, baseline and socket records through Unity domain reload using the existing retained-snapshot pattern; validate their joint ownership before reopening commands. A failed restore preserves data for recovery and does not restart initialization.

Its separate reset command first closes owned UI and cancels economic actions, then prepares all affected owners together. Remove this feature's canister instances and empty/charged battery items, restore one empty starter canister, clear owned placements/sockets, and set the linked chest's Ironstone count to four. Preserve every other item, ore outside that chest, the salvage wreck/awarded parts, portable camp state, vitals, actor positions, original mining depletion and animation fixtures. Label the command to disclose removal of trial batteries and replacement of chest ore. Validate complete candidate drafts, capacity, mass and salvage reward conservation before one coordinated commit. If the starter canister or seeded ore cannot fit alongside preserved contents, reject reset without loss. Disarm input until reset finishes; if validation fails, keep contents unchanged and report the cause. Never register this economy reset as an ordinary animation reset participant.

## Planned source and asset boundary

All paths below are repository-relative. Preserve each existing `.meta`; new imported assets require paired metadata. Proposed filenames may be split into partials for readability, without adding new authority layers.

| Boundary | Existing sources to change or inspect together | New sources/assets |
| --- | --- | --- |
| Instance inventory | `Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventorySlot.cs`, `TopDown3DInventorySnapshot.cs`, `TopDown3DInventoryState.cs`, `TopDown3DInventoryState.Transactions.cs`, `TopDown3DInventoryPolicy.cs`, both `TopDown3DInventoryTransferService` files and `TopDown3DPlayerInventory.cs` | `Scripts/Runtime/TopDown3D/Items/TopDown3DCanisterPayload.cs` |
| Cargo | `Scripts/Runtime/TopDown3D/BigArm/TopDown3DBigArmCargo.cs`, `TopDown3DCargoAutoPacker.cs`, `TopDown3DBigArmCargoSnapshot.cs`, `TopDown3DBigArmCargoVisuals.cs` | None unless a small typed visual adapter is required. |
| Harvester | `Scripts/Runtime/TopDown3D/Resources/TopDown3DPlacedHarvesterState.cs`, `TopDown3DHarvesterView.cs`, `TopDown3DHarvesterSettings.cs`; player action controller and current interaction target dispatch | Lab-capable record/transaction helpers under the same Resources folder. |
| Recipes and lab content | Inspect production `Settings/Items/TopDown3DItemCatalog.asset` without changing it; extend existing `Settings/Studies/CampLoopSalvage/LabItemCatalog.asset` after salvage ownership handoff; inspect `Salvage/CampLoopSalvageSession.cs` and its reward ledger | `Scripts/Runtime/TopDown3D/Camping/TopDown3DCraftingRecipe.cs`, `TopDown3DCraftingService.cs`; `Settings/Items/CampLoop/` harvester definition, battery definitions and recipe; project-authored battery icons under `UI/Icons/CampLoop/`. |
| Stations and session | Existing `Inventory/TopDown3DResourceCache.cs`, player action dispatch, shared input/menu owners | `Camping/TopDown3DCampWorkbench.cs`, `TopDown3DDustRefineryState.cs`, `TopDown3DDustRefinery.cs`, `TopDown3DCampLoopEconomySession.cs`. |
| Interface | `UI/TopDown3DInventoryUiController.cs`, `.Commands.cs`, `TopDown3DInventoryCanvas.cs`; inspect system-menu suspension/input ownership | `UI/TopDown3DCampWorkstationUiController.cs`, `TopDown3DCampWorkstationCanvas.cs`; station UI prefab under `Prefabs/TopDown3D/Camping/`. |
| Furnishing | Only `Scenes/Reference/CampLoopTesting.unity`; preserve the existing cache prefab GUID and use instance overrides | `Prefabs/TopDown3D/Camping/CampWorkbench.prefab`, `DustRefinery.prefab`; primitive materials under `Materials/TopDown3D/Camping/`. |
| Editor | Inspect existing `Scripts/Editor/TopDown3D/CampLoopTestingSceneBuilder.cs`; do not run Build/Complete | `Scripts/Editor/TopDown3D/CampWorkstationAuthoring.cs`, `CampWorkstationValidator.cs`, in the existing Editor assembly. |
| Tests | Existing inventory, transfer, packing, harvester, cargo, UI, camp-scene and authored persistence fixtures | `Tests/Editor/CampCraftingTests.cs`, `CanisterInstanceInventoryTests.cs`, `CampRefineryTests.cs`, `CampEconomyResetTests.cs`, `CampWorkstationSceneTests.cs`, `CampWorkstationUiTests.cs`. |
| Documentation | This plan and audit; update `Docs/Engineering/UNITY_AUTOMATION.md` only after real new entry points exist | Implementation receipt under `Docs/Evidence/Gameplay/CampWorkstations20261014/` or actual implementation date. |

Runtime code remains in the existing TopDown3D assembly. No new packages, assemblies, shaders, input actions, build scenes, art downloads or archive dependencies are planned. Production save sources are read-only in the first slice.

## Implementation batches and gates

### Batch 0 Revalidate ownership and baseline

Gottspan checks current branch/HEAD/index and file owners. Coordinate action-controller and scene edits with active animation work and wait for the salvage scene/catalog batch to be sealed before touching coupled files. Preserve its wreck, three points, catalog bindings and exact reward/reset contract. Babineaux checks Unity processes/lock and prepares a task-owned isolated hydrated candidate from the exact implementation state, including needed local private animation dependencies. Record hashes/GUIDs for production catalog/prefabs/scene, nine TerrainData assets, original scene content and package/build settings. Snapshot the lab's current wiring before editing. Stop if source ownership, hydration, scene state or background-safe validation is unresolved.

### Batch 1 Add recipe and lab content contracts

Add typed recipe validation, battery definitions/icons and lab harvester definition; extend the existing lab catalog with GUID continuity. Add pure craft preparation using the existing draft model: remove four ore from linked chest, add one empty battery to Booter, and commit only when both drafts and access pass. Create no station objects yet. Gate: missing/extra/invalid ingredient cases, exact output and full-kit failure pass; production catalog bytes and package pins match baseline; salvage still validates the shared lab catalog. No chest grant occurs until session initialization exists.

### Batch 2 Preserve canister records across all inventory operations

Implement typed payloads, capability-specific v2 snapshots, exact-instance commands, payload-aware policy/mass, deep copy and packing. Extend prepared transactions for non-inventory owners. Exercise atomic install/notification ordering before using it for world state. Gate: two different fills survive transfer/move/swap/Auto-Pack; a full or overweight destination loses nothing; duplicate IDs/malformed snapshots reject before mutation; ordinary production v1 inventory tests still pass. Stop if any live path can erase or create payloads through ID-only commands.

### Batch 3 Complete placement and pickup continuity

Add lab capability wiring to the existing harvester registry/action controller. Deploy removes the selected instance, preserving dust/remainder; pickup moves that same instance into the legal accessible owner. Preserve placement geometry checks, line of sight, cancellation, reach presentation and device limit. Nonempty canisters can redeploy from accessible cargo. Gate: empty/partial/full cases, collection cap, stale target, absent/full Legger, interrupted pickup, repeated commands and lab snapshot round trips pass. Existing production pickup still yields an empty tool plus loose dust with the feature absent.

### Batch 4 Implement chest and economy session

Add explicit camp-storage policy and Booter/Legger destination selection. Add session configuration and initialization/reset barrier with serialized lab overrides, including one instantiated starter canister. Seed chest from one recipe only after every owner validates. Establish reset baseline after all economic owners are ready. Gate: repeated open/enable/domain reload never reseeds; equipment and legal canisters transfer; raw resources/nonempty canisters cannot enter Booter's kit; reset restores four chest ore and one empty starter canister, clears trial batteries/refinery/placed state, preserves unrelated stacks and salvage conservation, leaves actors and animation state untouched and invalidates stale commands.

### Batch 5 Implement station state and modal interface

Wire workbench crafting and refinery insertion/refining/retrieval through explicit interaction dispatch. Add two socket visuals and a shared station modal, preserving current UI/input/menu ownership. Never reuse a preview action as economic execution. Gate: 9 dust rejects; 10 becomes 0; 13 becomes 3; canister remains; charged battery cannot charge again; capacity/access/cancel/disable failures preserve ownership; menu suspend/resume does not replay submit; gameplay/animation actions remain blocked during station modal. Close UI without refunding or ejecting docked items.

### Batch 6 Furnish and validate the camp scene

Babineaux authors only the owned station root and required lab instance overrides. Survey support, working approaches, cargo parking and existing routes before saving. Installer accepts the already-loaded correct scene, uses Undo, checks conflicts and never opens/discards another scene. Validator accepts a Scene and inspects without installing, saving or running gameplay; its CLI wrapper may open the lab only in the isolated candidate. Gate: one chest, one workbench, one refinery, one economy session, original nine terrain/collider pairs, twelve nodes, seven rocks, salvage wreck/three points/catalog binding, actor/input/camera/EventSystem ownership and twelve animation stations remain intact. Hash protected production assets and TerrainData again before accepting the scene diff.

### Batch 7 Seal evidence and hand off

Run the final relevant tests/validators against the exact candidate, inspect errors and metadata/GUID references, verify no gameplay checkpoint writes, review the entire scoped diff, and record commands/results. Document delivered behavior and outstanding user appearance/feel acceptance. Gear Ball commits only verified task-owned paths on `main`; a commit does not authorize push. Stop feature work once the lab endpoint passes. Do not proceed into production conversion, charging consumers or timed processing by momentum.

## Verification matrix

| Claim | Required proof | What it does not establish |
| --- | --- | --- |
| Recipe/chest agree | Pure recipe and seed tests; quantity/capacity validation; repeated initialization/reset tests | Economy balance or player understanding. |
| No loss or duplication | Focused instance/transaction tests across all owners, failure injection, stale epoch/revision and observer exceptions | Persistent restart continuity in production. |
| Resource/cargo rules remain physical | Inventory policy, transfer, mass/packing and access tests; existing inventory-loop regressions | Legger navigation feel or route acceptance. |
| Lab/production remain separate | Legacy v1 inventory/pickup tests, explicit absence-of-feature checks, protected asset hashes, production persistence fixtures and sentinel checkpoint-file comparison | Complete production feature rollout. |
| Controls and modal lifecycle work | Focused UI/interaction tests covering keyboard/gamepad action-map events, cancel, menu suspension and stale-submit release | Actual controller comfort. |
| Scene references are safe | Asset/meta pairs, GUID uniqueness, lab validator, existing camp/animation structure fixtures and protected TerrainData hashes | Visual placement quality or animation contact. |
| Unity compiles and imports | Pinned-editor import and focused EditMode execution in isolated candidate; inspect logs/XML, not only exit status | Hands-on gameplay, Player performance or build proof. |
| Runtime remains bounded | Static allocation/query review and measured station UI refresh under representative loaded-camp conditions if implementation introduces a new hot path | World-scale performance or extended Player profiling. |
| Loop feels clear | Author opens chest, crafts, collects, docks, refines, retrieves and deliberately resets | Automated state proof; this is user-owned acceptance. |

For subsequent implementation, use the existing Windows background-launch pattern from `LOCAL_WORKSPACE.md`, with the installed pinned executable and explicit candidate path. Test arguments are `-batchmode -nographics -runTests -testPlatform EditMode -testFilter <fixture-filter> -testResults <candidate-Logs-XML> -logFile <candidate-Logs-log>`; launch with `-WindowStyle Hidden` and wait on the process. Verify filter semantics in the installed runner before composing multiple fixtures. New validator command is planned as `-batchmode -nographics -quit -executeMethod BooterBigArm.Editor.CampWorkstationValidator.ValidateFromCli`, only after its non-mutating implementation is reviewed. Existing `CampLoopTestingSceneBuilder.ValidateFromCli` may run in that candidate; Build/Complete may not.

Run new fixtures plus directly affected existing `TopDown3DInventoryTests`, `TopDown3DInventoryLoopTests`, `TopDown3DInventoryLoopUiTests`, `TopDown3DPackingTests`, `TopDown3DInventoryUiTests`, `TopDown3DMicroDustHarvesterTests`, `GreaterWastelandInventoryPersistenceTests`, `CampLoopTestingSceneTests`, `CampLoopSalvageTests` and the actual current animation interaction/reset fixtures discovered in Batch 0. Expand beyond these only for changed dependencies or unresolved failures. Test file I/O uses temporary paths/sentinels, never the user's real checkpoint profile. No gameplay smoke tests, window focusing or Player build is part of planning or automatically part of implementation.

## Risks and containment

| Risk | Containment and stop gate |
| --- | --- |
| Shared inventory extension changes production | Explicit disabled-by-default capability, independent lab catalog/scene overrides, ordinary v1 behavior tests and protected asset hashes. Stop on any unexplained difference. |
| Payload survives transfer but disappears in packing, snapshots or visuals | Cover every ID/quantity reconstruction caller; deep-record tests and one shared slot-mass function. Stop on orphan/duplicate ID. |
| State commits partly across registry/inventory/socket | Prepare and lock every participant before installing; publish only afterward. Failure injection tests must show one complete pre- or post-state. |
| Starter grants duplicate or reset erases unrelated work | Explicit session identity, one initialization barrier and a distinct labelled economy reset; unrelated stacks, salvage rewards/wreck, original mining/actor/animation state are excluded. |
| Canister becomes inaccessible after retaining partial dust | Partial redeployment from reachable cargo and content-aware retrieval; full destinations retain sockets/placements unchanged. |
| Lab inventory initialized before catalog/capability overrides | Author configuration before startup, validate all owners, reject missing references. Never late-reconfigure existing inventories. |
| Animation work races scene or input changes | Named file/scene ownership at Batch 0, fresh diffs before integration, current dispatch preserved. No concurrent writes to coupled files. |
| Unsaved scene or shared terrain changed by authoring | Work on owned scene objects, preserve TerrainData hashes/GUIDs, no old builder, no automatic opening/discarding. |

Recovery uses task-owned snapshots and Unity Undo for owned scene edits. Do not reset/restore unrelated dirty files, regenerate metadata, delete local source archives, rewrite Git history or overwrite incompatible save files. Any failed gate stops the next batch until the task-owned cause is fixed and relevant checks pass.

## Production follow-on boundary

Production promotion requires a new approved slice. Expand old stacked empty canisters into exact empty instances with safe capacity handling; preserve loose dust and provide a real refinery input/migration rule; migrate coordinated checkpoint versions for every owner and socket. Add session grant state and revision tracking to the authored-world save service, validate all owners before restore, preserve recovery files and test interruption/restart. Only then author production station anchors and cut over production pickup. Retired generated-world saving remains inactive.

## Completion and next authorized move

### Implementation closeout — October 14, 2026

The user subsequently authorized implementation. The marked Camp Loop lab now contains the workbench, one stocked existing chest, refinery sockets and exact filled-canister lifecycle described by this plan. All seven plan-audit findings have been resolved against the sealed loadout API. The [implementation receipt](../../Evidence/Gameplay/CampWorkstations20261014/README.md) records the installed behavior, focused EditMode proof, additive scene/production protection hashes, six reviewed UI captures and validation limits.

The implementation uses hidden background batchmode authoring in an isolated copy, followed by a guarded additive source-scene delta. Its CLI installer may open that isolated lab; it does not discard an author-owned Editor scene. The original four-Ironstone/ten-dust defaults remain provisional asset data. Production promotion and hands-on acceptance remain separate.

### Original planning boundary — retained

At the planning checkpoint, creating this plan did not begin implementation. Lab completion required exact ingredient seeding, crafting, preserved canister transport, docking, refinement, retrieval and deliberate reset to pass focused state/UI/scene checks while production contracts and protected assets remained intact. The implementation receipt above now supplies that evidence; author visual/feel acceptance remains separate.

The planning task establishes source inspection and a reviewed dependency sequence only. It makes no fresh Unity compilation, EditMode, gameplay, performance, production persistence, Player-build or publication claim.

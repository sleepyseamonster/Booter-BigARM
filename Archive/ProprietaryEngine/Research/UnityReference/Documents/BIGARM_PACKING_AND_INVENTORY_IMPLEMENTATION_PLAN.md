# BigARM Packing And Inventory Implementation Plan

Status: Implementation-ready; planning complete, implementation not started

Planning owner: Gottspan

Implementation lane: TopDown3D production runtime, Input System, uGUI, editor tooling, project-owned assets, focused EditMode tests, coordinated persistence, and user-owned gameplay acceptance

Last audited: 2026-08-17

## 1. Objective

Turn BigARM into Booter's physically present mobile item mule and make packing a large, fun part of the expedition loop without reproducing Death Stranding's menu burden or simulation complexity.

The finished system must let the player:

- prepare a compact personal field kit for Booter;
- carry bulk resources and expedition supplies on a visible BigARM loadframe;
- collect, transfer, auto-pack, and manually reposition whole item stacks quickly;
- understand load, balance, and remaining capacity at a glance;
- feel the load through BigARM's silhouette, motion, sound, and traversal response;
- make occasional meaningful choices about what to carry, what to leave, and how to pack it;
- preserve both inventories and the packing layout across chunk streaming and coordinated save/load;
- use the complete flow with gamepad, keyboard, or mouse;
- recover cleanly from full capacity, separation, interruption, invalid targets, save migration, and teardown without duplication or item loss.

The system is successful when item management creates anticipation, expression, and expedition decisions while routine handling is nearly automatic.

## 2. Fun-First Product Contract

These rules control every implementation decision in this plan.

1. **The decision is the gameplay; moving icons is not.** The player chooses what matters, where important cargo belongs, and whether a haul is worth the cost. Routine sorting is automated.
2. **Auto-pack must be trustworthy.** One action produces a safe, balanced load. Manual packing is optional optimization, never repair work for a weak algorithm.
3. **Use whole stacks as cargo containers.** One occupied stack is one mounted cargo container. There is no free rotation, multi-cell Tetris, or per-item physical placement in the first production system.
4. **Reward good packing more than punishing imperfect packing.** Stable loads feel better. Heavy or top-biased loads remain usable and create mild, predictable tradeoffs rather than hard failure.
5. **No routine loss spiral.** No random cargo drops, per-container durability, rain damage, strap maintenance, or repeated collection of scattered items.
6. **BigARM is a character in the loop.** Cargo appears on him, changes his posture and sound, and remains attached to his true simulated position. He is never a remote bottomless menu.
7. **Management happens at interesting moments.** Preparation, a major discovery, a dangerous route, regrouping, and unloading are valid packing moments. Every Ironstone pickup is not.
8. **Common actions are short.** Routine pickup takes about one second, a meaningful salvage decision takes about five to ten seconds, and normal expedition preparation should take less than thirty seconds once the player knows the interface.
9. **The screen explains itself.** The player sees `LOAD`, `BALANCE`, destination, and the reason an action cannot complete. Internal optimizer scores and multiple competing capacity numbers stay hidden.
10. **Feel gates outrank feature count.** Later cargo features proceed only if the simpler system is already enjoyable in user playtesting.

## 3. Planning Frame

### 3.1 Authority

- The user's current instruction is the product authority for BigARM becoming a mobile item mule.
- Gottspan owns scope, architecture coordination, integration order, source-of-truth alignment, and evidence review.
- The TopDown3D runtime assembly owns production gameplay behavior. It must not depend on Legacy2D or the former isometric lab.
- Babineaux owns any later approved Unity bridge activity. Normal implementation and validation must remain background-safe.
- Gear Ball owns Git/GitHub publication activity under current authority gates. A commit does not authorize a push, pull request, branch change, release, or other external write.

### 3.2 Source of truth, in priority order

1. The user's current mobile-mule and fun-over-complexity direction.
2. `AGENTS.md` and `Docs/Agents/Gottspan/README.md`.
3. This plan for the bounded packing/inventory implementation contract.
4. `Docs/WORLD_BASIS.md` and `Docs/BIGARM_COMPANION_STANDARD.md`, after the canon alignment in Batch 0.
5. `Docs/WORLD_SYSTEMS_STANDARD.md` for deterministic world, streaming, and persistence boundaries.
6. `Docs/INPUT_ARCHITECTURE_STANDARD.md` for gamepad-first input, UI separation, and device-aware prompts.
7. `Docs/IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md` for the current transactional inventory and exactly-once harvest contract.
8. Current production code, assets, tests, and serialized scene state under `Assets/_Project/`.
9. Prior research and historical prototype documents as reference only.

Live code remains implementation fact; design documents remain intended behavior. Neither silently overwrites the other.

### 3.3 Canon alignment

The current canonical documents say BigARM is not a storage point or storage depot. The user's new direction supersedes that prohibition only as needed to make him a mobile carrier.

The replacement rule is:

> BigARM is a physically present mobile carrier and expedition partner. He is not an infinite storage depot, mobile home, crafting platform, vehicle, safe zone, or remote menu. Cargo access and transfer require his real simulated presence.

The no-teleport rule remains fully locked. Recall, inventory access, save/load, chunk unload, quick release, and failure recovery never relocate BigARM or his cargo to Booter.

### 3.4 In scope

- Fun-first concept validation and iterative greybox gates.
- Booter's compact field-kit inventory.
- BigARM's authoritative cargo inventory and visible loadframe.
- Item carrying metadata and validation.
- Deterministic auto-pack and optional whole-stack manual placement.
- Transactional Booter/BigARM transfers and best-destination harvesting.
- BigARM proximity/access rules and contextual interaction.
- Load-derived BigARM movement and presentation feedback.
- Full packing UI redesign with controller, keyboard, and mouse parity.
- Required Input System actions, prompts, focus behavior, and modal state coordination.
- Loadframe visuals, mount anchors, placeholder cargo containers, animation hooks, sound hooks, and feedback.
- Versioned snapshots, migration from the current inventory snapshot, and integration with one coordinated game-state save owner.
- Procedural-world, streaming, off-screen simulation, and no-teleport compatibility.
- Builder, validator, focused tests, fixed-view evidence, performance checks, accessibility checks, and documentation alignment.
- A gated follow-up experiment for whole-load quick release after the core loop proves fun.

### 3.5 Explicit non-goals

- No freeform spatial Tetris, item rotation, or manual strap simulation.
- No per-item durability, container damage, weather damage, theft simulation, or random cargo loss.
- No individual physics bodies or colliders for every mounted stack.
- No crafting, recipes, merchants, equipment-stat redesign, combat inventory redesign, or settlement economy implementation.
- No remote transfer while BigARM is physically absent.
- No new navigation package or teleport fallback.
- No final character art requirement. The first implementation uses project-owned greybox loadframe visuals and mount anchors.
- No generic universal container framework, multiplayer authority, or network replication.
- No hands-on gameplay smoke test unless the user explicitly performs or requests it.
- No push, pull request, release, package change, purchase, or external write from this plan.

### 3.6 Planning stop condition

Planning is complete when the product behavior, ownership model, data contracts, UI and controls, source boundary, migration, implementation batches, iteration gates, validation matrix, risks, and final completion criteria are explicit enough to begin Batch 0 without another architecture decision.

This document is written to meet that condition.

## 4. Audited Current Repository Truth

### 4.1 Inventory and items

- `TopDown3DItemDefinition` already owns stable item ID, display data, icon, maximum stack, category, and per-unit mass.
- `TopDown3DItemCatalog` strictly rejects invalid or duplicate definitions.
- `TopDown3DInventoryState` already provides fixed slot order, stack-first adds, all-or-nothing add/remove, move/swap/merge, one change event per commit, and versioned snapshots.
- `TopDown3DPlayerInventory` currently owns a 16-slot player inventory.
- The only current production item is Ironstone Ore. It stacks to 99 and has mass `1.25`, but mass is informational and does not limit carrying.
- The current inventory snapshot is version 1 and stores capacity plus ordered item/quantity records. No application restart save owner exists.

### 4.2 Harvesting and interaction

- Ironstone harvesting uses a deterministic nearby target, an in-place gather action, and an exactly-once conditional transaction.
- The current gather state is constructed with one player inventory and therefore cannot choose between Booter and BigARM.
- `ITopDown3DInteractable` is currently harvest-specific: it includes reward, duration, and `TryConsume` directly.
- `TopDown3DInteractionController` currently recognizes only `TopDown3DIronstoneNode` targets.
- A failed or full inventory leaves the node untouched; there is no ground pickup fallback.

### 4.3 Input and UI

- `TopDown3DInputRouter` owns explicit `Gameplay`, `Inventory`, and `Disabled` modes.
- `System/ToggleInventory` remains live while the UI map is enabled. Tab and Gamepad North currently toggle the inventory.
- Prompts derive from live Input System bindings and the current keyboard/gamepad device.
- `TopDown3DInventoryCanvas` currently creates a 4-by-4 runtime uGUI slot grid with details and controls.
- `TopDown3DInventoryUiController` lets the player inspect, move, merge, and swap stacks through the inventory state.
- The inventory installer owns one EventSystem/InputSystemUIInputModule and keeps the passive gameplay HUD raycaster-free.
- Opening the current inventory suppresses Gameplay input but does not pause `Time.timeScale`.

### 4.4 BigARM

- `TopDown3DBigArmFollower` is a kinematic physical follower with route history, a follow band, acceleration/deceleration, turning, avoidance, stuck recovery, physical catch-up, and `WaitingForTerrain`.
- The current implementation has one compact rectangular-prism visual, one box collider, no cargo owner, no cargo anchors, and no snapshot.
- BigARM's current movement has no load input. Cruise, catch-up, acceleration, turning, and obstacle probes are authored follower values.
- World-scale off-screen travel remains an unresolved companion seam. The packing system must provide load state to that future owner without pretending the seam already exists.
- The loaded follower is currently the only live position owner. Coordinated application-restart persistence therefore requires a persistent companion-state owner above the follower before it can be considered complete.

### 4.5 Persistence and procedural world

- Inventory, resource depletion, and survival each expose local versioned snapshots.
- Resource deltas live above chunk GameObjects and survive chunk unload/reload.
- No coordinated whole-game disk save currently serializes those snapshots, player position, or BigARM state together.
- Carried inventory is not generated world content. Its identity belongs to Booter or BigARM, not the currently loaded chunk.
- A detached cargo bundle, if the gated experiment is accepted, becomes world state and therefore needs a stable instance ID, authoritative world coordinate, chunk presentation lifecycle, and saved delta record.

### 4.6 Workspace and proof conditions

- The repository is on `main` with a broad user-owned dirty overlay, including current inventory, input, BigARM, scene, builder, validator, landscape, and test work.
- `Temp/UnityLockfile` is present at planning time. Batchmode must not run against the open project.
- The current inventory feature files are untracked in Git at planning time. Implementation must treat their live content as the base, preserve GUIDs and `.meta` files, and never reconstruct them from the older plan.
- This planning task owns only this new plan document.

## 5. Product Defaults

These defaults are selected to remove implementation ambiguity. Tuning values remain data-driven and can change at iteration gates without replacing the architecture.

| Decision | Default | Fun-first reason |
| --- | --- | --- |
| Core representation | One occupied stack equals one cargo container/mount | Physical enough to read; avoids spatial Tetris. |
| Booter capacity | 8 fixed field-kit slots | Booter keeps essentials and short-term overflow; BigARM matters. |
| BigARM capacity | 12 fixed mount slots | Enough visible variation without a dense screen. |
| Mount topology | 6 low/core mounts, 4 side mounts, 2 upper mounts | Creates balance and access choices with three readable zones. |
| Weight | Per-unit item mass multiplied by stack quantity | Reuses current authored data. |
| Booter mass | Soft authored field-kit limit with strict maximum | Prevents Booter replacing BigARM while allowing emergency overflow. |
| BigARM mass | Comfortable, Loaded, and Heavy bands; Heavy remains usable up to a strict maximum | Consequences remain mild and predictable. |
| Packing | Deterministic one-button auto-pack plus whole-stack manual moves | Safe default with optional expression. |
| Auto-pack priority | Heavy low, left/right balanced, quick items on sides, fragile items protected, deterministic ties | Understandable result every time. |
| Imperfect load consequences | Mild acceleration, turning, sound, posture, and energy changes | Felt but not punishing. |
| Cargo loss | None during normal traversal | Avoids repetitive recovery and frustration. |
| Harvest destination | BigARM first for cargo items when physically accessible; otherwise Booter; fail only if neither fits | Keeps common harvesting flowing while preserving physical presence. |
| Transfer | One-action quick transfer moves the maximum valid quantity transactionally | No repeated split-stack busywork. |
| Remote state | BigARM manifest may be viewed read-only when away; transfer and repacking are disabled | Planning information without magical access. |
| Inventory time | Keep the current unpaused-world behavior for the first playable slice; reassess only after threat gameplay exists | Avoids inventing a global pause owner before there is evidence it improves the loop. |
| Physical cargo | Non-colliding pooled visuals attached to authored mount anchors | Strong silhouette with no per-box physics instability. |
| Manual locking/pinning | Deferred until the first auto-pack playtest proves a need | Avoids stable parcel identity and extra commands before value is proven. |
| Quick release | Gated post-core experiment, not part of the minimum ship gate | Promising but requires world persistence and must earn its complexity. |

Initial tuning targets, stored in an authored settings asset rather than constants:

- Booter: 8 slots, comfortable through 75% of authored mass, strict rejection above 100%.
- BigARM: 12 mounts, Comfortable through 75%, Loaded from 75% to 100%, Heavy from 100% to 115%, strict rejection above 115%.
- Comfortable BigARM has no movement penalty.
- Loaded BigARM is capped initially at roughly 10% cruise-speed reduction and 15% acceleration/turn reduction.
- Heavy BigARM is capped initially at roughly 20% cruise-speed reduction and 25% acceleration/turn reduction.
- Balance effects must remain smaller than weight-band effects and may never stop movement.

Those caps are safety rails, not final feel claims.

## 6. Player Experience Specification

### 6.1 Expedition preparation

1. The player opens Packing with Tab or Gamepad North, or interacts with nearby BigARM using the existing Interact action.
2. The screen opens with Booter's Field Kit on the left, BigARM's loadframe in the center, item details on the right, and load/balance status at the bottom.
3. If BigARM is physically accessible, both inventories are interactive. If not, Booter's kit is interactive and BigARM's manifest is visibly read-only.
4. `Auto-Pack` is the default highlighted command when BigARM contains cargo that can be improved; otherwise focus returns to the last valid item.
5. The player can quick-transfer a selected stack or pick it up and place it into a specific valid slot.
6. Closing returns cleanly to Gameplay with stale movement/look/sprint intent cleared.

Normal preparation should be possible through quick transfer plus Auto-Pack without selecting every stack.

### 6.2 Routine harvesting

1. Booter completes the existing gather action.
2. The destination service simulates the complete reward before the node commits consumption.
3. Cargo-preferred resources go to nearby accessible BigARM first. Personal-preferred items go to Booter first. `Either` items use the first valid preferred destination.
4. The HUD reports destination in one compact receipt, such as `+1 Ironstone -> BigARM`.
5. BigARM's visual mount refreshes with a short magnetic snap animation and sound.
6. The packing screen does not open.

If neither destination can accept the complete reward, the action reports the reason and the resource remains untouched.

### 6.3 Regroup and transfer

- When BigARM is within the authored access radius and has line of sight, `Transfer Cargo to BigARM` moves every eligible stack or the maximum valid quantities in one atomic batch.
- The reverse bulk action, `Take Essentials`, moves only player-selected or personal-preferred stacks; it never empties BigARM unexpectedly.
- Individual quick transfer moves the maximum valid quantity and leaves a clearly labeled remainder when the whole stack cannot fit.
- No transfer is available while BigARM is absent, waiting in unloaded terrain, disabled, or outside access range.

### 6.4 Meaningful discovery

When an important item does not fit, the UI offers a short decision rather than a generic error:

- `Make Room` opens Packing on the conflicting destination.
- `Carry on Booter` appears only if Booter's policy can accept it.
- `Leave and Mark` is displayed as unavailable until a world-marker owner exists; the packing implementation must not silently invent that subsystem.

The first production slice needs only `Make Room` and a clear reason. Future marker integration remains a separate feature dependency.

### 6.5 Manual packing

- The player selects an occupied stack, then a destination mount.
- Compatible stacks merge; other occupied mounts swap; empty mounts receive the stack.
- Manual moves operate on entire stacks. Quantity splitting is not required for spatial placement.
- Mount presentation communicates low/core, side, and upper zones through shape and position, not dense text.
- The summary updates immediately: total load, current band, balance direction, and expected movement effect.
- An invalid placement does not mutate state and explains why.

### 6.6 Auto-pack

- Auto-Pack is a single UI command and may also run automatically after bulk transfer if the player has enabled the accessibility/QoL option.
- It produces one inventory change event and one visual refresh, never a cascade per slot.
- Repeating Auto-Pack without state changes is idempotent.
- It never discards, splits, merges beyond normal stack rules, changes quantities, or moves items between Booter and BigARM.
- It only reorders BigARM's existing stacks across the 12 mounts.

## 7. Recommended Runtime Architecture

### 7.1 Authority map

| Concern | Single authority |
| --- | --- |
| Static item truth | Existing `TopDown3DItemCatalog` and extended `TopDown3DItemDefinition` |
| Generic stack and transaction rules | Extended plain-C# `TopDown3DInventoryState` |
| Booter mutable inventory | Existing `TopDown3DPlayerInventory`, reconfigured as the field-kit owner |
| BigARM mutable cargo | New `TopDown3DBigArmCargo`, containing one inventory state with 12 stable mount slots |
| Mount topology and tuning | New immutable `TopDown3DPackingSettings` ScriptableObject |
| Cross-owner transfers | New plain-C# `TopDown3DInventoryTransferService` |
| Harvest destination selection | New `TopDown3DItemDestinationService` used by the gather action transaction |
| Packing algorithm | New pure `TopDown3DCargoAutoPacker` |
| Derived load/balance | New immutable `TopDown3DBigArmLoadProfile`, calculated from catalog, state, and mount definitions |
| BigARM load movement response | `TopDown3DBigArmFollower` consuming the derived load profile; it does not inspect UI or item definitions directly |
| Physical cargo presentation | New `TopDown3DBigArmCargoVisuals` consuming committed mount state |
| Target selection | Existing `TopDown3DInteractionController`, generalized just enough to target harvestables and BigARM cargo access |
| Packing screen behavior | Existing `TopDown3DInventoryUiController`, expanded as the only inventory/packing UI coordinator |
| Packing screen presentation | Existing `TopDown3DInventoryCanvas`, expanded into reusable Booter, BigARM, detail, summary, and command regions |
| Input modes and prompts | Existing `TopDown3DInputRouter` and canonical Input System asset |
| Coordinated persistence | New `TopDown3DGameStateSaveService` owning one versioned root snapshot and atomic disk writes |
| Persistent BigARM identity/position | New `TopDown3DBigArmState`; the follower executes loaded movement and publishes validated state but is no longer the only persistence authority |
| Generated resource deltas | Existing `TopDown3DResourceWorldState` |
| Optional detached cargo | Separate world-state owner only if the quick-release experiment passes its gate |

No UI component, visual cargo object, chunk GameObject, animation event, or follower movement script owns item quantities.

### 7.2 Item metadata

Extend `TopDown3DItemDefinition` with serialized, validated metadata:

- `TopDown3DCarryPreference CarryPreference`: `Either`, `BooterPreferred`, `BigArmPreferred`, `BooterOnly`, or `BigArmOnly`;
- `TopDown3DPackingPreference PackingPreference`: `Normal`, `QuickAccess`, `Protected`, or `UpperAllowed`;
- optional cargo visual prefab/material reference, allowed to be null so the canonical greybox container remains available;
- existing mass remains per unit and must be finite and non-negative.

The enum zero values must preserve existing assets safely: `Either` and `Normal`. The Ironstone asset is explicitly authored as `BigArmPreferred` and `Normal` in Batch 1.

Do not add size, orientation, fragility percentage, monetary value, durability, or multiple handling flags until real content requires them.

### 7.3 Inventory policy and state

Introduce immutable `TopDown3DInventoryPolicy` data when constructing a state:

- stable owner kind (`BooterFieldKit` or `BigArmLoadframe`);
- fixed slot count;
- comfortable and strict mass limits;
- allowed carry preferences;
- optional mount definitions for BigARM.

Extend state queries with:

- `TotalMass` calculated from authoritative item quantities;
- `GetMaxAddable(itemId, requestedQuantity)`;
- `CanAdd`/`TryAdd` under slot, stack, owner, and strict-mass rules;
- prepared mutations used by cross-inventory transactions;
- `TryApplySlotOrder` for a validated auto-pack permutation;
- structured failure codes for mass limit, carrier restriction, invalid layout, and destination unavailable.

All existing guarantees remain locked:

- no partial mutation on failure;
- no unknown items or invalid quantities;
- no stack overflow;
- one event per committed transaction;
- snapshot apply is fully validated before mutation;
- deterministic slot order;
- no scene scan or view-owned state.

### 7.4 Mount topology

`TopDown3DPackingSettings` contains 12 ordered mount definitions with stable IDs. Ordering is serialized and versioned because inventory snapshot slot order maps directly to mount placement.

Each mount has:

- stable `MountId`;
- zone: `Core`, `Side`, or `Upper`;
- normalized lateral moment from `-1` left to `+1` right;
- normalized height from `0` low to `1` high;
- protection and quick-access preference weights;
- required visual anchor index;
- optional per-mount maximum stack mass, disabled by default.

Initial topology:

- Core: 6 mounts, three left and three right, lowest height, highest protection.
- Side: 4 mounts, two left and two right, middle height, highest access.
- Upper: 2 mounts, one left and one right, highest visibility and lowest stability preference.

Every mount accepts every normal stack in the first slice. Preferences affect optimizer scoring, not hard compatibility, unless an item is explicitly `BooterOnly` or `BigArmOnly`.

### 7.5 Deterministic auto-pack algorithm

The auto-packer operates only on an immutable copy and returns either a complete slot permutation or a structured failure. It must not mutate live state during search.

Algorithm:

1. Collect occupied stacks and calculate total stack mass.
2. Sort stacks by packing preference priority, then descending stack mass, then item ID, then original slot index.
3. For each stack, score every empty mount using:
   - left/right moment after placement;
   - height-weighted mass;
   - QuickAccess-on-side reward;
   - Protected-in-core reward;
   - upper-zone penalty for heavy cargo;
   - small movement penalty to prefer stable results when scores tie.
4. Select the lowest score; break exact ties by stable mount order.
5. After all placements, run one deterministic left/right swap-improvement pass. Accept only swaps that lower the total score.
6. Validate that item IDs, quantities, and stack count are byte-equivalent to the input.
7. Commit the full order once.

The scoring weights live in `TopDown3DPackingSettings` so feel can be tuned without rewriting rules. Unit tests use fixed settings and exact expected layouts.

### 7.6 Load profile

`TopDown3DBigArmLoadProfile` is derived data and is never serialized. It contains:

- total and normalized mass;
- load band: `Comfortable`, `Loaded`, `Heavy`, or `Invalid`;
- normalized lateral imbalance and direction;
- normalized weighted height;
- cruise, acceleration, deceleration, and turn multipliers;
- presentation intensity for posture, servo effort, and cargo rattle.

The follower receives one profile when cargo changes and uses clamped multipliers at its existing speed/acceleration/turn seams. It must not recalculate catalog mass every physics tick.

Catch-up remains physical. A heavy load may slow ordinary movement, but urgent catch-up retains a separately clamped minimum so packing cannot make BigARM permanently unable to regroup. Load never authorizes relocation.

### 7.7 Persistent companion state

`TopDown3DBigArmState` is the durable owner for BigARM's stable companion ID, authoritative world coordinate, current high-level task, current loaded/unloaded simulation mode, and cargo snapshot reference. It lives above replaceable follower presentation and exposes validated capture/apply operations.

- While detailed BigARM is loaded and grounded, `TopDown3DBigArmFollower` publishes its validated coordinate to the state owner after movement.
- When detailed presentation is unavailable, the state retains the last authoritative coordinate and explicit simulation status; absence never means "near Booter."
- Loading a save restores the recorded coordinate into the state owner first. Detailed presentation appears only when terrain and the companion simulation owner can instantiate or ground BigARM at that coordinate.
- This plan does not invent arbitrary off-screen route progress. Until the world-scale traversal slice supplies it, the state records an honest `WaitingForTerrain`/unresolved-route condition.
- Cargo remains accessible only through a loaded, grounded, in-range physical representation even though its state persists while unloaded.

The state owner is the seam later off-screen traversal must extend; it must not create a competing route simulator inside the inventory system.

### 7.8 Atomic transfer service

`TopDown3DInventoryTransferService` accepts source, destination, item, requested quantity, and transfer mode:

- `Exact`: all requested units or no mutation;
- `Maximum`: largest valid amount up to the request;
- `AllEligible`: deterministic batch transfer for bulk unload/load.

The service:

1. validates both owners and policies;
2. computes the exact source removal and destination addition on copies;
3. optionally auto-packs the destination copy;
4. commits source and destination together on the main thread;
5. emits one change notification per affected owner plus one transfer result;
6. leaves both owners unchanged on any failure.

No implementation may perform `remove` and then hope `add` succeeds.

### 7.9 Harvest destination service

Replace the gather state's constructor dependency on one inventory with `TopDown3DItemDestinationService`.

The service evaluates the reward and current physical context:

1. Resolve destination order from the item's carry preference.
2. Treat BigARM as accessible only when his cargo owner is active, within the authored access radius, and unobstructed.
3. Simulate the full reward, including BigARM auto-pack if selected.
4. Call the resource's `TryConsume` only as the prepared destination transaction's commit condition.
5. Commit exactly one destination once.
6. Return destination identity for HUD feedback.

This preserves the existing exactly-once node/inventory boundary and never spawns fallback loot.

### 7.10 Interaction contract

Generalize interaction without installing a universal entity framework:

- Keep `ITopDown3DInteractable` as the small common target surface: Unity object, stable ID, prompt, interaction point/range, availability, and interaction kind.
- Move reward, action duration, and consume behavior to `ITopDown3DHarvestable : ITopDown3DInteractable`.
- `TopDown3DIronstoneNode` implements the harvestable contract.
- New `TopDown3DBigArmCargoAccess` implements the base contract and requests the packing UI when selected.
- `TopDown3DInteractionController` deterministically scores both types through one target list and one line-of-sight policy.
- `TopDown3DPlayerActionController` dispatches by typed interaction kind: harvest begins the current gather action; cargo access opens Packing if allowed.

Do not add reflection, a message bus, or a generic serialized action graph.

### 7.11 Cargo visuals

`TopDown3DBigArmCargoVisuals` owns presentation only:

- twelve authored child mount anchors created by the builder;
- a small pool of non-colliding cargo-container visuals;
- one visual per occupied mount, scaled within fixed bounds by stack fullness only if that remains readable;
- item icon/decal or category color where available;
- short unscaled-time snap/settle animation after committed changes;
- load-driven body lean/posture offset applied to a dedicated visual root, never the collider or navigation origin;
- hooks for servo effort, magnetic latch, cargo rattle, and unload sounds.

Visual teardown and scene rebuild cannot modify inventory. Missing item-specific visuals use one project-owned neutral cargo container; missing gameplay definitions still fail validation.

## 8. UI And Control Specification

### 8.1 Screen layout

Use the existing separate interactive canvas and sole EventSystem. Replace the current single 4-by-4 presentation with:

- **Header:** `PACKING`, current access state, and Auto-Pack command.
- **Left:** `BOOTER FIELD KIT`, 2-by-4 slot grid, current/maximum mass.
- **Center:** `BIGARM LOADFRAME`, twelve mount buttons arranged as the physical Core, Side, and Upper silhouette.
- **Right:** selected item icon, name, quantity, per-unit/stack mass, carry preference, and plain-language action result.
- **Footer:** BigARM total load band, simple balance indicator, expected movement effect, and live device-aware control hints.

At smaller safe areas, collapse details below the inventories rather than scaling text beneath the readable minimum.

### 8.2 Controls

Existing controls remain:

- Tab / Gamepad North: open or close Packing.
- UI Navigate: move focus.
- UI Submit / Gamepad South / keyboard submit: select a stack, place it, activate a command, merge, or swap.
- UI Cancel / Gamepad East / keyboard cancel: cancel a held stack first, then close the screen.
- Mouse point/click: equivalent focus and activation; no drag requirement.

Add one UI action only:

- `UI/QuickTransfer`: Gamepad West plus an authored keyboard binding selected during Batch 0 binding-conflict review.

Auto-Pack, Transfer All, and Take Essentials remain selectable buttons and do not require more global shortcuts. The input router exposes typed events; UI code never reads devices directly.

### 8.3 Focus behavior

- Opening from the inventory toggle restores the last valid focus, preferring Auto-Pack when the layout is improvable.
- Opening by interacting with BigARM focuses his first occupied mount or Auto-Pack.
- Removing or moving the selected stack moves focus to the nearest remaining valid control.
- Disabled read-only BigARM controls stay visible but are not focus traps.
- Directional navigation follows the visual layout and does not jump unpredictably between zones.
- Submit/Cancel/QuickTransfer behavior is idempotent under repeated input in the same frame.

### 8.4 Feedback language

Use short actionable messages:

- `Moved 24 Ironstone to BigARM`
- `BigARM is too far away`
- `BigARM loadframe is full`
- `Too heavy for Booter's field kit`
- `Auto-packed: Stable`
- `Cannot transfer this item to Booter`

Do not expose `InvalidRequest`, internal enum names, optimizer scores, or generic `Inventory full` when the destination is known.

### 8.5 Accessibility and comfort

- Auto-pack can run automatically after pickup and/or bulk transfer through an option, default off until user acceptance.
- Balance is communicated by text and shape/position, not color alone.
- Load bands use label, icon, and color.
- No essential action requires holding multiple buttons, precise pointer dragging, rapid timing, or manual item rotation.
- UI text and selected focus meet readable contrast and safe-area constraints.
- Cargo snap/rattle intensity respects reduced-motion and audio settings when those canonical settings owners exist; until then the effects stay brief and mild.

## 9. Persistence And Procedural-World Contract

### 9.1 Ownership

- Booter inventory belongs to the player-state owner.
- BigARM cargo belongs to BigARM's persistent companion state.
- Neither inventory belongs to a chunk or scene presentation object.
- Mounted visuals reconstruct from committed BigARM cargo state whenever his detailed GameObject enters simulation.

### 9.2 Snapshots

Version the existing inventory snapshot to support owner policy and strict validation without serializing derived mass.

Create:

- `TopDown3DBigArmCargoSnapshot`: schema version, loadframe version, and ordered inventory snapshot;
- `TopDown3DBigArmCompanionSnapshot`: schema version, authoritative world coordinate, task/follow state required by the current owner, route progress when that owner exists, and cargo snapshot;
- `TopDown3DGameStateSnapshot`: root schema version, world seed/generation versions, Booter transform state, Booter inventory, BigARM companion state, survival snapshot, and resource-world snapshot.

Do not serialize load multipliers, balance scores, visual object IDs, pooled instances, or chunk GameObjects. Recalculate derived state after apply.

### 9.3 Coordinated disk save

Implement one `TopDown3DGameStateSaveService` rather than a cargo-only JSON file.

Requirements:

- path under `Application.persistentDataPath`;
- JSON DTO with explicit root and child versions;
- write to a temporary sibling file, flush/close, then atomically replace the primary where the platform permits;
- retain one previous known-good backup;
- validate the entire payload and referenced item IDs before mutating live owners;
- stage all child snapshots, then apply in a controlled order;
- if apply fails, leave the current live session unchanged and report a recoverable error;
- no scene scanning to discover state owners;
- save owner receives explicit serialized references or a validated scene service registry;
- no automatic save during an active inventory transfer or gather commit.

Batch 8 may land coordinated save/load only after the persistent companion state exposes honest position ownership. If that owner or its terrain-valid restore path cannot be completed, the gate stays red and the overall implementation remains incomplete; do not ship a cargo-only disk save as a workaround.

### 9.4 Migration

Current inventory snapshot version 1 has 16 player slots and no BigARM cargo.

Migration policy:

1. Read the old snapshot into a temporary validated state.
2. Move the first valid personal-preferred stacks into Booter's new 8-slot field kit.
3. Move remaining eligible stacks into BigARM cargo using deterministic transfer order and Auto-Pack.
4. Reject the migration before live mutation if the combined new owners cannot contain every old item.
5. Never discard overflow or synthesize a ground drop.
6. Record the migrated root version only after the full coordinated snapshot validates.

No public disk saves currently exist, but the migration is still implemented and tested so editor evidence/current snapshot fixtures do not silently break.

### 9.5 Streaming and off-screen simulation

- Unloading BigARM's detailed presentation does not unload cargo state.
- Off-screen movement consumes the same derived load profile or a documented deterministic approximation derived from the same mass/balance inputs.
- On physical re-entry, visuals rebuild at BigARM's true simulated coordinate and never at Booter's coordinate.
- Access remains false until the physical representation is loaded, grounded, in range, and unobstructed.
- Save/load cannot convert `WaitingForTerrain` or a missing route into a teleport.

### 9.6 Optional quick-release bundle

Quick release is a separately gated experiment after Batch 7.

If approved:

- detaching creates one secured world bundle containing a full cargo snapshot and stable bundle ID;
- a world-root bundle-state owner stores ID, position, layout, and contents above chunk presentation;
- bundle creation and BigARM inventory clearing commit as one transaction;
- reattachment simulates the full destination before consuming the bundle record;
- chunk unload destroys only presentation;
- root save includes bundle records;
- one bundle represents the load; it never explodes into individual physics items.

If the experiment is not more fun than ordinary packing, omit it without leaving placeholder runtime code.

### 9.7 Required procedural-world checklist

- **World identity:** carried Booter/BigARM inventory is player/companion state and is not regenerated from the world seed. Any optional detached bundle records the world seed/generation context in the coordinated root save but does not derive its contents from generation.
- **Stable identity:** Booter and BigARM use stable owner kinds; BigARM also has one stable companion ID. Mount IDs and loadframe version are authored and stable. Optional detached bundles receive saved stable instance IDs.
- **Streaming lifecycle:** carried state survives when detailed actors or chunks unload. Visual mounts reconstruct from state. Optional bundle presentation reconstructs from its world-root record when the owning chunk loads.
- **Persistence:** item IDs, quantities, ordered mounts, BigARM's authoritative coordinate/status, and optional bundle records are saved. Total mass, optimizer score, load band, pooled visuals, and other derived presentation are regenerated.
- **Authored constraints:** item catalog metadata, inventory policies, mount topology, mass bands, access radius, movement caps, and visual anchors are authored data. Procedural terrain never silently changes capacity or item definitions.
- **Deterministic proof:** identical input state/settings must produce identical Auto-Pack order and load profile. Snapshot round trips, chunk unload/reload, BigARM presentation unload/re-entry, repeated save/load, and optional bundle boundary tests prove continuity.

No implementation batch may mark one of these concerns not applicable without recording the reason in its handoff.

## 10. Planned Source Boundary

Exact names may adjust only to established naming discovered in Batch 0. Responsibilities and single-owner boundaries may not drift.

### 10.1 Existing runtime files to extend

- `Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemDefinition.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemCatalog.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryState.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventorySnapshot.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DPlayerInventory.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/ITopDown3DInteractable.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DGatherActionState.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DInteractionController.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryCanvas.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventorySlotView.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiSceneInstaller.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInteractionFeedbackHud.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs`

### 10.2 New runtime files

Under `Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/`:

- `TopDown3DInventoryPolicy.cs`
- `TopDown3DInventoryTransferService.cs`
- `TopDown3DItemDestinationService.cs`

Under `Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/`:

- `TopDown3DPackingSettings.cs`
- `TopDown3DBigArmState.cs`
- `TopDown3DBigArmCompanionSnapshot.cs`
- `TopDown3DBigArmCargo.cs`
- `TopDown3DCargoAutoPacker.cs`
- `TopDown3DBigArmLoadProfile.cs`
- `TopDown3DBigArmCargoSnapshot.cs`
- `TopDown3DBigArmCargoAccess.cs`
- `TopDown3DBigArmCargoVisuals.cs`

Under `Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/`:

- `TopDown3DGameStateSnapshot.cs`
- `TopDown3DGameStateSaveService.cs`
- `TopDown3DGameStateMigration.cs`

Optional quick-release files are not created unless its iteration gate passes.

### 10.3 Editor, asset, and serialized files

- Extend `Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DPrototypeBuilder.cs` to author/load packing settings, configure both inventories, add BigARM mount anchors and cargo components, and install the expanded screen.
- Extend `Assets/_Project/Scripts/Editor/Validation/TopDown3DPrototypeValidator.cs` for unique owners, exact mount topology, item metadata, input bindings, references, and persistence service wiring.
- Extend `Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DIronstoneAssetBuilder.cs` only for the new Ironstone carry metadata and evidence capture; do not make it the general packing asset owner.
- Modify `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions` only after preserving existing action/map IDs and confirming the new binding has no conflict.
- Add `Assets/_Project/Settings/Player/TopDown3DPackingSettings.asset`.
- Add project-owned neutral cargo-container material/mesh/prefab assets under the existing Art/Materials/Prefabs structure as required by the builder.
- Update `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity` only through the approved builder/editor path after ownership is re-audited.
- Preserve all existing `.meta` files and GUIDs.

### 10.4 Tests

Extend:

- `TopDown3DInventoryTests.cs`
- `TopDown3DInventoryUiTests.cs`
- `TopDown3DInteractionTests.cs`
- `TopDown3DPlayerLocomotionTests.cs`
- `TopDown3DFoundationTests.cs` where the BigARM contract changes

Add:

- `TopDown3DCargoPackingTests.cs`
- `TopDown3DInventoryTransferTests.cs`
- `TopDown3DBigArmCargoTests.cs`
- `TopDown3DGameStatePersistenceTests.cs`

No new assembly is planned. Reconsider only if compilation proves a real boundary problem.

### 10.5 Documentation to align during implementation

- `Docs/WORLD_BASIS.md`
- `Docs/BIGARM_COMPANION_STANDARD.md`
- `Docs/DECISION_LOG.md`
- `Docs/ROADMAP.md`
- `Docs/PROJECT_STATUS.md`
- `Docs/DOCS_INDEX.md`
- `Docs/INPUT_ARCHITECTURE_STANDARD.md` if the canonical UI action set changes
- `Docs/IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md`
- a final implemented-system document, either by converting this plan's status/implementation checkpoint or adding `Docs/BIGARM_PACKING_AND_INVENTORY_SYSTEM.md`

At implementation start, preserve any unrelated dirty hunks in these documents and edit only task-owned sections.

## 11. Concept And Implementation Sequence

Each batch has a stop gate. Do not push through a red gate with fallbacks or adjacent scope.

### Batch 0: Re-audit, canon alignment, and ownership manifest

1. Re-read repo rules, Gottspan contract, relevant standards, current status/diffs, current branch, and `Temp/UnityLockfile`.
2. Diff every existing file in Section 10.1 and record task-owned versus user-owned hunks.
3. Confirm the live inventory/interaction feature has not changed since this audit.
4. Write the canon replacement rule into the controlling BigARM/world/decision documents without weakening no-teleport behavior.
5. Confirm Booter 8 slots, BigARM 12 mounts, one-stack-per-mount, unpaused first slice, and no cargo damage as the working product defaults.
6. Capture an implementation file manifest and exact validation commands.

**Gate:** stop if the user has revised a default, the dirty overlay cannot be isolated, current code has a competing owner, or BigARM's role has changed again.

### Batch 1: Paper model and pure packing simulation

1. Create no scene behavior yet.
2. Define 8 Booter slots, 12 BigARM mounts, representative item stacks, mass bands, and mount weights in pure data/tests.
3. Model three expedition scenarios:
   - routine Ironstone collection;
   - a prepared hunt kit plus room for salvage;
   - a nearly full load receiving one valuable heavy stack.
4. Use clearly labeled non-canon fixtures for at least a heavy resource stack, a Booter-preferred personal reserve, a quick-access tool case, a protected salvage case, and a stack-one heavy machinery core. These fixtures live in tests/evidence unless separately approved as production items.
5. Implement the pure auto-packer and load-profile calculator behind tests.
6. Produce fixed textual/diagram evidence showing before/after layouts and why each choice was made.

**Gate:** Auto-Pack must be deterministic, preserve every quantity, improve or maintain the score, finish well below a frame budget for 12 mounts, and create understandable layouts. If the layout requires rotation, multi-cell pieces, or many exceptions to feel interesting, return to product design rather than adding complexity.

### Batch 2: Item metadata and policy-aware inventory foundation

1. Add carry/packing metadata with backward-safe enum defaults.
2. Add inventory policies, mass calculation, maximum-add query, prepared transactions, and validated reorder.
3. Reconfigure Booter's owner as an 8-slot field kit in builder/test fixtures.
4. Implement snapshot version 2 and version-1 migration in pure tests.
5. Author Ironstone as BigARM-preferred and validate the catalog.

**Gate:** existing stack/add/remove/move/merge guarantees remain green; mass/carrier failures leave state unchanged; migration preserves all quantities or rejects without mutation.

### Batch 3: BigARM cargo authority and transfer transactions

1. Add packing settings asset and strict mount validation.
2. Add `TopDown3DBigArmState` with stable identity, honest world-coordinate/status ownership, and validated snapshot capture/apply; connect the loaded follower as its detailed movement executor.
3. Add `TopDown3DBigArmCargo` with 12 stable slots and derived load profile.
4. Add atomic exact, maximum, and all-eligible transfer operations.
5. Add Auto-Pack commit and idempotence.
6. Add BigARM cargo and companion snapshot capture/apply.

**Gate:** transfers cannot duplicate or lose items under full, mass, restriction, repeated-command, disable, or invalid-snapshot paths. Combined quantities before and after every successful transfer are identical. Disabling or unloading detailed BigARM preserves his authoritative identity, coordinate, status, and cargo without making him accessible remotely.

### Batch 4: Harvest routing and contextual BigARM access

1. Split the common interaction and harvestable contracts narrowly.
2. Generalize deterministic target selection to Ironstone and BigARM cargo access.
3. Route gather rewards through the destination service.
4. Implement in-range/line-of-sight/loaded-presence rules.
5. Update HUD receipts with the actual destination.

**Gate:** a valid gather consumes exactly once and awards exactly once to one destination; all invalid/full/range/unload/disable races award zero and consume zero. BigARM absence never routes cargo remotely.

### Batch 5: Packing UI information architecture

1. Greybox the complete two-owner screen using current uGUI and the sole EventSystem.
2. Implement Booter grid, BigARM mount silhouette, details, load/balance summary, access state, Auto-Pack, quick transfer, bulk transfer, and cancel behavior.
3. Add `UI/QuickTransfer` and device-aware prompt resolution.
4. Prove focus/navigation behavior without requiring a pointer.
5. Capture fixed-view evidence at target aspect ratios and safe areas.

**Gate:** all routine flows take the intended small number of actions; controller focus never traps; keyboard and mouse parity hold; views never mutate storage directly; the passive HUD remains raycaster-free.

### Batch 6: Physical cargo presentation and load-responsive movement

1. Add twelve stable visual anchors and pooled neutral cargo containers.
2. Bind committed slot order to visuals and add brief snap/settle feedback.
3. Feed derived load profile into the existing follower movement seams with safety caps.
4. Add posture/lean only to the visual root, never the collider or transform authority.
5. Add restrained servo/rattle hooks and debug readout for load profile.

**Gate:** visual state reconstructs exactly from inventory, never owns quantities, and allocates no steady per-frame garbage. Comfortable movement is unchanged. Loaded/Heavy remain controllable, physical, and able to regroup without teleporting.

### Batch 7: First end-to-end fun iteration

1. Rebuild/repair the production scene only after file ownership and Unity access are safe.
2. Run focused automated proof.
3. Capture the three Batch 1 scenarios in the actual UI and world presentation.
4. Hand off a short user-owned playtest focused on preparation time, trust in Auto-Pack, readability, satisfaction, and whether movement consequences improve the loop.
5. Classify every note as bug, tuning, comprehension, or new feature.
6. Fix bugs and high-confidence comprehension failures. Tune within the safety caps.

**Gate:** proceed only if the player trusts Auto-Pack, understands load/balance without explanation, and prefers using BigARM to the old 16-slot abstraction. If packing feels like maintenance, simplify commands or consequences before adding features.

### Batch 8: Coordinated persistence

1. Implement the root game-state DTO, explicit owner registry, atomic file writes, backup, full-payload validation, and staged apply.
2. Include world identity, Booter transform/inventory, BigARM position/cargo, survival, and resource snapshots.
3. Integrate honest BigARM position/task state without inventing teleport recovery.
4. Implement corrupted, stale-version, unknown-item, capacity, partial-write, and rollback tests.
5. Prove application restart in a safe Unity/player validation lane when authorized.

**Gate:** save/load either restores the coordinated state exactly or changes nothing. Inventory-only disk files and partial live apply are prohibited. BigARM must restore through the persistent companion-state owner at his recorded coordinate; missing terrain or route readiness produces an honest waiting state, never placement beside Booter.

### Batch 9: Optional quick-release experiment

This batch runs only if the Batch 7 playtest identifies a real need for tactical unloading.

1. Implement one secured detached bundle, not scattered cargo.
2. Add world-root bundle state, stable ID, streamed presentation, save record, atomic detach, and atomic reattach.
3. Test narrow traversal, emergency regroup, chunk unload/reload, save/load, and duplicate input.
4. Compare enjoyment and interruption cost against simply opening Packing.

**Gate:** keep the feature only if it creates a clear enjoyable choice and remains faster than manual unloading. Otherwise remove the experimental task-owned code and retain no dormant framework.

### Batch 10: Production validation and closeout

1. Make the builder idempotently author exact settings, mounts, components, actions, references, and visuals.
2. Make the validator reject missing/duplicate authority, invalid item metadata, mount drift, input drift, bad snapshot versions, and competing EventSystems.
3. When `Temp/UnityLockfile` is absent, run background-safe import/compile, focused EditMode suites, and the production validator according to `Docs/UNITY_AUTOMATION.md`.
4. Inspect Console/import logs and serialized diffs.
5. Capture final fixed-view evidence for Booter-only access, nearby BigARM access, Auto-Pack, transfer, full/heavy feedback, physical load, and restored state.
6. Run only an explicitly authorized Development Player performance profile.
7. Align system docs with proven behavior and record user-owned feel results separately.
8. Stage and commit only task-owned verified files. Push/PR/release remain separately authorized.

**Gate:** all required proof is green, no workaround/duplicate owner/fallback loot/teleport path exists, docs match implementation, and unproven user/performance claims are labeled.

## 12. Verification Matrix

### 12.1 Pure and EditMode proof

| Area | Required proof |
| --- | --- |
| Item definitions | Enum defaults preserve old assets; invalid mass/preferences/visual refs fail; stable IDs remain unchanged. |
| Inventory policy | Owner restrictions, slot caps, comfortable/strict mass, and max-add queries are exact and deterministic. |
| Existing inventory | Stack fill, add/remove, merge/swap, atomic batch behavior, one event, and unchanged-on-failure remain green. |
| Migration | Version 1 to version 2 preserves every item and order deterministically or rejects with zero live mutation. |
| Mount config | Exactly 12 unique stable mount IDs, valid zones/anchors/moments, and matching loadframe version. |
| Auto-Pack | Deterministic expected layouts, quantity preservation, idempotence, stable ties, improved/equal score, no live mutation on failure. |
| Load profile | Exact band boundaries and clamped movement/presentation multipliers; no NaN/infinite output. |
| Transfers | Exact/maximum/batch success and all full/mass/restriction/repeated/disable failures preserve combined quantities. |
| Harvest | BigARM-first and Booter fallback rules, physical access, exactly-once destination commit, no remote access, no ground fallback. |
| Interaction | Deterministic tie-break, prompt kind, line of sight, unavailable target, cargo access, and harvest regression. |
| UI/input | One EventSystem, canonical map modes, quick-transfer binding, controller focus, cancel hierarchy, mouse parity, safe area, live prompts. |
| Visuals | Stable mount mapping, pool reuse, rebuild from state, no colliders, no state mutation, no steady update allocation. |
| Movement | Comfortable regression equivalence, loaded/heavy caps, catch-up floor, WaitingForTerrain, disable, and no relocation. |
| Persistence | Full round trip, child version validation, unknown item, corrupt JSON, interrupted write, backup recovery, all-or-nothing apply. |
| Architecture | No Legacy2D dependency, no scene-scan authority, no second catalog/inventory/save owner, no inventory mutation in views/visuals. |

### 12.2 Unity-owned proof

Only when the open-editor safety gate permits:

- Unity imports and compiles with zero relevant errors.
- Focused inventory, transfer, packing, interaction, UI, BigARM, locomotion, persistence, resource, and HUD EditMode suites pass.
- `TopDown3DPrototypeValidator` passes.
- Builder rerun is idempotent and preserves GUID/reference continuity.
- Scene contains one Booter inventory, one BigARM cargo owner, one packing UI coordinator, one EventSystem/module, one save owner, and twelve exact cargo anchors.
- Console/import logs contain no new missing-script, serialization, Input System, uGUI, physics, or save errors.

### 12.3 User-owned fun and feel proof

Automated checks cannot establish the central success condition. The hands-on checklist is:

- Routine collection does not interrupt exploration.
- Auto-Pack is trusted after seeing it work a few times.
- Preparation is understandable and normally under thirty seconds.
- BigARM's physical load is readable from the gameplay camera.
- Comfortable, Loaded, and Heavy feel distinct without Heavy feeling miserable.
- The player notices meaningful capacity decisions but does not constantly reorganize.
- Controller navigation feels deliberate and fast.
- Messages explain failures without requiring system knowledge.
- BigARM still feels like a companion, not a chest with legs.
- The player wants to make one more salvage trip.

Any failure here reopens tuning or simplification before optional features.

### 12.4 Performance proof

- Auto-Pack for 12 mounts is allocation-bounded and completes within the same frame in focused profiling.
- Cargo visuals are pooled and event-driven.
- Load profile recalculates only on committed cargo/settings change.
- BigARM movement performs no catalog scan or packing calculation in `FixedUpdate`.
- UI refresh is event-driven and avoids rebuilding the full hierarchy per frame.
- Player-performance claims require an authorized Development Player profile with representative world streaming and cargo changes.

## 13. Iteration Scorecard

At Batch 7 and after any major revision, record:

| Measure | Target |
| --- | --- |
| Routine pickup menu openings | Zero |
| Typical prepare-and-leave flow | Under 30 seconds after learning |
| Quick transfer of one stack | One shortcut action while focused |
| Bulk transfer plus safe packing | Two deliberate actions or fewer |
| Auto-Pack trust | Player does not routinely inspect/fix ordinary layouts |
| Failure comprehension | Player can state why an item did not move without outside explanation |
| Capacity decision frequency | Occasional expedition decisions, not every pickup |
| Accidental item loss/duplication | Zero |
| Required pointer dragging | Zero |
| BigARM physicality | Cargo state is recognizable on his body from the normal camera |

Do not convert these into grind scores, delivery ratings, or bonuses until the base loop is fun.

## 14. Risk Register And Containment

| Risk | Containment |
| --- | --- |
| Packing becomes menu labor | Whole-stack model, trustworthy Auto-Pack, bulk transfer, short scorecard, simplification gate before optional features. |
| Auto-Pack makes player decisions irrelevant | It chooses placement, not expedition loadout or what to leave behind. Manual placement remains optional. |
| Booter still replaces BigARM | Eight slots plus field-kit mass policy and BigARM-first resource routing. |
| BigARM becomes a mobile base again | Carrier-only canon; no crafting, safe zone, remote access, infinite storage, or home functions. |
| Mass and slot capacity conflict confusingly | Show one load band plus slots/mounts; plain-language failure; keep optimizer math hidden. |
| One Ironstone stack becomes unrealistically heavy | Tuning stays data-driven; stack limits and mass can be rebalanced at Batch 7 without changing architecture. |
| Cross-owner transfer duplicates or loses items | Prepared two-owner transaction and invariant tests; never remove-then-add. |
| Harvest commits node before destination | Destination preparation wraps `TryConsume` as the commit condition. |
| UI grows into a second inventory authority | Controller issues state commands; views display immutable reads only. |
| Mounted cargo changes collision/pathing unpredictably | Visual-only pooled containers and visual-root lean; stable collider/navigation envelope. |
| Load makes BigARM unable to regroup | Clamped penalties and urgent catch-up minimum; no hard movement stop. |
| Off-screen travel ignores load | Shared derived profile contract required for the future traversal owner; no false proof until integrated. |
| Partial save restores duplicate state | One root save, full validation, staged apply, atomic write/backup, no cargo-only disk file. |
| Dirty worktree overwrites current feature work | Batch 0 manifest, narrow hunks, preserve `.meta`/GUIDs, stop on ownership conflict. |
| Open Unity blocks safe proof | Do not run batchmode while lock exists; perform static/pure proof and defer Unity-owned gate honestly. |
| Quick release creates a world-item framework too early | Separate experiment after core fun gate; remove it if value is not demonstrated. |

## 15. Implementation Completion Criteria

The implementation described by this plan is complete only when:

- the controlling documents describe BigARM as a physical mobile carrier without weakening no-teleport or turning him into a mobile base;
- Booter has one compact, policy-bound field kit and BigARM has one authoritative 12-mount cargo state;
- item mass and simple carry/packing preferences are canonical and validated;
- Auto-Pack is deterministic, idempotent, quantity-preserving, and trustworthy in the user feel gate;
- exact, maximum, and bulk transfers are atomic across owners;
- harvesting selects one physically valid destination and preserves the existing exactly-once resource commit;
- nearby BigARM cargo access and away/read-only behavior are clear and cannot transfer remotely;
- the packing UI is fully usable with gamepad, keyboard, and mouse, with no drag-only action or focus trap;
- mounted cargo is visible, pooled, state-derived, and presentation-only;
- load affects BigARM mildly through the existing movement seams without random loss, hard immobilization, or teleport recovery;
- Booter inventory, BigARM cargo/layout, survival, resources, world identity, and honest companion position participate in one validated coordinated save/load path;
- all required focused tests, Unity compile/import, validator, serialized-diff review, and fixed-view evidence are green;
- user-owned fun/feel results are recorded separately from automated proof;
- no Legacy2D dependency, duplicate inventory/save owner, fallback loot, item physics swarm, or hidden complexity framework was introduced;
- documentation matches proven behavior and remaining limitations;
- only task-owned files are staged and committed, with no push/PR/release implied.

Then stop. Do not continue into crafting, merchant economy, combat inventory, cargo durability, weather damage, marker systems, final art, broad autonomous BigARM AI, or optional quick release without the applicable gate and current authority.

## 16. Implementation-Start Handoff

The next authorized implementation session begins at Batch 0, not by immediately editing `TopDown3DInventoryState`.

Before the first code change, the implementer must:

1. re-audit the dirty overlay and Unity lock;
2. align the controlling BigARM canon;
3. create the task-owned file manifest;
4. freeze the initial packing settings in pure fixtures;
5. prove the auto-pack/load-profile concept in Batch 1;
6. proceed batch by batch, stopping at every red gate.

This ordering preserves the existing inventory and harvesting guarantees while ensuring the system earns its complexity through fun, readable play rather than accumulating features by momentum.

# Ironstone Mining And Inventory System

Status: Implemented in the TopDown3D production lane

Owner: Gottspan coordinates the repository; the TopDown3D runtime owns gameplay state and behavior.

This document records the implemented contract. The approved design and proof plan remains [IRONSTONE_MINING_AND_INVENTORY_IMPLEMENTATION_PLAN.md](./IRONSTONE_MINING_AND_INVENTORY_IMPLEMENTATION_PLAN.md).

## Player Loop

- Deterministic Ironstone nodes are generated as sparse interactive resources in the canonical natural-object chunk plan.
- A valid nearby, unobstructed node can be targeted through the production interaction controller.
- A press of Interact begins a tool-less, in-place 1.1-second Humanoid gather animation. The Input action has no Hold interaction because the gather state machine and animation own the duration. The motor holds an owner-bound action constraint and faces the node; root motion remains disabled.
- Completion revalidates the target and inventory capacity. Resource consumption is the inventory transaction's commit condition, so a failed consume publishes no inventory mutation. A successful action consumes one node use and publishes one direct inventory change containing one Ironstone Ore.
- No ore pickup, ground drop, or fallback object exists. A full inventory leaves the node untouched and reports `Inventory full`.
- A depleted node remains as collision/world geometry, uses its depleted material, and can no longer be targeted.

## Canonical Authorities

- Item truth is `TopDown3DItemDefinition` plus one `TopDown3DItemCatalog`. Ironstone Ore stacks to 99; its current mass metadata is informational and does not limit carrying.
- Player inventory truth is one `TopDown3DPlayerInventory` containing a 16-slot `TopDown3DInventoryState`. All add, remove, move, swap, merge, and snapshot operations are validated transactions; slot views never mutate storage directly.
- Resource type truth is `TopDown3DResourceDefinition` plus one `TopDown3DResourceCatalog`. Resource topology has its own version and is not keyed to decorative or physical-rock generation versions.
- Generated instance truth is the planner's stable ID derived from world seed, resource version, definition ID, and global candidate cell. Chunk load order and runtime generation tokens are excluded.
- Mutable resource truth is one world-root `TopDown3DResourceWorldState`, not a chunk or node component. Chunks reconstruct presentation from the shared plan and current delta state.
- `TopDown3DInputRouter` owns Disabled, Gameplay, and Inventory modes. Inventory mode enables UI and System maps, suppresses Gameplay, clears stale gameplay intent, and leaves `System/ToggleInventory` live on Tab and Gamepad North.
- The router also owns prompt-device selection. Connecting or using a gamepad changes the gather and inventory hints to the current gamepad's binding names; keyboard/mouse use and final gamepad disconnect return them to keyboard labels. HUDs resolve the live Input System bindings rather than embedding `E`, Tab, or controller-specific names.
- The inventory uses its own interactive canvas and the scene's sole EventSystem. The existing gameplay HUD remains passive and raycaster-free; inventory opening does not change `Time.timeScale`.

## Procedural Generation Contract

`TopDown3DProceduralWorld` calls `TopDown3DNaturalObjectPlanner.BuildChunkPlan` once for a chunk and passes that immutable plan to both natural-object and resource decorators. Resource admission uses authored geology, slope, corridor, spawn, formation, spacing, border ownership, and per-chunk density constraints. Depletion does not remove the physical envelope, so traversal and dust behavior keep a stable silhouette.

## Persistence Boundary

Inventory and resource state each expose strict versioned snapshots. Resource snapshots also require the matching resource-generation version. These snapshots prove capture/apply and chunk unload/reload continuity, but no canonical whole-game disk save owner exists yet. Application-restart persistence remains deliberately outside this system; a future save owner must serialize both snapshots together rather than scan loaded chunks.

## Proven Surface And Handoff Boundary

Focused EditMode proof covers inventory rules and snapshots, deterministic resource placement and state, interaction failure paths and exactly-once commit, input/UI modes, locomotion regression, and natural-object regression. The current production scene rebuild and expanded validator pass. Fixed-view evidence for the active/depleted states, target prompt, success receipt, and inventory layout lives in [Docs/Evidence/Ironstone](./Evidence/Ironstone).

Automated proof does not establish subjective animation feel, controller comfort, final art approval, representative Development Player performance, or application-restart persistence. Those remain user/player or future save-owner proof. The separately owned action-D-pad suite remains 7/8 because triangle 263 of its custom UI geometry fails the canvas-facing winding assertion; that pre-existing geometry is outside the Ironstone lane and was not changed here.

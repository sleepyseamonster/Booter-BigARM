# Ironstone Mining And Inventory System

Status: Greater Wasteland bounded harvest area implemented; generated-world prototype retained as reference.

Owner: Gottspan coordinates the repository; the TopDown3D runtime owns gameplay state and behavior.

This document records the implemented contract. The approved design and proof plan remains [IRONSTONE_MINING_AND_INVENTORY_IMPLEMENTATION_PLAN.md](./IRONSTONE_MINING_AND_INVENTORY_IMPLEMENTATION_PLAN.md).

The October 11 production adoption follows the [Greater Wasteland audit](GREATER_WASTELAND_IRONSTONE_AUDIT_2026-10-11.md) and is recorded in the [implementation receipt](../../Evidence/Gameplay/GREATER_WASTELAND_IRONSTONE_IMPLEMENTATION_2026-10-11.md). Greater Wasteland uses twelve authored native-terrain placements in a production-only runtime prefab, not the generated-world planner. The fixed terrain remains always loaded and unchanged. Wider resource distribution and fixed-world disk persistence remain separate slices.

## Player Loop

- Greater Wasteland installs twelve authored Ironstone outcrops near the saved Booter location. The disabled generated-world reference retains its sparse natural-object chunk planner.
- A valid nearby, unobstructed node can be targeted through the production interaction controller.
- A press of Interact begins a tool-less, in-place 1.1-second Humanoid gather animation. The Input action has no Hold interaction because the gather state machine and animation own the duration. The motor holds an owner-bound action constraint and faces the node; root motion remains disabled.
- Completion revalidates availability, range, visibility and inventory capacity. Accessible Legger cargo is preferred; Booter is the fallback if cargo moves away or fills. Resource consumption is the inventory transaction's commit condition, so a failed consume publishes no inventory mutation. A successful action consumes one node use and publishes one direct inventory change containing one Ironstone Ore, with its destination in the receipt.
- No ore pickup, ground drop, or fallback object exists. A full inventory leaves the node untouched and reports `Inventory full`.
- A depleted node remains as collision/world geometry, uses its depleted material, and can no longer be targeted.

## Canonical Authorities

- Item truth is `TopDown3DItemDefinition` plus one `TopDown3DItemCatalog`. Ironstone Ore stacks to 99. Mass does not limit Booter's inventory, while Legger cargo retains its packing mass limits and updates its load profile after direct harvest delivery.
- Player inventory truth is one `TopDown3DPlayerInventory` containing a 16-slot `TopDown3DInventoryState`. All add, remove, move, swap, merge, and snapshot operations are validated transactions; slot views never mutate storage directly.
- Resource type truth is `TopDown3DResourceDefinition` plus one `TopDown3DResourceCatalog`. Resource topology has its own version and is not keyed to decorative or physical-rock generation versions.
- Greater Wasteland authored instance truth is the serialized immutable `greater-wasteland:ironstone:v1:authored:NN` ID. The generated reference derives IDs from seed, resource version, definition ID and global candidate cell. Runtime instance IDs and load order never establish identity.
- Mutable resource truth is one world-root `TopDown3DResourceWorldState`, not a chunk or node component. Node reconstruction reads the shared deltas, and applying a snapshot refreshes already-live nodes. Duplicate live IDs fail closed.
- `TopDown3DInputRouter` owns Disabled, Gameplay, and Inventory modes. Inventory mode enables UI and System maps, suppresses Gameplay, clears stale gameplay intent, and leaves `System/ToggleInventory` live on Tab and Gamepad North.
- The router also owns prompt-device selection. Connecting or using a gamepad changes the gather and inventory hints to the current gamepad's binding names; keyboard/mouse use and final gamepad disconnect return them to keyboard labels. HUDs resolve the live Input System bindings rather than embedding `E`, Tab, or controller-specific names.
- The inventory uses its own interactive canvas and the scene's sole EventSystem. The existing gameplay HUD remains passive and raycaster-free; inventory opening does not change `Time.timeScale`.

## Generated Reference Contract

`TopDown3DProceduralWorld` calls `TopDown3DNaturalObjectPlanner.BuildChunkPlan` once for a chunk and passes that immutable plan to both natural-object and resource decorators. Resource admission uses authored geology, slope, corridor, spawn, formation, spacing, border ownership, and per-chunk density constraints. Depletion does not remove the physical envelope, so traversal and dust behavior keep a stable silhouette.

## Persistence Boundary

Inventory and resource state expose versioned snapshots. Resource snapshots require the matching resource-generation version. Greater Wasteland's in-memory state survives resource-view reconstruction, but restarting or reloading the scene resets the resource owner and inventory. Its fixed-world save integration remains separate and must save inventory/cargo and resource deltas together. The disabled generated reference contains a game save service; it must not be installed into Greater Wasteland.

## Proven Surface And Handoff Boundary

The October 11 implementation receipt records current-machine compilation, production prefab/native-ground validation and focused EditMode proof. Earlier generated-world proof covers inventory rules and snapshots, deterministic resource placement and state, interaction failure paths and exactly-once commit, input/UI modes, locomotion regression, and natural-object regression. Its fixed-view evidence lives in [Docs/Evidence/Ironstone](../../Evidence/Ironstone) and is retained prototype evidence, not a new Greater Wasteland playtest.

Automated proof does not establish subjective animation feel, controller comfort, final art approval, representative Development Player performance, or application-restart persistence. Those remain user/player or future save-owner proof. The separately owned action-D-pad suite remains 7/8 because triangle 263 of its custom UI geometry fails the canvas-facing winding assertion; that pre-existing geometry is outside the Ironstone lane and was not changed here.

# Micro Dust Harvester

## Player intent

Booter can set down a small sand canister. Once deployed, it creates an electromagnetic charge that draws suspended sand from the air. Sand visibly builds up in the device until it reaches capacity. Booter can pick it up at any time and use the collected dust as a resource; waiting for a full canister is optional.

The charge and airborne sand are the in-world explanation. The gameplay loop is **place, let it collect, read its fill level, pick it up**. The dust remains in the canister while it is deployed. Collection stops at capacity and does not consume or erase terrain, ground drifts, or the visual dust atmosphere.

## Interaction and presentation contract

- Placement uses a carried canister and a valid reachable ground point. The action shows Booter setting the device down, then turns on a restrained charge and inward-moving dust effect.
- A deployed canister shows its progress from empty to full. Its visual fill, effect intensity, and interaction prompt should communicate that state without requiring the inventory screen.
- Pickup is available at any fill level, including empty and full. The player receives the canister and its collected dust without loss. Whether the carried dust is represented inside a filled canister item or as a separate resource stack is an implementation decision; either path must be atomic. The device stays deployed if pickup cannot complete.
- A full canister stops collecting and remains available for pickup. The player can then use the stored dust through the normal resource/inventory path. The exact dust-consuming recipes are outside this feature.
- Capacity and collection rate are balance values to tune during play. Any environmental modifier would be a later design choice. The first implementation should keep its tuning explicit and serialized, rather than hiding it in an animation or particle effect.

## Procedural-world contract

- **World identity:** placement belongs to the active world manifest and uses an absolute world position. A player-created placement does not alter deterministic terrain generation.
- **Stable identity:** each placed canister receives its own persistent identity. Its identity survives chunk unload/reload and save/load; it must not be inferred from a transient Unity instance ID or local floating-origin position.
- **Streaming:** the world state owns the placement and fill amount. The visible device is instantiated only while its chunk is active and is removed when that chunk unloads. Unloading must not reset collection or duplicate the device.
- **Persistence:** save placement, canister identity, fill amount, and collection progress or last simulation time. Pickup removes the placement only after inventory acceptance. Save/load and unload/reload preserve partial fills.
- **Authored constraints:** placement must respect traversable ground, collision clearance, and any authored no-placement regions. The electromagnetic effect is local to the canister, not a change to the world dust field.
- **Deterministic proof:** repeat placement at a known seed and absolute address; verify identity and fill across chunk unload/reload, save/restore, full capacity, and pickup with a full inventory. If collection depends on local conditions, sample them through a stable world query rather than camera visuals or frame rate.

## Integration seams

The current TopDown3D prototype already has a player inventory, an interactable/gather action, world-delta storage in `TopDown3DGameStateSaveService`, and chunk streaming. The implementation should add a dedicated placed-device state and interaction instead of treating the canister as an Ironstone node or a cosmetic generated prop. The existing world-delta store has a fixed record bound, so the implementation must reject placement cleanly when it cannot persist another device.

Before accepting runtime behavior, verify inventory transfer is atomic, device state survives the streaming lifecycle, and any new animation/event wiring does not change existing gather or cargo interactions. Hands-on gameplay feel and visual acceptance remain a separate player check.

## Current playable slice

The TopDown3D production scene has a world-owned harvester registry and a separate 32-device limit. Press **G** (gamepad D-pad down) to preview a placement, press it again to set the canister down, or press **C** (gamepad East) to cancel. Face a placed canister and press **E** (gamepad South) or **C** (gamepad East) to pick it up. Booter uses the existing reach/gather animation for both actions. The visible device has a rising dust fill and local inward-moving particles; its target prompt shows the current amount and capacity.

Each canister holds up to 100 dust at a rate of one unit per 10 seconds of active game time. These are provisional values in `MicroDustHarvesterSettings.asset`. Pickup grants the empty canister and a separate airborne-dust inventory stack in one transaction. If both do not fit, the deployed device remains in place. An explicit game save includes the placed records and their collection clock; the game does not grant offline progress.

Booter starts a new game with one micro dust harvester in the normal player inventory, so the placement loop works in the game without an Editor command. Loading a saved game restores its inventory exactly; the starting item is not granted again on load. The canister is returned on pickup and can be placed again. A crafting or loot source for additional canisters remains a later economy decision.

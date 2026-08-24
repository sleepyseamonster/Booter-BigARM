# Inventory And Item Management System

Status: product and implementation specification for the TopDown3D production lane.

This document defines how Booter and BigARM carry, store, harvest, and manage items. It is intentionally built around **fun expedition decisions**, not difficult menu work. The detailed execution order remains in [BIGARM_PACKING_AND_INVENTORY_IMPLEMENTATION_PLAN.md](./BIGARM_PACKING_AND_INVENTORY_IMPLEMENTATION_PLAN.md).

## Design Promise

Booter & BigARM is a traversal game. The world asks the player to choose routes through dry canyons, shelves, chokepoints, exposed basins, cover, and landmark approaches. Inventory should make those choices more interesting:

- Booter travels light and keeps immediately useful essentials close.
- BigARM carries the expedition's bulk on a visible, finite loadframe.
- Ironstone and future bulk salvage naturally become a reason to keep BigARM nearby, assess a route, and decide whether a find is worth taking farther.
- Packing is fast by default and expressive when the player wants to optimize.

The system must never turn into freeform Tetris, strap maintenance, random cargo loss, or repeated icon shuffling. The decision is the game; routine sorting is not.

## World And Traversal Relationship

The [World Creator Charter](./WORLD_CREATOR_CHARTER.md) makes readable routes and agent-profiled traversal first-class world outputs. Inventory consumes that truth; it does not create a second terrain, navigation, or route authority.

| World condition | Inventory consequence | Intended feeling |
| --- | --- | --- |
| Long, open transect | Bulk cargo is useful, but heavy loads make the return journey more deliberate. | A successful haul has weight without becoming a punishment. |
| Narrow canyon, shelf, crossing, or choke | Keep traversal-critical supplies in Booter's field kit or BigARM's quick-access mounts. | Prepare before the commitment, not during a panic menu. |
| Booter-only shortcut | BigARM cannot magically follow. The player chooses a regroup route, a safer shared route, or leaves the shortcut for later. | Meaningful separation, never teleportation. |
| BigARM-compatible main route | BigARM remains physically present and can receive automatic harvested cargo. | The companion is a practical partner, not a remote UI. |
| Overlook, shelter, rest area, or major discovery | A natural moment to inspect the load, transfer essentials, and Auto-Pack. | Inventory management happens between beats. |
| Resource field near difficult terrain | The player decides whether more ore is worth the added return cost. | Exploration and extraction reinforce one another. |

BigARM always retains a true simulated world position. Cargo access, transfer, and repacking require his physical presence. Recall requests traversal; it never teleports BigARM or cargo to Booter. The future World Creator must expose Booter and BigARM route compatibility, separation, and regroup affordances so this rule remains playable across streamed and unloaded terrain.

## Core Carry Model

### Booter: Field Kit

Booter has a deliberately small personal inventory: **8 slots** in the first slice. It is for essentials, quick-use supplies, and short-term overflow—not for replacing BigARM as the main hauler.

Booter inventory is always locally available. It supports stack merge, move, swap, and exact transactional add/remove operations. A full kit does not delete, drop, or silently reroute an item.

### BigARM: Loadframe

BigARM has **12 visible cargo mounts**: six low/core mounts, four side mounts, and two upper mounts. One occupied item stack is represented as one cargo container on one mount. This is a readable abstraction, not a simulation of every ore piece.

The loadframe is the authoritative owner of bulk cargo. Its layout is visible on BigARM, persists with him, and affects his travel presentation. It is finite storage, not a mobile base, safe zone, crafting platform, or remote warehouse.

### Item Metadata

Every item definition supplies its stable ID, display information, icon, maximum stack, category, and per-unit mass. The packing extension additionally classifies an item as:

- `Either`, `BooterPreferred`, `BigArmPreferred`, `BooterOnly`, or `BigArmOnly`;
- `Normal`, `QuickAccess`, `Protected`, or `UpperAllowed` for packing preference.

Ironstone Ore is the current and only resource. It is bulk cargo: `BigArmPreferred`, stackable, and given a per-unit mass that participates in load calculation.

## Harvesting And Destination Rules

Ironstone nodes remain deterministic procedural world objects with stable IDs and world-root depletion deltas. A node is consumed only when its full reward has passed destination capacity validation.

1. Booter targets an unobstructed Ironstone node and starts the in-place gather action.
2. The harvest system validates the entire reward against the best legal destination before consuming the node.
3. If BigARM is within the **harvest tether**—initially tuned around his normal follow band—the reward goes directly to BigARM cargo and the HUD reports `+1 Ironstone -> BigARM`.
4. If BigARM is physically separated, the system may use Booter's field kit only when the item policy permits it and the complete reward fits. It reports that temporary destination plainly.
5. If neither owner can accept the full reward, nothing is consumed. The player receives a specific reason and can make room or regroup.

The harvest tether is intentionally more forgiving than the close transfer radius. It preserves the fantasy of BigARM hauling beside Booter without allowing remote storage across a canyon or unloaded world region.

Gather feedback is mandatory: a target prompt, gather progress/animation, destination receipt, and clear failure state. A player must never interpret a missing target, invalid range, full destination, or missing BigARM as an unresponsive control.

## Packing: Fast First, Optional Depth

### Default Actions

- **Quick Transfer:** move the greatest valid quantity of the selected stack between nearby inventories.
- **Move / merge / swap:** place a selected whole stack in a chosen valid mount or field-kit slot.
- **Auto-Pack:** reorder BigARM's existing cargo into a stable, balanced layout in one action.
- **Take Essentials:** a future convenience action that moves explicitly selected or Booter-preferred stacks; it must never empty BigARM unexpectedly.

Auto-Pack is deterministic and idempotent. It keeps quantities intact, never moves cargo between owners, and prioritizes heavy stacks low, left/right balance, quick-access items on side mounts, protected items in safe mounts, then stable-ID tie breaks. Manual packing is an optional expression layer, never required repair work after ordinary collection.

### Load Consequences

BigARM has three readable bands:

| Band | Initial target | Effect |
| --- | --- | --- |
| Comfortable | 0–75% recommended mass | No handling penalty. |
| Loaded | 75–100% | Mild reduction to acceleration, turning, and cruise speed. |
| Heavy | 100–115% hard limit | Noticeable but usable reduction; never immobilizes BigARM. |

Left/right imbalance is secondary to total mass. It supplies small presentation and handling differences, never a hard prohibition. There are no random drops, container durability, weather damage, or mandatory repacking while moving.

## Interface And Controls

Packing uses one controller-friendly uGUI screen with Booter's Field Kit, BigARM's loadframe, item details, and a concise load summary. When BigARM is nearby, both panes are interactive. When he is absent, transfer and repacking are unavailable; the system does not present a magical remote inventory.

The current Input System remains authoritative:

- `Gameplay/Interact` opens nearby BigARM packing or begins gathering.
- `System/ToggleInventory` opens Booter's field kit and closes the screen.
- The UI map owns navigation, submit, cancel, pointer, and click behavior.
- Prompts use the active device and binding display names; no view hardcodes controller glyphs or keyboard keys.

Opening packing cancels a gather action and suppresses gameplay input without changing global time scale. Closing restores gameplay with stale movement, camera-look, and sprint intent cleared.

The loadframe must communicate only what helps a decision: total load, current band, balance direction, available mounts, item destination, and a plain-language reason when an action is rejected. Optimizer scores and hidden simulation numbers stay out of the player-facing view.

## Presentation

Each occupied BigARM mount has a non-colliding cargo visual attached to an authored anchor. Cargo visuals pool and refresh from inventory state; they are never individual physics bodies. Load should be legible from the production camera through silhouette, side balance, a restrained magnetic-snap receipt on successful transfer, and future sound/posture hooks.

Booter's Ironstone gathering animation is an in-place, non-looping action owned by the player animation driver. The motor owns movement constraint and facing during the action; root motion never moves the gameplay body. Resource award timing remains transactional and cannot be owned by animation presentation.

## Procedural And Persistence Contract

- A carried stack belongs to Booter or BigARM, never to a loaded chunk.
- Ironstone node identity derives from world identity, resource generation version, resource type, and deterministic placement cell. Its depletion is a world-root delta and reconstructs after unload/reload.
- BigARM cargo layout belongs to BigARM's persistent companion state and remains at his saved world position. Save/load may restore the companion there; it must not relocate him beside Booter.
- Inventory and cargo snapshots are versioned DTOs. A coordinated save snapshot validates all owners before accepting a load and must reject incompatible data clearly rather than partially duplicating or losing items.
- The loadframe has no dependency on Legacy2D, scene-only storage, a particular streamed chunk, or a second resource generator.
- A future quick-release cargo bundle is out of the core system. If approved later, it becomes a persistent world object with a stable instance ID, exact world coordinate, chunk presentation lifecycle, and save record.

## Non-Goals And Guardrails

- No grid Tetris, rotation, strap simulation, individual container physics, item durability, theft, weather damage, or random cargo loss.
- No crafting, trading, combat-loadout redesign, generic container framework, multiplayer, or remote cargo transfer in this slice.
- No terrain edits or alternate navigation owner. The World Creator remains responsible for route topology and traversal affordances.
- No promise that a distant landmark is harvestable or that BigARM can use a Booter-only route; those are world affordance decisions.

## Success Criteria

The system is successful when a player can harvest Ironstone with a clear animation and receipt, see it appear on nearby BigARM, access both inventories through physical interaction, transfer stacks in either direction quickly, Auto-Pack a useful stable load, and understand how a fuller load changes a return route—all without repetitive sorting or surprise loss.

Implementation proof must separately establish transactional inventory correctness, deterministic resource/depletion continuity, scene wiring, input/UI parity, save/load behavior, and BigARM's physical-position invariant. Interactive animation feel, route readability, load feel, and controller comfort remain player-acceptance evidence rather than claims this document can prove.

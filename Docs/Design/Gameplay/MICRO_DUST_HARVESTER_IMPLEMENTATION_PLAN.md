# Micro Dust Harvester Implementation Plan

Status: implemented in the TopDown3D production lane; Booter starts with one reusable canister. Seven harvester tests and ten related inventory/persistence Unity EditMode tests passed in an isolated project copy on 2026-10-05. Hands-on visual and animation acceptance remains with the user.

Verification note: the full prototype validator opened the scene and completed its checks, then reported two pre-existing authored formations, `MixedPileScatter` and `HandbuiltSpire`, needing gameplay rebakes. No harvester-specific validator error was reported. The player's foreground Unity session was left untouched.

Planning owner: Gottspan. Product and creative authority: user.

## 1. Done, scope, and source of truth

**Done means:** Booter can carry and place one or more small canisters on valid ground, see each device draw in airborne dust and visibly fill, and pick one up at any fill level. A successful pickup returns a canister and all accrued dust for resource use. Deployed state survives chunk unload/reload and an explicit game save/load. Placement and pickup never duplicate or lose items. Keyboard/mouse and gamepad prompts use the existing input router.

**In scope:** TopDown3D item definitions/catalog, a focused deployment action, placed-device world state and save integration, chunk presentation, interaction/animation, local VFX, HUD feedback, tuning, focused tests, production-scene wiring, and documentation. The first visual is assembled from runtime primitives rather than an authored prefab.

**Out of scope:** crafting recipes, consumption of dust, weather-based yield, automatic collection while the application is closed, a new global dust simulation, terraforming, a general placeable-object framework, multiplayer, a new autosave/menu flow, and work in `Legacy2D/`, `Unreal/`, or `Engine/`.

The controlling sources are the user's [feature brief](./MICRO_DUST_HARVESTER.md), `AGENTS.md`, [WORLD_BASIS.md](../../WORLD_BASIS.md), [WORLD_SYSTEMS_STANDARD.md](../../Engineering/WORLD_SYSTEMS_STANDARD.md), [INPUT_ARCHITECTURE_STANDARD.md](../../Engineering/INPUT_ARCHITECTURE_STANDARD.md), [IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md](./IRONSTONE_MINING_AND_INVENTORY_SYSTEM.md), and then current TopDown3D code/assets for implementation fact. Re-read those live seams before each batch; the terrain, procedural-world, scene, and validator files currently have unrelated edits.

## 2. Brainstormed approaches

### Player-experience directions

| Direction | What the player does | Tradeoff |
| --- | --- | --- |
| Quiet camp tool | Places the canister before exploring, returns to a slowly filled device | Clear first loop and useful preparation; needs readable progress without constant checking |
| Storm chaser | Moves the canister toward dusty areas or storms for higher yield | Adds location strategy, but needs a stable gameplay dust-density query and hazard balance |
| Powered extractor | Feeds it a battery or charge and chooses when to run it | Creates a stronger resource decision, but adds a second consumable and UI state |
| Legger-assisted setup | The companion carries, positions, or protects the device | Fits the pair's synergy, but depends on later companion task and pathing rules |

Start with the **quiet camp tool**. The other directions can be tested later without changing the basic place/collect/pickup contract. A visible settling layer inside a transparent chamber, a soft coil pulse, and a few inward-moving motes can sell the electromagnetic pull; the fill silhouette should remain legible when particles are disabled or the camera is far away.

### Technical approaches

| Question | Candidate | Strength | Cost or failure mode |
| --- | --- | --- | --- |
| Collection | Fixed rate while the game runs | Predictable balance; independent of rendering and camera | Less regional variation until a later rule is authored |
| Collection | Sample the dust-atmosphere intensity | Dusty places could be more productive | Global haze is currently parked; visual intensity is not stable economy authority |
| Collection | Consume nearby ground dust | Strong physical connection to terrain | Requires persistent terrain depletion, regrowth, and new world-query/save rules |
| Inventory | Return canister plus a separate dust resource stack on pickup | Uses the existing stack inventory and multi-item add transaction | Pickup needs room for both; the carried canister no longer displays its fill |
| Inventory | Carry a filled canister with per-instance contents | Retains fill while carried and allows later extraction | Requires non-stack item instances, inventory UI and save-schema changes |
| World state | Keep a world-root registry, with views owned by active chunks | Stable through unload/reload and floating-origin shifts | Requires a narrow streaming lifecycle hook and save integration |
| World state | Let the placed MonoBehaviour own dust | Quick prototype | Unload destroys the authoritative amount; save scanning misses absent chunks |
| Presentation | Small 3D prefab with a fill window and local inward particles | Reads at gameplay distance without changing global haze | Needs cost cap and visual review |
| Presentation | Global field distortion or screen-space dust suction | Dramatic effect | Risks camera-dependent noise and a rendering-system change |

**Recommendation:** fixed-rate, active-game simulation; a world-root registry with absolute position and stable placed ID; a chunk-local 3D view; and, for the first playable slice, an atomic canister-plus-dust pickup. This is the smallest path through the current inventory. A filled carried canister is a valid later direction if the player experience calls for one, but it should not be simulated with hidden item IDs for every fill amount. Legacy2D's canister is a read-only behavioral reference. Its pickup checks room for the canister but does not ensure the dust fits, so its transaction order must not be ported.

## 3. Proposed player flow and provisional tuning

1. Booter owns an empty canister item. A dedicated `Deploy Harvester` Gameplay action enters a short placement preview in front of Booter. Confirm places it; cancel spends nothing. Inventory mode and other constrained actions suppress placement. The Input System asset and `TopDown3DInputRouter` own bindings and prompt names; choose non-conflicting keyboard and gamepad bindings during implementation.
2. Placement requires active traversable terrain, acceptable slope, collision clearance, reach, and no authored exclusion. Show invalid placement before the item is spent. On confirm, Booter plays a short set-down action. The canister is removed only when the world state accepts a new placement.
3. The device draws dust through a small local charge and inward particles. A visible fill window and target prompt show stored units and capacity. The effect eases or stops at full. The renderer does not supply the collection rate, and collection does not reduce the atmospheric or ground-dust visuals.
4. Interact with a nearby canister to pick it up, including when empty or partially full. The action locks movement briefly and uses a reach/pickup animation. Completion rechecks range, identity, and inventory capacity. It atomically adds one canister plus all integer dust units, then removes the deployed record. If either item cannot fit, leave the device and its dust in place and report why.
5. First-pass values are **tuning proposals, not accepted balance**: capacity 100 units, 1 unit every 10 seconds of active game time, one-unit display increments, and an initial deployed-device cap of 32 per save. Keep these in one authored settings asset. The user can tune them after a hands-on pass.

The plan permits multiple canisters because each has its own identity and placement record. The cap bounds save size and view cost. Booter starts a new game with one reusable canister, so the loop is accessible in the game itself. A crafting or loot source for additional devices is outside this slice.

## 4. Authority and persistence design

| Concern | Owner |
| --- | --- |
| Canister and dust item definitions | Existing `TopDown3DItemCatalog` and `TopDown3DItemDefinition` assets |
| Player item counts | Existing `TopDown3DPlayerInventory` / `TopDown3DInventoryState` |
| Placement command and animation timing | Player action controller plus input router and animation driver |
| Deployed device records and collection clock | New world-root `TopDown3DPlacedHarvesterState` (plain versioned data behind a MonoBehaviour owner) |
| Active visual, collider, fill display, local VFX | Chunk-owned harvester view/prefab, reconstructed from the world-root record |
| Disk save/load | Existing `TopDown3DGameStateSaveService`, extended with the typed harvester state |

Each deployed record stores a stable ID, world-manifest identity, precision-safe absolute position, owner chunk/address, stored integer dust units, fractional collection progress, and last evaluated simulation tick. The world-root clock advances while the game simulation runs, including when inventory is open or the owning chunk is unloaded; it stops when the application is closed or the simulation is paused. Evaluate a record lazily when it becomes visible, is queried, is picked up, or is saved. Clamp at capacity. Materialize records before capture so reload cannot award the same interval twice. Changes to future tuning must not retroactively recalculate previously accrued dust.

The stable ID is minted once from current world identity, absolute address, and a persisted placement nonce; it is not a Unity instance ID, local floating-origin coordinate, or generated-resource ID. The registry is independent of terrain generation. A new chunk lifecycle hook publishes/removes only the view for records in that chunk. A chunk unload destroys the view, not the record. Rebase updates view positions from absolute addresses, not the saved identity.

Prefer a typed, versioned harvester snapshot wired into the existing coordinated save transaction over gameplay code directly mutating opaque `worldDeltas` payloads. Define an explicit compatibility path so existing version-3 saves load with an empty harvester set; avoid a version bump that simply rejects those saves. Save validation checks duplicate IDs, finite positions/progress, world compatibility, bounds, and quantity limits before applying any inventory or world state. Rollback restores the previous registry if another part of the coordinated load fails. The existing `Save()`/`Load()` API is an explicit save path; this slice does not claim automatic saving.

For placement, add a narrow conditional inventory removal or equivalent transaction: validate site and registry capacity, commit the world record, then publish the inventory decrement exactly once with rollback on failure. For pickup, simulate a two-item add and use one commit condition to remove the exact deployed record. Do not use two sequential `TryAdd` calls or a drop fallback. Because the generic gather interface exposes only one `Reward`, use a dedicated pickup action that shares target selection, line of sight, feedback, and motor constraints without misrepresenting a canister as an Ironstone node.

## 5. Implementation batches and gates

| Batch | Work | Pass evidence |
| --- | --- | --- |
| 0. Reconcile ownership | Recheck current Git status, scene/world changes, Unity lock, catalog builder behavior, and save tests. Reserve only the exact files needed. Do not rebuild the dirty production scene or run batchmode against an open editor. | File ownership list and no accidental modifications to other work |
| 1. Economy and state | Add item definitions/icons, authored tuning, world-root registry, clock, stable IDs, placement/pickup transactions, and strict snapshot validation. Extend the item builder so an Ironstone rebuild does not reset the catalog to one definition. | Focused EditMode tests for partial/full fill, cap, duplicate identity, no dust loss at inventory limits, exactly-once pickup, and old-save compatibility |
| 2. World lifecycle | Add a narrow chunk loaded/unloaded/rebased view hook and valid-ground placement query. Keep the registry above chunks. | Focused tests for same-ID view reconstruction, no duplicate view, no unloaded-state reset, boundary ownership, and rebase-safe position |
| 3. Controls and presentation | Add deploy/confirm/cancel input, bound prompts, set-down/pickup action animation, 3D prefab, fill display, and local charge/dust particles. Wire the production scene only after its unrelated edits are reconciled; update the guarded builder and validator consistently. | Input-mode and action-interruption tests; validator/compile pass; fixed-camera visual evidence at empty, partial, and full states |
| 4. Coordinated save | Wire typed registry capture/prepare/apply/rollback into `TopDown3DGameStateSaveService`, including old saves. | Save/reload tests with loaded and unloaded devices, partial progress, full state, corrupted payload rejection, and inventory/world rollback |
| 5. Review and closeout | Run only affected EditMode/validator checks through a background-safe Unity path, inspect serialized assets and `.meta` files, self-audit edge cases, and update status docs. | Exact test result, Git diff check, performance counts for the proposed cap, and an explicit user-owned hands-on gameplay/animation assessment |

Batch order can group tightly coupled code, but every commit candidate must include its matching `.meta` files and pass the relevant gate. The large unrelated terrain/scene changes remain user-owned; integrate through their then-current APIs instead of reverting or silently absorbing them.

## 6. Decisions and stop conditions

- **Ready to implement under the proposed default:** fixed-rate collection, integer dust, no offline progress, atomic separate dust/canister pickup, multiple deployed devices within a cap, and local 3D VFX. The values in Section 3 remain tunable.
- **Ask before changing the model:** if the carried canister must retain and display its exact fill, choose that explicitly before Batch 1 because it changes the inventory and save contract. Also ask if weather/region should affect yield in the first slice or if collection should continue while the game is closed.
- **Stop implementation rather than weaken proof:** if the current dirty scene or procedural-world owner cannot be safely integrated, finish independent state/tests, then report the exact blocked wiring. Do not create a parallel runtime bootstrap to avoid that ownership seam.
- **Acceptance boundary:** automated tests can prove transactions, serialization, identity, and lifecycle contracts. The user assesses placement feel, animation timing, VFX readability, and balance in a hands-on session; do not create or run a gameplay smoke test without an explicit request.

# Booter and Legger inventory audit and iteration plan

Date: October 11, 2026. Owner: Gottspan. Status: audit and proposed iteration sequence; no gameplay changes authorized by this audit.

The existing item catalog, inventory state, cargo owner, input router and UI provide a useful foundation. The next iteration needs strict carrier rules and a clearer controller interaction model. Booter currently accepts resources, the production backpack still has 16 slots, transfers do not honor the chosen destination slot, and cargo navigation relies on automatic geometry-based focus. These are more urgent than adding packing simulation or increasing the number of items.

## Controlling direction and scope

The author's October 11 direction is explicit: **Booter's limited backpack carries personal items and tools, never resources; all resources are carried by the Legger.** This supersedes the earlier plan's emergency resource overflow and Booter harvest fallback. The Legger remains finite and physically present; opening a menu, harvesting, recalling him or loading a snapshot must never teleport him or permit remote cargo mutation.

This audit proposes quick sorting and transfers as the first experience, with optional physical packing afterward. That is an iteration recommendation, not acceptance of a new packing mechanic. Eight backpack slots are a reasonable starting experiment because the current code default already uses eight; the final capacity and treatment of tool stacks remain tuning decisions. No spatial item shapes, Tetris, new art, item recipes, storage depots, or package changes are assumed.

## Verified current implementation

Inspection started at `6ff388e9693ed96df5b08d8521aa24f5ab8d6e9a` on `main`, tracking `origin/main`. Waypoint/camp, UI, material, rock and terrain-transfer work was already dirty or in progress and remained separately owned.

| Surface | Current fact | Consequence |
| --- | --- | --- |
| [Booter inventory](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DPlayerInventory.cs) | Code default is 8 slots; saved Greater Wasteland overrides capacity to **16**. It starts with one dust harvester and one popup camp. | Changing the default alone cannot shrink production. Storage is stack slots, not volume. |
| [Item definitions](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemDefinition.cs) | Four catalog items: Ironstone Ore, Airborne Dust, Micro Dust Harvester and Popup Camp. All currently resolve to `Either` carrying permission. | Neither resource is restricted to the Legger. |
| [Inventory policy](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryPolicy.cs) | Booter rejects only `BigArmOnly`; the Legger rejects only `BooterOnly`. Preferred destinations are not ownership restrictions. | Resource-only carrying needs explicit metadata and catalog validation, not a UI filter. |
| [Legger cargo](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/TopDown3DBigArmCargo.cs) | 12 mounts; 3.2 m access range; mass is enforced and direct inventory changes update the load profile. | Preserve its authoritative state and recent harvest fixes. |
| [Packing settings](../../../Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset) | 6 core, 4 side, 2 upper mounts; comfortable through 60 mass, recommended 80, hard limit **92**. Every mount declares `maxStackMass: 20`. | The displayed recommended capacity and actual maximum differ; mount limits are declared but unenforced. |
| [Cargo movement response](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs) | Load bands affect speed and acceleration. Balance is not consumed by the follower. Normal follow distance is **4.2 m**, outside cargo reach. | Balance currently supplies a score rather than a demonstrated movement benefit. Resource-only harvesting needs proximity coordination. |
| [Inventory screen](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryCanvas.cs) | Two four-column grids, item details, a mass/balance summary, Auto-Pack and radial customization. | Keep useful regions, but replace ambiguous slot activation with explicit actions and focus. |
| [Controller behavior](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs) | Submit selects a stack, then a target; Cancel closes the screen. Opening cargo can focus its first mount, while reopening normally restores a Booter slot. | Selecting, carrying, transferring and cancelling are not distinct interaction states. |

Ironstone stacks to 99 at mass 1.25; dust stacks to 100 at mass 0.05. A harvester stack holds up to four tools; a popup camp stack holds one. The two tool definitions use QuickAccess packing preference, but carrying permission is still unrestricted. Booter's personal algae canister is a separate survival state, not an inventory catalog item occupying a backpack slot. Decide whether it is equipped personal gear or backpack storage when defining capacity; personal survival supplies must not be confused with raw gathered resources.

## Findings that must precede a controller redesign

### Carrier rules and harvest coordination

[The destination service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DItemDestinationService.cs) always tries accessible permitted Legger cargo first, then returns Booter. It does not implement `BooterPreferred` routing. The recently implemented Ironstone gather deliberately rechecks cargo access and falls back to Booter at completion. That behavior was correct for the prior instruction and must now change for resources.

Author both resource definitions as `BigArmOnly` and validate every resource-producing definition against that restriction. Update authoring tools as well: the Ironstone asset builder currently authors `BigArmPreferred`, while the saved Ironstone asset omits that field and therefore resolves to `Either`. Future resources must not silently inherit permissive zero defaults. Introduce a typed storage-role field only if it adds a clear authority beyond existing permission metadata; do not rely on a display category string alone to enforce ownership.

Resource gathering must require a valid physically reachable Legger destination both before starting and at commit. If he is absent, obstructed, inactive or full, leave the world resource untouched and report the specific reason. Personal/tool rewards may use their permitted carriers. Do not increase reach arbitrarily to conceal a follow-behavior mismatch.

The current normal follow goal is 4.2 m behind Booter, versus 3.2 m cargo access. On straightforward paths, normal following can leave him too far away for resources. The existing `RequestCallOver()` physically approaches a closer position and retains that stance until Booter moves away. Reuse or refine this seam for an explicit local harvesting/cargo-access context: approach without teleporting, show `Legger approaching`, permit cancellation, and report route failure. Opening cargo should keep him physically nearby rather than let ordinary following walk him out of reach while Booter manages items. Survival and threats need not pause to accomplish that.

### Dust harvester pickup spans two carriers

[Placed-harvester pickup](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DPlacedHarvesterState.cs) currently adds the recovered tool **and collected dust to the same inventory**, and the player action controller passes Booter's state directly. Restricting resource metadata alone would make every dust-filled pickup fail.

Prepare a combined operation that returns the tool to an allowed carrier and routes dust only to the Legger. Validate both destinations and consume/remove the placed device only when the entire pickup can commit. If either destination is unavailable or full, preserve the device and its contents. Do not create an implicit exception that lets Booter carry resources inside a tool to bypass the rule.

### Transfers are not a joint atomic operation

[The transfer service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryTransferService.cs) commits source removal and publishes its change event before destination addition. On destination failure it attempts to add the items back. This is not the prepared two-inventory transaction described in the old plan. A scratch-data probe confirmed an observer sees a combined quantity of **0 during a successful transfer of 10 dust**, although the final combined quantity correctly returns to 10. No routine item loss was demonstrated; the issue is observable intermediate state and an unvalidated rollback dependency.

Use prepared mutations for both inventories, validate all constraints, install both states, then publish notifications after the joint commit. This is necessary for coherent save capture, mixed tool/resource pickup, bulk operations and future observers. Reject invalid requests before any mutation. Revalidate access at the transaction boundary and return moved quantity plus a structured reason.

### Selecting a slot does not transfer that slot

Cross-container activation ignores the clicked destination index and calls item-ID transfer. Destination addition merges into existing stacks and uses its first empty slot. Source removal walks matching stacks in reverse slot order, so it can empty another stack instead of the one selected. A scratch probe selecting a 100-dust transfer from an inventory containing `[100, 50]` leaves `[50, empty]` rather than moving the first whole stack. Total quantity is preserved, but the visual promise is wrong.

Separate **quick transfer**, which explicitly chooses placement automatically, from **manual move**, which honors the selected source and destination. Same-container movement can retain existing move/merge/swap rules. Cross-container manual movement must validate carrier permission and mass before a joint transaction; selecting an occupied slot must not silently mean a different placement.

### Selection and failure feedback are fragile

- Transfer and move results are discarded by the UI. Failure can clear selection with no explanation, and partial movement supplies no receipt or remaining quantity.
- Pending selection is only a slot index and container flag. Auto-Pack can reorder that slot without cancelling the pending selection, so subsequent confirmation can act on a different stack. External inventory changes need the same protection.
- Cancel closes the whole inventory even when a stack is selected. It should first cancel the local operation, then back out of a submenu, then close the inventory.
- Cargo slot refresh sets every slot button interactable. Access locking is reapplied only when reach changes or the screen opens; an ordinary refresh while away can make locked cargo buttons focusable again. Handler distance guards still reject transfers, but visual state and focus become misleading.
- Reopen focus remembers a slot number without preserving the owning container. Losing cargo access does not reliably move focus to a valid inspectable/read-only element or personal item.
- UI cargo actions use distance alone, unlike initial interact targeting with line of sight and harvesting with active-cargo checks. An inactive or obstructed carrier must not gain remote transfer/repack access through the menu.

Store an explicit focus address `(container, stable slot or item address)` and explicit browse/action/move states. Cancel or validate pending commands when item state changes. Read-only cargo inspection should remain usable while away, with visible unavailable reasons and mutations guarded by one shared access service.

## Packing is currently a misleading optimization target

[Auto-Pack](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/TopDown3DCargoAutoPacker.cs) deterministically reorders whole stacks using mass, height and preference scores. It does not split or consolidate stacks and does not enforce `MaxStackMass`; neither normal cargo addition nor movement enforces that per-mount setting. Do not begin enforcing 20-mass mount limits without defining how larger stacks are represented or split, or legitimate cargo becomes impossible to pack.

With no other cargo, 73 Ironstone fit under the total mass limit (91.25), while 74 do not (92.5). The item stack maximum of 99 is therefore not the effective cargo capacity. Current UI reports load against 80, hiding the actual 92 maximum. Show occupied storage and remaining hard capacity clearly; the comfortable/recommended bands can remain secondary feedback.

A single 12-ore stack weighs 15 and receives **0% balance** because it sits wholly on one side. Auto-Pack cannot split that stack and repeating it cannot improve the score. The score also ignores mount height, and the follower uses weight bands rather than balance. Telling the player this load is defective creates work with no actionable reward. The transient `Auto-packed: stable` UI message is immediately replaced by the normal summary.

For the first controller iteration, use a trustworthy `Organize cargo` action and clear load status. Physical packing should be optional and display only consequences the system actually implements. Later choose deliberately between distributing stack mass across physical containers or retaining whole stacks with a less punitive balance presentation. Neither choice is accepted by this audit. Restore mount meaning before exposing a low/core/side/upper arrangement as optimization gameplay.

## Proposed gamepad interaction model

Use one clearly labelled screen with **Booter Kit**, **Legger Cargo** and a compact selected-item panel. Ordinary navigation and cargo browsing should need one stick or D-pad, not a virtual mouse or both sticks. Keep physical packing in a separate optional view so routine resource collection never requires moving an icon.

| Action | First iteration proposal | Required behavior |
| --- | --- | --- |
| Open/close | Keep current North/Y and keyboard Tab initially. | Preserve established control expectations. Contextual Legger Interact opens cargo focus. |
| Navigate | D-pad or left stick, with explicit focus links. | Movement matches the visible grid; every slot and command is reachable. Right stick need not navigate the same screen. |
| Switch container | LB/RB, using dedicated inventory-mode actions. | Preserve focus separately in each panel; no radial opening or gameplay leakage. |
| Contextual action | South/A. | Inspect resources; show or execute only valid personal/tool actions. Resource-to-Booter transfer is unavailable with its reason. |
| Quick transfer | West/X for a permitted selected personal/tool stack. | Automatic placement, one command, moved-quantity receipt; never ask for a destination slot. Resources remain with the Legger. |
| Optional move/quantity | Explicit item action. | Whole-stack manual moves honor target slots; quantity uses a D-pad-adjustable selector with digital confirm/cancel, not a mandatory hold or chord. |
| Organize/Auto-Pack | Persistent reachable command activated by South/A; keep the existing radial shortcut. | Visible result, no data loss or stale pending selection. A dedicated shortcut can follow control review. |
| Cancel/back | East/B. | Cancel a move/quantity choice first, return from item actions second, close third. Keep North/Y as a direct close. |

**Do not bind Y to sorting while the existing System/ToggleInventory is still Y.** The System map remains enabled in Inventory mode, so such a binding would also close the screen. A later proposal can deliberately move the opener and assign a sort shortcut, but the first iteration need not change the whole control scheme. LB currently opens the radial in Gameplay; its controller already rejects Inventory mode, and new inventory actions must preserve that isolation.

Both sticks currently feed UI/Navigate. Returning from Inventory does not use the neutral-stick safeguards applied to other modal modes, so held navigation input may immediately become gameplay movement or camera input. Add focused held-input transition proof and choose a consistent release/neutral barrier across inventory, radial and system menus.

The recommendations use predictable focus, consistent confirm/back actions, digital navigation and clear contextual labels, consistent with [Xbox UI navigation guidance](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112). Reduce required simultaneous/rapid inputs and support action remapping as described by [XAG 107](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107). Explain the effect and availability of an action before confirmation, following [XAG 114](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/114). These principles support the proposal; they do not prove this game's feel or accessibility conformance.

## Proposed implementation batches and acceptance gates

1. **Carrier rules and capacity.** Restrict all resource producers and stock definitions to the Legger, update authoring tools, reconcile production Booter capacity, and specify equipped personal supplies versus stored tools. Coordinate physical proximity rather than reintroduce overflow. Gate: every resource addition, transfer, reward, restore and mixed harvester pickup rejects Booter safely; unavailable cargo never consumes a resource.
2. **Transaction and access correctness.** Prepare joint transfers and multi-destination pickup; unify active/range/visibility access, explicit failure reasons and pending-command validation. Gate: partial/failed requests, range loss, auto-pack changes and event observers preserve quantities and coherent state, and exact placement honors both slots.
3. **Controller browsing and actions.** Add explicit focus, one-stick/digital navigation, panel switching, quick-transfer, layered cancel and device-derived context hints. Integrate existing radial/system/camp modes and held-input barriers. Gate: all actions are reachable without a mouse, selection survives ordinary refresh, unavailable actions explain themselves, and cancel never loses an item or moves Booter accidentally.
4. **Packing feedback and optional expression.** Decide whether to retain mount limits, physical bin allocation and balance consequences; first make organization and capacity understandable. Gate: organization preserves counts, remains deterministic/idempotent, supplies an accurate result and cannot label an unfixable single-stack load as the player's mistake. Do not add punishment mechanics to make a score appear meaningful.

Fun acceptance belongs to the author. Suggested scenarios are finding a personal tool, transferring one permitted spare, moving a cargo stack, recovering from full cargo, inspecting an absent Legger's manifest and cancelling an operation. Start with the current four items, then use a clearly non-canon mixed/full-inventory fixture to test navigation and decision load. Measure navigation presses, time, accidental actions and whether the player can explain what happened; proposed targets are one command for routine transfer, no menu work per resource pickup, and ordinary preparation within roughly thirty seconds once learned. These are trial gates, not proven performance numbers.

## Persistence and world compatibility

Carried state belongs to stable Booter/Legger inventory identities, not terrain tiles, dressing cells or UI objects. Item definitions own stable IDs; UI focus and sorting never become item-quantity authority. Terrain streaming is not applicable in fixed, always-loaded Greater Wasteland, but view recreation must restore the same inventory and cargo state without relocating the Legger. Generated-world save services remain outside production.

Version 1 inventory snapshots require an exact capacity match and reject items disallowed by the current policy. Shrinking 16 slots to 8 or restricting resources therefore needs an explicit load/migration policy. Transfer existing resources only into available, valid Legger capacity; never truncate slots, silently destroy overflow or manufacture remote cargo access. Current production has no coordinated disk inventory/resource save, but tests and retained snapshots still need compatibility handling. A future save owner must capture both carriers and world-resource deltas after a joint commit, not between source and destination events.

Authored constraints remain physical reach, safe approach, companion route/collision behavior, stable placement and finite carrying capacity. Inventory work must not generate or stream terrain. World drops, detached cargo, depots and bulk unloading are later world-state slices requiring their own IDs, ownership and persisted deltas; they are not assumed implementations here.

## Evidence and closeout boundary

Evidence comprises current C# source, stock item/settings assets, parsed input bindings and saved scene configuration, plus two background Unity CLI probes using **new temporary plain inventory models only**. The probes confirmed resource acceptance by Booter, the 73/74-ore hard-limit boundary, the single-stack zero balance, the selected-stack mismatch, and the intermediate transfer notification state. Real inventories, items, scene objects and UI were not mutated. The production scene SHA-256 remains `BD1E6687C586B6EB9B2AA191263B45FB33DF04898E47FA26EB0ABADB36B6E850`.

The earlier Ironstone implementation's 72 passing focused tests remain retained evidence of the existing loop, not proof of this new ownership direction or gamepad comfort. Current packing tests cover deterministic ordering and one all-fitting transfer; inventory UI proof is primarily single-container. Future proof must add strict-owner/migration matrices, two-container atomicity and manual placement, crowd/full-inventory navigation, sorting during selection, carrier-access loss, layered cancel, held-stick transitions and mixed-reward pickup.

The August packing plan and first-slice status contain stale scene, item, save and validation statements. They remain historical context; this audit controls the next proposed iteration under the author's new resource-only carrier direction. No runtime, scene, prefab, item definition, binding, package or project setting was changed, no Play Mode or gameplay smoke test was run, and no Unity window was activated.

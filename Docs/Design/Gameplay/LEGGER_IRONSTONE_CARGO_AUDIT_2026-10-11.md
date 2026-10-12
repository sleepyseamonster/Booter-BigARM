# Separate inventories and automatic Ironstone cargo audit

Date: October 11, 2026. Owner: Gottspan. Status: focused audit; gameplay implementation remains a subsequent task.

**Booter and the Legger already have separate authoritative inventories.** The immediate work is to enforce resource ownership, make their separation obvious in the UI, and make automatic mining delivery reliable. A new inventory framework is unnecessary for this slice. Calculated load risk/reward remains the longer-term direction; breakdown, damage, advanced balancing and complex physical packing are deferred by the author's clarification.

This narrows the next implementation suggested by the [broader inventory audit](BOOTER_LEGGER_INVENTORY_AUDIT_2026-10-11.md). Full transfer redesign and packing simulation are not prerequisites for one Ironstone reward going directly to one cargo owner. Existing dust pickup still needs its own mixed tool/resource handling before the global resources-only-on-Legger rule can be declared complete.

## What exists now

| Concern | Booter | Legger |
| --- | --- | --- |
| Production owner GameObject | `Booter Perspective 3D Controller` | `Legger` |
| Inventory component | [TopDown3DPlayerInventory](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DPlayerInventory.cs) | [TopDown3DBigArmCargo](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/TopDown3DBigArmCargo.cs) |
| Storage | Its own private `TopDown3DInventoryState` and slot collection | A different private `TopDown3DInventoryState` and slot collection |
| Production capacity | 16 serialized slots; code default is 8 | 12 cargo mounts |
| Current permission | Can still carry Ironstone and dust | Can carry those resources and other permitted goods |
| Current mass enforcement | No strict mass limit | Strict total mass limit of 92 |

Both reference the same item catalog. That catalog describes what an item is: stable ID, icon, name, stack maximum and mass. It does not store either carrier's quantities. Sharing definitions, a generic inventory class and a UI coordinator does not share stored goods.

[The inventory UI controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs) already passes Booter and cargo states into separate grids. The screen's broad `INVENTORY` title, weaker `LEGGER LOADFRAME` label and permissive resource rules can obscure the distinction. Give each owner an explicit heading, independent occupancy/capacity display and distinct focus target. Do not build a second item catalog or move quantities into the UI to achieve that clarity.

## What automatic mining currently does

[The player action controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs) resolves same-scene cargo and constructs a destination service with both inventories. [The destination service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DItemDestinationService.cs) chooses the Legger when active, near enough and able to accept the reward; otherwise it returns Booter. [The gather state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DGatherActionState.cs) chooses again at completion, then adds ore directly to that destination under the node-consumption condition. Booter does not have to receive the ore first. The existing receipt identifies `Legger` or `Booter`.

Thus automatic Ironstone delivery already works for accessible cargo with room. The current fallback and unrestricted item permission explain why ore can instead appear in the backpack. The [stock Ironstone definition](../../../Assets/_Project/Settings/Items/Item_IronstoneOre.asset) omits carry preference, resolving to `Either`; its authoring builder would currently write `BigArmPreferred`, which also permits Booter.

## Smallest useful next implementation

1. **Keep the two existing inventory owners.** Booter's limited kit holds personal items/tools; Legger cargo holds resources. Reconcile the intended backpack capacity separately from the mining destination fix, and show the actual configured capacity rather than an assumed eight slots.
2. **Make Ironstone Legger-only.** Set its authored permission to `BigArmOnly`, update its asset builder, and enforce the restriction through inventory add, transfer and snapshot validation. Existing illegal backpack contents require an explicit migration/rejection policy; do not delete or remotely transfer them silently.
3. **Remove resource fallback to Booter.** Return a structured destination failure when cargo is missing, inactive, unreachable, full or too heavy. The public destination service must handle an unavailable result safely; merely returning a null inventory would break its current `AddHarvest` helper. Keep reason-specific feedback rather than reporting every case as `Inventory full`.
4. **Coordinate physical harvesting access.** Normal following targets 4.2 m behind Booter, while cargo access is 3.2 m. Use the existing physical call-over/approach seam for harvesting and cargo management, with approach, cancel and blocked-route feedback. Revalidate access at completion. Do not create remote delivery or teleport the Legger to avoid this mismatch.
5. **Keep the existing direct harvest transaction.** A successful gather consumes one node use and adds one ore directly to Legger cargo once. Failure/cancellation consumes nothing, and Booter's quantities and inventory notifications remain unchanged. No manual transfer, menu opening, ground drop or temporary backpack stage should be part of ordinary mining.
6. **Make the result visible.** Preserve the `+1 Ironstone Ore -> Legger` receipt, refresh cargo quantity/load, and present `Booter Kit` and `Legger Cargo` as separately labelled inventories. Existing gamepad navigation is enough to inspect the first slice; the broader controller-action redesign can follow.

The existing one-use/one-ore/1.1-second gather tuning can remain unchanged. The player sees a depleted physical outcrop after extraction; the harvested inventory item is Ironstone Ore, not the entire terrain rock. No changes to yield, recipes or extraction tools are implied.

## Capacity now and calculated risk later

[Packing settings](../../../Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset) already provide three load regions: comfortable through 60 mass, loaded above 60 through 80, heavy above 80 through the hard maximum of 92. Existing load response reduces speed/acceleration; it does not implement breakdown, wear, power drain or cargo loss.

Ironstone currently weighs 1.25 per unit and stacks to 99. An otherwise empty Legger can carry **73 ore** under the 92 total-mass limit; 74 exceed it. A twelve-node starter harvest adds 15 mass. Other carried goods reduce that remaining allowance. The twelve slots and item stack maximum are separate limits; the UI should show occupied storage and actual hard load capacity, with the load band as readable feedback. These values are current tuning, not accepted final balance or a claim of kilograms.

Future calculated risk/reward can build on these existing authorities: how much to take, how much reserve capacity to retain, whether a heavier return trip is worthwhile, and eventually what condition/energy budget permits. Those future mechanics should expose predictable costs before the player commits. They are not needed to prove exclusive automatic cargo delivery, and this slice must not claim a breakdown risk that no system simulates.

## Required proof for the next implementation

- Separate inventory and slot identities; shared immutable definitions remain safe.
- Accessible Legger gains exactly one ore, Booter gains zero, and only the Legger inventory emits a reward change notification.
- Resource-to-Booter add, transfer and incompatible restore attempts reject without mutation.
- Cargo unavailable/full/too heavy before starting or at completion leaves the resource untouched, with the appropriate reason and no backpack fallback.
- Legger approach stays physical and reports blocked routes; UI access cannot bypass active/range/visibility checks.
- Capacity boundary, stack merging, cargo load refresh and actor/scene reinitialization remain correct.
- Controller inspection shows the correct owner and count; opening cargo does not require manually moving each mining reward.

Broader transfer atomicity, exact manual placement, sorting during selection and controller focus issues remain in the prior audit for later batches. Dust harvester pickup is the important adjacent integration: it currently returns tool and dust to Booter together. Applying resource-only restrictions globally must route that mixed reward safely without breaking device recovery.

## World compatibility and persistence

Carried items belong to stable carrier inventories, independent of terrain or renderer objects. Native Greater Wasteland remains fixed and always loaded, so terrain streaming is not applicable. Resource IDs/depletion stay with the existing resource world-state owner. Inventory UI reconstruction must reconnect to the same two states and cannot relocate the Legger. No generated terrain/save services are reactivated.

Application-restart persistence is still a separate production slice. Both carriers and resource depletion must eventually be saved coherently so exclusive delivery cannot be duplicated or lost on restore. This audit does not install a save owner or change snapshot formats.

## Verified evidence

Inspection started at `fa791d4194c65ba2a0934ae1796177506381e5a2` on `main`, tracking `origin/main`. Background read-only inspection of the loaded production scene confirmed different owner GameObjects, a shared catalog, Booter's 16 slots, the Legger's 12 mounts, 3.2 m cargo reach and 60/80/92 mass thresholds. The scene was clean and outside Play Mode.

A separate scratch-data probe used new temporary plain inventory states built from the stock catalog and packing settings. The inventory objects and slot collections were different; their catalogs were the same. Automatic accessible delivery succeeded with **Booter ore 0, Legger ore 1, Booter events 0, Legger events 1**. Marking cargo inaccessible selected the Booter fallback, and Booter's policy still accepted another ore. No real inventory quantities, scene objects, assets, input bindings or gameplay were changed by these probes.

This is fresh structural/data-path evidence, not a mining replay or subjective controller playtest. No gameplay smoke test, foreground activation, runtime implementation, asset migration, package change or external publication was performed. Separately owned waypoint/camp, UI, material, rock and terrain work remains untouched.

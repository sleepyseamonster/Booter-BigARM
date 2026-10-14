# Camp Loop inventory audit and redesign brainstorm

Date: October 14, 2026. Owner: Gottspan. Status: design proposal only. Target: `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`.

Booter's inventory should show the equipment he has fitted and the supplies he can carry. The Legger, Booter's physically present companion, carries gathered resources and the expedition's larger cargo. Replace Booter's generic backpack presentation with six dedicated equipment slots and a small supply area; keep cargo capacity, access and transfer rules distinct.

The author clarified that “Lager” meant the Legger, not a new camp-wide store. This audit changes no Unity assets, gameplay, survival tuning or accepted canon. The six slot purposes and resource prohibition below come from the author's direction; pocket counts, layout and handling rules remain proposals.

## Author direction

- Booter carries supplies and equipment, never resource items.
- Six equipment slots accept only their designated equipment: dust canister, algae canister, grappling hook, cloak, jacket and mining/excavation tool.
- Start the excavation slot with a simple combined digging shovel/axe. Its working label is **Digging tool**; no final name, combat capability or material progression is assumed.
- Algae supports eating, hydration and healing. The author corrected the mention of sleep: it does not directly restore sleep or Reserve. Rest and sleep remain separate camp actions.
- Work in the existing Camp Loop scene. Audit and brainstorming do not authorize implementation.

## Current inventory audit

These are source and saved-scene findings, not a live gameplay acceptance report. Earlier October 11 audits describe some problems that have since been repaired; the current implementation receipt and live code control implementation fact.

| Area | Verified current state | Redesign implication |
| --- | --- | --- |
| Booter storage | The saved Camp Loop player has 16 generic stack slots, although the component default is eight. Starter contents are one micro dust harvester and one popup camp. | Changing a default or relabeling six grid cells would not establish dedicated equipment ownership. |
| Resource separation | Booter policy rejects resources. Ore and dust are authored Legger-only; resource-definition validation enforces that restriction. | Preserve these working rules at every acquisition, transfer and restore boundary. |
| Item classification | The shared catalog contains four definitions: Ironstone Ore, Airborne Dust, Micro Dust Harvester and Popup Camp. `IsResource` depends on the exact category string `Resource`. There is no equipment-slot compatibility field. | Equipment and supplies need explicit eligibility; display labels alone cannot enforce the new spaces. |
| Dust canister | A generic tool stack currently permits four harvesters. Deployment removes one tool from Booter's inventory. Filled-device recovery jointly returns the tool to Booter and collected dust to accessible Legger cargo. | A single dedicated slot needs an equipped/deployed lifecycle and cannot remain a stack of interchangeable fitted devices. |
| Algae canister | A separate player survival component owns precise fill and passive growth. Drink, Eat and Treat draw from one reserve. It is not an item in the generic backpack. | The slot should expose that same state, without creating another reserve or refilling it on equip. |
| Cloak | Camp settings and animation presentation supply worn-cloak and blanket assets. The current catalog has no cloak item or dedicated equipment authority. | Worn and blanket presentations must become views of one owned cloak. |
| Jacket, grapple and digging tool | No matching item definitions or dedicated runtime equipment systems were found in the active catalog/runtime audit. Existing traversal and gathering are separate systems. | These are new equipment concepts, not already functioning inventory slots. Clothing appearance or animation props do not establish item ownership. |
| Legger cargo | Twelve mounts; authored load bands are 60 comfortable, 80 recommended and 92 hard mass. Access range is 3.2 m, guarded by active state, same scene, range and line of sight. | Keep finite bulk storage and physical access. Equipment spares on the Legger remain cargo until explicitly fitted to Booter. |
| Resource cache | The existing local cache has 24 resource-only slots. Opening it replaces the left pane; unloading transfers the greatest fitting resource quantities from cargo. | Preserve it as local resource storage, separate from Booter's kit. It is not a general equipment locker or globally shared stockpile. |
| UI and controls | Booter and cargo use separate four-column grids with selected-item details. Explicit navigation, owner switching, quick transfer, layered cancel, held-input guards and notices already exist. | Preserve useful interaction behavior while replacing the player's grid with meaningful spaces. |
| Transactions | Selected-stack quick transfers and bulk unloading use prepared joint mutations. Filled canister pickup also prepares both destinations. | Extend this foundation to equipment changes; do not reintroduce sequential remove/add with visible intermediate loss. |
| Packing | Auto-Pack organizes whole cargo stacks. Per-mount maximum mass is declared but not enforced; load affects movement, while balance is not a demonstrated movement mechanic. | Keep load feedback honest. A visual redesign must not introduce new packing penalties or imply that balance gameplay works. |
| Camp Loop lifecycle | The saved scene contains an animation-playground session marker. `IsSessionOnly` prevents checkpoint startup, restore and writes. Current player-reset registration cancels legacy actions and restores vitals/input; it does not establish a complete inventory/algae/world reset. | Preserve session-only trials. Future loadout trials need a coordinated reset rather than an assumption that the existing button restores every owned item. |

## Proposed inventory spaces

### Booter equipment

Use six permanently visible, labelled slots in a two-column arrangement. Every slot contains at most one equipment instance. An empty slot shows its intended equipment silhouette and label, making missing preparation visible without requiring inspection.

| Slot | Accepted equipment | What the slot should show | Proposed interaction |
| --- | --- | --- | --- |
| Dust canister | A compatible dust-harvesting canister | Fitted, deployed, or unavailable; deployed-device fill when known | Select its existing deployment action; inspect deployment location/state. |
| Algae canister | A personal living-algae canister | Remaining algae and capacity | Inspect Eat, Drink and Treat costs. Keep existing gameplay controls and action gating. |
| Grappling hook | A compatible terrain-navigation grapple | Fitted or missing; unavailable reason | Expose traversal capability after a separate grapple design is accepted. No rope length, ammunition, fuel or cooldown is invented here. |
| Cloak | A compatible cloak | Worn, in camp use, or missing | Treat cloak-to-blanket presentation as using the same equipment. |
| Jacket | A compatible jacket | Equipped item and supported properties | Keep separate from cloak. Protection, temperature and durability effects require their own design. |
| Digging tool | The starter shovel/axe or a compatible future excavation tool | Equipped tool and supported gathering capability | Later gathering can check this slot before beginning an excavation action. Do not claim that current mining already requires it. |

Slot compatibility should be explicit, including validation during restore and direct acquisition. A jacket cannot fill the cloak slot; a grapple cannot fill the digging slot. Future variants may fit the same slot without granting new general-purpose slots. Do not require all six to be filled merely to open inventory or move; missing gear should explain which supported action is unavailable.

### Booter supplies

Start brainstorming with **four supply pockets**, separate from the six equipment slots. This is a capacity experiment, not an accepted number. Pockets accept explicitly approved field supplies and retain each item's authored stack limit. They reject raw ore, loose dust, scrap and other resources, and cannot serve as extra equipment slots.

The current catalog does not yet offer ordinary supply items. Use empty labelled pockets in the design rather than inventing a roster of canonical food, ammunition or medicine. Personal algae is already held in its dedicated canister; displaying its contents must not also charge a pocket or create an algae stack.

The existing popup camp needs an explicit home before replacing the backpack. Recommended first option: permit one packed popup camp as a designated field-supply bundle in a pocket, preserving Booter's existing independent camp ownership. Alternate option: carry it as Legger cargo, which deliberately changes when Booter can camp while separated. Do not silently adopt that alternate or add a seventh equipment slot.

For the first experiment, spare jackets, cloaks, grapples and excavation tools stay in Legger cargo. This keeps the supply area legible and makes choosing the fitted loadout meaningful. If the author later wants player-carried spare equipment, define that capacity deliberately rather than allowing every generic item into a pocket.

### Legger cargo and local cache

Give the Legger a cargo manifest organized for browsing, with resource and spare-gear filters that view one finite inventory. Filters must not manufacture independent capacity. Display occupied mounts, actual mass against the 92 hard limit, and the supported load band. Retain the current capacities during the initial layout experiment; changing them is a separate balance decision.

Show spare equipment and supplies only when their carrier policy permits cargo storage. A physical transfer can move a permitted supply to a pocket or fit a compatible spare to Booter. Resources have no transfer-to-Booter action. Optional packing stays behind a secondary command, with outcomes limited to what it actually does.

At a local cache, present **Legger Cargo ↔ Resource Cache** as the transfer workspace, retaining bulk unloading and fitting quantities. Booter's loadout remains reachable through its own view rather than being relabelled as a cache. Both physical destinations must be accessible before unloading. Distant cargo can remain inspectable with a clear read-only state; inspection grants no transfer or equip authority.

## Proposed screen layout and controls

The main view uses Booter on the left and the Legger on the right, with a shared item-detail area below. On smaller displays, show one owner at a time and preserve focus separately.

| Booter column | Legger column |
| --- | --- |
| Dust canister · Algae canister | Cargo access status |
| Grappling hook · Digging tool | Resource and spare-gear filters |
| Cloak · Jacket | Cargo stacks or manifest rows |
| Four supply pockets | Occupied mounts · Mass · Load band |
| Equipment or supply actions | Transfer or organize actions |

Use icons with short text labels, a clear focus outline and textual unavailable reasons. Algae fill belongs on the canister tile; dust-device state belongs on the dust tile. Display only supported properties, without adding fictitious durability meters or a balance warning.

Preserve Tab/Y opening and direct close; D-pad/left stick navigation; LB/RB owner switching; A confirm; B cancel/back. Navigation links must follow the actual new regions instead of retaining hardcoded four-column movement. Keep one modal input owner and the existing neutral/release barrier when returning to gameplay.

Change the meaning of A on equipment to open a small action list, rather than selecting a stack for generic swapping. X remains quick transfer for supplies and cargo. Fitting a spare opens **Equip to [named slot]**, showing the outgoing piece's destination. B cancels that operation before closing the screen. Radial assignments reference the equipped capability and never count as another storage location.

Inventory inspection should not bypass current algae-use, gathering, camp or traversal restrictions. For example, inspecting the algae tile may show doses without allowing consumption while the Inventory input mode is active. A future Use action must close/handoff safely and revalidate in gameplay; adding it is not a consequence of the layout alone.

## Equipment ownership and action rules

An equipment instance occupies exactly one authoritative location: fitted to Booter, stored in accessible cargo, or deployed in the world. A reserved slot can show deployed equipment without duplicating the instance or making it usable in two places.

- **Dust:** preserve the existing deployable harvesting mechanic for the first redesign. Reserve its slot while deployed. Recover the same device into that slot and deliver collected raw dust directly to physically accessible cargo in one operation. If cargo is absent/full or the return slot is unavailable, keep the world device and dust unchanged. Booter does not carry raw dust inside the returned tool as an exception. If the author instead intends a wearable active collector, that is a distinct harvesting design decision.
- **Algae:** living algae inside its fitted survival canister is a personal supply, distinct from gathered resource stacks. Preserve fill and passive growth through view rebuilds and equipment changes. The current attached-state implementation needs deliberate ownership handling before removable or swappable canisters work. Until that handling exists, the new slot may display the attachment without offering removal.
- **Cloak:** camp use changes presentation/state, not quantity. Keep the fitted cloak reserved while used as a blanket and prevent transfer or replacement during that use. Whether sleep requires a cloak, or what happens without one, remains a separate design choice.
- **Swaps:** validate slot compatibility, current action restrictions, both carrier capacities, mass and physical access before changing either owner. A full destination must not delete the outgoing gear. Commit the whole swap before notifying observers.
- **Gathering:** preserve exactly-once resource delivery to the Legger. A later digging-tool prerequisite must leave deposits untouched when gear or cargo is unavailable. Ghost-machine salvage is still a proposed feature, not an inventory item roster already present in Camp Loop.

## World identity and persistence boundaries

**Deterministic world identity:** equipment belongs to Booter or the Legger, independently of terrain generation. Preserve the authored world's existing identity and deployed-device identities; the inventory redesign does not generate terrain or select a new world seed.

**Streaming and reload:** terrain in this lane remains fixed and always loaded, so terrain chunk streaming is not applicable. UI reconstruction, scene reload and future visual culling must not refill canisters, duplicate fitted gear or relocate cargo. Logical ownership survives view changes.

**Stable object identity:** give each fitted equipment instance stable identity and each slot a stable purpose. A deployed dust device needs an explicit relationship to its reserved slot. Reusing an item-definition ID as every equipment instance's identity would make spare canisters and swaps ambiguous.

**Authored constraints:** preserve the existing camp, terrain, resource IDs and local cache. No shared production item asset, prefab or survival setting is changed by this proposal. A later trial should use private authoring where shared changes would unintentionally alter Greater Wasteland.

**Persisted runtime deltas:** keep the marked Camp Loop playground session-only. Register future equipment, supplies, algae and deployed-device trial reset together, including rewards that must be removed when a source is restored. For later production adoption, version equipment ownership and migrate the existing 16-slot kit explicitly. Preserve overflow and unknown items for recovery; never truncate them, auto-transfer them to a distant Legger, refill algae or reactivate the retired generated-world save service. Existing kit snapshots alone do not encode the six new slot purposes.

## Design choices and future proof

The next design discussion should settle the four-pocket experiment, popup-camp placement and whether the dust canister remains deployed harvesting equipment. Grapple operation, clothing effects, digging capabilities and loss/durability are separate feature designs; they need not be invented to approve the inventory spaces.

If implementation is later authorized, its focused proof should cover wrong-slot rejection, resource rejection in every Booter space, nonduplicating equip/deploy/return, unchanged algae fill, cloak use, full-cargo swap failure, access lost during a transfer, exact quantity conservation, controller navigation and layered cancel, coordinated trial reset, and safe migration of an overfull old backpack. Hands-on inventory feel remains author-owned.

## Source references

- [Camp Loop scene](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity) and [original scene receipt](../../Evidence/Gameplay/CAMP_LOOP_TESTING_SCENE_2026-10-13.md).
- [Player inventory](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DPlayerInventory.cs), [carrier policy](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryPolicy.cs) and [item definition](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Items/TopDown3DItemDefinition.cs).
- [Catalog](../../../Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset), [packing settings](../../../Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset) and [resource cache prefab](../../../Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandResourceCache.prefab).
- [Inventory UI](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryCanvas.cs), [UI commands](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.Commands.cs), [transfer commands](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryTransferService.Commands.cs) and [cargo access](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DCargoAccess.cs).
- [Player actions](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs) and [placed harvester state](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Resources/TopDown3DPlacedHarvesterState.cs).
- [Survival standard](SURVIVAL_SYSTEM_STANDARD.md), [algae consumption](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DAlgaeConsumptionController.cs) and [camp controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Camping/TopDown3DCampController.cs).
- [Playground session policy](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.cs), [player reset registration](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs) and [checkpoint service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs).
- [October 11 inventory implementation receipt](../../Evidence/Gameplay/INVENTORY_LOOP_IMPLEMENTATION_2026-10-11.md), [earlier inventory audit](BOOTER_LEGGER_INVENTORY_AUDIT_2026-10-11.md) and [proposed ghost-machine salvage](CAMP_LOOP_GHOST_MACHINE_SALVAGE_PLAN_2026-10-14.md).

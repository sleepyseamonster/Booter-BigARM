# Inventory and Radial Menu System

Status: design and implementation sequence for Greater Wasteland, 2026-10-06. The user confirmed a player-customizable menu hub opened by holding left bumper or a keyboard equivalent: north opens player inventory, south opens the Legger's inventory, and west selects the dust canister for placement. Other defaults below are proposals; they do not establish new item economy, companion abilities, or world canon.

The radial menu provides entry points into inventory and tool selection. The inventory remains the place to inspect, organize, transfer, and assign items. Both use the same item and command authorities so a shortcut never becomes a second inventory or a way around physical reach, capacity, or action constraints.

## Existing implementation and production gaps

The [inventory specification](./INVENTORY_AND_ITEM_MANAGEMENT_SYSTEM.md) supplies the small field kit, finite companion cargo, carry preferences, and optional Auto-Pack model. Live production code already supplies item IDs, categories, icons, stack limits, mass, inventory snapshots, move/swap/merge, exact add/remove, maximum/exact transfer, and companion packing.

The following facts were inspected in the Windows checkout:

| Surface | Current behavior | Work needed |
| --- | --- | --- |
| Input | `System/ToggleInventory` uses Tab and gamepad North. `Gameplay/RecallBigArm` uses F1 and left bumper. | Give the wheel exclusive ownership of left bumper and an explicit keyboard binding. Preserve recall through a wheel command and keyboard shortcut. |
| Inventory screen | Select a source, then a destination; displays both owners when cargo exists. | Add explicit contextual operations, quantity selection, rejection feedback, and radial assignment. |
| Cargo access | The cargo component exposes a physical access radius. Ordinary inventory slot handlers and Auto-Pack do not recheck it. | Recheck access at every operation and refresh availability while the screen is open. |
| Cargo selection | Source selection mixes owner and index checks; cargo details read from Booter's slots. | Use owner plus slot as the selection identity, show the selected owner's definition and quantity, and support deliberate cargo rearrangement. |
| Transfer | The service removes by item ID and then adds to the other owner; failure restores by item ID. | Stage both owners before publishing a transfer; preserve the chosen source stack and layout on rejection. |
| Dust harvesting | Greater Wasteland's player action component has unassigned `harvesterState` and `proceduralWorld`. Placement requires a loaded procedural chunk. | Adapt placement to authored ground and install a world-owned device registry without enabling generation or streaming. |
| Ironstone | Runtime resource systems exist for the generated reference world. The authored production scene does not serialize Ironstone/resource authorities. | Treat scene resource availability as a separate integration gate; do not invent or scatter new nodes as part of the wheel. |

Sources: [input asset](../Assets/_Project/Settings/Input/InputSystem_Actions.inputactions), [input router](../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs), [inventory controller](../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs), [transfer service](../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryTransferService.cs), [player action controller](../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs), and [Greater Wasteland](../Assets/_Project/Scenes/TopDown3D/GreaterWasteland.unity).

Historical Ironstone and dust documents describe the generated prototype. Their generation, scene-wiring, and save statements do not prove those features work in Greater Wasteland. Root [AGENTS.md](../AGENTS.md) controls the authored-world production boundary.

## Proposed controls and selection

Start with four cardinal sectors so the confirmed menu entries have large, consistent targets. Additional player-named pages and an optional eight-sector layout are proposed extensions. Keep page and sector limits in validated configuration rather than spreading constants across views.

| Starting direction | Confirmed entry | Result after selection |
| --- | --- | --- |
| North | Player Inventory | Close the radial and open Booter's inventory with Booter's pane selected. |
| South | Legger Inventory | Close the radial and open the Legger's inventory with his pane selected, subject to physical cargo access. |
| West | Dust Canister | Close the radial, select the carried canister, and enter placement preview. Placement is confirmed separately. |
| East | Unassigned | No action. Its purpose has not been specified. |

Preserve these positions in the initial default layout. The user's broader customization request allows editing through the customization screen; whether any entries should remain permanently fixed is still an open product choice.

| Intent | Controller default | Keyboard and mouse default |
| --- | --- | --- |
| Open wheel | Hold left bumper | Hold Q |
| Select wedge | Right stick | Mouse position relative to wheel center |
| Activate | Release opener with a valid selection | Release Q with a valid selection |
| Cancel | East while holding opener | Escape while holding Q |
| Previous or next page | D-pad left or right | Mouse wheel; arrow keys as fallback |
| Open inventory directly | North | Tab |
| Navigate without a mouse | D-pad up/down cycles wedges | Arrow up/down cycles wedges |

Q is the proposed default because Tab already opens inventory. Check every binding in Gameplay, UI, and System before adding Q. The opener and page bindings support scheme-specific rebinding and persisted binding overrides. Display prompts from current bindings and the router's active device.

Open immediately on press; do not add a delay to the normal hold interaction. A press and release with no selection does nothing. Start each opening and page change with no selection. A configurable center dead zone cancels a selection; returning the stick or pointer to the center clears it. Give wedge boundaries a small configurable hysteresis to prevent highlight flicker. Releasing on an empty or unavailable wedge performs no action and shows its reason.

On keyboard opening, center the released pointer on the wheel once. Absolute pointer position selects the wedge; pointer delta must not rotate the camera. Arrow selection uses the same selected-wedge state as pointer/stick selection. Use the most recent meaningful input source when devices overlap; stale stick drift must not override deliberate pointer or key input.

Cancel must suppress release activation until the opener has been released and pressed again. Losing focus, disconnecting the opening device, disabling the controller, changing scene, opening another modal screen, or entering a disabled mode also cancels without executing. A canceled hold must never reopen itself because a button remains down.

## Wheel contents and player customization

A wedge stores an assignment kind and stable target ID. Item shortcuts reference an item definition ID, never an inventory slot index. Command shortcuts reference a registered command ID. Each opening resolves current quantities, action availability, and icons; reordering stacks leaves assignments intact.

An item shortcut invokes an explicitly supported item action, such as deploying a carried dust canister. Raw Ironstone and airborne dust remain inspectable resources; assigning them does not invent a generic Use operation. Show non-actionable resources in the customization browser as unavailable for quick use, with an explanation.

Proposed initial command registry:

| Command | Meaning | Availability |
| --- | --- | --- |
| Player Inventory | Open inventory after closing the wheel with Booter's pane selected | Local inventory exists |
| Recall Legger | Request physical regroup through the existing companion command | Companion command service is available; never teleport |
| Legger Inventory | Open the packing screen with the Legger's cargo pane selected | Legger is within the existing cargo access radius |
| Auto-Pack Legger Cargo | Repack current companion cargo | Legger is within access radius; packing is legal |
| Dust Canister | Select the canister and begin placement preview | Player owns a canister and authored-world placement is integrated |
| Pick Up Dust Canister | Begin pickup of the current reachable deployed canister | Target, reach, and combined reward capacity pass validation |

The north, south, and west entries are the default hub, not a contextually rearranged item wheel. Recall, Auto-Pack, and pickup are optional assignable commands rather than additional default entries. Do not seed enabled dust commands before their authored-scene integration passes. Placement remains a deliberate preview and confirmation action; releasing the wheel does not automatically commit a placement. Existing direct deployment and pickup bindings may remain during the transition. The west entry must explain whether the canister is missing or placement integration is unavailable.

Players customize from a dedicated inventory tab or button, not while trying to execute a quick action. The customization screen provides:

- Choose a page and wedge, then assign from Items or Commands.
- Rename, create, duplicate, reorder, and delete pages; retain at least one page.
- Move or swap assignments, clear a wedge, and allow the same shortcut on several pages.
- Choose the default page and whether reopening remembers the last used page.
- Set wheel size, opacity, text scale, selection dead zone, and release versus explicit-confirm activation.
- Restore defaults through a visible confirmation because it replaces the player's layout.

Use readable icons, quantity badges, selected labels, and a concise unavailable reason in the wheel center. Show owner availability separately: carrying an item in the Legger's cargo does not make it locally usable by Booter. The first slice resolves usable items from Booter's kit; future cargo use must obey item policy and physical access.

Explicit-confirm mode invokes through South or left click and closes once. Releasing without confirmation only closes. It must share validation and exactly-once execution with release mode.

Nested wheels, macros, automatic multi-action sequences, arbitrary player-authored commands, and automatic item consumption are outside the first implementation. Expand the command registry as actual gameplay abilities become available.

## Inventory improvements

Keep routine packing fast. Whole-stack transfer and Auto-Pack stay the default path. Add optional quantity controls where the player needs to keep some dust or ore behind, rather than requiring a quantity dialog for every move.

| Operation | Required behavior |
| --- | --- |
| Inspect | Show correct owner, description, quantity, stack limit, unit and stack mass, category, and supported actions. |
| Move or swap | Select owner plus source slot, then a destination; same-owner moves preserve exact slot intent. |
| Merge | Merge matching items to the stack limit, leaving the remainder in the source. Report a full destination. |
| Split | Pick a positive quantity smaller than the source stack and an empty same-owner slot. Preserve total quantity and mass. |
| Quick Transfer | Move the greatest valid quantity from the selected stack to the nearby other owner. Report the moved quantity and any remainder. |
| Quantity Transfer | Transfer an explicitly selected amount atomically, or reject without mutation. |
| Auto-Pack | Repack the Legger's cargo without changing owner or total quantity. Require current proximity. |
| Assign to Radial | Open the customization picker for a supported item action. Sorting later preserves the assignment. |

All operations report their actual result. Keep selection on a failed operation so the player can recover; clear it if its source has disappeared. Read-only cargo summaries may remain visible when separated, but transfer, rearrangement, and Auto-Pack are disabled and explain that the Legger must be nearby. Revalidate proximity at the moment of commit even if the button was enabled when drawn.

Do not add discard or ground drop until persistent authored-world pickups exist. No silent deletion, invisible transfer, or item creation should be introduced to make a menu option appear complete.

## Runtime responsibilities

Keep implementation in `BooterBigArm.TopDown3D.Runtime`, under the established runtime folders. Reuse the scene's single EventSystem and uGUI/Input System path.

- **Input router:** owns the hold action, radial selection/page/confirm input, prompt device, cursor, and a distinct Radial mode. The hold/release action belongs to a map that remains enabled throughout the modal transition; disabling Gameplay must not create a false release.
- **Radial layout state:** validates pages, assignments, and versioned preference snapshots. It has no world mutation authority.
- **Command registry:** resolves labels, icons, availability, and a single execution adapter per stable command ID. Availability and execution share the same gameplay checks.
- **Radial controller:** owns open/select/cancel/commit lifecycle and dispatches one command. It closes and leaves Radial mode before executing so opening inventory or beginning placement can establish its own state.
- **Radial view:** draws pages, selection, quantities, and unavailable reasons. It does not consume items or invoke input actions directly.
- **Inventory operation service:** validates source owner/slot, destination, policy, range, amount, and capacity; prepares mutations before publishing state changes.
- **Preferences persistence:** stores radial layouts and binding overrides separately from world saves, with validated defaults and recovery from corrupt data.

Gameplay movement, camera orbit, sprint, interaction, recall, and harvesting input are suppressed while the wheel is open. Clear stale intent on both transitions. Cancel an incomplete gather without issuing a reward; reject opening during a committed action phase that cannot safely cancel. Modal routing must define this phase explicitly rather than rely on animation timing.

Proposed first-slice time policy matches inventory: no global time-scale change. Dust collection and companion simulation continue. If slower wheel time is desired later, treat it as an explicit simulation policy with collection-clock and action-timing tests.

## World identity and persistence

| Concern | Contract |
| --- | --- |
| Deterministic world identity | Radial preferences belong to the player profile and do not change terrain seeds or world manifests. Commands resolve against the active world at execution. |
| Streaming and reload | Greater Wasteland is fixed and always loaded. No streaming is implemented. Menu state may not retain transient node/view references across scene changes; future streaming can invalidate a target cleanly. |
| Stable object identity | Item assignments use catalog IDs; command assignments use registry IDs. A deployed canister uses the device state's persistent instance ID. Authored Ironstone nodes need authored stable IDs before persistence integration. |
| Authored constraints | Commands reuse reach, obstruction, slope, clearance, authored no-placement regions, carry policy, and motor action constraints. Opening a wheel does not waive them. |
| Runtime deltas | Harvest, transfer, and pickup mutate existing state owners transactionally. The wheel stores no quantities, depletion, placement, or companion-position records. |
| Saved preferences | Use a versioned layout DTO with bounded pages and assignments. Validate before replacement; preserve unknown IDs as unavailable assignments so upgrades do not silently rearrange a player's layout. |
| Saved game state | Do not copy the generated prototype's save service into Greater Wasteland. Authored inventory/device persistence requires a coordinated save owner with an explicit authored-world identity. |

Save preference edits on Apply, using a temporary file and safe replacement. A failed write retains the previous saved layout and reports that the change is only active for the session. Binding overrides load before prompt resolution. Validate malformed IDs, invalid enum values, duplicate page identities, unsupported versions, excessive counts, and non-finite tuning values.

## Implementation sequence and proof

1. **Inventory correctness and feedback.** Repair owner-aware selection and details, enforce live cargo access, and make selected-stack transfers atomic. Focused EditMode proof covers same-index slots across owners, partial/full transfer, rejected policy/capacity/range, unchanged rejected layouts, and change notifications after both owners are coherent.
2. **Radial state and input.** Add the layout model, command registry, Radial mode, hold selection, cancel, and page lifecycle. Test binding conflicts, false release on map transitions, one execution per hold, center cancellation, boundary selection, page reset, focus loss, device removal, and stale intent.
3. **Production menu hub.** Install the four-sector wheel through the existing scene UI lifecycle. North selects the player inventory pane, south selects the physically accessible Legger pane, and west selects canister placement. Add an owner-aware inventory opening API so south does not merely open Booter's details. Until authored placement integration passes, west remains visible with a clear unavailable reason. Validate Greater Wasteland wiring and exactly one EventSystem. Test cardinal mapping, owner focus, inventory/wheel handoffs, and unavailable command feedback.
4. **Player customization and preferences.** Add the controller-friendly editor and versioned preference storage, then optional split and quantity transfer operations. Test assignment continuity through sorting/depletion, page edits, round trips, bad data, and failed writes.
5. **Authored harvesting integration.** Remove placement's procedural loading dependency through an authored-ground adapter and install the device state/presentation in Greater Wasteland. Prove placement clearance, atomic pickup, partial fill, full inventories, stable device IDs, and the explicitly selected authored-save contract. Keep procedural generation disabled. Integrate authored Ironstone separately without creating unapproved resource placements.

Each batch requires current Unity compilation and its directly affected EditMode tests. Scene and serialized changes also require the applicable non-mutating production validator. Preserve existing metadata and package versions. Run background-safe validation only; do not focus Unity or run gameplay smoke tests.

Player acceptance covers right-stick comfort, mouse reach, page switching, readability, editing speed, and accidental activation. These checks are user-owned and remain distinct from compiler and automated contract proof.

## First release acceptance

A player can hold left bumper or the configured keyboard key, choose north for player inventory, south for the Legger's inventory, or west to select dust-canister placement, and release to enter that system once. Selecting west does not place or consume the canister. They can cancel without side effects, customize entries from inventory, and recover their saved layout after restarting. Sorting or consuming an item does not move its shortcut. The wheel shows why an action is unavailable. The Legger's real position and every inventory quantity remain authoritative. Greater Wasteland runs without introducing generation, streaming, or a second save authority.

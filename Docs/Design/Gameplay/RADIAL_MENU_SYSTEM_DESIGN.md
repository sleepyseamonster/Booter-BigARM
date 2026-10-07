# Radial Menu System Design

Design specification for Greater Wasteland, 2026-10-06. This specification turns the [research and controller audit](../../Research/UI/RADIAL_MENU_UI_CONTROLLER_RESEARCH.md) into one implementation design. The [inventory and radial plan](./INVENTORY_AND_RADIAL_MENU_PLAN.md) remains the scope and integration reference. Runtime implementation is a subsequent step; the accompanying interactive preview demonstrates the design only.

The [polish audit](./RADIAL_MENU_POLISH_AUDIT.md) refines the ring's selection treatment, compact labels, single feedback location, focus continuity and protection of customization drafts. Those refinements supersede earlier preview presentation where they differ, while retaining this system's command and world contracts.

The [first runtime integration receipt](../../Evidence/UI/RADIAL_MENU_IMPLEMENTATION_2026-10-06.md) records the implemented subset, corrected constant-width annular geometry, production controls and validation. Expansion features below remain design targets where the receipt identifies them as deferred.

## Player experience

Hold left bumper, aim the right stick toward a menu entry, and release the bumper to execute it. Keyboard and mouse use hold Q, pointer selection, and Q release. Returning to the center clears selection. East/Escape cancels. Opening immediately shows the available directions; it does not wait for a timed Hold interaction.

The initial page is named Field and has four permanent angular positions. Players may edit their assignments, but item availability never rearranges the menu.

| Direction | Initial assignment | Activation result |
| --- | --- | --- |
| North | Player Inventory | Open Booter's field kit, with his pane focused. |
| South | Call Legger Over | Close the radial and request a sprinting physical approach to Booter through legal pathfinding. Gameplay continues. |
| West | Dust Canister | Select a carried canister and enter placement preview. A separate confirmation places it. |
| East | Empty | Execute nothing. This sector is available for a player assignment. |

The physical gamepad West button remains Interact, with keyboard E as its equivalent. Targeting the nearby Legger and pressing Interact opens his cargo. The radial west direction selects the canister; it does not change that gameplay button. Neither calling the Legger nor his arrival opens inventory automatically.

Q, release activation, page limits, appearance and tuning values below are chosen design defaults, not additional user-confirmed requirements. They remain configurable. Confirmed cardinal meanings and physical Legger behavior remain controlling.

## Hub layout and visual language

Use one compass ring centered within the screen safe area. Work in the existing canvas's 1920 by 1080 reference units. Default ring diameter is 420, inner hole diameter 120, icon diameter 40, and icon centers lie 150 units from the origin. User scale ranges from 0.85 to 1.35; clamp the whole ring and its text to the safe area. At constrained resolutions, prioritize labels and usable sectors over decoration.

Four equal sectors have axes at 0, 90, 180 and 270 degrees clockwise from north. Render a narrow gap between sectors, but include it in the angular selection region so visual seams do not become dead strips. A thin outer perimeter and highlighted arc communicate the selected region. Label text remains upright. Optional eight-sector pages retain cardinal axes and add diagonals.

| Token | Default | Purpose |
| --- | --- | --- |
| Local backing | `#1B1D1C` | Stable contrast over bright terrain; opacity setting applies to decoration, not text backing. |
| Sector fill | `#303431` | Subtle separation from the center and surrounding game. |
| Primary text | `#F2EADD` | Warm readable labels, including unavailable entries. |
| Secondary text | `#C5BFB2` | Binding hints and short contextual detail. |
| Selected arc | `#E8B86D` | Warm selection accent, paired with shape and label feedback. |
| Unavailable marker | `#D7A19A` | Additional status cue; a text reason is mandatory. |
| Default typography | 24-unit entry labels; 20-unit hints; 28-unit selected title | Scales with UI text preferences, separately from wheel diameter. |

These are original game UI tokens, not copied Wilds assets. Use the project's canister/inventory icons and an original Legger silhouette in production. The preview uses generic symbols. Render an item quantity only on item actions; commands do not have invented count badges.

The page name appears above the ring. Entry icon and short label sit in each sector. The center shows `Choose`, `Release`, or `Confirm`, plus a small selection direction cue. A fixed detail area below the ring shows the selected entry's full name and its consequence or unavailable reason. This moves longer text out of the small center hole and avoids wrapping or resizing the wheel when labels change.

Footer hints show the actual opener, selection, confirm/cancel and page bindings. Page navigation appears only if there is more than one page. The world remains visible around the local ring; optional dim is limited to 15 percent. No full-screen blur or decorative moving dust is needed. Opening/closing uses a proposed 100 ms opacity transition; selection is responsive from the first frame. Reduced-motion mode changes immediately.

## Visible states

| State | Presentation | Command behavior |
| --- | --- | --- |
| Neutral | All entries readable; center Choose | Release closes without execution. |
| Available selection | Selected arc, icon emphasis, center Release/Confirm, detail consequence | Revalidate and execute once. |
| Unavailable selection | Selected arc remains visible; unavailable marker and reason | Close on commit with failure feedback; no mutation. |
| Empty selection | Empty label; detail No action assigned | Close without execution. |
| Canceled hold | Hide radial; suppress new opening until opener returns up | Subsequent release does nothing. |
| Inventory handoff | Radial closes before inventory opens with Booter's pane | Preserve exactly one modal input owner. |
| Callover handoff | Return to gameplay; show Legger approaching | Existing companion simulation moves physically. |
| Placement handoff | Return to gameplay; show canister ground preview | Existing action authority validates and later commits placement. |

Examples of reasons are `No canister carried`, `Placement unavailable here`, `Legger unavailable`, and `Action already in progress`. Distinguish a missing service from a temporarily invalid action. Never hide or move a default sector because it is unusable.

## Controls and input modes

| Action | Controller | Keyboard and mouse |
| --- | --- | --- |
| Open | Hold LB | Hold Q |
| Analog selection | Right stick | Pointer position relative to origin |
| Digital selection | D-pad selects cardinal direction | Arrow keys select cardinal direction |
| Confirm in explicit mode | South | Enter or left click |
| Cancel | East | Escape |
| Next page | RB, cycles back to first | PageDown or mouse wheel down |
| Previous page | Optional player binding | PageUp or mouse wheel up |
| Gameplay interaction after closing | West | E |

Accessible mode uses tap-to-open, digital selection, and explicit confirmation. Tapping the opener again closes without execution. The editor uses ordinary sequential navigation and Submit; it never requires a held modifier. Bindings are editable by scheme, and prompts use the router's current device and binding overrides.

Add `System/OpenRadial` and a dedicated Radial map containing selection, pointer, digital selection, confirm, cancel and paging actions. Gameplay, Radial, and UI are mutually exclusive; System remains enabled through those transitions. Remove left bumper from the gameplay recall binding, retain F1, and route radial callover to the same companion authority. Gate System inventory toggling while Radial owns input. South, East, D-pad and right stick cannot reach gameplay handlers while Radial is active.

Radial disables Booter's movement, camera orbit and other action input, clears stale intent, and leaves global simulation time unchanged. The companion and dust collection continue. Do not implement moving while the wheel is open without revisiting this modal policy.

## Selection and session contract

One session stores its initiating device, session ID, opener state, page ID, selected sector ID, active selection source and consumed flag. It stores no quantities or persistent world objects. Freeze command identity only at commit, then resolve availability against current gameplay state.

Stick selection begins above 0.30 processed magnitude and clears below 0.22. This is selection hysteresis after the existing Input System processor, not another StickDeadzone processor. Pointer neutral radius is 44 reference units. Exact angular ties use half-open sector intervals; an existing selection changes only after crossing a boundary by 5 degrees. Center clear always wins. Pointer movement beyond the ring retains its direction.

Open with no selection. A stick already displaced by camera input must return to neutral before it becomes a selector. A keyboard-opened pointer is centered once; that synthetic motion cannot claim selection ownership. Digital direction selects and stays selected until a deliberate new source or cancel replaces it. Page changes clear selection and require a fresh selection gesture.

Collect input intents for one input update and resolve cancel/lifecycle invalidation first, explicit confirmation second, opener release last. A genuine release must come from the opening device while focus and session remain valid. Disabling a map is lifecycle cancellation, never release activation. Explicit confirmation marks the session consumed before mode changes. After any consumption or cancellation, require opener-up before a new session.

On commit, freeze the assignment, mark consumed, hide view, restore Gameplay, revalidate the command and dispatch once. A mode change, focus loss, device removal, scene change or component disable cancels. A command rejection returns clear feedback; it is never queued to fire later. Simultaneous cancel plus release cancels regardless of callback order.

## System architecture

| Owner | Inputs and outputs | Required interface or invariant |
| --- | --- | --- |
| Input router | Input actions to semantic menu intents | Open, select, confirm, cancel and page intent; modal maps and initiating-device identity are authoritative. |
| Selection state | Direction/digital intent to sector selection | Pure geometry and source arbitration; testable without a scene. |
| Radial controller | Session plus selected assignment to command dispatch | One execution maximum per session; closes before handoff. |
| Canvas view | Resolved page, selected entry, hints and status | uGUI presentation only; no independent polling or item mutation. |
| Command registry | Stable command ID to presentation, availability and result | Availability and execution call the same authoritative gameplay checks. |
| Layout state | Validated pages and assignments | Item assignments reference definition IDs, never slot indexes. |
| Customization controller | Draft editing to validated Apply/Cancel | Edits do not execute gameplay commands. |
| Preference store | Versioned DTO to disk and validated in-memory layout | Atomic save replacement; no world state or duplicated quantities. |

Use the existing TopDown3D runtime assembly and uGUI EventSystem. New proposed source files are `UI/Radial/TopDown3DRadialMenuController.cs`, `TopDown3DRadialMenuCanvas.cs`, `TopDown3DRadialSelectionState.cs`, `TopDown3DRadialLayoutState.cs`, `TopDown3DRadialCommandRegistry.cs`, `TopDown3DRadialCustomizationController.cs`, and `TopDown3DRadialPreferenceStore.cs`. These are planned paths, not existing implementations. Reuse the existing scene UI installation lifecycle and one EventSystem; add no packages.

Default registry IDs are `inventory.player`, `companion.callover`, and `tool.dustcanister.select`. The canister entry references the existing item definition `tool.micro_dust_harvester`. Commands expose current label/icon, quantity if relevant, availability/reason, and a validated execution result. Additional supported commands can be registered without changing selection geometry.

## Gameplay adapters

**Player inventory:** expose an owner-aware opening method. North focuses Booter's kit. Physical Legger interaction focuses his cargo. Neither path accesses distant cargo; every transfer/repack operation revalidates physical range and policy before commit.

**Callover:** expose an explicit approach task on the existing companion authority. Target a legal stopping point within cargo access radius rather than the normal trailing point or Booter's occupied position. Keep urgent movement until reachable arrival or a blocked state; replan toward moving Booter at a bounded cadence. Speed respects acceleration, braking, slope, collision and cargo consequences. The existing 4.2 m trail distance, 7 m catch-up release and 3.2 m access radius cannot substitute for this task. Long-route limits produce honest blocked feedback, never teleportation.

**Dust canister:** expose a public validated BeginPlacement command that runs after Gameplay is restored. Validate ownership, action phase and authored-ground placement service. Preview and confirmation remain separate; opening or selecting a wheel never consumes the item. Greater Wasteland needs an authored-ground adapter and device registry before this entry is enabled. Do not activate procedural terrain or copy the generated prototype's save authority.

## Customization editor

Open `Customize Radial` from inventory. Show page controls at the top, a preview wheel with selectable sectors, an Items/Commands assignment browser, selected-assignment details, and Apply/Cancel at the bottom. At narrow widths, stack preview above browser. The preview never invokes an assignment; selecting a sector edits its content.

Default capacity is four named pages, each using four or eight stable directional sector IDs. Field starts with the confirmed assignments. Create, duplicate, rename, reorder and delete pages; retain at least one. Increasing from four to eight adds empty diagonals. Reducing eight to four asks for confirmation when populated diagonal assignments would be removed. Players can assign the same command to several positions, swap assignments, or clear them. Do not auto-fill empty positions.

Maintain a draft until Apply. Cancel discards all edits. Apply validates IDs, bounds and settings, attempts atomic preference persistence, then updates the live layout. If disk write fails, preserve the saved state and offer Retry or Keep for Session; do not label unsaved edits as saved. Restore Defaults affects the draft and requires confirmation; persistence still waits for Apply.

The editor's digital navigation graph links page controls to preview sectors, preview to browser, and browser to Apply/Cancel. Use explicit neighbors and remembered focus per pane; do not depend on spatial auto-navigation for a circular arrangement. Assignment pickers use Submit and Cancel, not drag-only interaction. Filters contain only real supported commands/items. Unknown saved commands retain their position and show Unavailable.

Settings include opener mode (hold/toggle), activation (release/explicit confirm), wheel scale, text scale, neutral thresholds, dim, reduced motion, sound/haptics, default page, remember-last-page, and scheme-specific bindings. Toggle mode requires explicit confirm; reject contradictory configurations instead of silently guessing.

## Persistence and world lifecycle

Version 1 preferences contain pages with stable IDs, display names, sector count and assignments; default/last page IDs; presentation/selection settings; and binding overrides. Validate bounds, duplicate IDs, unknown enums, finite numbers and supported schema before replacing live state. Recover corrupt data with default preferences and a visible message while retaining the invalid file for recovery. Store last-page only when remember-last-page is enabled.

Preferences belong to the player profile. They do not change deterministic terrain identity, resource generation or any carried quantity. Greater Wasteland is fixed and always loaded; no streaming is implemented. Future unload/scene transitions invalidate transient targets and cancel sessions, while stable item/command assignments remain. Authored constraints and persisted gameplay deltas stay with existing or explicitly approved gameplay/save authorities. A layout file never restores the Legger beside Booter or stores depleted nodes/devices independently.

## Acceptance and implementation gates

The design is ready when every default entry has a clear result, every unavailable state has a reason, cancellation has no side effects, and customization can be completed without a mouse. The interactive preview demonstrates holds, selection, cancel/release, three handoff results, and draft Apply/Cancel; it simulates gameplay outcomes and never controls Unity or changes real inventory.

Preview QA passed in headless Edge: keyboard Q hold/release, default inventory/callover/canister handoffs, cancellation suppressing release, missing-canister feedback, separate placement confirmation, draft discard and Apply, page creation and switching, no horizontal overflow at 320/736 pixels, and non-overlapping entry targets. Representative field/editor renders were visually reviewed. Prototype preference retention uses conversation state when supported; host icons and state delivery are not established by the standalone browser check. Final Unity graphics, controller input and on-disk preference behavior require implementation proof.

Implementation batches are input/session safety, selection/view, command adapters and callover, authored placement, then full customization/preferences. Required focused proof covers simultaneous events, map-disable cancellation, device/focus loss, held reopen prevention, owner-aware inventory focus, callover safe arrival/blocked routes, physical cargo access, canister preview versus commit, quantity preservation, and corrupt preference recovery. Scene edits require the applicable structural validator and GUID checks. Compilation and targeted EditMode tests are separate from hands-on controller comfort and visual acceptance. No gameplay smoke tests or foreground Unity interaction are part of this design task.

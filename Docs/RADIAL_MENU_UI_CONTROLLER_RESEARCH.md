# Radial Menu UI and Controller Design

Research and source audit completed 2026-10-06 for Greater Wasteland. This is a design handoff, not a runtime implementation or a gameplay acceptance record. Confirmed entries remain north Player Inventory, south Call Legger Over, west Dust Canister, and east unassigned. Calling closes the wheel and requests a physical sprinting approach; nearby gamepad West or keyboard E interaction opens cargo.

## Research findings

| Source | Supported finding | Application here |
| --- | --- | --- |
| [Capcom official Wilds manual](https://manual.capcom.com/mhwilds/) and its [English content data](https://manual.capcom.com/mhwilds/locale/data/en.json) | The Items and Radial Menu topic documents held L1, right-stick selection, stick release to use while L1 remains held, D-pad shortcut-loadout changes, and customization from the Start Menu. Keyboard shortcuts have a distinct numbered-key layout. | Borrow a held shortcut layer and deliberate customization. Our proposed bumper-release activation is an adaptation, not the same mechanic as Wilds. Keep keyboard input first-class. |
| [PlayStation Wilds starter guide](https://www.playstation.com/en-ie/games/monster-hunter-wilds/monster-hunter-wilds-starters-guide/) | The official platform guide describes customizable menus, up to eight menus and twelve objects per menu. | Treat those counts as Wilds' capacity, not a requirement for our game. Start with four sectors and one useful page; propose up to four pages and optional eight-sector pages. |
| [Kurtenbach, The Design and Evaluation of Marking Menus](https://www.research.autodesk.com/publications/the-design-and-evaluation-of-marking-menus/) | Directional gestures can bridge visible novice guidance and practiced selection. Results include advantages for four-, eight-, and twelve-item menus in the studied marking tasks. | Stable directions and visible labels let players learn the same motion they later perform quickly. This research does not prove gamepad error rates or a universal ideal sector count. |
| [Kurtenbach and Buxton, design refinements](https://www.research.autodesk.com/app/uploads/2023/03/some-design-refinements-and.pdf_recDHrrSOdirknTZs.pdf) | The authors discuss compass directions, symmetric labels, and hiding unnecessary information. | Use cardinal targets, upright labels, and one selected-detail area. Avoid nested wheels in the first release. |
| [Xbox Accessibility Guideline 107](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107) | Support remappable actions, digital and analog navigation, and alternatives to prolonged holds or simultaneous input. | Hold is the default. Add toggle-open plus explicit confirm, digital selection, and complete menu-action rebinding. |
| [Xbox Accessibility Guideline 102](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102) | Text and essential visual controls require adequate contrast, including unavailable states. | Use an opaque local backing, text/icon contrast checks, and status text in addition to color. Aim for 4.5:1 normal text and 3:1 large text/essential control boundaries. |
| [Unity Input System 1.19 actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Actions.html), [InputAction API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.InputAction.html), and [UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html) | Action callback order is unspecified when several actions occur in a frame. Disabling an in-progress action can invoke cancellation synchronously. uGUI input integrates through InputSystemUIInputModule. | Do not equate all canceled events with physical release. Preserve the opener through mode changes, resolve input precedence centrally, and reuse one EventSystem. |

The manual web app did not render through the text browsing tool. Its English JSON was retrieved directly from Capcom and inspected at `Controls & Screen Layout` / `Items & The Radial Menu/Shortcuts`, entries 10 and 13. This confirms documented controls, not current option-screen names or every post-launch configuration. No game session was played and no exact Wilds art, pixel geometry, or animation timing was measured. Third-party guides and player discussions were discovery leads; the conclusions above rely on primary sources.

## Recommended visual design

Use a compass ring near screen center, clamped inside the safe area. At the existing 1920 by 1080 reference resolution, begin with a 420-unit outer diameter, 120-unit center hole, and icon centers about 150 units from the origin. These are proposed tuning values. The menu never follows stick motion or rotates its sectors.

Four equal 90-degree sectors put their centers at north, east, south, and west. Empty east retains its space. Equal hit regions matter more than the precise arc artwork. In an optional eight-sector layout, keep the cardinal entries on their original axes and add diagonal assignments; switching layouts never redistributes items by quantity or availability.

Use charcoal surfaces, warm off-white labels, and a restrained amber selection treatment consistent with the existing survival setting. Selection adds a bright inner/outer arc, a small directional marker, and stronger icon/label weight. An unavailable entry remains readable with an unavailable marker and a specific center message; do not rely on a dim gray icon alone. Use original game-owned icons or neutral prototype symbols, not copied Monster Hunter assets.

Each entry has an upright icon and a short label. The center contains the selected action name and one line of consequence or rejection: `Open field kit`, `Sprint to Booter`, `Choose a placement point`, or `No canister carried`. Quantity badges appear only for item actions. A neutral center reads `Choose an action`. Page name sits above; page position and previous/next hints appear below only when multiple pages exist. Footer hints derive from the active bindings.

Keep the world visible with a local opaque menu backing and, optionally, a very light full-screen dim. Do not blur the whole game or reuse inventory's strong world dim. Proposed opening/closing animation is a brief opacity/scale transition under 120 ms, with hit testing active immediately; reduced-motion mode uses an immediate transition. Selection sound and optional light haptic feedback occur once on sector change, with player volume/haptic controls.

The conversation preview compares **Compass Ring**, with filled sector regions, and **Compact Compass**, with separated directional targets. Compass Ring is recommended because it communicates the angular hit areas more clearly. Both retain the same cardinal meanings. These are design alternatives, not Unity screenshots.

## Input design

| Input | Hold mode | Toggle alternative |
| --- | --- | --- |
| Left bumper / proposed Q | Press opens; genuine opener release commits the currently selected available entry | Press opens; next opener press closes without commit |
| Right stick | Select by direction; return to center clears selection | Same |
| Mouse | Absolute displacement from wheel origin selects; opening centers pointer once | Same |
| Arrow keys / D-pad | Select the corresponding cardinal sector directly | Same |
| Optional confirm | Gamepad South or mouse click commits once while open | Confirm is required; gamepad South, Enter, or mouse click |
| Cancel | Gamepad East / Escape closes without execution | Same |
| Additional pages | PageUp/PageDown and mouse wheel; gamepad shoulders can provide previous/next through the Radial map after conflict review | Same |

This refines the earlier proposal that used D-pad left/right for paging and up/down to cycle entries. Direct cardinal selection is clearer for this hub and provides a full digital path. Use right bumper for next page and a dedicated remappable previous-page binding; do not bind the held opener to previous page. The first one-page release needs neither page binding. Eight-sector digital mode can use discrete next/previous-entry actions with explicit prompts, rather than require simultaneous diagonal presses.

Q is currently unassigned in the inspected input asset. Left bumper currently recalls the companion; gamepad North opens inventory; gamepad South jumps; right-stick press pings companion danger; gamepad East handles canister pickup; D-pad is also a move composite plus previous/next/deploy bindings. These are mode-specific collisions. Keep gameplay and radial maps mutually exclusive, and gate `System/ToggleInventory` while Radial is active so North cannot also open inventory during an unrelated selection. Preserve gamepad West/keyboard E for gameplay interaction.

Offer release-to-activate as the proposed default and explicit-confirm as an option. Do not add Wilds' stick-return-to-activate behavior in the first release: it conflicts with our center-to-cancel rule. If added later, expose a separately named activation mode and test it independently. Do not assign right-stick click by default without resolving its existing danger-ping binding.

No repeat activation while held. Explicit confirmation consumes the open session and subsequent release cannot commit again. Cancel or focus/device loss takes precedence over confirm or release. Do not queue a rejected gameplay command for execution later when it becomes available.

## Selection geometry and input ownership

Measure direction in the wheel's local coordinate space. For stick input use the Input System's existing stick processing, then a selection threshold; do not add a second StickDeadzone processor. Proposed thresholds are 0.30 to enter selection and 0.22 to clear it. Pointer center radius begins at 44 reference units and scales with the canvas. Stick and pointer thresholds use different units and separate settings.

Compute clockwise angle with north as zero. Choose the nearest sector center. Four-sector boundaries lie at 45, 135, 225, and 315 degrees. Add a proposed 5-degree angular hysteresis around the currently selected boundary; use half-open intervals for initial exact-boundary ties. Entering the center always clears selection, regardless of angular hysteresis. Moving beyond the outer ring retains directional selection, so large mouse movements do not lose the target.

At opening, clear remembered selection. If the stick is already displaced because it was orbiting the camera, require a return to neutral before accepting stick selection. Mouse pointer recentering must not count as real player input or change prompt device. A deliberate key, pointer movement, or stick movement owns selection until another device produces meaningful input; stationary nonzero drift does not repeatedly steal ownership. Page changes clear selection and require a fresh selection gesture.

For release mode, selection is evaluated from the latest accepted intent at genuine opener release. Returning to center before release is cancellation. Buffer cancel/confirm/release intents within one input update and resolve cancel first, explicit confirm second, release last; Unity does not promise callback ordering. Device identity, session ID, and focus state must still match. If an opener is held on two devices, use the initiating device's release; another device's release cannot commit it.

## Audit findings and integration decisions

| Severity | Source and observed issue | Required change |
| --- | --- | --- |
| High | `TopDown3DInputRouter.SetMode` disables Gameplay, UI, and System maps before enabling the destination. | Keep System's opener enabled across Gameplay/Radial/Inventory transitions. Establish a transition guard and destination mode before disabling departing maps. Distinguish cancellation reasons. |
| High | Left bumper is bound to `Gameplay/RecallBigArm`. | Move left bumper ownership to `System/OpenRadial`. Retain F1 recall and execute the same companion authority from radial south. Preserve action IDs and serialization. |
| High | `TopDown3DBigArmFollower.RequestRecall` only sets `callRequested`; desired position remains the normal trail-follow target. Scene follow distance is 4.2 m; access radius is 3.2 m. Catch-up release is 7 m. | Give callover an explicit approach task targeting a validated stopping point inside access range. Keep urgent intent until approach completes or is blocked. Do not silently enlarge access range to hide the gap. |
| High | Follower route search is a 13 by 13 local grid at 1.25 m cell spacing; it is not world-scale detour navigation. | Surface blocked status and bounded replanning. Long detours are a separate navigation gate; never promise universal arrival or teleport recovery. |
| High | Greater Wasteland's action component has no harvester state or procedural world; placement checks loaded procedural chunks. | Provide an authored-ground placement adapter and world-owned device state before enabling radial west. Do not copy generation or prototype save ownership. |
| High | Cargo UI handlers and Auto-Pack do not recheck physical range on commit. | Validate live proximity and owner policy for every mutation. Disable inaccessible actions while open. |
| Medium | Inventory source selection uses a bool plus index, and equal-index checks can conflate different owners. Cargo detail selection reads Booter's inventory. | Use `(owner, slot)` selection, owner-aware details and opening focus. Support cargo-to-cargo moves deliberately. |
| Medium | Transfer service removes then adds and restores by ID on failure; observers can see intermediate state. | Prepare both owners, commit coherently, then notify. Bind quantities to the selected source slot. |
| Medium | Inventory opening always restores Booter's selection; physical cargo access calls `Open()` without owner focus. | Introduce `Open(owner)` or equivalent focus target. Physical cargo interaction selects the Legger pane; callover never opens it. |
| Medium | Action controller cancels gathering/placement when leaving Gameplay. Harvester entry handlers are private. | Add a validated command API for beginning placement after Gameplay has been restored. Avoid synthesizing InputAction events or invoking private handlers by reflection. |
| Medium | Router changes prompts on started/performed events; focus loss only has a capture-on-regain path. | Add explicit radial cancellation on focus loss/device removal, initiating-device ownership, and meaningful-source arbitration. |
| Medium | No production radial view/layout/preference owner exists. Current inventory uses uGUI and one installer-owned EventSystem. | Extend the existing UI lifecycle, not a second EventSystem or UI toolkit. Persist layout/binding preferences independently of world deltas. |

Audit sources are the live [input router](../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs), [input asset](../Assets/_Project/Settings/Input/InputSystem_Actions.inputactions), [inventory controller](../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiController.cs), [inventory canvas](../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryCanvas.cs), [UI installer](../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DInventoryUiSceneInstaller.cs), [follower](../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs), [cargo](../Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/TopDown3DBigArmCargo.cs), [transfer service](../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DInventoryTransferService.cs), [action controller](../Assets/_Project/Scripts/Runtime/TopDown3D/Interaction/TopDown3DPlayerActionController.cs), and [production scene](../Assets/_Project/Scenes/TopDown3D/GreaterWasteland.unity).

## Controller responsibilities and transitions

| Proposed owner | Responsibility | Boundary |
| --- | --- | --- |
| Existing input router | Maps, mode, opener session/device, cursor, prompt device, semantic radial intents | Does not choose availability or mutate items |
| `TopDown3DRadialSelectionState` | Pure sector geometry, thresholds, hysteresis, page and selection identity | No scene, time-scale, or storage mutation |
| `TopDown3DRadialMenuController` | Open/cancel/confirm/release lifecycle and exactly-once dispatch | Does not consume inventory directly |
| `TopDown3DRadialMenuCanvas` | Safe-area ring, labels, hints, accessibility, unavailable reasons | Presentation only; no independent input polling |
| Command registry/adapters | Stable command IDs, current availability, execution result | Delegates to inventory, companion, or placement authorities |
| Layout editor and preference store | Validated draft editing, Apply/Cancel, layout and input override persistence | Does not mutate world inventory or task state |
| Existing companion authority | Approach route, urgent speed, safe arrival, blocked reason | Always owns real physical position |

Place proposed scripts in the existing TopDown3D runtime assembly, under `UI/` for views/controllers and dedicated data folders where useful. Input System 1.19.0 and uGUI 2.0.0 are already pinned; no package addition or upgrade is needed.

The controller states are Closed, Open with no selection, Open with selection, and Waiting for opener release after consumption/cancellation. Opening records a unique session and initiating device. UI selection and availability changes never dispatch a command.

On valid commit: freeze the chosen stable command ID, mark the session consumed, close the view, restore Gameplay, revalidate the command, then dispatch once. Player Inventory immediately transitions from Gameplay to Inventory with Booter's focus; Call Legger Over stays in Gameplay; Dust Canister stays in Gameplay and begins a separate placement preview. If dispatch fails, show the reason and leave gameplay usable. Focus/device loss cancels and never recaptures the pointer while unfocused.

The System map remains live, but the router accepts the opener only in Gameplay. UI and Gameplay maps are disabled during Radial; its dedicated map owns select, confirm, cancel and paging. Inventory enables UI and System. Disabled mode cancels before disabling System. The radial remains modal for Booter's movement/camera in the first slice, matching the earlier plan; the world and companion continue at normal time scale. Moving while the wheel is open is a future product option, not something research silently adds.

## Customization screen

Use a preview wheel on the left and an assignment browser on the right, with page name above and Apply/Cancel below. At smaller widths, stack preview above browser. The player selects a sector, chooses Items or Commands, and assigns with Submit. Selecting an unavailable supported command shows its current reason but still permits assignment. Non-actionable resources explain that they have no quick-use operation.

Provide controller navigation with explicit neighbors between sector preview, tab controls, assignment list, and footer. Edit using discrete actions; no drag-and-drop requirement. Mouse users may drag as an optional convenience after the discrete path works. Search is optional when the actual command list grows; do not add empty categories or invented abilities.

Changes live in a draft until Apply. Cancel leaves the previous layout intact. Validate page and command IDs before replacement, retain unknown assignments as unavailable, and report failed persistence. Default cardinal entries remain until the player deliberately moves them. An item reaching zero or a temporarily absent companion never causes automatic compaction.

## Verification and implementation order

First repair input transition safety and command APIs; then implement selection state and the four-sector view; then wire inventory and callover; then authored canister placement; then customization/persistence. Inventory mutation repairs can be a bounded batch alongside command integration, with separate tests. Do not implement optional page depth before the base hub works.

Focused EditMode cases must cover cardinal/boundary selection, center clear, stick already displaced on opening, pointer warp, page reset, competing devices, synthetic cancellation during map changes, cancel plus release in one update, confirmation plus release exactly once, held reopen prevention, unplug/focus loss, all mode/map gates, System North suppression during Radial, physical West interaction after callover, owner-aware inventory focus, inaccessible cargo operations, absent canister/service, and preference validation.

Companion contract cases must prove urgent approach remains active until reachable stopping distance, route failure preserves true position, cargo load affects movement legally, and moving Booter changes the destination through the navigation owner. Structural production proof must establish one EventSystem, runtime assembly references, installed UI lifecycle, authored placement authority, unchanged package pins, and preserved asset GUIDs. No smoke tests or foreground Unity interaction are authorized by this research task.

Player acceptance should compare release and explicit-confirm modes on a real controller, readable unavailable states over bright terrain, UI scale at 1080p/4K and ultrawide safe areas, customization without a mouse, and whether callover consistently ends within interaction reach. Proposed usability targets are no accidental action in ten open/cancel trials and successful selection of all three defaults without looking after familiarization; these are future acceptance targets, not measured outcomes.

## World and save boundaries

This UI owns preferences and transient selection, not world identity. Item/command IDs stay stable; physical actions mutate existing authorities. Greater Wasteland remains authored and always loaded. Scene changes cancel menu sessions and discard transient target references. Future chunk unload invalidates targets without changing assignments. A future authored save owner captures the Legger's actual task/position, carried quantities, depletion and device deltas; no UI record restores him beside Booter. Terrain generation and streaming remain deferred.

## Research closeout

The recommended design is ready for implementation planning: four stable cardinal sectors, hold plus release activation, optional explicit confirmation/toggle access, separate radial input routing, owner-aware inventory entry, physical callover approach, and a draft-based customization editor. The implementation gates are map-transition safety, approach stopping distance/navigation limits, cargo transaction/access correctness, and authored canister integration. Source inspection and documentation verification establish this handoff; Unity behavior, gamepad feel, final art, and long-range navigation remain unverified.

The conversation preview passed headless Edge checks for selection feedback, missing-canister feedback, reset, no horizontal overflow, and non-overlapping entry targets at 320 and 736 pixels. Both layouts were captured and visually reviewed at representative widths. This proves the preview's local behavior and layout; host-provided icon rendering, design controls, Unity integration, and physical controller input are separate boundaries. Diagnostics remain under ignored `Logs/radial-preview-*`.

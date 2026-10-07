# Radial Menu Production Integration

Implemented 2026-10-06 in the TopDown3D runtime for `Assets/_Project/Scenes/Production/GreaterWasteland.unity`. The existing inventory scene installer installs the menu at runtime, without rebuilding or replacing the authored scene. The [system design](../../Design/Gameplay/RADIAL_MENU_SYSTEM_DESIGN.md) and [polish audit](../../Design/Gameplay/RADIAL_MENU_POLISH_AUDIT.md) remain the design references.

## Controls and behavior

- Hold left bumper or Q to open. Gamepad: aim the right stick and release it to neutral while still holding the bumper to activate. Releasing the bumper first cancels. Keyboard: select with mouse or arrow keys and release Q to activate. D-pad selection also supports explicit confirmation. East/Escape cancels before activation. Mouse center return clears selection.
- North opens Booter's inventory. South calls the Legger over through his physical pathfinding and urgent movement. West selects the carried dust canister and begins a ground preview; G or D-pad down confirms placement separately. C/gamepad East cancels the preview.
- Calling and arrival do not open cargo. Target the reachable Legger and press the existing gamepad West/keyboard E Interact control. Cargo receives focus and rechecks physical access for operations.
- The Legger uses a close approach target rather than his normal trailing point. The menu supplies approaching, blocked-route and ready feedback. Existing terrain/collision, acceleration, braking and cargo rules still govern movement; there is no teleportation recovery. Once Booter moves away after arrival, normal following resumes.
- Right bumper/PageDown and PageUp select pages. Eight-sector layouts also support sequential entry selection with gamepad West/North or keyboard right/left brackets while the radial is open. Those gameplay buttons retain their normal meanings after it closes.

## Segment geometry and input safety

The ring is built from explicit annular strips. Each side is offset by half the six-reference-unit seam gap; inner and outer arc endpoints use the corresponding radius-dependent trim. Adjacent sides therefore remain parallel with a constant perpendicular gap. No center fan or conical-gradient cutout defines the sectors. The selected perimeter uses the same edge geometry. The center remains open without a redundant selection prompt.

The conversation preview now uses equivalent SVG annular paths. Its browser checks still pass after replacing the old gradient/clipped wedge treatment.

System's opener remains enabled across modal transitions. Gameplay, UI and Radial maps have separate ownership. System inventory toggling is suppressed during radial selection/customization. Cancel wins over confirm and release; commands dispatch once after closing the radial. Cached pointer coordinates do not switch prompt devices, and synthetic recentering does not select a wedge. A pre-displaced stick must return to neutral before selecting. Camera orbit also waits for neutral after the menu, and cursor relocking cannot immediately feed a synthetic delta into orbit.

## Customization and inventory handoff

The inventory's Radial Setup button opens a controller-friendly editor. The first runtime slice supports four named pages, assigning/clearing the available commands, adding/deleting pages, four/eight-sector layouts, wheel scale, and hold-release/hold-confirm/toggle-confirm activation. Destructive draft changes require confirmation. Apply validates the layout and atomically writes `radial-layout-v1.json` in Unity's player preference directory; failure retains the draft and asks the player to retry. Cancel preserves the prior saved layout. Active page identity is retained when it survives the edit.

Default assignments are north Inventory, south Call Legger Over, west Dust Canister, and empty east. Item quantity is resolved from Booter's real inventory. Auto-Pack is an optional assignment and requires the Legger's current proximity. Inventory owner selection now distinguishes equal slot indexes across owners, cargo details read from cargo, and mount rearrangement/Auto-Pack revalidate reach. Cargo buttons disable when access is lost. Cargo mounts, their summary, item details and action buttons have separate layout regions.

The first runtime editor does not yet expose interactive binding overrides, page duplication/reordering, sound/haptic tuning or custom artwork. These remain expansion seams; they are not claimed as completed features.

## Authored canister state

Greater Wasteland receives a stationary world-root canister registry. Device identity derives from the preserved scene GUID and a monotonic placement sequence, independent of the scene's path. Positions remain in the fixed authored coordinate frame. The registry owns active-game collection time and partial fill; presentation stays at the placed world point as Booter moves.

Placement uses the existing ground markers, maximum slope and clearance checks, without a procedural loaded-chunk requirement. Selection does not remove an item. Placement removes one canister only when the state commit succeeds; pickup accepts canister plus dust atomically or leaves the device deployed. Versioned snapshots include the authored identity and reject another world's records. Existing generated-world snapshots retain their prior seed behavior.

This task does not install a whole-game authored disk-save owner. Canister/inventory snapshots are available, but application restart or starting a fresh Play session does not restore deployed devices through a newly invented save system. Radial preferences are independently saved.

## Verification

Validation ran on a fresh isolated Windows project copy because the working checkout was owned by an open Unity GUI. Unity `6000.4.0f1` used the pinned packages; all launches were hidden/background batchmode. No gameplay smoke test, foreground activation, or simulated gameplay traversal was run.

The final focused EditMode run passed **22 tests, zero failures**, covering `TopDown3DRadialTests`, `TopDown3DInventoryUiTests`, and `TopDown3DMicroDustHarvesterTests`. It checks constant gap width, canvas-facing annular triangles, center clearing/boundary hysteresis, preference validation and clone independence, held-opener continuity, suppression of inventory input during radial mode, camera neutral return, simultaneous cancel/release, exactly-once inventory handoff, close callover target without relocation, and authored placement/snapshot/pickup transactions. Existing inventory and micro-harvester contracts are included.

`TopDown3DRadialProductionValidator.ValidateFromCli` also runs the existing authored-terrain/play-root validator and production routing checks, then proves one EventSystem, a closed installed menu, valid preferences and an authored registry with a stationary world owner. It saves no scene or project content.

Final logs and XML are in ignored `Logs/radial-acceptance-tests.log`, `Logs/radial-editmode.xml`, and `Logs/radial-final-production-validation.log`. Cold-import package/API migration diagnostics cleared in warm validation. The UI test's batch harness now opens the saved gameplay template before creating an additive test scene; input tests use isolated, restored settings to route manual events in EditMode. Assertions were retained and expanded.

Automated proof establishes compilation, geometry, state transactions, input/UI contracts and scene wiring. Actual rendered appearance in Unity, physical controller comfort, long detours and subjective sprint/arrival feel remain user acceptance boundaries.

## In-game feedback correction — 2026-10-06

User feedback reported text without ring graphics and requested right-stick release activation. The custom graphic now requires its CanvasRenderer before Canvas registration, uses the modern VertexHelper rebuild path, and explicitly supplies a white texture for solid vertex colors. The ring keeps its constant-width annular seams. The center prompt/backing and neutral-state instruction panel are removed; selected-command detail and binding hints remain.

The default gamepad gesture arms a newly selected sector and activates it when the stick returns below the neutral threshold while the opening bumper is still held. Bumper release first cancels. Cancel has priority over simultaneous stick release; a stick displaced before opening must first return to neutral without activation. Consumption requires bumper-up before another opening. Explicit-confirm accessibility modes remain available.

Correction validation: 12 focused radial EditMode contracts passed on the isolated Windows candidate, including actual CanvasRenderer mesh/material submission and hide/reopen, absent redundant prompts, cancel precedence, pre-displaced-stick gating, bumper-only cancellation, and exactly-once inventory opening with the bumper still pressed. The production radial validator also passed (`Logs/radial-stick-release-production.log`). Background batchmode proves Canvas submission rather than final on-screen appearance; hands-on rendered acceptance remains with the user. Logs: `Logs/radial-stick-release-tests.log` and `.xml`.

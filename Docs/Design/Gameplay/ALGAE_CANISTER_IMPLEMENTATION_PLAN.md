# Algae canister control implementation plan

October 11, 2026. The author specified a lower-left control that spends a shared algae reserve to restore thirst on D-pad left, hunger on right and health on up, then authorized implementation as a goal. The initial session-scoped milestone is implemented and verified in the [implementation receipt](../../Evidence/UI/AlgaeCanister/IMPLEMENTATION_2026-10-11.md). On October 12 the author specified naturally replenishing algae; [passive replenishment](../../Evidence/UI/AlgaeCanister/REPLENISHMENT_2026-10-12.md) is now implemented at a provisional 1.2 units per game-minute. This supersedes the finite-only reserve and blanket refill deferral below. The [brainstorm](ALGAE_CANISTER_BRAINSTORM.md) retains the original design alternatives. External refill sources and production restart persistence remain future slices.

The audit's four gaps are resolved: input requests are collected until modal priority is resolved; the consumption controller runs after survival advancement and synchronizes the existing clock; cardinal rejection uses the originating D-pad while other rebound buttons remain valid; dust-placement previews explicitly block consumption. Production installation uses the motor's assigned scene input router, which is on a separate object in Greater Wasteland.

## Existing framework and live audit

| Surface | Current implementation | Planned use |
| --- | --- | --- |
| Shared HUD | `TopDown3DGameHudCanvas`, 1920 × 1080 reference scaling, sorting order 120, runtime installation | Keep one HUD canvas and existing bootstrap |
| Upper-left statuses | `TopDown3DSurvivalHud`, five meters, safe-area placement, vitals change subscription | Preserve layout and update the affected meter from the same state owner |
| Lower-left control | `TopDown3DActionDpadHud`, 168 reference pixels, 34-pixel margin | Extend this panel with action labels, icons, canister fill and outcome feedback |
| D-pad graphic | `TopDown3DActionDpadGraphic` inherits the custom graphic base in `TopDown3DActionDpadHud.cs` | Replace the three arrow icons with survival icons; preserve the dust action below |
| Survival state | `TopDown3DSurvivalVitals.SetValue`, capacities, normalized getters, `Changed`, snapshots v1/v2, precise hunger/thirst accumulators | Sole authority for all restoration; do not write meter fills as gameplay state |
| Input | `TopDown3DInputRouter`, separate Gameplay/UI/Radial/Camp maps and binding-display authority | Add semantic survival intents and exclusive mode gating |
| Existing dust canister | `TopDown3DHarvesterSettings.CanisterItemId` is `tool.micro_dust_harvester`; placement uses D-pad down | Preserve it as a distinct object and mechanic |

The input asset binds `Gameplay/Previous` to D-pad left and keyboard 1, and `Gameplay/Next` to D-pad right and keyboard 2. No production C# consumers of those two action names were found in the audited scripts, but implementation must check serialized/event consumers before changing their bindings. `Gameplay/Move` has a composite named `Dpad` containing keyboard WASD and arrow keys; that name does not mean the gamepad D-pad moves Booter. Do not remove keyboard movement by mistaking the composite for a conflict.

UI navigation and radial selection use the full D-pad. Camp placement uses left/right for rotation and up for sun orientation. These uses remain valid in their own modes. Gameplay consumption cannot run while those modes own input.

Sources: [HUD canvas](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DGameHudCanvas.cs), [D-pad HUD and graphic base](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DActionDpadHud.cs), [survival HUD](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DSurvivalHud.cs), [vitals](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Survival/TopDown3DSurvivalVitals.cs), [input router](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.cs), and [input asset](../../../Assets/_Project/Settings/Input/InputSystem_Actions.inputactions).

## Scope and ownership

Gottspan coordinates the slice, Babineaux owns Unity wiring/import/validation, and the author owns visual acceptance and balancing. No asset package or UI framework replacement is needed. Runtime code remains in `BooterBigArm.TopDown3D.Runtime`.

Create a dedicated `TopDown3DAlgaeCanisterSettings` asset under `Assets/_Project/Settings/Survival/Resources/`, a player-owned `TopDown3DAlgaeCanisterState`, and a small `TopDown3DAlgaeConsumptionController` under `Scripts/Runtime/TopDown3D/Survival/`. Settings contain capacity, initial fill, maximum dose gains, algae cost per restored vital point and cooldown. The state owns remaining algae; the controller owns eligibility, quoting, input and committing a dose. HUD components only read state and present results.

Keep the algae reserve independent of dust collection and integer inventory stack quantities. The first slice treats it as one player survival attachment, not a tradable stack item or a second equipment inventory. Do not put quantities into the backpack prefab. Inventory-backed canisters would require a later contract for per-instance fill, transfer, mass and active-container selection.

## Consumption contract

For each accepted press, read the current mode, player eligibility, target value, capacity and remaining algae. Let `D` be the missing vital points, `G` the authored maximum gain and `R` the positive algae cost per point. Quote `gain = min(D, G, remainingAlgae / R)` and `cost = gain × R`. At default tuning, `R` is .25 for Drink/Eat and 1 for Treat. Use sufficient internal precision and a documented small tolerance; no rounding may repeatedly create free restoration, negative contents or overfill.

Validate finite, nonnegative tuning and state. Reject zero-effect doses, a missing canister, a full target, an empty reserve, a dead player, disallowed modes/actions and active cooldown. Health treatment must not revive a dead player unless a later design explicitly adds revival. Recheck eligibility immediately before commit. A rejected or cancelled request changes neither value and does not start cooldown.

Commit the reserve reduction and vital increase as one operation, then publish the result. All observers must see the final pair of values. Account for the current vitals setter firing `Changed` immediately; prepare the reserve update before that notification or add a narrowly scoped deferred-notification seam. Protect against reentrant event listeners and duplicate input requests. Avoid two independent UI callbacks that each modify one half of the transaction. Only successful commits start the shared cooldown.

Preserve the vitals owner's precise accumulators and timestamped activity reconciliation. A dose must not discard accrued ordinary depletion or reset transient activity clocks. Restore or reset must cancel pending input and cooldown presentation without refilling algae unless an explicit new-session/reset operation owns both survival states.

## Input and modal ownership

Add semantic actions `DrinkAlgae`, `EatAlgae` and `TreatWithAlgae` to Gameplay, bound to left/right/up. Remove those gamepad bindings from Previous/Next after the consumer audit; retain or deliberately reassign any real selection behaviour. Provisional keyboard alternatives are separate rebindable keys, such as F5/F6/F7, after checking every map and existing override. Do not use keyboard arrow keys for doses because they already move Booter.

The router remains the sole raw-input adapter. It emits one consumption intent per fresh cardinal press in Gameplay. A shared cardinal resolver must see the D-pad vector and reject diagonals before either action callback commits. Multiple simultaneous survival commands reject until their controls return neutral. Holding does not repeat, and changing from left to up without returning neutral does not create a second gamepad dose.

When leaving a menu, radial, camp mode or rebinding session, require neutral input before rearming. Apply the same rule after focus loss and gamepad reconnection. Clear pending requests when another system takes input ownership. Camp placement, camp action panels, sleeping/resting, menus, inventory, gathering, climbing, traversal and constrained actions cannot consume. Ordinary walking and sprinting may continue in the first prototype; no locomotion animation lock is introduced. Preserve attacks and dust-placement controls, while suppressing survival use whenever an existing exclusive action owns control.

Binding prompts come from the input router. Rebound directions must show their actual binding; the graphic cannot promise that physical D-pad left still drinks after a custom override. Keep new actions out of UI navigation and camp maps. Down continues the existing dust deployment action.

## HUD presentation

Retain the upper-left five-meter panel and the lower-left safe-area anchor. Extend the lower-left footprint only enough for the canister gauge and short action labels; begin with the current 168-pixel control and a roughly 100-pixel adjacent gauge area, then verify readability at target resolutions. Keep 34-pixel reference margins. HUD graphics must not intercept gameplay pointer input or create another EventSystem.

Display current fill and capacity with a bar and text. Show Drink, Eat and Treat using distinct icons and the existing meter colours. Preserve a clearly labelled Dust action below. Indicate full/empty/blocked states through shape/text as well as colour. During modal contexts the reserve may remain visible, while survival actions are visibly inactive. Keep existing press indication accurate for the active context rather than flashing a consumption success when the same direction navigates a menu.

Present the actual committed gain and cost, including small partial doses. Animate the reserve and target meter together, then clear feedback. Use a short local pulse and optional quiet use sound/haptic; support reduced motion and disabled vibration. Do not allocate fresh label strings or rebuild the graphic every frame when values are unchanged. Check that the expanded panel avoids other interaction feedback and remains legible on small screens and unusual aspect ratios.

## World and persistence contract

| Concern | Required behaviour |
| --- | --- |
| Deterministic world identity | Algae belongs to Booter's survival state; it does not depend on a terrain seed or procedural placement |
| Streaming and reload | Greater Wasteland terrain is fixed and always loaded. Dressing visibility, HUD rebuilds and visual-rig replacement cannot reset the reserve |
| Stable generated-object identity | No generated-object IDs are needed. One canister state is attached to the existing player identity; future refill objects need their own IDs |
| Authored constraints | Algae supplies thirst/hunger/health only. Preserve oxygen, Reserve, existing camp costs, dust mechanics and all native terrain |
| Persisted deltas | Capture capacity-compatible remaining fill with a versioned canister snapshot; restore it alongside vitals atomically in any future production save integration |

The current [survival standard](SURVIVAL_SYSTEM_STANDARD.md) describes snapshot support without a complete production survival save owner. The existing game-state save service has generated-world dependencies and must not be installed into Greater Wasteland for this feature. Preserve vitals snapshot versions 1 and 2 rather than silently appending algae to an unrelated format.

The initial milestone is explicitly session-scoped: fill survives menus, disabling/re-enabling UI and repeated installation, but a genuinely new session starts with authored initial contents. Provide a tested canister snapshot seam. Do not claim restart persistence until the actual production save owner includes both canister and vitals, restores them together, and passes restart/migration proof. Never save held buttons, pending doses or active cooldown as a reusable consumption request.

## Implementation batches and gates

1. **Confirm the bounded contract and audit consumers.** Recheck Git ownership, current scene/UI installation, Previous/Next consumers, map switching and player-action eligibility. Use the trial tuning from the brainstorm unless the author revises it. Gate: exact shared reserve and down-button behaviour are documented; no conflicting input consumer remains unresolved.
2. **Build state and transactions.** Implement validated settings, state initialization, a pure quote function, successful commit and a versioned snapshot seam. Gate: full/empty/partial/near-cap doses, all three ratios, repeated small doses, invalid tuning, reentrancy and duplicate calls pass focused tests; observers see consistent reserve/vital values.
3. **Route the controls.** Add semantic intents, migrate conflicting bindings, cardinal/neutral guards, mode/action eligibility and cooldown. Gate: fresh left/right/up work exactly once; hold, diagonal, simultaneous keys, menu return, camp rotation, reconnection and rebinding cannot spend accidentally. Other input modes retain their current behaviour.
4. **Extend the existing HUD.** Add canister fill, clear survival icons/labels, disabled reasons and committed-result feedback to the existing lower-left panel. Gate: one HUD canvas, no new EventSystem, safe-area/layout checks, accurate custom-binding prompts and no mismatch between displayed and authoritative values.
5. **Integrate Greater Wasteland and close out.** Extend the existing runtime installer idempotently; attach one state/controller to Booter's real survival owner. Rebuilding UI never seeds another fill. Align the survival standard and record current-machine proof. Gate: production component/reference validation, import/compilation and affected focused regressions pass; terrain, backpack, camp and dust records remain unchanged. Commit only verified task-owned files. No push is implied.

## Verification and acceptance

Add focused `TopDown3DAlgaeCanisterTests` and input tests covering quote/commit accounting, capacities other than 100, floating-point tolerances, empty/full/dead-player rejection, useful partial doses and snapshot validation. Extend `TopDown3DActionDpadHudTests` for safe-area/layout/icon/label state and installation idempotence. Regress `TopDown3DSurvivalTests`, relevant camp/sleep and radial input tests, and dust-canister placement tests only where changed shared owners warrant them.

Test menu/camp modes without consuming, cooldown under different time scales, cancellation/focus loss, gamepad disconnect/reconnect, rapid alternating inputs and low-fill exhaustion. Verify that all precise survival values still advance correctly after a dose and that Changed observers never see a free restore. Inspect UI screenshots at 1920 × 1080, a smaller window, ultrawide and a simulated inset safe area.

Use the connected Editor only for background-safe checks that preserve the user's loaded/unsaved scene. Tests that open scenes or exercise installation use an isolated validation candidate or disposable preview scene. No foreground activation, automatic Play Mode stop, package changes or unrequested gameplay smoke tests. Full terrain-seam audits and player builds are unnecessary unless this slice introduces a concrete dependency on them.

The author's hands-on acceptance checks are: the lower-left control is readable, each requested direction spends and restores the right amount, the shared supply creates an understandable choice, and movement/menu transitions feel reliable. Final dose balance, refill economy and any physical use animation remain open. Completion of the initial milestone means a working session-scoped control with those limits stated, not an implemented refill economy or production restart persistence.

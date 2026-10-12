# Algae canister implementation

October 11, 2026. Authority: the author requested the brainstorm and plan, audited it, then explicitly authorized pursuing implementation as a goal. The [plan](../../../Design/Gameplay/ALGAE_CANISTER_IMPLEMENTATION_PLAN.md) controls the bounded session milestone.

## Delivered behaviour

Greater Wasteland's existing runtime HUD bootstrap now installs one algae state/controller on Booter and extends the existing lower-left D-pad HUD. The installer follows `TopDown3DPlayerMotor.InputRouter`; production input lives on the separate `Top Down 3D Input` object. The upper-left five-meter HUD remains the same owner and layout.

| Control | Restoration ceiling | Cost at that ceiling |
| --- | --- | --- |
| D-pad left / F5 | 20 thirst | 5 algae |
| D-pad right / F6 | 20 hunger | 5 algae |
| D-pad up / F7 | 10 health | 10 algae |

The shared canister starts at 100 / 100 once per session. Dose costs are proportional to useful restoration and available contents. Full meters, empty stores, dead players and unavailable actions spend nothing. One press consumes once; holding, diagonals and conflicting commands do not repeat or double-spend. Successful doses start a shared 0.35-second unscaled cooldown.

Input callbacks collect semantic requests instead of consuming immediately. The router resolves requests after the Input System update and modal requests. The controller commits later, after survival advancement, with another eligibility check. Mode transitions clear pending requests. Rebinding to a non-D-pad button remains valid, while diagonal checks use the originating D-pad. Focus loss/reconnection require a fresh release; removed devices are never queried for state. The Previous/Next gamepad bindings were removed; their keyboard bindings and existing action IDs remain unchanged. Down continues dust-canister deployment.

The controller blocks menus, camp, gathering, dust-placement previews, climbing, traversal and motor-owned constraints. It does not lock ordinary movement for a cosmetic animation. The canister/vitals pair commits silently before either notification; reentrant consumption is rejected. Restoration synchronizes the existing survival clock without advancing the same interval twice and preserves fractional hunger/thirst depletion. Sub-visible restoration and unrepresentable spending reject rather than creating free healing or charging without a visible change.

The lower-left panel has three distinct survival icons, a green canister gauge, live binding prompts, Full/Empty/Busy/Menu reasons and actual gain/cost feedback. A dark backdrop supports contrast over terrain. Reserve and target meter animate over the same 0.25-second presentation interval; accepted reduced-motion settings make the use presentation immediate. No sound or haptic was added. The existing D-pad graphic lacked a `CanvasRenderer`; the concrete requirement and installation repair now guarantee its geometry renders.

## Persistence and world boundaries

The reserve belongs to Booter's player identity, independent of seeds, fixed terrain, dressing cells and visual-rig replacement. It adds no generated-object identity, world delta, inventory stack or physical backpack attachment. Repeated installation and presentation lifecycle changes preserve remaining contents.

A validated version-1 canister snapshot seam captures remaining contents and capacity. Invalid versions, non-finite/out-of-range values reject; smaller authored capacity clamps contents without refilling or proportional scaling. Existing survival snapshot versions 1 and 2 remain unchanged. Production restart persistence and atomic restore of the two snapshots require the future production save owner. No generated-world save service, refill economy, cartridge transfer, oxygen/Reserve restoration or revival was added.

## Verification

Unity 6000.4.0f1 imported and compiled the candidate on Windows. The final focused verification invoked 84 NUnit test cases directly, with their setup/teardown, in an additive sandbox and disposable preview scenes. The runner captures error/exception logs and treats them as case failures. Final result: **84 passed, 0 failed, no new error logs during those cases**. This is direct EditMode fixture execution, not a TestRunner XML suite or gameplay smoke test.

| Fixture | Executed cases |
| --- | --- |
| Algae canister accounting/snapshots | 11 |
| Algae controller, production-style separate router and HUD | 8 |
| Algae input, modal conflicts, rebinding and disconnect | 7 |
| Camp input regressions | 5 |
| Menu regressions | 16 |
| Radial regressions | 12 |
| Sleep regressions | 16 |
| Existing survival pure/fixture regressions | 9 |

Production reference inspection confirmed one Booter motor, its assigned input owner, all three actions in the actual production input asset and valid canonical algae settings. Greater Wasteland remains loaded and clean. No scene, terrain, package or project-setting file is part of this change.

Six inspection PNGs cover 1920 × 1080, 1280 × 720, 2560 × 1080, a simulated inset safe area, a committed Drink dose and an empty canister. They were visually reviewed. The dose view shows thirst rising from 35 to 55 and algae falling from 60 to 55. Capture uses a disposable preview scene and a transient feature-free renderer because production uses an overlay HUD; original render-pipeline references are restored and no settings file is written. These are isolated UI renders, not an in-world Player capture or hands-on gameplay acceptance.

One legacy survival installation test opened a new single scene during the initial verification. The previously clean Greater Wasteland scene was restored in the background. That method is now explicitly excluded from this connected-Editor runner; all subsequent cases run in an additive sandbox or preview scenes and preserve the loaded production scene. No unsaved scene edits were discarded and no foreground activation occurred. Earlier development exceptions remain historical diagnostics; the final error-capturing run above is the acceptance result.

## Reproduction and remaining acceptance

`Tools/Repository/AlgaeCanisterAuthoring.cs` creates the settings asset through the existing Unity CLI `run_script` path without replacing existing tuning. `AlgaeCanisterVerification.RunFocused` runs the selected fixtures in Edit Mode; `AlgaeCanisterCapture.Capture` captures the UI from the loaded production input reference. Both refuse Play Mode and never stop the user's session.

The [test receipt](focused-tests.json) contains individual results and source hashes. Logs remain under ignored `Logs/AlgaeCanister/`. Native source/model content and unrelated dirty files are excluded from the commit. No push is authorized or performed.

Hands-on controller feel, readability over actual gameplay terrain and final dose balance remain author acceptance. This milestone supplies a functioning session control; refill sources and production restart persistence remain deferred as planned.

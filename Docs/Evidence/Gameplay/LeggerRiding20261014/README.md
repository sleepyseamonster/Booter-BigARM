# Camp Loop Legger riding — October 14, 2026

Implementation of the [audited plan](../../../Design/Gameplay/CAMP_LOOP_LEGGER_RIDING_IMPLEMENTATION_PLAN_2026-10-14.md), limited to CampLoopTesting. Mounting uses an idle standing placeholder; there is no mounting clip.

## Controls and behavior

Approach the Legger and tap the current Interact binding to mount. Use the existing movement and sprint bindings to steer and change gait. Tap Interact while mounted to brake and find a supported left/right/rear exit. When all exits are blocked, remain mounted and reposition. A dismount request requires neutral movement followed by fresh steering to cancel. Keyboard Interact hold retains cargo access when the standing seat passes existing range/visibility checks. Menu, inventory and palette keep priority and require fresh controls after returning.

The study profile starts at 6/10 m/s, acceleration 16 m/s², braking 24 m/s², 500/360 degrees/s steering, 55-degree supported slopes and 1 m up/1.25 m down steps. Cargo load scales speed and acceleration. These values are provisional until hands-on acceptance. Tuning is captured at startup; restart the session after changing it.

The follower is the only Legger physics writer. Booter yields before ordinary movement and remains passive during mount ownership. Translation sweeps, body rotation envelopes, standing rider clearance, full footprint support, and bounded step continuation reject unsafe motion. Changed landing/obstruction can hold a faulted rider until lab reset. Reset validates an authored supported return before restoring Booter's body settings and preserves the Legger's coordinate/cargo. A managed reset participant supports recovery when the native riding component is removed.

## World and ownership boundary

Native terrain and deterministic world identity are unchanged. The fixed terrain is always loaded; no terrain generation or streaming is introduced. Existing `legger.primary` and cargo identity remain intact. Seat, exits and corridor solids are authored constraints. Ride relationships, epochs and query/route work are transient, with no checkpoint format change or mounted save continuity. The reference lab remains session-only. Production scene, package pins and editor settings are outside this slice.

Shared file integration follows the sealed atmosphere/camera, animation, inventory, NPC and workstation handoffs. Source scene installation is additive and preserves prior records; its exact transfer receipt accompanies final proof.

## Verification boundary

Focused EditMode tests exercise actual physics queries, controller ownership, input devices/rebinding, multi-tick steps, blocked release, component lifetime and native scene binding. Background authoring is mutating; validation is nonmutating and opens the scene only in an isolated candidate. No gameplay smoke test, performance acceptance capture, player build or hands-on feel claim is made. Final integrated proof: **186/191 passed**; all 37 riding checks and all ten camera checks pass. The same five failures independently reproduce with every new riding script removed and the committed workstation scene/shared sources restored (**149/154 baseline**). The final focused riding/camera run passed **47/47**, including disabled-controller cargo fallback, after publication line-ending normalization. Repeated installation passed without changing scene bytes; the final validator rechecked serialized installation after whitespace normalization. No compiler errors were reported.

## Automation

Use the pinned Windows editor and the background-only workflow in [Unity automation](../../../Engineering/UNITY_AUTOMATION.md). Author only in an isolated task-owned candidate with `BooterBigArm.Editor.CampLoopLeggerRidingAuthoring.InstallFromCli`. Repeated installation validates existing data without resetting adjusted anchors. Validate with `BooterBigArm.Editor.CampLoopLeggerRidingValidator.ValidateFromCli`.

The controller Inspector reports phase/epoch, achieved speed, terrain blocking, last-tick queries/substeps and actionable failure. Query/substep ceilings are 256/eight; these bounds do not establish representative frame timing.

## Baseline failures and retained evidence

The five unchanged failures are `GameplayActions_ContainRequiredGamepadAndKeyboardBindings` (old Recall gamepad binding), `LandscapeCameraPullback_HasMatchingWorldCoverage` (old expected coverage), `PerspectivePrototypeValidator_ReportsNoErrors` (historical prototype catalog), and two `TopDown3DInventoryLoopUiTests` quantity expectations: `CacheViewUnloadsLocallyAndClosingRestoresThePersonalKit` and `QuickTransferAndPanelSwitchKeepTheCorrectOwnerAndNeverMoveOreIntoKit`. Their messages match the clean-code/no-riding baseline exactly. No baseline expectations or production behavior were changed to make them pass.

[Integrated results](final-integrated-tests.xml), [focused results](final-focused-tests.xml), [baseline results](full-baseline-tests.xml), [source proof](source-proof.json) and [scene transfer](scene-transfer.json) define the tested boundary. Raw background import/authoring/validator logs remain in the local candidate's ignored `Logs` folder. The candidate includes local private animation imports for validation only; those assets are not part of the task commit.

Source placement and user handoff are complete for this reference-scene slice. User-owned play acceptance should cover analog creep, fast turns/reversal, camera orbit, load differences, rough ground, the authored corridor, headroom and blocked exits. Representative timing and any subsequent tuning need their own recorded evidence.

Local implementation batches: runtime `a19ec4ab9`; scene, tooling, tuning and tests `b200e6a80`. The final evidence batch also normalizes empty Unity metadata fields without changing their GUIDs. No push was performed.

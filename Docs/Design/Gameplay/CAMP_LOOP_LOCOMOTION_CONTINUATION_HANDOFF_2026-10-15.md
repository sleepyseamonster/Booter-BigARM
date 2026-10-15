# Camp Loop locomotion continuation handoff

October 15, 2026. The author accepted the latest forward-gait comparison: **“Okay, that looks great.”** The next requested work is **strafing, turning, slopes and jumping**, integrated into the Camp Loop playtesting loop. This handoff records the starting point for the incoming implementation agent. Its preparation changes documentation only.

**Continuation update:** the author subsequently chose LT facing lock with camera look-ahead retained. The [first slow lateral slice](CAMP_LOOP_STANDING_STRAFE_SLICE_2026-10-15.md) is now installed for bounded AP02 review; its [evidence and recovery](../../Evidence/Gameplay/LocomotionImplementation20261014/StandingStrafeReview20261015/README.md) record 157 passing checks and the remaining direction/turn/slope/jump gaps. Statements below about an unchosen strafe control describe handoff preparation, before this decision.

## Start here

Work in `D:/Arc & Dust/Project`, on `main` tracking `origin/main`. Read [AGENTS.md](../../../AGENTS.md), [local workspace](../../LOCAL_WORKSPACE.md), [Gottspan](../../Agents/Gottspan/README.md), [Babineaux](../../Agents/Babineaux/README.md), [Unity automation](../../Engineering/UNITY_AUTOMATION.md) and [input architecture](../../Engineering/INPUT_ARCHITECTURE_STANDARD.md). Recheck the live tree and Editor before editing; this is a shared checkout.

The work scene is `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Greater Wasteland remains the sole production build scene. Continue bounded Camp Loop implementation and author review; this request does not promote experimental locomotion into production or resume unrelated terrain work. Use unarmed motions. The visible capsule is a placeholder, not a reason to replace the working rig or physics controller.

Read the [expanded-gait evidence](../../Evidence/Gameplay/LocomotionImplementation20261014/ExpandedGaitReview20261015/README.md) and [expanded-gait plan](CAMP_LOOP_EXPANDED_GAIT_REVIEW_2026-10-15.md) next. The earlier [locomotion implementation plan](CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) supplies history; its older speed tables and pending forward-feel statements are superseded by the accepted setup below.

## Accepted forward setup

| Stage | Nominal physical speed | Current motion |
| --- | ---: | --- |
| Idle | 0 m/s | Existing idle |
| Walk | 1.4 m/s | MCU walk |
| Jog | 3.0 m/s | MCU jog |
| Run | 4.0 m/s | Complete MCU run |
| Fast run | 5.0 m/s | `SprintArmDriveReview.anim`, the accepted former sprint |
| Sprint | 6.0 m/s | `FullSprintStandardReview.anim`, a separate complete Standard sprint |

Gamepad is primary. Stick deflection continuously blends ordinary gaits; full stick reaches **fast run at 5 m/s**. Hold **RB / right shoulder** for the **6 m/s sprint**. These are blend anchors, not digital stick zones. The walk/jog blend ends at 2.7 m/s, leaving a pure-jog interval through 3.0 m/s. Preserve these accepted values while adding the new behavior. Keyboard stage design is deferred; retain existing ordinary/Alt/Shift behavior.

The new sprint is the complete Unity Standard `m@SprintForwards.fbx` take, phase aligned and repeated twice to match the established four-contact cycle. Horizontal root translation is removed; the physics motor owns travel. Original imports and metadata are intact. Do not return to searching for a missing half of the sprint: full-take and equipped-rig audits already established its provenance. Forward thigh drive and arm travel are stronger than fast run, with about 29% higher cadence at the selected speeds. The author now accepts this comparison.

**Acceptance is bounded.** The expanded family runs only on the AP02 flat gait deck. `ForwardReviewSupportAvailable()` in [the driver review partial](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.ForwardReview.cs) requires the owned Camp Loop player/station, support beneath AP02, a support angle at most 0.25 degrees and a position inside its footprint with capsule margin. Action, crouch, mounting and pose ownership also control presentation. Baseline locomotion remains elsewhere. Both `qualified` and `terrainQualified` are false. Extending this operating envelope is part of the new work; deleting the gate or flipping flags is not qualification.

Private selection under `Assets/_Project/Settings/AnimationPlayground/PrivateBindings/`:

- `PlayerBindings.asset`: `forwardLocomotion` remains null; `forwardReviewOverride` selects `ExpandedForwardGaitReview.asset`; `forwardReviewOverrideRequired` is true.
- `ExpandedForwardGaitReview.asset`: schema 2, five moving entries plus idle; paired with `ForwardLocomotionClipsStageAlignedCandidate.asset` and the current foot-projection data.
- `ForwardLocomotionDistanceAlignedCandidate.asset`: retained schema-1 three-gait comparison; do not overwrite it to implement the new direction contract.
- `ForwardTurnCandidate.asset`: unqualified stationary-root turn profile; it does not describe the later adapted native trajectory.
- `AirborneProfile.asset` and `LowMotionFittedProfile.asset`: existing bounded qualified families, selected through the current binding. Qualification does not cover the new six-stage combinations automatically.

Preserve one `AnimationPlayableOutput`, the completed-displacement gait clock, explicit binding precedence and visible failure for required missing private data. The expanded branch uses a nested six-input mixer at outer input 24. Its forward channels do not already implement a directional blend space. Do not reorder serialized `TopDown3DLocomotionClipRole` entries or insert a second Animator/controller/root-motion writer.

Implementation commits to inspect: `70cb896c8` (expanded gamepad gaits), `b546349b5` (equipped sprint effort audit), `58329fa76` (verified author installation). The startup repair is already present; do not recreate the earlier static-pose failure.

Runtime entry points are under `Assets/_Project/Scripts/Runtime/TopDown3D/`: the `TopDown3DPlayerAnimationDriver` main/`ForwardReview`/`LaboratoryLocomotion` partials, and the `TopDown3DPlayerMotor` main/`LaboratoryLocomotion`/`AnimationPlayground` partials. Profiles, the action coordinator, session partials, airborne/low-motion owners and `AnimationPlaygroundInputFactory.cs` are in its `AnimationPlayground/` subfolder. Shipped controls live in `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`; extend the owned lab clone rather than changing unrelated production bindings.

## State at handoff preparation

A background query at **2026-10-15 20:47:47 UTC** found the author Editor in **Edit Mode**, not compiling, with CampLoopTesting loaded and its in-memory scene clean. The loaded selection was the schema-2 expanded profile with speeds 1.4/3/4/5/6, both forward qualification flags false, and a qualified airborne profile. `Uninitialized`, inactive forward presentation and null runtime status are normal before this session starts in Play Mode. They do not establish a broken animation graph.

A separate current compiler query at **20:48:59 UTC** reported no compilation failure and **zero current console errors or warnings**. Use `console_status.groundTruth` for current console state; cumulative bridge counts contain historical messages. These observations are a snapshot, not a guarantee about the next session.

At preparation, HEAD was `28a1547f8`, and `main` was 159 commits ahead of `origin/main`. Foreign dirty work included CampLoopTesting, `TopDown3DCampLookEditor.cs`, the `CampOptics20261014` CombinedOptics/CombinedTwilight assets, untracked Fused rocks, `Assets/_Project/VFX/Textures.meta` and `outputs/`. Preserve all of it and re-inventory ownership. The scene was clean in the Editor while different from Git; those are separate facts. Do not restore it from HEAD or an older validation copy.

## What already exists and what remains

### Strafing

Four source imports exist in `Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/`: `StrafeWalkLeft.fbx`, `StrafeWalkRight.fbx`, `StrafeRunLeft.fbx`, `StrafeRunRight.fbx`. Derived `DirectionalStrafeLeftTwoCycles.anim` and `DirectionalStrafeRightTwoCycles.anim` already exist under `Mixamo/Derived/`. The [import receipt](../../Evidence/Gameplay/AnimationPlaygroundImplementation20261013/Imports/README.md) records calibrated two-cycle derivatives and the twelve-role profile. Inventory exact current assets before downloading or deriving replacements.

The legacy driver already weights `DirectionalLeft`/`DirectionalRight` using local desired velocity and yaw rate in `UpdateLocomotionTargets()`. That is a turn-gait blend, not a completed independent-facing strafe controller. The standing motor normally faces its travel velocity. The expanded six-channel forward branch also replaces the legacy weights while active. Adding clips to a list will therefore not make the accepted gait family strafe.

First define the standing facing/travel contract, then connect the existing clips through the current motor and single graph. Measure lateral stride, phase, fixed-sole contact, blend rates and diagonal behavior rather than applying the forward stride map to lateral travel. Audit backward-motion coverage explicitly; do not play forward running backwards or claim left/right sources cover backward locomotion.

**No standing strafe control has been chosen.** RB is sprint; A is jump; LT is camera look-ahead; LB opens the radial menu; right stick controls camera orbit; right-stick press pings danger; laboratory left-stick press crouches and RT attacks. Audit the shipped action asset and owned lab clone before proposing a facing lock or strafe mode. Preserve action IDs/overrides, menus and camera behavior. If a product choice is needed, ask one concise gamepad-control question while continuing source/calibration work. Keyboard staging remains deferred.

### Turning

Distinguish stationary pivots, ordinary moving steering, reversals and strafing. Read [native-turn calibration](../../Evidence/Gameplay/LocomotionImplementation20261014/TurnNativeCalibration20261015/README.md), [tangent constraint](../../Evidence/Gameplay/LocomotionImplementation20261014/TurnTangentStabilization20261015/README.md) and its later [passive author import](../../Evidence/Gameplay/LocomotionImplementation20261014/TurnTangentAuthorImport20261015/README.md). Earlier dialog/static-pose statements in those receipts are historical.

Corrected private one-shot/native candidates retain the complete 1.50-second approximately 180-degree turns, disable inappropriate looping and remove the mirrored right clip's half-cycle offset. The native take travels about 134.2 mm. A fitted C1 actor trajectory has bounded speed/acceleration and zero endpoint velocity; using it alone still produced about 10 cm cumulative foot drift.

`AnimationPlaygroundTangentFootPose.cs` is installed **passively and unbound**. Its bounded 20 mm tangent correction preserves ankle vertical position, orientation and limb lengths without owning the physics root or animation time. Eighteen focused checks and 4,350 equipped adapted-trajectory poses supported the candidate: selected-point excursion at most 0.313 mm, cumulative travel about 11.33–11.62 mm and correction below 14.1 mm. This is offline adapted-trajectory evidence, not motor/collision/terrain acceptance.

Integrate an explicit adapted-trajectory contract with the existing motor, collision response, braking, cancel/reset and action ownership. The old stationary `ForwardTurnCandidate` must not be relabeled or qualified as that new contract. Reuse `Tools/Diagnostics/CampTurnNativeImport.cs`, `CampTurnContactQualification.cs`, `CampTurnProfileAuthoring.cs` and `camp_turn_root_curve.py` where applicable. Preserve their immutable recovery inputs.

### Slopes

The author explicitly chose **a slower uphill gait on very steep walkable ground**. [Uphill policy](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardUphillGait/README.md) is already implemented in `TopDown3DPlayerMotor.LaboratoryLocomotion.cs`: a continuous 32-to-40-degree transition toward the 1.4 m/s walk cap, with completed support and bounded look-ahead/braking. Preserve flat/downhill targets, partial-stick intent, precedence and smooth release. Older 1.4/2.7/3.8 tables describe historical tuning.

`AnimationPlaygroundTerrainPose.cs` and `AnimationPlaygroundGarmentFloor.cs` are gated experiments. Read [terrain runtime](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTerrainRuntime/README.md) and the later [reach audit](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTerrainReach/README.md). The latter still rejects installation: 720 unavailable poses, 115 garment-over-budget samples at 48 degrees, severe penetration when correction is rejected and rapid knee motion needing scrutiny. A single approximately 448 microsecond warm Editor sample does not establish stable Player performance.

Those sweeps used the older family up to 3.8 m/s. Re-measure the new six-stage family, fast entry/braking, downhill and sidehill travel, lateral/diagonal travel, turns, uneven native support and crest/seam changes. Start with a bounded mild-slope envelope and expand only on evidence. Retain the +80/−30 mm visual-host limits, equipment constraints and explicit unavailable results; do not hide failures by widening correction limits or dropping difficult transients. Keep physics support separate from presentation and preserve a safe fallback outside the qualified envelope.

### Jumping, falling and landing

Do not rebuild jumping from scratch. The [airborne receipt](../../Evidence/Gameplay/AnimationPlaygroundImplementation20261013/AirborneQualified/README.md) records twelve derivatives and the owned takeoff/rise/fall/approach/landing/settling system. `TopDown3DAnimationPlaygroundSession.Airborne.cs` routes jump through the existing action coordinator. Rigidbody gravity owns the flight arc; the derivative clips remove the source's second vertical arc. AP04 is the existing jump fixture. A qualified low-motion family now supplies an optional selected `FallRollLanding` recovery; the older airborne receipt's pending-roll statement is superseded by the current hookup.

**Audit immediately:** the selected private airborne profile still has `maximumPlanarSpeed: 3.8`. Air control moves current planar velocity toward move direction times that value. This predates accepted fast run at 5 and sprint at 6 m/s. It can alter momentum after takeoff; decide and qualify preservation, air-control limits, braking and landing recovery before changing the number. Facing is captured at takeoff. Establish how that interacts with the new strafe/turn policy.

Verify all entry gaits, standstill, ledge fall, held/repeated jump, low ceilings, slopes/edges, interruptions and landing back into forward/directional locomotion. Rebase the distance/contact clock and emit takeoff/landing effects once under the existing sequence owner. Preserve current fall-damage and reset behavior unless a separate design change is authorized. Keep mounting, crouch, low-motion and other action conflicts explicit.

## Recommended implementation slices

1. **Refresh and protect the baseline.** Inspect status, current private assets and loaded Editor APIs; retain exact recovery bytes. Verify the input/facing contract and audit the airborne 3.8 m/s mismatch before designing the shared interfaces. Write a short updated slice plan grounded in the files above. Do not rerun the completed expanded-gait installer.
2. **Deliver real flat-ground strafing first.** Add owner-scoped facing control and left/right movement in the existing motor/driver, preserve the accepted forward branch, calibrate directional contacts and document unsupported directions. Provide a bounded Camp Loop review route and visible state feedback. Confirm the gamepad control choice without taking over another action.
3. **Integrate turning.** Implement stationary/moving/reversal transitions with the correct trajectory contract and bounded tangent constraint where justified. Include collision/cancel/ownership release, foot-contact continuity and directional-to-forward handoffs.
4. **Extend slope support deliberately.** Qualify each surface/heading/speed envelope with equipped Booter and the accepted family, including uphill anticipation, downhill and lateral cases. Revise the AP02-only selection/fallback through explicit scoped configuration after proof; keep production selection closed.
5. **Finish jumping across the new combinations.** Reconcile momentum and facing, then qualify takeoff/landing and return-to-locomotion from all new directions, speeds and supported slopes. Reuse the existing airborne/low-motion owners and AP04 fixture.
6. **Prepare a combined author review.** Reuse AP02/AP04 and add only the necessary authored slope, corner and directional fixtures with stable station IDs, safe returns and reset cleanup. Keep reusable props as prefabs. Give short gamepad instructions and identify what remains experimental. The author performs Play Mode feel testing; iterate on that feedback.

The slices may be reordered for a demonstrated dependency. Commit verified, task-owned increments with documentation and private recovery receipts; do not attempt all families in an unreviewable scene rewrite.

## Verification and limits

The accepted forward candidate previously passed **78 focused EditMode checks**, an equipped one/two/four-bone pose screen and an actual-driver contact screen. These are retained baseline results, not new-family proof. At 6 m/s the sprint still measured approximately **30.05 mm persistent-point excursion / 67.54 mm cumulative travel**, above the approximately 20 mm contact aim. Author feel acceptance does not close that qualification gap.

Use existing tests under `Assets/_Project/Tests/Editor/`, especially `AnimationPlaygroundExpandedGaitTests`, `AnimationPlaygroundForwardLocomotionTests`, `AnimationPlaygroundInputTests`, `AnimationPlaygroundTurnProfileTests`, `AnimationPlaygroundTangentFootPoseTests` and `AnimationPlaygroundAirborneTests`. Add focused tests for the new ownership, facing, momentum and support contracts. Re-run affected motor/player/action/low-motion/reset checks when their inputs change. Do not treat offline poses as proof of collision-driven gameplay.

For each slice, pair exact loaded source/private-asset hashes with current compilation and focused test results. Check one graph output, finite continuous weights/time, actual-displacement cadence, bounded playback rates, fixed foot-point excursion **and cumulative travel**, floor/height bounds, full cloak/rigid-kit geometry at one/two/four influences, cancel/reset/menu/device removal and graph disposal. Report unavailable poses and performance limits explicitly. No agent-driven gameplay smoke tests or Play Mode are authorized by this handoff.

World-system scope is unchanged: authored fixed terrain is always loaded; no terrain generation or streaming is added. Station identity remains authored and stable. Gait, facing and pose owners are transient, released on reset/disable/unload; no generated-object identity or persisted runtime delta/save-format change is needed. If implementation introduces one, stop and design that boundary explicitly.

## Local recovery and background tools

Animation payloads/private profiles and raw pose/mesh tracks are local/ignored. A fresh Git clone alone is insufficient. The recovery root is `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/`. Relevant immutable bundles include `ExpandedGaitReview20261015`, `ExpandedGaitEffort20261015`, `ExpandedGaitAuthorInstall20261015`, `TurnNativeCalibration20261015`, `TurnTangentStabilization20261015`, `ForwardTerrainRuntime20261014`, `ForwardTerrainReach20261014` and `PHY1AirborneQualified`. Follow the linked receipts to verify manifests before reuse. Preserve Blender and terrain sources locally; this task authorizes no external publication.

The existing isolated project is `D:/BooterBigArmValidation/AnimationPlayground20261013`, marked by `.animation-playground-validation-project`. It has an older scene/dependency snapshot and a candidate-only event declaration matching the author's resource-study fields. **Never copy that stub, its whole scene or the old dependency tree into the author project.** Transport only reviewed task-owned changes and verify them against current author dependencies. Do not overwrite existing checkpoint/output directories or assume preparation/install scripts are idempotent.

Use the installed Unity CLI at `%LOCALAPPDATA%/Unity/bin/unity.exe`, with an explicit project path for every connected command. At observation the author used port 7800 and the isolated candidate port 7801; rediscover them rather than relying on these numbers. Safe read-only starting commands in PowerShell:

```powershell
$locomotionProject = 'D:/Arc & Dust/Project'
$unityCli = Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
git status --short --branch
& $unityCli editors running --json
& $unityCli command console_status --project-path $locomotionProject --json
```

Consult the current command help and automation guide for `eval`, guarded `run_script` and focused `run_tests`. Use a single filter per test call; repeated filter flags were rejected in this bridge version. Check both transport success and the nested command result, and establish that the loaded assembly exposes the intended revision. Do not launch a second Editor against an open project.

Preserve the user's foreground application. Never focus Unity, send keystrokes, enter Play Mode or dismiss native dialogs automatically. Do not save/reload the scene or restart/kill the author Editor to recover automation. Planned task-owned scene changes need current scene/dirty-state checks and recovery first. Avoid broad refreshes that can trigger external-scene-change dialogs. The author previously chose **Reload**; the current disk scene has subsequent work. Inspect current state and preserve it rather than assuming an older Ignore/recovery instruction still applies. If a dialog genuinely blocks required work, explain the exact issue and continue independent repository/candidate work.

For author testing, the existing route is **author starts Play Mode → F9 → Reset to gait deck**, then analog movement and RB. Gamepad Select opens the animation palette; A is jump. Do not diagnose an Edit Mode session as failed because these runtime controls are inactive. Include a new explicit route/instructions when a later slice extends review beyond the flat deck.

## Handoff completion

This preparation inspected current driver, motor, input, airborne/low-motion bindings, private selections, retained turn/terrain evidence, live background readback and Git ownership. It changes this handoff and the two expanded-gait status documents only. Relative links, referenced paths and the exact documentation diff are checked before the task-owned commit. No runtime, scene, private asset, package or source animation is changed by this handoff.

The incoming agent should begin with the refreshed inventory, the standing facing/strafe contract and the airborne speed mismatch, then implement the bounded slices above. The author accepted forward appearance and requested the next animation families; preparation stops here because this turn requested the handoff for that next agent.

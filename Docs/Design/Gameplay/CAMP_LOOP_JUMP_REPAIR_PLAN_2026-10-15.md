# Camp Loop jump repair plan — October 15, 2026

Implementation steps 1–5 are complete and installed for author testing. Step 6 remains author-owned. This document defines the work, defaults and completion gates. Read the [initial audit](CAMP_LOOP_JUMP_AUDIT_2026-10-15.md) for J01–J10 and the [continued study receipt](../../Evidence/Gameplay/JumpRepairStudy20261015/README.md) for measurements and limitations.

The preferred repair is to make jumping a continuous locomotion policy inside the existing single Rigidbody owner, then rebuild its presentation around matching source phases. Keep the coordinator's ownership and once-only effects. Ordinary takeoff and landing must stop inheriting the braking rules of unrelated laboratory actions. Longer blends or additional animation downloads alone cannot fix those movement rules.

On October 16 the author reported excessive float and hang time. The [weightier jump follow-up](../../Evidence/Gameplay/JumpWeightTuning20261016/README.md) is installed for retesting: jump-local gravity is 1.6×, with the existing impulse and world-gravity setting preserved. Its lower arc and shorter airtime supersede the original physical calibration without changing accepted grounded gaits.

## Execution checklist

**Target behavior:** stationary, walking, running and LT directional jumps respond promptly, carry movement through the air, land on native terrain and return smoothly to the appropriate accepted gait. The main Camp Loop is the delivery target. Isolated platforms remain diagnostic aids.

**Exact installation boundary:** the current delivery is `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`, on its main native-ground camp area. Its session owns the accepted gait bindings and currently refuses other scene identities. Greater Wasteland remains the enabled production scene; promotion of this study's complete gait/session installation is not claimed here. Preserve the guard and production scene instead of silently copying the laboratory into it.

**Protected baseline:** preserve accepted forward and directional sources, speed bands, LT facing lock and camera look-ahead, input IDs, current jump impulse and world gravity, fixed terrain and unrelated action behavior. Start with fixed-height jumping. Use 100 ms buffer/coyote windows and a maximum 100 ms eligible-request-to-impulse budget as provisional implementation targets, subject to author feel testing. These provisional values are now implemented; final tuning awaits author testing. The visual entry blend takes 120 ms and may finish after the 80 ms impulse marker without delaying takeoff.

1. **Freeze the baseline and isolate the jump policy.**
   - [x] Record current task inputs and main Camp Loop bindings; preserve unrelated dirty work and the author's unsaved scene.
   - [x] Add an explicit jump movement/ownership policy within the existing motor/coordinator. Keep one Rigidbody writer and leave default action stop semantics intact.
   - **Exit:** the implementation boundary is limited to jumping and its locomotion handoffs; no package, world, gait-source or scene-regeneration changes are needed.
2. **Repair momentum and physical support first.**
   - [x] Preserve completed planar velocity through accepted acquisition, rejected acquisition rollback and ordinary release.
   - [x] Continue grounded movement during short anticipation; distinguish walkable support from walls and retain valid wall/ceiling response.
   - [x] Separate completed observations from upcoming commands; keep the upward impulse once-only and gravity physically owned.
   - **Exit:** focused command/ownership checks establish the new contract without changing other actions. Actual terrain trajectory acceptance belongs to step 6; do not treat a mocked movement check as that proof.
3. **Repair touchdown and moving run-out.**
   - [x] Confirm capsule-aware walkable contact once, using a contact-associated impact record for recovery/damage.
   - [x] Remove compulsory braking/full-clip/idle settle from ordinary moving landings; neutral landings use ordinary deceleration. Keep justified hard recovery separate.
   - [x] Hand motion, contacting-foot information and gait phase back to live locomotion; emit contact effects at touchdown.
   - **Exit:** landing state/ownership/event checks pass, with no global change to roll or other Recovery-mode actions.
4. **Rebuild the continuous jump presentation.**
   - [x] Reuse complete existing jump families, including their unused descent, with a shared height correction that preserves compression and absorption.
   - [x] Blend advancing poses, interpolate presentation between physics ticks and remove the airborne synthetic-floor correction.
   - [x] Measure final equipped output and locomotion re-entry. Obtain additional Mixamo motion only for a demonstrated responsiveness, directional or run-out gap.
   - **Exit:** offline pose/transition checks cover all boundaries and supported skinning qualities; report remaining visual uncertainty explicitly.
5. **Finish input and main-loop integration.**
   - [x] Implement bounded jump buffering/coyote acceptance, once-only consumption and reset/menu/device invalidation.
   - [x] Preserve held LT through the normal jump/landing handoff; separate preview selection from gameplay variant selection.
   - [x] Verify that the actual main Camp Loop uses the repaired path, with correct binding and lifecycle cleanup. Account for Greater Wasteland integration through the established production route.
   - **Exit:** code, bindings and focused non-smoke checks are complete; accepted gait contracts still hold. Prepare the precise author test checklist and known limitations.
6. **Author testing and final acceptance.**
   - [ ] Test the main loop on native ground across the movement, contact, interruption and render-rate matrix below; record actual trajectory and visible transition results.
   - [ ] Fix reproduced defects within the same scope and repeat only affected checks.
   - **Exit:** the author accepts jump responsiveness, movement continuity and landing feel. Until then, report readiness for testing separately from gameplay acceptance.

Keep implementation commits separated by coherent concern, with only verified task-owned files staged on `main`. Work in the background and preserve the user's foreground application. Automated gameplay smoke tests remain prohibited without an explicit request; author-owned gameplay testing is step 6. Implementation readiness is compilation/import plus the focused command, ownership and offline pose gates. Real trajectories and feel are accepted only after step 6; they are not required as an unowned prerequisite before author handoff.

### Plan-audit corrections governing implementation

- Landing identity is **per distinct touchdown**, with a new contact ID after actual support departure, within the session epoch. Landing, losing support and falling again must permit another impact; sustained contact must not repeat it.
- Damage keeps the existing **vertical** incoming-speed metric and the existing threshold/multiplier. Use the fresh walkable-contact collision record's relative vertical magnitude on fixed terrain, gated by departure/touchdown; do not replace the metric with contact-normal speed or use the flight's peak. The completed-contact/solver timing remains part of the author trajectory evidence.
- Validate short-anticipation source feasibility before binding the replacement. The preferred bounded experiment trims the existing take's prelaunch lead-in rather than speeding its whole anticipation eightfold; use a different source if equipped transition evidence rejects that window.
- Reauthor private animation/profile data only in the marked candidate with explicit source importer hashes. Invalidate qualification while authoring, replace the legacy exact-flat-hip assertion with the shared-source-height/continuity contract, and restore readiness only against the new candidate evidence. Preserve asset GUIDs and retain paired recovery bytes before author installation.
- The table below lists source boundaries and final claims, not additional physical-test gates before step 6. Every pre-handoff claim must name its static, command or offline pose proof boundary.

The sections below retain the pre-implementation audit and proposed contracts. The [implementation receipt](../../Evidence/Gameplay/JumpImplementation20261015/README.md) records the current behavior, exact proof boundaries, private recovery and author test checklist.

## What the continuation established

### J11 — P2: A rejected jump can still brake the player

[`AnimationPlaygroundActionCoordinator.Acquire`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundActionCoordinator.cs#L109) first acquires motor ownership and only then attempts presentation acquisition. Motor acquisition clears horizontal velocity; if presentation refuses, rollback calls `ReleaseAnimationMotion`, which clears it again. Thus a false return is not guaranteed to leave running movement unchanged. This is a source-confirmed failure path, not a reproduced gameplay failure.

**Repair contract:** make jump acquisition transactional. A rejected request leaves velocity, gravity, collider, facing-lock state and prior owners intact. A successful request preserves completed horizontal movement. Keep the existing stop semantics for non-jump actions.

### J12 — P2: Airborne blend projection can apply an artificial floor

[`ApplyPoseBlend`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.PoseBlend.cs#L101) invokes `ProjectLaboratoryFootPlane` even when the airborne controller has disabled ordinary animation foot projection. [`TryEvaluateNativeCampGround`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.NativeGround.cs#L43) refuses native-ground adaptation when the motor is airborne. The fallback compares the sampled minimum against an actor-relative feet plane and can lift the visual pose, or reject correction above 18 cm. It is not a measurement of the real floor below an airborne capsule.

This code path can interfere with a repaired shared source-height policy. Its actual contribution to the author's visible jump has not been measured. Do not claim a measured 18 cm snap or relax the envelope to conceal a mismatch.

**Repair contract:** fade actual support correction at lift/contact; do not ground-fit airborne poses to the capsule's standing feet plane. Evaluate the combined pose before its single final terrain/equipment correction pass. Preserve ordinary accepted grounded correction.

### J13 — P2: Landing severity is peak fall speed, not contact-associated speed

[`ObserveMotion`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L122) accumulates the greatest downward velocity during the fall. That value later selects recovery and drives damage. An intervening wall/ramp deflection can reduce actual impact speed without reducing the accumulated value. An illustrative downward-speed sequence of 9→4→2 m/s would retain 9; under the existing 6.5 m/s damage threshold and 10 HP per excess m/s, that means 25 HP instead of zero. This is an analytical counterexample, not a measured collision.

**Repair contract:** associate severity with the touchdown contact and a validated pre-impact velocity record. Keep the existing damage tuning during the first repair. Do not substitute post-solver zero velocity, or assume `Collision.relativeVelocity` guarantees a particular solver timestamp. Record and verify the collision/command ordering, including wall and ground contacts in the same step. Unity documents relative linear collision velocity, but its API page does not establish that timing guarantee. [Unity collision velocity reference](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Collision-relativeVelocity.html).

### Source finding: matched descent sections already exist

All three existing complete jump sources include an unused section between the current rise endpoint and landing start. At both boundaries, all **55 measured body-muscle channels match exactly** against the selected derived clips. These are source-curve comparisons; they do not prove rendered position, velocity, hands, equipment, or contact continuity.

| Source family | Existing recipe lift→apex | Unused apex→contact | Rate if spread over the ideal physical half-flight |
| --- | ---: | ---: | --- |
| Standing | 333 ms | 250 ms | rise 0.711×; descent 0.533× |
| Running | 278 ms | 229 ms | rise 0.593×; descent 0.488× |
| Unarmed | 333 ms | 300 ms | rise 0.711×; descent 0.640× |

The markers are the **existing authoring recipe landmarks**, not newly verified biomechanical events. Their nearest 120 Hz retargeted samples and native hip maxima are in the receipt. The rates use the unchanged 4.6 m/s impulse and gravity magnitude 9.81 m/s²: ideal apex time 469 ms, same-height flight 938 ms. They are timing calculations, not tested playback recommendations. Half-speed running descent could look sluggish.

Use these complete takes as the first repair candidates. Their matching limb endpoints make them a better starting point than inserting a generic fall and approach into every normal jump. Actual source-height removal, phase warping and final rig continuity still need qualification. The 633 ms standing anticipation also needs a suitable short anticipation window or a better source; compressing the whole section into 80 ms would be about eight times playback speed.

## Proposed movement contract

These numerical responsiveness/grace budgets are provisional engineering targets for later author testing. Preserve the current impulse, gravity, accepted gait speeds and active 6 m/s air limit initially. Keep fixed-height jumping for the first repair; release-sensitive height is a separate design decision.

| State | Movement and exit contract | Presentation contract |
| --- | --- | --- |
| Grounded | Existing accepted locomotion and LT facing lock. Queue a valid jump edge once. | Existing forward/directional gait source, speed and phase rules. |
| Anticipation | Continue grounded movement, input and support solving. Target no more than 100 ms from eligible request to impulse. Apply the impulse once. | Blend from the advancing current gait into a short compatible anticipation; select by motion, not palette preview. |
| Ascending | Preserve launch momentum; use the existing bounded air steering. Gravity owns vertical motion. Walls/ceilings remain physical constraints. | Advance a matching family rise with physical progress; smoothly release grounded correction. |
| Descending | Use actual completed motion and support observations. Predicted contact is presentation information, not touchdown. | Use matching descent for normal jumps; generic sustained fall only when the trajectory needs it. |
| Soft landing | Confirm new walkable contact. Keep held movement and allow locomotion to continue immediately. Neutral input uses ordinary deceleration. | Absorption overlays/blends into the live matching gait; no compulsory full landing clip, idle settle or phase-zero restart. |
| Hard landing | Separate explicit severity/recovery policy, with a bounded lock only when justified. Preserve once-only damage. | Qualified severe-impact recovery; roll remains a deliberate separate mechanic. |

A jump-specific policy must opt into momentum-preserving ownership. `Recovery` alone is insufficient to identify it: rolls and other actions share that mode. Avoid changing `TryBeginActionConstraint` or release behavior globally.

Use the existing grounded support/tangent solver during anticipation and normal run-out. During flight, retain one motor writer and physical collision response; handle a wall's blocking normal while preserving permitted tangential motion. A sweep hit needs semantic support/blocker classification. Unity's `SweepTest` reports the closest hit in the requested direction; it does not supply the project's walkable-support policy. [Unity sweep reference](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rigidbody.SweepTest.html). Simply ignoring every walkable hit, switching all casts to one tangent, or adding a second character controller would leave collision gaps.

Split **completed observations** from **next-step commands**. The current animation snapshot combines prior-step displacement with velocity just assigned for the upcoming step. Record completed body position/velocity/support with tick and epoch, retain the intended command separately, and derive collision response, landing and phase handoff from the appropriate observation. Publish the takeoff transition consistently rather than leaving subscribers with a stale supported snapshot after the impulse marker.

Touchdown needs capsule-aware walkable support/contact, a departure/landing edge, contact normal and point, and an associated impact record. The centre-foot 30 mm ray must not veto valid rounded-capsule contact on a permitted slope. Prediction can prepare the pose; only confirmed contact commits landing effects/damage. Select the contacting foot from the pose/contact policy instead of always emitting left after the blend.

Add a small bounded input buffer and coyote window, initially about 80–100 ms each, using one gameplay clock. Consume each request once and invalidate it on menu/device/reset/epoch changes. An accepted coyote jump must not be rejected later by a redundant grounded-only impulse marker. Allow a buffered next jump to interrupt the tail of an ordinary landing. Keep menu/reset neutral gates; preserve held LT facing lock across a normal jump without requiring release/repress.

## Proposed animation contract

1. Keep each normal jump in one source family from anticipation through descent and absorption. Prefer one corrected continuous take with semantic landmarks; contiguous derivatives are acceptable if they share the same correction function and endpoint derivatives.
2. Remove the authored global flight translation once. Preserve compression, residual hip motion and absorption; independently fitting each airborne hip and grounded sole creates new seams. Measure actual corrected positions and velocities rather than treating equal muscle curves as a full continuity pass.
3. Map ascent to physical progress and descent to a stable contact forecast. Clamp source phase to advance monotonically; noisy terrain rays must not rewind the pose. Unexpected contact can interrupt presentation safely. Long falls and ceiling hits need explicit entry rules rather than endless replay of a short normal descent.
4. Continue both outgoing and incoming pose time during a blend, with a smooth blend curve and duration appropriate to the phase. Interpolate fixed-step state for rendered frames. Do not freeze the outgoing gait or use the same 120 ms transition for every boundary.
5. Keep one Animator/output and one final terrain/equipment correction path. Blend the action with a live locomotion underlay before final IK/garment work; independently calling two fully corrected presentation branches would apply corrections twice. Preserve or deliberately seed gait phase from contact side and completed movement at re-entry. Do not advance grounded distance from intended airborne velocity.

Use the current calibrated rig for initial source work, then qualify the actually equipped player at all supported skinning qualities. The fresh source study used **OneBone** and the imported target mesh without separately attached equipment. It did not evaluate controller blends or prove the complete player's appearance.

Additional Mixamo assets are conditional. Seek a short neutral anticipation, directional jump or moving landing only if the current family cannot meet responsiveness, direction and equipped continuity gates after correction. Define the missing motion and timing before download. Do not replace accepted grounded forward/strafe sources to hide a jump defect.

## Bounded implementation sequence

| Slice | Likely file boundary | Contract to establish; physical acceptance is step 6 |
| --- | --- | --- |
| 1. Jump ownership and motion | `AnimationPlaygroundActionCoordinator`, `TopDown3DPlayerMotor.AnimationPlayground`, `AnimationPlaygroundAirborneController`; narrowly shared motor support helpers | Acquisition/rejection/release preserve expected momentum; one Rigidbody writer; native support cannot cause an artificial stop; walls/ceilings still block. Defaults for unrelated actions unchanged. |
| 2. Completed contact and run-out | Same motor/controller boundary; `TopDown3DPlayerMotor.LaboratoryLocomotion`, `AnimationPlaygroundGroundedMotion` | Confirmed contact once; no peak-speed damage error; moving soft landing continues; neutral landing decelerates normally; correct gait observation and contact-foot handoff. |
| 3. Source and rendering | `AnimationPlaygroundAirborneAuthoring`, `AnimationPlaygroundClipDerivatives`, airborne profile/contiguous derivatives, driver `PoseBlend`, `AnimationPlayground`, `LaboratoryLocomotion`, `Directional`, `NativeGround` partials | Shared source-height reference; advancing blend inputs; render interpolation; no airborne synthetic floor; actual equipped final-pose continuity. Preserve existing asset GUIDs and grounded gait policy. |
| 4. Input and integration | `TopDown3DInputRouter.AnimationPlayground`, session airborne/low-motion routing and controller request handling | Buffer/coyote once-only; preview selection isolated; LT continuation; interruption/reset cleanup; no leaked gravity/owner/effects. |
| 5. Qualification and handoff | Relevant existing Editor tests/evidence and the main Camp Loop binding | Compilation/import, focused command/ownership regressions and offline equipped transition evidence establish readiness for testing. Author-owned main-loop trajectory/feel testing establishes final acceptance. Isolated platforms are diagnostic only. |

Avoid broad refactors, package upgrades, scene regeneration, or new movement assemblies. Camp interactions, turns, crouch/slide/roll, cover/traversal, mounted controls, menu ownership and reset remain explicit regression boundaries. The current UI/Cancel and destroyed-driver errors need separate lifecycle triage; they are not proof of the jump's root cause.

## Acceptance gates for the later implementation

- **Motion:** real Rigidbody trajectories through impulse, flight, contact and locomotion re-entry. Held unobstructed movement must not acquire a zero planar frame at acquisition, uphill support, normal touchdown or release. Compare against the existing grounded slope policy, not a claim of constant world speed on every incline. Include a wall glance with preserved tangent and a ceiling interruption.
- **Contact:** native uphill/downhill/sidehill, crest/ledge loss and permitted steep support, plus hard drops and compound wall/ground contact. Landing/damage/contact effects occur once per distinct touchdown within the epoch, including a second impact after support loss, with no early prediction effects or arbitrary 30 mm slope veto.
- **Pose:** measure same-timestamp stage-switch continuity on the final equipped rig, including root/hip/feet/hands/equipment and correction outputs. An initial switch-discontinuity target is 1–2 mm, subject to sampling precision and source qualification. Compare velocity continuity with the expected advancing source/blend envelope; never demand zero natural joint velocity. Verify 30/60/120 Hz rendering with the current 50 Hz fixed step.
- **Controls:** stationary, walk/jog/run/fast run/sprint; LT left/right/back/diagonal jumps; stick hold/release/reversal, near-contact presses, consecutive jumps, coyote exit, pause/world-speed changes, cancel/device/reset and failed presentation acquisition. Preserve input IDs, accepted speed bands, camera look-ahead and LT semantics.
- **Scope:** validate the actual main Camp Loop native-ground area in CampLoopTesting with current bindings. Preserve the separate Greater Wasteland production installation; this study repair does not claim promotion of its laboratory-only session/gaits. A test-platform pass is insufficient.

Future automated trajectory tests must be explicitly authorized; the repository reserves gameplay smoke/feel testing for the author. Existing manually posed FixedUpdate tests and isolated mesh sweeps can remain useful, but cannot serve as trajectory proof. No such gameplay tests ran in this continued audit.

World identity, chunk streaming, generated-object identity and persisted deltas are not affected: this proposal changes transient player motion/presentation on fixed, always-loaded terrain. Do not introduce terrain generation, streaming or save-format changes. Existing authored collision and permitted-slope constraints remain binding.

## Closeout

The authorized implementation is installed in the main native-ground Camp Loop and ready for author testing. Continuous existing sources replace the normal multi-clip jump, momentum and ordinary moving run-out use the jump policy, and input grace and contact ownership are implemented. The implementation receipt separates focused command/offline pose proof from actual trajectories and visual/feel acceptance. No Play Mode or gameplay smoke test ran; step 6 remains open. Accepted gait assets, scene content, production Greater Wasteland, packages and world persistence were preserved.

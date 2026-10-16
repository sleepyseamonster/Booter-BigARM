# Camp Loop jump audit — October 15, 2026

Audit only. The author considers strafing acceptable for now and reports poor jump blending, loss of running movement and incorrect landings. Preserve the accepted forward and directional gaits. No implementation, animation, profile, scene, package or control changes are authorized by this audit.

Jumping currently inherits the braking and recovery rules of laboratory actions. Those rules conflict with continuous running jumps. The strongest movement defect is the jump motor's obstacle sweep treating walkable terrain as a blocker, followed by cached loss of takeoff speed. Mandatory landing braking, velocity-clearing handoffs, frozen-pose blends and mismatched source-height policies compound the result. Replacing animations alone would leave these controller problems intact.

Continued audit: the [repair plan and J11–J13](CAMP_LOOP_JUMP_REPAIR_PLAN_2026-10-15.md) add rejected-acquisition rollback, airborne synthetic-floor projection, contact-associated severity, a measured existing-source reuse path and bounded implementation/acceptance contracts. Its [separate receipt](../../Evidence/Gameplay/JumpRepairStudy20261015/README.md) includes offline source-pose evaluation in disposable preview scenes; the original evidence boundary below remains the scope of the initial audit.

## Evidence boundary

- Inspected the active Camp Loop code, selected private profiles, twelve clip curves, input routing, action ownership, terrain projection, roll recovery and relevant existing tests. [Exact inspected file hashes](../../Evidence/Gameplay/JumpAudit20261015/inspected-source-hashes.json) identify the 77-file snapshot.
- Queried the existing author Editor in the background. It was in Edit Mode with CampLoopTesting dirty, compilation inactive and runtime module `c3a1e99e-5521-459a-82b2-8bd9717da120`. [Live selection and capsule sweeps](../../Evidence/Gameplay/JumpAudit20261015/live-selection-and-sweeps.json) record the selected native continuation and jump parameters. No scene was saved/reloaded or brought forward.
- Ran 960 **read-only geometry queries**, comprising hypothetical horizontal and local-tangent capsule casts at five existing native sites, eight headings, three speeds and four near-ground clearances. No actor moved, no physics stepped, no graph evaluated and no Play Mode or gameplay smoke test ran. These queries establish a collision-rule failure condition, not a recording of the author's exact jump. The eight actual resting-capsule sweeps at the Editor spawn were clear.
- Read current source curves directly. Curve units and normalized muscle differences below are **pre-retargeting source evidence**, not a measured rendered snap or joint acceleration. Auxiliary quaternion component differences are not used as pose-distance measurements.
- Inspected existing tests and their previous receipts; did not rerun them for this audit. The earlier 286 passing checks did not establish continuous physical traversal or animation feel.

## Findings

### J01 — P1: Walkable terrain can stop a run during takeoff

[`AdvanceAnimationMotion`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.AnimationPlayground.cs#L310) sweeps the capsule horizontally whenever vertical velocity belongs to Rigidbody gravity. Any hit becomes a blocker. It does not distinguish an uphill walkable floor from a wall or resolve motion along a contact surface. It subtracts a fixed 25 mm margin from the hit distance and clamps the entire planar vector to the permitted distance. Ordinary locomotion instead constructs a slope tangent before applying movement.

[`ObserveMotion`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L116) then replaces the cached jump `planar` velocity with the blocked snapshot. [`Intent`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L90) deliberately performs no steering/re-acceleration during Takeoff. A floor hit can therefore reduce speed to zero and keep it there until launch. After launch, held input only restores speed at 3 m/s²; neutral input preserves the reduced velocity.

The [native collision probes](../../Evidence/Gameplay/JumpAudit20261015/native-collision-probes.json) produced walkable terrain hits in **119/480 horizontal casts**, including **27 whose permitted speed becomes zero** under the current rule. All 119 had positive hit distances; these counts do not depend on initial-overlap zero-distance hits. At the spawn site, one 1.4 m/s forward cast with 5 mm support clearance hit native terrain at 45.51 mm, permitting only 1.025 m/s. Local-tangent casts still hit curved walkable support in 74/480 cases, with six zero-speed results, so projecting onto one plane alone is insufficient.

**Required correction direction:** separate support from blocking obstacles throughout anticipation and flight. Preserve completed motion, resolve genuine walls appropriately, and avoid permanently caching a support-induced braking result. Verify actual movement across native uphill/sidehill changes before accepting this fix.

### J02 — P1: Jump ownership clears horizontal momentum at acquisition and release

[`TryBeginActionConstraint`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs#L245) immediately writes zero horizontal Rigidbody velocity. `TryBeginAnimationMotion` acquires this constraint and initializes a zero motor intent. The airborne controller captures launch velocity beforehand and restores its cached intent on a later fixed tick, so this is an acquisition discontinuity rather than proof that every launch permanently loses all speed.

[`ReleaseAnimationMotionCore`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.AnimationPlayground.cs#L331) clears horizontal velocity again. Normal landing calls `Settle`, cancels the physical sequence, acquires another action constraint and eventually releases it. Thus movement cannot flow through the handoff even if a future landing clip looks correct.

**Required correction direction:** define a jump-specific momentum-preserving acquisition/release contract. Avoid changing the stop semantics of unrelated interactions, turns, slides, camp actions or previews. A running jump should hand completed velocity back to locomotion without an artificial zero-speed frame.

### J03 — P1: Every ordinary landing is a braking recovery followed by forced idle

[`Intent`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L96) brakes every reviewed Landing toward zero at **20 m/s²**, regardless of continued movement input. From 5 m/s, that is a 250 ms stop. The controller retains ownership for the complete landing clip, then [`Settle`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L191) adds a 120 ms stationary constraint. [`PresentStandingPoseBlend`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.PoseBlend.cs#L124) targets idle during this settle. Grounded distance/phase observations also reset under the action, so this is not a phase-matched return to the accepted moving gait.

| Selected landing | Clip duration | Nominal clip + 120 ms settle budget |
| --- | ---: | ---: |
| Running | 295 ms | 415 ms |
| Standing, including fast side/back jumps | 950 ms | 1,070 ms |
| Palette-selected unarmed | 867 ms | 987 ms |

These are controller/clip budgets, not measured gameplay recordings. Fixed-step rounding and same-tick event ordering shift actual boundaries by approximately one 20 ms tick. [Calculations](../../Evidence/Gameplay/JumpAudit20261015/audit-calculations.json) retain the inspected values.

**Required correction direction:** distinguish normal moving landings from deliberate hard-impact recovery. Preserve intended travel on ordinary touchdown and blend into the matching moving gait. Only a justified severe impact should impose a longer recovery lock.

### J04 — P1: Source-height policies disagree across the airborne sequence

[`AnimationPlaygroundAirborneAuthoring`](../../../Assets/_Project/Scripts/Editor/AnimationPlayground/AnimationPlaygroundAirborneAuthoring.cs#L78) removes the second airborne arc by fitting rising/falling clips to a reference hip height. Grounded takeoff, approach and landing derivatives use a different per-frame sole-height fit. The resulting source offsets do not share one continuous reference at all boundaries.

The current [clip-curve audit](../../Evidence/Gameplay/JumpAudit20261015/clip-curves-and-transitions.json) shows a **−0.4660 RootT.y curve-unit difference** from the falling loop's end to the approach clip's start. Their 55 matching body-muscle channels are almost identical at that boundary: mean absolute normalized difference about **0.002**. That combination points to a height-policy seam rather than a need for a different falling limb pose. It is not evidence of a literal 466 mm rendered snap; retargeting, blending and projection change the displayed result.

Other boundaries also need alignment: standing takeoff→rise has +0.0919 RootT.y units despite identical muscle endpoints; running landing→idle has +0.0983 units. Rising→generic fall and generic approach→variant landing have substantial body-muscle differences, with mean normalized deltas around 0.24–0.31. [Muscle-only measurements](../../Evidence/Gameplay/JumpAudit20261015/muscle-channel-endpoints.json) exclude quaternion and translation helper curves.

**Required correction direction:** retain a consistent actor-relative vertical reference while removing only the authored flight arc. Audit source boundary positions and velocities on the actual equipped rig. Preserve genuine anticipation/compression and landing absorption instead of fitting every stage independently to a synthetic standing floor.

### J05 — P2: Frozen snapshots and fixed-step presentation limit blend quality

Every [`Begin`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L73) takes the same 120 ms transition. [`TryCapturePoseBlend`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.PoseBlend.cs#L38) reconstructs the outgoing source mixture at fixed clip times and sets each cloned clip's speed to zero. This preserves a sampled pose, not its outgoing movement velocity. It also records raw mixer sources and a scalar visual height rather than the evaluated downstream terrain-IK pose.

The 98 ms running takeoff completes after about 100 ms, before its 120 ms entry blend finishes. The next stage captures that unfinished transition again. Takeoff always starts at source time zero, regardless of the current running gait phase or which foot leads. Pose time and blend elapsed advance from fixed-step coordinator callbacks; the action branch does not interpolate them for each rendered frame. The inspected timestep is about 20 ms, giving approximately 50 distinct action-pose updates per second even when rendering faster.

Approach uses ground distance rather than time to impact. A single 120 ms transition consumes much of the approach interval, especially on fast falls; another snapshot can replace it at contact. Terrain projection also changes eligibility when support is lost or regained, without an evaluated-pose continuity contract.

**Required correction direction:** use phase-appropriate transition durations and continuously advancing outgoing/incoming poses, interpolate render presentation, and explicitly preserve pose continuity when terrain correction changes. Measure final equipped output through the entire transition, including source position/velocity and correction changes. A single output is useful ownership proof but does not establish smooth animation.

### J06 — P2: Variant selection creates large and surprising responsiveness changes

[`TryJump`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L46) chooses running at only 0.6 m/s, so an ordinary 1.4 m/s walk already uses the running family. In the native directional review, travel more than 60 degrees away from captured facing substitutes the standing source while retaining the selected physical impulse. A 5 m/s sideways run therefore receives about **640 ms anticipation**, compared with **100 ms** forward. The standing landing lock is also much longer.

[`RequestJump`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Airborne.cs#L30) lets palette selection of `JumpUnarmed` override ordinary jumping; it introduces about 580 ms anticipation. Palette selection also controls whether roll landing is attempted. These laboratory choices leak into normal movement behavior.

**Required correction direction:** choose gameplay poses from completed speed/direction/posture and a coherent responsiveness budget. Separate preview selection from normal controls. Evaluate existing neutral sources after timing/height repair before deciding whether additional directional jump animations are necessary. No new download is justified solely by the current symptoms.

### J07 — P2: Landing detection is a proximity rule with slope and event-order gaps

[`AdvanceAnimationMotion`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.AnimationPlayground.cs#L321) computes supported state from a ground probe before that fixed tick's physics simulation, with a vertical-velocity threshold and a two-tick release suppression. [`ObserveMotion`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundAirborneController.cs#L116) additionally requires a centre-foot ray gap no greater than 30 mm. This is not direct contact-manifold evidence. At takeoff, the impulse marker updates `IsGrounded` after the published snapshot; this ordering needs explicit treatment in a continuous trajectory test.

For the inspected 0.2 m radius capsule, ideal planar slope geometry gives a centre-foot vertical gap of `r × (sec(slope) − 1)` while the rounded capsule is in contact. At 40 degrees it is about **61 mm**, exceeding the 30 mm landing gate. The threshold is crossed around **29.6 degrees**. This is an analytical failure condition, not a reproduced 40-degree landing; the previously measured native pose sites only reached 16.48 degrees. Steep support can therefore require a different contact rule even though the physical motor permits it.

**Required correction direction:** use completed physical support/contact with consistent slope tolerances, distinguish anticipation from impact, and make touchdown edge-triggered. Verify near-ground slow motion, uphill launch, crests, ledge loss, ceiling contact, side collisions and steep landings. Keep damage based on an appropriate pre-impact velocity; do not infer impact solely from the post-contact zeroed velocity.

### J08 — P2: Input lacks the grace expected around ground transitions

[`ResolveAnimationCommands`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DInputRouter.AnimationPlayground.cs#L69) consumes a one-update jump edge. `TryJump` rejects it immediately when airborne, busy, not standing or unsupported. There is no pending jump request for shortly-before-landing presses, no coyote-time allowance after ground loss and no jump-button-release height policy. Presses during landing recovery are discarded rather than handed to the next valid movement state.

`TryCancel` during Rising selects the falling pose without changing upward velocity; this is cancellation of an animation stage, not a short-jump physical policy. Existing menu/device/reset rearming is intentional and should remain.

Acquiring the jump's action constraint also calls `ResetStandingStrafe(true)`. Holding LT throughout the jump does not automatically reacquire standing facing lock after landing: the existing motor requires LT release/repress. Decide the jump-specific continuation policy separately from the menu/device/reset neutral gates. This is a jump/strafe handoff issue; the accepted directional gait sources and speeds do not need to change.

**Required correction direction:** specify bounded jump buffering and coyote time, and explicitly choose fixed-height versus release-sensitive jumping. These are proposed gameplay policies, not silently accepted tuning values. Preserve input IDs, menu ownership and the accepted LT contract.

### J09 — P2: Landing presentation/events and roll eligibility are too coarse

All variants use the same generic falling and approach sources. Ordinary landing choice follows the launch variant or the 6.5 m/s downward-impact threshold; it does not distinguish a moving run-out from a stationary landing. The first landing foot effect is delayed until the pose blend ends and always requests the left foot, without identifying the actual first contacting foot.

[`TrySelectedRollLanding`](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.LowMotion.cs#L24) requires a particular palette choice, speed no greater than 3.8 m/s, near-forward travel and ground within 0.25 degrees of flat. This means a fast native-ground jump cannot rely on roll recovery. The roll controller also imposes its own constant roll speed and stationary recovery. Treat it as a separate deliberate mechanic, not a universal landing fix.

**Required correction direction:** derive normal touchdown presentation from current motion and contact, align foot effects with real contact, and define severe-impact/roll recovery separately. Preserve once-only damage and event ownership.

### J10 — P1 validation gap: Previous checks encoded the wrong movement contract

[`AnimationPlaygroundAirborneTests`](../../../Assets/_Project/Tests/Editor/AnimationPlaygroundAirborneTests.cs#L50) manually invokes motor FixedUpdate while positioning the body and assigning vertical velocity. Its synthetic graph uses a new Animator and one starting clip instead of the equipped native-ground player. The moving-jump cases use a flat floor, manually provide flight/landing states, and explicitly accept landing braking. They check cached velocity, ownership and one landing event without advancing a real physical trajectory through the slope collision path.

[`AnimationPlaygroundNativeGroundTests`](../../../Assets/_Project/Tests/Editor/AnimationPlaygroundNativeGroundTests.cs#L69) supplies grounded takeoff/landing source poses at selected clip phases through a synthetic Recovery action. Its useful mesh/sole-clearance proof does not exercise `TryJump`→impulse→flight→contact→run-out. Earlier counts of 19,200 locomotion poses and 840 grounded-air poses therefore do not qualify jump transitions or controller feel.

**Required correction direction:** retain useful ownership/clearance checks, but change the intended contract to continuous movement. Add trajectory-level collision/support and velocity-handoff coverage, render-rate transition continuity measurements, and explicit future author feel acceptance. Do not describe static pose sweeps or passing state tests as a successful normal jump.

## Additional current integration observations

[Current console status](../../Evidence/Gameplay/JumpAudit20261015/console-status.json) reports no compilation failure and **five visible errors**. The [five recent error entries](../../Evidence/Gameplay/JumpAudit20261015/console-errors.json) comprise two `UI/Cancel` Input System callback failures and their wrapper entries, plus a destroyed animation-driver reference during session shutdown/status refresh. They occurred during the author's preceding session, not during simulated jumps in this audit. The bridge tail also contained older historical errors; those are not current compilation failures.

These are relevant to cancellation and teardown acceptance, but the stacks do not establish the cause of the in-flight blending or running stop. Reproduce them separately and repair their ownership/lifecycle boundary without assuming that a package upgrade is needed. No package changes are proposed here.

## What the audit does not implicate

The selected derivatives and qualified airborne profile are present. The serialized 3.8 m/s airborne cap is superseded by the active continuation's **6 m/s** override; changing that old number alone would not fix this report. All variants currently use a 4.6 m/s upward impulse with gravity −9.81 m/s². Ideal same-height flight is about 0.938 seconds and apex rise about 1.078 m, excluding collisions and numerical integration. Those numbers are not obviously the primary defect and should not be retuned blindly.

Existing ownership, once-only impulse/effects, gravity retention, reset cleanup and bounded visual projection provide useful foundations. Preserve them, along with the now-accepted strafing and forward sources. Terrain is fixed and always loaded; this audit proposes no world identity, streaming, generated-object identity, save-format or persisted-delta change.

## Recommended implementation order, pending a separate implementation request

1. Establish the normal-jump movement contract: momentum through acquisition/touchdown/release; short consistent anticipation; ordinary landing run-out; severe-impact recovery as a separate branch. Freeze accepted forward/strafe behavior.
2. Repair support-aware collision motion and velocity handoffs first. Prove no artificial stop on unobstructed native ground, while retaining genuine wall/ceiling collision handling.
3. Align source height references and stage boundaries on the equipped rig. Then replace frozen/restarted transitions with continuous phase-aware presentation and render interpolation. Assess additional directional animation needs after this alignment.
4. Rework landing contact, moving/stationary/severe presentation and locomotion re-entry together. Add the chosen input-grace policy and contact-timed effects; keep preview controls separate.
5. Verify the complete movement/pose timeline, then return for author testing. Include idle/walk/jog/run/fast run/sprint; LT side/back/diagonal jumps; stick hold/release/reversal; consecutive jumps and near-contact presses; uphill/downhill/sidehill, crest/ledge loss, walls, ceilings and hard drops; menu/cancel/device/reset interruption; and 30/60/120 Hz presentation. Specify required author-owned feel checks rather than claiming they already passed.

Only audit documentation/evidence was written. No runtime/editor implementation, private asset, scene, input, profile, package, or accepted strafe/forward gait was changed. No fresh test pass, visual acceptance, actual jump reproduction or final normal-jump qualification is claimed.

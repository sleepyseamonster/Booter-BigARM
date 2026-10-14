# Walking, running and sprinting implementation plan

Date: 2026-10-14. Status: planned; this request creates the plan and does not start implementation. Target: the existing Booter character in CampLoopTesting. Source of truth: the [locomotion audit](./CAMP_LOOP_LOCOMOTION_AUDIT_2026-10-14.md) and its [measured evidence](../../Evidence/Gameplay/LocomotionAudit20261014/README.md).

## Outcome and scope

Make walking, running and sprinting readable, responsive and consistent with the distance the player travels. Feet should plant convincingly; changing speed, starting, stopping and steering should preserve the pose rather than pop, skate or sink. The author's playtest acceptance completes the work.

Keep the current 1.4/2.7/3.8m/s lab speeds as initial design inputs. Preserve the accepted rig, 1.80m character scale, physics root, existing input/camera, inventory/vitals and one animation output. The visible capsule placeholder needs no repair for this work. Its separate collider remains the motor's collision shape.

Broader animation features remain deferred. Existing crouch, jump/fall, slide/roll, mounted ownership and session reset must remain compatible; this plan does not extend them. Work stays in the lab until qualification and any separate production handoff are authorized. No package change, new Animator architecture, automatic terrain traversal, second player controller or wholesale refactor is required.

## Sequence and completion gates

| Step | Work | Deliverable and gate |
| --- | --- | --- |
| L0 — Baseline | Snapshot current source/profile/clip hashes, scene references, input and actual startup configuration. Use the marked isolated background candidate. Create a diagnostic path that evaluates the three sustained gaits without legacy one-shots or turn overlays. | Reproducible baseline and rollback seal. Assert the lab applies 1.4/2.7/3.8m/s rather than historical motor defaults. A missing startup dependency must be visible, not treated as a working gait. |
| L1 — Motion selection and stride calibration | Compare the existing unarmed walking/running/sprinting alternatives on the accepted rig. Measure stance windows, tracked sole motion, body orientation/amplitude, native cadence and seams. Assess compatible timing and transition candidates together. | Three selected motions, measured contact/stride metadata and explicit playback budgets. Keep native sources intact. Each gait must be usable individually before blends are qualified. |
| L2 — Continuous cadence and actual motion | Repair the shared phase clock and source-time mapping. Resolve the incompatible current playback limits. Account for grounded actual displacement, partial input, acceleration, braking, obstruction and discontinuous reset/teleport. | No dominant-role cadence jump; accepted segment playback rates for all significant blend contributors; exactly-once supported foot contacts. Blocked travel does not keep emitting sustained steps. |
| L3 — Blends and grounding | Qualify walk/run and run/sprint across intermediate weights. Measure the evaluated pose, then apply bounded visual grounding through the existing driver. Preserve airborne motion and support transitions. | Flat-floor sole penetration at most 1mm through accepted samples; no excessive lift or visible vertical pumping. Full body motion remains intact. |
| L4 — Start, stop and steering | Replace, adapt or omit incompatible legacy jog transitions according to measured pose compatibility. Match planted side and outgoing phase; preserve steering. Review acceleration/deceleration alongside animation timing. Qualify turns before restoring directional overlays. | Starts/stops finish or interrupt into a compatible pose. Turning does not inject an unqualified sideways gait. Movement stays responsive and visually weighted. |
| L5 — Player integration and regression | Bind the selected private assets and qualified profile to the existing lab player. Expose clear gait/readiness feedback in the existing lab controls. Verify cross-family owner and lifecycle handoffs. | Saved references, source/meta hashes and private restoration seal pass. Existing supported actions and production/null configuration remain regression-safe. |
| L6 — Author playtest and closeout | The author reviews the trio on AP02's flat deck, then starts/stops, speed changes, steering, obstacles and native slopes. Record findings and retune the same slice. | Author accepts appearance and feel. Numerical, integration and visual results are reported separately. Broader work does not resume automatically. |

L0/L1 produce the first reviewable candidate. L2/L3 repair sustained locomotion. L4/L5 add complete player behavior. L6 establishes acceptance. Do not enable later layers to hide an unfinished earlier gate.

## L1: selection and calibration details

Start with existing downloads; exclude weapon poses. Walking is the closest current baseline. Running and sprinting need particular scrutiny because the audit's stance-drift proxies were much larger than walking's.

Use consistent rig/world scale and distinguish foot-bone proxies from actual contact. Track stable sole vertices during measured supported stance windows, including heel/toe rolling, rather than taking horizontal velocity from whichever mesh vertex happens to be lowest. Report both metrics and their limits. Recheck source versus derived poses, facing, limb amplitude and seams before accepting a derivative.

Document one phase convention: a complete profile loop and its actual number/order of left/right plants. Preserve source asymmetry where appropriate. Sprint's two stride halves are different; its markers must come from measured contacts, not an assumption of equal quarters.

Choose a compatible family or deliberately author/retime a candidate within reviewed limits. Do not reduce player speed or widen playback bounds merely to conceal sliding. If the intended speed cannot be met without an unnatural pose, try another existing source or a bounded authored adaptation. Any later acquisition is a recorded motion gap, not the first response.

Initial numerical targets are no more than 1mm flat-floor penetration and approximately 2cm accumulated drift for an identified supported contact patch. Drift is a provisional motion-review target: define the patch/window and account for rolling before treating it as a pass/fail gate. Record accepted rate and drift budgets before enabling the family.

## L2: solve the timing constraints

The current profile's native 0.9–1.1 playback interval corresponds to these complete-loop cadence ranges:

| Gait | Duration | Corresponding cadence range |
| --- | --- | --- |
| Walk | 2.3333s | 0.3857–0.4714 loops/s |
| Run | 1.4667s | 0.6136–0.7500 loops/s |
| Sprint | 1.0667s | 0.8438–1.0313 loops/s |

Adjacent ranges do not overlap. A shared phase cannot strictly satisfy both native intervals during a substantial blend with the current assets. The implementation currently permits sub-minimum cadence for slow travel and clamps against only the dominant role; that does not resolve full-speed blend compatibility.

L1/L2 must choose a documented solution: compatible source timing, measured authored retiming and/or reviewed blend-specific rate budgets. Do not claim that clamping every contributor independently while sharing one phase solves the conflict. Check the actual derivative of source time after contact remapping; a bounded whole-loop clock can still create unbounded segment rates, as the sprint audit demonstrated.

Use a continuous cadence/stride policy over the accepted speed range, including the 2.05 and 3.25m/s dominant-role crossings. Keep phase continuous across speed changes and suspension/resumption. Evaluate meaningful frame subdivisions rather than depending on render-frame timing.

Grounded cadence should reflect completed movement. Separate ordinary travel from surface correction, falling, externally owned motion and reset/teleport so those events cannot generate false steps. Clear or rebase measurements explicitly at ownership and lifecycle boundaries. Footstep effects retain one contact authority.

## L3/L4: pose continuity and controller feel

The current weighted sum of per-clip height corrections misses 38–46mm of blended sole penetration. Measure the final mixed pose and use the existing driver's visual host/projection seam to correct supported grounding. Do not move the gameplay body from animation or introduce a competing pelvis/foot writer. Respect the bounded correction envelope and reject mismatched sources rather than compensating without limits.

Sweep gait weights and phase together, not only three pure clips and one midpoint. Include equipment after the bare rig baseline; boots, backpack and cloak must not conceal or introduce grounding/clearance errors. Native slopes and support changes are separate from flat-floor acceptance.

The old jog starts/stops are nominally 4.2m/s. They are currently sought into and exited rapidly, with unmatched pose gaps. Use measured entry/release/plant windows, actual incoming phase and planted side. Retain enough recovery time or a qualified interruption route. Check idle-to-walk, idle-to-run, idle-to-sprint and each gait-to-idle separately.

The old acceleration settings produce different response times after reducing lab speeds. Record the current ramps and compare them with clip timing and the provisional [movement feel standard](../../Engineering/MOVEMENT_CAMERA_STANDARD.md). Tune movement and transitions together when required; retain immediate input intent and steering rather than adding input latency.

Establish travel-aligned forward motion first. Reassess the 55% sideways strafe overlay and legacy pivots using actual yaw, trajectory and gait speed. Turns, reversals and diagonal travel need their own pose/phase checks. Standalone strafing and unrelated traversal are outside this slice.

## Ownership, assets and integration

| Area | Existing seam / planned responsibility |
| --- | --- |
| Physics and motion observation | TopDown3DPlayerMotor and its lab partials: one Rigidbody writer, actual grounded displacement, support and ownership transitions. |
| Presentation and clock | TopDown3DPlayerAnimationDriver: one Playables output, accepted cadence, compatible gait blends, visual grounding and contacts. |
| Gait data | TopDown3DLocomotionClipProfile and private CalibratedLocomotion/PlayerBindings: measured role/contact/stride/rate authority. Preserve compatible serialization and GUIDs. |
| Lab startup and input | TopDown3DAnimationPlaygroundSession.Player and existing input/router/preferences: reliable speed/readiness configuration, keyboard and analog gait selection, existing modal ownership. |
| Qualification | Tools/Diagnostics/CampLocomotionAudit.cs and bounded Editor tooling: isolated pose/clock measurements and reproducible receipts. |

Runtime implementation stays in the established TopDown3D assembly; editor automation stays editor-only. Use an explicit lab opt-in for new timing/grounding so shared driver edits do not activate experimental behavior in production or older profiles. Never promote an experimental profile into the author scene before its dependent gates pass. Keep licensed source FBXs, derived clips and private bindings local under the existing private-source policy. Capture recipes, exact hashes and metadata alongside each private checkpoint.

Preserve prior checkpoints, author scene changes and unrelated work. Use a current exact dependency manifest for candidate transfer. If scene installation is required, use the documented background bridge and save only the intended lab binding/configuration after checking editor ownership and unsaved state. No foreground activation is part of this plan.

## Verification and author review

Add meaningful Edit Mode fixtures for the gaps identified by the audit:

- Continuous cadence at both dominant-role midpoints, nominal speeds and accepted analog range; compare actual source-segment rate against accepted budgets.
- Tracked contact drift and native/derived stride preservation; phase/weight sweeps for evaluated sole clearance and transition pose compatibility.
- Start, stop, resume, reverse and obstruction snapshots; supported motion versus requested velocity; pause, disable/re-enable, reset, focus loss and action/modal ownership.
- One contact source with no duplicate, stale or stationary-blocked footsteps; compatible handoffs to/from existing crouch, jump/fall, slide/roll and mounted presentation.
- Hash/GUID continuity, exact private restoration and legacy production/null configuration.

Use deterministic Editor measurements and focused tests. No agent-owned Play Mode or gameplay smoke test is authorized. The author owns hands-on testing. Do not broaden testing after a gate passes unless a new change or unresolved concern justifies it.

The author review sequence is: steady walk, steady run, steady sprint; gradual changes between them; repeated starts/stops; steering and reversals; then collision/native-ground behavior and return from supported actions. Review speed, full-body motion, visible skating, foot penetration, pose pops and responsiveness with equipment on. Record accepted clips and tuning plus any remaining defects.

Each completed implementation step receives its own bounded evidence and verified local commit on main, staging task-owned paths only. Commits do not authorize pushes, PRs or releases. Closeout includes a self-audit of source compatibility, transitions, ownership, documentation and the actual author acceptance.

## World and lifecycle boundaries

Camp Loop uses authored identities and fixed, always-loaded native terrain. No terrain generation or terrain/chunk streaming is applicable or introduced. This system adds no generated object IDs or persisted world deltas. Gait phase and contact state are transient actor state: unload/reload, disable and session reset must release owned resources and rebase observations without emitting stale effects. Existing authored surface/collision constraints remain authoritative.

# Walking, running and sprinting implementation plan

Date: 2026-10-14. Revision 3, following the [plan audit](./CAMP_LOOP_LOCOMOTION_PLAN_AUDIT_2026-10-14.md) and the author's slower uphill gait decision. Status: implementation active by the author's subsequent instruction. Target: the existing Booter character in CampLoopTesting. Source of truth: the [locomotion audit](./CAMP_LOOP_LOCOMOTION_AUDIT_2026-10-14.md) and its [measured evidence](../../Evidence/Gameplay/LocomotionAudit20261014/README.md). Follow the [implementation checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/README.md) for current gates; the broader playground expansion remains deferred.

Implementation finding: the [transition/current-scene checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTransitions/README.md) closes the older saved-scene dependency gap and adds verified direct stop/restart blending. Its active-rig plane audit shows that general slope correction cannot be achieved within the existing visual-host lift limit. L3/L4 must qualify terrain-relative stance/leg adaptation inside the existing pose output while preserving the physics root and collision envelope. This does not authorize widening limits, a competing transform writer or source/terrain changes. The private policy remains gated pending that work and the remaining acceptance requirements below.

## Outcome and scope

The subsequent [terrain observation/prototype checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTerrain/README.md) adds a completed-support plane readout without new queries and passes 247 relevant Edit Mode checks. Its offline single-output Humanoid experiment measures contact reach separately from sole clearance; steep slopes still fail the sampled plant window. That experiment remains outside runtime presentation. Complete supported-contact, reach/body/equipment and one-evaluation qualification before installing any terrain adaptation.

The newer [gated runtime terrain checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTerrainRuntime/README.md) adds an optional stage inside the same driver output and verifies its ownership, cold equipment, reset and near-flat boundaries. All 266 relevant checks pass. Its source-sealed 264-case pose sweep still fails steep equipment/motion and the presentation CPU budget. Neither the forward family nor its terrain extension is qualified or selected in the author binding. These failures keep L3/L4 open; the smaller focused tests do not override the wider qualification results.

The later [terrain reach checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardTerrainReach/README.md) preserves explicit unavailable poses and confirms that installation remains rejected. The [uphill motor checkpoint](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardUphillGait/README.md) implements the author's slope preference through the existing controller: a smooth 32–40-degree uphill cap, anticipatory braking, existing release acceleration and Rigidbody-based lab support. All 285 coupled Edit Mode checks pass. Native support changes, motion/steering qualification, the bounded experimental author-review candidate and final integration/acceptance remain open; this policy does not qualify the terrain pose stage.

The [experimental flat-deck review](../../Evidence/Gameplay/LocomotionImplementation20261014/ForwardReview/README.md) is now installed in the saved author scene with 306 passing relevant Edit Mode checks. F9's explicit reset-to-deck action makes AP02 directly accessible. The original binding remains the baseline outside the bounded review; both qualification flags remain false. This fulfills the early feedback opportunity below, while native support/contact, steering, moving action handoffs and final author acceptance remain open. It does not close L3–L6 globally.

Make walking, running and sprinting readable, responsive and consistent with the distance the player travels. Feet should plant convincingly; changing speed, starting, stopping and steering should preserve the pose rather than pop, skate or sink. The author's playtest acceptance completes the work.

Keep the current 1.4/2.7/3.8m/s flat-ground lab speeds as initial design inputs. The author explicitly selected a slower uphill gait on very steep walkable ground (about 40–48 degrees). Implement a continuous cap toward walking pace through the existing motor, with braking on approach and smooth acceleration when leaving the incline. Preserve partial input and slow-walk precedence. Presentation must follow completed displacement rather than switch to walk while the body still sprints. Qualify steady travel and fast entry/exit, including downhill and sidehill stress cases; the new uphill policy does not accept the failed terrain prototype. This is an approved movement design change, not a general permission to lower flat speeds to conceal source mismatch.

Preserve the accepted rig, 1.80m character scale, physics root, existing input/camera, inventory/vitals and one animation output. The visible capsule placeholder needs no repair for this work. Its separate collider remains the motor's collision shape.

Broader animation features remain deferred. Existing crouch, jump/fall, slide/roll, mounted ownership and session reset must remain compatible; this plan does not extend them. Work stays in the lab until qualification and any separate production handoff are authorized. No package change, new Animator architecture, automatic terrain traversal, second player controller or wholesale refactor is required.

## Sequence and completion gates

| Step | Work | Deliverable and gate |
| --- | --- | --- |
| L0 — Baseline and contracts | Snapshot current source/profile/clip hashes, scene references, input and actual startup configuration. Use the marked isolated background candidate. Define the schema, simulation/render timing, motion and resting-state contracts below. Create a diagnostic path that evaluates the three sustained gaits without legacy one-shots or turn overlays. | Reproducible baseline and rollback seal, plus recorded contracts and performance baseline. Assert the lab applies 1.4/2.7/3.8m/s rather than historical motor defaults. A missing startup dependency must be visible, not treated as a working gait. |
| L1 — Motion selection and stride calibration | Compare the existing unarmed walking/running/sprinting alternatives on the accepted rig. Measure stance windows, tracked sole motion, body orientation/amplitude, native cadence and seams. Assess compatible timing and transition candidates together. | Three selected motions, measured contact/stride metadata and explicit playback budgets. Keep native sources intact. Each gait must be usable individually before blends are qualified. |
| L2 — Continuous cadence and actual motion | Repair the shared phase clock and source-time mapping. Resolve the incompatible current playback limits. Account for grounded actual displacement, partial input, acceleration, braking, obstruction, resting recovery and discontinuous reset/teleport. | No dominant-role cadence jump; accepted segment playback rates for all significant blend contributors; cycle-counted supported contacts. Blocked travel settles safely without sustained steps or a frozen raised foot. |
| L3 — Blends and grounding | Qualify walk/run and run/sprint across intermediate weights. Measure the evaluated pose, then apply bounded visual grounding through the existing driver. Preserve airborne motion and support transitions. | Flat-floor sole penetration at most 1mm through accepted samples; no excessive lift or visible vertical pumping. Full body motion remains intact. |
| L4 — Start, stop and steering | Replace/adapt incompatible legacy jog transitions or keep their dispatch gated off using the lab policy; preserve required serialized roles. Match planted side and outgoing phase; preserve steering. Review acceleration/deceleration alongside animation timing. Qualify turns before restoring directional overlays. | Starts/stops finish or interrupt into a compatible pose. Turning does not inject an unqualified sideways gait. Movement stays responsive and visually weighted. |
| L5 — Player integration and regression | Bind the selected private assets and qualified profile to the existing lab player. Expose clear gait/readiness feedback in the existing lab controls. Verify cross-family owner and lifecycle handoffs. | Saved references, source/meta hashes and private restoration seal pass. Existing supported actions and production/null configuration remain regression-safe. |
| L6 — Author playtest and closeout | The author reviews the trio on AP02's flat deck, then starts/stops, speed changes, steering, obstacles and native slopes. Record findings and retune the same slice. | Author accepts appearance and feel. Numerical, integration and visual results are reported separately. Broader work does not resume automatically. |

L0/L1 produce the first reviewable candidate. L2/L3 repair sustained locomotion. L4/L5 add complete player behavior. L6 establishes acceptance. Do not enable later layers to hide an unfinished earlier gate.

## Schema and readiness gate

The existing profile requires all twelve roles, non-null clips, preferred entry/exit roles and the approved 0.90–1.10 bounds. The driver constructs those inputs before any lab qualification. A three-gait experiment therefore cannot remove starts/stops/pivots from the profile or install empty placeholders.

Keep the valid retained roles while a separate optional lab policy gates their dispatch. If L1 requires different timing/rate metadata, add a versioned, explicitly validated lab extension or a narrowly routed validation path. Preserve the existing production validator and null/legacy behavior. Do not silently weaken validation or bypass it to accept a candidate. The preferred entry/exit arrays are currently validated metadata rather than driver routing; qualified handoffs must be implemented and exercised, not inferred from populated arrays.

L0 records this data/dispatch contract; L1 seals the selected sources, effective rate budgets and measurement definitions before L2 uses them. If no compatible family fits those budgets, return to source selection and record the missing adaptation rather than compensating indefinitely in the clock or grounding layer.

## L1: selection and calibration details

Start with existing downloads; exclude weapon poses. Walking is the closest current baseline. Running and sprinting need particular scrutiny because the audit's stance-drift proxies were much larger than walking's.

Use consistent rig/world scale and distinguish foot-bone proxies from actual contact. Track stable sole vertices during measured supported stance windows, including heel/toe rolling, rather than taking horizontal velocity from whichever mesh vertex happens to be lowest. Report both metrics and their limits. Recheck source versus derived poses, facing, limb amplitude and seams before accepting a derivative.

Document one phase convention: a complete profile loop and its actual number/order of left/right plants. Preserve source asymmetry where appropriate. Sprint's two stride halves are different; its markers must come from measured contacts, not an assumption of equal quarters.

Choose a compatible family or deliberately author/retime a candidate within reviewed limits. Do not reduce flat-ground player speed or widen playback bounds merely to conceal sliding. The author's slower steep-uphill gait is the specific exception described above. If the intended speed cannot be met without an unnatural pose, try another existing source or a bounded authored adaptation. Any later acquisition is a recorded motion gap, not the first response.

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

Count the full effective retiming from original source to derivative to runtime source-time mapping. Changing a derivative's duration and playing it at 1.0 does not erase its original retiming cost. Record the significant-contributor weight threshold and separately check weight tails during fade-in/out; low blend weight does not exempt a pose from clearance or seam checks.

Use a continuous cadence/stride policy over the accepted speed range, including the 2.05 and 3.25m/s dominant-role crossings. Keep phase continuous across speed changes and suspension/resumption. Evaluate meaningful frame subdivisions rather than depending on render-frame timing.

Grounded cadence should reflect completed movement. Separate ordinary travel from surface correction, falling, externally owned motion and reset/teleport so those events cannot generate false steps. Clear or rebase measurements explicitly at ownership and lifecycle boundaries. Footstep effects retain one contact authority.

## Clock, motion and resting-state contract

Use completed Rigidbody simulation samples with a tick/epoch and support state; avoid render-interpolated Transform displacement and the next commanded velocity as distance authorities. On a supported slope, use locomotion displacement along the authoritative support tangent so real uphill/downhill travel counts while normal settlement does not. Rebase on support/owner changes, reset and discontinuities. Static ground/decks are the initial support scope; any moving support encountered needs relative-motion handling or explicit unavailability.

The simulation owns unwrapped phase/cycle progression and contact crossings. Rendering may interpolate accepted snapshots but cannot advance the contact ledger or feed motion measurement. Define evaluation order so the sampled pose, visual grounding and contact position describe the same timestamp. The current graph uses GameTime and one-shots advance at speed 1; mixing that automatic advance with manual projection must not produce a second time advance. Keep one graph output and explicitly coordinate any additional pose sampling with existing action presentation.

Handle zero, one or several fixed ticks per render frame, source wrapping and hitches. Count every semantic crossing by epoch/cycle/contact identity instead of a wrapped-phase boolean or a 0.95 advance cap. Define bounded cosmetic catch-up separately: stale dust/audio may be discarded under an explicit policy, but must not be replayed as new contacts.

Actual low/blocked movement must settle into a supported idle/recovery pose instead of freezing an airborne part of the stride. Use recorded enter/exit hysteresis to avoid idle/start flicker during tiny input, collision pressure or noisy measurements. Holding input against a wall must not repeatedly restart a one-shot. Continue responding to steering. Explicit settling/turn plants may represent a real weight transfer at zero travel; distinguish them from sustained locomotion footsteps.

Preserve current input semantics: ordinary keyboard movement requests run, the existing slow-walk modifier takes precedence over sprint, and analog magnitude remains continuous. Gait presentation follows accepted actual travel within that request; it must not label sprint-held partial input as full-speed sprint. Retain one deadzone, stable binding overrides, neutral-on-modal-return and router-derived prompts. Test device disconnect/reconnect and camera-relative steering without adding a second input or camera authority.

## L3/L4: pose continuity and controller feel

The current weighted sum of per-clip height corrections misses 38–46mm of blended sole penetration. Measure the final mixed pose and use the existing driver's visual host/projection seam to correct supported grounding. Do not move the gameplay body from animation or introduce a competing pelvis/foot writer. Respect the bounded correction envelope and reject mismatched sources rather than compensating without limits.

Sweep gait weights and phase together, not only three pure clips and one midpoint. Include equipment after the bare rig baseline; boots, backpack and cloak must not conceal or introduce grounding/clearance errors. Native slopes and support changes are separate from flat-floor acceptance.

Screen equipment clearance during source selection, then repeat the complete L3 matrix with equipment. Check the currently supported one/two/four bone influence limits in the candidate without changing author quality settings. Test cold startup and returns from crouch/low motion; equipment normalization/projection state must not make the standing result depend on which action ran first. Grounding may not shift head, torso or backpack beyond the reviewed collision/roof envelope.

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

The motion matrix includes partial analog ramps and jitter around idle/gait thresholds, keyboard walk/sprint modifier overlap, uphill/downhill and cross-slope travel within the existing slope limit, support loss/return, held input against a wall and release/restart, and camera orbit during travel. Reversal means reorienting the existing actor toward travel; it does not silently add facing-locked backward locomotion or standalone strafing. Cover several fixed-tick/render-frame ratios, a bounded long frame, and crossings of more than one contact in a frame.

Use deterministic Editor measurements and focused tests. No agent-owned Play Mode or gameplay smoke test is authorized. The author owns hands-on testing. Do not broaden testing after a gate passes unless a new change or unresolved concern justifies it.

The author review sequence is: steady walk, steady run, steady sprint; gradual changes between them; repeated starts/stops; steering and reversals; then collision/native-ground behavior and return from supported actions. Review speed, full-body motion, visible skating, foot penetration, pose pops and responsiveness with equipment on. Record accepted clips and tuning plus any remaining defects.

Each completed implementation step receives its own bounded evidence and verified local commit on main, staging task-owned paths only. Commits do not authorize pushes, PRs or releases. Closeout includes a self-audit of source compatibility, transitions, ownership, documentation and the actual author acceptance.

## Measurement, performance and staged review

Define every budget before the affected gate. For a selected contact patch/window, report maximum tangent excursion from its contact anchor, cumulative travel, peak/typical drift and the support/rolling classification. An end-to-start displacement alone can hide visible back-and-forth skating. Treat the approximately 2cm target as a reviewed engineering target, not existing proof. Record the exact limits for source retiming, contact height, correction/lift/vertical velocity, handoff gaps and blend seams.

For the clock, check values and one-sided limits at role crossings; an arbitrarily tiny speed perturbation must not create a finite cadence jump. Check accepted frame subdivisions against the same fixed simulation trace. For geometry, sample native curve keys, contact/time-map breaks, wrap boundaries and intermediate phase/weight combinations, refining near worst cases. Report the tested envelope and sampler error; a finite grid does not prove every possible pose. The 1mm flat-floor gate requires a sampling/prediction error budget smaller than that limit.

Offline full mesh baking is evidence tooling. Runtime grounding/contact sampling must use a validated bounded cache or another measured bounded method. Record timing, allocation and graph-evaluation counts against the L0 baseline on the installed editor/player configuration; reject per-frame full-mesh baking, repeated buffer allocations and unbounded queries. Set the accepted incremental CPU/allocation budget before enabling the implementation rather than claiming performance from numerical pose tests.

Offer a short author review after the three forward loops, continuous clock, initial grounding and equipment screening pass. Label that checkpoint experimental; leave unresolved legacy transitions/turn overlays gated and record the limited coverage. Collect clip/feel feedback before completing all L4 work. This is an author-owned playtest opportunity, not permission to automate Play Mode or claim full acceptance.

Before author-scene promotion, hydrate and validate the current saved scene and its complete shared runtime/bootstrap dependency closure. The audit's older frozen scene is suitable for isolated gait measurements, but cannot prove current integration. Recheck tested hashes against the promotion candidate and preserve concurrent changes.

Keep a scoped rollback recipe for the lab policy/binding and task-owned source deltas, with paired metadata and the prior private seal. Do not restore an older whole scene, use a broad Git reset or overwrite later work. If a gate fails, retain evidence and return to that step; a failed source/budget gate stays unavailable, while the existing safe baseline and foreign action owners remain intact.

## World and lifecycle boundaries

Camp Loop uses authored identities and fixed, always-loaded native terrain. No terrain generation or terrain/chunk streaming is applicable or introduced. This system adds no generated object IDs or persisted world deltas. Gait phase and contact state are transient actor state: unload/reload, disable and session reset must release owned resources and rebase observations without emitting stale effects. Existing authored surface/collision constraints remain authoritative.

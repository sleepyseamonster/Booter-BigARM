# Camp Loop player and animation integration plan

Date: 2026-10-13. Revision 2 of the [master plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Apply the [prerequisites](./CAMP_LOOP_ANIMATION_FOUNDATIONS_PLAN_2026-10-13.md) and [motion backlog](./CAMP_LOOP_ANIMATION_MOTION_BACKLOG_2026-10-13.md) before enabling dependent families.

## Actor and data ownership

Keep the existing Booter root, Rigidbody/capsule, vitals/inventory, interaction/action components, footstep effects and camera/input/companion. Add optional lab configuration; production/null configuration keeps current behavior and serialized assets. Do not create a second actor, health value, physics writer or Animator output.

Public recipes carry source/action IDs, calibrated parameters, readiness and required roles. A private binding set carries the resolved source/target model/Avatar, imported clips and actual locomotion profile. A project-authored visual host contains no missing nested FBX prefab. Initialize through explicit readiness: dependencies, services, visual and graph. Subscribe/read visual-ready safely regardless of component order; missing bindings make new actions unavailable rather than substitute another model. Destroy/unsubscribe only owned resources on failure, disable or reload.

Runtime additions stay under `Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/`, in `BooterBigArm.TopDown3D.Runtime` and its namespace. Editor tools remain in `BooterBigArm.Editor`; tests in `BooterBigArm.Editor.Tests`. Shared seams to inspect/extend: motor/animation driver; interaction/action controller; input router partials; control preferences and menu/controller/installer; checkpoint service; camp; footstep presentation. Preserve callers and serialized signatures, with compatible optional configuration/overloads.

Planned contracts: scene session/configuration, target descriptor/view, activity projection, capability/presentation recipes, private binding set, action coordinator and pure clock/marker ledger, motor-owned intent/snapshot, prop handler, lab damage receiver and reset participant. These extend current owners; they are not parallel global services.

## Target, activity and input foundation

The current selector discovers four concrete target types. Add station discovery through one target view/descriptor, with stable ID, object/reference, epoch, action kind, collider ownership, range/point and availability. Preserve existing tie-break/hysteresis and dedicated canister/cargo behavior. Capture the target at press, revalidate the same target at release/effect, and never retarget a completed tap. Compound colliders deduplicate by target identity and reset invalidates captured epochs.

Dispatch station actions explicitly before the old reward gather path. Do not make a chair/button implement a fake zero reward or consume result. A shared activity view includes gather, placement, waiting for Legger, camp and new actions; availability, cargo, inventory, algae and canister commands all consult it. The motor's owner lease remains the lock. Interruption asks the owner to release safely; no lease stealing.

Use an owned runtime instance of the lab input asset, preserving shipped binding IDs and generating stable IDs for new actions. The control-preference policy must include all coactive fixed/new actions in conflict checks, not only its current editable whitelist. Inject the lab controls directory separately from normal menu settings. Read compatible production overrides without modifying their file; report new-control collisions and provide lab rebinding before enabling conflicting direct commands. Prompts use effective bindings. [Input System bindings](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/ActionBindings.html).

Host the palette as a panel of the existing menu owner. Keep one EventSystem/module/modal lifecycle, release gating and focus/device transitions. Selecting a variant closes into a neutral state; a fresh gameplay press executes it. Preview/seek occurs only in a no-effect preview mode, not by replaying markers in the physical action.

| Lab intent | Proposed controls | Required policy |
| --- | --- | --- |
| Move / camera / sprint | Existing sticks and right shoulder; WASD/mouse/Shift | Preserve current feel outside lab capabilities. |
| Slow walk | Low analog magnitude; keyboard Left Alt | Current keyboard target is 4.2, so explicitly select the qualified walk speed instead of assuming keyboard walking already exists. |
| Jump | South / Space | Bind existing dormant action through the router; takeoff edge requires release before repeat. |
| Crouch / slide | Left-stick press / V | Crouch toggle normally; sprint plus adequate speed requests slide. C remains occupied. |
| Context interaction | West/E tap | Preserve target capture/revalidation and mutually exclusive cargo hold. |
| Selected applicable strike/throw/dodge | Right trigger / existing Attack | Button press semantics and analog threshold; one request per press, no modal leakage. |
| Palette | Select / F9 | Menu-owned panel; current source paths are free, active overrides/reserved-device paths still require FND3 validation. |
| Cancel owned action | East while cancellable lab action owns control; Backspace | Resolve cancel before pickup/dose/interaction. Idle East retains canister pickup. Unsafe traversal uses safe exit policy. |

No gameplay component reads buttons directly. New modes must disarm/rearm jump/attack/crouch/modifier commands just as current item commands do. Menus, inventory, radial, camp and device changes cannot forward held confirm/attack or stale target intent into gameplay. Lab preferences cannot write production `controls-v1.json`.

Interpret routed Move intent by the owned mode: cover projects onto the wall tangent, push onto the reviewed crate axis, and ladder ascent uses the vertical input axis with stationary hold at zero. Approach uses normal movement until alignment owns the actor. Descent is unavailable until qualified. Context entry, safe cancel/exit and menu preview remain distinct intents; an axis change never silently selects another target.

## Action clock and physical authority

Requests identify action/variant, posture, captured target/epoch and held prop. Availability checks required motion roles and contact/marker readiness before atomic target/motor reservation. Lifecycle: approach → align → enter → active/contact → exit/recovery → complete, with timeout/failure policy per phase.

One scaled simulation clock advances physical state; presentation reads it. FixedUpdate owns motor movement and queued effect commits; preview has a separate no-effect clock. Ledger keys include action instance/epoch, marker and cycle. Handle skipped/multi-cycle intervals, quotas, cancellation before/after commit, reset and pause. Commit once and retain a committed effect during recovery; a stale effect never survives an epoch change.

Advance constrained motion inside the motor's current early-return branch, which otherwise zeros velocity. Publish actual mode/posture/support, velocity/displacement, facing and phase. Use posture-aware capsule probes with feet anchored, takeoff support hysteresis, real landing contact and safe gravity restoration. Suppress automatic vault/steep-climb while conflicting modes own the actor. Source trajectories are measured/normalized or extracted and discarded; Rigidbody/action paths move the actor. Never add Animator delta movement on top.

## One pose and contact graph

Retain the existing 21-slot graph as the normal locomotion/camp/gather branch. Add optional posture/directional and current/next timed-action branches under the same driver/output. Instantiate/connect clips on mode/action changes, not every frame. Only active blend sets evaluate. A fuller target rig has its own qualified Avatar; matched source animations retain the source Avatar used at import.

Switch clock/contact routing with pose ownership. Inactive legacy gait must not advance effects or emit footsteps while crouch/carry/action branches are active. Locomotion uses qualified cyclic contact maps; physical one-shots use approved contact windows. Keep root motion/foot IK baseline disabled; measured anchors and authored poses qualify contact first. Hand poses, if needed, belong to the same graph/presentation owner, not a competing bone writer.

The legacy gait mapper requires R0-L0-R1-L1 ordering, two contacts per foot and compatible cadence. A calibrated source/target fixture qualifies the twelve roles and any two-cycle/phase-origin derivative. New directional facing mode requires backward locomotion and standing turn roles; until then expose only supported lateral tests and state the restricted capability. Do not animate backward movement with a forward run.

## Physical family contracts

| Family | Required behavior and closure |
| --- | --- |
| Locomotion/strafe | Calibrated speed/phase/starts/stops/pivots, explicit keyboard walk and supported facing/travel combinations. |
| Crouch/duck/slide/dodge | Capsule height/centre and ceiling tests; timed duck versus sustained posture; speed/surface gates, sweeps, obstruction and crouched blocked-exit recovery. Split running-slide entry/active/recovery roles deliberately. |
| Jump/fall/land/roll | Physical takeoff/airborne/contact drives segmented source poses; no timer landing/doubled arc. Roll/hard landing owns reviewed recovery and damage policy. |
| Cover/wall | Reserve wall plane/tangent/segment, clamp ends, probe corners/exit and pair crouched cover with an accepted exit. Standing wall-hug is a requested role gap. |
| Ladder/step | Qualified rung/contact axis, entry/top anchors and clearance; gravity treatment/restoration inside motor. Use step clips on appropriate obstacles, not as proof of ladder descent. Return ramp closes ascent testing. |
| Pickup/hold/drop/throw | Reserve/attach once, persistent held-object state with qualified hold pose, safe cancellation/drop, release trajectory and self-collision restoration. Moving carry is disabled until its own pose/locomotion set qualifies. Place-down is a distinct role, not an instant drop presented as animation. |
| Button/push | Exactly-once switch contact; crate/actor movement and obstruction ownership, start/loop/stop, hand anchors and reset. |
| Seat/ground/sleep | Complete enter/idle/exit/abort roles and clearance. Existing camp remains owner of actual sleep/rest/recovery and equipment; a station adapter cannot independently drive a second camp pose/clock. |
| Mining | Adapt tool grip/swing/impact/return. Use a distinct lab target/component and session reward ledger, preserving twelve original Ironstone nodes and the real cargo/economy path. |
| Strike/hurt/death/get-up | Reviewed windows, one hit per target/strike, one vitals authority, direction/severity, posture-specific reactions, gravity/collision and terminal hold. Recovery requires matching floor posture and full rise clearance. |

Use passive targets and intentional damage controls initially. Standing damage sequences qualify first; crouched, airborne, seated and ladder cases have explicit policies/gates. Nonfatal unsupported visual reactions cannot freeze or corrupt physics. Fatal airborne/traversal exits release ownership and continue safe descent. Terminal death uses a low corpse collision profile and holds pose; no initial ragdoll. Crouched death needs a new unarmed variant because the downloaded source is excluded. Health changes are not rolled back merely because a reaction clip is unavailable.

## Reset, safety and acceptance

Resolve session-only checkpoint policy before restore/writer startup; honor timer/manual/disable/quit and isolate controls separately. Reset invalidates epochs/queued effects, releases owned motion/props, restores registered station state and lab vitals, checks an actor return/rise position, then rearms input. No global Rigidbody deletion, inventory farming or companion teleport. Missing clear return/support reports a blocked operation and stays safe.

Qualification covers target capture/dispatch/busy state, coactive bindings and modal handoff, clocks/markers, shape/support/movement modes, selected contact stream, held props, damage/posture/corpse/recovery, complete family exits, bootstrap/reset/unload and byte-identical saved files. Regress touched existing interaction, controls/menu, locomotion/traversal, camp/survival/checkpoint and scene fixtures in the appropriate isolated candidate. The author accepts deformation, grip, slip, responsiveness and transition quality. Recipes never set PhysicalQualified solely because a clip imported or previewed.

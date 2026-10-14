# Camp loop Legger riding design exploration

October 14, 2026. Continues the [riding audit](CAMP_LOOP_LEGGER_RIDING_AUDIT_2026-10-14.md) for [CampLoopTesting](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity). The author permits ignoring mounting animation for now. The proposed first slice is local mount attachment, direct riding control, useful rough-terrain traversal and safe dismounting. Values and behavioral choices here are candidates for implementation and acceptance, not shipped mechanics.

The [implementation plan](CAMP_LOOP_LEGGER_RIDING_IMPLEMENTATION_PLAN_2026-10-14.md) selects the first-slice architecture, source boundaries, ordered batches and proof gates from this exploration.

Recommend controlling the Legger as an agile extension of Booter's movement: point the stick where you want to go, accelerate into a fast gait, slow precisely near camp, and step across terrain that currently obstructs the pair. Preserve weight through acceleration, braking and terrain contact. The first slice does not need a vehicle transmission, new animation downloads, stamina economy or jumping system.

## Animation free mounting

Ignoring animation removes clip acquisition and contact timing as prerequisites. It does not remove a local physical eligibility check. Mount request should brake and reserve the Legger, then commit a seat attachment only after Booter, seat and combined clearance are valid. A small explicit Mounting state can represent braking and validation without advancing an animation clock. Keep a bounded timeout so a blocked approach cannot reserve the companion forever.

Use an authored seat frame and a placeholder standing or static pose. Do not require a chair clip or disguise a frozen mining/crouch clip as riding. Seat height must come from this greybox's actual body and rider envelope: the existing Legger box top is 1.4 m above its root, not an assumed horse saddle height. Booter's calibrated capsule is 1.82 m tall. A standing placeholder may demand considerably more headroom than a future seated posture; keep the first clearance profile honest and revise it explicitly when a seated pose exists.

Treat mounting as one short, local, checked transfer to the seat, with an authored maximum entry separation. Trace a feasible entry corridor for Booter's shape; do not demand that his standing capsule physically intersect the solid Legger body while approaching the seat. Ignore only the selected mount's own body where the deliberate seat transition requires it, and still validate surrounding world geometry. If the corridor or seat is blocked, reject with feedback. This permits a local attachment shortcut while preserving the rule that the Legger never relocates to Booter.

Booter's root remains the same inventory, interaction and survival owner. Acquire explicit ride ownership, capture the pre-ride body/collider/presentation settings, clear velocities, suppress on-foot gravity and movement, and switch the rider to a passive representation. Use a query shape for rider clearance while his ordinary world collider is suppressed. Keep the Legger's existing body as the physical owner. Do not parent a live dynamic Booter Rigidbody beneath the mount or add a second mount body.

Prefer retaining both actor roots and deriving the rider pose from the mount's seat in the same controlled physics step. Use a shared interpolated presentation frame for mount, rider and camera so independent smoothing does not make Booter slide around the seat. The physics pose stays authoritative; render alignment must not write gameplay position. Restore captured settings through an idempotent release operation rather than enabling every component unconditionally. Avoid disabling Booter's whole motor as a shortcut: its OnDisable clears action ownership, lab capability and presentation state.

The animation driver still matters even without a mounting clip. Its Update writes visual local position/rotation, advances locomotion transitions and can emit footsteps. Its OnDisable resets several presentation owners but does not destroy the playable graph. Add a small mounted presentation override that suppresses on-foot gait, foot projection and foot contacts while retaining equipment. Keep it separate from the lab's clip-required `TryBeginAnimationPresentation`. The first requirement is a rider that remains attached and does not walk in the air.

## Direct movement options

| Model | Advantage | Cost | Recommendation |
| --- | --- | --- | --- |
| Camera-relative velocity control | Same movement vocabulary as Booter; precise analog creep and fast changes of direction. | Needs controlled reversal and coordinated facing to avoid sideways skating. | First camp slice. |
| Forward throttle plus steering | Strong feeling of committed momentum. | Extra steering model; awkward reverse and tight camp turns. | Consider only if direct movement fails the desired feel. |
| Click or stick-selected route destination | Reuses autonomous planning. | Planner can override player intent and introduce delay near obstacles. | Keep for autonomous follow; not direct riding. |

Use the existing [movement basis](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DMovementBasis.cs) so camera orbit and diagonal input remain consistent. Preserve analog magnitude after deadzone processing. Full stick requests the selected gait; small deflections request proportionally smaller movement. Keyboard movement remains full magnitude, with an optional explicit slow modifier if needed after testing. Do not introduce a second deadzone that makes creep impossible.

Separate velocity response from facing. At low speed, permit tight turns and facing toward intended movement. At high speed, decelerate harder as the desired heading diverges, then accelerate into the new direction. Reversal should not instantly rotate the velocity vector while keeping its full magnitude. Releasing the stick should stop without drifting through camp props. Terrain vetoes should remove unsafe motion immediately rather than waiting for the normal braking curve.

Do not use the autonomous follower's 0.75-second route cycle for direct steering. Each physics tick should resolve current player intent, terrain contact and clearance. Keep input sampled through the existing router, with no new mouse polling or device owner.

## Candidate tuning and stopping distance

| Parameter | Initial candidate | Purpose |
| --- | --- | --- |
| Normal gait | 6 m/s maximum | Useful travel while keeping room for analog precision. |
| Fast gait | 10 m/s maximum | A measurable speed benefit beyond both this lab and the production saved sprint baseline. |
| Acceleration | 16 m/s squared | Reaches 10 m/s in about 0.63 s on flat empty ground. |
| Stop braking | 24 m/s squared | Stops from 10 m/s in about 0.42 s over 2.08 m under a constant-deceleration model. |
| Heading reversal | 24 m/s squared before accelerating away | Firm direction changes without instant full-speed reversal. |
| Facing | 500 degrees/s at low speed, blending toward 360 at fast gait | Tight maneuvering while retaining visible turning at speed. |
| Continuous slope | 55 degrees as a bounded study candidate | Greater than Booter's existing 48-degree walking limit; use verified support and clearance. |
| Discrete step up | Up to 1 m, at reduced speed | Tests a meaningful gain over the follower's 0.55 m limit. Requires a clear supported landing. |
| Controlled step down | Up to 1.25 m, at reduced speed | Tests uneven terrain without granting unrestricted cliff falls. |

These candidates are stronger than the original follower values and require corridor proof before acceptance. At the existing Heavy speed factor of 0.82, a 10 m/s fast gait becomes 8.2 m/s, roughly 11% above the production saved 7.4 m/s sprint and 2.16 times the lab's 3.8 m/s sprint. Keep the existing cargo speed/acceleration factors initially; recommend braking that remains effective at all accepted loads. Do not infer kilograms or add rider mass to cargo without a separate decision.

The stored fixed timestep is approximately 0.02 s. Motion at 10 m/s covers about 0.20 m per tick; this already exceeds the thickness of some obstacles. A cast across the full displacement is necessary even when an endpoint looks clear. Prepare dressing collision ahead by at least the verified stopping distance plus actor footprint, query margin and preparation latency allowance. The present follower's broad look-ahead is useful, but any future speed change must update that bound deliberately.

## Rough terrain without terrain generation

Separate four cases: smooth inclines, shallow uneven ground, discrete ledges and missing support. On a continuous incline, project travel onto verified support rather than treating the rise over a route cell as a step. Specify speed along the terrain surface so a steep climb does not secretly gain extra distance per second from vertical projection. Use local normals and support continuity; height difference alone cannot distinguish a staircase from a slope.

For uneven ground, sample the center and several points around the footprint. Select support near the previously occupied surface, within local vertical limits, so a ray from above cannot switch to a roof or another floor. Reject large unsupported regions and narrow perches. Require a stable support pattern rather than every corner being exactly coplanar, which would reject ordinary rocks and terrain undulations. Support hysteresis may reduce chatter but must never retain permission past an actual ledge.

For a step, detect a forward obstruction, verify its top is an allowed support surface, check upward clearance, then sweep across to a full-footprint landing. Consume the step through small bounded physical motion at reduced speed, continuing to inspect current geometry. Do not use a diagonal sweep through the vertical face and assume raising the destination solves it. Step-down needs its own safe descent and headroom checks. For the first slice, stop before an unsupported drop beyond the profile; free falling and emergency rider ejection can be a later explicit mechanic.

Use a combined clearance envelope for the mount and rider. Keep the root upright for initial physics predictability, with restrained slope lean on presentation only if useful. Translating a box cast at one orientation does not check the volume swept by a rotating rectangular body. Either conservatively enclose the yaw rotation or validate bounded intermediate orientations and their inflated swept envelopes. Budget substeps by displacement and corner travel; exhausting the query budget reduces permitted motion, not safety checks.

Offer sliding along a valid wall tangent when forward travel is blocked, but recheck ground support and the combined shape along the slide. Limit contact iterations and prevent tiny alternating corrections from producing jitter. Report blocked speed from actual achieved displacement, not commanded speed. Existing follower `CurrentSpeed` can remain nonzero while WaitingForRoute; that is unsuitable as the sole riding animation or HUD speed signal.

Ground permission must be explicit. Existing terrain and marked support are candidates; salvage wreck shells, roofs, camp equipment and narrow rock tips remain obstacles unless deliberately authored for traversal. Avoid blanket ground tags or a layer change across the scene. The newly appearing uncommitted [NPC motor](../../../Assets/_Project/Scripts/Runtime/TopDown3D/NPC/NpcGroundMotor.cs) offers five support samples, overlap checks and segment casts, but assumes a foot-origin upright capsule and NPC tuning. It is a useful comparison, not an accepted drop-in replacement for Legger's box, rider envelope or step solver. Do not make riding depend on that concurrent enemy batch.

## Input and interruption rules

Keep contextual tap Interact for mounting, then make tap Interact request dismount while mounted. Mounted Interact must be intercepted before world harvesting and station actions, regardless of which world target is nearest. Keyboard tap/hold recognition must preserve E's cargo hold behavior; a completed hold must not also mount or dismount. Gamepad West remains tap interaction; its cargo shortcut is currently disabled. Show one clear riding/dismount prompt with the active rebound binding.

Represent riding as locomotion/activity state beneath the existing Gameplay, Inventory, Radial and SystemMenu modes. That preserves existing navigation and rebinding. A dedicated Riding map is possible later, but currently router capture predicates assume Gameplay; adding the map alone would suppress commands or break menu return. Put mount/dismount intent into the existing input-update arbitration with explicit priority: reset and menu cancellation first, then mounted dismount, then allowed ordinary commands. Keep command epochs so a queued request cannot execute after reset or a different mount relationship.

| Event | Proposed response |
| --- | --- |
| Mount pressed while moving or following | Reserve and brake locally; commit only when valid, or time out without attachment. |
| Dismount pressed at speed | Brake immediately; try a safe exit after stopping. New steering cancels the pending exit and resumes riding. |
| All exits blocked | Stay mounted, report no safe exit, and return control so the player can reposition. Do not retry every tick indefinitely. |
| Inventory, cargo or radial opens | Suppress riding intent and brake physically while remaining mounted. World interaction remains local. |
| System menu opens | Retain the relationship, suppress intent and suspend mount/dismount commits. Do not assume opening the menu globally pauses physics. |
| Focus or controller lost | Clear commands and safe-stop intent; require appropriate rearming before resuming. Remain mounted. |
| Jump, crouch, attack, camp or waypoint command | Reject with concise feedback until a compatible riding action is deliberately implemented. |
| Call Legger while mounted | Ignore or report Already riding; never arm a competing follow/call-over task. |
| Lab reset | Invalidate ride intent and release transient attachment before Booter's safe return; retain the Legger's real coordinate. If release/return is unsafe, keep input blocked. |

The existing [menu controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DMenuController.cs) changes input mode but does not itself set timeScale to zero. The world speed controller owns acceleration requests from 1x to 4x, not a pause lease. Treat menu suspension as actor control state, and retain scaled physics time for any physical braking. Do not add a second global timeScale owner for riding.

## Safe exits and companion continuity

Use authored left and right exit corridors, with rear as an optional fallback. Try the player's indicated side first, then a stable fallback order. Validate descent from seat, landing support, slope, standing capsule and nearby Legger clearance. Booter may need a different ground height from the mount. Recheck the path and endpoint at commit even without animation.

Dismount is an atomic handoff: resolve an exit, place Booter locally, restore his captured physics/presentation state, release riding authority, then enable on-foot input and autonomous companion behavior. Commit only at low speed; no arbitrary inherited launch velocity. Failed validation leaves the valid mounted relationship intact. If the selected companion is disabled or destroyed, preserve both actors' last known poses, try a bounded local safe detach, and fail closed when no legal location exists. Never fall back to world origin, nearest camp or a newly spawned Legger.

After dismount, hold the Legger briefly at his actual stopped position while Booter clears the body, then rebuild follow intent from current positions. Avoid stale route/call-over commands and immediate foot overlap. This is a physical pause, not a teleport or saved-route restoration.

## Motor choices and reference limits

Recommend a bounded kinematic Legger motor with explicit overlap, sweep, support and step validation. Autonomous follow and riding should supply different intents to that same owner. A narrow extraction from the follower preserves cargo and identity contracts better than adding another FixedUpdate movement component. Start with no production scene opt-in; use a camp-only installer with exact authored references and no whole-scene rebuild.

Unity documents kinematic MovePosition with interpolation and physics-step movement. That supports retaining the current Rigidbody approach; it does not establish our terrain or collision rules. [Unity 6.4 MovePosition](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rigidbody.MovePosition.html).

A built-in CharacterController provides wall sliding, slope limits and step offset, but uses its own collider/control behavior and does not react to forces or automatically push Rigidbodies. Replacing the current box/body would require a deliberate migration and still leave rider clearance, safe exits and support identity to solve. Treat it as an alternative if the bounded motor proves too costly, not a prerequisite or package addition. [Unity 6.4 CharacterController](https://docs.unity3d.com/6000.4/Documentation/Manual/class-CharacterController.html).

BoxCast takes one box orientation and translation direction; rotation clearance requires our separate treatment. ComputePenetration can test hypothetical poses and support bounded overlap recovery, but ignores backfaced triangles. Never rely on depenetration alone to repair a crossed wall or restore invalid terrain support. [Unity 6.4 BoxCast](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.BoxCast.html), [Unity 6.4 ComputePenetration](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Physics.ComputePenetration.html).

## First playable scope and evidence

The first implemented slice should finish animation-free mount, direct analog riding, two gait speeds, supported slopes/steps, safe dismount, camera framing, modal suppression and lab reset. Mounting animation, seated contact polish, jumping, falling, mounted combat, rider mass and production persistence follow separately. Missing mount clips no longer block this slice.

Use a flat maneuvering patch and a bounded rough-terrain corridor in the camp scene. Compare full-stick gait, partial-stick creep, 90/180-degree changes, camera orbit during travel, wall glancing, narrow turns, slope thresholds, legal and blocked steps, step-down, low ceilings and blocked exits. Include changed obstacles during pending mount/dismount and cargo load changes. Automated contract checks cover ownership, collision/support, input priority, interruption and complete release; the author's playtest determines feel. No gameplay smoke test is created or run by this exploration.

World contract remains fixed authored terrain with no generation or terrain streaming. Retain `legger.primary`, existing cargo identity and authored support constraints. Collision preparation must cover the actual motion corridor even when dressing renderers are absent. Scene unload clears transient ownership. Camp lab state remains session-only; production mounted checkpoints need a later versioned coordinated restore, never independent pose loads that detach or duplicate the pair.

Continuation inspected `main` after `60767c96b`, including concurrent uncommitted inventory, crouch and NPC work. New evidence comes from animation-driver lifecycle, movement basis, world-speed/menu code, population coverage checks, reset ordering, NPC motor source and the stored fixed timestep. Braking distances and load comparisons are calculated candidates, not executed traversal measurements. Gameplay and Unity assets are unchanged.

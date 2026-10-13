# Camp Loop player and animation integration plan

Date: 2026-10-13. Part two of the [implementation plan](./CAMP_LOOP_ANIMATION_PLAYGROUND_IMPLEMENTATION_PLAN_2026-10-13.md). Integrate the new character and all accepted animation variants into the existing Booter actor, with movement and interaction systems that make the poses behave correctly in the game.

## Player and animation ownership

Keep the existing Camp Loop Testing Booter root and its Rigidbody, capsule, inventory, vitals, interaction/action controller and footstep component. Assign a new character visual prefab and a lab locomotion/profile set on that actor. Keep the existing input router, camera, EventSystem and companion. Do not create a second controllable character or second health value.

Extend `TopDown3DPlayerAnimationDriver` through optional character/capability/catalog configuration. With no lab configuration, retain the current visual constants, 21-slot locomotion/camp/gather graph and public caller behavior. With it, the same driver creates the new visual and remains the only output to its Animator. The new visual/profile defaults apply only in Camp Loop Testing; production serialized assets are not repointed.

Recommended graph: keep the existing 21-slot mixer as the normal locomotion/camp branch; add a small directional/posture branch for crouch/strafe blend sets and a two-port action branch for crossfading the current and next one-shot/loop. A final mixer, inside the same PlayableGraph, chooses the appropriate branch. Create/connect clip playables at action or posture changes, not on every frame. Only selected blend clips evaluate; 68 candidates do not mean 68 active playables. Do not attach a competing Animator Controller or Timeline output.

The driver exposes visual-ready and action presentation handles so hand sockets/anchors can bind after its `Awake` visual instantiation. Initialization must work whether the coordinator subscribes before or after the visual becomes ready. Use explicit component references, not repeated scene searches. On disable/unload, unsubscribe, release ownership and destroy graph resources through the existing driver lifecycle.

## Planned code and serialized assets

All new runtime files are under `Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/`, in `BooterBigArm.TopDown3D.Runtime`, namespace `BooterBigArm.TopDown3D`. Editor helpers remain in `BooterBigArm.Editor`; tests in the existing `BooterBigArm.Editor.Tests` assembly.

| Planned type or existing seam | Contract |
| --- | --- |
| `TopDown3DCharacterPresentationProfile` | Visual prefab, calibrated scale/offset, feet plane, hand/head sockets and collider fit. New lab asset; missing optional profile preserves the old visual path. |
| `TopDown3DActionClipCatalog` and clip entry data | Stable action/variant IDs, reviewed clip metadata and readiness from the import plan. No raw curves embedded in public data. |
| `TopDown3DPlayerCapabilityProfile` | Lab-only allowed actions, posture/speed/clearance parameters and motion policies; null means current behavior. |
| `TopDown3DPlayerActionCoordinator` and pure action state/marker ledger | Availability, reservation, phase, priority, exactly-once effects and interruption; explicit adapters into existing camp/gather ownership. |
| `TopDown3DPlayerActionMotor` | Requests movement modes through `TopDown3DPlayerMotor`; never writes a parallel player Transform/Rigidbody authority. |
| `ITopDown3DActionTarget` and station target components | Non-resource actions, stable target identity, anchors, reservation and availability; preserve `ITopDown3DInteractable` and Ironstone consumption contract. |
| `TopDown3DPlayerPropHandler` | One held prop, socket alignment, release, physics ownership and restoration; separate from inventory rewards until an explicit transfer commits. |
| `TopDown3DPlayerCombatActions` and `TopDown3DLabDamageReceiver` | Strike windows, directional/severity requests and reaction/death selection, using existing survival vitals as health authority. |
| `TopDown3DAnimationPlaygroundController` and view | Action palette, station context, diagnostics and reset; subscribes to the existing input/camera/UI owners. |
| `TopDown3DAnimationPlaygroundSession` and existing `GreaterWastelandInventorySaveService` | Explicit session-only checkpoint policy for this configured scene, resolved before restore/writer creation; autosave/manual/quit cannot persist lab damage or actor position. No marker preserves current behavior. |
| `TopDown3DLabMiningTarget` and session reward ledger | Separate lab target/state, not a thirteenth production Ironstone component. Reward feedback is ephemeral and cannot enter the normal persisted inventory sink. |
| Existing driver, motor, input router, interaction/action controller | Add bounded optional interfaces/configuration; retain current production and camp/gather paths. Inspect all callers and serialized consumers before changing a signature. |

Settings: `Assets/_Project/Settings/AnimationPlayground/BooterCharacter.asset`, `BooterLocomotion.asset`, `PlayerCapabilities.asset`, `ActionCatalog.asset` and `AnimationPlaygroundActions.inputactions`. Visual prefab: `Assets/_Project/Prefabs/AnimationPlayground/Player/MaleBaseMeshVisual.prefab`. The old profile's twelve roles are all required; reuse/retarget and recalibrate the existing eight transition/directional roles alongside the new four preferred idle/walk/run/sprint choices. Source preparation resolves the two-contacts-per-foot contract before this profile is assigned.

## Action lifecycle and arbitration

An action request contains the selected action/variant ID, target ID/reference, held-prop identity, input intent and current posture. Return either an action instance or a specific availability reason. Reserve the target and motor lease atomically; if either fails, release everything acquired by this request.

Phases are **approach → align → enter → active/contact → exit/recovery → complete**. Each has a timeout or completion condition. Pure state logic decides phase and interruption; scene components supply probes, target anchors and effects. Keep animation time and action phase on one playback clock. The motor applies collision-tested movement in FixedUpdate; animation presents that state. Paused/seekable preview uses a separate clock and cannot execute effects or speed up survival/camping time.

Use the existing `TryBeginActionConstraint(owner)` / `EndActionConstraint(owner)` as the exclusive ownership boundary. Extend its motion mode deliberately: it currently preserves vertical velocity, so acquiring the constraint alone does not implement a ladder, seat attachment or jump. Only the owning action can request constrained velocity/path, posture, facing or gravity treatment. Revalidate when an anchor moves, a target disappears or an exit becomes blocked.

| Priority or interruption | Rule |
| --- | --- |
| Death | Stop further effects, end reservations, suspend normal gameplay commands, enter reviewed terminal pose. Only lab reset or explicitly compatible recovery can leave it. |
| Damage | Existing vitals/camp damage behavior stays authoritative. Small hits may preserve a committed interaction; large hits abort when safe. Traversal falls/exits follow their physical recovery rather than snapping to idle. |
| Camp, gathering, canister placement or another owned action | New requests are rejected while another owner holds the motor; the lab does not steal its lease. Existing flows may use adapters, not a second independent busy flag. |
| Inventory, menu, pause, focus loss or device disconnect | Consume input at the router. Station-specific policy pauses or safely exits the action; no held confirmation press is replayed into gameplay on return. |
| Target removal, scene unload, reset or actor disable | Release reservations/props/graph subscriptions exactly once, restore motor/posture/gravity state, cancel queued uncommitted effects. |

Each effect has an `(action instance, marker, cycle)` identity. Pickup, release, button activation, hit and mining reward each commit once. Before the marker, cancellation releases the reservation. After a committed marker, preserve that effect and run recovery. Mining/push loops can have deliberate per-cycle behavior; loop evaluation alone never awards another item. Process skipped markers across a long frame correctly and never re-fire effects when a presentation clip is sought backwards.

## Movement and posture behavior

| Behavior | Physical implementation and failure behavior |
| --- | --- |
| Idle/walk/run/sprint | Preserve camera-relative motor control and momentum. Tune nominal clip speed against measured visual displacement/contact phases; retain start/stop/pivot behavior. Reject a profile that requires unreasonable playback speeds. |
| Strafe | Lab capability decouples facing from travel. Use walk/run left/right clips while facing the selected camera/target direction; do not substitute existing turning gait for strafing. Fall back to the normal forward motor mode only after a declared mode exit. |
| Crouch and duck | Change capsule height/centre with feet anchored and restore only after a ceiling/side clearance test. Blend forward/back/left/right crouch loops from planar intent. A duck is a timed action with recovery, not necessarily the sustained crouch state. |
| Jump/fall/landing | Activate the dormant Jump action only for the lab capability. Physical takeoff/velocity/gravity drive airborne state; source jump clips provide reviewed takeoff/airborne/landing segments. A landing occurs on valid contact, not on a clip timer. Prevent repeated held-button jumps. |
| Slide/dodge/roll | Entry speed/posture and collision sweeps control travel. Slide exits to crouch if standing is blocked. Dodge ends in a checked stable position; missing forward/left variants are not silently fabricated. Roll landing owns a recovery interval. |
| Cover | Reserve a wall plane/tangent and valid segment. Align enter, blend idle/sneak, clamp along the wall, probe ends/corners and exit on valid clearance. The present clips implement crouched cover; standing wall-hug remains a gap. |
| Ladder | Reserve bottom/top anchors, fit rung spacing and cycle height, enter from an allowed position and drive vertical progress through the motor's explicit ladder mode. Restore its normal gravity/movement path on safe exit or abort. Block a top exit with insufficient space. Descent is unavailable until reviewed. |

Root motion stays disabled by default and source trajectories remain measurable. The import policy and action motor decide whether to discard extracted travel, normalize it or consume it through collision checks. Never apply both clip travel and motor displacement to the actor. Do not toggle global foot IK merely to hide contact errors; initial contact is calibrated through anchors, poses and measured offsets. Any later IK feature must be scoped and verified separately.

## Prop and interaction behavior

Pickup has a reachable target, a reservation, grip alignment, an attach marker and an inventory/held-object decision. Examine rotates the held prop around a reviewed socket. Throw detaches once at the release marker, computes launch position/velocity, ignores actor self-collision initially and restores collision after separation. Stop double Rigidbody simulation while held and clean up a lost prop without inventing a new item.

Button pressing commits one switch change at hand contact. Push start/loop/stop constrains the actor and crate together along a checked lane; obstruction stops both. Seats and bedrolls provide enter, contact, idle and exit anchors. Sleeping Idle is a loop inside the existing camp/rest lifecycle; missing lie-down/wake/stand-up transitions need adaptation before full execution is accepted.

Mining uses a blunt tool-use prop and a resettable lab resource target. Adapt the axe-style source to the intended grip and impact plane, then commit the lab strike/reward through shared action/transaction rules. The lab target is not another `TopDown3DAuthoredIronstoneNode`, `TopDown3DIronstoneNode` or production resource cache: existing scene validation requires exactly twelve Ironstone nodes and one cache. Send lab rewards to a session ledger, not the ordinary inventory reward sink. Production gathering retains its existing controller and reward path; reset cannot farm persistent inventory.

Punch/kick use reviewed hit windows and reach probes, with one hit per target per strike. Damage requests include source direction/severity; vitals changes once and the animation reacts. A passive training dummy and deliberate lab damage controls are enough; enemy AI, weapons and combat balance are outside this slice. Death/get-up pairings must match compatible final floor posture and clearance. Health restoration is a lab reset operation, not another persistent survival system.

## Proposed lab controls

Clone the action asset for the lab, preserve established action semantics/binding IDs, and add explicit stable IDs for new lab actions. Extend the existing router with commands gated by the lab capability; no gameplay component reads buttons directly. The clone uses lab preferences or defaults and cannot write production `controls-v1.json`.

These proposed additions were checked against the current source bindings: V, F9, left-stick press, right trigger and Select have no current source binding. Batch 0 must also inspect active overrides and coactive map/reserved-control rules. C, F6, D-pad and left shoulder are occupied.

| Intent | Gamepad | Keyboard/mouse | Routing |
| --- | --- | --- | --- |
| Move / camera / sprint | Existing sticks / right shoulder | Existing WASD / mouse / Shift | Preserve current router and camera behavior. |
| Jump | South | Space | Existing dormant action, now handled only when lab capability allows. |
| Crouch or slide | Left-stick press | V | Toggle crouch normally; sprinting with adequate speed requests slide instead. |
| Context interaction | West tap | E tap | Existing target-at-press/revalidate-at-release logic; hold remains Legger cargo. |
| Selected punch/kick/throw/dodge | Right trigger | Left click / existing Attack | Execute the currently selected applicable action; exclude effects while palette/UI is open. |
| Open action palette | Select | F9 | A lab modal consumes gameplay input; includes variants, context reason, preview and reset. |
| Cancel active lab action | East, only while a cancellable lab action owns control | Backspace / existing CancelItemAction | Cancellation resolves before canister pickup; idle East retains existing pickup behavior. Unsafe ladder aborts follow a safe exit policy. |
| Palette navigation/confirm/back | Existing UI navigation / South / East | Existing UI keys / Enter / Escape | Held inputs are ignored across map transfer until released. |

Preserve inventory, radial, algae, canister, camp and device-switch prompts. Derive labels from the router/action bindings. Pressing West near a station never activates both cargo and the station. Settings rebinding must not import lab-only controls into production or reserve a control whose other coactive action is still live.

## Reset and diagnostics

One reset service restores action/motor/capsule state, temporary health, held/thrown props, crate/switch/target state and the actor's validated return pose. It clears ledgers and input edges and destroys session effects. It does not teleport Legger. Use explicit registered participants and authored snapshots rather than scanning and deleting every Rigidbody in the scene.

Checkpoint isolation must be explicit: `GreaterWastelandInventorySaveService` captures survival and actor transforms on its 30-second timer and on quit. Resolve a scene-owned session-only policy before its Start restores a checkpoint or creates a writer. In the configured playground it performs neither checkpoint restore nor write; the UI explains that saves are disabled for the lab. Autosave, explicit Save, disable and application quit all honor the same policy, with no late queued write. A scene without the marker retains the existing save/load path. Reject this marker in GreaterWasteland, test byte-identical pre-existing checkpoint files across lab use, and keep production controls preferences independent from lab bindings.

Show selected clip and action phase, posture, motion owner, current target/availability, prop ownership and marker/hit status. Let the author pause/seek preview, compare variants and choose a close camera view through the camera owner. In physical mode, state remains observable but animation seek cannot bypass physics or replay an effect.

## Acceptance and failure containment

Add focused fixtures for catalog gating, lifecycle/markers, motor modes, input conflicts, props, damage and reset. Regress current `TopDown3DPlayerLocomotionTests`, `TopDown3DSmartTraversalTests`, `TopDown3DInteractionTests`, resource/inventory tests, relevant modal/controls tests, survival/camp tests and checkpoint tests when their seams change. Verify null lab configuration preserves the existing driver/motor path. Test long frames, repeated inputs, ceiling blockage, lost targets, interruption before/after commit, blocked exits, scene disable/reload and repeated reset.

The author checks deformation, grip, foot sliding, responsiveness, transitions, collision and readable cover behavior in the furnished scene. Do not call clip selection or an EditMode test proof of those qualities. A failing family remains visibly disabled with its exact reason while accepted families remain usable; no silent animation substitute counts as completion.

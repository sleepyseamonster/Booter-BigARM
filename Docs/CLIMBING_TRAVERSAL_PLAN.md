# Booter Terrain Climbing Plan

Status: prototype implementation in progress, 2026-09-28. The user requested traversal from steep slopes through vertical walls and overhangs, with increasingly weighty hand and foot animation. The motor now uses the angle bands when Sprint is held and the animation driver uses its existing gather clip as a visible climbing placeholder. The current 48 degree ordinary walk limit remains unchanged; final contact, top-out, and visual acceptance are pending.

Climbing is intended as a major gameplay system. The current region should establish trustworthy movement, contact, and readable terrain assessment before later regions add equipment-dependent routes. The Legger is the companion's new name; older code and asset identifiers still use BigARM for reference safety.

## Current implementation boundary

- `TopDown3DPlayerMotor` owns a dynamic Rigidbody and capsule. A sphere cast beneath the capsule, followed by a raycast for the true triangle normal, accepts only surfaces at or below `maxWalkableSlope` (48 degrees in the production scene). `TopDown3DSlopeMath.RemoveSteepUphillComponent` then rejects uphill input above that limit.
- `TopDown3DPlayerAnimationDriver` owns the single Humanoid Playables mixer. Its current clips cover ordinary locomotion, side steps, vaulting, and gathering. There are no incline, scramble, wall climb, or overhang clips in the project.
- The active procedural chunk mesh samples one height per horizontal vertex. It cannot form vertical walls or overhangs. The World Creator architecture reserves bounded feature meshes or volumes for those shapes; that path needs collision and streaming proof before it becomes production terrain.

## Traversal contract

Treat slope angle as a transition input, not as a new walkable-slope limit. Proposed initial bands, subject to visual and controller tuning:

| Contact angle from world up | Motion mode | Animation intent |
| --- | --- | --- |
| 0-35 degrees | Existing walk/run | Existing grounded gait |
| 35-55 degrees | Incline walk | Shorter uphill stride, stronger forward lean, slower ascent |
| 55-75 degrees | Scramble | Hands occasionally support the ascent; slower, deliberate steps |
| 75-105 degrees | Wall climb | Continuous alternating hand and foot contacts |
| 105-160 degrees | Overhang climb | Body hangs below the surface, slowest travel, continuous contacts |

The 48 degree ordinary walk limit stays in place. The existing Sprint action is bound to gamepad right shoulder; holding it while pushing uphill starts climbing when contact passes the checks. Without Sprint held, uphill movement beyond that limit remains blocked. Keep Sprint held and release movement input to hold position while contact remains; releasing Sprint exits climb and restores gravity. Use angle hysteresis near each boundary so a player does not flip modes on adjacent mesh triangles. Speed should scale with difficulty, with no sprint boost during scramble or climbing. Reverse input descends. Lateral input moves along the contacted surface. At a safe top edge, transition to ordinary grounded motion; at a lost contact or unloaded collider, return to gravity and normal falling behavior. Never invent a handhold or move through solid geometry.

## Physics and contact ownership

1. Keep the existing Rigidbody and capsule as the only movement authority. A climb state may suspend gravity while valid contact is present, but must restore the previous gravity state on exit, teleport, action constraint, disable, and destruction. It must not alter vault or side-step ownership.
2. Probe the intended surface from the capsule, require an authoritative terrain/feature collider and a usable triangle normal, and recheck contact every physics step. Check capsule clearance at projected positions and at the top-out destination. Do not accept decorative colliders merely because they are near terrain.
3. Build movement from a surface tangent and a stable outward normal. Camera-relative input must map unambiguously to up, down, and sideways travel even on vertical faces. For a near-horizontal ceiling, use the maintained contact frame rather than deriving ascent from a vanishing horizontal normal.
4. Handle convex corners and seams by reacquiring nearby contact within a bounded distance and angle. Cap the correction speed and reject discontinuous position jumps. Keep the character on the exposed side of the collider.
5. Publish traversal mode, measured angle, contact normal, and along-surface speed in the motor's read-only locomotion snapshot. Presentation consumes this state and never moves the gameplay body.

## Animation ownership

Replace the temporary gather-clip climbing placeholder with authored Humanoid clips for incline walk, scramble, wall climb, and overhang climb. Candidate sources include the free, CC0 [KayKit Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) pack, which advertises climbing motions; its exact clip coverage and retargeting onto Booter still need verification. Use explicit clip availability checks in the scene builder and validator when real clips are integrated. Blend across bands using the same contact state that drives movement. Keep footstep dust confined to grounded gait; climbing contact effects, if added later, should use authored hand/foot contact events. Avoid root motion, procedural foot-goal rewrites, and a second Animator authority because the current locomotion recovery deliberately excludes them.

## Procedural world and persistence

- **World identity and stable identity:** The climb state is transient and does not create generated objects. It attaches to the current collider and surface normal. Any future climbable feature must derive its stable identity from the owning seed, generation version, chunk/feature coordinate, and authored rule, following the World Creator contract.
- **Streaming:** Only active colliders can support a climb. A missing, disabled, or unloaded collider ends the mode safely. Chunk loading around a climber must include vertical reach and top-out space; no movement through an unloaded feature.
- **Authored constraints:** Authored landmark geometry may define valid climbing surfaces and top-outs. The collision mesh, visible geometry, and surface classification must agree.
- **Persisted deltas:** There is no world delta for climbing itself. Save/restore must place the player at a validated position; a restored mid-climb state must reacquire a matching loaded contact or fall safely. Do not serialize a Unity collider reference.

## Terrain assessment and tools

For the current region, ordinary inclines and clearly exposed hand-and-foot surfaces can remain traversable with Sprint held. The player should be able to read the route before committing: visible surface shape and contact material, a consistent movement response at each angle band, and a clear reason when a climb fails. Surface angle alone is not enough for harder regions. Add an authored or deterministic surface affordance record for grip, exposure, wet/loose condition, usable handholds, and a top-out. Keep it separate from the visual material so gameplay collision and art can be validated together.

Future tool-assisted routes should declare a required capability and contact points in that affordance record. Inventory/equipment supplies the capability; the motor checks it at entry and each contact transition. A missing or exhausted tool must prevent that route before gravity is suspended, with readable feedback. Tools may change reach, anchoring, or security, but must not let the motor skip clearance, contact, or valid top-out checks. Do not invent a mandatory equipment list for this region until the tool design is approved.

The Legger uses a separate traversal profile. A Booter handhold does not imply a passable Legger route. Generated and authored features need to expose both profiles so macro routes can choose a physical detour or honestly report that the Legger cannot follow.

## Implementation and proof order

1. Add a small, testable surface-frame and mode-selection model, including hysteresis, upside-down normals, and zero-input hold behavior.
2. Add incline and scramble movement against existing generated mesh slopes, preserving current walk, vault, action, and teleport behavior. Add focused EditMode tests for the transition rules and contact-loss cleanup.
3. Add wall and overhang motion against a bounded feature mesh with authoritative collider. Prove the exposed-side constraint, edge top-out, corners, unloading, and restore behavior in isolated automated fixtures. Do not use a gameplay smoke test without the user's request.
4. Integrate the selected animation clips into the sole Playables mixer, then validate transition timing and contact placement with the user in interactive play.
5. Wire the bounded feature family into deterministic chunk generation and document repeat-seed, chunk-boundary, unload/reload, and save/restore evidence before production cutover.

Acceptance requires the player to ascend, pause, descend, traverse sideways, top out, and lose contact safely on each supported shape; the visible gait must match each band. Source compilation and EditMode tests alone do not prove the final feel or animation quality.

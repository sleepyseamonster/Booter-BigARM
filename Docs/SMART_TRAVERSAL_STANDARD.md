# Smart Traversal Standard

This document defines the first perspective-player seam for contextual sprint traversal. It is an evolving implementation standard, not a commitment to final animation timing or a complete move set.

## Input Contract

- Smart traversal uses the existing higher-level sprint and movement state from `TopDown3DInputRouter`.
- It does not add a vault button or inspect raw device controls.
- A move may begin only while the player is grounded, holding sprint, and supplying intentional movement input.
- Once a move begins, it completes as a short controlled traversal so releasing a button cannot strand the Rigidbody in an invalid mid-obstacle state.

## First Move Set

- A generated natural obstacle must carry `TopDown3DTraversalObstacle` before the player considers it traversable.
- When a running approach intersects the narrow outer side band of a rock and the complete path is clear, the motor chooses a directional left or right sidestep away from the rock and slightly forward.
- When the approach is centered and the rock is no taller than the low-fence vault limit, the motor chooses a short forward hop.
- A centered rock above the vault limit remains blocking. The system does not silently phase through it or force a blind dodge.
- An approach that is too slow, too far from the side band, too far from the obstacle, or lacks side clearance remains ordinary collision.

## Safety And Ownership

- `TopDown3DPlayerMotor` remains the Rigidbody movement authority and owns traversal state.
- `TopDown3DTraversalPlanner` owns deterministic move selection and path math.
- The motor checks for walkable landing ground and capsule clearance before beginning either move.
- The sidestep samples its complete curved path before beginning; blocked left or right paths do not trigger.
- The player collider remains enabled during traversal so physics still rejects unexpected obstruction.
- Traversal timing is calculated from path distance and capped at normal run speed. A vault never injects sprint speed on completion.
- Traversal ends with a short cooldown to prevent one held sprint input from repeatedly retriggering on the same rock.
- Teleport and component-disable paths cancel traversal and restore gravity.

## Initial Tuning

- Normal run and sprint speeds remain `4.20` and `7.40`; acceleration/deceleration are reduced to `18`/`24`, with a `540` degree-per-second turn cap for a heavier body response.
- Minimum movement input: `0.50`
- Minimum approach speed: `2.80` world units per second
- Forward probe distance: `0.75` world units
- Maximum vault height: `0.80` world units above the player's feet
- Side-region threshold: `0.76` of the rock's projected half-width
- Minimum vault duration: `0.68` seconds, extended as needed to remain at or below run speed
- Minimum sidestep duration: `0.45` seconds, extended as needed to remain at run speed
- Traversal cooldown: `0.28` seconds

These values are tuning-ready defaults. Hands-on gamepad feel, visual clarity, and final animation timing remain user-owned acceptance.

## Deferred Moves

Jump, slide, roll, non-rock dodge, character vaulting, stamina interaction, invulnerability windows, root motion, and animation-event timing are outside this first slice. They should extend the same planner/motor seam rather than introduce separate input or transform-motion paths.

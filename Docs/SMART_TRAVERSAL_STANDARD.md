# Smart Traversal Standard

This document defines the first perspective-player seam for contextual sprint traversal. It is an evolving implementation standard, not a commitment to final animation timing or a complete move set.

## Input Contract

- Smart traversal uses the existing higher-level sprint and movement state from `TopDown3DInputRouter`.
- It does not add a vault button or inspect raw device controls.
- A move may begin only while the player is grounded, holding sprint, and supplying intentional movement input.
- Once a move begins, it completes as a short controlled traversal so releasing a button cannot strand the Rigidbody in an invalid mid-obstacle state.

## First Move Set

- A generated natural obstacle must carry `TopDown3DTraversalObstacle` before the player considers it traversable.
- When the approach intersects the outer side of a rock, the motor chooses a 360-degree spinning sidestep away from the rock and slightly forward.
- When the approach is centered and the rock is no taller than the configured vault limit, the motor chooses a forward vault arc.
- A centered rock above the vault limit remains blocking. The system does not silently phase through it or force a blind dodge.

## Safety And Ownership

- `TopDown3DPlayerMotor` remains the Rigidbody movement authority and owns traversal state.
- `TopDown3DTraversalPlanner` owns deterministic move selection and path math.
- The motor checks for walkable landing ground and capsule clearance before beginning either move.
- The player collider remains enabled during traversal so physics still rejects unexpected obstruction.
- Traversal ends with a short cooldown to prevent one held sprint input from repeatedly retriggering on the same rock.
- Teleport and component-disable paths cancel traversal and restore gravity.

## Initial Tuning

- Minimum movement input: `0.35`
- Forward probe distance: `1.25` world units
- Maximum vault height: `1.15` world units above the player's feet
- Side-region threshold: `0.48` of the rock's projected half-width
- Vault duration: `0.48` seconds
- Spin duration: `0.34` seconds
- Traversal cooldown: `0.20` seconds

These values are tuning-ready defaults. Hands-on gamepad feel, visual clarity, and final animation timing remain user-owned acceptance.

## Deferred Moves

Jump, slide, roll, non-rock dodge, character vaulting, stamina interaction, invulnerability windows, authored animation clips, root motion, and animation-event timing are outside this first slice. They should extend the same planner/motor seam rather than introduce separate input or transform-motion paths.

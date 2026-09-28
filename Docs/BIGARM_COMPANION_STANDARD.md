# The Legger Companion Standard

This document is the canonical implementation baseline for how the Legger exists, moves, and coordinates with Booter. The user's current direction controls when it conflicts with older mobile-base language.

**Character name (2026-09-28):** the Legger, formerly BigARM. Existing `BigARM` code and asset identifiers remain stable until an intentional reference-safe naming pass.

## Locked Direction

- The Legger is Booter's companion, not a rover, vehicle, mobile habitat, home base, safe zone, or crafting platform. His finite loadframe can carry expedition cargo, but it is not a depot or remote menu.
- Booter and the Legger form one synergistic gameplay partnership even when they are physically apart.
- The Legger may wander, pursue autonomous tasks, and make local movement decisions.
- The Legger always has a true world position.
- The Legger must traverse the world to reach Booter. A call, recall, large separation, unloaded chunk, stuck state, save/load, or scene transition must never silently snap or teleport him to Booter.

## First Follow Slice

The perspective prototype establishes the loaded-world behavior:

- use one simple upright rectangular prism, taller than it is wide, with no secondary round shoulder shape during the greybox prototype;
- record Booter's recent route and follow a position along that route instead of orbiting a camera-relative formation point;
- use a follow band so the Legger can stop without jittering around an exact coordinate;
- accelerate, decelerate, turn, and slow for sharp heading changes;
- avoid local obstacles and detect stalled movement;
- enter a faster physical catch-up state when called or far behind;
- enter `WaitingForTerrain` when startup ground is unavailable;
- use a bounded local ground route search over loaded colliders. Reject terrain above the Legger's slope limit, excessive steps, occupied body space, and unloaded gaps; report `WaitingForRoute` when no physical route exists. The local search must never convert Booter's climbable surface into an implicit Legger path.

`WaitingForTerrain` and `WaitingForRoute` are honest simulation boundaries, not permission to relocate the Legger. The current grid only plans within roughly eight meters of the loaded position and does not yet solve a long detour, travel beyond loaded terrain, or a route around a Booter-only cliff. Replan at a bounded interval and validate every physical step against live collision so a stale route cannot drive through changed geometry.

## Next Simulation Seam

Before the Legger can wander beyond the streamed area, define and validate a world-scale traversal owner that preserves:

- the Legger's authoritative world coordinate and task;
- a traversable route or route-progress record between the Legger and Booter;
- deterministic progress while detailed terrain is not rendered;
- streaming priority around the Legger and, when needed, along the route corridor;
- collision- and terrain-valid re-entry into full physical simulation at the Legger's simulated coordinate;
- save/load continuity without converting absence or failure into a teleport.

The exact mix of multi-anchor streaming, coarse off-screen simulation, navigation data, and route persistence remains the next design decision. The local search does not need a navigation package or a streaming ownership change.

## Route data contract for the next slice

- **World identity:** build macro route cells/edges from the world seed, generation version, chunk coordinate, and authored feature rule. Keep the route profile explicit: Booter can scramble and climb; the Legger needs enough width, clearance, traction, step height, and slope tolerance for its body and load.
- **Streaming:** retain coarse route truth when detailed colliders unload. Pause detailed movement at the last valid coordinate if the next route segment is unavailable; request chunks through the established world owner when that path is implemented.
- **Stable object identity:** authored route constraints and generated route obstacles must refer to stable feature identities, not transient collider instance IDs.
- **Persisted deltas:** save the Legger's authoritative coordinate, active task, route destination/progress, and any world changes that block a previously valid edge. Revalidate on restore. Never serialize local grid samples or teleport to Booter as recovery.
- **Authored constraints:** mark Booter-only climbs and deliberate Legger detours in feature rules. A route that is unavailable to the Legger should be legible to the player before they leave him behind.

## Proof Boundary

Automated checks can prove serialized structure, speed rules, color identity, and the absence of an immediate call-time relocation. Natural feel, obstacle behavior, controller response, and extended traversal require the user's hands-on playtest acceptance.

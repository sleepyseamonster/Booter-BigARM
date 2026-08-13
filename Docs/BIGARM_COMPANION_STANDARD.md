# BigARM Companion Standard

This document is the canonical implementation baseline for how BigARM exists, moves, and coordinates with Booter. The user's current direction controls when it conflicts with older mobile-base language.

## Locked Direction

- BigARM is Booter's companion, not a rover, vehicle, mobile habitat, home base, safe zone, storage depot, or crafting platform.
- Booter and BigARM form one synergistic gameplay partnership even when they are physically apart.
- BigARM may wander, pursue autonomous tasks, and make local movement decisions.
- BigARM always has a true world position.
- BigARM must traverse the world to reach Booter. A call, recall, large separation, unloaded chunk, stuck state, save/load, or scene transition must never silently snap or teleport him to Booter.

## First Follow Slice

The perspective prototype establishes the loaded-world behavior:

- record Booter's recent route and follow a position along that route instead of orbiting a camera-relative formation point;
- use a follow band so BigARM can stop without jittering around an exact coordinate;
- accelerate, decelerate, turn, and slow for sharp heading changes;
- avoid local obstacles and retain stuck-recovery steering;
- enter a faster physical catch-up state when called or far behind;
- enter `WaitingForTerrain` when the ground needed for the next physical step is unavailable.

`WaitingForTerrain` is an honest simulation boundary, not permission to relocate BigARM. The current slice does not yet solve travel beyond loaded terrain.

## Next Simulation Seam

Before BigARM can wander beyond the streamed area, define and validate a world-scale traversal owner that preserves:

- BigARM's authoritative world coordinate and task;
- a traversable route or route-progress record between BigARM and Booter;
- deterministic progress while detailed terrain is not rendered;
- streaming priority around BigARM and, when needed, along the route corridor;
- collision- and terrain-valid re-entry into full physical simulation at BigARM's simulated coordinate;
- save/load continuity without converting absence or failure into a teleport.

The exact mix of multi-anchor streaming, coarse off-screen simulation, navigation data, and route persistence remains the next design decision. Do not install a navigation package or expand streaming ownership until that slice is explicitly approved.

## Proof Boundary

Automated checks can prove serialized structure, speed rules, color identity, and the absence of an immediate call-time relocation. Natural feel, obstacle behavior, controller response, and extended traversal require the user's hands-on playtest acceptance.

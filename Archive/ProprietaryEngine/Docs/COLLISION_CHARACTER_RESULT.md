# Collision and Third-Person Foundation

P09/P10 implemented 2026-09-10. Jolt collision, a foot-position capsule controller and obstruction-aware third-person camera now connect to the shared runtime and workbench. This is a first character-calibration pass; imported character art, animation, checkpoints and a separate player app follow in P11–P13.

## Implementation

`engine_physics` hides Jolt behind engine-owned vectors, stable labels and lifetime tokens. Jolt 5.6.0 is pinned at `e77f175595e64cb44218cc9d9d56fc365ad0e36a` in the existing runtime lock. Its MIT notice is retained. `CMake/Physics.cmake` builds only the library, keeps upstream flag overrides out of the parent application, and uses baseline CPU options rather than assuming AVX2 on the eventual PC. [Jolt's architecture/character documentation](https://jrouwe.github.io/JoltPhysics/) and the pinned HelloWorld/CharacterVirtual sample informed lifecycle and update order. No upstream source was modified.

The physics owner provides static boxes with normalized rotations, triangle meshes, heightfields, static/dynamic capsules, ray hits with normals, swept spheres, capsule overlap and self-exclusion filters. Stable collision labels map to owner/incarnation tokens; removed or foreign tokens cannot mutate replacement bodies. Dynamic contacts enter a bounded event queue and are sorted by stable identity/subshape before consumption. Removed-contact events retain cached identity after Jolt destroys the body. Callbacks do not mutate the game world.

The first profile admits 2,048 bodies by default, 100,000 triangles per mesh, 8–512 square power-of-two heightfields, an 8,192-event/contact cache and a 16 MiB temporary allocator. Coordinates and shape inputs are finite and bounded. Update/contact-capacity failures report explicitly; CharacterVirtual's hit-capacity flag is also checked. The adapter runs Jolt's single-threaded job system; shared worker scheduling comes with P19. This is not a target-PC budget or a cross-platform physics determinism claim.

`CharacterController` uses a 0.35 m radius, 1.8 m tall capsule with feet as the logical position, a 45-degree maximum slope, 0.35 m step allowance and configurable jump speed. `ExtendedUpdate` supplies collision recovery, stair walking and floor sticking. Gravity and jump intent advance at the shared 60 Hz tick. A slightly inset kinematic inner body makes the character visible to ordinary ray/overlap queries; camera queries exclude that body. The controller owns its removal, and shared physics lifetime prevents teardown while a controller still exists. Multiple-character behavior, arbitrary moving platforms and climbing are not accepted by these fixtures.

`Game/Locomotion.h` converts actions to camera-relative movement with normalized diagonal speed, walking/running and one-shot jump intent. The workbench ticks commands/world state, character motion and rigid-body physics in explicit order, then adopts the authoritative pose without a second velocity integration. Previous/current poses supply presentation interpolation.

The camera follows a pivot 1.35 m above feet, sweeps a 0.2 m sphere toward the requested orbit position, contracts immediately before an obstruction and smooths only outward recovery. A pivot already embedded in solid geometry falls back to a very short view; robust recovery from that invalid placement is still open. No shoulder swapping, aiming system or camera feel acceptance is claimed.

## Workbench use

Expand **Shared simulation** and enable **Enable character**, then click outside the inspector. WASD/left stick moves, Space/South jumps, Shift/left-stick click runs, right-drag/right stick orbits, and P/Start pauses. The rendered capsule matches the default controller dimensions. The physical ground and scale marker match the renderer's existing transforms. Character mode caps camera distance at 8 m; normal inspection retains its larger orbit range.

The calibration ground is finite. Walking off it tests ground loss and falling; checkpoint/recovery behavior is not implemented yet. Character poses are not stored in inspection documents. No new game content or creative acceptance follows from this fixture.

## Procedural contract

The physics adapter creates no world seeds, generated IDs or geographic canon. Callers supply authored/generated labels; Jolt body IDs remain transient and never enter saves. A future chunk owner creates collision when a region is ready, removes its owned bodies on unload, and resolves fresh tokens after reload. Character inner bodies are controlled by their controller lifetime. Positions currently occupy a bounded local physics frame; region rebasing and streamed collider readiness remain P20 work. Authored constraints enter through shape/pose/controller inputs. Authoritative gameplay deltas and player snapshots remain the world/save owner's responsibility in P13/P21, not contact callbacks.

## Evidence and limits

[Focused native evidence](../Evidence/P09-P10-runtime/result.json) covers ray/mesh/heightfield/sweep/overlap, a falling dynamic capsule, contact identity after destruction, invalid/stale/budget cases, character query visibility, a low step, tall blocker, walkable and steep ramps, jump/landing, ground loss, camera wall clearance/recovery, and identical fixed-tick motion at 30/60/144 render rates. The low step reached 0.253 m; the tall blocker stopped the capsule at x=4.38 m. Geometry checks include the new capsule's outward winding and normals. Existing simulation cases passed alongside these checks.

The native workbench compiled with the physical runtime and capsule renderer. This batch did not run a gameplay smoke test, physical-controller session, Windows test, visual acceptance session or repeated rendering/package suite. The current graphical integration is compile-backed; the new controller/camera behavior is proved by native technical cases, not a claim of hands-on play quality. The earlier packaged executable is historical. A representative animated player/rendering pass follows after mesh/skin import.

Next: P11 restricted glTF/skin import, animation sampling and GPU skinning, then P12's shared player application. P06 inspection commands remain ready; further lighting polish is not the next step.

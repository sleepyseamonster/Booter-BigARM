# Camp Loop legger enemy audit and AI foundation

October 14, 2026. Scope: audit and brainstorm for the first enemy in [CampLoopTesting](../../../Assets/_Project/Scenes/Reference/CampLoopTesting.unity). Build toward one simple legger that notices Booter's approach, keeps its distance and flees. This is a proposed implementation foundation; this audit does not install a model or AI in the scene.

The strongest first slice is a separate NPC actor with a replaceable blockout visual, a small explicit state machine and collision-aware escape planning. Robustness should come from clear ownership, bounded work, safe failure and repeatable tests. An exhaustive class hierarchy, combat framework or behavior-tree editor would add commitments before the first enemy proves the contracts.

The [continued NPC and game AI methods brainstorm](NPC_AND_GAME_AI_METHODS_BRAINSTORM_2026-10-14.md) compares decision methods and develops perception, execution, scheduling, group coordination, persistence and verification seams. It refines Blocked into an execution result while retaining Flee as the actor's behavioral intent.

The [first Lager enemy implementation plan](CAMP_LOOP_LAGER_ENEMY_IMPLEMENTATION_PLAN_2026-10-14.md) selects the bounded first slice, tuning workflow, source boundaries and proof gates. Lager is the user's current working display name; the companion Legger remains unchanged.

## Direction and setting boundaries

The user selected a legger as the first enemy and skittish avoidance as its initial behavior. Enemy classification does not imply immediate aggression: this actor has no attack, damage, loot or recruitment behavior in the first slice. The companion Legger remains an independent actor with its existing cargo and physical call-over authority.

[World Basis](../../WORLD_BASIS.md) defines the companion but does not establish a general legger species, machine origin, anatomy or hostile faction. Use `Skittish Legger` as a working display name and separate type, instance and affiliation IDs. Anatomy, intelligence, ecology and the relationship to the companion remain open. Do not infer canon from the proposed ghost-machine wreck.

The current fixed, always-loaded terrain instruction controls this implementation, even though World Basis retains a broader infinite-world description. Neither enemy AI nor this audit reactivates generated terrain or the old generated-world save service.

## Existing implementation audit

| Surface | Current evidence | Implication |
| --- | --- | --- |
| Camp Loop scene | [Scene builder](../../../Assets/_Project/Scripts/Editor/TopDown3D/CampLoopTestingSceneBuilder.cs) validates nine native terrain/collider pairs, Booter, one companion follower, camp route, seven camp rocks, twelve Ironstone nodes and a cache. [Animation status](CAMP_LOOP_ANIMATION_IMPLEMENTATION_STATUS_2026-10-13.md) records the added station layout and ongoing qualification. | Install one NPC additively in the existing scene; preserve terrain, actors and stations. The original builder is not a complete validator for all later playground content. |
| Companion movement | [Follower](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs) owns a kinematic Rigidbody, ground projection, occupancy checks, a 13 by 13 route window, acceleration, turning, stuck tracking and physical call-over. | Reuse movement concepts and the pure path search. Do not attach this follower to the enemy or invert its follow distance: it also owns companion input, cargo and task semantics. |
| Route search | [Local grid pathfinder](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DLocalGridPathfinder.cs) checks up/down limits and blocked diagonal corners. It may return a reachable endpoint closer to the goal rather than reaching that goal. [Existing tests](../../../Assets/_Project/Tests/Editor/TopDown3DLocalGridPathfinderTests.cs) cover a blocked corridor and steep/unloaded barriers. | Partial progress is not proof of a safe escape. Classify complete, partial and failed results and evaluate the actual endpoint and route. |
| Navigation cost | The search allocates cost, parent and closed arrays and scans the grid for the next cell. Ground and occupancy probing belong to the follower, not a shared navigation service. | Suitable for a bounded first actor; repeated searches for many candidates and many actors need budgets, shared sampled grids and later measured optimization. |
| NPC foundation | Searches of active TopDown3D runtime sources found no general enemy/NPC, faction, threat, aggro, damageable or behavior-tree framework. | Add a small NPC lane rather than extending archived runtime implementations. Player resource targeting is not threat perception. |
| Blockout art | [Prototype builder](../../../Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DPrototypeBuilder.cs) creates a cube named `Legger Greybox Body`; the current scene companion also has cargo presentation children. | There is no qualified enemy model established by this audit. Build a dedicated visual subtree; copying the entire companion imports unrelated ownership. Do not execute the old generated-world builder for this task. |
| Time | [World speed controller](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Simulation/TopDown3DWorldSpeedController.cs) owns accelerated simulation through timeScale. | NPC movement, memory and cooldowns use scaled simulation time; NPC code never writes timeScale. An open menu is not automatically a simulation pause. |
| Lab lifecycle | [Playground session](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.cs) marks Camp Loop as session-only. [Reset registry](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundResetRegistry.cs) invalidates all owners before ordered restoration. [Checkpoint service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs) suppresses startup and writes for that session. | Integrate an explicit enemy reset participant; keep disk persistence suppressed. Reset must cancel routes and threat memory before returning the NPC to a validated fixture anchor. |

This audit inspected saved scene/source files and package manifests. It did not query live unsaved Editor state or establish fresh compilation, rendering or gameplay acceptance. Concurrent animation and workstation work changed repository state during inspection; this document owns no changes to those files.

## First model and encounter proposal

Create one reusable prefab with an unscaled physics root, one main collider, and a `Visual` child. Use a compact body, a readable front sensor and a few angular segmented legs built from Unity primitives. Four legs are a disposable blockout choice, not species canon. A starting envelope around 1.3 m long, 0.9 m wide and 1.1 m high is a tuning proposal. Measure the complete silhouette and collision clearance against the 1.80 m Booter baseline before accepting dimensions.

Use subdued rust/dark metal and a small pale sensor to distinguish it from the cargo companion without relying only on color. Primitive leg children have no colliders. A simple gait may animate visual pivots from actual displacement, while the body motor remains the sole position authority. No Humanoid rig, private player animation dependency, Blender source or root motion is needed for the blockout. A later model can replace `Visual` without replacing instance identity or behavior.

Place it on a clear side excursion approximately 20 to 35 m from camp, after surveying the current terrain, rocks, stations and other proposed fixtures. Require at least two escape directions with full body clearance. Protect camp/rest/return anchors and station entrances with authored exclusion areas. Give the actor a bounded activity area inside the retained terrain; approaching that boundary changes route selection rather than despawning it or bouncing it toward Booter. Exact placement remains an implementation measurement.

## Skittish behavior proposal

| State | Behavior and transition |
| --- | --- |
| Idle | Stand and make small visual motions. Valid Booter detection enters Alert; a close threat can immediately enter Flee. Idle wandering is optional later. |
| Alert | Face the perceived threat briefly. Approach inside the comfort radius enters Retreat; loss of threat enters Recover after memory expires. |
| Retreat | Move along a safe route to increase distance. Hold the selected escape direction for a minimum interval to avoid circling and jitter. Close pressure enters Flee. |
| Flee | Run to a reachable safe endpoint. Replan when the route is invalid, threat moves materially or the bounded deadline expires. |
| Recover | Slow down and remain wary. Return to Idle only after a wider release distance or expired threat memory and a calm interval. Renewed pressure preempts recovery. |
| Blocked | Brake at safe support when there is no legal route. Retry on a bounded cooldown and expose the failure reason. Trapping does not grant an attack or teleport escape. |

Starting tuning: detection 14 m, comfort entry 10 m, panic entry 5 m, calm release 16 m, threat memory 2 seconds, calm interval 3 seconds, perception 5 Hz, route retries no faster than every 0.5 seconds. Set retreat/run speeds relative to the currently configured Booter speeds during implementation; turn rate and acceleration must remain finite. These values are experiment defaults. Emergency collision/support checks run every physics step, independently of slower decision ticks.

Detection uses explicit same-scene Booter binding, range and line of sight to body sample points. Within a small configured near radius, a proximity check can trigger panic even outside the view cone; it must still respect intervening geometry. Ignore self, visual triggers and stale/disabled actor handles. Deduplicate compound colliders by actor identity. Only Booter is the initial perceived threat; companion response, sound, dust visibility and multiple-threat aggregation are later profiles.

Record a last perceived position and timestamp. After line of sight breaks, flee relative to that remembered position until memory expires; never continue reading the hidden player's exact transform as if it were perceived. With multiple threats later, relationship and threat scoring select intent rather than treating every nearby collider as hostile.

## Escape planning and safe movement

Sample a bounded ring of candidate destinations, biased away from the perceived threat. Reject unsupported, too-steep, occupied, excluded or out-of-area candidates. Score legal routes by endpoint separation, route exposure, clearance, travel cost and continuity with the current route. Resolve equal scores with stable ordering, not Unity's global random state.

Build one sampled local grid per planning pass and reuse it across candidate evaluation. Cap candidate count, physics probes and search expansions. The first implementation may call the existing pure search on that grid; measure its allocations and cost before scaling the population. Do not run a full ground scan every frame or search the entire valley.

Evaluate the returned path rather than just the requested destination. Require a meaningful improvement in separation for ordinary retreat. A detour may temporarily reduce distance only within an explicit exposure allowance and must end at a safer reachable point. If no candidate satisfies the policy, enter Blocked. Do not label a partial route that ends at a cliff or dead end as successful escape.

The motor sweeps the full collider along each movement segment, checks support and step/slope bounds, brakes before hazards and reports observed progress. Destination occupancy alone can miss a thin wall between samples. Saturated query buffers fail conservatively with diagnostics. Missing ground, overlapping threats and a zero away-vector use stable fallback directions or stop safely; none permits NaNs or relocation.

Use a kinematic Rigidbody in FixedUpdate as the initial motor authority, with explicit sweep checks rather than assuming kinematic movement will resolve every obstacle. This follows Unity's [MovePosition contract](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Rigidbody.MovePosition.html). Presentation observes motion; it never writes the physics root.

NavMesh remains a future backend option. Unity's [AI Navigation documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html) describes geometry-derived meshes, dynamic obstacles and traversal links. The repo has the built-in AI module but no direct AI Navigation package dependency. Defer installation and whole-scene baking; package changes need their own authority and navigation backend selection should follow measured route requirements.

## Extensible NPC skeleton proposal

Keep runtime code in `Assets/_Project/Scripts/Runtime/TopDown3D/NPC/`, within `BooterBigArm.TopDown3D.Runtime`. Proposed contracts are not existing types:

| Responsibility | Initial contract | Future extension |
| --- | --- | --- |
| Definition | `NpcDefinition` asset with stable type ID and referenced behavior, perception and locomotion profiles | Variants share profiles without copying behavior code. |
| Instance | `NpcActor` with authored stable instance ID, definition and lifecycle epoch | Generated spawn identity can later map into the same actor contract. |
| Perception | Timestamped `ThreatObservation` tied to a valid actor handle | Hearing, multiple targets, visibility and faction attitudes. |
| Decision | Pure `SkittishBrain` consumes observations/state and emits one movement intent | Other brains can use state machines, utility scoring or behavior trees through the same intent contract. |
| Navigation | `INpcNavigation` reports complete/partial/blocked/unavailable routes, endpoint and reason | Later NavMesh or regional routes do not rewrite the brain. |
| Movement | `NpcGroundMotor` executes one intent and reports grounded/blocked/progress state | Distinct flying, burrowing or climbing motors when actually needed. |
| Presentation | `NpcBlockoutView` reads speed, facing and behavior state | Species rig, animation, audio and effects remain replaceable. |
| Lab integration | Scene-specific binding and reset adapter | Generic NPC runtime stays independent of AnimationPlayground types. |

Treat enemy as a relationship, legger as an archetype, skittish as a behavior profile and ground movement as a capability. Future combat roles can include scavenger, ambusher, pursuer, ranged attacker and heavy machine; these are brainstorm categories, not accepted game taxonomy. Avoid an inheritance chain such as enemy to machine to legger to fleeing legger. Companion AI need not migrate into this framework for the first enemy to work.

One actor has one brain and one movement authority. A small serializable runtime state holds current state, timers, perception memory and route request revision. Immutable definitions never hold per-instance mutable state. Disable, scene unload and reset invalidate requests by actor ID plus lifecycle epoch; stale callbacks cannot move a recycled actor. Scene bindings must resolve actors explicitly rather than finding the first object named Legger.

Expose selected threat, state/transition reason, remembered position age, route status, endpoint, stuck duration and work counters in the Inspector and optional scene gizmos. No always-visible debug HUD is needed. Start with bounded per-actor ticks for this one enemy; add shared scheduling and simulation detail tiers only when population measurements justify them.

## Identity terrain and persistence contract

- **Deterministic world identity:** terrain identity stays untouched. The authored actor receives a stable ID such as `lab.npc.skittish_legger.001`, separate from `lab.npc.legger.skittish` type identity. Cosmetic/decision variation uses a local seeded source if needed. Stable ordering does not promise cross-machine deterministic physics.
- **Chunk streaming and unload/reload:** terrain streaming is not applicable because Camp Loop uses fixed, always-loaded native tiles. Scene unload still cancels subscriptions, requests and actor handles. Future NPC simulation tiers must not change real world position or resurrect an actor from its prefab default.
- **Stable generated-object identity:** generated NPC spawning is not in this slice. Reserve separation of spawn identity and Unity instance handles; do not implement procedural population to demonstrate one authored enemy.
- **Authored constraints:** obey collider envelope, slopes, steps, camp exclusions, station footprints, fixture bounds and available support. Keep the original terrain bytes and GUIDs.
- **Persisted runtime deltas:** this lab is session-only and writes no enemy state to disk. Reset restores the fixture intentionally, not as gameplay recovery. A future production save slice must version NPC records for real position/orientation, removed/dead state and selected durable behavior/task data; transient routes and perception handles are rebuilt. Do not bolt a second save file onto this enemy or reuse generated-world save services.

## Implementation sequence and proof gates

1. Recheck live saved scene, actor scale, package pins and concurrent ownership. Survey one safe fixture area. Seal production scene, terrain and companion baselines before authoring.
2. Add the definition/identity, observation/intent contracts and pure skittish brain. Test threshold hysteresis, memory expiry, emergency preemption, invalid targets, malformed profiles and stable fallback directions.
3. Add bounded ground navigation and the single motor. Test complete versus partial routes, actual endpoint safety, corners, thin walls, slopes, cliff edges, exhausted budgets, saturated queries, stuck/retry behavior and a fully trapped actor. Reuse relevant existing pathfinder checks without modifying the companion owner.
4. Author the dedicated primitive prefab/profile and additive scene installer under `Assets/_Project/Scripts/Editor/`. Installation is idempotent, validates the exact lab identity and never rebuilds the whole scene. Register reset through an adapter; cancel before restoration, validate support/clearance, clear velocity and memory, and leave the fixture safely disarmed if restoration fails.
5. Validate import/compilation and focused EditMode contracts in an isolated background candidate. Check pause/acceleration consistency, disable/re-enable, scene unload, repeated reset, duplicate IDs/owners and unchanged scene/terrain/companion references. Do not focus Unity or run gameplay smoke tests.

User acceptance: the silhouette reads from the elevated camera; slow approach causes a wary retreat, close pressure causes a stronger flee, obstacles cause legal detours, and withdrawing lets the actor calm without jitter or tracking through walls. Confirm repeated pursuit stays inside safe terrain and that reset returns only the new fixture without changing companion position/cargo or player station ownership.

Defer attacks, health/death/loot, factions beyond the initial relationship setting, pack behavior, sound sensing, procedural spawning, long-distance world simulation and production persistence. Build those against the proven first-actor contracts when their gameplay is requested.

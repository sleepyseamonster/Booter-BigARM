# Audit of the first Lager enemy plan

October 14, 2026. Reviewed the [implementation plan](CAMP_LOOP_LAGER_ENEMY_IMPLEMENTATION_PLAN_2026-10-14.md) against the current playground lifecycle, reset registry and local pathfinder. Verdict: the scope and architecture are sound, but seven execution contracts need correction before the plan can reliably guide implementation. Two directly threaten the intended behavior or reset containment.

This audit records proposed corrections without changing the plan or implementing the feature. It is based on repository source and saved assets, not a live Editor query or gameplay run. Concurrent scene, animation, inventory and salvage work remains independently owned; HEAD advanced during inspection.

## Findings and required corrections

### P1 Maintain threat awareness while fleeing

Plan line 46 proposes a 180-degree sight cone and 3 m close awareness; lines 73 through 75 release behavior through memory expiry. The plan never defines a sensor orientation independent of the motor. If sight follows body-forward, turning away to flee puts pursuing Booter behind the cone. At more than 3 m, sight cannot refresh memory even with clear visibility. The Lager can calm after two seconds while Booter is still following inside its comfort radius.

For this first slice, use 360-degree horizontal sight with occlusion and a measured vertical eligibility limit. Keep the visible front sensor as presentation, not a directional sensing promise. If directional sight is later desired, define an independently aimed sensor and rear-awareness behavior explicitly. Do not keep memory alive by reading the hidden Booter's transform.

Required proof: a visible Booter following 6 to 9 m behind the moving Lager for longer than the memory duration keeps threat response active. An occluding wall still causes memory to expire. This verifies the approach/retreat contract without granting knowledge through obstacles.

### P1 Do not rearm the NPC on partial reset success

Plan line 116 specifies Actor-stage restoration and containment when the NPC's own anchor is blocked. It does not say what happens if the NPC restores successfully but a later participant fails. The [reset registry](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundResetRegistry.cs) restores sequentially and returns on the first failure. The [session reset method](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs) keeps player input blocked after any failure. Rearming inside the NPC's restore callback would allow it to resume during a globally failed reset.

Restoration must record a valid restored epoch but leave motion disarmed. Rearm only after a complete successful reset, valid session readiness and unblocked lab reset state. Use the existing session/registry result through the adapter; a new shared completion event is optional only if the existing state cannot express the condition safely. A tuning change, component enable or readiness notification must not override a failed-reset latch.

Choose an explicit ResetId and test its order against Booter's registered Actor-stage participants. The registry sorts same-stage participants by ID and synchronizes transforms only once before the Actor stage. Anchor-clearance queries must observe already-restored actors, including any required synchronization after their repositioning. Do not assume the earlier sync covers later actor moves.

Required proof: failures before and after NPC restoration leave NPC motion disarmed; a later full successful retry rearms it once, preserves valid tuning, and does not relocate the companion. Include an occupied NPC anchor and an overlap with Booter's return location.

### P2 Resolve complete state transitions and stop conditions

Plan line 73 says Flee releases to retreat/recovery after a wider condition but does not choose between those destinations. It also leaves unspecified whether visible Booter outside comfort but inside calm release interrupts Recover, when the alert timer starts, and whether entering Recover cancels the existing movement intent. Different implementations can pass selected tests while producing different behavior.

Add an ordered transition table. Evaluate lifecycle/safety gates first, then valid panic, active-flee release hysteresis, retreat retention until calm release, memory expiry and calm hold. Specify inclusive/exclusive boundary tests. Start the alert delay on entry to Alert; panic bypasses it. Flee releases to Retreat at the panic-release boundary while pressure remains. Recover begins only at calm separation or no remaining threat; entering it cancels escape movement and brakes safely. Renewed approach inside comfort resumes Retreat; valid panic resumes Flee. Seeing Booter at a calm distance does not itself reset the calm timer forever.

Required proof: test every transition and its equal-threshold case, plus sustained visible pressure, memory expiry during an active route and profile application while each state is active. Confirm Blocked remains movement status rather than an emotional-state transition.

### P2 Define the body envelope and continuous support query

Plan lines 79 and 83 require a full-body sweep and a simple collider but leave shape/orientation and support coverage open. A translating box cast does not by itself qualify the body while turning. A center support ray can also report ground while part of the body crosses a hole or ledge. Copying the companion's broad vertical projection would be inappropriate: [its follower](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs) uses projection ranges intended for its existing setup, including separate startup positioning.

Choose a conservative yaw-invariant vertical capsule or cylinder envelope for the disposable Lager, measured around the collision-relevant body. Visual legs remain nonblocking decoration. Alternatively, explicitly test rotational box clearance before applying yaw. Define root/feet offset, allowed support colliders, support coverage, slope and bounded vertical step movement together. Runtime ground probes may follow legal ground changes but cannot adopt a distant upper/lower surface or repair an unsupported spawn by snapping to it. Invalid startup stays disarmed.

Required proof: turning beside a wall, crossing a thin obstacle, crossing a partial-support ledge/hole, stacked surfaces and unavailable initial support. The current plan's endpoint and step checks alone do not prove these cases.

### P2 Specify route candidate construction and scoring

Plan lines 53, 55, 57 and 77 leave candidate radius, angular ordering and each normalization denominator undefined. The 13 by 13 grid at 1.25 m spans 7.5 m from center along each axis, while the behavioral destination can be 16 m from the threat. Without a local-horizon rule, an implementer may clamp several outside-grid candidates to the same cell or silently treat a local endpoint as having achieved calm separation. Undefined normalized terms also make the proposed 0.10 switching margin irreproducible.

Define a stable eight-direction order and construct distinct in-grid candidate cells, deduplicating before search. Treat these as receding-horizon waypoints: reaching one does not imply reaching the requested safe distance. Give formulas and denominators for separation gain, minimum route clearance, maximum route exposure, path length and heading continuity, including clamping and zero-value handling. Compare a still-valid current route and new routes against the same current observation/context. Preserve the 1 m gain/exposure rules as hard eligibility tests and report rejected candidates.

Required proof: clamped/deduplicated candidates, safe distance beyond one local horizon, tied scores, no eligible improvement, dynamic threat movement and current-route commitment. Fine-tuning should change known quantities rather than undocumented math.

### P2 Make work caps compatible with the reused pathfinder

Plan lines 59 and 79 promise validated caps but do not list them. The [existing pathfinder](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DLocalGridPathfinder.cs) allocates three arrays per call and has no external expansion-budget/cancellation parameter. It can scan all grid cells for every expansion. Wrapping it cannot enforce an arbitrary early-stop expansion budget or eliminate those allocations while leaving it unchanged.

For the initial reuse, fix a maximum 13 by 13 grid, at most eight deduplicated search calls per planning pass, one sampled ground/occupancy grid per pass, fixed query-buffer/probe limits and at most one due planning pass per actor update. Drop excess catch-up ticks rather than accumulating backlog under acceleration. Document that each search is bounded internally by grid size and retains its current allocations; measure those rather than claiming allocation-free routing. Use any tighter budget only through a separately justified implementation change.

Validate tuning cadence and speed against supported caps, and test that panic cannot trigger repeated full route builds on every decision tick. Invalidation may request the next eligible pass; immediate motor safety never waits for planning.

Required proof: worst-case call/probe counts, retry deadline under sustained blockage, acceleration catch-up, repeated panic requests and named allocation/cost measurements for the one-actor fixture. This does not establish large-population capacity.

### P2 Define activation independently of incidental callback order

Plan line 97 says register after the session is available and validate readiness, but does not select a readiness gate, initial registration owner or re-enable behavior. The session exposes `ReadinessChanged`, initializes early, and can recheck dependencies on enable. [Its source](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.cs) also cancels registry owners when disabled. Mere discovery of its component is not a successful activation contract.

The adapter should have serialized session, Booter and NPC references. Bind once after startup dependency checks, register a unique participant, and enable simulation only while the expected scene/session identity, valid profile/target and at least VisualReady state hold. Subscribe explicitly to readiness changes; loss of readiness suspends and invalidates motion. Re-enable resumes at the real position after support/clearance validation, without restoring the fixture anchor. Keep registration stable during reset; do not call Register/Unregister from reset invalidation/restoration callbacks, because the registry forbids registration mutations while resetting.

An invalid optional fixture stays disarmed and reports its own error without registering an unusable reset participant. A valid registered fixture that later cannot restore correctly follows the existing global reset failure policy. This resolves the plan's otherwise ambiguous promise that missing NPC configuration does not break the camp loop.

Required proof: dependency failure, late readiness, session disable/re-enable, NPC disable/re-enable, duplicate registration, reset followed by readiness change and teardown during scene unload. None may create a second motor owner or bypass the failed-reset latch.

## Scope that passes the audit

The dedicated NPC lane, immutable profiles, separation of behavior and movement status, stable instance identity, session-only persistence and additive scene installation remain appropriate. Keeping the companion, production scene, packages, inventory and combat outside this slice is coherent. The current owner handoff and isolated-candidate gates correctly account for concurrent work.

No new creative decision is needed to resolve these findings. The corrections narrow routine technical contracts within the requested skittish enemy behavior. Final art, species lore and balance remain provisional.

## Readiness and verification boundary

Revise the seven contracts and map their regression cases into FND, NAV and LAB before treating the plan as implementation-ready. Then refresh the shared scene baseline and ownership at PRE. Until those revisions, the plan's unconditional readiness statement is too strong.

Audit proof consists of source tracing and plan consistency checks. It does not establish Unity import, compilation, executed tests, rendering, NPC behavior, performance or author acceptance. No plan or runtime edits, scene saves, package changes, gameplay smoke tests or publication were performed by this audit.

Subsequent implementation on October 14 incorporated all seven corrections. See the [implementation evidence](../../Evidence/Gameplay/CampLoopNpc20261014/README.md) for the separate compilation, focused test and installed-scene proof; the findings above retain the original audit context.

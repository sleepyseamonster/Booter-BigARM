# Camp Loop platform, ramp and stair repair — 2026-10-16

This repair addresses the author's frozen jump/fall animation on grid platforms and unreliable walking on ramps/stairs. The [audited plan](../../../Design/Gameplay/CAMP_LOOP_PLATFORM_RAMP_STAIR_REPAIR_PLAN_2026-10-16.md) records the investigation, implementation order and safety boundaries. [Audit evidence](audit.json) records the original missing ground classification and its failing regression.

## Cause and changes

The saved scene had twelve station roots and 254 solid station colliders. Only AP02 carried the inherited ground marker: eleven roots and 240 station solids were unmarked. Another 39 static solids outside the stations were unmarked, including camp rocks, ironstone and workshop fixtures. The Rigidbody could collide with these surfaces while jump contact collection and ground observation rejected them. The fall action could then retain its busy/input ownership despite the capsule resting on a platform.

The Camp Loop session now registers active static solid geometry in its own scene and player ground mask before actor startup. Registration is runtime-only and idempotent; triggers, attached Rigidbodies, disabled/inactive geometry and excluded layers are skipped. It does not edit scenes, prefabs, terrain, geometry, asset GUIDs or persisted state. Actual hit normals still enforce the motor's walkable-slope limit.

Six existing movable test-prop colliders need physical support independently of that static registration. Ground probing, jump forecasts and actual jump contact collection share a physical-support predicate for these Camp Loop Rigidbodies. They can support landing and another jump without gaining a static observation/foot-adaptation identity. Prediction still cannot fabricate touchdown; unsupported static pose adaptation uses the existing animated fallback. This does not add platform velocity inheritance or qualify moving-platform foot planting.

Ground probing now examines all candidates for a valid true walkable face, grows bounded query buffers on saturation and separates the true-normal ray buffer from the sphere-cast candidates. A nearby unusable edge no longer prevents consideration of another valid floor. Adjacent static treads in one registered station share a transient observation identity, preserving travel/cadence continuity. Actual collider/matrix validity remains authoritative; movement, disable/destroy, foreign support, flight and reset still invalidate continuity.

Completed motion captures the Rigidbody position at fixed-tick entry, before step writes, and commits that captured travel after support resolution. The new sloped-step regression confirms a commanded vertical correction cannot become gait distance or false turn demand.

The existing Rigidbody motor now handles bounded standing steps up to its 0.28 m probe/step limit, with clearance skin. It requires a low encountered riser, a walkable static top, a clear standing capsule and clear upward/raised forward paths. Nearby confirmed step-down support closes the previous hovering gap. Rounded capsule/plane distance prevents adhesion from pulling an already-supported actor into an incline. Taller obstacles, ceilings, blocked paths, moving props, triggers, real drops, airborne actors and foreign action ownership reject the correction. Planar velocity and facing remain with the existing motor.

Accepted forward source assets, speeds (1.4 / 3 / 4 / 4.9 / 6.15 m/s), cadence/contact maps, moving-turn selection, LT controls, jump gravity/launch and dodge calibration remain unchanged. The motor's 48-degree slope limit and the animation adaptation's separate 40-degree bound remain distinct; pose-reach failures retain the accepted source pose.

## Verification boundary

All **570 checks across twenty focused EditMode suites passed**, including 46 new surface/step cases. [Verification results](verification.json) and [sealed code](sealed-code.json) bind the final selected files to the candidate and author readback. The new focused tests cover all twelve decks/approaches, landing forecasts without fabricated touchdown, all active static camp solids, registration idempotency/exclusions, stair observation continuity and support invalidation. Held motor/query cases cover all five gait speeds at a quarter-metre riser, descent, half-metre rejection, low ceilings, blocked raised paths, triggers/moving props, real drops, supported ramps through 48 degrees, saturated overlapping supports, nearby steep edges and unbound/foreign ownership.

Two retained legacy test fixtures needed calibration isolation: their cloned three-gait bindings now take the speeds from their own profile and clear the optional five-band moving-turn selection. Existing assertions remain; shipped bindings and profiles are unchanged. Failed original fixture results and the corrected final series are retained separately in the private checkpoint.

These are focused EditMode controller/query, startup, animation and pose checks in the marked hidden candidate. No author Play Mode, gameplay smoke test or physics simulation was run. Queries and injected completed-contact fixtures are not a recorded landing run: Unity's [EditMode physics API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/PhysicsScene.Simulate.html) does not dispatch ordinary collision callbacks. Hands-on feel and contact acceptance remain author-owned.

## Installation and recovery

Private checkpoint: `D:/BooterBigArmPrivateAssets/CampLoop/2026-10-16/SurfaceRecovery/`. It retains original source/protected hashes, seven original source backups, the current scene dependency seals, failed/final test results, final code freeze, installation plan and before/after author snapshots. No raw private animation payload is included in this receipt.

`Run-CampSurfaceRepairChecks.ps1` runs the twenty directly affected EditMode filters in a marked candidate and preserves each result in a fresh evidence directory. `CampSurfaceRepairInstallation.Import` requires stable author Camp Loop Edit Mode, exact candidate/protected hashes, original source/recovery bytes, unused new paths and GUID ownership. It imports only paired task code, never metadata paths directly, and requests compilation. It does not open/save/reload scenes, select another binding, drive input, change focus or enter Play Mode. `CampMovingTurnReadback.Snapshot` independently records current gaits, bindings, roots, dirty state, scene disk bytes and nine native terrain tiles. Pass absolute tool paths when targeting a candidate so retained tool versions cannot be selected accidentally.

This changes transient actor movement/support and presentation on fixed, always-loaded authored terrain. It creates no generated/world identities, chunks, streaming or persisted runtime deltas; existing authored collision constraints remain authoritative.

Author acceptance: start a fresh Play session in the current Camp Loop, walk up/down approach ramps and return stairs, then jump onto/off several station decks and camp surfaces. Confirm movement/idle recover after landing, LT remains steady and running/sprinting into jump or dodge still returns smoothly to movement.

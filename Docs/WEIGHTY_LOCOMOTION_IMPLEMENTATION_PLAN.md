# Grounded Locomotion Recovery And Weight Plan

Status: Batches 0-4 implemented with MoCap Central Core Motion on 2026-08-17; Unity import/test and user hands-on proof remain open

## 2026-08-17 Core Motion Buildout Record

The user purchased MoCap Central `Animation Pack - Core Motion` version `1.8.0`, satisfying the asset-decision boundary that previously stopped Batch 0.

- Both delivered archives passed container-integrity checks. Their SHA-256 hashes and license/source boundary are recorded beside the imported clips in `Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/README.md`.
- Only twelve Unity-skeleton animation FBXs were admitted: idle; no-root-motion walk, jog, run, forward-left jog, forward-right jog, jog start, jog stop, and jog pivot, plus explicit mirrored start/stop/pivot imports. Vendor models, controllers, demos, unrelated clips, root-motion duplicates, and package content remain excluded.
- Source-skeleton sampling measured two contacts per foot in each sustained two-stride loop. Those values, nominal speeds, one-shot plants, playback bounds, mirror intent, and preferred entry/exit roles now live in one `TopDown3DLocomotionClipProfile` asset.
- After the first live import, Unity exposed that these FBXs use the explicit animation subasset ID recorded in their importer metadata rather than the generic `7400000` clip ID. The profile references were corrected to that canonical ID so its twelve clips deserialize instead of appearing missing, and runtime validation now reports the exact missing or invalid prerequisite if this gate fails again.
- The Rigidbody snapshot now includes actual facing and measured yaw rate. The Playables driver has one explicit locomotion-state authority for idle, start, sustained travel, stop, pivot, traversal, and constrained action. Start/stop state is hysteretic, directional gait reads local desired trajectory plus measured yaw, and the dominant sustained clip is held within its profile playback bounds.
- Authored one-shots are non-looping, selected by the next calibrated plant, never apply root motion, and emit their explicit plant contact. Traversal and constrained actions remain higher priority and clear locomotion transitions.
- The scene and builder reference the same profile. The validator proves role completeness, project ownership, no-root-motion naming, Humanoid imports, and mirror agreement. An Editor-only hidden-preview reporter can inspect ankle contact candidates, root/facing drift, and loop gaps and can write only caller-approved contact phases.
- Runtime, Editor, and focused test sources compile together with Unity's own cached Roslyn response files. The only compiler warning is an unrelated pre-existing obsolete API use in `TopDown3DRockMeshTopology.cs`.

The Unity GUI still owns the project. No competing batchmode process was started and no visible Editor interaction was performed. AssetDatabase import/readback, the focused EditMode suite, the production validator, and the hands-on matrix therefore remain proof boundaries rather than completed claims.

## 2026-08-17 Baseline Playback Recovery

The user clarified that Booter's legs were not completing a stride and only wiggled while the player moved, and explicitly prioritized repairing existing locomotion before adding animations. That evidence superseded the original assumption that asset qualification had to precede removal of the known broken pose path.

- The live graph routed every locomotion clip through `TopDown3DHumanoidPoseJob`, which applied Humanoid body offsets, foot goals, and `SolveIK` after the mixer.
- The gait clock advanced the source clips at approximately normal time, leaving the downstream pose/IK authority as the production path capable of collapsing a complete stride into the reported partial motion.
- The recovery connects the locomotion mixer directly to the Humanoid output, disables clip-level Foot IK, and removes the pose job, foot locking, pelvis/body rewriting, their serialized fields, and their obsolete focused tests.
- Footstep dust retains read-only contact sampling from the animated foot bones; it does not write to the pose.
- Rigidbody movement, `4.2 m/s` run, `7.4 m/s` sprint, `48`-degree slope limit, traversal, input, and camera code are unchanged.

This recovery is the prerequisite visual baseline. It does not claim that authored starts/stops or the final calibrated profile exist, and it does not waive the Batch 0 asset gate for those later features.

## 2026-08-16 Execution Record

The user authorized implementation, but the first required asset gate did not pass:

- Assimp inspection confirmed that the Timeline `Turns_StartStop-Wk.fbx` source has the expected Humanoid bone names and nine distinct start/walk/stop sequences. Its 30 fps hip displacement peaks at approximately `4.44 cm/frame`, or `1.33 m/s`. Matching Booter's `4.2 m/s` normal run would require roughly `3.15x` playback, far outside the approved `0.90-1.10` correction range.
- `Nav-Accelerations.fbx` reaches jog/sprint speeds, but its defined subclips are sustained walk/jog/sprint, walk-to-jog, jog-to-walk, jog-to-sprint, sprint-to-jog, and turns. Its fastest apparent idle-adjacent acceleration takes approximately `1.43 s` to reach `4.2 m/s`, versus the preserved `0.45-0.60 s` normal-run response. Compressing it enough would require more than twice its source rate and would not provide a matched stop family.
- `Nav-Turn_on_Spot.fbx` remains a possible future pivot source but does not provide translational starts or stops.
- The Unity account owns `Starter Assets: Character Controllers | URP` version `2.0.2`, but it is not installed in this repository and the Asset Store describes it as importing as a Local Package. Its official 147-file Package Content inventory exposes only `Stand--Idle`, forward/backward run, forward walk, walk/run landing, jump, and in-air animation FBXs. It contains no start, stop, pivot, or matched left/right transition family, so importing it would both cross this plan's no-package-change boundary and fail the Batch 0 requirement. The current project animation inventory and Unity's public Standard Assets Characters source likewise expose sustained gait, strafe, jump/fall, landing, and turn clips, but no matched idle-to-run and run-to-idle family.
- The Unity GUI owns the project lock, so no competing batchmode import, test, or validator process was started.

Per Batch 0 steps 7-8, implementation stopped before adding a clip profile, editor profiler, copied animation asset, runtime state authority, scene migration, or fallback. No partial or competing locomotion authority was created.

The exact unblock is a user-approved asset decision that supplies Humanoid, in-place or root-motion-removable, left/right-compatible run starts and stops that can match the existing response bounds without more than `0.90-1.10` playback correction. The currently owned Starter Assets package has now been qualified from its official inventory and does not satisfy that requirement; importing it is not an unblock.

## Authority And Stop

This document is the canonical implementation plan for repairing Booter's broken leg animation and making ordinary grounded movement feel heavy, natural, and responsive.

The original planning pass was limited to repository inspection and this document. On 2026-08-16 the user separately authorized the plan's buildout, activating the proposed implementation scope below while preserving every listed non-goal, proof requirement, ownership boundary, and stop condition. The buildout does not authorize package changes, purchases, unrelated Unity interaction, commits, pushes, builds, deployments, or releases.

## Current Approved Scope

- Complete the baseline playback recovery before adding or purchasing animation assets.
- After the existing legs visibly complete their gait, execute Batch 0 and then Batches 1-4 only while each prior gate passes.
- Change only the runtime, animation data, qualified assets, editor tooling, builder, validator, scene, focused tests, and documentation surfaces named in the direct source boundary.
- Preserve unrelated dirty work and every movement, traversal, camera, input, procedural-world, package, Git, and release invariant below.
- Stop at the first explicit asset, validation, ownership, package, or hands-on boundary rather than creating a partial or fallback authority.

## Objective

Replace the current phase-assumed locomotion presentation with a calibrated, state-based animation system that visibly communicates starts, sustained travel, braking, stopping, and planted direction changes while preserving responsive player intent and the Rigidbody as the only collision and world-displacement authority.

The implementation is successful when:

- Booter's legs animate continuously during ordinary movement and never remain pinned in a standing or distorted pose while the Rigidbody moves;
- starts, stops, medium turns, and hard reversals visibly carry weight at the production camera distance;
- normal run remains `4.2 m/s`, sprint remains `7.4 m/s`, and the raw walkable-slope cutoff remains `48` degrees;
- locomotion clips use measured, per-clip phase/contact metadata instead of a universal `0.0/0.5` source-time assumption;
- authored start, stop, and planted-turn animations carry the main weight signal;
- procedural foot placement is not part of the acceptance path unless a later measured need and separate proof justify it;
- traversal, camera behavior, input bindings, action constraints, and dust behavior preserve their intended contracts.

## Owner And Lane

- The user is product and creative authority and owns final hands-on feel acceptance.
- Gottspan owns repository scope, integration order, evidence reconciliation, and this plan.
- `BooterBigArm.TopDown3D.Runtime` owns the production motor and animation runtime.
- Babineaux owns the later Unity/editor validation handoff under Gottspan's coordination.
- No other gameplay, terrain, UI, camera, Git, release, or package lane is included.

## Source Of Truth

Use this order when implementation facts or design expectations disagree:

1. The user's current instruction and root `AGENTS.md`.
2. `Docs/Agents/Gottspan/README.md` and `Docs/Agents/Babineaux/README.md` for their respective lanes.
3. This plan for the approved locomotion-recovery slice.
4. `Docs/MOVEMENT_CAMERA_STANDARD.md`, `Docs/INPUT_ARCHITECTURE_STANDARD.md`, and `Docs/SMART_TRAVERSAL_STANDARD.md`.
5. Live TopDown3D runtime code, editor tooling, scene serialization, animation imports, packages, and focused tests.
6. Research references and prior implementation records.

The existing movement standard still describes the failed stance-window foot-goal experiment. For this recovery slice, this plan supersedes that foot-goal requirement. The implementation must reconcile the standard after the replacement path passes automated validation; the old prose must not cause a second foot-placement authority to survive.

## Live Repository Truth

Verified from the workspace on 2026-08-16:

- `TopDown3DPlayerMotor` is a 3D Rigidbody motor. It applies camera-relative movement in `FixedUpdate`, enables Rigidbody interpolation, keeps root motion out of collision displacement, and currently uses constant-rate `Vector3.MoveTowards` response.
- Serialized movement values are normal run `4.2 m/s`, sprint `7.4 m/s`, acceleration `8.5 m/s²`, commanded speed reduction `12.5 m/s²`, planted no-input stopping `20 m/s²`, reversal `13.5 m/s²`, run turn `420 deg/s`, sprint turn `300 deg/s`, and raw slope limit `48` degrees.
- `TopDown3DLocomotionSnapshot` exposes world current/desired velocity, acceleration, support normal, grounding, sprint, traversal ownership, alignment, and signed heading error. It does not expose measured yaw rate or a canonical local-space trajectory sample.
- `TopDown3DPlayerAnimationDriver` owns a manual Playables graph with idle, walk, run, sprint, two run-strafe clips, two hard-turn clips, vault, and gather slots.
- Sustained clips are sampled from one manually advanced phase. The current foot stream assumes semantic contacts at phase `0.0` and `0.5`, while the actual imports use different cycle offsets: walk `0.0`, run `0.96`, sprint `0.9`, and both directional run clips `0.0`.
- The rapid-turn FBX exposes `RapidTurningLeft` and mirrored `RapidTurningRight_Mirror`. Both are imported with `loopTime: 1`; the mirrored clip also has `cycleOffset: 0.5`, although the runtime treats these clips as bounded pivot actions.
- The downstream `AnimationScriptPlayable` applies torso response, pelvis correction, and stance-gated foot goals. Recent playtests showed both stale-world foot pinning and a graph-input error that replaced moving legs with the standing Humanoid pose. Later increases to lean and compression remained visually ineffective.
- The project contains ten animation FBXs and one custom gather `.anim`. It contains no authored idle-to-run start, run-to-idle stop, left/right planted stop, or complete directional locomotion family.
- The installed Timeline package includes Unity Companion License sample source takes `Nav-Accelerations.fbx`, `Turns_StartStop-Wk.fbx`, and `Nav-Turn_on_Spot.fbx`. They live in `Library/PackageCache`, are not durable project assets, and require qualification, copying into `Assets/_Project/`, retargeting, and explicit subclip definitions before production use.
- `Nav-Accelerations.fbx` already defines walk, jog, sprint, walk/jog and jog/sprint transitions, plus turn clips. `Turns_StartStop-Wk.fbx` and `Nav-Turn_on_Spot.fbx` are long unsplit source takes, so their useful frame ranges remain unproven.
- `TopDown3DPrototypeBuilder`, `TopDown3DPrototypeValidator`, `TopDown3DPrototype.unity`, `TopDown3DPlayerLocomotionTests`, and `TopDown3DFootstepDustTests` are directly coupled to the current animation contract.
- The worktree is broadly dirty, including all main player-controller surfaces. Existing changes are user-owned. Implementation must patch only task-owned blocks and preserve unrelated edits.
- The Unity GUI currently owns the project lock. No competing batchmode validation is allowed while that remains true.

## Research Basis

The plan applies the following production techniques without copying another game's full architecture:

- Unity's blend guidance requires similar motions to align their normalized contact timing. The project must measure and remap each clip before treating it as part of one gait phase: <https://docs.unity3d.com/6000.0/Documentation/Manual/class-BlendTree.html>.
- Motion-matching systems choose animation from both current pose and desired future trajectory. Full motion matching is out of scope, but the separation between current motion, desired trajectory, and transition state is adopted: <https://www.gdcvault.com/play/1023280/Motion-Matching-and-The-Road>.
- Capcom describes `Monster Hunter Wilds` character animation as a mixture of revised legacy animation, motion capture, and hand-authored work, balanced against in-game performance. The project inference is that authored starts/stops/turns should create weight while input remains responsive: <https://blogs.autodesk.com/media-and-entertainment/2026/04/23/the-production-infrastructure-behind-capcoms-monster-hunter-wilds/>.
- Short inertial-style transitions preserve continuity better than long generic crossfades, but a true inertialization solver is not required for this slice. The Unity implementation will use phase-aware, velocity-aware short blends rather than claim full inertialization: <https://dev.epicgames.com/documentation/en-us/unreal-engine/transition-rules-in-unreal-engine>.
- Stride and orientation warping can correct bounded speed/direction mismatch, but only after source animation, state selection, and contact calibration are correct. They are deferred unless the acceptance pass proves a specific residual need: <https://dev.epicgames.com/documentation/unreal-engine/pose-warping-in-unreal-engine?lang=en-US>.

## Proposed Implementation Scope

This scope becomes active only after the user separately requests implementation.

### Runtime

- Preserve `TopDown3DPlayerMotor` as movement authority.
- Extend its read-only presentation snapshot only with animation-required facts that cannot be derived reliably in the render update: facing direction and measured yaw rate.
- Replace the current scalar heading-error gait selection with explicit locomotion states and local forward/lateral trajectory inputs in `TopDown3DPlayerAnimationDriver`.
- Remove foot-position, pelvis-position, and foot-rotation writes from the production locomotion path.
- Retain animation-driven foot contacts and make their phase source profile-driven.
- Preserve `TopDown3DFootstepDust` as a contact consumer; it must not regain distance cadence or guessed foot alternation.

### Canonical Animation Data

Create one project-owned `TopDown3DLocomotionClipProfile` ScriptableObject authority under `Assets/_Project/Settings/Player/`.

Each clip entry must record:

- semantic role: idle, sustained walk/run/sprint, directional gait, start, stop, pivot, or traversal;
- `AnimationClip` reference and Humanoid compatibility;
- nominal forward and lateral speed;
- looping versus one-shot behavior;
- source left/right foot-contact phases for loops;
- planted or leading foot for one-shots;
- one-shot plant and release times where applicable;
- phase remap data needed to align both contacts to semantic gait phase;
- allowed playback-rate correction bounds;
- preferred entry/exit roles and whether mirroring is intentional.

The profile replaces individually serialized locomotion timing assumptions in the scene and driver. The scene, builder, validator, tests, and driver must all reference this one authority. Missing required roles are validation failures; they never select an unrelated clip.

### Editor Qualification Tooling

Add an Editor-only locomotion clip profiler under `Assets/_Project/Scripts/Editor/TopDown3D/` that:

- instantiates the project Humanoid only in a hidden preview context;
- samples each candidate clip at a fixed resolution;
- reports left/right ankle height and velocity, root drift, body orientation, loop discontinuity, and likely contact windows;
- previews the clip and its proposed subclip boundaries without modifying runtime movement;
- writes approved measurements to the canonical clip profile;
- never edits package-cache files or creates runtime references into `Library/`.

Automatic contact detection supplies candidates, not unquestioned truth. The implementation must inspect the sampled curves and reject clips with ambiguous plants, severe retargeting distortion, incompatible cadence, or unbounded root drift.

### Qualified Assets

- First qualify the existing Unity Standard Humanoid loops and rapid-turn clips.
- Then qualify the installed Timeline sample takes for authored transitions.
- Copy only accepted source FBXs into a project-owned `Assets/_Project/Art/Characters/Prototype/UnityTimelineHumanoid/` folder with new Unity-generated `.meta` files and a license/source README.
- Define explicit non-looping subclips for starts, stops, and pivots. Do not keep the current rapid-turn clips looping when used as one-shots.
- Reuse the project Humanoid avatar/retargeting contract. Do not duplicate the character model.
- Stop for a user asset decision if no suitable start/stop pair can be extracted. Do not substitute vault, jump, gather, or sustained gait clips.

### Directly Coupled Surfaces

Expected implementation boundary:

- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs`
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.cs`
- new runtime clip-profile/state types under the same assembly
- `Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DFootstepDust.cs` only if its contact subscription contract must change
- new Editor-only clip profiler
- `Assets/_Project/Scripts/Editor/TopDown3D/TopDown3DPrototypeBuilder.cs`
- `Assets/_Project/Scripts/Editor/Validation/TopDown3DPrototypeValidator.cs`
- `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`
- the project-owned animation profile and qualified transition assets with their `.meta` files
- `Assets/_Project/Tests/Editor/TopDown3DPlayerLocomotionTests.cs`
- `Assets/_Project/Tests/Editor/TopDown3DFootstepDustTests.cs`
- a focused clip-profile/asset-contract test if separation improves clarity
- `Docs/MOVEMENT_CAMERA_STANDARD.md`, this plan's implementation record, and the Humanoid asset README after behavior is proven

No other file is in scope unless implementation proves it is an unavoidable serialized consumer. That expansion must be explained before editing.

## Preserved Behavior And Non-Goals

- No change to the established normal-run or sprint maximum speeds.
- No change to the `48` degree raw slope cutoff.
- No change to input bindings, input deadzone authority, camera, camera framing, camera shake, camera bob, or HUD.
- No vault, sidestep, crouch, jump, fall, roll, dodge, combat, gathering, inventory, or interaction redesign.
- No root-motion movement authority.
- No Animator Controller migration.
- No full motion matching, Animation Rigging package, new Unity package, asset purchase, or project-wide setting change.
- No second controller, fallback Animator, backup character, duplicate profile, or unrelated-clip fallback.
- No broad refactor of the motor, traversal, world generator, terrain, camera, or action systems.
- No runtime debug HUD or player-facing UI. Diagnostics are Editor-only.
- No gameplay smoke-test creation.
- No commit, push, pull request, branch operation, build, deploy, or release.

## Procedural-World Contract

- Deterministic world identity: locomotion reads loaded collision/support data and does not alter seed derivation or generated content.
- Chunk streaming: animation state contains no durable collider or chunk reference. Ground/contact state drops immediately when support becomes invalid or unloads.
- Stable generated-object identity: not applicable because locomotion does not persist relationships to generated objects.
- Authored constraints: the motor's slope cutoff and traversal probe rules remain authoritative.
- Persisted runtime deltas: none. Clip phase, current state, and contact state are transient and reset on disable, teleport, action constraint, traversal takeover, respawn, or visual rebuild.

## Approaches Considered

### A. Continue Tuning The Existing Phase And Foot Job

Source boundary: current animation driver, pose job, and existing tests only.

Benefits:

- smallest code and serialization change;
- no new data asset or animation imports;
- fast automated validation.

Risks and likely breakage:

- preserves the universal phase assumption despite incompatible imports;
- preserves the same downstream job that already froze and distorted the legs;
- cannot create convincing starts/stops because the clips do not exist;
- further lean/IK tuning may reintroduce foot pinning, knee stretch, pose replacement, or invisible inspector-only motion.

Validation path: existing math tests and user replay of the same failed behavior.

Decision: rejected. It treats symptoms and has already failed the hands-on proof boundary.

### B. State-Based Playables With A Calibrated Clip Profile

Source boundary: current Rigidbody motor, Playables driver, one canonical profile, qualified transition assets, coupled builder/validator/scene/tests.

Benefits:

- preserves the accepted motor and traversal architecture;
- fixes clip timing and transition selection at their source;
- makes starts/stops/pivots authored and explicit;
- supports deterministic tests for state selection, phase mapping, and fallback rejection;
- remains package-free and avoids a second animation authority.

Risks and likely breakage:

- profile or scene migration could temporarily leave missing references;
- incorrect subclip boundaries could cause pose pops or wrong-foot starts;
- poor transition assets could retarget badly to the current Humanoid;
- traversal/action priority could regress if the top-level state order changes;
- dust contacts could desynchronize if phase mapping and contact emission diverge.

Validation path: asset qualification, profile-contract tests, pure state/phase tests, focused Unity compilation/tests, validator, then user hands-on matrix.

Decision: recommended.

### C. Migrate To An Animator Controller And Blend Trees

Source boundary: new Animator Controller, parameters, state machine, Blend Trees, transition assets, driver rewrite, scene/builder/validator/tests.

Benefits:

- strong Unity-native visual authoring and debugging;
- straightforward 2D blend trees and transition inspection;
- less custom mixer-weight code.

Risks and likely breakage:

- duplicates or replaces the existing Playables authority used by traversal and gather;
- broad serialization and builder changes in a dirty worktree;
- risks losing action priority, contact timing, and no-root-motion guarantees during migration;
- does not solve missing or uncalibrated source animations by itself.

Validation path: complete parity audit of every current action and traversal state, followed by broader scene and hands-on regression.

Decision: rejected for this slice. It increases migration risk without addressing the limiting asset coverage first.

### D. Full Motion Matching, Animation Rigging, Or Runtime Pose Warping

Source boundary: package manifest/lockfile, a motion database, new runtime architecture, rig constraints, assets, tests, and likely profiling/build work.

Benefits:

- highest long-term ceiling for trajectory-aware transitions and broad motion coverage.

Risks and likely breakage:

- the current ten-FBX dataset is far too small for a useful motion database;
- requires package/architecture authority that is not approved;
- introduces major performance, authoring, debugging, and build-proof requirements;
- can hide bad source animation temporarily while creating new foot/retargeting failures.

Validation path: separate research prototype and package decision.

Decision: deferred outside this plan.

## Recommended Architecture

### Movement And Presentation Separation

The motor continues to calculate and apply physics. The animation driver reads one immutable presentation snapshot per render update and never writes the Rigidbody, collider, player root, input, camera, or traversal trajectory.

The snapshot provides:

- current and desired planar velocity;
- planar acceleration;
- facing direction;
- measured yaw rate;
- support normal and grounded state;
- sprint and traversal ownership;
- direction alignment and signed heading error.

The driver transforms velocity and trajectory data into character-local space. World/local conversion lives in the driver; the motor does not publish duplicate local vectors.

### Locomotion States And Priority

Use one explicit state authority with this priority:

1. constrained action or traversal override;
2. hard pivot/reversal;
3. stop when intent is removed while physical motion remains;
4. start when intent exists but physical motion is still near rest;
5. sustained grounded locomotion;
6. idle.

Initial classification values are serialized in the profile and covered by tests:

- idle exit: desired speed at least `0.35 m/s`;
- start completion: one-shot completion or current speed at least `1.2 m/s` with a valid sustained target;
- stop entry: desired speed below `0.15 m/s` while current speed exceeds `0.75 m/s`;
- idle entry: current and desired speeds both below `0.15 m/s` after the stop release plant;
- pivot entry: grounded, no override, current speed at least `2.4 m/s`, and current/desired direction angle at least `125` degrees;
- pivot release: angle below `72` degrees, speed below the minimum, desired motion removed, or override ownership changes;
- state hysteresis must prevent alternating start/stop or left/right pivot decisions across adjacent frames.

These values are starting contracts, not invisible feel claims. Changing them during implementation requires tests and must remain inside the preserved response bounds.

### Sustained Gait Blend

- Blend walk/run/sprint by physical planar speed and sprint intent.
- Blend directional gait by local desired trajectory and measured yaw rate, not signed heading error alone.
- Normalize all weights and retain one semantic gait phase.
- Map semantic phase into each source clip using its profile contacts; do not sample every clip at the same raw normalized time.
- Align both left and right contacts. A two-segment phase remap between measured contacts is sufficient unless profiling proves the clip incompatible.
- Bound sustained playback correction to `0.90-1.10` initially. Reject a clip that needs more correction to match the Rigidbody rather than stretching it visibly.

### Starts, Stops, And Pivots

- Starts and stops are non-looping authored clips selected by planted/leading foot.
- Start selection uses the current semantic phase or idle stance metadata and immediately yields steering to the motor.
- Stop selection uses current speed, predicted physical stopping time, and the next measured contact to choose the planted side.
- Pivots are non-looping left/right clips aligned so the plant occurs near the motor's velocity-zero crossing.
- No transition may buffer input, lock steering, or apply animation displacement.
- Use short phase-aware crossfades, initially `0.12-0.25 s`; transitions above `0.35 s` require evidence because they risk visible foot blending and control lag.

### Foot Contacts And Procedural Pose

- Emit contacts only from calibrated semantic phase crossings or explicit one-shot plant metadata.
- A contact includes side, grounded world position, planar speed, and normalized impact.
- Dust remains a subscriber and never invents cadence.
- Remove production foot locking, pelvis translation, and custom foot rotation in the first canonical cutover.
- Keep torso lean/compression disabled during the initial recovery batches. Reintroduce only a small body-only additive pass after starts/stops/turns pass hands-on review, and only if the silhouette still needs it.
- If later proof shows terrain contact correction is necessary, it requires a separate bounded plan using measured ankle velocity, FK reach clamps, knee stability, and loaded-support validity. It is not a fallback in this plan.

## Implementation Sequence

### Recovery Gate — Restore Full Locomotion Playback

1. Remove the downstream pose `AnimationScriptPlayable`, disable clip-level Foot IK, and connect the locomotion mixer directly to the Humanoid output.
2. Remove foot-position, pelvis, and procedural torso writes plus their stale state, helpers, scene fields, and tests.
3. Keep root motion disabled and preserve read-only foot contact emission for dust.
4. Verify structurally that the mixer is the sole output source and ask the user to confirm that both legs now complete each gait cycle.

Recovery proof:

- no production `IAnimationJob`, Humanoid IK solve, foot goal, or pelvis/body offset remains;
- the locomotion mixer is the sole source of the Humanoid animation output;
- controller, traversal, camera, and package surfaces are unchanged;
- automated compile/contract proof passes when Unity can safely provide it;
- full-stride visual behavior remains a user-owned hands-on check.

### Batch 0 — Asset And Profile Qualification Gate

1. Capture task-file diffs and exact existing scene references.
2. Do not run batchmode while the GUI lock exists; use the safe GUI validation path only when the user/editor state permits, or wait until the lock is gone.
3. Add the profile type and Editor-only profiler without changing the production scene or runtime selection.
4. Measure existing idle/walk/run/sprint/directional/pivot clips and record contact, loop, drift, and retargeting evidence.
5. Preview Timeline candidates and identify exact start/stop/pivot subclip ranges.
6. Copy only qualified source assets into the project and preserve license attribution.
7. Stop before runtime cutover if the start/stop pair or left/right pivot coverage is unsuitable.
8. If qualification fails, remove the task-owned unused profile/tooling additions before closeout so no partial or competing authority remains.

Batch proof:

- profile asset contains no missing required role and no duplicate role authority;
- all accepted clips are Humanoid-compatible and project-owned;
- loops have two ordered contacts and bounded loop discontinuity;
- one-shots are non-looping with explicit plant side/time;
- no runtime or scene reference points into `Library/PackageCache`;
- no gameplay behavior has changed yet.

### Batch 1 — Preserve The Recovered Single Pose Authority

1. Revalidate that the downstream pose `AnimationScriptPlayable` remains absent after profile integration.
2. Keep foot-position, foot-rotation, pelvis, and procedural torso writes out of the production graph.
3. Do not reintroduce stale foot-lock fields, state, math helpers, or tests as a backup path.
4. Keep root motion disabled.
5. Keep profile-driven contact emission and dust subscription compiling, without procedural foot goals.

Batch proof:

- no runtime code writes Humanoid foot or pelvis goals;
- the mixer is the sole graph input to the Humanoid output until optional Batch 5 finishing is approved;
- no stale-world foot target remains;
- no duplicate graph output or fallback Animator exists;
- focused graph/contract tests and validator pass;
- user can verify that legs play freely before weight polish proceeds.

### Batch 2 — Canonical State Machine And Sustained Gait

1. Extend the presentation snapshot with facing direction and measured yaw rate.
2. Add the single locomotion-state authority and reset rules.
3. Replace raw shared sampling with profile phase remapping.
4. Drive sustained gait from local current/desired velocity plus yaw rate.
5. Preserve traversal/action priority and all maximum speeds.

Batch proof:

- state priority and hysteresis tests pass;
- directional weights are symmetric, continuous, normalized, and bounded;
- phase remapping aligns both profile contacts across every blended loop;
- resets occur on disable, teleport, constraint, traversal takeover, and visual rebuild;
- movement values, slope limit, traversal serialization, and camera serialization remain unchanged.

### Batch 3 — Authored Starts, Stops, And Pivots

1. Add planted-side start/stop selection from the qualified profile.
2. Align stops with predicted physical stopping time without changing motor velocity.
3. Convert rapid turns and qualified turn clips to explicit non-looping pivot use.
4. Use short state-specific crossfades and reject unrelated fallbacks.
5. Preserve immediate steering and traversal interruption rules.

Batch proof:

- left/right start, stop, and pivot selection tests pass;
- stop prediction stays consistent with the `20 m/s²` no-input stopping rate while commanded speed reduction and reversal retain their `12.5/13.5 m/s²` response values;
- no state can remain latched after input removal, traversal, constraint, disable, or teleport;
- no locomotion animation modifies Rigidbody displacement;
- missing transition roles fail validation.

### Batch 4 — Scene, Builder, Validator, And Contact Cutover

1. Migrate the driver from individually serialized clip assumptions to the one profile reference.
2. Patch only the player animation block in the builder and production scene.
3. Make the validator prove the profile, clip roles, one-shot loop flags, root-motion setting, one driver, one dust subscriber, hidden pill renderer, and preserved movement/camera/traversal values.
4. Remove obsolete serialized fields and documentation claims.
5. Update the movement standard and Humanoid README to match proven behavior.

Batch proof:

- builder and scene resolve the same canonical profile;
- exactly one production animation driver and dust consumer exist;
- no obsolete serialized foot-job fields remain in the production scene;
- task-owned `git diff --check` passes;
- unrelated dirty files are byte-for-byte untouched by this task.

### Batch 5 — Bounded Visual Weight Finishing

Enter this batch only after the user confirms that legs, starts, stops, and pivots are stable.

1. Compare the production-camera silhouette with body-only finishing disabled.
2. If still needed, add a small upper-body/hips additive response that cannot affect feet, pelvis position, root motion, collision, or state selection.
3. Keep current motor response rates unless hands-on evidence identifies a physical-response defect separate from animation.
4. Do not add stride warping or IK during this plan.

Batch proof:

- an off/on comparison produces a visible but bounded silhouette change;
- legs and feet are identical between finishing off/on;
- motor velocity traces are identical between finishing off/on;
- the user owns the final preference decision.

## Automated Proof Requirements

Implementation is not ready for hands-on acceptance until all available proof below passes:

- task-owned C# and serialization changes pass static inspection and `git diff --check`;
- runtime, Editor, and focused test assemblies compile in Unity;
- focused EditMode tests pass for response invariants, snapshot data, state priority, hysteresis, local blend weights, phase remapping, contact wraparound, start/stop/pivot side selection, reset boundaries, profile completeness, asset import flags, and dust contact consumption;
- `TopDown3DPrototypeValidator.ValidateFromCli` or its safe GUI equivalent passes;
- no task-related compiler, import, Playables, missing-reference, avatar, or validator warning remains;
- animation references are project-owned and complete, with no `Library/PackageCache` runtime dependency;
- `4.2 m/s`, `7.4 m/s`, `48` degrees, traversal authority, input bindings, camera values, and action constraints are unchanged;
- the procedural world receives no new persisted state or generated-object coupling;
- the pre-task diff of every touched dirty file is reconciled so only task-owned blocks changed.

Automated proof does not prove visual quality, controller feel, retargeting quality in motion, or player acceptance.

## User-Owned Hands-On Proof Matrix

Use the production Humanoid, production camera, and `TopDown3DPrototype` scene. Test both keyboard and a physical gamepad where applicable.

Required cases:

- idle to movement and movement to idle, repeatedly, from both planted feet;
- normal run and sprint at steady speed;
- gentle curves, tight circles, and left/right 45/90/135-degree direction changes;
- left/right 180-degree reversals;
- short taps, partial analog input, sudden input release, and sprint release;
- camera orbit while moving and turning;
- flat terrain, craggy terrain, representative slopes below the limit, at the limit, and above the limit;
- vault and both traversal sidesteps as regression coverage;
- action constraint/gather entry and exit;
- repeated stop/start near a chunk boundary and after streamed support changes.

Acceptance criteria:

- both legs animate through the gait and no leg remains fixed in the standing pose while the body moves;
- no foot remains pinned in world space after its source pose leaves contact;
- no knee inversion, extreme stretch, foot teleport, or pelvis collapse;
- starts and stops visibly transfer weight without delaying input or overshooting physical speed;
- turns choose the correct side and do not oscillate between left/right clips;
- sustained gait does not moonwalk noticeably at `4.2` or `7.4 m/s`;
- traversal and actions interrupt and return to locomotion cleanly;
- no new controller jitter, camera jitter, collision change, or slope-climbing regression appears;
- the user can see the improvement at the normal production camera distance.

## Implementation Stop Condition

Stop successfully when:

- the recovery gate is complete and the user confirms full-stride playback;
- Batches 0-4 are complete;
- all available automated proof passes;
- the scene uses one calibrated clip profile and one Playables locomotion authority;
- the broken foot/pelvis authority and unrelated fallbacks are removed rather than preserved;
- hands-on cases are handed to the user with visual/feel claims clearly marked unproven;
- Batch 5 is either accepted with proof or explicitly unnecessary.

Stop early and report the exact boundary when:

- suitable authored start/stop or pivot clips cannot be qualified from current licensed assets;
- Unity validation cannot proceed safely because the GUI owns the project or compilation/import errors block trustworthy proof;
- a required fix would change maximum speeds, slope behavior, traversal, camera, packages, purchases, project settings, another owner's files, or any other non-goal;
- implementation discovers that an Animator migration, motion-matching database, Animation Rigging package, or broader character-art replacement is actually required;
- the next action is commit, push, branch/worktree work, player build, deployment, release, or user-owned hands-on acceptance.

## Why This Plan Won

The recommended path keeps the working physics/controller authority and fixes the animation source boundary that playtesting disproved. It combines the strongest low-risk parts of the alternatives: Unity-style calibrated phase alignment, trajectory-aware state selection, authored transition coverage, short state-specific blends, and strict validator/test ownership. It rejects the failed procedural-foot workaround and avoids an Animator or motion-matching rewrite that the current asset set cannot support.

## What Remains Unproven

- Unity AssetDatabase readback of the newly admitted Core Motion clips and profile while the current GUI session owns the project.
- Focused EditMode and production-validator results after the Editor refreshes and imports the new assets.
- Visual retargeting quality of Core Motion starts, stops, pivots, and sustained gaits on the current Humanoid.
- Whether the Core Motion forward-left/right jog clips provide enough lateral coverage at the production camera distance.
- Whether body-only finishing is needed after proper starts/stops/pivots exist.
- Final controller feel, animation weight, visible foot sliding, and terrain presentation.

Those are Unity/editor and user hands-on proof boundaries, not reasons to retain the removed foot-placement authority or add a fallback controller.

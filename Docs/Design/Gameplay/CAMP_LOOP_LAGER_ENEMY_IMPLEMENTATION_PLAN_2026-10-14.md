# Camp Loop first Lager enemy implementation plan

October 14, 2026. Status: implemented in the saved CampLoopTesting scene; 41 focused checks and six exact installed-scene checks pass. Create one skittish enemy NPC in the existing `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`, with a simple replaceable model and behavior that can be tuned over time. Hands-on feel and appearance acceptance remain author-owned.

The [plan audit](CAMP_LOOP_LAGER_ENEMY_PLAN_AUDIT_2026-10-14.md) is incorporated below and in the implementation. The [implementation evidence](../../Evidence/Gameplay/CampLoopNpc20261014/README.md) records the isolated candidate, coordinated additive integration and exact proof boundary.

Use **Lager** as the user's current working display name for the previously discussed legger enemy. Keep the companion's existing Legger name, code and cargo behavior intact. This spelling does not establish a new species or change [World Basis](../../WORLD_BASIS.md). Final anatomy, art and lore remain open.

## Observable result and scope

Booter can approach one separately identifiable NPC near camp. It notices him, becomes wary, retreats to maintain space and runs away under close pressure. It selects legal routes around obstacles, remains physically present if trapped and calms when pressure ends. Its Inspector explains its state and route result. Settings can be adjusted without rewriting the controller, changing the model or adding an enemy subclass.

Deliver one primitive prefab, one authored definition/profile asset, the minimum reusable NPC runtime, an additive Camp Loop installer, an Inspector tuning surface, a lab reset adapter and focused automated verification. Keep the production scene, build scene selection, terrain, player/companion ownership, inventory, camera, controls, survival and current animation stations unchanged by this feature.

Exclude attacks, health/death, loot, recruitment, factions/reputation, procedural spawning, packs, distant simulation, production persistence, graph editors and package changes. Do not migrate the companion into the new framework. Do not create or run gameplay smoke tests; hands-on appearance and feel acceptance remain with the author.

## Ownership sources and current constraints

The user owns creative direction and behavior acceptance. Gottspan owns scope/integration; Babineaux owns background Unity authoring and verification; Gear Ball owns verified local commits. Blender and Lorekeeper need no implementation work for a primitive, lore-neutral fixture. These are role responsibilities, not a requirement to spawn agents.

Controlling sources in priority order: current user request, root AGENTS.md, [local workspace routing](../../LOCAL_WORKSPACE.md), World Basis, [first enemy audit](CAMP_LOOP_LEGGER_ENEMY_AUDIT_2026-10-14.md), [AI methods brainstorm](NPC_AND_GAME_AI_METHODS_BRAINSTORM_2026-10-14.md), [world architecture baseline](../../Engineering/GREATER_WASTELAND_WORLD_ARCHITECTURE_BASELINE_2026-10-11.md), then live assets/code. This plan resolves the first slice; the broader brainstorm remains optional future direction.

Planning inspection: branch `main` tracks `origin/main`; initial HEAD was `dfb2219dd74758e81c59b69c89b867aa04d7ba9b`. Unity is pinned to `6000.4.0f1`, URP 17.4.0, Input System 1.19.0 and Test Framework 1.6.0. Active runtime is `BooterBigArm.TopDown3D.Runtime`; editor/test assemblies are `BooterBigArm.Editor` and `BooterBigArm.Editor.Tests`. No active NPC/enemy implementation was found during this plan inspection.

The shared worktree contains concurrent animation, salvage and scene changes. Treat them as independently owned. `Temp/UnityLockfile` was absent at inspection; that does not establish Editor ownership or license readiness. Before implementation, recheck processes, lock, live scene dirty state, serialized baseline and owner handoffs through [Unity automation](../../Engineering/UNITY_AUTOMATION.md). Do not launch a second Editor on the same project, focus an app, save another owner's unsaved work or copy an old scene over current work.

## Approaches and selected architecture

| Approach | Boundary and benefit | Cost or breakage risk | Decision |
| --- | --- | --- | --- |
| Explicit state policy with bounded grid routes | New NPC lane reuses the existing pure search; typed observations, intent and motor result are independently testable. | Requires a small new ground-query/motor adapter; unsuitable as proof of long-distance navigation. | Select for the first Lager. |
| Utility policy for both motives and route selection | Same actor/motor ownership; scored Alert/Retreat/Flee/Recover choices can evolve into more competing motives. | Adds score normalization and switching behavior to modes that currently have clear thresholds. | Use scoring only for escape candidates now; retain the seam for later policy replacement. |
| Authored behavior graph driving the same executor | A graph adapter can later reuse tasks and expose visual debugging. | Requires a separately authorized package/tool evaluation and cancellation/serialization qualification. Larger dependency boundary for this slice. | Defer adoption; preserve observation/intent/result contracts. |

Do not implement flight by attaching the companion follower and reversing its target. That script also owns cargo, call-over and companion state. Reuse `TopDown3DLocalGridPathfinder.TryFindPath` through a new NPC adapter without changing existing callers or archive implementations.

Use one `NpcActor` coordinator, one pure `SkittishNpcBrain`, one perception adapter, one bounded navigation adapter and one motor. An immutable definition holds type identity and a referenced tuning profile; per-instance state holds behavior, timers, perception memory, lifecycle epoch and intent revision. Presentation reads observed displacement and facing and never moves the physics root.

**Behavior** is Idle, Alert, Retreat, Flee or Recover. **Movement status** separately reports Idle, Moving, Partial, Blocked or Unavailable; budget waiting remains Running with a Queued reason. The Lager can remain in Flee while physically blocked. This supersedes the audit's simplified Blocked behavior-state row.

## Proposed tuning contract

All values below are provisional defaults that implementation may refine within the requested behavior after measurements. No final balance or anatomy decision is required to implement the disposable fixture.

| Setting | Initial proposal | Validation or purpose |
| --- | --- | --- |
| Detection / calm release / comfort / panic | 20 / 16 / 10 / 5 m | Strictly descending positive values. Detection 20 m replaces the audit's proposed 14 m so visible separation can be assessed beyond calm release. |
| Sight / vertical eligibility | 360-degree horizontal sight / 4 m vertical difference | Occlusion always applies; the fleeing body's yaw never hides a visible pursuer. |
| Alert delay / memory / calm hold | 0.35 / 2 / 3 simulation seconds | Close panic skips alert delay. Memory tracks last observed position, never the hidden live player. |
| Retreat / flee speed | 4.5 / 7.5 m/s | Starting values only; compare with serialized Booter speeds and actual locomotion scaling. No promise of guaranteed escape. |
| Acceleration / braking / turn rate | 12 m/s squared / 18 m/s squared / 300 degrees/s | Finite positive values; collision/support veto overrides acceleration. |
| Perception / decision cadence | 0.2 / 0.1 simulation seconds | Physics safety runs every fixed step; no decisions run while simulation is paused. |
| Replan interval / route commitment | 0.5 / 0.6 simulation seconds | Panic, target invalidation and route hazards may preempt commitment. |
| Stuck sample / minimum progress | 1.25 s / 0.15 m | Use actual displacement while motion is requested; no teleport recovery. |
| Grid / cell / candidate count | 13 by 13 / 1.25 m / up to 8 | Bound probes/searches; one sampled grid per planning pass. |
| Maximum slope / step up / step down | 35 degrees / 0.35 / 0.45 m | Conservative starting ground capability, independent of Booter traversal. |
| Candidate gains and exposure | At least 1 m separation gain; detour allowance up to 1 m temporary approach | Evaluate the returned route and endpoint; no endpoint-only success. |

Use fixed documented normalization for separation gain, clearance, exposure, travel cost and route continuity. Begin with positive weights 0.45, 0.20, 0.20, 0.10 and 0.05 respectively, subtracting exposure/cost. Show each contribution. Hard constraints always reject illegal candidates before scoring. Switch between legal routes only for at least 0.10 normalized score improvement after commitment; stale or unsafe routes are cancelled immediately.

Implemented normalization uses separation gain / 7.5 m, minimum sampled clearance / 2 m, continuous maximum route approach / 1 m, route length / 30 m and heading dot mapped to [0,1]. The same current context evaluates retained and replacement routes. Candidates use a stable eight-direction ring at six grid cells, deduplicated before search. Obstructed destinations may return safe partial progress, but each local waypoint is not a claim of reaching calm separation. Ground support samples include diagonals and intermediates at at most 0.15 m; reservation and exposure checks use analytical segment distances. Capsule sweeps and occupancy checks remain independent of support sampling. Planning is capped at eight searches, one 13 by 13 grid and 8,192 physics queries per pass; the existing search's arrays remain allocated per call. Added support qualification required the explicit query cap rather than the audit's earlier endpoint-only estimate.

Profile validation rejects non-finite values, invalid threshold order, impossible body sizes, unsupported capability references and work limits above validated caps. Separate tuning from fixed safety limits; an Inspector speed change cannot disable sweeps, support checks or work caps.

### Editing and repeated experiments

Expose three Inspector groups: Perception, Skittish Behavior, and Ground Movement. Show read-only state, observation age, active route/endpoint, transition/failure reason and work counters. Scene gizmos display thresholds, candidate rejection and the current route only when selected. No new player controls or debug HUD are needed.

In Edit Mode, changes use ordinary asset editing with Undo. At startup, copy validated tuning into an instance snapshot; never store live fear/timers in a ScriptableObject. In Play Mode, expose temporary instance overrides with **Apply Tuning** and **Restore Authored Tuning**. Apply validates the whole candidate atomically, increments intent revision, clears stale route work and reevaluates behavior next tick without relocating the NPC. Invalid settings retain the last valid snapshot and display the reason. Play changes never write the shared asset automatically.

The lab reset retains the active valid tuning snapshot so repeated trials compare behavior under the same settings; it clears transient state and restores the fixture. Persisting a chosen session override to the asset is an explicit Edit Mode authoring action. Lifecycle disable/reset and route cancellation must remain safe during override application.

## Perception behavior and movement

Bind Booter explicitly by scene component reference; never select an actor by the first name/tag match. Validate same scene, active target and actor lifetime. Perception checks planar range and useful body points with occlusion before publishing timestamped observations. Ignore self and nonblocking decorative triggers. With one potential threat, no general spatial registry or faction service is needed.

Idle enters Alert on valid detection. Comfort pressure enters Retreat after the alert delay, while panic enters Flee immediately. Flee releases to wary retreat/recovery only after its wider release condition; use a panic-release threshold between panic and comfort, initially 7 m, to avoid 5 m boundary toggling. Retreat aims for the calm-release distance. Recover requires a continuous calm hold, then returns to Idle. Renewed pressure preempts recovery. Expired memory removes the threat; the first actor does not automatically return home or wander.

Memory uses last observed position, distance recomputed from the NPC to that position and a simulation-time expiry. It does not track the hidden Booter. Missing/disabled Booter clears the handle and enters safe recovery. While blocked, threat response remains active, speed brakes to zero and route retries obey the deadline.

Sample candidate directions around the away-vector on the local grid. Reject unsupported, occupied, excluded, out-of-area and unsafe routes. Evaluate actual endpoints because the existing search can return partial progress. Permit bounded detours only when their endpoint is safer and their worst approach respects the exposure allowance. Prefer continuity; use stable tie-breaking. If no legal progress exists, stop at current safe support and report Blocked.

The kinematic Rigidbody is the sole movement authority. Fixed-step movement sweeps the entire body along segments, rechecks support/slope/steps and brakes before obstacles and terrain edges. End-position occupancy alone is insufficient. Saturated query buffers are treated as unavailable/blocked with diagnostics. Reuse bounded arrays; cap search/probe work and elapsed decision catch-up. Zero away-vectors choose a stable authored fallback or stop. No snapping, recall, hazard bypass or destination teleport is permitted during ordinary simulation.

## Model and scene boundary

The author specified bipedal anatomy on October 14, then requested a simple red cube for visibility while troubleshooting behavior. The current trial displays one 1.25 m unlit red cube beneath `Visual`; the earlier two-leg study records remain disabled for later modeling. Use an unscaled root and a conservative yaw-invariant capsule of radius 0.67 m and height 1.40 m, with no decorative colliders. Future modeling replaces only the visual subtree. Keep the art readable relative to 1.80 m Booter without claiming the placeholder is final design.

Survey a side excursion around 20 to 35 m from camp, with two verified escape directions. Author one grounded initial anchor and a bounded activity area within retained terrain. Exclude camp/rest/return spaces, station approaches and the current salvage/workstation footprints. The exact transform, collider and region boundaries are outputs of the survey, not guessed coordinates in this plan. Do not flatten terrain or regenerate dressing for placement.

Install a dedicated `Camp Loop Lager Trial` root additively. Exactly one NPC has instance ID `lab.npc.skittish_legger.001` and type ID `lab.npc.legger.skittish`; display name is Lager. Installation validates the session marker and scene path, explicit Booter reference, unique identity and legal anchor. Repeated installation finds that identity and makes no duplicate; a conflicting existing actor stops installation. Keep it separate from reserved AP01 through AP12 station identities.

## Planned file boundary

New runtime files under `Assets/_Project/Scripts/Runtime/TopDown3D/NPC/`:

- `NpcDefinition.cs` and `SkittishNpcProfile.cs`: immutable definitions and validated tuning.
- `NpcContracts.cs`: observation, intent, movement/route results and lifetime keys; small DTOs share this file.
- `NpcActor.cs`, `SkittishNpcBrain.cs`, `NpcThreatPerception.cs`: orchestration, pure policy and sensing.
- `NpcLocalGroundNavigation.cs`, `NpcGroundMotor.cs`, `NpcBlockoutView.cs`: bounded routes, sole physical movement and disposable presentation.

New integration adapter: `Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/CampLoopNpcTrial.cs`. It owns explicit lab bindings, safe reset and registry registration, keeping generic NPC classes free of playground dependencies. Register during scene initialization after the session is available and before interaction can invoke reset; validate readiness. Unregister/cancel on disable and destruction. No changes to the existing session partials are planned; if current lifecycle requires one, resolve that seam with its owner first.

New editor files under `Assets/_Project/Scripts/Editor/NPC/`: `CampLoopNpcAuthoring.cs`, `CampLoopNpcValidator.cs`, and `NpcActorEditor.cs`. Proposed CLI entry points: `BooterBigArm.Editor.CampLoopNpcAuthoring.InstallFromCli` and `BooterBigArm.Editor.CampLoopNpcValidator.ValidateFromCli`. These methods do not exist yet; define and inspect them before invocation. Keep the validator non-mutating.

New assets: `Assets/_Project/Prefabs/TopDown3D/NPC/LagerSkittish.prefab`, `Assets/_Project/Settings/NPC/LagerDefinition.asset`, `Assets/_Project/Settings/NPC/LagerSkittishProfile.asset`, and a small material set under `Assets/_Project/Materials/NPC/`. Every new Unity asset/folder receives paired metadata through the controlled authoring flow; preserve all existing GUIDs.

New focused tests under `Assets/_Project/Tests/Editor/`: `NpcSkittishBrainTests.cs`, `NpcGroundNavigationTests.cs`, `NpcLifecycleAndTuningTests.cs`, and `CampLoopNpcSceneTests.cs`. Small synthetic fixtures avoid private character dependency where possible; final installed-scene checks use the current hydrated candidate. Relevant existing `TopDown3DLocalGridPathfinderTests` remain unchanged and run as regression.

Existing asset edited: only the additive records for the trial root in `CampLoopTesting.unity`, after the concurrent owner baseline is sealed. Existing runtime pathfinder, companion scripts, catalogs, session partials, production scene, project settings and package manifests remain outside edit scope. Update this plan with implementation status/evidence and add one clean Docs Index route when that file's ownership is available.

## World identity and lifecycle contract

Terrain identity, generation versions and native data stay unchanged. This is one authored actor; procedural spawning and generated-object ordinals are not applicable to its creation. Separate stable actor identity from Unity instance ID, lifecycle epoch and current spatial location so future generation can join the same contract without rewriting the brain.

Terrain streaming/unload is not applicable because Camp Loop uses fixed, always-loaded terrain. Scene unload, disable, reset and target destruction still cancel subscriptions, routes and requests. A result keyed to an old epoch/intent revision cannot be adopted. Future work-cell changes must not reroll identity; no chunking service is introduced now.

The marked lab is session-only, so no production checkpoint schema or second NPC save file is added. Runtime behavior and position are transient trial deltas. Future production persistence is a versioned slice through the existing save authority. Lab reset is explicitly different from gameplay recovery: only the new enemy fixture may return to its authored anchor, after validating ground/clearance; the companion is never relocated by this adapter.

The reset registry invalidates all participants before sequential restoration; it is not an atomic rollback of the world. Register the NPC at Actor stage before Input restoration. On failed anchor clearance, keep the NPC motion disarmed and report the reason through the existing reset failure path; do not claim other participants were rolled back. Do not introduce a private replacement reset button or reset unrelated inventory rewards.

The adapter's ResetId is `CampLoop.Npc.Lager`, following Booter's Actor-stage IDs. Synchronize predecessor moves before anchor queries. Restoration records an epoch but never arms motion: the adapter requires complete registry success, unblocked laboratory input and valid active session readiness. Tests inject failures before and after NPC restoration and verify successful retry, tuning retention and component re-enable. Readiness notifications invalidate/suspend the actor; re-enable validates support at its real position. Registration changes are never made by reset callbacks.

All time-sensitive behavior uses scaled simulation time. Match real pause policy, not menu visibility; do not write timeScale. Test consistency under world-speed acceleration and pause without changing the current time owner.

## Dependency ordered batches and gates

| Batch | Work | Required gate before continuing |
| --- | --- | --- |
| PRE | Refresh current ownership and Editor/lock state; survey native support/obstacles; seal scene/terrain/companion/station baseline and dependency closure. | No overlapping scene writer; known safe anchor/region; isolated candidate represents latest accepted scene. Stop author-scene writes while owner handoff is unresolved. |
| FND | Add definitions/contracts, pure brain, tuning snapshots and bounded diagnostics. | Pinned-editor compilation; threshold, memory, target-loss, profile validation and panic-preemption tests pass. |
| NAV | Add perception, local ground route adapter, motor and lifecycle cancellation. | Focused occlusion, complete/partial/blocked routes, sweep, support, stuck retry and stale-request checks pass; existing pathfinder checks pass. |
| ART | Author primitive prefab/materials/profile and Inspector override workflow in the isolated candidate. | Root scale/collider/GUID/asset references validate; override failure is atomic; visual updates never move physics root. |
| LAB | Install candidate trial root and lab reset adapter. | Exactly one NPC, stable ID/target binding, safe anchor, repeat-install idempotence and reset tests pass; protected baseline unchanged. |
| INT | Apply the verified additive change to the refreshed author scene through guarded background authoring after owner handoff. | Scene changes match the candidate's owned delta; terrain bytes/GUIDs, existing actor records and stations unchanged by this batch; final scene validation and relevant regressions pass. |
| CLOSE | Record evidence and tuning instructions; review task-owned diff and commit coherent verified feature batch. | Only authorized files staged; no active writer race; no push/publication. User-owned feel/appearance acceptance is recorded separately. |

Import, authoring and scene-opening tests run in an isolated background candidate by default. Do not reuse another lane's validation folder as a mutable workspace. Use a dedicated candidate and the installed pinned Editor, deriving repository paths rather than copying historical machine paths. Final integration must compare the current author baseline immediately before applying, not assume the original planning HEAD is still current.

## Verification matrix

| Claim | Focused proof | Boundary |
| --- | --- | --- |
| Coherent skittish decisions | Table-driven exact threshold/equality, timer, visible/memory/expired and renewed-pressure cases. | Policy proof; not physical feel. |
| Honest perception | Occlusion, close awareness behind a wall, self filtering, target disable and same-scene binding fixtures. | Known synthetic geometry/actor cases. |
| Legal escape | Blocked corners, thin walls, slopes, missing support, cliff edge, partial endpoint, bounded detour and enclosure fixtures. | No claim of whole-valley navigation or guaranteed escape. |
| Bounded work | Counters assert profile caps, failed-route deadlines and one grid per planning pass; no per-frame full grid scan. | Cost limits, not a supported population count or FPS claim. |
| Safe lifecycle and tuning | Disable/re-enable, old epochs, invalid override, valid override while fleeing, pause/acceleration and repeated reset fixtures. | No production disk-save continuity claim. |
| Additive scene integrity | Non-mutating NPC validator plus protected record/native-data/GUID comparisons and selected existing scene/pathfinder checks. | Saved-scene and serialized-reference proof. |
| Visible behavior quality | Author observes approach, retreat, panic run, legal detour, trapped wariness and calm recovery from the normal camera. | User-owned acceptance; no agent smoke test. |

New/modified C# requires fresh pinned-editor compilation/import and executed focused EditMode tests, not just file inspection. Select regressions by actual integration coupling after rechecking current tests. Record commands, candidate/scene hashes, test counts and unresolved visual/feel limits under `Docs/Evidence/Gameplay/CampLoopNpc20261014/`. No production build is needed for this reference-scene slice.

## Failure containment and completion

Contain missing configuration to the new NPC: it stays stationary/disarmed with an actionable diagnostic while the existing camp loop remains usable. Missing safe route means Blocked/Unavailable, not an exception loop or teleport. Guard duplicate identity, query saturation and stale work. Stop integration for overlapping scene ownership, missing dependencies, unsafe placement, broken compilation/tests or a required change to packages, save schemas or another actor's authority.

Recovery preserves existing files and metadata. Keep a sealed task baseline and restore only task-owned changes through an inspected recovery path; never restore the entire scene over another owner's later work. If integration fails, retain the verified candidate and evidence and leave the author scene at its latest accepted checkpoint.

Implementation is complete when the saved Camp Loop contains exactly one Lager with the specified tunable skittish behavior, the focused compilation/tests/validator gates pass, unrelated content remains intact and documentation accurately separates those checks from pending author acceptance. Stop there: broader enemies, combat and production rollout are later slices.

The user subsequently authorized implementation. PRE through INT are complete; closeout seals only task-owned files and releases the shared scene slot. This implementation does not authorize broader NPC systems or external publication.

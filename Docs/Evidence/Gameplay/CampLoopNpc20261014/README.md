# Camp Loop Lager NPC implementation

October 14, 2026. The first Lager is installed in the saved CampLoopTesting scene. The current-source candidate passes 41 selected EditMode checks, and all six installed-scene cases pass again against the exact final scene bytes. Animation, atmosphere, crafting, inventory and riding writers acknowledged the exclusive integration slot before the guarded write.

## NPC and tuning

One bipedal primitive blockout uses a separate NPC definition, authored identity and yaw-invariant capsule motor. Its candidate anchor is approximately (-1842, -45.416, -1470), 28 m north of camp, selected by native-ground and clearance queries with at least two supported escape directions. The companion's follower, cargo and identity remain separate.

The Lager senses Booter in all horizontal directions within 20 m when line of sight is clear and vertical separation is within 4 m. It enters wary alert, retreats inside 10 m, flees inside 5 m, releases panic at 7 m and seeks 16 m calm separation. Hidden Booter leaves only a last observed position, expiring after two simulation seconds. Calm recovery lasts three seconds. These are trial settings, not final balance or species canon.

In Edit Mode, select `LagerSkittishProfile` under `Assets/_Project/Settings/NPC/` to edit grouped authored settings with Undo. During Play Mode, select the Lager actor and edit its temporary tuning draft. **Apply Tuning** validates and adopts the whole snapshot, invalidates its route and never relocates the actor; invalid drafts retain the previous snapshot. **Restore Authored Tuning** reloads the asset defaults. Play changes do not write shared assets. Lab reset retains the valid session tuning while clearing memory/routes and restoring the fixture.

The actor Inspector shows behavior, movement result, decision reason, observation age, selected route score, search/probe counts and candidate eligibility reasons. Selected-object gizmos show activity/comfort bounds, route and accepted/rejected candidates. Gait responds to actual displacement; final modeling, sound and animation polish are deferred.

## Safety and lifecycle

Behavior intent remains independent of movement failure: a trapped Lager still wants to flee but reports Blocked. Routes evaluate actual partial endpoints, continuous closest approach to the threat and segment intersection with expanded camp/station reservations. Ground support checks include diagonal footprint samples and intermediate samples at no more than 0.15 m along segments; capsule sweeps separately reject occupied corridors. This is bounded sampled-support qualification, not an analytic proof of every possible sub-sample terrain defect.

Each planning pass uses one 13 by 13 sampled grid, at most eight existing-pathfinder calls and at most 8,192 physics queries. Search allocations remain in the reused pathfinder. Eligible verified routes can be selected before the budget ends; no world-scale route or population FPS capacity is claimed. Runtime movement always rechecks support, body clearance and reservations.

The lab adapter binds the explicit session, Booter/input and NPC. Failed configuration keeps the optional fixture disarmed. Session readiness loss, scene unload and component disable invalidate motion; re-enable checks support at the real position. Reset restoration is Actor-stage, after Booter's IDs, with predecessor physics synchronization. Recording a restored epoch does not arm motion: the whole registry must succeed, reset input must be unblocked and session readiness must be valid. Tests exercise failures before and after NPC restoration, component re-enable and a successful retry.

The existing playground remains session-only. No NPC save file, terrain generation/streaming, generated-world save service, combat, loot or companion migration is installed. The only allowed fixture relocation is the explicit lab reset after anchor validation.

## Current proof

Candidate: `D:\BooterBigArmValidation\CampNpc20261014`. It began from a retained hydrated salvage copy, then refreshed current repository scripts/tests and scene dependency closure. Neither retained validation copy nor original private art was overwritten. The final candidate scene uses an additive payload based on the current author scene, rather than overwriting it with the older candidate scene.

The 41-case selected run passes without failures or skips: `NpcSkittishBrainTests`, `NpcGroundNavigationTests`, `NpcLifecycleAndTuningTests`, `CampLoopNpcSceneTests`, `TopDown3DLocalGridPathfinderTests` and `CampLoopSalvageTests`. An earlier combined run exposed a fixture setup dependency on untitled scenes; using disposable single-scene NPC fixtures resolved it without weakening assertions. Current-source compilation/import is included in the passing run. All six scene cases then pass against the whitespace-normalized exact installed payload. [Focused results](focused-tests.xml) and [installed-scene results](installed-scene-tests.xml) record the executed gates.

Static prefab rendering ran in a background candidate without Play Mode, scene save or foreground activation. The [blockout preview](lager-blockout.png) is a neutral isolated art check; it does not prove live Camp Loop lighting, visual acceptance, player pursuit or animation feel.

`prepare_camp_npc_delta.py` prepared and validated the task-owned payload while leaving the author scene untouched. It remapped four local IDs, verified three existing external bindings, and preserved 1,280 existing documents exactly; only the scene root list received one added entry. `Install-CampNpcDelta.ps1` applied it after the scene-writer handoff, with an exclusive file lock, exact source-hash guard and verified readback. The [scene receipt](scene-delta.json) records before/after hashes and IDs. Native terrain data, existing actor/station records and scene metadata were not rewritten.

Repeated candidate installation exits successfully and preserves the exact saved scene hash. The [verified manifest](verified-manifest.json) seals task-owned file hashes, candidate code/asset equality, unique GUIDs and valid documentation links. It excludes its own hash to avoid a circular receipt.

The author subsequently specified two legs and bipedal anatomy. The prefab correction retains the original left/right leg records, centers them beneath the body and removes the extra pair. The existing displacement gait now alternates those two pivots. Prefab GUID, actor identity, capsule, tuning and scene bindings are preserved; the shared author scene was not edited. The authoring recipe also creates two legs for future installs. Background Unity compilation/import and all seven focused scene/prefab checks passed, including an explicit two-leg anatomy and decorative-collider check. [Biped results](biped-tests.xml) and [correction receipt](biped-manifest.json) cover this later asset revision; the original manifest and test files remain historical implementation proof. The preview now shows the corrected biped.

No gameplay smoke test, production build, production scene change, package change or external publication is part of this implementation. Hands-on feel and appearance acceptance remain author-owned.

# Forward stop, restart and current-scene checkpoint

October 14, 2026. Continues the [focused locomotion plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) and [forward draft](../ForwardDraft/README.md). The goal remains active. The private forward policy remains unqualified and the author binding remains unchanged.

## Implemented behavior

An abrupt loss of completed travel now holds the accepted gait phase and fades the displayed gait weights directly to idle over 180ms. It does not traverse intervening walk/run/sprint roles, advance physical distance or add sustained contacts. Interruption returns continuously from the current weights to actual-travel weights over 160ms. Ordinary sustained movement still follows completed distance and the original measured cadence. These are presentation durations; the motor and input response were not delayed or retuned.

Reset/ownership invalidation now vetoes queued effects even when it happens after Update and before LateUpdate. The driver compares the motor's current tick/epoch/availability with the rendered observation before consuming effects. Disabled/re-enabled presentation owns one subscription and clears its pose envelope. The graph still has one output and evaluates one supplied pose per presentation frame. No physics root, per-bone transform writer or new runtime ground query was added.

The sole cache can also measure a normalized support plane. Existing horizontal projection delegates to the same calculation with the horizontal plane; the new API is groundwork for terrain qualification, not an enabled slope correction.

## Verification

All [220 playground checks](tests-playground.json) and [18 legacy locomotion checks](tests-legacy.json) pass without failures or skips against the refreshed current saved scene and sources. The new deterministic completed-tick fixtures cover 30/60/144Hz rendering, duplicate publications, zero/multiple fixed ticks per frame, blocked rest, stale hitch effects and same-frame reset. They exercise marked-floor contact queries without physics simulation or Play Mode. The real session startup/preview/jump/reset fixture passes both with the existing binding and an in-memory qualified forward-policy clone; this does not install that clone.

The active-rig fixtures now assert the Animator is active, verify actual foot movement between source phases, and verify repeated evaluation of the same pose does not move the foot. Earlier inactive-actor stop/slope pilots are explicitly invalidated in the private receipt. The spurious 1.83m restart height from those pilots was a harness error. No idle source was replaced on that evidence.

The [dense source matrix](dense-pose-summary.json) adds native curve keys, phase-map breaks and adjacent samples, signed-height keys, uniform phases and additional idle phases: 25,515 poses across 21 cases. Sampled body/sole floor error stays below 0.001mm. Projection error is below one micrometre, and maximum equipped height is approximately 1.811m. Supported anchor excursion remains below 19mm; cumulative tangent travel reaches about 34mm and remains a motion-review limitation.

The [final stop/restart matrix](stop-pose-summary.json) covers 288 cases and 5,664 active equipped poses across three nominal speeds, sixteen incoming phases, three frame rates and full/interrupted stops. Body and soles clear the floor within 0.001mm; maximum sampled height is 1.8091m and correction is 54.452mm, within the existing envelopes. There are no locomotion effects in the pose-only recovery probe.

The initial 120ms virtual-speed fade and 80ms restart were superseded after measurements exposed large transitional foot swings. The final fade preserves current source weights and uses the durations above. In the previously worst sampled restart at 30Hz, 3.8m/s and phase 0.6875, ankle displacement per frame fell from about 379mm to 194mm. The complete final matrix peaks at about 252mm per 30Hz frame, including intentional foot movement into recovery. This metric is not planted-foot drift or appearance acceptance. Native loop-end samples and repeated same-pose checks did not establish a seam discontinuity; the large recovery movements scale with frame duration.

## Current scene and dependency proof

The old frozen validation scene lacked newer Camp components. The guarded [hydration planner](../../../../../Tools/Diagnostics/plan_camp_locomotion_hydration.py) and [transfer tool](../../../../../Tools/Diagnostics/Apply-CampLocomotionHydration.ps1) collected the saved Camp scene GUID closure plus current scripts/tests, dynamic Resources and shader includes. With the task-owned Editor stopped, 721 changed/missing files, about 66.5MB, were transferred to the marked candidate. Before copies and the exact plan remain under its `CurrentSceneHydration20261014` recovery directory. The repository was read only during that transfer.

The [Unity verification](current-scene-integration.json) confirms all 2,525 selected files matched the current root at verification, 1,353 metadata GUIDs resolved to canonical paths, all 27 external GUIDs resolved into pinned packages, and the imported scene had 494 dependencies. The structural validator preserves nine native terrains, twelve stations, the twelve original Ironstone nodes and the cache. It reports no errors. The final [source manifest](source-manifest.json) seals subsequent task-owned transition/test tuning against the same candidate. Packages and editor version were not changed.

A background author-editor read initially timed out at the default 5s limit. Command discovery showed an explicit eval timeout parameter; a read with a longer limit succeeded and reported clean saved CampLoopTesting in Edit Mode. It did not activate a window, save the scene or alter the player binding. Any eventual cutover must recheck live ownership/dirty state at that time.

## Open gates and continuation

The [corrected active-rig slope audit](slope-envelope-audit.json) rejects whole-host lifting as the general terrain solution. It requires up to 525mm additional lift on sampled 48-degree planes; even some 15-degree cases exceed the 80mm visual bound. This is a complete-sole proxy on uniform support planes with upright capsule contact geometry, not terrain/controller acceptance. Do not widen the host bound or raise the body beyond the reviewed collision envelope to hide it. Qualify terrain-relative leg/stance adaptation within the existing graph/pose owner and retain the physics root. Unity exposes [Humanoid goal solving in an animation job](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimationHumanStream.SolveIK.html); that route is researched, not implemented or accepted by this checkpoint.

Next work remains actual slope adaptation and steering/reversal qualification, transition/cross-owner cold-return coverage, the private qualified binding/readiness feedback and scoped live cutover. Then hand the concrete current-scene result to the author for walking/running/sprinting and start/stop playtesting. Source appearance, cumulative drift and movement feel remain author-owned acceptance gates. Broader animation expansion stays deferred.

## Recovery and boundaries

The [private receipt](private-receipt.json) preserves raw matrices, invalid/superseded pilots, hydration evidence and source snapshots under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/ForwardTransitions20261014`. All 127 prior private payload/metadata pairs still match PHY1LowMotionQualified. The five forward draft pairs are unchanged from the preceding checkpoint. Rollback is the scoped code delta and paired new script metadata; do not restore an older whole scene or discard concurrent work.

Pose/clock state is transient actor state. Existing authored world identities and fixed always-loaded terrain remain authoritative; no generation, streaming, persisted gait state or generated-object identity was added. Unload/disable releases subscriptions and resets the presentation state. No author-owned scene/binding, terrain, source animation bytes, package version, production setting, Play Mode or gameplay smoke test was modified/run. Foreign Fused/VFX files remain outside these commits.

# Forward terrain observation and pose prototype

October 14, 2026. Continues the [transition checkpoint](../ForwardTransitions/README.md) and [focused plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The goal remains active. The forward family is still unqualified and unselected in the author's binding.

## Implemented boundary

The motor now retains the point and normal from its existing true-surface ray. Standing presentation can read the resulting plane only from an eligible completed motion observation. Reading adds no raycast, allocation, physics movement or pose change. Disabled, moved and Rigidbody-owned supports are vetoed immediately; reset and action/posture ownership invalidate the snapshot. Duplicate/unavailable observations cannot expose a new plane. The crouched probe retains its existing behavior and discards the additional point output.

The [source manifest](source-manifest.json) matches the three changed motor files, their metadata and the new fixture pair against the current isolated candidate. The [223 playground checks](tests-playground.json), [18 legacy locomotion checks](tests-legacy.json) and [six slope checks](tests-slopes.json) pass without failures or skips. New static collision fixtures check actual flat/20/40-degree marked surfaces and disabled/moved/Rigidbody-owned support/reset boundaries without physics simulation. An initial compile caught the additional crouch caller of the shared normal helper; that caller is included in the final passing candidate. Initial scene-creation harness failures were corrected by using the existing disposable saved-scene pattern.

## Terrain experiment

The [offline tool](../../../../../Tools/Diagnostics/CampForwardTerrainQualification.cs) inserts an animation job after the existing mixer and before its sole output. It verifies an active Humanoid, obtains the source foot goals, aligns foot orientation to a uniform plane, changes goal height and optionally lowers the Humanoid body before solving. Unity exposes this operation through [AnimationHumanStream.SolveIK](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimationHumanStream.SolveIK.html). The experiment confirms world-metre goal positions and the accepted avatar's differently scaled foot-height property; it does not use the unscaled height as a world offset.

The final [matrix](terrain-prototype-summary.json) covers 336 cases and 21,840 sampled poses: idle and three nominal speeds, 65 phases, six inclines through 48 degrees, four headings except flat and four trial body lowerings. Each pose first evaluates the qualified flat baseline, then evaluates the terrain job at the same timestamp. Two offline evaluations are intentional measurement scaffolding, not a proposed runtime loop or performance proof. Original clips, phases, source rates and the physics root are unchanged.

The selected semantic plant side is measured at each quarter-cycle centre and within +/-0.03125 cycles. An ankle-height proxy is retained separately and is explicitly not accepted stance classification. Maximum all-pose goal error includes airborne feet and must not be described as planted drift.

| Trial | Maximum plant-window goal error | Worst sole-plane penetration |
| --- | ---: | ---: |
| 10 degrees, 80mm body lowering | 0.033mm | 0.123mm |
| 20 degrees, 80mm body lowering | 0.681mm | 0.786mm |
| 30 degrees, 160mm body lowering | 0.794mm | 0.898mm |
| 40 degrees, 240mm body lowering | 4.277mm | 1.075mm |
| 48 degrees, 240mm body lowering | 100.630mm | 1.344mm |

These are bare-rig uniform-plane experiments, not accepted settings. Small penetration alone does not prove that a downhill plant reaches its target. Constant large lowering would also change posture unnecessarily on gentler ground. The results support further bounded leg/reach work and reject installing this prototype as general terrain support. No body lowering or animation job has been added to runtime presentation, and the existing +80/-30mm host envelope is unchanged.

## Remaining work and recovery

Next, qualify a terrain adaptation with an actual supported contact classifier, preserved flight/stride motion, bounded continuous body adjustment and the existing single pose output. It must fit the accepted body/equipment and collider/roof envelope, use completed support planes, and meet the existing allocation/evaluation budgets. Native uneven terrain, blends, steering, support/owner handoffs and source contact drift remain open, followed by private binding/readiness integration and the author's feel/appearance review. The new observation API is infrastructure for that work, not a claim that slopes are fixed.

The [private receipt](private-receipt.json) seals 15 retained files under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/ForwardTerrainProbe20261014`: final sources and six exploratory reports. Earlier v1-v3 matrices are retained exploration, not source-bound qualification; v4 is bound to the source snapshot. Raw bone values stay private. All 127 prior asset/metadata pairs and ten forward-draft files still match in both checkouts. Rollback is the scoped motor/fixture delta with its paired new metadata; do not restore an older scene or replace private assets.

Authored identities and fixed, always-loaded terrain remain authoritative. No generated IDs, terrain generation/streaming or persisted gait state is introduced. Observation state is transient and invalidated on lifecycle/owner changes. The author scene/binding, source animations, terrain, packages and production settings remain unchanged. No Play Mode or gameplay smoke test was run; foreign Fused/VFX files remain outside the commits.

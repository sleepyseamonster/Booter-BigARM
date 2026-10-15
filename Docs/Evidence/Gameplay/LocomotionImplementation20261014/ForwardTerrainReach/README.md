# Terrain reach and source screening checkpoint

October 14, 2026. Continues the [gated terrain prototype](../ForwardTerrainRuntime/README.md). The implementation goal remains active. Revision 13 is an independently disabled experiment, not a qualified player cutover. Both private qualification flags and PlayerBindings remain unchanged.

The pose stage now maps the complete foot trajectory into the support frame at the capsule's actual plane contact. Joint limits constrain the normal component while retaining tangent stride. A bounded search reduces body lowering when it would prevent sole clearance. Near-flat onset includes the knee hint. The analytic solve records unreachable targets; its final position correction is limited to an already reachable residual of at most 1mm. One graph output, the physics root and existing visual-host limits are preserved.

The selected sprint source reaches 144.44 degrees of native knee flexion across both legs. An earlier 140-degree trial limit was therefore inappropriate; this revision uses a provisional 150-degree limit. This is a source compatibility bound, not motion acceptance. The new airborne-foot fixture independently checks tangent stride, clipped flight height, orientation, actual output position and flexion. A previous approximately 0.2mm apparent output error came from inferring host motion from a knee; the fixture and sweep now measure the visual host directly.

All [243 playground](tests-playground.json), [18 legacy locomotion](tests-legacy.json) and [six slope](tests-slopes.json) Edit Mode checks pass without skips. The [source manifest](source-manifest.json) pairs exact runtime/test bytes and metadata with the isolated candidate. These tests establish structural and sampled behavior; the wider sweep still rejects installation.

## Qualification results

The [revision-bound actual-driver sweep](terrain-summary.json) attempts 34,056 poses in 264 speed/incline/heading cases. Of these, 720 are explicitly unavailable: 66 at 30 degrees, 282 at 40 degrees and 372 at 48 degrees. Unavailable samples are excluded from geometry and adjacent-step calculations and are not passes. There are 4,405 complete equipped-mesh samples.

- At 48 degrees, 115 poses still exceed the existing garment limit; equipped penetration reaches 379.44mm when that correction is rejected.
- The largest coarse knee step is 729.29mm per 1/128 cycle. Three refinements reduce the maximum local step to 0.0864mm per 1/4,194,304 cycle. This location is a very fast swing; other locations remain unqualified. Maximum recorded foot-target error is 2.32mm.
- The warm Edit Mode presentation sample measures 448.00 microseconds and zero recurring managed bytes. It includes prepare, one graph evaluation and garment projection. This run is within the provisional 500-microsecond budget, but earlier runs exceeded it; machine-concurrent Editor timing does not establish Player performance or a stable performance margin.

The [seven-source screen](source-screen.json) independently samples 11,375 poses. Both walking sources avoid its reach rejection; the five running/sprinting sources have rejected poses. The screen omits equipment, controller timing and contact qualification, so it accepts no replacement. Its original clips are temporary in-memory inputs; native files and importers were not changed.

The [private receipt](private-receipt.json) seals 301 source/raw measurement files in a new local archive. V6 and SourceScreenV2 are bound to revision 13 and source hashes. Earlier pilots are exploration; probes 14–16 lack loaded-code proof and cannot support comparisons. The [private verification](private-verification.json) confirms 127 prior payload/metadata pairs and ten draft files still match both checkouts.

## Author direction and remaining work

The author selected a slower uphill gait on very steep walkable ground. Flat-ground targets remain 1.4/2.7/3.8m/s. The [revised plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) requires a continuous slope policy, braking on approach, smooth release and actual-displacement cadence. **That speed policy is not implemented by this checkpoint.** The broad speed/slope matrix remains useful stress evidence; a future operating envelope cannot silently discard fast entry transients or downhill/sidehill failures.

Terrain reach/equipment, contact drift, steering, native uneven support, action handoffs, current author-scene integration and hands-on acceptance remain open. The smaller experimental flat-deck review option in the plan remains available after its own prerequisites. No scene, terrain, package, source FBX or private binding changed. No Play Mode, gameplay smoke test or foreground activation occurred. Fixed authored world identities and always-loaded terrain are unaffected; the stage adds no generation, streaming or persisted gait state.

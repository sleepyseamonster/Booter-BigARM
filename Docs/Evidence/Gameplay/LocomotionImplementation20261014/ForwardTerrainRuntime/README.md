# Gated forward terrain runtime checkpoint

October 14, 2026. Continues the [support-plane experiment](../ForwardTerrain/README.md) and [focused locomotion plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The goal remains active. This checkpoint implements and measures an optional terrain stage; it **does not qualify or install it**. The private forward and terrain flags remain false, and PlayerBindings is unchanged.

## Implemented and verified boundary

The optional stage sits between the driver's existing mixer and its single output. It uses the paired bounded sole cache, completed static support plane and an analytic two-bone adjustment. It preserves native flat poses, carries airborne feet with the body adjustment and maps source stride into the surface tangent. Body lowering stays inside the animation graph; animation does not move the physics root. Existing +80/-30mm visual-host limits remain intact. Trial body/garment caps are separately validated, not permissions to qualify failed poses.

The existing garment correction now runs through actual LateUpdate for this owner, without a second graph evaluation. Cold equipment normalizes its influences before projection. Same-frame reset or owner changes invalidate the plane and restore the garment; disable/destroy releases the stage and native buffers. A rebased observation may supply a static pose plane only through an explicit option; it still supplies no completed travel or footsteps.

The [final source manifest](source-manifest.json) pairs nine runtime/test sources with their metadata and records the three diagnostics. All paired source and metadata bytes match the isolated candidate. The [242 playground](tests-playground.json), [18 legacy locomotion](tests-legacy.json) and [six slope](tests-slopes.json) Edit Mode checks pass without skips. The new 18-case terrain fixture includes one/two/four influence quality, actual LateUpdate, native-flat equivalence, near-flat onset, action/reset ownership, cold equipment and two recorded knee-switch locations. These finite fixtures are structural and sampled-pose proof, not full terrain qualification.

## Wider qualification still fails

The [source-sealed sweep](terrain-summary.json) uses the real driver and LateUpdate: eight speeds from idle through 3.8m/s, five inclines through 48 degrees and eight headings except flat. Its 264 cases contain 34,056 pose samples and 4,488 complete equipped-mesh samples. The runner records source hashes before and after measurement. Mesh baking is offline evidence tooling only.

| Measured boundary | Result |
| --- | --- |
| Equipped clearance, 0/15/30/40-degree sampled planes | Within 0.001mm of the plane; this alone does not accept motion quality. |
| 48-degree equipment | Three cases exceed the 350mm garment cap, across 115 pose samples. Maximum requested correction is 383.27mm. Uncorrected equipped penetration reaches 379.47mm. |
| Maximum body lowering / host correction / equipped height | 215.96mm / 52.20mm / 1.80955m in this sweep. |
| Largest coarse knee step | 752.46mm per 1/128 cycle at sprint, 48 degrees. Refinement reduces its sampled step to 0.227mm over 1/4,194,304 cycle: a very fast swing, not evidence of a finite jump at that location. The motion remains unacceptable for installation. |
| Warm presentation cost | Final source-bound run: 844.90 microseconds per sample, zero recurring managed bytes. An earlier run measured 563.33 microseconds. Both exceed the 500-microsecond budget; these are Editor wall-time observations under concurrent machine work, not Player performance. |

Earlier solvers also produced genuine 130–396mm knee switches at arbitrarily small phase changes. Replacing the cross-slope pole choice and blending the knee hint at terrain onset repairs the specifically recorded locations. It does not establish continuity everywhere. The earlier smaller matrix and initial coarse clearance results must not be treated as acceptance; the wider sweep controls this gate. The final runtime prototype stays independently disabled.

## Preservation and continuation

The [private receipt](private-receipt.json) seals 1,077 source/raw evidence files in an independent local checkpoint. Raw bone/vertex tracks remain private; earlier experiments are labelled exploration, while V5 carries the final in-run source seal. Diagnostic tool files received only UTF-8/terminal-whitespace normalization afterward; the source manifest records both hashes, and the exact measurement versions remain in the private seal. Runtime/test bytes did not change. The [private verification](private-verification.json) confirms all 127 prior asset/metadata pairs and ten forward draft files still match in both checkouts. No source FBX, private binding, author scene, terrain, package or production setting was changed.

Next work must resolve steep contact/reach and equipment fit without widening bounds, then qualify contact drift, steering and action handoffs. The original approximately 34–35mm cumulative flat contact travel remains an open motion-review limit. Native uneven terrain and support changes need separate evidence; uniform planes cannot prove them. Performance and author-scene promotion remain gated, followed by author-owned feel/appearance review. The plan's earlier flat-deck review option remains available only as a clearly bounded experimental checkpoint after its own prerequisites; this receipt does not activate it.

Camp Loop uses authored identities and fixed, always-loaded terrain. This transient stage adds no generated identity, terrain/chunk streaming or persisted gait state. Rollback is the scoped source/test delta with paired metadata; do not restore an older whole scene. No Play Mode, gameplay smoke test or foreground activation was used. Concurrent rendering/editor work and Fused/VFX files remain outside this batch.

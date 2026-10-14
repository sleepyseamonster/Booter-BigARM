# Forward locomotion draft

October 14, 2026. Continues the [focused implementation plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). This is a verified, gated runtime/data checkpoint. The goal remains active; the author player still uses its prior binding.

## Implemented

The optional `AnimationPlaygroundForwardLocomotionProfile` pairs the accepted model, complete twelve-role clip profile and foot cache. It retains original imported MCU walk/jog/run sources at 1.4/2.7/3.8m/s. Measured semantic plants map monotonically to the native R0-L0-R1-L1 sequence without discarding source asymmetry or rewriting source clips. Pure and significant-contributor blend rates are validated separately; the production profile's 0.90–1.10 contract is unchanged.

Independent low-drift fitting initially chose different stages of stance in adjacent clips. The supporting ankle could differ by over 30cm relative to the hips. The stage-aligned recipe instead requires the supporting ankle within 6cm of the hips. All proposed centres meet that bound. This improves run/sprint blends without a competing bone writer.

One remaining walk/run stance drifted about 31mm. A measured speed-dependent distance derivative, active only around the second right stance and only between walk and run, brings that initial sample below 14mm. Its trapezoid integral is monotone and invertible; completed physical distance advances semantic phase. Changing speed preserves unwrapped phase. Rendering interpolates accepted physical distance without adding simulation contacts. Native source derivatives are checked at map, speed and distance-calibration breaks for every contributor of at least 10% weight.

The existing driver owns one manual graph evaluation at the selected source times. It applies signed source height fitting and bounded cached sole projection through its visual host. It does not move the physics root or change limb poses. Below walk speed, blending idle shortens stride while preserving cadence. Zero/blocked travel blends into the supported breathing idle. The optional path gates old starts/stops/pivots and the sideways overlay; higher presentation owners retain their existing route.

Completed ticks generate all quarter-cycle contact identities. A separate eight-entry queue permits at most two cosmetic effects per rendered frame and discards stale effects beyond 0.12s or 0.03 cycles. LateUpdate samples effects from the displayed pose. Disable/re-enable owns one subscription; idle time is retained across normal frames and reset after another presentation owner. Tests cover those boundaries without running Play Mode.

## Measurements and limits

The [source calibration](source-calibration.json) records native contacts, strides and pure effective rates. Full recipes and raw licensed pose/vertex tracks stay local. Signed height keys must retain bounded linear interpolation; malformed curves, newer schemas, mismatched pairs and unqualified policies fail readiness.

The [pose matrix](pose-matrix.json) covers 15 actual speeds, one/two/four global skin weights with Auto renderers, plus a cold four-bone equipment case: 60 cases and 7,740 uniformly sampled geometry poses, with 240 separately sampled contact windows. It uses the actual driver pose method, accepted rig, existing cloak/backpack and the 674-point cache. There is no runtime mesh baking.

- Sampled sole/body floor error is below 0.001mm, within the 1mm target. Maximum sampled equipped height is about 1.811m under the 1.82m standing collider.
- Maximum supported fixed-patch anchor excursion is 18.832mm. Cumulative tangent travel reaches 34.874mm. The approximately 2cm accumulated-drift aim therefore remains an open motion-review gate; this checkpoint does not declare source/visual acceptance.
- Some low analog full-source windows include a brief unsupported portion. Reported supported traces use a connected window with at most 12mm ground gap and a persistent vertex identity. A rolling contact can use two explicitly overlapping identities. Missing full-window classification is not reported as a complete contact pass.
- Sprint's full-loop nearest-sole gap reaches about 13cm during intended flight. Height fitting does not push those airborne poses down to the floor.
- The rigid packed kit contains 532 measured vertices. It stays above the floor; its sampled height envelope is about 0.882m. Boots have inventory state without a separate equipped renderer.
- Warm core pose evaluation is about 0.09ms with zero recurring managed bytes. The [100,000-sample clock benchmark](clock-performance.json) measures advance plus distance interpolation at 8.45 microseconds/sample with zero recurring bytes, within the 0.2ms observation/clock reservation. These are Editor measurements, not player profiling.

This finite matrix is preliminary. Its idle contributor is sampled at time zero. It does not establish other idle phases, dense key/break coverage, native slopes, cold one/two-bone returns from low actions, steering, transition feel or current author-scene integration.

## Candidate and private assets

The isolated candidate had 75 byte-identical animation payload aliases with canonical GUIDs outside the expected Mixamo folder. Unity had regenerated canonical metadata. The [repair receipt](candidate-alias-repair.json) records 103 quarantined aliases, including folder metadata, and 104 restored canonical metadata files. Recovery copies remain under the candidate's `AssetAliasRecovery20261014`. After a hidden candidate-only restart, all 127 prior payload/metadata pairs matched and all 127 GUIDs resolved to canonical paths. The author assets were not repaired or moved. Earlier alias-based measurements were byte-equivalent but had incorrect path provenance; final tests/matrices use canonical resolution.

The [private receipt](private-draft-receipt.json) seals five new unqualified profile/metadata pairs and private measurements under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/ForwardLocomotionDraft20261014`. Matching local copies are present in the ignored PrivateBindings folder. `ForwardLocomotionDistanceAlignedCandidate.asset` references `ForwardLocomotionClipsStageAlignedCandidate.asset`; PlayerBindings does not select them. Original 127 pairs still match PHY1LowMotionQualified.

Restore the prior private checkpoint first, verify its hashes, then copy only absent or identical draft payload/metadata pairs from `DraftAssets`. Do not overwrite different bytes or set qualified true. Code rollback is the task-owned delta with paired new script metadata. Do not restore an older whole scene, discard unrelated work or regenerate metadata.

## Verification and continuation

All [209 playground checks](tests-playground.json) and [18 legacy locomotion checks](tests-legacy.json) pass without failures or skips. The final [10 forward fixtures](tests-forward-final.json) rerun the modified policy, curve/rate bounds, inverse-distance/subdivision, immutable render ledger, graph/root ownership and lifecycle contracts. The [source manifest](source-manifest.json) binds the source bytes and metadata to the isolated candidate; tools and private recovery are separately hashed.

Next: dense key/break and transition geometry; start/stop/rest and actual-tick/contact integration; steering/reversal and slope handling; cross-owner cold/return permutations. Then hydrate the current saved Camp scene dependency closure, seal the qualified binding, add readiness feedback and perform the scoped background installation. Offer the experimental trio review at that point, retaining open L4/L6 limitations. The older frozen candidate scene cannot establish current-scene integration.

Fixed authored terrain and existing actor identity remain authoritative. No world generation, streaming, persisted gait state, package/settings change, production promotion, Play Mode, gameplay smoke test or foreground activation was performed. Broader animation expansion stays deferred. Hands-on appearance and feel acceptance remain author-owned.

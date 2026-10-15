# Turn source contract and contact checkpoint

October 14, 2026. Continues the [focused plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) and [steering checkpoint](../ForwardSteering/README.md). The full goal remains active. A typed, validated source asset is implemented and tested, but it remains unqualified and disconnected from the player. This batch does not implement or accept the final turn controller.

## Implemented source contract

[AnimationPlaygroundTurnProfile](../../../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundTurnProfile.cs) pairs the accepted model, sole cache, scale/offset, exact source/contact receipt hashes and two distinct named clips. It stores evaluated signed yaw against native source time, with allocation-free binary lookup in both directions. Source yaw pauses retain their earliest inverse time. It rejects wrong signs, nonfinite/nonmonotone samples, duplicate timestamps, partial clips, duration/calibration mismatches, swapped clip identities and discarded root trajectories above 1mm.

The default validation route rejects unqualified data. Offline probes must explicitly request the unqualified route. The new type adds no motor, input, graph or world owner; neither the author scene nor the existing player binding references this asset.

The private `ForwardTurnCandidate.asset` is materialized from the named 120Hz source observation. Its left/right clips retain approximately −180/+180 degrees over the native 1.50s. Raw importer root curves are not substituted for evaluated mirror behavior. The [authoring receipt](source-authoring.json) binds exact observations and serialized bytes. A local identity-field upgrade retains the new asset's original GUID and metadata, with its earlier bytes preserved privately. Fresh authoring includes that identity field directly. The exact unqualified asset/metadata pair is now restored locally in the author checkout for the offline checks; it remains disconnected from the player.

## Grounding and equipment

The [contact observation](contact-observation.json) evaluates both named turns at nominal 120Hz with cold authored equipment and one/two/four bone influences: six cases, 1,092 poses. It supplies evaluated source-root yaw to the existing single-output review graph without running the motor or physics. The existing sole cache supplies bounded upward host projection; no live root-motion owner is installed.

All sampled full-mesh body, cloak and rigid-kit positions are finite. Maximum required lift is **8.278mm**, maximum height is **1.79876m**, and the worst floor residual/cache prediction difference is **0.0229mm**. The sampled geometry fits the existing 80mm lift, 1.821m height and 1mm penetration limits. This remains a finite flat-floor screen, not continuous-pose, native-slope or player-performance qualification.

## Contact remains unresolved

The optimistic window test chooses the best fixed sole point that stays within −1mm/+8mm of the floor across each centered window. It does not prove support or a persistent rolling contact. Four-influence results are:

| Actual window | Left-turn coverage within 20mm | Right-turn coverage within 20mm | Worst best-point excursion, left / right |
| --- | ---: | ---: | ---: |
| 82.9ms | 100% | 100% | 17.95 / 18.26mm |
| 132.6ms | 77.71% | 86.75% | 29.32 / 29.72mm |
| 182.3ms | 71.88% | 75.63% | 40.18 / 41.19mm |

Even the shortest test switches its selected point 95/96 times. Its 100% result cannot qualify a planted foot. The longer windows expose drift; the 132.6ms left-turn result includes about 307ms of consecutive uncovered window centers. Do not shorten contact definitions or widen budgets to turn this screen into acceptance. Measure persistent patches/rolling and qualify bounded correction or source adaptation before enabling a turn transition.

## Verification, recovery and author state

All [303 animation-playground Edit Mode checks](tests-playground.json) pass without skips, including 21 new [turn-contract checks](../../../../../Assets/_Project/Tests/Editor/AnimationPlaygroundTurnProfileTests.cs). They cover signed interpolation/inversion, pause behavior, invalid samples, target/source pairing, named identity, provenance, the default qualification gate and absence from the authored scene dependency graph. Passing these data/ownership checks does not accept the failed contact screen or author feel.

The [source manifest](source-manifest.json) records the initial pairing of task code/metadata and all 272 measured inputs with the author checkout, plus any later source deltas. The [repeat verification](repeat-verification.json) records identical pose rows and inputs across three executions, while recording loaded assembly revisions separately. These are sealed measurements of those source versions. Concurrent author scene/lighting work is outside this batch; that scene is not paired or overwritten.

The initial [author state](author-state.json) observed Play Mode, with the new type not yet loaded; no author refresh or private asset transfer was attempted then. The subsequent [startup repair](../StartupLookup/README.md) compiled in the author Editor and passed 304 playground checks in the isolated candidate. It repairs early scene lookup for character preparation and F9 input. A fresh author Play Mode observation remains pending. The later [data restoration](author-data-restore.json) imports only the exact unqualified asset pair in Edit Mode, verifies its validation gates and absence from scene dependencies, and retains the dirty scene and identical saved-scene hash. This satisfies local source/test restoration; player activation still requires contact, timing and handoff qualification.

The [private receipt](private-receipt.json) preserves exact source, private curves/poses, asset/metadata, authoring history and test execution, including an immutable final-execution supplement. Unchanged licensed inputs reuse verified earlier seals by SHA-256; duplicate raw tracks use declared aliases. Restoration must verify those references and expand aliases. Do not restore an older scene or bulk-copy recovery source over concurrent work. No licensed motion payload or pose track is published here.

Reproduction uses explicit `run_script` entries in [CampTurnSourceScreen](../../../../../Tools/Diagnostics/CampTurnSourceScreen.cs), [CampTurnContactQualification](../../../../../Tools/Diagnostics/CampTurnContactQualification.cs) and [CampTurnProfileAuthoring](../../../../../Tools/Diagnostics/CampTurnProfileAuthoring.cs). Generate the immutable `TurnSourceContactBasisV1` and `TurnContactV1` inputs before fresh authoring. Existing output/asset guards prohibit overwriting earlier trials. The upgrade entry is only for the exact privately recorded initial candidate; it is unnecessary for fresh authoring.

The subsequent [complete-track checkpoint](../TurnContactTracks/README.md) repairs an export omission: the original dynamic track DTO's saved files lacked their frame lists, although the contact metrics used the complete in-memory data. Its six complete replacement tracks reproduce every earlier numeric result and preserve the original archives. Persistent-contact analysis rejects a simple fixed-pivot correction; the current diagnostic source version and its new private seal are recorded there.

No agent entered Play Mode, activated a window, ran a gameplay smoke test or changed production. Deterministic world identity, terrain/chunk streaming, generated IDs and persisted deltas are inapplicable: this static source contract and temporary pose measurement create no runtime world state. Disable/reset/world lifecycle behavior remains a requirement for the future turn controller, not a claim made by this data slice.

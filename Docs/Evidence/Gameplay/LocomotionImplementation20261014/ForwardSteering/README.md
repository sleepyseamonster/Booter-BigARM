# Forward steering and turn-source checkpoint

October 14, 2026. Continues the [focused plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md) after the installed [AP02 review](../ForwardReview/README.md). The goal remains active. This batch adds diagnostic implementation outside `Assets`; the author controller, scene, binding and qualification flags are unchanged. The author review remains available through F9 → Reset to gait deck.

## Reproduced steering defect

The existing motor independently accelerates velocity toward input and rotates the body toward velocity. After a reversal, its forward clip can therefore travel backward relative to facing. The [numerical command model](command-model.json) uses the saved lab's 1.4/2.7/3.8m/s speeds, saved motor settings and loaded runtime acceleration/turn helpers. It assumes each issued rotation completes in its tick; it does not step physics or establish collision/interpolation behavior.

Six hundred traces cover starts, stops, signed 45/90/135/180-degree changes and continuous turns at 30/50/60/120Hz. The existing model reaches 169.5 degrees of velocity/facing mismatch and 90.26mm of backward travel. At 50Hz, its complete 180-degree response is:

| Gait | Existing backward travel | Existing arrival | Prototype arrival |
| --- | ---: | ---: | ---: |
| Walk | 73.51mm | 0.52s | 0.54s |
| Run | 68.60mm | 0.62s | 0.78s |
| Sprint | 77.13mm | 0.72s | 1.00s |

Arrival requires velocity within 0.02m/s of target and facing within one degree. The prototype selects nonnegative forward velocity within the same acceleration ball and reduces yaw if needed for feasibility. All sampled speed/acceleration/yaw limits hold; 210 independent ray/ball projection checks pass. No trace uses its unavailable fallback. Sampled facing error stays below 0.03 degrees and backward travel is zero. This alone does not qualify the prototype: the slower reversal is a response tradeoff, and feet still need a valid turning pose.

## Pose result: installation rejected

The [equipped review graph observation](pose-observation.json) covers 48 cases and 7,200 direct poses: all three speeds, straight travel/180-degree reversal, four starting phases and both policies. It retains the single output, accepted rig and authored four-influence equipment. Roots are manually placed from numerical commands; phase follows modeled distance. This is animation measurement without controller/physics simulation or hands-on testing.

Each plant tracks a fixed nominal-gait sole vertex inside significant contributors' intersected stance proposals. Broad windows include points up to 24.26mm above the floor and must not be called proven contacts. The separate near-ground metric requires the same point between −1mm and +8mm for at least three contiguous 50Hz samples. Height alone still does not prove support; rolling and changing blend windows remain interpretation limits.

| Motion | Existing maximum near-ground excursion / travel | Prototype maximum near-ground excursion / travel |
| --- | ---: | ---: |
| Straight | 19.96 / 25.63mm | 19.96 / 25.63mm |
| 180-degree reversal | 447.02 / 467.76mm | 194.27 / 215.85mm |

The maxima are across cases, not necessarily the same window. The prototype improves alignment but remains far outside the provisional drift target. Do not install it alone, count elevated points as contact or widen limits to pass. The straight 50Hz observation does not replace the earlier denser 34–35mm cumulative-travel finding. This probe measures selected points, not complete-mesh clearance or every continuous pose.

## Existing turn sources

The [named source screen](turn-source-screen.json) resolves four clips from three existing imports. The retained jog pivot pair has no applied root turn in this screen. `m@RapidTurningLeft.fbx` contains **RapidTurningLeft** and **RapidTurningRight_Mirror**; a path-only sampler would silently choose one of them.

Separate manual graph samples on the accepted target show approximately −180 and +180 degrees of applied root yaw over 1.50s, respectively, with negligible net translation. Raw imported `RootQ` curves have the same sign for both mirrored clips; their sign must not become a mirrored motor contract. Evaluated root motion establishes the signed candidate behavior. Root motion is enabled only in this isolated animation measurement.

These are candidates, not accepted transitions. Raw rapid-turn soles reach roughly 8.2mm below the nominal floor; jog pivots reach roughly 34.7mm below it. Grounding, actual contact windows, native timing, blend continuity, equipment and response require qualification. Bone-height proposals can span moving feet and cannot establish a planted foot. All source pose tracks remain private.

## Proof and next implementation gate

[Repeat verification](repeat-verification.json) records identical rows and source hashes across two executions of each final measurement: 117,000 command ticks, 7,200 equipped poses and 1,020 source poses per execution. Both diagnostics compile without errors and execute in the marked background candidate. The [manifest](source-manifest.json) pairs 24 selected inputs/tool sources; this is not a new full-project dependency or player-performance proof. The previous 306 Edit Mode checks belong to the installed review checkpoint and were not rerun for this tool-only batch.

Use CLI `run_script` with the exact file and entry: [CampForwardSteeringAudit.cs](../../../../../Tools/Diagnostics/CampForwardSteeringAudit.cs) (`Run`, then `RunPoses`) and [CampTurnSourceScreen.cs](../../../../../Tools/Diagnostics/CampTurnSourceScreen.cs) (`Run`). Supply a fresh alphanumeric output name as the JSON argument array. `RunPoses` requires the sealed `ForwardSteeringCommandV3` settings/source receipt and retained native calibration recipe. Entry points reject the author checkout, Play Mode, compilation and overwriting prior output. No assets or scene are saved.

The [private receipt](private-receipt.json) records the 71-file base seal and ten-file contact-classifier supplement: failed trials, raw poses, calibration input, selected model/motion bytes, metadata and diagnostic source. Restore the base before the supplement. Other private assets retain their preceding checkpoints; no licensed pose track or payload is published here.

Next, qualify named turn-source contact/grounding and bounded timing, then prototype braking/turn/restart and interruption through the existing motor and single output. Preserve completed-distance phase, command limits, input responsiveness and owner/reset rebasing. A source curve must not become a second root writer. Reject the candidate before installation if contact or response fails. Native slopes, moving action handoffs and author acceptance remain open.

No window was activated, Play Mode entered, gameplay smoke test run or production scene changed. World identity, fixed terrain, streaming, generated IDs and persisted deltas are inapplicable to these temporary diagnostics because they create no runtime world state. Rollback is the two diagnostic files and documentation delta; no asset rollback is required.

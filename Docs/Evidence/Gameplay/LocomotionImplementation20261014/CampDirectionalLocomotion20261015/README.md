# Camp Loop directional locomotion and native ground

October 15, 2026, Phoenix time. Continues the author's request for walking/running strafes, diagonals and backward movement while preserving the accepted forward gaits. The [implementation record](../../../../Design/Gameplay/CAMP_LOOP_DIRECTIONAL_LOCOMOTION_2026-10-15.md) supplies scope. This selection supersedes the preceding slow lateral and uniform mild-plane review, whose assets and sealed evidence remain preserved.

## Installed behavior

Hold LT to capture completed body facing, including while moving. Move through the full circle with analog gait blending; LT retains camera look-ahead. Release LT to resume ordinary steering. There is no forced stop between directional sectors and no diagonal speed bonus. Existing menu, focus/device, reset and owner-transfer gates still require neutral/release where appropriate.

| Travel | Physical limit | Sources |
| --- | ---: | --- |
| Forward | Ordinary 5 m/s; RB 6 m/s | Exact accepted idle, walk 1.4, jog 3, run 4, fast run 5 and sprint 6 anchors |
| Left/right | 5 m/s | Complete sideways walk and run, continuously blended |
| Backward | 4 m/s | Complete neutral backward walk and backward run, continuously blended |
| Diagonal | Continuous interpolation of neighboring limits | Blended neighboring cardinal sources; normalized input magnitude |

The accepted forward profile and source bytes are unchanged. Pure forward LT travel returns to that exact branch at its existing distance phase. All channels share the completed-distance clock in one Animator/output. Backward walking has a 1.1 m/s source anchor and backward running 1.8 m/s; these describe the source blend, while its calibrated running playback supports travel to 4 m/s. Side walk/run anchors are 1.4/3 m/s. The earlier schema-1 0.5 m/s profile remains intact and unselected.

Existing moving steering, bounded flat adapted pivots, A jump, B cancellation, gravity, air steering/braking and landing recovery remain integrated. The continuation preserves launch momentum through 6 m/s. Side/back jumps use neutral source poses while retaining captured facing, selected launch impulse and planar velocity. Forward jumps retain the existing running sources. No input action IDs or serialized save formats changed.

## Source and native-ground findings

The old run strafe appeared diagonal because its qualification import extracted body-based root orientation. Baking the original orientation yielded a true lateral run; the selected left source and its Unity-imported mirror provide both directions. Mixamo supplied a sideways walk, neutral backward walk and backward run. Complete native takes are retained and repeated twice with horizontal RootT removed in separate derivatives. Slow cautious backward exports and an extra running export remain unselected. [Source provenance](source-provenance.json), [selected dependencies](selected-inputs.json) and [new private file hashes](new-private-files.json) identify the exact selection. Source/model payloads remain local.

The read-only author audit measured 1,681 collision samples in a 320 m square around spawn. Maximum sampled slope was 16.48 degrees. The previous uniform-plane gate accepted only 178 samples; median local plane residual was about 24.6 mm and maximum 178.9 mm. A failed pose could also reject an entire static terrain tile. [Native geometry](author-native-support.json) and its [summary](native-support-summary.json) retain those observations. Candidate terrain data hashes and world transforms match the author's nine loaded terrains.

The new schema separates source-family eligibility from individual pose adaptation. Static owned camp/AP04 support is eligible through 40 degrees; each foot samples its own centre and eight perimeter points, with the existing bounded leg reach and body lowering. Terrain-aware cloak correction retains the 350 mm garment cap. Grounded jump/landing body clearance uses the existing 126-point body cache, a 5 mm cache margin, the retained 180 mm visual limit and re-evaluated planted feet. A local unavailable foot pose retains the accepted source family and exposes a failure status instead of rejecting its tile. The 40-degree eligibility ceiling is not formal terrain qualification; measured native pose coverage below describes the tested region.

This is transient player motion on fixed, always-loaded authored terrain. World identity, generated-object identity, terrain generation, chunk streaming and saved runtime deltas do not change. Reset, disable, unload and ownership transfer clear directional state. Greater Wasteland and enabled build scenes are unchanged.

## Verification and limits

[Verification](verification.json) records 286/286 passing checks across 22 focused EditMode fixtures, with no failed, skipped or inconclusive cases. [Verified task code](verified-code.json) identifies 24 source files and their metadata. The final series additionally binds the [complete current script/test snapshot](complete-script-snapshot.json): 1,538 files paired exactly between author and candidate, including current camera/menu/resource dependencies. The candidate-only temporary resource event shim was moved outside Assets before this series. Source and selected-input hashes are checked before every fixture and again at completion. The legacy three-gait fixture pairs explicitly with its preserved schema-1 profiles. Earlier failed/intermediate series and the dependency-upgrade backup remain private and are not the final proof. These hashes describe the exact Windows worktree bytes; Git may normalize text line endings.

The native-ground fixture exercises 19,200 walking/running poses: five measured sites, four body facings, eight travel headings, five requested speeds and eight phases at one/two/four skinning influences. Every sampled pose retains the directional family and native adaptation; sampled soles and sparse full equipped-mesh checks meet the 2 mm penetration tolerance. A separate 840-pose grounded jump/landing sweep covers seven takeoff/landing sources, two native sites, four facings and five clip phases at those three influence qualities, including full equipped mesh checks. Flat directional full-mesh, clock/reversal, ownership, airborne, turn, reset and forward regressions also pass. These are direct pose/state checks, with no Play Mode or physics stepping.

[Contact observations](directional-contact-observations.json) retain 144 pure-source fixed-identity plants across six channels, three influence qualities and near/authored world origins. Only 60 meet the strict combined 20 mm excursion/travel and ground-gap aim. Maximum selected-point excursion/cumulative travel in millimetres:

| Source | Excursion | Cumulative travel |
| --- | ---: | ---: |
| Side walk left / right | 12.80 / 9.37 | 27.27 / 20.82 |
| Side run left / right | 14.83 / 14.91 | 47.32 / 43.14 |
| Back walk | 20.39 | 27.08 |
| Back run | 12.13 | 15.56 |

These observations do not qualify blended transitions, all terrain, collision-driven traversal or performance. The accepted forward sprint's earlier contact limits and the flat pivot's authored-origin cumulative-drift gap also remain. Both forward qualification flags stay false. This is ready for the author's requested hands-on review, with contact/feel acceptance still experimental.

## Installation and recovery

[Installation](author-installation.json) and [independent readback](author-readback.json) record selection in the existing author Editor. Only 31 explicitly listed new private payload/metadata files and two existing player-binding selection fields were installed after source/protected-hash checks. Targeted code import compiled successfully. Current console evidence is in [author status](author-console-status.json): no compilation failure or current errors, with 74 compiler warnings retained. The dirty CampLoopTesting scene was never saved, reloaded, restarted or brought forward. Its disk bytes, root objects and dirty state survived installation. No gameplay smoke test ran.

Private recovery is `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/CampDirectionalLocomotion20261015`. It retains the original 810-file baseline, the immediate pre-install binding/scene, exact final code/private assets, original downloaded exports, complete loop pose tapes, recipes, calibration proposal, raw receipts and a final file seal. [Recovery manifest](recovery.json) records the location, counts and seal hash. Earlier sealed strafe/combined checkpoints are immutable. Normalized public JSON copies retain raw-to-public hash provenance in [receipt transformations](receipt-transformations.json).

For author testing, start Camp Loop on its native ground: vary stick deflection while holding LT, circle through both forward and backward diagonals, change direction at speed, release/re-engage LT, then jump and land in each direction and across nearby grades. Exercise LB/menu interruption and reset before returning to movement. AP02/AP04 remain useful comparisons; the main native-ground selection is active. Hands-on testing belongs to the author.

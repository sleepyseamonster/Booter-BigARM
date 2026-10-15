# Corrected turn imports and bounded root-curve candidate

October 15, 2026. Continues the [focused gait plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md), [source contract](../TurnSources/README.md) and [complete contact tracks](../TurnContactTracks/README.md). The full goal remains active. These are isolated source/calibration implementations, not a player cutover or accepted turn controller.

## Import correction

The retained rapid-turn imports were configured as loops with loop-pose correction. The right mirror also had a **0.5 cycle offset**. That starts it halfway through the motion. Both imports baked planar root translation into the pose. The earlier stationary-root result described those import settings, rather than proving the underlying motion had no planar root trajectory.

[CampTurnNativeImport](../../../../../Tools/Diagnostics/CampTurnNativeImport.cs) creates two private candidate copies from the exact original FBX bytes:

- **OneShot:** both clips are nonlooping, loop-pose correction is off, and cycle offset is zero. Existing root extraction settings are retained.
- **Native:** the same correction, plus planar center-motion extraction. Vertical root motion remains baked into the pose; body motion is retained.

The [import receipt](imports.json) records separate intentional GUIDs and complete relevant settings. Both directions retain the native **1.50s**, approximately −180/+180-degree turn. The native import exposes **134.2mm** of net planar root travel. Original source and metadata hashes still match in the author checkout. These private variants exist only in the isolated candidate and have no player references.

## Contact and precision

The extended [source screen](../../../../../Tools/Diagnostics/CampTurnSourceScreen.cs) and [contact measurement](../../../../../Tools/Diagnostics/CampTurnContactQualification.cs) accept an explicit source/basis and can retain recorded planar root motion. Near-origin measurement avoids loss of precision when deriving motion derivatives from large world coordinates; the authored and measurement origins are explicit. This is calibration, not native-world collision proof.

The [one-shot source](one-shot-source.json), [native source](native-source.json), [one-shot contact](one-shot-contact.json) and [native near-origin contact](native-contact.json) show much closer left/right behavior after the phase correction. Clearance and equipment remain inside the sampled floor/height/lift envelope. Longer contact windows still fail; correcting import settings alone does not establish a planted foot.

## Bounded root-curve implementation

[camp_turn_root_curve.py](../../../../../Tools/Diagnostics/camp_turn_root_curve.py) constructs a C1 actor-root curve from one persistent near-floor sole point and the separately captured native root motion. It uses existing NumPy, with no new dependency. Native and adapted root motion remain distinguishable in the private inputs and fit recipe.

The declared engineering bounds are **1.4m/s**, **8.5m/s²**, **400mm maximum root offset**, zero initial/final velocity, unchanged native source time, and **19mm** fitted anchor error, leaving 1mm against the existing 20mm target. Internal margins tighten those limits; they do not expand them. Between nodes, velocity is linear and acceleration constant. Position extrema are checked analytically, not inferred from endpoints.

| Source | Maximum root offset | Peak speed | Peak acceleration | Replayed anchor excursion | Replayed cumulative travel |
| --- | ---: | ---: | ---: | ---: | ---: |
| Left | 399.01mm | 1.2805m/s | 8.4991m/s² | 13.97mm | 104.58mm |
| Right | 399.01mm | 1.2661m/s | 8.4991m/s² | 13.14mm | 103.20mm |

The [fit](bounded-root-fit.json) meets those independently measured curve bounds. The optimizer reached its iteration cap, so no optimality claim is made; feasibility follows from the actual serialized curve checks. [Math checks](math-checks.json) include a known interior extremum, a known moving C1 curve, serialized continuity, endpoint velocity and an independent dense-plus-Lipschitz radius bound. The known continuous moving-curve error is below 0.001mm; the larger linear-interpolation bound is recorded separately.

The [equipped replay](bounded-root-replay.json) evaluates the exported actor-root positions through the actual single-output rig at one/two/four influences. It reproduces the fitted point excursion. Both repeated fits and all six repeated replay cases agree. This establishes a useful bounded correction path, while approximately **10cm of cumulative foot travel remains unacceptable as a stable plant**. A targeted tangent foot constraint is still needed; do not enable this root curve alone.

The comparison using [Unity's built-in foot IK](builtin-foot-ik.json) worsens the native source's 132.6ms screen: coverage falls to about 68.7%, with worst available excursion about 119mm. That option remains off. It does not qualify a custom foot constraint.

## Verification and remaining work

The final diagnostic defaults reproduce all six preceding baseline cases and all 272 original measured inputs exactly. Across this checkpoint, seven cold equipped runs evaluate **7,644 poses**; two standalone source screens evaluate **4,056 poses**. These are deterministic EditMode measurements, not physics/gameplay simulation or author visual acceptance. The existing 304 playground checks are retained prior evidence; runtime code was not changed in this batch.

[Verification](verification.json) and the [private seal](private-receipt.json) retain exact code, import metadata, original payload references, complete tracks, fitted nodes and execution. Raw FBXs, pose coordinates and root curves remain local. Earlier seals are immutable. Final recipes reproduce the earlier scalar results even though later metadata/optional parameters were added during the diagnostic work.

Reproduction uses `CampTurnNativeImport.Run()` once in a fresh marked copy, source-screen runs with each explicit FBX, and contact runs with the corresponding basis. Root fitting uses `camp_turn_root_curve.py --project <candidate> --run TurnNativeContactOriginV1 --output <fresh-name>`. For an equipped replay, supply the fitted run as the contact tool's `rootCurveRun`; `footIK` remains false. Existing output guards preserve prior runs. The old stationary-source profile is still unqualified and is not repurposed to discard the newly captured planar motion.

Next: implement and qualify the bounded tangent foot constraint in the existing pose output, then carry an explicit native/adapted trajectory contract through the existing motor and test braking, interruption, collision, reset and action handoffs. Native terrain and final walking/running/sprinting acceptance remain open. The latest author main-thread query timed out, so fresh live playback after the [startup repair](../StartupLookup/README.md) is still unverified.

World identity uses existing authored scene/actor identities. Terrain/chunk streaming, generated IDs and persisted runtime deltas do not apply: only offline source copies and measurements are added. No terrain, production scene, author scene, player binding, package or runtime controller was changed. No agent entered Play Mode, advanced physics/gameplay or activated an application window.

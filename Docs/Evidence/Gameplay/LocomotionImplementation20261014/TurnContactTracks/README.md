# Complete turn tracks and fixed-pivot feasibility

October 14, 2026. Continues the [source contract](../TurnSources/README.md) and the [focused gait plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). The goal remains active; no turn controller or qualification flag is enabled here.

Inspection found that `JsonUtility` omitted the frame list from the dynamically compiled diagnostic's private track DTO. The earlier contact metrics were calculated from the complete in-memory list, but its saved track files contained only clip, quality and side metadata. The old immutable archives are preserved. [CampTurnContactQualification](../../../../../Tools/Diagnostics/CampTurnContactQualification.cs) now writes scalar DTOs through Newtonsoft and reads back every frame/point count. This fixes the saved evidence; it does not alter animation poses or contact budgets.

The rerun recorded both native 1.50s turns at one/two/four influences: **six tracks, 1,092 frames and 736,008 sole-point samples**, all finite. Every earlier numerical geometry/window result and all 272 measured data inputs match the preceding execution exactly. The [contact observation](contact-observation.json) adds private-file SHA-256 and completeness metadata. Raw coordinates remain local under the [private seal](private-receipt.json).

[camp_turn_contact_analysis.py](../../../../../Tools/Diagnostics/camp_turn_contact_analysis.py) validates those hashes, frame times, evaluated yaw, point counts and anatomical sides. It reports complete continuous near-floor episodes instead of replacing the contact point every frame. It distinguishes the preceding optimistic 8mm screen from a 1mm near-floor screen. Height alone still cannot establish physical support.

The analysis also tests one specific correction: translating the actor along an orbit about a single fixed pivot while preserving the measured native yaw. For each sole point that remains within −1/+1mm of the floor throughout the source, it solves the unconstrained least-squares pivot. The minimum RMS residual across those points lower-bounds the maximum residual of every fixed pivot in this model, including pivots constrained to the capsule.

| Four-influence source | Persistent near-floor points | Minimum RMS residual | Maximum drift at that fit |
| --- | ---: | ---: | ---: |
| RapidTurningLeft | 2 | 65.85mm | 108.72mm |
| RapidTurningRight_Mirror | 2 | 92.69mm | 184.84mm |

All six cases reject this correction against the existing **20mm** target. Two independent known 3D world-pivot cases recover the expected radius and half-turn root shift to numerical precision; see [math checks](math-checks.json). Repeated scalar analysis agrees exactly. [Verification](verification.json) records source hashes and these checks; [persistent contact](persistent-contact.json) retains the full scalar results.

This rejects a single fixed-pivot model for the tested persistent points. It does not reject every possible rolling contact, leg adaptation, contact-driven root trajectory or replacement source. The next implementation must address those distinctions and preserve collision, completed displacement, response timing and action handoffs. The active character assets contain only the already screened rapid-turn pair and retained NoRM jog pivots; those jog pivots do not provide an evaluated root-yaw trajectory. Original purchased package archives were not found in the checked Downloads, OneDrive, local tools, private recovery and repository transfer locations; retained Mac archive references do not establish Windows availability.

Reproduce in the marked isolated background candidate with `CampTurnContactQualification.Run("TurnContactTracksV2")`, then run the Python script with `--project <candidate> --run TurnContactTracksV2 --output <fresh-name>`. Existing output guards require fresh names. Use the existing Python/NumPy runtime; no package update was made.

World identity, terrain/chunk streaming, generated IDs and persisted deltas do not apply: these tools read authored assets and write offline measurements. They add no runtime owner, root writer, scene object or saved-world state. No agent entered Play Mode, advanced gameplay/physics, focused a window or saved the author scene. Fresh author playback after the [startup repair](../StartupLookup/README.md) remains pending.

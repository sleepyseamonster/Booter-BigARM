# Walking, running and sprinting audit evidence

October 14, 2026. Audit only. The [findings and revised priorities](../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_AUDIT_2026-10-14.md) supersede broader animation feature expansion for the next slice.

- [Live state](live-state.json) and [serialized tuning](live-tuning.json): read-only inspection of the author's loaded, clean CampLoopTesting scene outside Play Mode. An uninitialized runtime session and absent runtime Animator in this Edit Mode snapshot are expected; they are not bootstrap failures.
- [Graph summary](graph-summary.json): twelve actual clock evaluations and nine steady driver-mixer envelopes, each sampled at 241 semantic phases. Sole height uses native baked geometry; drift and handoff gaps use stable foot-bone positions.
- [Independent source summary](source-summary.json): three fresh single-clip graph instances sampled at 90Hz, totaling 443 finite frames. This separates source behavior from the driver's semantic remapping.
- [Curve periodicity](curve-periodicity.json): the sprint source has unequal stride halves. This is evidence to review its phase mapping, not proof of a corrupt derivative.
- [Focused checks](tests.json): all 18 player locomotion and three hydrated gait tests pass, without skips. Existing tests miss the measured blend/midpoint failures.
- [Input hashes](input-manifest.json), [cleanup read](candidate-cleanup.json) and [receipt](receipt.json): candidate/main gait code, private bindings and derived clips match. The offline actor was removed and Play Mode stayed off.

The reproducible [diagnostic source](../../../../Tools/Diagnostics/CampLocomotionAudit.cs) stays outside Assets. Run its entry points through the installed Pipeline run_script command in the marked background candidate. It refuses the author project/Play Mode and preserves existing output directories. It temporarily opens only the isolated saved-scene fixture and a preview scene; it never advances the motor, simulates physics, changes a clip/profile or saves a scene. The final Run entry produces ProbeV3; RunSourceAudit independently samples the three sources.

The full graph RPC exceeded the CLI's 30-second response limit, while the operation completed in the isolated editor. Completed output and a later read with zero remaining fixture objects/Play Mode false established cleanup. The independent source RPC completed successfully in approximately eight seconds. Earlier probe versions remain local and are not acceptance evidence.

Private full-frame timelines are retained locally under D:\BooterBigArmPrivateAssets\Mixamo\2026-10-13\LocomotionAudit20261014. Public results contain only hashes and compact measurements. No licensed FBX/derived motion payload was published.

Measurements use the accepted bare rig, the current paired gait assets and a flat reference plane. They do not qualify equipment, native terrain, collision response, frame pacing or the author's playtest appearance. The isolated saved-scene fixture predates concurrent scene/rendering edits; its gait inputs match the author data, but this audit does not validate those unrelated changes.

No runtime repair, private asset change, author scene/prefab edit, package change, foreground activation, Play Mode, gameplay smoke test or external publication was performed.

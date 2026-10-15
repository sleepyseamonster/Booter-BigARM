# Isolated turn foot constraint candidate

October 15, 2026. This continues the [native-turn calibration](../TurnNativeCalibration20261015/README.md) and the [focused locomotion plan](../../../../Design/Gameplay/CAMP_LOOP_LOCOMOTION_IMPLEMENTATION_PLAN_2026-10-14.md). It implements and tests a tangent foot constraint in the existing single animation output. It is **not installed in the author's Assets**, selected by the player, or qualified for motor/action handoffs. The [exact source patch](candidate-runtime.patch) preserves the tested runtime class, its tests and their intentional metadata without triggering a script reload in the author Editor. [Source hashes](sources.json) identify the candidate bytes.

The author Editor has an external-scene-change dialog over dirty CampLoopTesting. Read-only accessibility inspection established why main-thread queries timed out; it does not diagnose the reported static pose by itself. The dialog was left untouched, and no scene save/reload, foreground activation or Play Mode command was used. Fresh playback and F9 inspection remain the immediate author-side gate.

## Implementation and observations

`AnimationPlaygroundTangentFootPose` wraps one existing `AnimationPlayableOutput`. It accepts one sole-cache point entirely under the left or right foot subtree. It applies bounded leg rotations while retaining foot orientation, the ankle's vertical target and limb lengths. It owns neither animation time nor the physics root. The correction cap remains 20mm, reach failures remain unavailable, and failed post-solve checks restore the source rotations. Disable passes through; ordered disposal restores the prior output source and releases only this stage's native storage.

The first dense replay exposed an accumulation of small endpoint errors. Extra iterations did not fix it: a direct Unity check showed `Quaternion.FromToRotation` discarded corrections of 0.003–0.05 degrees, leaving up to 0.44mm of error on a 0.5m vector. The final solver uses cross/atan2 alignment to retain small angles. It converged in one iteration in all final dense samples; its iteration count remains bounded at eight.

The final [dense replay](TurnTangentDenseV3.json) evaluates **4,350 cold equipped poses**: both complete 1.50s sources, 725 timestamps each, and one/two/four bone influences. The source pose is evaluated between the original 120Hz yaw knots. Yaw uses quaternion interpolation of those knots, and translation evaluates the fitted C1 polynomial; this is an adapted trajectory replay, not a new dense measurement of native root motion.

| Observation | Final measured range or bound |
| --- | --- |
| Unavailable samples | 0 / 4,350 |
| Selected skin-point excursion from its initial anchor | at most 0.313mm |
| Selected skin-point cumulative planar travel | 11.33–11.62mm over the full source |
| Stream point residual | below 0.001mm |
| Maximum requested tangent correction | below 14.1mm; unchanged 20mm cap |
| Full equipped mesh minimum above the floor | within 0.001mm numerical error |
| Full equipped height | below 1.799m |
| Host floor lift | below 8.29mm |

The stream residual and evaluated skin-point travel are distinct measurements; contact observations use the latter. These finite samples do not establish continuous contact, performance under gameplay, collision response, terrain support or moving action handoffs. The [first stage replay](TurnTangentContactV1.json), [first dense replay](TurnTangentDenseV1.json) and [unsuccessful iteration refinement](TurnTangentDenseV2.json) retain the earlier results. No source/import, correction limit or qualification flag was changed to obtain the final numbers.

## Verification and recovery

[18/18 focused Edit Mode checks](tests.json) pass: seven alignment cases and eleven real-rig cases covering disabled pass-through, both legs at all three influence limits, finite/bounded configuration, rejected over-budget requests, unchanged physics position, retained leg lengths/foot rotation, disable/re-enable, nested disposal order, graph destruction and zero managed allocation during an active warm solve. Each supplied pose evaluates the existing graph once.

The [verification receipt](verification.json) also records 1,021 known-polynomial interpolation checks with zero float-output error. The final default diagnostic reproduced all 120 previously recorded row fields, six private track hashes and 272 input hashes from `TurnFinalToolControlV1`. Runtime and test assembly IDs pair with the final dense run. The diagnostic's optional subdivisions default to one, preserving its original route.

Raw tracks, fitted nodes, execution records and input dependencies remain in the immutable local `TurnTangentStabilization20261015` recovery bundle. Its [manifest](private-archive-manifest.json) lists 329 sealed files, 719,197,455 bytes and 274 input references, in addition to the bundle manifest itself. The [patch round-trip check](patch-roundtrip.json) verifies all four transported source/metadata files byte-for-byte, both tool sources and every listed archive payload. The public receipt contains scalar observations, hashes and code only.

The patch is a review/transport artifact. Apply it to the established Assets paths only after positively checking the author Editor's state and retaining its unsaved scene; then verify import and byte pairing. Keep the stage unbound until the explicit native/adapted trajectory contract, the existing motor's steering/collision/action handoffs and the remaining locomotion gates pass. The stationary `AnimationPlaygroundTurnProfile` remains unchanged.

World identity and persistence are unchanged. This stage creates no generated object, seed, terrain or streaming service. Its owner must disable/release it on reset or unload; tests cover the stage's disposal and graph-destruction behavior, not a completed session-owner integration. The laboratory remains an authored, always-loaded scene with its existing session-only checkpoint policy.

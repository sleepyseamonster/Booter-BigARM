# Camp Loop weightier jump tuning — October 16, 2026

The author reported excessive float and hang time after the [jump repair](../JumpImplementation20261015/README.md). This follow-up prepares a lower, faster arc using **1.6× gravity during launched jump flight**, retaining the 4.6 m/s impulse. The tuning is verified and **installed in the main CampLoopTesting scene for author retest**.

## Behavior

The ideal same-height airtime changes from 0.938 to **0.586 seconds**, a **37.5% reduction**. Ideal peak height changes from 1.078 to **0.674 metres**. These are ballistic calculations; real contact and rendered body motion remain author-tested. Keeping the impulse preserves the ideal same-height landing speed rather than raising it toward hard recovery.

Only the existing jump Rigidbody owner adds the excess 0.6× acceleration. It starts on the impulse step, runs once per physics timestamp, and pauses with jump simulation. Grounded anticipation, ordinary run-out, passive ledge falls and other actions retain world gravity. A jump that becomes a long fall retains its gravity until touchdown; a new departure after landing starts an ordinary fall. Horizontal steering/momentum, accepted forward/directional gait data, LT behavior, input grace, the global gravity setting and damage thresholds are unchanged. Falling onto a lower surface still increases actual contact speed; check real uneven/drop landings during author testing.

Animation source timing and both landing forecasts now use the motor's effective jump gravity. Complete source clips, GUIDs, terrain/equipment correction and moving landing blends are retained. Missing calibration fields default to 1× for older private profiles; the current Camp Loop profile explicitly stores `gravityScale: 1.6`, constrained to 1–3×.

This modifies transient actor motion on fixed, always-loaded terrain. World identity, streaming/unload, stable generated-object identity and saved deltas are not applicable because none is created or changed. Existing authored collision/slope constraints remain in force. Greater Wasteland production promotion is outside this study follow-up.

## Proof and recovery

[Evidence](checks-and-install.json) records **110 passing focused EditMode checks**: motor 19, airborne 36, timing 14, equipped presentation 15 and low-motion regression 26. New checks cover owner/parameter rejection, mass-independent gravity force, first-impulse/once-per-timestamp application, pause, new-lease defaults and passive-fall isolation. Physics is not stepped. The source clock is checked at the new shorter half-flight as well as the previous one.

The equipped analytic model covers 30/60/120 Hz, one/two/four-bone qualities, two native camp sites and five source/movement scenarios. Its **5,850 poses** passed the existing sampled clearance and one-output contracts, with a minimum measured mesh gap of about +0.89 mm. The analytic root is supplied; this does not prove real trajectories or accepted feel. No gameplay smoke, Play Mode or automated input ran.

Immutable initial files, the prepared private profile pair, full test responses and the guarded install command are under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/JumpWeightTuning20261016/`. Restore `before/.../AirborneProfile.asset` with a targeted stable-Edit-Mode import to return to 1× gravity; preserve the paired metadata. Do not restore or save the author's scene, remove animation pairs, reset Git, or overwrite unrelated work.

Unity displayed a low-disk modal during the author code reload. The new calibration was not written while that dialog blocked the Editor. The owned validation Editor was closed after qualification. Its retained 4.235 GB log was relocated intact to `C:/BooterBigArmValidationRecovery/2026-10-16/Logs/forward-review-hydration-editor.log`, with SHA-256 verification, restoring about 5 GB of D: headroom. The author dismissed the modal, then the guarded single-profile install succeeded. All 158 protected files, the profile GUID, eight existing scene root IDs and the dirty scene state remained unchanged. No source art or production assets are part of that relocation.

Retest stationary and running jumps in the main CampLoopTesting area, followed by LT side/back/diagonal jumps, moving landings and small drops. This pass deliberately lowers jump height along with airtime; final feel remains the author's decision.

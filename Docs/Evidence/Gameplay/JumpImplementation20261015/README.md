# Camp Loop jump repair implementation — October 15, 2026

The [audited plan](../../../Design/Gameplay/CAMP_LOOP_JUMP_REPAIR_PLAN_2026-10-15.md), steps 1–5, is implemented and installed for **author testing in the main native-ground CampLoopTesting area**. Greater Wasteland remains the enabled production scene; this study repair does not promote the laboratory session into production. Gameplay acceptance is still plan step 6.

The author's October 16 floatiness feedback is addressed by the [weightier jump follow-up](../JumpWeightTuning20261016/README.md). That follow-up supersedes this receipt's original 1× jump-gravity calibration; the measurements below remain the October 15 baseline.

## Result

Jump acquisition, rejection and ordinary release preserve horizontal momentum. The existing single Rigidbody owner runs normal grounded movement during the short anticipation and ordinary landing run-out. Flight retains gravity and physical collision response, with bounded steering and recent wall-normal rejection. Non-jump actions retain their prior stop policy. Held LT retains facing lock and camera look-ahead through normal jump/landing; menu/device/reset neutral gates still disarm it.

Takeoff targets 80 ms, quantized to physics ticks, with a provisional maximum 100 ms normal request-to-impulse budget. Buffer and coyote windows are 100 ms. Jumps have fixed height using the existing 4.6 m/s impulse and gravity; the existing continuation permits up to 6 m/s planar air speed. Visual entry blends for 120 ms without delaying the impulse. Ordinary absorption fades into the live accepted gait over 220 ms, followed by one fixed-step interpolation allowance. Neutral input uses normal deceleration. Hard recovery remains separate. Palette preview selection no longer chooses normal jump/landing behavior or forces a roll.

Completed walkable collision contacts supply touchdown IDs, support and contact-associated relative vertical impact speed. Prediction only prepares the pose; it cannot declare contact, damage or landing effects. Sustained support cannot repeat a touchdown, and another actual departure rearms it. The contacting rendered sole selects the foot effect and gait phase. Existing damage threshold/multiplier remain unchanged. Actual solver/contact timing still needs the author's trajectory checks.

Three private continuous clips reuse the complete existing Standing, Running and Unarmed sources. One shared root-height correction removes the measured flight arc while retaining compression and absorption. A monotonic source clock spans rise and descent; normal apex transitions keep the same clip. Sustained falls use the existing fall loop. Render interpolation, advancing transition snapshots, live locomotion underlay and one final terrain/equipment correction share the existing Animator/output.

Cached support correction fades away at lift. A presentation-only rounded-capsule forecast fades it back toward landing. No actor-relative artificial floor is applied during free flight. The final review also added cleanup when a disabled motor stops sending fixed-step callbacks; it cannot leave jump presentation owning the actor indefinitely.

## Verification and limits

[Command and pose evidence](command-and-pose-tests.json) records **162 unique passing focused EditMode checks**. Affected checks were repeated after the entry-blend and lifecycle refinements. They comprise airborne 32, motor 16, timing 11, gait clock 4, low-motion 26, input 5, jump presentation 15, directional 13, forward locomotion 14, terrain pose 19 and native ground 7. These are command/state checks, synthetic completed-contact injection and offline graph/geometry measurements. They are not physical trajectories.

The equipped jump model covers stationary, forward running, LT lateral/backward and the additional Unarmed family, at two existing main-camp terrain sites, all one/two/four-bone qualities, and 30/60/120 Hz rendering with the current approximately 50 Hz fixed step. It supplies an analytic vertical root and pose history; it does not simulate movement or horizontal travel. Its 8,040 rendered poses retained one output and passed sampled equipped-mesh terrain clearance; the smallest measured gap was approximately +0.89 mm. Same-source-time lift and predicted/actual contact switches passed the 2 mm joint-discontinuity limit across all three source families and qualities.

Native-ground regressions separately retained the accepted family, directional selection and terrain adaptation across 19,200 offline poses over five existing sites, four facings, eight headings and five requested speeds, with the existing sole/mesh sampling checks. No accepted gait source, speed band or calibration asset was reauthored.

The measured worst running-entry hand speed fell from approximately 28.39 to 21.67 m/s at 120 Hz after extending visual entry from 60 to 120 ms. This is a diagnostic maximum, **not a visual-quality acceptance threshold**. The fast arm motion, in-air steering, uneven/early contacts, ceiling interruptions, long falls and severe recovery still need author observation. Additional Mixamo motion remains conditional on an observed source gap. Zero pose displacement and zero natural joint velocity are not interchangeable claims.

The first whole native-ground request exceeded the CLI's 55-second response deadline even though its seven checks completed successfully. The final asynchronous receipt records all seven passing in 62.12 seconds. Failed draft measurements and transport/time-out receipts remain private; limits were not relaxed to conceal them.

No Play Mode, physics stepping, automated player input or gameplay smoke test ran. No package, project setting, terrain generation/streaming, stable world/object identity or saved-runtime-delta change was needed: this is transient actor movement on fixed, always-loaded terrain.

## Installation and recovery

[Installation evidence](private-asset-installation.json) records the exact eight private files: three new animation/meta pairs and the existing AirborneProfile/meta pair. The profile remains GUID `3798b4f7c60dc644ca52844228053fc3`, now schema 2. Its readiness flag enables author testing against the new command/offline evidence; it does not assert author gameplay acceptance. Legacy sources remain available for previews and severe/long-fall behavior.

Private preparation ran only in the marked, non-Git, hidden batchmode candidate at `D:/BooterBigArmValidation/AnimationPlayground20261013` (owned PID 12724), closed after verification. The author Editor PID 9748 remains open. Immutable recovery and drafts are retained under `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/JumpRepairImplementation20261015/`. `before/` and `before-manifest.json` retain initial task inputs; `continuous-source-V1/` retains the unqualified draft and recipe. `qualified-source-v1/` contains the sealed payload, previous profile pair, exact file/code/protected-input hashes and install manifests. Retain all earlier revisions.

The guarded author install checked exact old/new hashes, GUID ownership, loaded APIs, profile cleanliness, main-loop binding, 152 protected files and existing scene roots. It imported only the whitelisted private payload. The author Editor PID 9748 remained in Edit Mode; the dirty CampLoopTesting scene and all eight existing root IDs were preserved. No scene save/reload or foreground activation occurred. Private animation payloads remain local and ignored by Git. Installation receipts include the final subsequent lifecycle-code import and paired source hashes.

If recovery is needed, restore the previous AirborneProfile asset/meta pair from `qualified-source-v1/previous/` in stable Edit Mode with the same guarded targeted import. Leave new animation pairs retained and unbound; do not delete metadata or restore/save the author's scene. Old schema 1 intentionally leaves the repaired jump unavailable until a valid continuous profile is restored. Source recovery is not authorization for a Git reset or to overwrite unrelated work.

The first implementation foundation was committed as `2f8ed11a6` before controller binding; its [45-check receipt](batch01-command-tests.json) remains historical evidence. Later implementation/evidence supersede that batch's unbound status. Unrelated scene, art, lighting and inventory work remains outside the jump commit.

## Author test checklist

Use the open **CampLoopTesting main camp on native ground**, starting near the normal spawn, and press the existing **A jump** control. Test platforms are optional diagnostics.

1. Jump stationary, then while walking, jogging, running and sprinting. Hold movement through landing, release it in the air, and reverse it. Check prompt lift and continued horizontal travel/run-out.
2. Hold LT and jump left/right, backward and diagonally. Check facing and camera look-ahead remain held through landing without releasing/repressing LT.
3. Try near-contact presses for consecutive jumps and a press just after leaving a ledge. Check one jump per press and no queued jump after opening a menu, disconnecting the pad or Reset.
4. Try uphill/downhill/sidehill, an uneven/early landing, a wall glance, a low ceiling and a safe longer drop. Check support, absorption, contact effects and appropriate hard recovery; inspect the running-entry arm blend especially.
5. Observe at available render rates and pause/resume/world-speed settings. Record any visible snap, foot/garment penetration, lost momentum or duplicate impact with its location and movement direction.

These observations complete the remaining author acceptance step. The existing unrelated UI cancellation/destroyed-driver console issues from the audit are not claimed repaired by this change.

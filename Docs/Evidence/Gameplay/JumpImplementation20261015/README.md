# Camp Loop jump repair implementation — October 15, 2026

Implements the [audited repair plan](../../../Design/Gameplay/CAMP_LOOP_JUMP_REPAIR_PLAN_2026-10-15.md). Work is in progress; the result is not yet ready for author testing.

## Batch 1 — motor and ownership boundary

Added explicit `JumpLocomotion` policy to the existing motor/coordinator. It is **not yet bound by the airborne controller**. Existing actions retain the default policy and their stop semantics.

The new policy preserves planar velocity at acquisition/release, preflights motor ownership before acquiring presentation, and rejects foreign presentation without touching the body. Its grounded branch uses the existing normal locomotion solver; its flight branch uses the continuous Rigidbody and removes the generic action's horizontal support-blocking sweep. Contact normals prevent air steering from commanding motion into a recent wall without creating upward velocity along that wall.

Collision callbacks only collect completed support information. Commands and completed velocity are separate. Distinct support departures/touchdowns receive separate IDs; sustained support cannot emit a new ID. Impact severity retains the vertical incoming collision-speed metric on the fixed terrain. Accepted grounded gait sources, speed data, input and scene content were not edited.

The first focused run exposed a missed rearm on a one-observation support loss; it was corrected before the passing receipt. The rejected initial run remains in the private recovery checkpoint. [Batch 1 proof](batch01-command-tests.json) records **45 passing focused EditMode checks**: 15 motor tests and 30 airborne/coordinator tests, with exact paired author/candidate source hashes and loaded candidate module. New contact-edge checks inject completed observations; legacy airborne fixtures manually supply states. These results establish command/ownership contracts and regression containment, not physical trajectories or jump feel. No Play Mode, physics stepping or gameplay smoke test ran.

The task-owned candidate is `D:/BooterBigArmValidation/AnimationPlayground20261013`, launched hidden in batchmode as PID 12724 after confirming the author Editor was the only running instance. The stale lock file left by the earlier candidate shutdown was not proof of a live owner. The author Editor PID 9748 remained in dirty CampLoopTesting Edit Mode. No author scene save/reload or foreground activation was requested.

Original inputs and intermediate receipts are retained under the fresh private checkpoint `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-13/ImportCheckpoints/JumpRepairImplementation20261015/`. Do not overwrite earlier checkpoints. Private profiles/animations remain unchanged in this batch.

## Remaining batches

Bind the jump policy with contact-driven run-out and input grace; author and qualify the continuous source family and render transitions; install only verified private pairs without saving the author's dirty scene; then finish focused regressions and prepare main native-ground Camp Loop testing instructions. Author-owned actual trajectories and feel acceptance remain separate from readiness for testing. Greater Wasteland production promotion is outside this study repair's installation boundary.

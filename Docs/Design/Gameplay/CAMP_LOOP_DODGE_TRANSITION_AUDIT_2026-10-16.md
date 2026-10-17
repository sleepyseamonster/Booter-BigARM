# Dodge transition audit — October 16, 2026

**Correction installed:** the [transition-fix receipt](../../Evidence/Gameplay/DodgeTransitionFix20261016/README.md) records the LT/support lease, world-facing blend and moving-entry braking fixes, 222 passing focused checks and installation in the open main camp slice. The audit below preserves the pre-fix findings; author hands-on feel acceptance remains pending.

**Result: two confirmed recovery defects. The dodge is installed, but its running/sprinting handoff is not ready to be called smooth.** This is an audit; runtime behavior and accepted tuning were preserved. The author-owned CampLoopTesting scene remains open and unsaved.

## P1 — recovery drops held LT facing lock

The real session passes `presentation.StandingStrafeReviewSupportAvailable` to the motor ([session wiring](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs), line 101). That callback rejects any owned pose blend ([availability gate](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.StandingStrafe.cs), lines 44–46). Dodge deliberately owns a recovery pose blend after it releases its movement lease. The next ordinary motor tick therefore executes `ResetStandingStrafe(held)` and requires LT to be released before lock can return ([motor](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.StandingStrafe.cs), lines 57–68).

Reproduction used the actual session callback, native camp support and actual motor/driver callbacks, with a validated ready baseline. All six held-LT cases (4.9/6.15 m/s, forward/right/backward dodge) retained lock through tick 48 and lost it at tick 49, 0.98 s into the command sequence. LT and look-ahead remained held. Lock was still absent at tick 65, after recovery. This changes controls on return to movement, including normal strafing/facing behavior.

The earlier controller test substitutes `() => true` for support and ends its LT assertion at `Tick(48)`. The pose tests drive snapshots directly. Both remain useful contracts, but neither exercises this first normal recovery tick with production wiring.

## P2 — side/back recovery resets facing before blending

[Dodge completion](../../../Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundDodgeController.cs), lines 77–81, captures the roll pose and immediately calls `RestoreDodgeFacing`. [That method](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.Dodge.cs), lines 32–36, calls `FacePlanarDirection`, which directly assigns Rigidbody rotation. The pose snapshot stores clip contributions and height, not the actor's world yaw.

With LT held, the measured motor-facing change on release is **90° for side rolls and 180° for backward rolls**, at both fast-run and sprint entry speeds. A 160 ms local pose fade cannot preserve the snapshot's former world orientation across that host rotation. This is an instantaneous facing-command discontinuity; its exact appearance with physics interpolation remains author-owned Play Mode validation. Forward rolls show no yaw reset because both headings agree.

Fix orientation ownership together with the LT lease. Retaining lock alone will not make the existing local snapshot preserve its world-facing pose. Also cover recovery's directional target: `PrepareForwardGraphUpdate` currently resets directional presentation whenever a pose blend is owned, while dodge recovery attempts to compose a directional underlay.

## Running/sprinting tuning and additional constraints

On the 6.27° native spawn support, forward entry slows from 4.9 or 6.15 m/s to approximately **1.68 m/s by 80 ms**. The first-tick momentum assertion passes, but it does not cover the subsequent braking. The actual entry curve reaches roughly 1.686 m/s on a flat mathematical support. This is a measured tuning concern, rather than a declared failure to preserve the normal forward gait family. Set a deliberate dodge-entry deceleration target before changing it; avoid hiding it with faster normal running or a gait retime.

At release, the forward velocity command returns to the requested tangent speed (observed planar values about 4.871/6.113 m/s on this grade), without a forced zero. This is command/state proof with a fixed actor position, not a physical traversal measurement.

A secondary source audit found that `DodgeRunOutVelocity` duplicates only part of the ordinary movement request: it omits the existing uphill gait limit, and recovery presentation uses planar velocity while normal gaits use completed tangent displacement. The five retained native sites measure 6.27–16.48°, below the uphill limiter's 32° onset, so no steep-slope failure is claimed at those sites. A synthetic 40° policy comparison yields 1.4 m/s for ordinary uphill movement while dodge still requests its uncapped 4.9/6.15 m/s. Include that boundary when aligning the recovery target with normal movement.

A smaller status-text mismatch remains in `TopDown3DPlayerAnimationDriver.ForwardReview.cs`, lines 26–27: it still advertises “B cancel,” although East now requests dodge. Correct the status label alongside the transition fixes.

## Accepted forward gaits remain intact

Live author readback resolves the required qualified dodge and native Camp Loop motion review; dependencies pass. Accepted moving speeds remain **1.4 / 3 / 4 / 4.9 / 6.15 m/s**. Fast-run and sprint cycle distances remain approximately **5.4951744 / 5.483365 m**. Jump gravity stays **1.6×**. All 46 runtime/editor/test source-and-metadata seals, eight installed private-file seals, PlayerBindings and the preceding 172 preservation seals still match. Nothing in the accepted family was retuned by this audit.

**27 focused existing regression checks passed:** Dodge 6, Expanded Gaits 8 and Directional Movement 13. Eight additional diagnostic cases completed. Passing those existing checks does not clear the newly reproduced defects.

The [repeatable diagnostic](../../../Tools/Diagnostics/CampDodgeTransitionAudit.cs) runs through CLI `eval` only in the marked background candidate. It uses virtual Input System state, a fixed actor position, support/collision queries and motor/driver callbacks. No Play Mode, physics stepping, actor trajectory simulation, source reauthoring or foreground activation occurred. Gait-phase advancement and rendered temporal continuity under real physics are outside this fixed-position diagnostic; its phase values must not be presented as a measured real-traversal cadence fault.

[Measurements and live readback](../../Evidence/Gameplay/DodgeTransitionAudit20261016/audit.json), [regression results](../../Evidence/Gameplay/DodgeTransitionAudit20261016/tests.json) and [preservation seals](../../Evidence/Gameplay/DodgeTransitionAudit20261016/preservation.json) preserve the evidence. Full raw diagnostics and the invalid pre-support-prime trial remain in `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-16/ImportCheckpoints/DodgeTransitionAudit20261016/`. The retained first trial did not establish a ready baseline and was excluded from findings.

## Required correction and acceptance gates

1. Add a failing recovery contract with the real support callback. Hold LT through at least 65 ticks, in all eight directions, at fast-run and sprint speeds. Require lock retention and the correct recovery directional target without bypassing the native-support guards.
2. Give the roll's world-facing orientation an explicit entry/recovery handoff. Preserve or compensate snapshot world yaw before restoring locked facing; measure pose continuity across the boundary rather than asserting only the final heading.
3. Resolve the return velocity through the same accepted ordinary speed, directional and slope policy. Preserve the existing gait assets, cadence maps and normal movement settings. Cover neutral input, changed direction, sprint release, cancellation, pause and repeated rolls.
4. Measure the full entry/recovery window and completed gait phase with explicit valid motion observations, including differing render/fixed timing. Retain the existing source/mesh/garment bounds and one animation output on native camp terrain. Repeat the affected regression gates after corrections.
5. Have the author test fast-run/sprint → dodge → held movement in the main camp, especially side/back with LT. New Mixamo assets are not required to address the two confirmed ownership/handoff defects; a different running-entry source is a separate option if the measured braking remains undesirable after timing review.

# Camp Loop moving-turn arcs — implementation plan

The author requests walking, jogging, running and sprinting turn arcs, with all suitable Mixamo sources acquired and combined correctly in the main Camp Loop. Preserve the accepted straight family: 1.4 / 3 / 4 / 4.9 / 6.15 m/s, its exact clips, contact maps, stride distances and ordinary motor speeds. Five actual blend bands need coverage even where the catalog uses four gait labels.

## Scope and source gates

Audit the live signed-in Mixamo catalog for neutral unarmed left/right arc cycles and one-shot moving turns. Record title, description, export settings, file hashes and any missing gait coverage. Retain originals locally. Weapon, drunk, sad/happy and catwalk results are not interchangeable with neutral locomotion. Source reuse or intentional mirroring must be explicit and pass playback/contact/pose qualification; do not claim a catalog gait that was not found.

Prepare and validate in the marked background candidate. Install only in stable author Edit Mode. No author scene save/reload, automatic Play Mode testing, foreground activation, package change or unrelated asset changes.

The catalog audit selected **Walking Left Turn**, **Running Right Turn** and **Sprint Turn**, exported without skin at 60 fps with no keyframe reduction. Walking uses the walking source; jog/run share the neutral running source; fast run/sprint share the sprint source. Opposite directions are intentionally mirrored and phase aligned. The audited searches did not expose a separate neutral jogging turn.

## Implementation order

1. Acquire and inspect suitable sources. Measure complete cycles, evaluated root travel/yaw, foot contacts and equipped loop closure. Extract source travel/yaw so the existing Rigidbody remains the sole motion/facing owner.
2. Create a separate optional paired moving-turn profile with left/right contributors for the five accepted bands. Do not change the accepted forward profile or serialized role order. Reject incomplete required data visibly.
3. Observe signed curvature from successive valid completed-motion tangents, projected onto a common support plane. Use tick/epoch guards, speed thresholds, smoothing and release hysteresis. Camera yaw and LT strafing must not generate false turns.
4. Apply phase-aligned source torso/arm rotations after the accepted supported pose. Full-body arc mixer trials changed retargeted hips/feet and were rejected. The final route keeps the existing six-input, single-output gait graph, root, hips, legs, source timestamps and native-ground correction. Blend only an explicit upper-bone allowlist, using accepted gait weights and the completed-distance clock. Transfer measured source lean into spine/chest during offline authoring.
5. Preserve stationary pivots and action priority. Disable live moving-turn demand during LT facing lock, jump/airborne ownership, dodge, crouch, mounting, traversal and invalid support. Capture the displayed upper-body rotation delta with existing action pose snapshots and fade it using their blend clock. Accepted gait sources continue advancing through the unchanged snapshot route; no separate jump source-rate hook is needed. Clear residual pose on disable, unbind, unsupported/foreign ownership and hard rebase; preserve it across a captured action handoff. Ease ordinary supported release over 80 ms.
6. Qualify source rates, loop closure, straight/arc and left/right transitions, phase/render interpolation, reset/rebase/duplicate observations, camera-only changes, LT, jump and dodge handoffs. Run equipped source/mesh/sole/garment checks at existing native camp sites and one/two/four bone influences. Compare identical virtual curved paths with and without the upper-body layer to establish added foot drift and clearance change. Retain the existing grounding bounds and one animation output. Do not treat accepted baseline contact-window limits as newly solved foot planting.
7. Seal exact source/private/protected hashes, run affected focused EditMode regressions, import only verified task files/private pairs into the already open main Camp Loop in stable Edit Mode, verify loaded controls/dependencies/gaits, commit task-owned files on main, then hand over author Play Mode feel testing.

## World and persistence boundaries

This changes transient actor presentation on fixed, always-loaded authored terrain. It creates no deterministic world identity, generated object identity, chunks, terrain streaming or persisted runtime deltas. Existing terrain constraints, collision ownership and reset/scene-unload cleanup remain authoritative.

## Evidence and recovery

Checkpoint: `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-16/ImportCheckpoints/MovingTurnArcs20261016/`. Initial main revision: `183188b0bc3d84c4586b148e0dd0b1466c7d0119`. The audit identified completed-motion observation, forward pose composition, action snapshots and native-ground support as coupled seams. Retain rejected full-body trials separately from final upper-body results. Source exports, derived clips and the paired profile remain private/local under the existing ignore rules. Candidate development precedes author code imports. Record completed proof in [the implementation receipt](../../Evidence/Gameplay/MovingTurnArcs20261016/README.md); hands-on traversal and feel remain author-owned.

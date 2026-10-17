# Camp Loop dodge transition fixes — October 16, 2026

Installed in the already open **CampLoopTesting main camp slice**, compiled and ready for author hands-on testing. The author scene remains open, dirty and unsaved with the same root objects. Work stayed on `main`; no foreground activation, scene save/reload, Play Mode, package change or external publication occurred.

## Implemented behavior

Held LT remains eligible throughout the recovery blend, using the real native-support callback. Both movement and action release retain the current grounded ray for the next completed-motion observation. Disabled, moved, dynamic or unavailable support remains ineligible; ordinary input disarming still applies.

The Rigidbody keeps its entry facing through the roll. The visual host eases toward the chosen roll heading during the 80 ms entry, captures that world heading with the exit snapshot, then blends toward ordinary facing during the existing 160 ms recovery. Side/back rolls no longer rotate the actor under a local snapshot on release. The terrain footprint follows the visual heading.

Moving entry deceleration is bounded at 8 m/s² before collision constraints. The last 20% still resolves toward current held movement. Run-out now uses the existing ordinary speed, directional cap and uphill policy. Neutral input returns to ordinary braking. The moving roll can travel farther than the source's nominal 2.4 m; actual displacement depends on input, terrain and collisions.

Recovery composes the current forward/directional gait under the snapshot, retains directional weights and reads the existing completed-motion clock with the same render interpolation as normal movement. It creates no new animation clock, root-motion writer or graph output. The status label now advertises B dodge and Backspace cancel.

## Verification

**222/222 unique focused EditMode checks passed** against the final source freeze. [Case results](tests.json) include 34 new transition checks and the affected dodge, input, motor, jump, gait, directional, crouch, low-motion, reset and native-ground suites. The new tests exercise all eight headings at fast-run/sprint entry speeds with the production support callback; absent/released LT, changed/neutral input, sprint release, pause/resume, repeated/cancelled rolls, invalid support, the 40° uphill policy, and render interpolation at 0 / 0.5 / 1.

The new equipped-pose matrix covers **8,640 entry/recovery poses** across five native sites, eight headings, both entry speeds, two gait phases and one/two/four bone weights. Existing dodge poses and accepted native gait regressions also pass. Same-time side/back recovery keeps the sampled equipped joints within 2 mm. The graph retains one output, and mesh/garment bounds remain unchanged.

[Ten command measurements](measurements.json) show no lost held-LT lease and 0° actor/visual heading discontinuity on release. After 80 ms, fast-run retains approximately **4.26 m/s** and sprint **5.51 m/s**, rather than the earlier approximately 1.68 m/s. Maximum measured entry deceleration is 8 m/s² within floating-point tolerance. The scalar command budget over the discretized roll stage is approximately 3.54–4.07 m in these cases; this is not a measured physical path length.

[Fresh input equivalence](candidate-input-contract.json) matches 18 native terrain/collider records, nine TerrainData/meta pairs and 31 coupled runtime sources. Both Editors independently match all nine live terrain world matrices with clean TerrainData. The candidate retains earlier lab/lighting/hierarchy content; tests construct their own actor/support fixtures. Greater Wasteland promotion is outside this slice.

[Author readback](readbacks.json) confirms the new runtime API is loaded, dependencies and the required dodge validate, East/View bindings resolve, and accepted speeds/cadences and 1.6× / 4.6 m/s jump tuning remain intact. Compilation failure is false and the current console error count is zero; existing warning/history counts were retained. [Preservation seals](preservation.json) match all 181 protected inputs and the tested/installed source. Only the owned hidden validation Editor was closed.

Actual traversal feel and collision response under physics remain for the author's Play Mode test. Test **fast-run or RB sprint → East → held movement**, especially LT + side/back/diagonal dodge, then release LT and repeat. No automated gameplay smoke test was run.

## Preservation and proof boundary

The accepted forward gait assets, private animation/binding files, speeds **1.4 / 3 / 4 / 4.9 / 6.15 m/s**, cadence maps and jump tuning are unchanged. No new Mixamo source is required. The existing 180 mm action correction bound, −2 mm mesh threshold and garment limits are retained.

The new transition tests use virtual Input System state, actual motor/driver callbacks and explicit completed-motion unit observations while holding the actor fixed. A separate first-release test publishes a distinct completion timestamp using the real retained support ray without substituting support. No Play Mode or physics stepping is performed. The offline equipped-pose matrix checks geometry, not physical traversal feel.

This transient actor fix creates no deterministic world identity, chunks, generated objects or persisted runtime deltas. It queries the existing fixed, always-loaded authored terrain and respects current collision constraints.

## Recovery

The private checkpoint is `D:/BooterBigArmPrivateAssets/Mixamo/2026-10-16/ImportCheckpoints/DodgeTransitionFix20261016/`. Its immutable `before/` source copies, final source seal, protected-input manifest, failed development trials and raw CLI results are retained. A rollback should restore the task's runtime partials together; no private asset or scene rollback is needed for this code-only correction. Preserve metadata and unrelated author changes.

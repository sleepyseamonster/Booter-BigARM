# Greater Wasteland input and interaction profile — October 12, 2026

The author requested background-only profiling and implementation after the input/control remediation passes. This slice fixes missing startup cargo ownership and reduces measured recurring query/UI work. Tap/hold timing, input arbitration, scan cadence, target eligibility, cargo range and physical obstruction rules are preserved.

## Method and boundaries

Unity 6000.4.0f1 ran headless in `C:/BooterBigArmValidation/InputInteraction20261012-01`; production Unity stayed in the background. A dependency-selected copy sealed 57,455 live files, 2,949,306,429 bytes, preserving metadata and production terrain dependencies. The scene SHA-256 is `BC0B465DE9A1B75B82A20D99C25FA4F10F00377A18F9564F9E8B0516A4C8F93E`. Each capture uses a separate candidate-only save/preferences profile. Source scene, project settings, packages and production saves were unchanged.

The scene contains 9,216 Terrain components, 9,216 TerrainCollider components and twelve authored ironstone nodes. Ten seconds of settling precede two repetitions of thirteen warmed synchronous probes. Query samples batch sixteen calls; UI transitions use individual operations. Results are averages of two repeated medians/P95s of batched CPU averages, in microseconds per operation. They do not establish frame timing, rendered FPS, physical-device latency or controller feel.

The existing physical Legger call-over command establishes reachable cargo without teleporting actors or changing access range. Temporary fixtures at the actual motor produce 67 overlap hits. A separate clear origin near real cargo exercises 64 ignored actor-child ray hits per repetition and a wall behind them. Fixtures are validated outside measurements and removed before another simulation step. The stable HUD probe uses the current target or an authored ironstone fallback.

Allocation events use stopped current-thread `GC.Alloc` sample **Count**, with allocating/nonallocating controls of 64/0. The CLR thread-byte counter failed calibration; no byte claim is made. Owned processes are guarded by 12 GiB minimum free physical RAM, 20 GiB maximum private/working memory and a bounded deadline. No other app/process was closed.

## Implemented changes

- The action controller rebuilds inactive gather/destination context in `Start`, after scene roots are discoverable. Inspection previously found reachable inventory cargo but no action cargo reference, Gameplay mode and no busy/placement action. This blocked hold-to-open and cargo-backed actions. Explicit same-scene ownership and an already-active pre-Start action are preserved.
- Cargo LOS reuses a thread-local hit buffer; full buffers grow and retry before deciding whether a blocker exists.
- Overlap scans retain grown capacity instead of repeatedly allocating a full fallback. Complete selection, facing, distance, stable-ID ordering, hysteresis, availability, range and LOS remain intact. Canister-only negative hits avoid a duplicate component lookup.
- Slots reuse live component references; cold initialization/reconstruction reconnects authored children and preserves button navigation/listeners.
- The HUD checks target text/availability, cargo access, hold percentage and feedback expiry freshly, while reusing unchanged presentation. Mode/binding/device changes, re-enable, reconfiguration and destroyed text components invalidate the cache. Busy/placement prompts remain dynamic.

These are transient startup/query/presentation changes. Fixed terrain remains always loaded. World identity, generated-object IDs, authored constraints, chunk unload/reload and persisted runtime deltas need no new handling: world placement, IDs, streaming and persistence contracts are unchanged.

## Measured results

Accepted `baseline-wired` and `optimized-wired` captures both have accessible action/inventory cargo, 64/0 allocation controls, 67 dense overlap hits and 128 ignored-child hits across two repetitions. [Machine-readable evidence](input-interaction-profile-2026-10-12.json) retains all thirteen matched rows, P95s, source hashes, posture, test and memory receipts.

| Warm operation | Before median µs | After median µs | CPU reduction | Allocation events/op before → after |
|---|---:|---:|---:|---:|
| Cargo LOS | 1.77 | 1.44 | 18.7% | 1 → 0 |
| LOS with 64 ignored children | 32.36 | 29.64 | 8.4% | 1 → 0 |
| Dense target scan | 53.06 | 47.14 | 11.2% | 1 → 0 |
| Canister scan | 3.93 | 3.16 | 19.7% | 0 → 0 |
| Reachable inventory access poll | 2.34 | 1.92 | 17.6% | 1 → 0 |
| Unchanged cargo/target HUD | 14.17 | 2.15 | 84.8% | 4 → 0 |
| Inventory open/close | 1630.65 | 1399.10 | 14.2% | 87 → 80 |

Highlighted repeated median ranges are separated. Small differences on unchanged neutral-input/menu-navigation code are environment/measurement variation; no improvement is attributed to them. Inventory transitions still allocate for changing presentation/navigation. The final pre-Start active-gather guard is outside warmed probes: both conditions resolve cargo, and no gather input is injected. Its correctness is covered by the final regression gate.

Both processes exited successfully without budget stops. Optimized peak private memory was about 10.3 GiB, with more than 28 GiB free RAM retained. This comparison does not establish interactive FPS or lower end-to-end input latency.

## Validation and retained diagnostics

The exact candidate passed **173/173 focused EditMode tests**, zero failed/skipped, covering interaction/input arbitration, cargo/inventory/UI, controls preferences, menus, radial actions and camp/algae input. New checks cover complete dense queries and blockers behind ignored children, calibrated zero warmed query allocations, nearest/stable-ID selection beyond 64 hits, fresh access guards, late cargo resolution, explicit ownership, active pre-Start gathering/cancellation with motor release, slot repair/listeners and HUD invalidation/lifecycle. Nine final runtime/test files match the candidate. The scene hash and both new metadata GUIDs were verified.

An initial test fixture loaded setup geometry, causing a second-additive-scene failure and two later cargo-transfer failures. A runtime scene-creation attempt was rejected by EditMode. Final fixtures own isolated objects without loading saved geometry; a preview scene is used only for startup/cross-scene guards. The complete set passed after repair; gameplay rules and assertions were not weakened.

The initial profiling fixture used the input-system origin instead of the actual motor; its dense result is excluded. Corrected idle and first reachable captures are retained. The first reachable before/after pair exposed missing action cargo, so its HUD result is excluded from headline proof. Wired captures retain the cargo startup repair in both conditions. A partial optimized launch stopped below the RAM floor before measurement; later low-headroom launches were refused. All original logs/reports remain in the candidate's ignored Logs directory.

## Reproduction and closeout

[Diagnostic tools](../../../Tools/Diagnostics/README.md) describe copy selection, launch arguments, allocation controls, physical approach, wiring inspection and comparison checks. They stay outside production Assets. Copy receipt, raw reports, test XML/logs, source seals and process-watch receipts remain under candidate `Logs`; the compact evidence above binds the accepted results.

This authorized implementation/profiling slice is complete. Author-owned hands-on acceptance remains the next iteration: tap beside Legger, hold West, adjacent canisters, cancellation and modal transitions. Warm reuse does not promise zero cold-growth/changing-prompt allocations. No further cadence, navigation-repeat, movement, action-duration or world changes were introduced.

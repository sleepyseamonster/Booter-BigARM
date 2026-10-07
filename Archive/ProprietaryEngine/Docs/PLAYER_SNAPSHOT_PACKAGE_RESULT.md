# Player Snapshot and Runnable Package

P13 implemented 2026-09-10. The player restores its calibration state on startup and saves with F5, every ten seconds of simulation, and on normal exit. The HUD reports save/recovery status. The shared runtime restores stable player/marker identity, feet, velocity, facing, marker state and simulation tick; the player restores camera settings. Transient ECS, physics, GPU and audio handles are rebuilt and never serialized. Restoring a marker does not replay its audio cue.

`engine.player-snapshot` version 1 names the fixed `calibration:v1` world and its authored player/marker IDs. Two alternating slots retain the newest valid snapshot while replacing the older or invalid one. Reads validate both committed documents and select the highest valid generation. A corrupt newer file falls back to the older valid file. Incomplete `.writing` files never become load candidates. If no valid snapshot remains, startup reports the problem and preserves the profile instead of overwriting it. A fresh absent profile starts at spawn.

This is a single-writer calibration save format, not the later streamed-world delta store. Use a separate `--profile` directory for concurrent player sessions. Existing document staging locks prevent overlapping writes to a slot. A stale staging file can block future writes; the last valid snapshot remains readable, and the HUD reports the failure. Preserve the profile before resolving a stale file or choose a new profile. The implementation provides atomic file replacement and last-valid fallback, not a claim of complete power-loss durability or multi-file world transactions. P21 remains the owner for streamed deltas and broader save recovery.

Falling below y=-20 resets the capsule to the technical spawn while retaining marker state. This provides basic recovery from the finite calibration ground. It is not a checkpoint design or accepted game mechanic. Snapshot coordinates occupy the current bounded local frame; no new world seeds, generator versions or geography are introduced.

## Runnable package

The existing packaging tool now accepts `--model` and installs both applications, shaders, the cooked proxy and license notices. `out/player-skeleton/` is the current Mac package; its 29-file payload and source hashes are retained in [the package manifest](../Evidence/P13-package/package.json). It needs no source-relative shaders or model paths.

From `Engine/`:

```sh
out/player-skeleton/bin/engine_player --profile out/player-profile
```

Add `--silent` to avoid audio device initialization. Without `--profile`, the application uses `UserData` beside its executable. Saves are mutable user data and do not belong in the immutable package inventory. To produce a new package from a verified build:

```sh
python3 Tools/package_workbench.py --build build/foundation --out out/new-player-package --model out/models/calibration/model.json
```

A package destination must be new. This batch reused the existing cooked proxy and did not recook textures. The package is a local technical build; Windows, signing, distribution and complete shipping clearance remain open.

## Focused evidence

[Native snapshot cases](../Evidence/P13-snapshots/result.json) passed for runtime restart, stable identity, pose/velocity/time/marker restoration, no cue replay, alternating generations, interrupted staging, corrupt-newer fallback, invalid-state rejection and preservation when all committed data is bad. [The affected shared runtime/audio check](../Evidence/P13-runtime/result.json) also passed. Snapshot cases run as `build/foundation/engine_snapshot_tests <new-test-directory>` and retain a profile for the package check.

[One installed-player launch](../Evidence/P13-package-launch/result.json) ran from an unrelated working directory with no `--model` or `--shaders` override. It loaded generation 1, restored feet `(1, 2, -1)` and the active marker, and produced [a Metal capture](../Evidence/P13-package-capture/player.png) with zero reported GPU errors. This was a render-only restart check: simulation stayed paused and no gameplay input or audio device was exercised. The current first-pass package is usable; the full engine goal remains active.

Next: basic native placement constraints and deterministic rock generation, then connect those outputs to rendering, collision and the workbench. Keep the skeleton-first scope rather than extending snapshot or rendering polish.

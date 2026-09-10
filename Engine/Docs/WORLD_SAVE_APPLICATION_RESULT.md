# World saves in the player and workbench

P21 is complete at first-pass depth on the current Mac. Both applications now use `Game/WorldSession` to restore and save the same terrain/rock configuration, authored constraints, player/camera state and removed-rock deltas. Saves capture the runtime and deltas at one main-thread boundary and retain the expected generation, so another session cannot silently overwrite newer changes. The [library checkpoint](./WORLD_SAVE_CHECKPOINT.md) supplies the immutable-generation recovery and compatibility rules.

## Use

From `Engine/`, use the same recipe arguments and world-profile directory in either app:

```sh
build/foundation/engine_workbench --terrain Assets/Recipes/wasteland-terrain.json --stream-rock Assets/Recipes/wasteland-rock.json --world-profile out/my-world --model out/models/calibration/model.json
build/foundation/engine_player --terrain Assets/Recipes/wasteland-terrain.json --stream-rock Assets/Recipes/wasteland-rock.json --world-profile out/my-world --model out/models/calibration/model.json
```

Add `--constraints file.json` for an authored constraint document. Restart with matching recipes/constraints; a changed seed or configuration is rejected with a useful error and preserves the profile. Pick a different profile to start another world. `--world-profile` requires terrain mode. Interactive terrain sessions default to `UserData/World` beside the executable when no profile is supplied. New sessions start just above the generated ground.

F5 saves in both apps. The workbench's **World streaming** panel also has **Remove nearby rock** (within eight metres) and **Save world** buttons. Removal records a stable tombstone immediately; visual and collision replacement follows asynchronously. Saving before that replacement completes still saves the accepted removal. Both apps save every 600 simulation ticks and on normal exit. Failures appear in the panel/player overlay and stderr. If another session has saved meanwhile, reopen the profile to load that newer state before editing further.

The player's earlier `--profile` option remains for player-only calibration snapshots and cannot be combined with terrain mode. The single-rock generator/editor keeps its existing recipe workflow. Technical capture/verification modes do not save or auto-select a writable world profile.

## Evidence and limits

[Both apps and the focused test target build](../Evidence/P21-application-build/result.json). [The create process](../Evidence/P21-session-create/result.json) saves a removed rock before its replacement has completed and checks stale-session rejection. [A separate restore process](../Evidence/P21-session-restore/result.json) reconstructs the application runtime from that profile, confirms player/camera state and removed-rock identity, regenerates real Jolt collision, unloads/reloads the region and checks configuration, legacy-profile and unsupported-origin rejection.

[The actual player process](../Evidence/P21-player-restore/result.json) then loads generation 3, restores the same position and changed region, and renders one Metal capture with zero GPU errors. Its selected profile members retain their hashes. [The combined result](../Evidence/P21-applications/result.json) records the cross-process comparisons. The technical proxy is intentionally suspended; its waiting-for-collision message remains from initialization because simulation ticks do not run in this capture mode.

Repeat the focused native check with a **new** output directory:

```sh
build/foundation/engine_world_session_tests create out/new-session-check
build/foundation/engine_world_session_tests restore out/new-session-check
```

Workbench controls are compiled and source-reviewed; hands-on clicking and gameplay feel remain user-owned. This is a Mac application/session result, not Windows, full power-loss durability, migration, cargo transactions or final-art proof. Current adapters still use origin zero and approximately ±3.8 km traversal; a saved nonzero origin is rejected without rewriting it. P22 adds origin handling and bounded streamed integration next. No dependency or package change was needed.

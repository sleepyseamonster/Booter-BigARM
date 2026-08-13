# Perspective Top-Down 3D Foundation Evidence — 2026-08-13

## Outcome

The first perspective top-down 3D foundation is implemented in a separate generated scene at `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`. It is a development candidate, not a Build Settings cutover or release candidate.

The foundation includes:

- an elevated perspective camera with constrained right-stick orbit, damped follow, and obstruction pull-in;
- a camera-relative 3D Rigidbody player motor;
- centralized Gameplay input for keyboard and gamepad movement, sprint, and BigARM recall;
- deterministic streamed terrain meshes and colliders with matching geometry and lighting normals at chunk borders;
- walkable safe-spawn selection plus deterministic, slope-aware, collision-spaced greybox prop placement;
- immediate near-chunk loading followed by budgeted outer-ring creation and padded unload hysteresis;
- a compact BigARM follower with idle, follow, avoidance, recovery, and recall states;
- guarded scene build/open tooling, non-mutating validation, and focused EditMode tests.

## Automated Evidence

- Unity editor: `6000.4.0f1`.
- `TopDown3DPrototypeBuilder.RebuildFromCli`: exit `0`.
- `TopDown3DPrototypeValidator.ValidateFromCli`: exit `0`.
- The original foundation run passed `13 / 13` EditMode tests. After the traversal and camera-control hardening pass, the Unity GUI test runner passed `16 / 16`, with no failures, skips, or inconclusive results.
- The focused tests verify deterministic height sampling, exact neighboring chunk-border heights and normals, deterministic walkable safe-spawn selection, frame-rate-independent yaw and constrained pitch math, camera-relative normalized movement, required keyboard/gamepad bindings including right-stick Look, compact scene topology, explicit perspective renderer selection, and the protected legacy baseline.
- The Unity GUI perspective validator passed after the hardening pass. Unity completed its script reload without C# compilation errors.
- Fresh rebuild, validation, and EditMode-test logs contain no C# compilation errors, unhandled exceptions, missing-font warnings, import-worker transport errors, filesystem-time errors, or last-scene unload warnings.

## Preservation Evidence

The generated perspective scene remains absent from `ProjectSettings/EditorBuildSettings.asset`. The only enabled scenes remain:

1. `Assets/_Project/Scenes/PrototypeScene.unity`
2. `Assets/_Project/Scenes/SampleScene.unity`

The default URP renderer remains index `0`, backed by `Renderer2D.asset`. The perspective scene explicitly selects the existing non-default 3D renderer at index `1`.

Protected SHA-256 values still match the 2026-08-12 baseline:

| Protected file | Current SHA-256 |
| --- | --- |
| `PrototypeScene.unity` | `fab742d6de621ca5414d9f2ca273ce82ba1cbec52ca78e72a3a4437d50b6d9a7` |
| `SampleScene.unity` | `387d8216a517be775b6ecc2ab55b97ece687c3372322da2ba027a96908014912` |
| `Renderer2D.asset` | `0ad61c87afceceb7d18b08c9fe35d2e36a89640d5af073be0e9cec5839bf1516` |
| `EditorBuildSettings.asset` | `882bc38bca9cd5fe492cafb3583935c997bcbc691e50bcc7451ba2ddfffc8b5a` |

The pre-existing dirty ground-art files remained user-owned and were not included in this implementation.

## Visual Inspection Boundary

The generated scene was opened in Unity and entered Play Mode for visual inspection. The perspective camera rendered the generated terrain, the overlay reported the expected 25 loaded chunks, Booter appeared grounded, and the compact BigARM visual entered live follow-state logic without a runtime exception.

This is visual evidence only. It is not recorded as a gameplay smoke test or acceptance of feel.

## User-Owned Acceptance Still Required

Per the user's instruction, no gameplay smoke-test suite is retained and Codex will not run smoke tests unless explicitly asked. The user will verify:

- keyboard movement and sprint feel;
- physical-gamepad left-stick, RB sprint, and LB recall behavior;
- physical-gamepad right-stick orbit direction, speed, pitch range, and hold-on-release behavior;
- camera framing, damping, and obstruction response during extended movement;
- grounding and world streaming while crossing multiple chunk boundaries;
- BigARM follow spacing, size, avoidance, recovery, and recall feel.

## Deferred By Scope

Harvesting, items, survival balance, save migration, final BigARM mechanics, production models/animation, final biomes/landmarks, packages, Build Settings cutover, legacy cleanup, player builds, and release work were not changed.

## Import-Worker Incident

During one interactive closeout attempt, quit requests overlapped an in-progress Unity script compile. The editor reported missing-font Handle/IMGUI output and lost transport to both import workers. Both worker logs ended during ordinary scripted imports/domain reload and contained no asset-specific exception. After terminating only that exact editor process, three fresh Unity sessions completed rebuild, validation, and EditMode tests without reproducing any of those messages. No `Library`, `Temp`, metadata, package, protected scene, or user-art cleanup was performed.

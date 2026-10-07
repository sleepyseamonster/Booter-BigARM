# Batch 10 full-content review-build receipt

Date: 2026-08-24

Runtime implementation commit: `5a09dd5d2438bfa41a142ec81824cc254e3da2ce`

Async-startup verification commit: `6ffd502521bc3fa1d3a99f2c9d53b06fff296cc3`

## Outcome

The full-quality macOS visual-review player is built and ready for user review. A separate macOS Development Player proves that controlled profiling can retain production rendering, streaming, decoration, and generation settings instead of inheriting the low-cost stress posture.

The Windows hardware class and Windows target profile remain intentionally deferred. Neither this receipt nor the macOS diagnostic substitutes for the user's visual judgment or the later Windows result.

## Build artifacts

### User visual-review build

- Path: `/Users/worldbuilder/Desktop/Booter & BigARM/Builds/StandaloneOSX/WorldCreatorVisualReview-5a09dd5.app`
- Build type: non-Development `StandaloneOSX`
- Size: approximately 137 MiB
- Executable SHA-256: `f09214be292af5d5af2ffc8d8473cc4f8c6a574518068c54fc2e60768406c852`
- Build log: `/tmp/booter-worldcreator-visual-review-5a09dd5-build.log`
- Build result: success
- Performance-profile installation: compiled out; release graphics and world settings remain unchanged

### Full-content diagnostic build

- Path: `/Users/worldbuilder/Desktop/Booter & BigARM/Builds/StandaloneOSX/WorldCreatorFullContentDevelopment-5a09dd5.app`
- Build type: Development `StandaloneOSX`
- Size: approximately 339 MiB
- Executable SHA-256: `e7a0d5c2a99e1d7d5c31cce4519b61d727d9e2ebf8947d32d91295fcc0f1e594`
- Build log: `/tmp/booter-worldcreator-full-content-development-5a09dd5-build.log`
- Build result: success
- Full-content opt-in: `-topDown3DFullContentProfile`

## Mode contract

`TopDown3DPlaytestPerformanceProfile` remains Development/Editor-only and fail-safe:

- without the exact opt-in flag, the existing stress profile remains active;
- with `-topDown3DFullContentProfile`, telemetry remains active but the component does not replace the render pipeline, render scale, shadows, texture mip limit, frame cap, shader path, streaming radius, decoration radius, or generation budget;
- a non-Development Player does not install the profile.

Runtime receipts confirm the distinction:

| Mode | Scale | Mip limit | Settled terrain | Settled decorated | Renderers | Colliders | Decoration meshes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Development full-content opt-in | 1.00 | 0 | 225 | 49 | 420 | 269 | 144 |
| Development default stress | 0.50 | 2 | 30 | 1 | 32 | 30 | 2 |

The full-content diagnostic used a null graphics device at 640 by 480. Its frame rate and GPU fields are not artistic or target-performance evidence. It proves settings posture, world workload, and queue settlement only.

## Test receipts

- Full-content flag selection: 3/3 passed (`/tmp/booter-full-content-profile-tests.xml`).
- Complete World Creator namespace: 81/81 passed (`/tmp/booter-full-content-worldcreator-regression.xml`).
- Async startup staging contract: 1/1 passed (`/tmp/booter-async-startup-performance-regression.xml`).

The startup contract now verifies the topology-v2 asynchronous authority: the immediate ring is requested first, the remaining streaming ring stays staged, no mesh or decoration is synchronously realized during `Start`, and the target Rigidbody remains suspended until the center terrain integrates. The earlier synchronous 25-loaded-chunk assertion described the retired generation path and was replaced rather than weakened into an unrelated count check.

## Network and foreground behavior

Launching a macOS player is a visible application launch and creates a Dock item. Development Players also broadcast Unity PlayerConnection discovery on the local network for profiler attachment. Unity's enabled cloud-service bootstrap attempted `config.uca.cloud.unity3d.com` and `cdp.cloud.unity3d.com`; the recorded requests failed.

World Creator generation has no internet dependency. The user may deny the NetBarrier request. No agent should launch another player, focus Unity, or trigger a visible review without first warning the user and receiving explicit current-task permission.

## Remaining gates

1. The user runs the non-Development visual-review build and completes the seven-view and hands-on checklist.
2. At a later user-chosen time, the Windows hardware class, resolution, quality preset, and thresholds are defined and the full-content Development Player is profiled there.

Batch 10 remains technically sealed but not finally approved until both gates pass.

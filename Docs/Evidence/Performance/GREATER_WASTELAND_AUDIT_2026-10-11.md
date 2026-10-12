# Greater Wasteland performance audit

October 11, 2026, America/Phoenix. Greater Wasteland's main measured bottleneck is the CPU work associated with thousands of active native Terrain components. Keep the fixed terrain and collision data resident, but bound active terrain rendering around the gameplay cameras. Simply hiding distant heightmaps leaves substantial terrain culling work active. Failed companion route searches and synchronous inventory checkpoints are the next candidates for intermittent lag.

This is an audit and implementation recommendation. Production gameplay, terrain, materials, settings, packages, and asset GUIDs were not changed by this work. The rendering results below describe a controlled offscreen camera benchmark, not standalone Player FPS or visual acceptance.

## Measured rendering cost

The saved scene has **8,448 Terrain components and 8,448 TerrainColliders**, grouped into 33 sections and 528 sectors. Each native tile is 256 metres wide with a 257 × 257 height grid. The footprint is 553.65 square kilometres, and the tile grids contain 557,981,952 samples including duplicated seam vertices. Terrain instancing is already enabled, heightmap pixel error is 8, and terrain shadows are TwoSided. The inspected gameplay camera has a 500 metre far clip.

The task-owned copy at `D:\BooterBigArmValidation\PerformanceAudit20261011-01` matched production's saved scene, package manifests, quality settings, URP asset, renderer asset and pinned Editor version by SHA-256. [The snapshot receipt](greater-wasteland-audit-snapshot-2026-10-11.json) records those hashes and the frozen working-tree checkpoint. Hardware is an i7-9700K, RTX 3060 Ti and approximately 64 GB RAM.

The capture rendered the actual game camera through URP's `SingleCameraRequest` into a 1158 × 495 texture. Each condition used five seconds of settling followed by fifteen seconds of sampling. Initial renderer/shader warmup completed before the measurement clock. Actors were not driven. The distant-terrain comparisons kept a conservative 1,012 metre X/Z radius from the camera, leaving 52 Terrain components in the local area. All native terrain data, GameObjects and colliders stayed loaded.

| Condition | Enabled Terrain components | Drawing heightmaps | Samples | Mean update ms | P95 ms | Diagnostic updates per second |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Normal rendering | 8,448 | 8,448 | 70 | 215.02 | 232.16 | 4.65 |
| Distant heightmap drawing off | 8,448 | 52 | 180 | 83.30 | 95.17 | 12.01 |
| Distant Terrain components off | 52 | 52 | 1,542 | 9.73 | 12.13 | 102.77 |
| Normal rendering repeated | 8,448 | 8,448 | 69 | 215.18 | 230.95 | 4.65 |
| Half render scale | 8,448 | 8,448 | 69 | 216.30 | 223.84 | 4.62 |
| Shadows off | 8,448 | 8,448 | 76 | 197.45 | 292.90 | 5.06 |

The repeated normal baseline agrees within 0.1%. Restricting active Terrain components reduced the measured update cost by about **95.5%**, or roughly **22 times**. That is a relative result in this diagnostic path, not a promised improvement to Player FPS. Half resolution made no material improvement. Removing shadows was much less effective and had worse P95 variation.

| CPU marker | Normal rendering ms | Distant drawing off ms | Distant Terrain components off ms |
| --- | ---: | ---: | ---: |
| TerrainManager.CullAllTerrains | 141.83 | 52.14 | 0.46 |
| Terrain.Heightmap.RenderStep3 | 20.45 | 0.12 | 0.07 |
| Terrain.UpdateSplatMaterials | 10.23 | 0.07 | 0.07 |
| BehaviourUpdate | 1.09 | 0.60 | 0.22 |
| Physics.Simulate | 1.03 | 0.48 | 0.07 |

These markers are inclusive; do not add nested terrain markers together as independent costs. `TerrainManager.CullAllTerrains` alone exceeds the entire 16.7 ms budget for a 60 FPS frame, even with distant heightmap drawing disabled. Unity documents `drawHeightmap` as a drawing control; it does not promise to remove Terrain components from native processing. [Unity Terrain API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Terrain-drawHeightmap.html).

[The complete rendering measurements](greater-wasteland-offscreen-2026-10-11.json) include marker means and percentiles. GPU timings returned zero in this offscreen mode, and rendering counters such as triangle count were unavailable. Treat those zeros as missing evidence. Unity notes that frame timings supplement rather than replace detailed profiling and can fail to provide valid GPU measurements. [Unity frame timing documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/frame-timing-manager.html).

## Other sources of lag

The [source inspection receipt](source-inspection-2026-10-11.json) identifies the live code revisions behind these findings; it does not extend the frozen capture's runtime proof to newer inventory work.

### Simulation and physics

[The separate headless simulation capture](greater-wasteland-simulation-2026-10-11.json) did not draw terrain. Its initial baseline averaged 1.02 ms per update, with `BehaviourUpdate` about 0.14 ms and `Physics.Simulate` about 0.02 ms. Later conditions include more capture overhead and should not be treated as graphics comparisons. The stationary result does not indicate a large continuous gameplay-script or physics-simulation cost. It does not measure traversal, obstructed companion movement or overlay UI drawing.

The player motor, camera obstruction checks and ordinary follower probes already reuse NonAlloc buffers. Do not disable distant colliders or reduce collision accuracy to address the measured rendering bottleneck. Higher fixed-step counts during slow rendered frames can amplify the existing workload; that is a consequence to observe after rendering is bounded, not a reason to change movement timing blindly.

### Companion route retries

In [TopDown3DBigArmFollower](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DBigArmFollower.cs), `BuildRoute` samples a 13 × 13 grid: 169 ground probes and up to 169 occupancy probes before pathfinding. `TryGetRouteDirection` rebuilds when `route.Count == 0`, independently of the `routeReplanAt` deadline. A failed search leaves the route empty, so an unreachable goal can trigger another complete search on the next physics tick despite the configured 0.75 second interval. Movement obstruction also invalidates the route immediately.

This is a source-supported spike candidate, not a measured cost from the idle benchmark. Bound failed searches with a retry deadline, allow at most one rebuild per update, and consider budgeting grid queries over several frames. Preserve obstacle, slope and terrain-hole checks, responsive recovery when the route becomes valid, and BigARM's actual world position. No optimization may teleport him to Booter.

### Inventory checkpoints

The newer [inventory save service](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySaveService.cs) calls `Save` from `LateUpdate` when inventory revisions change or its thirty second checkpoint expires. [The file writer](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Persistence/GreaterWastelandInventorySnapshot.cs) serializes and hashes the snapshot, writes a temporary file, calls `Flush(true)`, then atomically replaces the previous checkpoint with a backup. That work is synchronous on the game thread. This newer implementation was inspected in the live working tree and was not in the frozen rendering candidate.

[A synthetic disk-pattern test](checkpoint-disk-pattern-2026-10-11.json) on the local application-data drive used a task-owned 64 KiB file, five warmups and thirty-five samples. Flush plus atomic replacement averaged **8.19 ms**, with **9.02 ms P95**. This excludes snapshot capture, serialization, hashing and actual save size, so it is not an actual autosave benchmark. It demonstrates that a durable write can consume a meaningful portion of a frame on this machine.

Capture an immutable checkpoint on the main thread, coalesce revisions, and serialize/write through one controlled background writer. Preserve checksum validation, durable flush, backup/recovery behavior and an explicit completion policy for shutdown and subsequent saves. Avoid fixing latency by silently weakening persistence guarantees.

### Dust refresh and renderer effects

[TopDown3DDustAtmosphere](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DDustAtmosphere.cs) resamples a 64 × 64 density map every 0.25 seconds, or sooner when the snapped camera cell changes. Each refresh performs 4,096 calls to `SampleAtPosition`, including zone and tint work, although the density texture consumes only intensity. The grid buffer is reused; texture upload uses `SetPixelData` and `Apply`. Cache unchanged intensity cells and avoid computing unused tint for each texel, with invalidation for zone and seed changes. Time this refresh separately to determine whether it explains periodic spikes.

The renderer enables SSAO, volumetric dust, bloom, lens flare, streak and tilt shift. Volumetric dust uses sixteen march steps and up to eight shadow samples along a ray at half resolution. Bloom uses a six-level pyramid. These are secondary graphics candidates after the terrain CPU work is removed. The present GPU evidence cannot rank their individual costs or justify reducing the accepted atmosphere and art quality. A subsequent bounded Player GPU capture should isolate effects at the intended display resolution.

### UI and allocations

[Cargo access](../../../Assets/_Project/Scripts/Runtime/TopDown3D/Inventory/TopDown3DCargoAccess.cs) uses `Physics.RaycastAll`, which returns a new hit array. The inventory controller polls access while the inventory is open and can repeat the same query while refreshing summaries, selected slots and details. Cache one display result per update and use a reusable query buffer with a correct saturation fallback. Transaction actions must still perform current reachability validation. Inventory is already guarded while closed, and most slot refreshes are event-driven; do not describe it as rebuilding all slots continuously.

[Cargo visuals](../../../Assets/_Project/Scripts/Runtime/TopDown3D/BigArm/TopDown3DBigArmCargoVisuals.cs) call `Refresh` in every `LateUpdate`, repeating activation, item lookup, position and scale writes. Parent visuals to their established anchors and update activation/scale on inventory changes. The load profile is already cached and recalculated on inventory changes, so mass recomputation is not an existing per-frame problem.

Algae and survival HUDs change gauge geometry as values change, while binding labels are refreshed periodically. Avoid rebuilding identical strings and geometry; isolate frequently changing gauges from static Canvas content if `Canvas.BuildBatch` proves significant. The main HUD already removes its GraphicRaycaster. Animation clips, work arrays and most physics hit arrays are reused; cloak mesh binding occurs once. These are smaller candidates, and the audit has not measured overlay Canvas rendering or moving-character skinning separately. [Unity UI optimization guidance](https://unity.com/how-to/unity-ui-optimization-tips), [Unity physics query guidance](https://docs.unity3d.com/ja/6000.0/Manual/physics-optimization-raycasts-queries.html).

## Implementation order

1. **Bound active terrain rendering.** Use the existing fixed tile/sector coordinates and cache ownership once. Update the active set when camera bounds change, with hysteresis and margins for look-ahead, shadows and every relevant camera. Keep native data, terrain GameObjects and every collider loaded. Restore prior enabled/drawing states on teardown. Do not scan all 8,448 objects on every frame.
2. **Separate collision validity from rendering state.** [FixedTerrainSurfaceIndex](../../../Assets/_Project/Scripts/Runtime/TopDown3D/FixedTerrainSurfaceIndex.cs) currently rejects `!terrain.enabled`. Component-based rendering control therefore needs a deliberate query-contract change: disabled rendering must still permit valid, enabled native collision. Verify holes, seams, outer borders, camera motion and remote waypoint checks before integrating it.
3. **Bound failed companion replans and checkpoint IO.** These target intermittent lag rather than the dominant steady rendering cost. Verify failure retry frequency, recovery and persistent checkpoint ordering independently.
4. **Measure the remaining spikes.** Add narrow timing scopes for dust refresh, route construction, save capture/write, cargo access and Canvas rebuilding. Replace generic generated-world telemetry with fixed-terrain counts, warmup exclusion and explicit timing availability. The existing telemetry's `world=not-found` is expected in Greater Wasteland and does not describe its terrain workload.
5. **Profile GPU effects after the CPU reduction.** Keep accepted rendering quality until isolated measurements identify a worthwhile change. If native component overhead still grows too far as the landscape expands, compare a versioned 1,024 metre TerrainData consolidation study. Sixteen current tiles per sector could become one 1025 × 1025 grid, but height/seam/collision/GUID and geographic-identity handling require a separate asset conversion plan. This is not needed for the first rendering-window implementation.

World identity, authored geometry and logical tile coordinates remain stable under the proposed rendering control. There is no terrain generation, terrain streaming or unload/reload. Rendering state creates no generated-object IDs or persisted gameplay deltas. Existing resource identities, waypoint constraints and inventory checkpoints remain their own owners. Rendering visibility must never become a proxy for whether terrain or saved-world collision exists.

## Operating limits and verification

Unity grew to roughly 15–16 GB of resident process memory while opening the complete isolated scene. Windows also recorded a desktop compositor crash at 19:57:40 with `0xc00001ad`, decoded locally as fatal memory exhaustion. Repeated Unity IL postprocessor helper crashes predate this audit. [The brief crash alert](crash-alert-summary-2026-10-11.json) preserves those observations without asserting a particular RAM, VRAM, driver or terrain root cause. At the author's request, detailed crash diagnosis was deferred. All audit-owned Editors have exited; no additional full-scene capture was launched after the alert.

Use one rendering Editor at a time and establish a rendering/resource budget before repeating heavy captures. Bounding rendering does not itself prove that the desktop crash is fixed, because native terrain data remains resident. The initial live attempts were interrupted by concurrent script reloads and exception floods; they were discarded. An earlier offscreen run timed out during renderer warmup and is also excluded. The accepted run completed all six conditions, reported no error and restored temporary settings. No production scene was saved, no gameplay smoke test was run and no Player FPS or visual acceptance is claimed.

The retained [audit tools](../../../Tools/Diagnostics/README.md) live outside production `Assets`. Their final source, including isolation guards and zero-sample rejection, compiled with Unity's installed Roslyn compiler and the current pinned assembly references without launching Unity. The executed measurements used the same capture conditions before those guard-only closeout changes. Raw counters include capture/render-request overhead; they are not proof of production allocations.

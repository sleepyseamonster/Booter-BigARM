# R5 — material, occlusion and representative rendering costs

Implemented 2026-09-15 under the [engine correction plan](./ENGINE_CORRECTION_PLAN.md). This closes A08 and the generic material path at the Mac engine boundary. [Evidence](../Evidence/R5-material-render-costs/result.json) binds the final source, binaries, representative capture and bounded frame trace. R6 native Windows validation is next; the visible Scene View/Game View presentation remains with the UI owner.

## Generic material ownership

`MaterialDefinition` now names its coordinate source explicitly: `WorldTriplanar` or `UV0`. UV materials carry a finite, nonzero scale and offset and reject unsupported UV sets before renderer acquisition. Texture identities are still semantic catalog IDs. `resolveSurfaceMaterial` verifies color, normal and packed-surface roles and transfer functions, acquires their shared `TextureStore` leases, and publishes one retained `SurfaceMaterialBinding` to render snapshots. GPU handles remain borrowed implementation details and never enter scene documents.

The same UV0 and tangent-space normal path is used by static and skinned draws. The representative Metal run resolves real cooked diagnostic color, normal and surface textures through the production catalog and renders three UV transforms. Existing world-triplanar material definitions retain their behavior.

## Lighting and depth convention

AO and contact shadows now share one declared convention: right-handed view space with positive linear view depth in metres. Screen UV plus linear depth reconstructs view position using the active vertical field of view and aspect ratio. AO radius, AO bias, blocker thickness, contact bias and contact thickness are all metre-valued engine parameters. Focused CPU contracts cover near/far reconstruction, multiple aspect ratios, projected radius, blocker rejection and invalid conventions.

The main scene target contains two `RGBA16F` attachments: direct radiance and indirect radiance with fog amount. Contact visibility affects direct sunlight. Material AO and screen AO affect indirect illumination. The display pass applies screen AO to indirect light, combines direct and indirect, applies fog, then performs exposure, tone mapping and display conversion. This removes the former coupling that darkened sunlight and fog.

AO or contact shadows allocate one shared auxiliary prepass with an `RGBA8` view-normal target, an `R32F` linear-depth target and `D24S8` hardware depth. With both features disabled, those targets retire and the main scene continues without them. Target creation checks attachment count and format support first; unsupported optional formats produce a bounded degradation string while preserving the required scene target. Allocation and resize accounting includes the live set, high-water set and simultaneous old/new overlap.

## Representative Mac measurement

The fixed workload uses 135 repeated render instances, 26 affine authored hierarchy draws with three LOD metadata values, three material instances, one textured skinned actor and one Jolt dynamic body. It submits 579 draws and uses the production renderer, material resolver, scene snapshot and physics owner. After 120 warm-up frames, telemetry retained 1,000 steady frames plus one explicit optional-target retirement frame at exactly 1920×1080. The trace capacity is 2,048 frames.

Hardware was a 10-core Apple M1 Max MacBook Pro with a 24-core integrated GPU and 32 GB memory. Backend was Metal with immediate presentation. The results are:

| Measurement | p50 | p95 | p99 | max | Samples |
|---|---:|---:|---:|---:|---:|
| Renderer draw submission CPU | 0.193 ms | 0.519 ms | 0.619 ms | 1.610 ms | 1,001 |
| Physics step CPU | 0.004 ms | 0.009 ms | 0.013 ms | 0.032 ms | 1,001 |
| `bgfx::frame` CPU call | 2.458 ms | 8.022 ms | 10.409 ms | 95.043 ms | 1,001 |
| GPU frame query | 2.419 ms | 4.800 ms | 5.983 ms | 7.660 ms | 907 unique GPU frames |

The active target estimate is 133,464,064 bytes (127.28 MiB), including the existing shadow resources. Disabling AO and contact shadows returns it to 108,580,864 bytes (103.55 MiB), so the shared 1080p auxiliary set costs 24,883,200 bytes (23.73 MiB). The resize-overlap high-water estimate is 174,936,064 bytes (166.83 MiB). The trace reports 15 live textures and a 49,416-byte backend texture estimate during the steady workload; these counters are backend estimates rather than process memory.

No batching or instancing rewrite follows from this run. Its measured GPU tail is acceptable as a first Mac architecture baseline, while the target Windows hardware and frame budget remain unset. The 95 ms frame-call maximum is reported rather than hidden, but one immediate-mode Mac run cannot identify its cause or establish display latency.

## Verification and limits

All 24 registered native suites pass, including the new material/occlusion contract. All scene shader variants compile. The ordinary Metal outdoor check completes with AO/contact enabled, resize and target replacement, and its authored captures remain pixel-identical when draw order is reversed. The representative 1080p run produced one capture and zero renderer callback errors.

This is a synthetic opaque architecture workload. It does not measure input-to-photon latency, gameplay feel, transparent rendering, actual streamed I/O, process-resident memory, target-PC performance or D3D11 behavior. Simulation and streaming phase values are zero because the fixture does not schedule those stages; they are not evidence that production streaming is free. No generator, terrain algorithm, permanent landscape, lore, world identity or persisted runtime-delta schema changed.

# Live Region Streaming First Pass

P20 implemented on 2026-09-10. The shared workbench/player path now generates nearby terrain regions in the bounded CPU queue, attaches ground and rock collision on the main thread, displays resident terrain/rock instances, and retires distant resources. Character movement waits while the required collision footprint is unavailable. This is live bounded streaming, not a completed world/gameplay system.

## Ownership and readiness

`RegionStream` owns desired regions, generation tickets, accepted CPU content and representation readiness. Each request has a monotonically increasing epoch/ticket; retired owners cancel pending work, and completions must match a live ticket before adoption. Authored constraints and generator configuration are copied into immutable job inputs. No worker touches GPU or physics APIs.

`StreamingScene` supplies the main-thread adapters. It prepares the terrain render mesh, builds a Jolt static collider from the terrain plus the highest-detail rock triangles, and publishes readiness only after both succeed. Failed attachment removes prepared resources and records an error while collision remains unavailable. Render and collision flags remain separate. Navigation version is explicitly zero/unavailable: P23 must supply a real matching tile version before AI navigation can consume it. Player-controlled traversal requires collision, not a fabricated navigation result.

Generated placement IDs remain separate from shared geometry and runtime resource handles. Four deterministic rock shapes, each with up to three detail levels, are reused across placements; the stable cell member selects a shape with `member % 4`. Each placement keeps its own generated ID, position and yaw. The renderer borrows meshes for a frame and submits per-instance transforms using shared buffers; this is not hardware instancing. Collision is aggregated into one static mesh per region. Physics queries currently identify that region collider; individual rock interaction/subshape resolution remains a later gameplay adapter.

Region retirement cancels work, removes the collider and GPU terrain mesh, then releases CPU content. The four shared rock shapes survive until the streaming scene is destroyed. Restart/regeneration reproduces base placement identity; mutable world deltas are not yet stored. P21 owns persistence beyond existing player snapshots.

## Bounds and traversal

Up to two anchors request a 3 by 3 neighborhood each. Lower numeric priority wins, with the center first. The adapter currently supplies the player; technical checks supply a second anchor, not an implemented BigARM. Regions within a two-region ring may remain resident to avoid immediate unload/reload at borders. A hard 25-slot cap evicts retained, undesired regions before preventing desired admission.

CPU jobs retain the existing 32-request/128 MiB staging limit, with one worker and explicit per-region peak reservations. Accepted region payloads have a separate 64 MiB estimated CPU residency cap. The adapter adopts at most one result per frame, so collider creation cannot accumulate into an unbounded frame batch. Upload admission uses a 512 KiB normal allowance and a 16 MiB hard single-item limit with the existing explicit one-item exception. These are provisional bounds, not measured target-PC budgets. Jolt allocator and total GPU/process memory are not represented by the estimated CPU payload counter.

The current combined collider profile rejects rock detail that could exceed the existing 300,000 triangle-vertex input bound. Use the provided subdivision-2 recipe. Render detail varies by camera distance while collision retains the highest level. Frustum sphere checks use the backend depth convention; the render far plane extends to 600 m for streamed scenes. The existing small sun shadow footprint follows the local camera target. Larger-area shadows, atmosphere and terrain/rock polish remain deferred.

Before each fixed character step, the runtime checks both endpoint footprints with a one-metre contact margin. It holds the character when collision is missing, including during initial loading and after retirement, instead of stepping into absent ground. Other world bookkeeping can continue. The native check exercises a loaded negative-coordinate boundary and missing-collision waiting; hands-on controls and feel remain user-owned.

The current graphical/physics working origin is fixed. The adapter bounds traversal to roughly +/-3,800 m while using the existing 4,096 m local conversion range. Region generation itself retains integer world addresses, but origin rebasing and region-aware persisted player coordinates remain P21/P22 work. Existing player snapshots save local feet/camera state; they do not yet bind the terrain seed/configuration or world deltas. Relaunch with the same configuration until that save contract is integrated. The debug workbench shows attachment errors; neither app silently invents replacement terrain when generation fails.

## Run

From `Engine/`, with the existing cooked textures:

```sh
build/foundation/engine_workbench --terrain Assets/Recipes/wasteland-terrain.json --stream-rock Assets/Recipes/wasteland-rock.json --constraints Assets/Recipes/wasteland-placement.json --catalog out/surfaces-accepted-a/catalog.json --model out/models/calibration/model.json
```

Terrain mode starts with the shared third-person character enabled. The existing controls apply. The workbench shows region count and waiting/error state. Single-rock editing remains a separate mode; `--rock` and `--terrain` cannot be combined. The dedicated rock-generator package remains unchanged and usable.

The separate player also accepts the terrain configuration:

```sh
build/foundation/engine_player --model out/models/calibration/model.json --terrain Assets/Recipes/wasteland-terrain.json --stream-rock Assets/Recipes/wasteland-rock.json --constraints Assets/Recipes/wasteland-placement.json --profile out/stream-player-profile
```

The player currently uses its neutral fallback material path; the workbench binds the real rock/ground textures. Use the current build for streaming. The earlier rock-generator package intentionally contains the delivered generator milestone and predates this integration.

## Evidence and next work

[The focused native check](../Evidence/P20-runtime/result.json) passed: center priority, real Jolt region attachment/query, safe waiting, loaded negative-border character traversal, hysteresis, two anchors, stable reload IDs, CPU/body retirement, stale cancellation and frustum depth/side conventions. It adopted 30 regions across its bounded scenarios and retired 39 slots, including pending work. No physical input or gameplay smoke test was run.

[The final render-only Metal check](../Evidence/P20-render/result.json) loaded the origin neighborhood, moved a technical anchor four regions away, then returned. All three captures used real textures with zero GPU errors; 18 regions retired. Vertex/index buffer counts returned to 31/24. The distant scene changed 160,314 of 358,560 sampled pixels; the returned scene had zero differences above three code values. Character simulation did not advance. Native Windows, companion behavior, persistent world changes and origin shifts are not proven by this check.

Both apps and the native test target build. Next: P21 persistent region deltas and consistent save generations, then P22 integration/origin handling at skeleton depth. Do not extend rendering polish before those ownership contracts are connected.

# TopDown3D Performance Audit And Optimization

Status: staged-startup packet present; the latest C# import, automated tests, shader visual validation, and player profiling are still required before performance acceptance.

## Scope And Preservation Contract

This pass targets runtime latency, frame spikes, and avoidable memory pressure in the current `TopDown3DPrototype` without changing world identity, generated placement, collision, streaming coverage, camera framing, materials, or graphic settings.

The following behavior remains unchanged:

- the world seed, generation versions, stable generated-object identities, and chunk coordinates;
- the 18-unit chunk size, 24 quads per axis, seven-chunk streaming radius, two-chunk immediate ring, two-chunk configured frame allowance, and one-chunk unload padding;
- the requested per-chunk natural-object densities and every existing placement/planning rule;
- terrain texture inputs, patch masks, blend order, rocky parallax, normals, lighting, shadows, render scale, and MSAA;
- chunk ownership and unload/reload reconstruction, including terrain MeshCollider ownership.

## Audit Baseline

The source and live-system audit was captured on 2026-08-13 in Unity 6000.4.0f1 while the Editor owned the project and was in Play Mode. This is an Editor observation, not a Development Player benchmark.

- The steady seven-chunk radius requests 225 loaded chunks. Startup immediately realizes a 5-by-5 ring of 25 chunks.
- Each terrain chunk creates a 25-by-25 vertex grid, a render mesh, and a MeshCollider, then synchronously realizes escarpments, natural objects, and deposition.
- The configured natural-object request is 22 scatter, 72 ground-detail, and 156 fine-gray candidates per chunk before deterministic rejection.
- Pending chunks were removed from the front of a `List`, shifting the rest of the queue for every realization.
- Combined presentation meshes retained CPU-readable copies even though their geometry is immutable after creation.
- The terrain fragment path sampled every surface family before its patch mask was known, including height, normal, and parallax inputs for masked-out rocky layers.
- The rock shader sampled the luster triplanar texture even for materials whose luster strength was zero.
- The volumetric dust renderer feature is installed, but no enabled scene atmosphere owner was found in the current scene audit; it was not treated as the primary live cause.
- The host was simultaneously under substantial unrelated CPU and memory pressure. Adobe Premiere Pro and WindowServer were major CPU consumers, and macOS had roughly 12 GB in compressed memory. Those conditions can independently make the Editor and Game view stutter and must be separated from game-code measurements.
- A five-second macOS sample of the running Editor found no sampled `TopDown3D` managed-code hotspot in steady state. The visible main-thread work was dominated by Editor camera/SRP rendering and gizmos, while Unity worker and graphics threads were commonly waiting on jobs or semaphores. This supports treating chunk realization as a spike problem and Editor/render/host load as the steady-state confounder; it is not a substitute for a Development Player CPU/GPU profile.

## Implemented Packet

### Chunk lifecycle

`TopDown3DProceduralWorld` now consumes its sorted pending list with a cursor instead of repeated front removal. The configured chunk-count allowance remains authoritative, and realization yields after the first completed work stage that takes the batch beyond a provisional 2 ms guardrail. This bounds continued work between frames without changing which chunks are requested or their distance order.

Startup still creates the entire immediate terrain and MeshCollider ring synchronously, then fully decorates the center chunk before the first physics step. That preserves safe spawn, visible ground coverage, and collision authority. Escarpment skin, natural objects, and deposited dust for the other immediate chunks are queued and completed through the normal frame budget; newly streamed chunks likewise complete terrain and decoration as separate stages.

The decoration queue is keyed by deterministic chunk coordinates, rejects duplicates, skips chunks that leave the required set, requeues required terrain-only chunks after a center change, and clears state when a chunk unloads. It does not change the world seed, planner inputs, stable IDs, authored constraints, or reconstructed output. No persisted runtime delta exists for these presentation objects, so persistence behavior is unchanged.

Splitting terrain generation or MeshCollider cooking itself is deliberately deferred until profiling proves it necessary. The immediate collision ring is the spawn and traversal safety boundary.

### Mesh construction and memory

Terrain data/build, chunk decoration, natural-object planning, formation combination, and cosmetic layer combination now have named profiler markers. Natural-object buckets, combined-mesh buffers, and escarpment-skin buffers receive known or calculated capacities before population. Immutable, presentation-only natural-object, escarpment-skin, and deposited-dust meshes release their CPU copy in Play Mode after renderers and simple source-bounds colliders are configured.

Terrain meshes remain CPU-readable because the same mesh is owned by a MeshCollider. No collider topology or generated transform changed.

### Shader work

The rock shader avoids the luster-mask triplanar samples only when luster strength is exactly zero. This condition is uniform for the whole material draw, so it does not introduce per-pixel divergence or alter nonzero-luster materials.

An attempted terrain-layer branch was removed during self-audit. Branching implicit texture samples on per-pixel procedural masks can change derivative and mip behavior across graphics platforms, so it did not meet the graphics-preservation contract without stronger proof. Terrain shader sampling is therefore unchanged in this packet.

## Profiler Markers

- `TopDown3D.World.RefreshChunks`
- `TopDown3D.World.ProcessPendingChunks`
- `TopDown3D.World.BuildChunk`
- `TopDown3D.World.DecorateChunk`
- `TopDown3D.World.BuildTerrainData`
- `TopDown3D.World.ApplyTerrainMesh`
- `TopDown3D.World.DecorateNaturalObjects`
- `TopDown3D.World.PlanNaturalObjects`
- `TopDown3D.World.BuildRockFormation`
- `TopDown3D.World.BuildCombinedNaturalLayer`

## Validation And Acceptance Gates

Evidence for the first packet:

- Unity's background import completed a successful Tundra build of `BooterBigArm.TopDown3D.Runtime.dll`. The only reported C# warning was the pre-existing obsolete API use in `TopDown3DCameraRig`. The newer staged-startup edits still require import.
- The modified rock shader completed import without a specific syntax/compiler error. Unity continued to emit its existing "all subshaders removed" import warning for this shader, which also appears in the log before this packet; visual and target-platform shader acceptance therefore remains open.
- Task-owned diffs pass `git diff --check`. Repo health still reports unrelated pre-existing failures: the intentionally absent legacy runtime asmdef path expected by the checker and whitespace in user-owned material/scene diffs.

Current staged-startup evidence:

- The exact Unity-generated Roslyn response file compiles the current runtime sources successfully into an isolated `/tmp` output. It reports only the same pre-existing obsolete camera API warning.
- The current Editor test sources plus `TopDown3DPerformanceTests.cs` compile successfully against that isolated runtime assembly.
- This compiler proof does not replace Unity import, executing the EditMode tests, scene validation, or player profiling.

Complete the remaining gates through the GUI when appropriate, or close the GUI and wait for `Temp/UnityLockfile` to disappear before any batchmode command. Never start batchmode against the open project:

1. Finish importing the staged-startup scripts and restored terrain shader; require a successful Tundra build and zero new shader errors.
2. Run the focused `BooterBigArm.Editor.Tests` EditMode suite. The new staged-startup contract must prove 25 immediate terrain/collider chunks, one synchronously decorated center chunk, 24 queued immediate decoration stages, and bounded forward progress. Existing deterministic generation, chunk-border, formation topology, mesh-bound, and visual-system contracts must remain green.
3. With the GUI closed, run `BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli` in a separate batchmode session.
4. Capture a Development Player profiler baseline and post-change trace at the same resolution, seed, camera path, quality settings, and host load. Record median frame time, 95th/99th percentile, worst streaming spike, render-thread/GPU time, GC allocations, and memory after the steady ring loads.
5. In the Frame Debugger and a fixed-camera image comparison, verify terrain transitions, rocky relief, triplanar rocks, shadows, fog, and dust are unchanged.
6. Revert any optimization whose representative trace does not improve its intended marker or whose fixed-camera comparison changes the accepted graphics.

A 16.67 ms frame is a useful 60-fps diagnostic reference, not an accepted shipping target. Target platform, minimum hardware, resolution, and final frame-time budget remain product decisions.

## Deferred, Evidence-Gated Work

- Jobified terrain generation, pooling, and asynchronous collider cooking are next only if `BuildChunk` remains a dominant spike after this packet.
- Terrain texture-sampling redesign, draw-distance, density, shadow, render-scale, texture, fog, and dust reductions are excluded because they would change current graphics or require a separately proven rendering architecture.
- GPU instancing, GPU Resident Drawer, and occlusion changes require representative Frame Debugger and target-hardware evidence; they are not assumed wins.

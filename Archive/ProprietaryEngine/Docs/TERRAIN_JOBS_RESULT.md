# Open Terrain and Bounded Generation Jobs

P18/P19 implemented as a CPU first pass on 2026-09-10. A native terrain recipe generates a 256 m patch with a 33 by 33 vertex grid, exact triangle-expanded collision data, deterministic rock placements, authored landmark anchors and shared macro travel corridors. The terrain cooker consumes the same generator through the new bounded job queue and writes the existing cooked-model format with a placement sidecar.

## Terrain contract

The versioned recipe supplies world seed and height amplitude. Signed region addresses remain integer values; generation never flattens a large world address into a float. Hashed region corners and continuous shoulders produce common border heights and normals. Mesh positions remain patch-local. Surface queries interpolate the actual two triangles in each 8 m grid cell; their normals describe the triangle, while render vertex normals use a smoothed field derivative. Triangle winding is upward. The public surface result does not expose heightfield storage to callers.

Flat corridors run along region boundaries, with a six-metre reserved half-width inside eight-metre flat ground. Each patch owns its west/south segments and neighboring patches continue the network. These provisional straight calibration routes connect across patches and accommodate both current traversal profiles. They are not final geography, detailed navigation or canyon support. Existing authored route constraints additionally reserve rock-free space; they do not automatically flatten terrain or prove an authored route traversable.

Each potential rock owns a stable 16 m cell slot. Seeded jitter and rejection do not renumber surviving IDs. Conservative footprints, grid corridor clearance and authored exclusions/routes constrain placement. This first population profile accepts footprints up to four metres; larger rocks produce an explicit unsupported-profile error. Rocks sit on the triangle surface height at their placement origin and receive deterministic yaw. Contact fitting across an entire sloped footprint is later art/placement work. Authored exclusions also emit named landmark anchors owned by their normalized region; this does not invent landmark geometry or narrative content.

Generated IDs and recipes survive regeneration independently of render/physics handles. Unload can discard CPU meshes and rebuild the same base from seed/version/address. Runtime deltas, origin-shift attachment, actual streamed collision/render residency and persistent object changes belong to P20/P21. This batch does not attach terrain to the player or implement live streaming.

## Jobs and admission

`BoundedJobs<T>` uses one CPU worker by default, with explicit bounds of one to four workers, 32 outstanding requests and 128 MiB of reserved payload/staging. Existing Jolt physics runs single-threaded, so no second physics worker pool competes with that default. Queued, running and completed results all retain reservations until cancellation retires them or the caller takes ownership. Completed results form a bounded staging queue; there is no independent unbounded result cache.

Requests carry owner, epoch and unique ticket. An accepted newer epoch cancels older requests for that owner; a rejected admission preserves existing work. Cooperative cancellation allows terrain generation to stop between rows/placements. Even a slow task that ignores cancellation cannot publish its cancelled result. Errors return as failed completions. Shutdown cancels and joins workers before releasing staged data. Tasks must cooperate and honor their declared peak-memory reservation; this is an internal service, not a sandbox for arbitrary callbacks. Payload byte accounting is an estimate of owned containers, not allocator/process/GPU memory measurement.

The caller drains completions on its own thread. No GPU or physics API is called by generation workers. Per-frame upload admission defaults to 8 MiB with a 128 MiB hard item limit. A single larger item may enter an otherwise empty frame with an explicit oversized-item flag, avoiding permanent starvation. The caller owns resident-memory accounting after taking a result and must compare tickets/epochs against its live region owner when attaching it. P20 will consume these contracts for real upload/collision readiness and retirement.

## Run and evidence

From `Engine/`:

```sh
build/foundation/engine_terrain_cook Assets/Recipes/wasteland-terrain.json Assets/Recipes/wasteland-rock.json Assets/Recipes/wasteland-placement.json out/terrain/example -1 0
```

Choose a new output directory. The result contains ground `model.json`/mesh, terrain/rock recipes, constraints and `terrain.json` placements/routes/anchors. It does not cook individual rock meshes into the ground mesh.

[One combined native check](../Evidence/P18-P19-runtime/result.json) passed: common east/north borders at ordinary, negative and large signed coordinates; regeneration identity; both triangle interpolation cases; exact render/collision triangle agreement; authored exclusions and landmark ownership; unchanged surviving placement IDs; both traversal classes on macro routes; version/cancellation rejection; real terrain generation through the queue; out-of-order delivery; cancelled epoch suppression; oversized/result-overrun failure; shutdown; and upload admission limits. The default region held 161 rocks and reported 205,232 estimated CPU bytes.

[One native cook](../Evidence/P18-terrain-cook/result.json) generated region (-1, 0) with 2,048 triangles, 150 placements and 204,990 estimated CPU bytes. Both new targets built successfully. No GPU terrain, live streaming, gameplay, target-PC performance or native Windows proof is claimed. The existing renderer and player were not changed in this batch.

## Immediate delivery priority

The user clarified during this batch that the **completed rock generator is the first milestone**. P18/P19 are foundational work already underway, not permission to move the first deliverable to a streamed world. Next, finish a convenient runnable rock-generator package and concise handoff using the implemented P14–P17 systems. Live P20 streaming follows that milestone. Advanced rock art and engine production polish remain later work.

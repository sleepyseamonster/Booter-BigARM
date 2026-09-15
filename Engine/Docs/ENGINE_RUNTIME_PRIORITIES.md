# Production runtime and AI authoring priorities

2026-09-14. Applies the user's updated [direction](./DIRECTION.md) within the [master plan](./FOUNDATION_PLAN.md). This is a sequencing clarification, not a claim of implemented performance or a replacement engine roadmap.

## Architectural target

Build a professional engine that can grow toward AAA workloads through iterations. Every batch must improve a production runtime capability, its content pipeline or an engine operation usable by AI authoring. Visual effects remain useful when integrated into that architecture with known costs. Avoid repeated fixture tuning, broad speculative rewrites and editor-control work in this lane.

The UI agent owns the minimal viewer and interaction surfaces. Engine operations must have a UI-independent owner. Both the agent and any UI should use the same validation, commit/apply and persistence path; neither may create a competing world model. Existing recipe documents, cooker tools, edit history and runtime modules are the starting point.

## Shared frame-performance baseline — implemented 2026-09-14

The [L2/runtime result](./OUTDOOR_LIGHTING_L2_RESULT.md) delivers shared optional frame traces, explicit presentation policy and two sun cascades. The following contract describes this completed measurement pass. Region adoption still has a measured frame-thread burst; its cause must be attributed before moving work across ownership boundaries.

1. Add bounded, machine-readable frame telemetry at shared runtime/render boundaries. Separate CPU update, streaming/adoption/upload, render submission and present/frame wait where observable. Read backend GPU timing only when supported and valid; retain frame identity because GPU results arrive later.
2. Make presentation/pacing policy explicit in renderer/platform configuration and preserve it on resize. Document the existing default. Measure changes before selecting a lower-latency policy; disabling VSync alone is not a latency strategy.
3. Use one representative scene with resident and streaming rock/terrain workloads. Retain backend, drawable size, geometry/resources, sample count, median and tail timings, memory/queue counters and configuration. Use bounded sample storage and export to a file, not a new profiler UI.
4. Identify the largest evidenced frame-thread stall or burst. Fix one bounded cause if found, with a comparison using the same workload. Do not invent a bottleneck to justify a rewrite.

Done: a shared, repeatable runtime baseline and explicit pacing configuration, with honest timing limitations and one evidence-backed scheduling correction if warranted. Compilation, frame intervals and GPU timestamps must not be labeled input-to-photon measurements. Technical capture mode timing must not be reported as interactive performance. No fixed hardware/frame-time promise is selected until a Windows target and representative workload support it.

## Next bounded batch: remove the streamed-region adoption burst

The corrected load/retire/return trace records streaming CPU p95 about 53 ms and maximum 68 ms. The attribution pass now isolates terrain render preparation at a maximum of about 0.35 ms and Jolt mesh cooking at about 59 ms. Move physics-shape preparation into bounded jobs, retaining physics/render ownership, epoch rejection, byte reservations and old valid resources until adoption. Compare the same workload once. Do not change world identity or geometry to hide stalls.

## Following engine work, in dependency order

- **Bounded asynchronous work:** profile generation, collision cooking, asset decode, upload/adoption and persistence. Keep expensive work off the frame thread where safe, with explicit per-frame budgets, request epochs, cancellation and backpressure. Preserve ownership and old valid resources until replacements are ready.
- **AI authoring interface:** expose inspect, validate, apply, save, export and capture through typed engine operations using existing documents and domain owners. Return operation identity, structured errors, result/version and completion status. Validate stale edits before commit and preserve undo/recovery. Begin with a local operation path and one real workflow; add a transport only when needed. Agent reasoning/network activity never blocks the real-time loop.
- **Scalable rendering:** extend pass/resource ownership, visibility and submission around representative world data. Wider shadows, AO and fog consume this infrastructure with explicit quality/resource budgets. Avoid making fixture state the permanent scene model or growing a general render graph without a demonstrated dependency need.
- **Platform proof:** retain Mac development while practical, then measure the actual Windows backend on declared hardware. Architecture portability is distinct from target performance.

Do not turn these into a long infrastructure detour. Implement each contract alongside a real workload, run focused checks once, and move forward when it works. Keep a usable scene viewer throughout.

## World and persistence invariants

Telemetry and pacing change no world seed, generated-object identity, authored placement constraints or persisted gameplay delta. Streaming budgets alter scheduling/residency, not generation results. Asynchronous completions require the current owner/epoch before adoption and must not resurrect unloaded objects. AI-authored changes use versioned documents and deliberate deltas; transient renderer/physics handles never become durable IDs. UI and agent commands share the same authoritative transaction boundary.

## Current scope and deferred claims

L1 sky/display and L2 shadow coverage are implemented on Metal. Shared depth/normal inputs with AO, height fog and the broader post-processing pipeline remain required engine work after this bounded adoption correction. Keep those rendering batches moving alongside AI authoring and runtime integration; do not expand profiling into an infrastructure detour. Windows performance, an AI command protocol and AAA readiness are not established by this pass.

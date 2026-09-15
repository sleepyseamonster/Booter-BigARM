# R4 — shared play lifecycle and durability

Implemented 2026-09-15 under the [correction plan](./ENGINE_CORRECTION_PLAN.md). This closes A09–A15 at the engine boundary and completes the shared runtime/durability prerequisite for R5. [Evidence](../Evidence/R4-play-durability/result.json) binds the final source and application binaries, all 23 registered native tests and a clean Metal standalone-player capture. **R5 material, AO/depth and representative resource-cost work is next.** Visible Scene View/Game View presentation remains with the UI owner.

## Shared play boundary

`PlaySession` now owns the explicit starting, running, paused, stopping, stopped and failed lifecycle shared by the Workbench and standalone Player. It starts from a retained immutable authoring revision, allocates one runtime, owns gameplay input focus, and gives every start a fresh epoch. Stale commands reject. Stop is idempotent and release-only; runtime state and unsaved changes are discarded without modifying the authoring source. Static-box and dynamic-capsule adoption provide the bounded generic physics seam while existing player, camera, animation, audio and streamed-world consumers continue through `CalibrationRuntime`.

Pause and focus suspension preserve the fixed clock's fractional presentation phase. Time received while suspended is discarded, so presentation neither rewinds nor accumulates a wall-clock backlog. Held inputs must return to neutral after focus changes before gameplay can consume them again. `PlaySessionInspection` exposes state, epoch, source revision/checksum, tick, focus/input ownership, player position, physics-body count and failure information without a widget dependency.

Both applications now instantiate this service. The Workbench uses its authoring snapshot as the retained source revision; the standalone Player uses the same session service and regular third-person presentation. The UI owner can place that frame in a separate Game View without creating another simulation, camera, physics world or save model. The current engine build does not claim that the final visible split/tab UI has passed human acceptance.

## Streaming and physics recovery

Region slots now distinguish pending, ready, retryable failure and permanent failure. Every completion is consumed, transient generation/attachment/collision failures use bounded exponential update backoff, three failures become permanent, and an explicit retry can reset that state. A valid prior resource remains resident while its replacement prepares. Content revisions travel with collision work; completion for a retired or replaced revision cannot become ready later.

Static support replacement captures both old and new bounds and wakes only dynamic bodies in their combined contact neighborhood. Removal wakes dynamics around the retired bounds. The focused proof lowers nearby support, observes that body fall, verifies a distant sleeping body remains stationary, then removes its own support and observes the wake/fall. Stream retirement deliberately removes collision support; guarded player traversal stops until valid collision is ready.

`StreamingScene` teardown now disconnects its guard without creating a calibration floor, shuts down collision work while the runtime is alive, retires the stream and releases residents. Recreating calibration collision is a separate fallible transition. The capacity proof fills the supported physics body budget and confirms disconnect remains allocation-free and nonthrowing.

## Durable save and load service

`DurabilityService` is a bounded one-worker disk owner. The frame thread submits immutable world values and receives separate queued, writing, durable, loaded, superseded or rejected receipts. Explicit saves retain expected disk generation, session epoch and source revision. Autosaves coalesce only queued autosaves for the same profile, and each superseded request retains an observable terminal receipt. A stale writer, permanent I/O error or incompatible content returns a rejected receipt without replacing the last good generation.

Loads run on the worker, validate through the existing world-generation reader and can be taken only by a caller presenting the matching session epoch and source revision. This supplies the prepare/validate/adopt boundary; the caller still owns reconstructing live GPU/physics consumers. Shutdown drains queued work and joins the worker, so pending explicit saves finish with an observable terminal receipt before destruction. Workbench and Player world-profile saves now use this service rather than writing on the frame loop.

The two-slot calibration reader now propagates unsupported kind/version errors instead of treating them as repairable corruption. The proof preserves a future-only field byte-for-byte across rejected load and save attempts. World generations retain their existing writer lease, immutable committed directories, expected-generation conflict check, last-good fallback and incompatible-version preservation.

File members are flushed before atomic replacement on supported platforms. A rename is not presented as a full power-loss guarantee: directory-entry durability varies by platform and still belongs in native packaging/recovery proof.

## Verification and limits

All 23 registered native tests pass. R4 added registration for player snapshots, coherent world saves and the two-process world-session create/restore contract, using isolated build directories. Focused coverage includes fractional pause/resume, source isolation, fresh epochs, failed start, stale commands, capacity teardown, support edits, bounded stream retry/permanent failure, stale collision completion, incompatible snapshot preservation, slow save receipts, stale/concurrent saves, permanent I/O errors, autosave coalescing, revision-bound loads and shutdown flush.

The rebuilt Workbench and Player both link the shared session/durability implementation. A noninteractive standalone Player launch rendered one 2240×1440 Metal capture with zero renderer callback errors. This does not prove gameplay feel, the UI-owned visible Game View, live local-socket play verbs, input-to-photon latency, representative performance or Windows/D3D11 behavior. No gameplay smoke test ran. No generator algorithm, permanent landscape, world seed, generated-object identity rule, authored constraint or saved-delta schema changed.

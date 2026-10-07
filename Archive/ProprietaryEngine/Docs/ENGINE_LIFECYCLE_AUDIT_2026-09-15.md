# Engine architecture audit — lifecycle follow-up

Second bounded audit, 2026-09-15, against `3170b9f`. Supplements the [first architecture audit](./ENGINE_ARCHITECTURE_AUDIT_2026-09-15.md), preserving its findings and R1–R6 sequence. No runtime behavior was changed.

## Decision and evidence

Keep the engine architecture and libraries. Add four concrete corrections to the lifecycle/durability batch, then stop broad auditing and start R1 scene authority. The two audits contain 15 open findings with different priorities and proof scopes, not 15 equally urgent rebuilds.

This pass examined physics ownership and sleeping bodies, fixed-tick presentation, streaming teardown, job cancellation/adoption, GPU resource owners, and two-slot snapshot recovery. Five diagnostic cases reproduced four new findings; the support-edit finding has separate removal and replacement cases.

- [Probe source](../Tests/RuntimeLifecycleAuditProbe.cpp), [Mac runner](../Tools/audit_runtime_lifecycle.py), [observations, source hashes and existing-test outputs](../Evidence/ARCH-2026-09-15-followup/observations.json).
- Freshly rebuilt and ran `engine_physics_tests` and `engine_snapshot_tests`: both passed. The latter is an unregistered executable identified in A11. Their current assertions do not cover the new cases.
- Probes use disposable profiles under `Engine/build/`, diagnostic collision geometry and a headless runtime without a model. No user saves, viewer launch or gameplay smoke testing.
- `reproduced_issue: true` records a defect. Successful execution is evidence collection, not acceptance.
- Capacity testing catches a teardown prerequisite's exception. Destructor termination is a source-level inference, not a deliberately crashed viewer.

## Additional findings

### A12 — P1: recovery can overwrite an unsupported snapshot version

Source: [PlayerSnapshot.cpp](../Source/Persistence/PlayerSnapshot.cpp), `readSlots`/`saveSnapshot`; [Document.cpp](../Source/Persistence/Document.cpp), `readDocument`.

`readDocument` distinguishes unsupported schema/kind with `DocumentCompatibilityError`, but `readSlots` catches it as ordinary corruption. When the other slot is readable, recovery selects that older snapshot. The next save chooses the unsupported slot as its repair destination and overwrites it.

The probe wrote generations 1 and 2, changed the newer slot to an unsupported version 2 containing a future-only field, then loaded and saved. Load selected generation 1; save replaced the version-2 slot with version 1 and lost its field. This affects the calibration `--profile` path. The separate coherent world-save path already propagates compatibility errors; this is not a claim that all save formats have the defect.

Preserve unsupported versions/kinds distinctly from damaged bytes. A readable backup may support explicit read-only recovery, but must not silently authorize overwriting an incompatible slot. Reuse the world-save compatibility policy before adopting this code into PlaySession. Close with future-version/kind preservation on load/save, alongside existing corrupt/interrupted-slot recovery.

### A13 — P2: changing static support leaves resting dynamic bodies suspended

Source: [PhysicsWorld.cpp](../Source/Physics/PhysicsWorld.cpp), `replaceMeshPrepared`/`remove`; consumer: [StreamingScene.cpp](../Source/Rendering/StreamingScene.cpp), collision adoption/retirement.

A dynamic capsule settled at Y approximately `0.730` on a flat mesh. Lowering the mesh three metres left the capsule at `0.730` after two simulated seconds. Removing the mesh entirely did the same in a separate world. An explicit wake through the existing velocity API made the capsule fall: to approximately `-2.270` on lowered support, or `-18.399` with no support. Ray queries independently confirmed that the support changed or disappeared.

This is an engine integration gap. Jolt leaves neighbors asleep after body removal and provides area activation for callers. Checked the pinned revision `e77f175595e64cb44218cc9d9d56fc365ad0e36a` (`Docs/Architecture.md`, Sleeping; `BodyInterface.cpp`, RemoveBody/SetShape) and [official sleeping-body documentation](https://jrouwe.github.io/JoltPhysicsDocs/5.3.0/#sleeping-bodies). The pinned `SetShape` implementation can activate the changed nonstatic body; changing its flag alone will not wake neighbors of a static mesh.

On replacement/removal, retain old bounds and wake affected dynamic bodies at the physics owner boundary. Include new bounds for replacement and a declared contact margin; avoid globally waking every body. Streaming unload needs a policy for dynamic residents losing support: retain required support, retire/persist residents coherently, or intentionally allow falling. Stable IDs and saved deltas remain separate from transient bodies. Close with resting bodies on replaced/removed support and an unaffected distant sleeping body; keep preparation on workers and adoption/wake on the owner.

### A14 — P2: pause rewinds presentation while simulation remains stationary

Source: [FixedClock.h](../Source/Simulation/FixedClock.h), `advance`; [CalibrationRuntime.cpp](../Source/Game/CalibrationRuntime.cpp), `present`.

Pause resets the clock remainder to zero. Presentation then interpolates with alpha zero and displays the previous fixed-tick pose. A 1.5-tick movement displayed X `0.025` before pause and X `0` afterward; physics remained X `0.050` and ticks remained 1. Both applications use this runtime, including focus suspension. This is a presentation discontinuity, not a physics/save rewind.

Define pause/suspend semantics in PlaySession. Preserve the displayed pose/interpolation state, or deliberately converge to the authoritative pose with a documented policy. Resume must not accumulate wall-time catch-up. Give physics, rendering and paused picking explicit snapshot semantics. Close with fractional-tick pause/resume and focus suspension, checking displayed pose and authoritative state. No widget changes are needed.

### A15 — P2: streaming teardown can allocate and throw before releasing resources

Source: [StreamingScene.cpp](../Source/Rendering/StreamingScene.cpp), destructor; [CalibrationRuntime.cpp](../Source/Game/CalibrationRuntime.cpp), `streamingGuard`.

The destructor first clears the guard, which creates a calibration floor before stopping collision jobs and retiring streamed bodies. At the supported 2,048-body runtime capacity, guard removal threw `Physics body budget exhausted`; freeing one body allowed it to succeed. The diagnostic fills API capacity without stepping physics. Normal current streaming content alone was not shown to reach that limit.

The destructor is implicitly nonthrowing and does not catch this call. The same exception through scene destruction would terminate the process before remaining shutdown steps. This consequence follows from source and C++ exception semantics; no GPU-backed scene was destroyed or running application crashed by the probe.

Make teardown release-only and nonthrowing: disconnect callbacks without creating replacement content, cancel/join workers while dependencies live, then retire resources. Any required calibration floor recreation belongs in a separate fallible transition after releasing capacity. A generic PlaySession stop should not insert calibration objects. Close with stop at capacity, stop after failed adoption and repeated stop/start without dangling callbacks. Do not substitute a catch that leaves incomplete state.

## Source-reviewed patterns retained

- `RenderModel` prohibits copying. `TextureLease` is move-only, generation-checked and weakly references its store. Render-target replacement stages resources before retiring current ones. Retain these patterns; prior resource-budget/per-view findings remain open.
- `BoundedJobs` counts running cancelled jobs in reservations, retains valid work on rejected admission, discards cancelled completions and joins workers on shutdown. Cooperative cancellation and declared peak memory remain requirements. Missing higher-level retry is already A09, not another finding.
- Physics tokens include world ownership/incarnation, and CharacterController shares the implementation lifetime. Preserve these distinctions when adding scene/session IDs.
- Contact draining returns immediately when empty. Nonempty draining transfers/replenishes the reserved vector: a possible later allocation optimization, with no measured frame-time claim or new prerequisite now.

These are source observations, not exhaustive acceptance. Opaque prepared-shape construction, cross-thread global backend teardown and broader GPU allocation-failure injection were not exhaustively verified. They remain coverage limits, not demonstrated defects. No fresh visual, Windows or representative latency proof was collected.

## Sequence update and stop condition

R1 scene-authority corrections remain first; R2 bounded hosting and R3 render snapshots follow unchanged. Fold A12–A15 into **R4, shared play lifecycle and durability**, together with A09/A10 and relevant persistence test registration. Fix A12 before reusing/extending two-slot profiles; carry A13 into dynamic-object adoption, A14 into pause/suspend and A15 into stop/resource ownership.

There is enough evidence to begin corrections. Further audits should address each implementation batch's interfaces and acceptance cases; another broad survey is not required before R1. Temporary diagnostic fixtures add no permanent landscape, authored constraints or generated world identity. Existing identity, unload/reload and persisted-delta contracts remain controlling.

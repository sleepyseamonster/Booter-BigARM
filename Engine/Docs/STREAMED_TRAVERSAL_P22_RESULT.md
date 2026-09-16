# P22 streamed traversal integration result

Implemented and measured on macOS on 2026-09-15. This package closes the first-pass M4 streamed-world foundation. It connects the existing precision-safe world identity to application streaming and retains a representative integration corpus.

## Runtime change

Durable positions continue to use signed 64-bit X/Z region addresses plus double local coordinates. `StreamingScene` now receives the saved local-frame origin and uses it consistently for:

- render-instance and terrain offsets;
- Jolt mesh-collider placement;
- player streaming anchors and collision-readiness guards;
- nearest generated-object queries;
- save restoration in both Workbench and Player.

The player and physics simulation remain close to zero in bounded float coordinates. Generated terrain and rock IDs remain based on their absolute region addresses, never on local render or physics floats. `WorldSession` now restores valid nonzero frame origins instead of preserving but rejecting them.

This is a cell-relative large-world design. It does not continuously move the world. Seamless in-place relocation to a new local frame during uninterrupted play remains a later capability if traversal approaches the current 3.8 km local radius.

## Retained corpus

`engine_streamed_traversal_tests` runs a 21-step route around absolute region origin `(4,000,000,000,000, -4,000,000,000,000)`. It checks:

- exact nearby conversion at very large positive and negative addresses;
- repeated region-boundary travel and rapid direction reversal;
- deterministic terrain and rock identity across unload/reload;
- real Jolt collider placement and ray-query identity in the local frame;
- the 25-slot and 64 MiB stream limits;
- retirement, worker drain and return to zero streamed CPU/collision residency;
- observed route-load p50, p95 and p99 times.

The retained Mac run in [P22 evidence](../Evidence/P22-runtime/result.json) passed with 59 unique regions, 48 reload checks and 95 retired regions. Peak residency was 15 slots, 15 Jolt bodies and 15,598,670 bytes. Observed load p50/p95/p99 was 36.15/54.11/60.45 ms. These are measurements of a technical CPU/Jolt corpus, not a product budget.

The actual Player also restored that nonzero origin and produced a [Metal capture receipt](../Evidence/P22-player-far/captures/player.json) with one screenshot, zero GPU errors and the expected world origin.

## Verification and limits

The focused `region_stream`, `world_session_create`, `world_session_restore` and `streamed_traversal_corpus` checks pass. Player and Workbench compile against the same frame-aware adapter.

The Metal receipt proves this bounded technical scene only; it is not visual-quality or performance acceptance. No navigation-tile workload, uninterrupted dynamic frame relocation, hands-on gameplay feel or native Windows result is claimed. P23 owns detailed navigation tiles. Windows W01/W02 remain deferred until native hardware is available.

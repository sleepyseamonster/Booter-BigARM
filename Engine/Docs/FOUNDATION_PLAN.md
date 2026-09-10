# Foundation Plan

Status: preparation package followed by bounded technical milestones. [Requirements](./REQUIREMENTS.md), [decisions](./DECISIONS.md) and [architecture](./ARCHITECTURE.md) define the plan. No deadline or production readiness is inferred from this document.

| Stage | Scope | Completion evidence | Excluded |
|---|---|---|---|
| P0 — Preparation | Requirements, source/reference records, tools, SOPs and unresolved decisions | Workspace checks, meaningful tool tests, current environment receipt and an accurate handoff | Engine/game implementation |
| P1 — Compatibility experiment | One exact C++/SDL/bgfx/ImGui source cohort; compile and bounded technical execution | Source hashes and licenses, configuration/build/run receipts; describe Noop versus real GPU proof separately | Production architecture selection, gameplay smoke tests |
| F1 — Application foundation | Selected build process, window/input, errors, shutdown and diagnostics | Fresh native build, resource/error-path checks, repeatable instructions | World generator and complete editor |
| F2 — Rendering/authoring foundation | Mesh, perspective camera, material controls, inspector, reload and resize | GPU output and resource lifetime evidence; native Windows path when available | Canyon generation and visual parity claims from compile alone |
| F3 — Representative rock workload | Recipe document, CPU generation, mesh/material/LOD/collision preview | Determinism, saved recipe round-trip, malformed input, near/side/far review and measured workloads | Whole-world simulation and expanded gameplay |
| F4 — Streamed open space | Region lifecycle, bounded generation/upload and durable overrides | Border/identity checks, cancellation, unload/reload, restart and memory behavior | Final geography and canyons |

## Immediate Technical Experiment

Use the source lock and experiment instructions under [Research/Experiments](../Research/Experiments/README.md). The first headless check can establish link compatibility, SDL event plumbing, bgfx resource calls through its Noop backend and ImGui draw-data creation. It must not be called a rendered inspector or GPU validation.

A subsequent GPU fixture must demonstrate actual meshes and inspector output with the chosen shader pipeline. If that workflow exposes a material limitation, revisit D-06 against that specific failure. Avoid expanding Mac support into a custom backend effort.

## Budget and Stop Conditions

Preparation has a bounded dependency surface: Python standard-library tools plus one integration cohort. No global installs or account/purchase operations are required. Limit native build parallelism to keep the workstation usable. Capture failures before repairing; do not suppress warnings/errors to claim a pass. Pause a technical experiment when progress requires an unresolved product choice, a substantial compatibility fork or unrelated project changes; complete independent documentation work and record the exact remaining need.

## Milestone Closeout

Record the requirement IDs exercised, exact source and command, result, unknowns, and next bounded step. Keep user-owned visual/feel evaluation separate. The current state and continuation instructions belong in [STATUS.md](./STATUS.md), not repeated across every plan.

# Booter & BigARM — New Engine

This is the working home for the proprietary engine and regular third-person game. Initial content is the rock generator for the open Greater Wasteland. Canyons are deferred. Windows PC is the product target; Mac development is optional.

Start with [current status and next step](./Docs/STATUS.md), then [accepted direction](./Docs/DIRECTION.md) and [the working agreement](./AGENTS.md).

| Need | Durable home |
|---|---|
| What must be built and how we will judge it | [Requirements](./Docs/REQUIREMENTS.md) |
| Accepted choices, proposals and unknowns | [Decision register](./Docs/DECISIONS.md) |
| Component ownership and procedural-world contracts | [Architecture](./Docs/ARCHITECTURE.md) |
| Ordered milestones and completion evidence | [Foundation plan](./Docs/FOUNDATION_PLAN.md) |
| Language, libraries and renderer comparison | [Foundation survey](./Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md) |
| Exact dependencies, licenses and integration findings | [Research index](./Research/README.md) |
| Historical rock visuals and setting context | [Reference collection](./References/README.md) |
| Environment checks, source preparation and experiment receipts | [Preparation tools](./Tools/README.md) |
| Repeatable working procedures | [Dependency evaluation](./SOPs/EVALUATE_DEPENDENCY.md), [experiments](./SOPs/RUN_EXPERIMENT.md), [handoffs](./SOPs/SESSION_HANDOFF.md) |

The preparation package includes a small, isolated native compatibility probe. It is not the production engine, a rendered scene or a playable game. See the status page for the exact result and proof limits.

All new source, tools, assets, tests and documentation stay here. The existing Unity project remains a read-only reference for this work. `.cache/`, `build/` and `out/` hold ignored generated output; pinned acquisition instructions make dependency sources reproducible without committing their caches.

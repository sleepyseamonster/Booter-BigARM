# Booter & BigARM — New Engine

This is the working home for the proprietary engine and regular third-person game. Initial content is the rock generator for the open Greater Wasteland. Canyons are deferred. Windows PC is the product target; Mac remains the main development machine while practical.

Start with [current status and next step](./Docs/STATUS.md), then [accepted direction](./Docs/DIRECTION.md) and [the working agreement](./AGENTS.md).

The native application renders a perspective fixture and interactive inspector on Metal, with corrected geometry, a linear HDR/display pipeline, versioned inspection documents and executable-relative assets. See [the first-pass runtime foundation](./Docs/FIRST_PASS_RUNTIME.md). [Build and run it](./Docs/RUN_FOUNDATION.md), or review [the latest verified result and limitations](./Docs/OUTDOOR_GEOMETRY_RESULT.md).

| Need | Durable home |
|---|---|
| What must be built and how we will judge it | [Requirements](./Docs/REQUIREMENTS.md) |
| Accepted choices, proposals and unknowns | [Decision register](./Docs/DECISIONS.md) |
| Component ownership and procedural-world contracts | [Architecture](./Docs/ARCHITECTURE.md) |
| Complete engine scope, build order and completion evidence | [Master implementation plan](./Docs/FOUNDATION_PLAN.md), [dependency index](./Docs/ENGINE_ROADMAP.json) |
| What exists, what is missing and why the plan changed | [System audit](./Research/ENGINE_SYSTEM_AUDIT.md), [plan audit and rewrite](./Docs/ENGINE_PLAN_AUDIT.md) |
| Language, libraries and renderer comparison | [Foundation survey](./Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md) |
| Exact dependencies, licenses and integration findings | [Research index](./Research/README.md) |
| Historical rock visuals and setting context | [Reference collection](./References/README.md) |
| Transferred rock/ground textures and material mappings | [Surface library](./Assets/SurfaceLibrary/README.md) |
| Texture formats, cooking, loading and material sequence | [Texture research](./Research/TEXTURE_SYSTEM_RESEARCH.md), [implementation plan](./Docs/TEXTURE_SYSTEM_PLAN.md) |
| Environment checks, source preparation and experiment receipts | [Preparation tools](./Tools/README.md) |
| Repeatable working procedures | [Dependency evaluation](./SOPs/EVALUATE_DEPENDENCY.md), [experiments](./SOPs/RUN_EXPERIMENT.md), [handoffs](./SOPs/SESSION_HANDOFF.md) |

The [master plan](./Docs/FOUNDATION_PLAN.md) now carries the engine through reusable outdoor rendering, character calibration, rock authoring, streamed persistence, BigARM travel, a survival expedition, production content and a supported Windows candidate. The earlier compatibility/geometry results remain bounded evidence. These later capabilities are planned; see the status page for what is actually implemented.

All new source, tools, assets, tests and documentation stay here. The existing Unity project remains a read-only reference for this work. `.cache/`, `build/` and `out/` hold ignored generated output; pinned acquisition instructions make dependency sources reproducible without committing their caches.

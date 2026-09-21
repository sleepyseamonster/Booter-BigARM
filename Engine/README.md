# Booter & BigARM — Preserved Proprietary Engine

> **Preserved reference:** On 2026-09-21 the active production lane returned exclusively to the repository-root Unity project. This directory retains the proprietary C++ engine, research and evidence. Useful concepts are reimplemented deliberately through Unity rather than developed here in parallel.

This was the working home for the proprietary engine and regular third-person experiment. Its retained content includes the rock generator, procedural landscape, terrain architecture, tools and evidence. These remain historical implementation evidence rather than the current product host.

For historical inspection, start with [the retained status handoff](./Docs/STATUS.md), then [the superseded direction](./Docs/DIRECTION.md) and [the preservation agreement](./AGENTS.md).

The [retained correction and integration plan](./Docs/ENGINE_CORRECTION_PLAN.md) organized the two architecture audits into six implementation batches. [R1 scene authority](./Docs/SCENE_AUTHORITY_R1_RESULT.md), [R2 bounded live authoring](./Docs/LIVE_AUTHORING_R2_RESULT.md), [R3 render snapshots/Scene View manipulation](./Docs/RENDER_SCENE_GIZMO_R3_RESULT.md), [R4 shared play/durability](./Docs/PLAY_SESSION_DURABILITY_R4_RESULT.md) and [R5 material/occlusion/performance](./Docs/MATERIAL_OCCLUSION_PERFORMANCE_R5_RESULT.md) were implemented. R6 native Windows validation was left open when this lane was preserved.

The [2026-09-15 engine architecture audit](./Docs/ENGINE_ARCHITECTURE_AUDIT_2026-09-15.md) records current strengths, reproduced defects and the correction order before human-AI viewport integration.

[Open the Unity-compatible single-rock generator](./Docs/SINGLE_ROCK_PARITY.md), with fresh seeded compositions, all eight silhouette profiles and measured shape comparisons.

[Open the earlier captured Unity Golden Rock setup](./Docs/GOLDEN_ROCK_TRANSFER_RESULT.md), with three saved shapes and their source settings.

[Open the rock workbench with the new sky and lighting controls](./Docs/OUTDOOR_LIGHTING_L1_RESULT.md).

[Open the new rock authoring workbench](./Docs/ROCK_AUTHORING_V5_RESULT.md), with four shape families, tilted source volumes and individual formation controls.

[Open the updated wasteland and formation workbench](./Docs/UNITY_FORMATION_PARITY_RESULT.md), with streamed outcrops/piles, editable presets and original ground transitions.

[See the three generated rock examples and open the updated workbench](./Docs/ROCK_SHOWCASE.md).

The native application renders a perspective fixture and interactive inspector on Metal, with corrected geometry, a linear HDR/display pipeline, versioned inspection documents, and a [cookable texture catalog with GPU previews](./Docs/TEXTURE_PIPELINE_RESULT.md). It now also has [sun shadows and lit rock/ground materials](./Docs/SUN_AND_SURFACE_RESULT.md). The [collision and third-person foundation](./Docs/COLLISION_CHARACTER_RESULT.md) adds a capsule motor and obstruction camera. The [shared simulation runtime](./Docs/SIMULATION_FOUNDATION_RESULT.md) now supplies fixed ticks, component storage and input actions. See [the first-pass runtime foundation](./Docs/FIRST_PASS_RUNTIME.md). [Build and run it](./Docs/RUN_FOUNDATION.md), or review [the latest verified result and limitations](./Docs/OUTDOOR_GEOMETRY_RESULT.md).

| Need | Durable home |
|---|---|
| Native scene and viewport interface work | [UI/UX agent workspace](./UIUX/README.md) |
| Proprietary engine ownership, SOPs and research routing | [Game Engine agent workspace](./Agents/GameEngine/README.md) |
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
| Professional procedural terrain, canyon-ready architecture and build sequence | [Terrain architecture research](./Research/PROCEDURAL_TERRAIN_ARCHITECTURE_RESEARCH.md), [source ledger](./Research/terrain-architecture-sources.json), [implementation plan](./Docs/PROCEDURAL_TERRAIN_IMPLEMENTATION_PLAN.md) |
| Wind, dust and electrical storms; lore-only resonance and scripted earthquakes | [Planetary environment and landscape contract](./Docs/PLANETARY_ENVIRONMENT_CONTRACT.md) |
| Environment checks, source preparation and experiment receipts | [Preparation tools](./Tools/README.md) |
| Repeatable working procedures | [Dependency evaluation](./SOPs/EVALUATE_DEPENDENCY.md), [experiments](./SOPs/RUN_EXPERIMENT.md), [handoffs](./SOPs/SESSION_HANDOFF.md) |

The [master plan](./Docs/FOUNDATION_PLAN.md) now carries the engine through reusable outdoor rendering, character calibration, rock authoring, streamed persistence, BigARM travel, a survival expedition, production content and a supported Windows candidate. The earlier compatibility/geometry results remain bounded evidence. These later capabilities are planned; see the status page for what is actually implemented.

Any explicitly authorized maintenance of this preserved engine stays here. The Unity project is the active production implementation and must not be modified as part of engine-reference maintenance unless the task explicitly includes a Unity change. `.cache/`, `build/` and `out/` hold ignored generated output; pinned acquisition instructions make dependency sources reproducible without committing their caches.

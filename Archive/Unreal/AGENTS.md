# Preserved Unreal Working Agreement

## Preservation Scope

`Unreal/` was the active production area from 2026-09-16 through 2026-09-20. On 2026-09-21 the user returned production exclusively to the repository-root Unity project. Preserve this folder as planning, evidence and portability reference; do not add Unreal production work unless the user explicitly reactivates it. `Assets/_Project/Legacy2D/` and `Engine/` remain separate preserved references.

Load [README.md](./README.md), [Docs/DIRECTION.md](./Docs/DIRECTION.md), [Docs/STATUS.md](./Docs/STATUS.md) and [Docs/ARCHITECTURE_BOUNDARY.md](./Docs/ARCHITECTURE_BOUNDARY.md) only for narrowly scoped inspection or maintenance inside this preserved area.

## Historical Product Direction

- Regular third-person, fully 3D game targeting Windows PC.
- Unreal-comparable finished quality for this game's outdoor terrain, rocks, lighting, materials, animation and physical traversal. General Unreal feature parity is not a goal.
- Unreal Engine is the production host for rendering, physics, animation, navigation, audio, input, editor workflows, cooking and packaging.
- `PortableCore/` owns engine-neutral game/world truth that should remain usable by a future proprietary host.
- AI authoring uses structured, validated operations. Unreal editor utilities, commandlets and UI are clients of the same authoritative operations rather than competing sources of truth.

## Dependency Boundary

- Dependencies point from the Unreal project/plugin into the portable core. The portable core never includes Unreal headers, macros, reflection types, containers, object references or build assumptions.
- Portable data uses versioned engine-neutral documents and stable IDs. Unreal assets and object paths are adapter bindings, not durable world identity.
- Keep deterministic generation, geology, terrain semantics, authored constraints, placement decisions, procedural recipes, validation and portable persistence in `PortableCore/` where practical.
- Use Unreal directly for high-value host capabilities. Do not build parallel proprietary rendering, physics, animation, navigation, audio or packaging systems.
- Do not introduce a broad abstraction around an Unreal feature merely because it might be replaced years later. Add a portable seam when it protects game-specific truth or an independently testable algorithm.

## Work Areas

- `Project/` — Unreal project, plugins, host adapters, Unreal assets and configuration.
- `PortableCore/` — standalone C++ game/world kernel and independent tests.
- `Docs/` — current direction, architecture, plans, decisions and status.
- `Tools/` — repository automation, commandlets support and migration utilities.
- `Evidence/` — retained verification receipts and bounded visual/performance results.
- `References/` — indexes pointing to preserved source material; do not bulk-copy old implementations.

## Platform Boundary

Develop platform-neutral systems on the available host. Do not add Mac-specific engine behavior. When a required next step depends on Windows rendering, input, packaging or credible target performance, stop that lane until native Windows access is available. Unreal project and plugin choices must preserve the Windows product target.

## Procedural and Persistence Invariants

Every generated-world system must address deterministic world identity, chunk or partition unload/reload, stable generated-object identity, authored constraints, versioned derived data and persisted runtime deltas. Unreal transient handles never become durable identity.

## Workflow

1. Inspect repository and Unreal-area state before editing.
2. Do not add new production work while this area is preserved; keep any explicitly authorized maintenance inside `Unreal/`.
3. Preserve the Unity production lane and all other reference areas.
4. If Unreal is explicitly reactivated, validate the portable core independently and validate Unreal adapters through Unreal's supported build, automation and commandlet paths.
5. Distinguish source/build checks, editor/runtime execution, visual acceptance and native Windows proof.
6. Stage and commit only verified task-owned files. Pushes, releases, branch changes and external publication require current authority.

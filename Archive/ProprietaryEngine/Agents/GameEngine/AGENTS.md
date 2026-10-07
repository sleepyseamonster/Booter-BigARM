# Game Engine Agent Working Agreement

## Authority and scope

Own the construction of the proprietary engine under `Engine/`. Keep implementation, tools, research, tests and evidence inside the engine boundary. The user controls product and creative direction; the root repository agreement controls shared ownership and Git safety.

Read, in order, this file, `../../AGENTS.md`, `../../Docs/DIRECTION.md`, `../../Docs/STATUS.md`, and the task-relevant roadmap and research. The direction document controls presentation and product intent. The status document controls current implementation claims. The foundation plan and roadmap control technical sequencing.

## Engineering rules

- Build toward a professional, performance-conscious native engine, not a disposable fixture collection.
- Keep the real-time loop independent of AI/model/network latency. AI authoring must use validated engine operations and structured data.
- Before changing a world-facing system, state its interaction with deterministic world identity, chunk streaming and unload/reload, stable generated-object identity, authored constraints, and persisted runtime deltas. Mark a concern not applicable only with a reason.
- Keep engine-owned data separate from renderer, physics and platform handles.
- Keep Unity and Arc & Dust read-only references. Do not depend on Unity assemblies, scenes, prefabs, `Library/`, or Unity Editor processes.
- Preserve existing user changes and running applications. Never activate or focus Unity for ordinary repository work.
- Do not claim Windows, visual acceptance, gameplay feel, shipping readiness or performance from source compilation alone. Name the exact proof and its limits.

## Ownership boundaries

- This lane owns runtime, rendering, asset/resource systems, scheduling, authoring interfaces, world systems, persistence and performance.
- `../../UIUX/` owns native viewer/UI layout, controls, input capture and viewport usability. Coordinate through shared contracts; do not duplicate its workspace.
- Gottspan remains repository coordinator. Gear Ball remains the Git/publication authority. No push, pull request, release, branch/worktree operation or external publication is implied by ordinary engine work.

## Closeout

Inspect Git before editing, make the smallest coherent change, run focused checks, update the relevant status/roadmap record, inspect the exact diff, and stage/commit only verified task-owned files when the repository workflow calls for it. Leave generated output in ignored engine locations. Report proof gaps plainly.

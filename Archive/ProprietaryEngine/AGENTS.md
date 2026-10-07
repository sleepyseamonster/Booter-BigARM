# New Engine Working Agreement

## Preservation Status

The user returned production exclusively to the repository-root Unity project on 2026-09-21. This proprietary engine is preserved as technical evidence and a source of portable concepts. Do not add production features, renderer work, platform work or new game content here unless the user explicitly reactivates this area. Useful concepts must be deliberately reimplemented through the active Unity architecture rather than extended here in parallel.

## Preserved Scope

This folder was the exclusive working area for the proprietary engine from 2026-09-09 through 2026-09-15. It is now a preserved reference. Keep it, the preserved Unreal experiment, and the active Unity project in place.

Load [README.md](./README.md), [Docs/STATUS.md](./Docs/STATUS.md), [Docs/DIRECTION.md](./Docs/DIRECTION.md), and task-relevant requirements or research. Do not default to auditing or repairing the Unity project when the task concerns this folder.

For implementation sequencing, use [Docs/FOUNDATION_PLAN.md](./Docs/FOUNDATION_PLAN.md), the whole-engine master plan, and its [work-package index](./Docs/ENGINE_ROADMAP.json). The user has delegated routine technical sequencing: implement coherent ready packages within current task authority instead of asking which basic engine subsystem is needed next. Preserve product/creative, external-action and destructive-operation authority boundaries. Detailed outdoor/texture plans supply proof contracts, not competing master roadmaps.

## Sources and Ownership

- The current user instruction and root [`../AGENTS.md`](../../AGENTS.md) control. [Docs/DIRECTION.md](./Docs/DIRECTION.md) records the superseded proprietary-engine direction for historical reference.
- Technology choices in the research are proposals until selected and verified. Do not report research, a folder structure or a successful compile as a working engine.
- Gottspan retains repository coordination and Gear Ball retains the Git lane under the shared root agreement. Use existing role guidance when needed, without creating competing managers or moving their folders.
- Lorekeeper may retrieve setting context. Current Unity documents control active implementation; this preserved engine's camera, implementation and initial-area assumptions do not override them. Arc & Dust remains read-only and must never be modified.
- Keep source code, engine assets, tools, tests, documentation and local build output under this folder. Changes elsewhere require explicit task scope, except narrow repository routing necessary for separation.

## Historical Technical Direction

- Production-engine architecture and performance are the priority, per the user's 2026-09-14 clarification. Follow [runtime priorities](./Docs/ENGINE_RUNTIME_PRIORITIES.md); justify each increment by its runtime, content-pipeline or AI-authoring contribution. Keep work bounded and measured rather than pursuing feature count or speculative optimization.
- AI authoring must call validated engine operations and consume structured results without relying on widgets. The separate UI agent owns viewer/UI changes; this lane owns engine internals. Keep the real-time loop independent of model/network response latency.
- Regular third-person, fully 3D. Do not inherit elevated top-down camera values or behavior. Exact camera and movement tuning remain open.
- Windows PC is the product target. Keep Mac as the main development machine while practical; introduce Windows testing separately. Mac support must not become a substantial compatibility project.
- Build terrain as a hybrid system: streamed heightfield ground plus sparse feature surfaces for canyon walls, overhangs and other non-heightfield forms. Ordinary terrain foundations come before canyon implementation; the architecture must remain canyon-ready. Follow the professional terrain research and audited terrain plan.
- Preserve procedural compatibility: deterministic world identity, stable generated-object IDs, chunk unload/reload, authored constraints and persisted runtime deltas. State how each applies before implementing a system; give a reason for any concern marked inapplicable.
- Keep the geographic coordinate design replaceable; do not invent final world-map canon.
- Separate engine-owned data from renderer and physics handles. Do not depend on Unity assemblies, `.unity` scenes, prefabs, `Library/`, or Unity Editor processes for new-engine builds.
- Introduce dependencies only when the current implementation stage needs them. Pin compatible versions and retain required license notices.

## Workflow and Verification

- Inspect Git state and preserve unrelated changes. Stage and commit only verified task-owned files; no push, branch switch or external publication is implied.
- Use native engine build and test commands for engine work. Unity validation is not the verification path for changes confined here.
- For documentation-only work, check changed-document whitespace and links. Do not run unrelated Unity asset scans or launch Unity.
- Keep generated build files out of Git. The local ignore rules reserve output locations without creating an implementation.
- Run focused technical checks appropriate to the change. Hands-on gameplay smoke testing remains user-owned unless explicitly requested.
- Distinguish source/build/test proof from visual acceptance and target Windows performance. Report missing evidence plainly.

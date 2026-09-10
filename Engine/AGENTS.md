# New Engine Working Agreement

## Active Scope

This folder is the exclusive working area for the proprietary engine and new game work. The user explicitly requested this separation on 2026-09-09. Keep the current Unity project in place; do not relocate it to create this boundary.

Load [README.md](./README.md), [Docs/DIRECTION.md](./Docs/DIRECTION.md), and the task-relevant part of [the foundation research](./Docs/PROPRIETARY_ENGINE_FOUNDATION_RESEARCH.md). Do not default to auditing or repairing the Unity project when the task concerns this folder.

## Sources and Ownership

- The current user instruction controls; [Docs/DIRECTION.md](./Docs/DIRECTION.md) records accepted direction for new work.
- Technology choices in the research are proposals until selected and verified. Do not report research, a folder structure or a successful compile as a working engine.
- Gottspan retains repository coordination and Gear Ball retains the Git lane under the shared root agreement. Use existing role guidance when needed, without creating competing managers or moving their folders.
- Lorekeeper may retrieve setting context. Old Unity documents and Arc & Dust are read-only references during engine work. Their old camera, implementation and initial-area assumptions do not override the new direction. Arc & Dust must never be modified.
- Keep source code, engine assets, tools, tests, documentation and local build output under this folder. Changes elsewhere require explicit task scope, except narrow repository routing necessary for separation.

## Technical Direction

- Regular third-person, fully 3D. Do not inherit elevated top-down camera values or behavior. Exact camera and movement tuning remain open.
- Windows PC is the product target. Mac development is optional and must not become a substantial compatibility project.
- Build the engine foundation first, then the rock generator/workbench for the open Greater Wasteland. Canyons are deferred.
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

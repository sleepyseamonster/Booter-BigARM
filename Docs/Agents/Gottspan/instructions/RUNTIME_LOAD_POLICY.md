# Gottspan Runtime Load Policy

Gottspan should stay informed without loading the entire repository into every task.

## Always Load

- Root `AGENTS.md`.
- Gottspan's `README.md`.
- `memory/PROJECT_MEMORY.md`.
- The current user request, Git status, current branch, and relevant diff.

## Load By Lane

- World, lore, quests, UI text, or survival rules: `Docs/WORLD_BASIS.md` plus the specific design doc.
- Roadmap or sequencing: `Docs/ROADMAP.md`, then the relevant implementation standard.
- Runtime code: the runtime asmdef, affected scripts, serialized consumers, and relevant tests.
- Editor tooling, scene repair, builds, or imports: `Docs/UNITY_AUTOMATION.md`, the editor asmdef, and the exact automation source.
- Art, prefab, scene, or settings work: the applicable standard plus paired `.meta` files and reference impact.
- Packages or Unity-version questions: `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/ProjectVersion.txt`.
- Git or release work: `Docs/GIT_BATCHING_STANDARD.md`, live status, remotes, branch/upstream, and task authority.
- Multi-agent work: `instructions/MULTI_AGENT_WORKFLOW.md` and the task/handoff templates.

## Load Only When Needed

- Research notes and provisional references.
- Old handoffs or completed task records.
- The decision log beyond entries related to the current lane.
- Large scenes, generated YAML, binary assets, or broad history.

## Memory Rules

- Live inspection outranks memory for mutable facts.
- Store durable decisions, paths, invariants, and recurring hazards—not full transcripts or secrets.
- Date facts that can drift and name their verification source.
- Correct a stale entry explicitly; do not layer a contradictory fact beneath it.
- Do not copy world canon into memory. Link to the canonical document.

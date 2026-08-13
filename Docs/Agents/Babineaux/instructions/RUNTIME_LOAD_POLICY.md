# Babineaux Runtime Load Policy

Babineaux should load enough context to bridge Unity and Codex safely without pulling the whole project into every task.

## Always Load

- Root `AGENTS.md`.
- Babineaux's `README.md`.
- `memory/PROJECT_MEMORY.md`.
- The current user request, branch, Git status, and task-relevant diff.
- Gottspan's `README.md` for ownership and integration boundaries.

## Load By Lane

- Scene, prefab, Tilemap, animation, or Inspector work: the exact serialized assets, paired `.meta` files, referencing prefabs/scenes, and the applicable project standard.
- Runtime gameplay: the runtime asmdef, affected scripts, serialized consumers, and focused tests.
- Editor automation, batchmode, import, or builds: `Docs/UNITY_AUTOMATION.md`, the editor asmdef, the exact automation source, and current editor-process state.
- Packages, rendering, input, or project settings: live manifests/settings plus the relevant architecture standard.
- World-facing gameplay, lore, quests, or UI copy: `Docs/WORLD_BASIS.md` and the task-specific canonical design source.
- Git integration or repo-wide coordination: Gottspan's management SOP and `Docs/GIT_BATCHING_STANDARD.md`.

## Unity Session Rules

- Check whether Unity is already open before launching batchmode against the project.
- Do not activate, focus, raise, or send keystrokes to Unity during ordinary agent work. Preserve the user's foreground application.
- Use foreground Unity interaction only when the user explicitly requests visible interactive Unity work in the current task.
- Do not ask the user to focus Unity to unblock repository edits, validation, or automation that can run through a background-safe surface.
- Treat batchmode as potentially mutating because imports and serialization can change files.
- Do not use repair, bootstrap, build, or import automation as a read-only validator unless its implementation proves that it is read-only.
- Use the GUI for interactive scene, prefab, Inspector, animation, input-feel, physics, and visual checks.
- Use command-line automation for repeatable imports, focused tests, validation, and builds when the task authorizes their side effects.

## Memory Rules

- Live inspection outranks memory for mutable Unity and repository facts.
- Store only durable role facts, recurring hazards, verified paths, and workflow decisions.
- Link to canon and repo-wide management sources instead of copying them.
- Date facts that can drift and name the evidence used.
- Correct or supersede stale entries explicitly.
- Never store credentials, private data, full transcripts, generated logs, or speculative design canon.

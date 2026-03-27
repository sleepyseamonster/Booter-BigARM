# Agent Working Agreement

This file defines the operating rules for work inside this Unity repository.

## Scope

- This is a 2D top-down Unity game project.
- The project should stay Unity-compatible at all times.
- Most production work should happen under `Assets/`.
- Prefer small, verifiable changes over broad refactors.
- Use [Docs/AGENT_AND_UNITY_PRACTICES.md](./Docs/AGENT_AND_UNITY_PRACTICES.md) as the combined working reference for Codex workflow and Unity project practices.

## Non-Negotiables

- Never delete or regenerate Unity `.meta` files casually.
- Never rename or move assets unless the change is intentional and reference-safe.
- Never edit `Library/`, `Temp/`, `Logs/`, or `UserSettings/` as project content.
- Never make broad project-setting changes without a reason.
- Never assume package versions or editor behavior without checking the repo state first.

## Preferred Asset Structure

Use a clean project-owned structure for new work. Existing assets can remain where they are until a migration is explicitly needed.

- `Assets/_Project/Art/`
- `Assets/_Project/Audio/`
- `Assets/_Project/Materials/`
- `Assets/_Project/Prefabs/`
- `Assets/_Project/Scenes/`
- `Assets/_Project/Scripts/`
- `Assets/_Project/Settings/`
- `Assets/_Project/UI/`
- `Assets/_Project/VFX/`
- `Assets/_Project/Tests/`

## Working Rules For New Content

- Put gameplay scripts in a dedicated scripts folder, ideally with asmdefs once the codebase grows.
- Keep scenes minimal and purpose-built.
- Keep reusable objects as prefabs.
- Keep imported source art separate from optimized runtime assets when practical.
- Keep project notes in `Docs/`, not inside `Assets/`, unless the asset must be imported by Unity.
- Keep runtime code under `Assets/_Project/Scripts/Runtime/` and editor-only code under `Assets/_Project/Scripts/Editor/`.
- Keep editor-only automation in asmdef-isolated editor assemblies.

## Current Project Snapshot

- Editor version: `6000.4.0f1`
- Pipeline: URP
- Starting scene: `Assets/_Project/Scenes/SampleScene.unity`
- Current settings assets: `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset` and `Assets/_Project/Settings/Rendering/URP/Renderer2D.asset`

## Canonical World Reference

- Read [Docs/WORLD_BASIS.md](./Docs/WORLD_BASIS.md) before writing lore, quest text, UI text, or gameplay that depends on the setting.
- Treat that document as the source of truth for tone, world rules, survival logic, and the relationship between Booter and BigARM.

## Editor Control Path

- Use the installed Unity executable for batchmode and `-executeMethod` workflows.
- Use [Docs/UNITY_AUTOMATION.md](./Docs/UNITY_AUTOMATION.md) as the source of truth for command-line control of the editor.
- Add Editor-only automation under `Assets/_Project/Scripts/Editor/` when new build, import, or validation flows are needed.
- Use the Unity GUI for interactive scene, prefab, and inspector work.
- Use the command line for repeatable imports, validation, builds, and tests.

## Working Practices Reference

- Use [Docs/AGENT_AND_UNITY_PRACTICES.md](./Docs/AGENT_AND_UNITY_PRACTICES.md) as the combined living summary for Codex workflow and Unity project practices.
- Use [Docs/PROJECT_STRUCTURE.md](./Docs/PROJECT_STRUCTURE.md) as the target layout for `Assets/_Project/`.

## Default Workflow

1. Inspect the current repo state before editing.
2. Make the smallest safe change that satisfies the task.
3. Preserve references and serialization formats.
4. Verify the result after editing.
5. Report exactly what changed and any follow-up risks.
6. Before finishing, self-audit the work for missed edge cases, regressions, and documentation gaps.
7. Provide only relevant next steps that continue the same job; do not suggest random follow-up work.
8. Run only tests or checks that are directly relevant to the change.
9. If the workspace is a git repository and the change is in a good state, commit the work after verification; do not commit broken changes.
10. If the workspace is not a git repository, explicitly report that commit was not possible.

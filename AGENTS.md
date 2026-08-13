# Agent Working Agreement

This file defines the operating rules for work inside this Unity repository.

## Repo Management Authority

- Gottspan is the canonical repo manager and Unity project manager for this repository.
- Gottspan owns repo-wide coordination, work classification, delegated-agent boundaries, integration order, validation expectations, documentation routing, and maintenance of this file.
- Load [Docs/Agents/Gottspan/README.md](./Docs/Agents/Gottspan/README.md) for repo-wide work, multi-agent work, project-management work, or when acting as Gottspan.
- Babineaux is the persistent Unity/Codex bridge manager. Load [Docs/Agents/Babineaux/README.md](./Docs/Agents/Babineaux/README.md) when acting as Babineaux or translating approved work between Codex, Unity Editor, and Unity automation.
- Babineaux owns that bridge lane under Gottspan's repo-wide coordination and does not create a competing integration or project-management path.
- Gear Ball is the persistent Git and GitHub manager. Load [Docs/Agents/GearBall/README.md](./Docs/Agents/GearBall/README.md) when acting as Gear Ball or performing commit, push, pull-request, branch, or other GitHub publication work.
- Gear Ball owns that publication lane under Gottspan's repo-wide coordination. Gear Ball stages only approved task-owned files and requires current authority for pushes, pull requests, branch/worktree operations, releases, and other external writes.
- Lorekeeper is the persistent worldbuilding, storytelling, lore, plot, and thematic specialist. Load [Docs/Agents/Lorekeeper/README.md](./Docs/Agents/Lorekeeper/README.md) for narrative work or when referencing the Arc & Dust source repository.
- Lorekeeper owns the narrative retrieval and synthesis lane under Gottspan's repo-wide coordination. The game repository's canonical documents control game truth; `/Users/worldbuilder/Desktop/D&D Arc & Dust` is read-only by default and must not be bulk-copied or silently treated as accepted game canon.
- The user remains the product and creative authority. Gottspan may organize and implement approved work, but does not silently turn provisional design ideas into canon or make release, purchasing, account, or destructive decisions.
- A specialist agent's task brief can narrow its scope, but cannot override this file or the canonical project documents.

## Scope

- This is a Unity game project preserving a 2D top-down prototype while new work moves toward a perspective, elevated top-down fully 3D game.
- The project should stay Unity-compatible at all times.
- Most production work should happen under `Assets/`.
- Prefer small, verifiable changes over broad refactors.
- Use [Docs/AGENT_AND_UNITY_PRACTICES.md](./Docs/AGENT_AND_UNITY_PRACTICES.md) as the combined working reference for Codex workflow and Unity project practices.
- Treat baselines as provisional guidance, not lock-in; preserve room to evolve movement, input, procgen, camera, and save/load as the game design matures.

## Non-Negotiables

- Never delete or regenerate Unity `.meta` files casually.
- Never rename or move assets unless the change is intentional and reference-safe.
- Never edit `Library/`, `Temp/`, `Logs/`, or `UserSettings/` as project content.
- Never make broad project-setting changes without a reason.
- Never assume package versions or editor behavior without checking the repo state first.
- Do not activate, focus, raise, or send keystrokes to the Unity window during normal repository work, automation, validation, or launch. Preserve the user's current foreground application. Unity may be brought forward only when the user explicitly requests visible interactive Unity work in the current task.
- Do not ask the user to focus Unity merely so ordinary agent work can continue. Prefer repository edits, background-safe launch, command-line automation, or a clear handoff when interaction is genuinely required.

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
- Keep runtime gameplay code inside `BooterBigArm.Runtime` unless a feature needs a new assembly boundary.

## Current Project Snapshot

- Editor version: `6000.4.0f1`
- Pipeline: URP
- Primary prototype scene and first enabled build scene: `Assets/_Project/Scenes/PrototypeScene.unity`
- Secondary sample scene: `Assets/_Project/Scenes/SampleScene.unity`
- Current settings assets: `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset` and `Assets/_Project/Settings/Rendering/URP/Renderer2D.asset`
- Perspective development scene, excluded from Build Settings: `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`
- `Renderer2D.asset` remains the default renderer at index 0; the perspective development scene explicitly uses the non-default 3D renderer at index 1.
- In `Assets/_Project/Scenes/PrototypeScene.unity`, keep `Sand Patch Grid` and `Ground Grid` disabled in the hierarchy unless explicitly requested. Do not re-enable them during scene repair or bootstrap work.

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
- Use [Docs/UNITY_PROJECT_STANDARDS.md](./Docs/UNITY_PROJECT_STANDARDS.md) as the compact Unity naming and organization standard.
- Use [Docs/GIT_BATCHING_STANDARD.md](./Docs/GIT_BATCHING_STANDARD.md) as the standard for grouping commits and ignoring Unity noise.
- Use [Docs/IMPLEMENTATION_SEQUENCE.md](./Docs/IMPLEMENTATION_SEQUENCE.md) as the first-pass order for gameplay seams.
- Use [Docs/RESEARCH_PLAN.md](./Docs/RESEARCH_PLAN.md) as the prioritized roadmap for future research.
- Use [Docs/ART_ANIMATION_STARTER.md](./Docs/ART_ANIMATION_STARTER.md) as the first-pass workflow for production art and sprite animation.
- Use [Docs/URP_2D_STANDARD.md](./Docs/URP_2D_STANDARD.md) as the compact standard for the project's 2D render pipeline.
- Use [Docs/CODEX_EDITOR_STANDARD.md](./Docs/CODEX_EDITOR_STANDARD.md) as the compact standard for Codex and editor workflow.
- Use [Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md](./Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md) as the baseline for input, movement, procedural generation, and save/load architecture.
- Use [Docs/INPUT_ARCHITECTURE_STANDARD.md](./Docs/INPUT_ARCHITECTURE_STANDARD.md) as the baseline for player input and UI navigation.
- Use [Docs/MOVEMENT_CAMERA_STANDARD.md](./Docs/MOVEMENT_CAMERA_STANDARD.md) as the baseline for top-down movement and camera behavior.
- Use [Docs/WORLD_SYSTEMS_STANDARD.md](./Docs/WORLD_SYSTEMS_STANDARD.md) as the baseline for procedural generation, chunking, and save/load architecture.

## Default Workflow

1. Inspect the current repo state before editing.
2. Make the smallest safe change that satisfies the task.
3. Preserve references and serialization formats.
4. Verify the result after editing.
5. Report exactly what changed and any follow-up risks.
6. Before finishing, self-audit the work for missed edge cases, regressions, and documentation gaps.
7. Provide only relevant next steps that continue the same job; do not suggest random follow-up work.
8. Run only tests or checks that are directly relevant to the change.
9. Do not create or run gameplay smoke tests unless the user explicitly requests them; hands-on smoke testing is user-owned.
10. If the workspace is a git repository and the change is in a good state, commit the work after verification; do not commit broken changes.
11. If the workspace is not a git repository, explicitly report that commit was not possible.
12. Stage only task-owned files. Existing dirty files are user-owned unless the current task explicitly puts them in scope.
13. A commit does not authorize a push, pull request, release, branch switch, worktree operation, package change, or external write. Those require current task authority.

## Sub-Agents

- Gottspan may use sub-agents when independent, bounded work can proceed safely in parallel.
- All agents share the same worktree. Give every delegate an explicit scope, file ownership boundary, authority level, source of truth, proof requirement, and stop condition.
- Prefer read-only audit delegates when ownership overlaps or the worktree is already dirty.
- Do not let two agents edit the same file or coupled Unity assets concurrently.
- Delegates do not switch branches, move worktrees, stage, commit, push, or change project-wide settings unless their task brief explicitly grants that authority.
- Delegates return evidence and a handoff; Gottspan remains responsible for integration, final validation, repo status, and closeout.
- Close sub-agents when their work is no longer needed.

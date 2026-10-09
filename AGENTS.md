# Agent Working Agreement

This file defines the operating rules for the Booter & BigARM repository and routes work among preserved implementations.

## Local Workspace And Resumption

- Work only in this repository's `main` branch, tracking `origin/main`, as selected by the user's Booter-BigARM link on 2026-10-06. A later explicit user branch instruction supersedes this scope.
- Load [Docs/LOCAL_WORKSPACE.md](./Docs/LOCAL_WORKSPACE.md) when starting on this Windows checkout or restoring local tools and packages. It routes the existing agent packages, the latest publication checkpoint, and Windows preflight.
- Derive local paths from the checkout root. Retained macOS paths and prior-machine validation records do not establish installed tools or fresh validation on Windows.
- Use [Docs/REPOSITORY_ORGANIZATION.md](./Docs/REPOSITORY_ORGANIZATION.md) for file placement, transfer receipts, Blender source archives, and duplicate handling. Dated `PreviousSave` Blender files are retained versions, not the active authoring source.
- The [professional layout audit and migration plan](./Docs/Operations/Repository/LAYOUT_PLAN_2026-10-06.md) selects an active Unity core plus segregated archives, source art/data, tools, and document ownership. The [execution plan](./Docs/Operations/Repository/REORGANIZATION_EXECUTION_PLAN.md) records current paths and verified batches; the dated audit's target paths are proposals until a receipt seals their migration.
- Preserve pinned package versions and the existing five agent roles. Setup does not authorize resuming the paused Badwater implementation goal or promoting retained build evidence to current-machine proof.

## Separate Work Areas

- The repository-root Unity project is the sole active production area as of 2026-09-21. New implementation, research, tooling, documentation and generated-content conventions must serve the Unity TopDown3D production lane.
- [Archive/Unreal/](./Archive/Unreal/README.md) is a preserved reference from the 2026-09-16 through 2026-09-20 experiment. Do not add production work there unless the user explicitly reactivates Unreal; load [its agreement](./Archive/Unreal/AGENTS.md) only for narrowly scoped reference maintenance or inspection inside that folder.
- The former 2D/isometric implementation is preserved in the standalone [historical restore project](./Archive/Unity/HistoricalProject/README.md), outside active `Assets`. It must not receive production work or be restored into production without verified dependency handling.
- The proprietary C++ engine remains preserved under [Archive/ProprietaryEngine/](./Archive/ProprietaryEngine/README.md). Do not extend its renderer, platform shell or runtime. Useful concepts may be deliberately reimplemented through the active Unity architecture; do not make the archive an active second production lane.
- Keep new Unity-hosted implementation and content in the established root `Assets/_Project/`, `Docs/`, `Packages/`, and `ProjectSettings/` boundaries. `Archive/ProprietaryEngine/` and `Archive/Unreal/` are preserved inactive hosts, not parallel production lanes.
- Shared ownership, Git safety and user-authority rules still apply across the repository. Root routing links may be maintained when needed to keep the two areas discoverable.
- The user authorized verified organizational file moves on 2026-10-06. Follow the [execution plan](./Docs/Operations/Repository/REORGANIZATION_EXECUTION_PLAN.md) and its batch gates. Source hashes, asset GUIDs, links, code/load contracts, and current gameplay must remain accounted for; permission to move files does not authorize package changes, external publication, or gameplay redesign.

## Repo Management Authority

- Gottspan is the canonical repo manager and Unity project manager for this repository.
- Gottspan owns repo-wide coordination, work classification, delegated-agent boundaries, integration order, validation expectations, documentation routing, and maintenance of this file.
- Load [Docs/Agents/Gottspan/README.md](./Docs/Agents/Gottspan/README.md) for repo-wide work, multi-agent work, project-management work, or when acting as Gottspan.
- Babineaux is the persistent Unity/Codex bridge manager. Load [Docs/Agents/Babineaux/README.md](./Docs/Agents/Babineaux/README.md) when acting as Babineaux or translating approved work between Codex, Unity Editor, and Unity automation.
- Babineaux owns that bridge lane under Gottspan's repo-wide coordination and does not create a competing integration or project-management path.
- Gear Ball is the persistent Git and GitHub manager. Load [Docs/Agents/GearBall/README.md](./Docs/Agents/GearBall/README.md) when acting as Gear Ball or performing commit, push, pull-request, branch, or other GitHub publication work.
- Gear Ball owns that publication lane under Gottspan's repo-wide coordination. Gear Ball stages only approved task-owned files and requires current authority for pushes, pull requests, branch/worktree operations, releases, and other external writes.
- Lorekeeper is the persistent worldbuilding, storytelling, lore, plot, and thematic specialist. Load [Docs/Agents/Lorekeeper/README.md](./Docs/Agents/Lorekeeper/README.md) for narrative work or when referencing the Arc & Dust source repository.
- Lorekeeper owns the narrative retrieval and synthesis lane under Gottspan's repo-wide coordination. The game repository's canonical documents control game truth; `/Users/worldbuilder/Desktop/D&D Arc & Dust` is permanently reference-only for Lorekeeper and must never be modified, bulk-copied, or silently treated as accepted game canon.
- The author confirmed on 2026-10-06 that the lore checkout is absent from this Windows machine and its GitHub repository is not connected. Lorekeeper remains active for local game canon and proposals; external retrieval and reconnection are deferred. The retained Mac path is historical, not a configured Windows source.
- Blender is the persistent 3D modeling specialist. Load [Docs/Agents/Blender/README.md](./Docs/Agents/Blender/README.md) for Blender research, model authoring, and 3D asset handoff. Blender owns its agent-local instructions, SOPs, and tools under Gottspan's coordination; Unity import and validation remain with Babineaux.
- The user remains the product and creative authority. Gottspan may organize and implement approved work, but does not silently turn provisional design ideas into canon or make release, purchasing, account, or destructive decisions.
- A specialist agent's task brief can narrow its scope, but cannot override this file or the canonical project documents.

## Active Unity Scope

- The root Unity project is the active perspective, elevated top-down fully 3D implementation. Former 2D/isometric work is outside its import/compile boundary in `Archive/Unity/HistoricalProject/`.
- The project should stay Unity-compatible at all times.
- Most production work should happen under `Assets/`.
- Prefer small, verifiable changes over broad refactors.
- Use [Docs/Operations/AGENT_AND_UNITY_PRACTICES.md](./Docs/Operations/AGENT_AND_UNITY_PRACTICES.md) as the combined working reference for Codex workflow and Unity project practices.
- Treat baselines as provisional guidance, not lock-in; preserve room to evolve movement, input, procgen, camera, and save/load as the game design matures.

## Non-Negotiables

- Never delete or regenerate Unity `.meta` files casually.
- Never rename or move assets unless the change is intentional and reference-safe.
- Never edit `Library/`, `Temp/`, `Logs/`, or `UserSettings/` as project content.
- Never make broad project-setting changes without a reason.
- Never assume package versions or editor behavior without checking the repo state first.
- Do not activate, focus, raise, or send keystrokes to Unity or other app windows during repository work, automation, validation, repairs, or launch. Preserve the user's current foreground application. The user reaffirmed background-only work on 2026-10-06. Opening a scene or checking its appearance does not authorize foreground activation; bring a window forward only when the user explicitly asks to bring it forward.
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
- `Archive/Unity/HistoricalProject/` — standalone preserved Unity work, never a production content destination.

## Working Rules For New Content

- Preserve future procedural compatibility when designing gameplay seams, but Greater Wasteland currently uses its fixed authored terrain. The user deferred procedural generation on 2026-10-06; do not implement or activate it without a new approval.
- Before implementation, state how the system interacts with deterministic world identity, chunk streaming and unload/reload, stable generated-object identity, authored constraints, and persisted runtime deltas. Mark a concern not applicable only when the reason is explicit.
- Greater Wasteland currently uses its fixed, always-loaded terrain by explicit user direction. Keep new gameplay separable from that loading assumption where practical, but do not implement generation or streaming until its redesign is approved.
- Procedural-generation-first does not mean randomizing every feature. Hand-authored rules, landmarks, encounters, and narrative content should constrain, anchor, and improve the generated world.
- Put gameplay scripts in a dedicated scripts folder, ideally with asmdefs once the codebase grows.
- Keep scenes minimal and purpose-built.
- Keep reusable objects as prefabs.
- Keep imported source art separate from optimized runtime assets when practical.
- Keep project notes in `Docs/`, not inside `Assets/`, unless the asset must be imported by Unity.
- Keep runtime code under `Assets/_Project/Scripts/Runtime/` and editor-only code under `Assets/_Project/Scripts/Editor/`.
- Keep editor-only automation in asmdef-isolated editor assemblies.
- Keep production runtime gameplay code inside `BooterBigArm.TopDown3D.Runtime` unless a feature needs a new assembly boundary. `BooterBigArm.Runtime` is legacy-only.

## Current Project Snapshot

- Editor version: `6000.4.0f1`
- Pipeline: URP
- Primary production scene and only enabled build scene: `Assets/_Project/Scenes/Production/GreaterWasteland.unity`
- Greater Wasteland is the existing playable Badwater terrain, promoted on 2026-10-06. All continuing gameplay mechanics and player controls target this scene. Do not add procedural terrain, generated props, chunk streaming, or procedural save integration until the user approves a redesigned generation approach.
- `TopDown3DPrototype.unity` remains a disabled generated-world reference; do not copy its generator or save service into Greater Wasteland.
- Legacy scenes and compatible renderer settings are registered only in the standalone historical project, not production.
- Current production renderer settings: `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset` with its sole 3D renderer at index 0 as the default. Active cameras resolve that renderer.
- Preserve `Sand Patch Grid` and `Ground Grid` as disabled in the archived prototype unless the user explicitly reactivates that historical work.

## Canonical World Reference

- Read [Docs/WORLD_BASIS.md](./Docs/WORLD_BASIS.md) before writing lore, quest text, UI text, or gameplay that depends on the setting.
- Treat that document as the source of truth for tone, world rules, survival logic, and the relationship between Booter and BigARM.

## Editor Control Path

- Use the installed Unity executable for batchmode and `-executeMethod` workflows.
- Use [Docs/Engineering/UNITY_AUTOMATION.md](./Docs/Engineering/UNITY_AUTOMATION.md) as the source of truth for command-line control of the editor.
- Add Editor-only automation under `Assets/_Project/Scripts/Editor/` when new build, import, or validation flows are needed.
- Use the Unity GUI for interactive scene, prefab, and inspector work.
- Use the command line for repeatable imports, validation, builds, and tests.

## Working Practices Reference

- Use [Docs/Operations/AGENT_AND_UNITY_PRACTICES.md](./Docs/Operations/AGENT_AND_UNITY_PRACTICES.md) as the combined living summary for Codex workflow and Unity project practices.
- Use [Docs/Engineering/PROJECT_STRUCTURE.md](./Docs/Engineering/PROJECT_STRUCTURE.md) as the target layout for `Assets/_Project/`.
- Use [Docs/Engineering/UNITY_PROJECT_STANDARDS.md](./Docs/Engineering/UNITY_PROJECT_STANDARDS.md) as the compact Unity naming and organization standard.
- Use [Docs/Operations/GIT_BATCHING_STANDARD.md](./Docs/Operations/GIT_BATCHING_STANDARD.md) as the standard for grouping commits and ignoring Unity noise.
- Use [Docs/Operations/IMPLEMENTATION_SEQUENCE.md](./Docs/Operations/IMPLEMENTATION_SEQUENCE.md) as the first-pass order for gameplay seams.
- Use [Docs/Operations/RESEARCH_PLAN.md](./Docs/Operations/RESEARCH_PLAN.md) as the prioritized roadmap for future research.
- Use [Docs/Archive/Legacy2D/ART_ANIMATION_STARTER.md](Docs/Archive/Legacy2D/ART_ANIMATION_STARTER.md) as the first-pass workflow for production art and sprite animation.
- Use [Docs/Archive/Legacy2D/URP_2D_STANDARD.md](Docs/Archive/Legacy2D/URP_2D_STANDARD.md) as the compact standard for the project's 2D render pipeline.
- Use [Docs/Operations/CODEX_EDITOR_STANDARD.md](./Docs/Operations/CODEX_EDITOR_STANDARD.md) as the compact standard for Codex and editor workflow.
- Use [Docs/Engineering/GAMEPLAY_ARCHITECTURE_BASELINES.md](./Docs/Engineering/GAMEPLAY_ARCHITECTURE_BASELINES.md) as the baseline for input, movement, procedural generation, and save/load architecture.
- Use [Docs/Engineering/INPUT_ARCHITECTURE_STANDARD.md](./Docs/Engineering/INPUT_ARCHITECTURE_STANDARD.md) as the baseline for player input and UI navigation.
- Use [Docs/Engineering/MOVEMENT_CAMERA_STANDARD.md](./Docs/Engineering/MOVEMENT_CAMERA_STANDARD.md) as the baseline for top-down movement and camera behavior.
- Use [Docs/Engineering/WORLD_SYSTEMS_STANDARD.md](./Docs/Engineering/WORLD_SYSTEMS_STANDARD.md) as the baseline for procedural generation, chunking, and save/load architecture.

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

## Local-Only Blender And Terrain Sources - 2026-10-09

The author requires Blender `.blend` files and backups, plus terrain source archives under `SourceData/Terrain/`, to remain local. Do not publish them to GitHub or add Git LFS rules for them. Preserve local copies; untracking does not authorize deletion or LFS pruning. Keep native Unity assets and metadata required by Greater Wasteland under `Assets/_Project/` publishable. Follow [the source-publication policy](Docs/Operations/LOCAL_ONLY_SOURCE_POLICY.md), including its unpublished-history gate. Earlier source publication and LFS instructions are superseded for these excluded paths; historical receipts remain evidence of their original scope.

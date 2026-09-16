# Booter & BigARM

The active production implementation now lives under [Unreal/](./Unreal/README.md). Booter & BigARM is a regular third-person game targeting Windows PC, with Unreal Engine supplying production rendering and platform systems while a portable game/world core preserves engine-neutral procedural logic and durable data.

## Work Areas

| Area | Status | Purpose |
|---|---|---|
| [Unreal/](./Unreal/README.md) | **Active** | Unreal host, portable core, AI authoring integration, tools and current planning |
| [Engine/](./Engine/README.md) | Preserved reference | Prior proprietary C++ engine, evidence and research; no continuing production implementation |
| Repository-root Unity project | Preserved reference | Former Unity 3D implementation and source material |
| `Assets/_Project/Legacy2D/` | Preserved legacy reference | Isolated original 2D implementation |

Read [AGENTS.md](./AGENTS.md) for repository routing and [Unreal/AGENTS.md](./Unreal/AGENTS.md) before new work. Do not move or delete the preserved implementations during the Unreal transition.

## Preserved Unity Baseline

### Current Baseline

- Unity Editor: `6000.4.0f1`
- Render pipeline: URP
- Core systems already present: Input System, 2D Animation, Aseprite import, PSD import, SpriteShape, Tilemap Extras, Timeline, Visual Scripting
- Current editable game content lives under `Assets/`

### Open In Unity

1. Open the project root in Unity Hub.
2. Use the editor version listed above if possible.
3. Let Unity reimport the project before editing gameplay content.

### Unity Layout

- `Assets/` - game content, scenes, settings, scripts, prefabs, art, and imported assets
- `Packages/` - package manifest and lockfile
- `ProjectSettings/` - Unity project configuration
- `Docs/` - working notes and initialization references for this repo

### Unity Working Rules

- Treat `.meta` files as required Unity source files.
- Do not move or rename assets without updating references intentionally.
- Prefer all new game content inside `Assets/`.
- Keep Unity-facing changes small and deliberate.
- Avoid changes to `ProjectSettings/` unless they are needed for the project setup.

### Unity Reference Documents

- Read [AGENTS.md](./AGENTS.md) before making changes.
- Read [Docs/Agents/Gottspan/README.md](./Docs/Agents/Gottspan/README.md) for repo management, project management, and multi-agent coordination.
- Read [Docs/PROJECT_STATUS.md](./Docs/PROJECT_STATUS.md) for the evidence-based implementation pulse and next decisions.
- Read [Docs/PROJECT_BASELINE.md](./Docs/PROJECT_BASELINE.md) for the current project snapshot.
- Read [Docs/WORLD_BASIS.md](./Docs/WORLD_BASIS.md) for the setting, tone, and design canon.
- Read [Docs/UNITY_AUTOMATION.md](./Docs/UNITY_AUTOMATION.md) for the current command-line and editor-control path.
- Read [Docs/AGENT_AND_UNITY_PRACTICES.md](./Docs/AGENT_AND_UNITY_PRACTICES.md) for the combined Codex + Unity working rules.
- Read [Docs/PROJECT_STRUCTURE.md](./Docs/PROJECT_STRUCTURE.md) for the target `Assets/_Project/` layout.
- Read [Docs/UNITY_PROJECT_STANDARDS.md](./Docs/UNITY_PROJECT_STANDARDS.md) for naming and organization conventions.
- Read [Docs/GIT_BATCHING_STANDARD.md](./Docs/GIT_BATCHING_STANDARD.md) for how to group commits and ignore Unity noise.
- Read [Docs/IMPLEMENTATION_SEQUENCE.md](./Docs/IMPLEMENTATION_SEQUENCE.md) for the first-pass order of gameplay seams.
- Read [Docs/RESEARCH_PLAN.md](./Docs/RESEARCH_PLAN.md) for the prioritized research roadmap.
- Read [Docs/URP_2D_STANDARD.md](./Docs/URP_2D_STANDARD.md) for the 2D URP rendering baseline.
- Read [Docs/CODEX_EDITOR_STANDARD.md](./Docs/CODEX_EDITOR_STANDARD.md) for Codex and editor workflow.
- Read [Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md](./Docs/GAMEPLAY_ARCHITECTURE_BASELINES.md) for input, movement, procedural generation, and save/load baselines.
- Read [Docs/INPUT_ARCHITECTURE_STANDARD.md](./Docs/INPUT_ARCHITECTURE_STANDARD.md) for the gamepad-first input baseline.
- Read [Docs/MOVEMENT_CAMERA_STANDARD.md](./Docs/MOVEMENT_CAMERA_STANDARD.md) for the top-down movement and camera baseline.
- Read [Docs/WORLD_SYSTEMS_STANDARD.md](./Docs/WORLD_SYSTEMS_STANDARD.md) for the procedural generation and save/load baseline.

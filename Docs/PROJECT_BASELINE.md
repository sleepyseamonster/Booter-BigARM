# Project Baseline

This document captures the current Unity project state so future changes can be made against a stable reference.

## Engine And Packages

- Unity Editor: `6000.4.0f1`
- Project uses URP
- Package set includes 2D animation, Aseprite, PSD import, SpriteShape, tilemap extras, input system, Timeline, and Visual Scripting

## Current Files Of Interest

- `Assets/_Project/Scenes/Production/GreaterWasteland.unity` — primary production scene and only enabled build scene; promoted from playable Badwater on 2026-10-06 with existing controls and mechanics retained. Procedural generation is deferred pending redesign.
- `Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity` — preserved generated-world reference, disabled in Build Settings.
- `Assets/_Project/Scripts/Runtime/TopDown3D/` — production runtime assembly.
- `Assets/_Project/Legacy2D/Scenes/` — preserved 2D prototype and sample scenes, disabled in Build Settings.
- `Assets/_Project/Legacy2D/Scripts/Runtime/` — preserved 2D runtime assembly.
- `Assets/_Project/Legacy2D/Scripts/Editor/` — preserved 2D editor tooling in an isolated editor assembly.
- `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset`
- `Assets/_Project/Settings/Rendering/URP/IsometricRenderer.asset` — default production 3D renderer at index 1.
- `Assets/_Project/Legacy2D/Settings/Rendering/URP/Renderer2D.asset` — preserved 2D renderer at index 0.
- `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`
- `Assets/_Project/Legacy2D/Settings/Profiles/DefaultVolumeProfile.asset`
- `Assets/_Project/Settings/Rendering/URP/UniversalRenderPipelineGlobalSettings.asset`

## Implemented Prototype Systems

The isolated legacy runtime tree includes:

- Gameplay, system, and UI input adapters.
- Physics-backed player movement and camera targeting.
- Deterministic world identity and prototype generation settings.
- Versioned save schema, JSON persistence, and save/load coordination.
- Survival state and debug/HUD surfaces.
- Inventory, item definitions, harvesting, world pickups, and dust-canister state.
- BigARM command, threat-signal, save-data, and AI-controller surfaces.

This inventory proves code and asset presence, not player-facing completeness or design quality. Use [PROJECT_STATUS.md](./PROJECT_STATUS.md) for the current evidence/verification distinction.

## Current Folder State

- `Assets/_Project/Scenes/`
- `Assets/_Project/Scripts/Runtime/TopDown3D/`
- `Assets/_Project/Legacy2D/`
- `Assets/_Project/Settings/Input/`
- `Assets/_Project/Settings/Rendering/URP/`

## Notes For Future Work

- Keep this baseline updated when the project gains a new main scene, a formal folder migration, or a major rendering/input change.
- If gameplay systems are added, document their source folders here.
- If the world canon changes, update [WORLD_BASIS.md](./WORLD_BASIS.md) first and then align any dependent docs.
- If editor automation changes, update [UNITY_AUTOMATION.md](./Engineering/UNITY_AUTOMATION.md) with the exact command-line entry points.
- If `Assets/_Project/` changes materially, align [PROJECT_STRUCTURE.md](./Engineering/PROJECT_STRUCTURE.md) with the new layout.


## Organization checkpoint — 2026-10-06

Verified organization is documented in [implementation evidence](./Operations/Repository/IMPLEMENTATION_EVIDENCE_2026-10-06.md). Production is Scenes/Production/GreaterWasteland; GameplaySetup is the terrain-free rebuild template; the generated-world scene is an imported disabled reference under Scenes/Reference/GeneratedWorld. Legacy2D/isometric work is the standalone frozen Archive/Unity/HistoricalProject. Blender sources/resources are SourceArt/Blender/Studies, art tools Tools/Art/Blender, repository tools Tools/Repository, and bounded recovered terrain inputs SourceData/Terrain. External lore remains absent/disconnected. Current owner terrain and radial changes remain separately owned; organization proof does not validate those concurrent feature batches.

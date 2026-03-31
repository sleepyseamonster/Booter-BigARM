# Project Baseline

This document captures the current Unity project state so future changes can be made against a stable reference.

## Engine And Packages

- Unity Editor: `6000.4.0f1`
- Project uses URP
- Package set includes 2D animation, Aseprite, PSD import, SpriteShape, tilemap extras, input system, Timeline, and Visual Scripting

## Current Files Of Interest

- `Assets/_Project/Scenes/SampleScene.unity`
- `Assets/_Project/Scripts/Runtime/Prototype/`
- `Assets/_Project/Scripts/Editor/Prototype/`
- `Assets/_Project/Settings/Rendering/URP/UniversalRP.asset`
- `Assets/_Project/Settings/Rendering/URP/Renderer2D.asset`
- `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`
- `Assets/_Project/Settings/Profiles/DefaultVolumeProfile.asset`
- `Assets/_Project/Settings/Rendering/URP/UniversalRenderPipelineGlobalSettings.asset`

## Current Folder State

- `Assets/_Project/Scenes/`
- `Assets/_Project/Scripts/Runtime/Prototype/`
- `Assets/_Project/Scripts/Editor/Prototype/`
- `Assets/_Project/Settings/Input/`
- `Assets/_Project/Settings/Profiles/`
- `Assets/_Project/Settings/Rendering/URP/`
- `Assets/_Project/Settings/Templates/`

## Notes For Future Work

- Keep this baseline updated when the project gains a new main scene, a formal folder migration, or a major rendering/input change.
- If gameplay systems are added, document their source folders here.
- If the world canon changes, update [WORLD_BASIS.md](./WORLD_BASIS.md) first and then align any dependent docs.
- If editor automation changes, update [UNITY_AUTOMATION.md](./UNITY_AUTOMATION.md) with the exact command-line entry points.
- If `Assets/_Project/` changes materially, align [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) with the new layout.

# Project Baseline

This document captures the current Unity project state so future changes can be made against a stable reference.

## Engine And Packages

- Unity Editor: `6000.4.0f1`
- Project uses URP
- Package set includes 2D animation, Aseprite, PSD import, SpriteShape, tilemap extras, input system, Timeline, and Visual Scripting

## Current Files Of Interest

- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/DefaultVolumeProfile.asset`

## Current Folder State

- `Assets/Scenes/`
- `Assets/Settings/`
- `Assets/Settings/Scenes/`

## Notes For Future Work

- Keep this baseline updated when the project gains a new main scene, a formal folder migration, or a major rendering/input change.
- If gameplay systems are added, document their source folders here.
- If the world canon changes, update [WORLD_BASIS.md](./WORLD_BASIS.md) first and then align any dependent docs.
- If editor automation changes, update [UNITY_AUTOMATION.md](./UNITY_AUTOMATION.md) with the exact command-line entry points.
- If `Assets/_Project/` changes materially, align [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) with the new layout.

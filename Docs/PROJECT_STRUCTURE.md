# Project Structure

This document defines the target layout for `Assets/_Project/` so new work lands in predictable places and Unity editor/runtime boundaries stay clean.

## Goals

- Keep game code, art, and scene content easy to find.
- Keep editor-only code separate from runtime code.
- Keep Unity importable assets under `Assets/` and project guidance under `Docs/`.
- Avoid mixing prototype content with stable project-owned layout.

## Target Layout

```text
Assets/_Project/
  Art/
  Audio/
  Legacy2D/
    Art/
    Prefabs/
    Scenes/
    Scripts/
      Runtime/
      Editor/
    Settings/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Runtime/
    Editor/
  Settings/
    Input/
    Profiles/
    Rendering/
      URP/
    Templates/
  Tests/
    Runtime/
    Editor/
  UI/
  VFX/
```

## Folder Responsibilities

- `Art/` - source art, concept references, and production art assets.
- `Legacy2D/` - preserved, self-contained 2D prototype content. It is reference and maintenance territory, not a production destination.
- `Audio/` - music, SFX, and audio import sources.
- `Materials/` - runtime materials and shader-related project assets.
- `Prefabs/` - reusable gameplay and UI prefabs.
- `Scenes/` - gameplay scenes, test scenes, and scene variants.
- `Scripts/Runtime/` - gameplay code that must compile into the player.
- `Scripts/Editor/` - Unity editor tooling, build automation, validation, and import helpers.
- `Settings/` - project-specific ScriptableObjects, renderer configs, and other shared settings assets.
- `Settings/Input/` - input action assets and other input-related configuration.
- `Settings/Profiles/` - volume profiles and other profile-style shared assets.
- `Settings/Rendering/URP/` - URP assets, renderer data, and render pipeline globals.
- `Settings/Templates/` - scene templates and reusable scene bootstrap assets.
- `Tests/` - test scenes, test fixtures, and test support assets.
- `Tests/Runtime/` - runtime tests and fixtures.
- `Tests/Editor/` - editor tests and fixtures.
- `UI/` - UI sprites, prefabs, layouts, and supporting assets.
- `VFX/` - visual effect assets and supporting content.

## Working Rules

- Create new runtime code under `Scripts/Runtime/` unless the task is explicitly editor-only.
- Create new editor automation under `Scripts/Editor/` and keep it in an Editor-only asmdef.
- If tests become real, mirror the runtime/editor split under `Tests/Runtime/` and `Tests/Editor/`.
- Keep scenes minimal and purpose-driven.
- Keep reusable gameplay objects in prefabs, not scene-only copies.
- Keep imported source art separate from optimized runtime art when practical.
- Keep documentation in `Docs/` rather than inside `Assets/`, unless Unity must import the file.
- Put all new production gameplay under the normal type-first roots; do not add new systems to `Legacy2D/`.
- Share an asset with production only when it is genuinely presentation-neutral. Legacy-only 2D art, scenes, scripts, prefabs, renderer data, and settings stay inside `Legacy2D/`.

## Migration Rule

- The 2D prototype has completed its reference-safe move to `Assets/_Project/Legacy2D/`.
- When moving assets, preserve `.meta` files and let Unity handle the reference updates whenever possible.
- Update [AGENTS.md](../AGENTS.md) and [PROJECT_BASELINE.md](./PROJECT_BASELINE.md) when the structure changes in a way that affects future work.

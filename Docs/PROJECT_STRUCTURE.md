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
  UI/
  VFX/
```

## Folder Responsibilities

- `Art/` - source art, concept references, and production art assets.
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
- `UI/` - UI sprites, prefabs, layouts, and supporting assets.
- `VFX/` - visual effect assets and supporting content.

## Working Rules

- Create new runtime code under `Scripts/Runtime/` unless the task is explicitly editor-only.
- Create new editor automation under `Scripts/Editor/` and keep it in an Editor-only asmdef.
- Keep scenes minimal and purpose-driven.
- Keep reusable gameplay objects in prefabs, not scene-only copies.
- Keep imported source art separate from optimized runtime art when practical.
- Keep documentation in `Docs/` rather than inside `Assets/`, unless Unity must import the file.

## Migration Rule

- Existing content can stay in place until there is a clear reason to move it.
- When moving assets, preserve `.meta` files and let Unity handle the reference updates whenever possible.
- Update [AGENTS.md](../AGENTS.md) and [PROJECT_BASELINE.md](./PROJECT_BASELINE.md) when the structure changes in a way that affects future work.

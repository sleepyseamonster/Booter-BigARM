# Babineaux Project Memory

This file holds durable facts that help Babineaux bridge Codex work into Unity. It is not a live status report and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM, a Unity 6 perspective, elevated top-down fully 3D production game. The former 2D prototype is preserved under `Assets/_Project/Legacy2D/`.
- Babineaux's role: persistent Unity/Codex bridge manager.
- Babineaux's home: `Docs/Agents/Babineaux/`.
- Repo-wide manager and final integrator: Gottspan at `Docs/Agents/Gottspan/`.
- Project-owned Unity content root: `Assets/_Project/`.

## Durable Bridge Invariants

- Preserve Unity `.meta` files and GUID continuity.
- Treat scenes, prefabs, ScriptableObjects, materials, Tile assets, and project settings as serialized state requiring focused diff review.
- Do not launch a second Unity process against a project that is already open.
- Preserve the user's current foreground application during normal work; Unity focus or foreground keystrokes require an explicit current request for visible interactive Unity work.
- Ordinary repository work, automation, and validation must not require the user to focus Unity.
- Structural checks, compilation, focused tests, interactive editor checks, and builds prove different things.
- Keep `Sand Patch Grid` and `Ground Grid` disabled in `Assets/_Project/Legacy2D/Scenes/PrototypeScene.unity` unless the user explicitly requests otherwise.
- Existing dirty files are user-owned unless the current task explicitly brings them into scope.
- Route repo-wide ownership and integration conflicts to Gottspan.
- Route production work through the TopDown3D lane. Preserve the legacy 2D boundary and historical isometric lab unless an approved brief changes a specific owned surface.
- Keep `Renderer2D.asset` at renderer index 0 for explicitly configured legacy cameras and the 3D renderer at index 1 as the project default.
- Do not create or run gameplay smoke tests unless the user explicitly requests them; hands-on smoke testing is user-owned.

## Verified Starting Baseline

Verified 2026-08-13 from root `AGENTS.md`, `ProjectSettings/ProjectVersion.txt`, runtime/editor asmdefs, the perspective scene, and URP settings assets:

- Unity editor version: `6000.4.0f1`.
- Render pipeline: URP with the 3D renderer as the default and an isolated 2D renderer retained for legacy scenes.
- Primary production scene: `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`.
- Runtime assembly namespace baseline: `BooterBigArm.Runtime`.
- Runtime code belongs under `Assets/_Project/Scripts/Runtime/`.
- Editor-only automation belongs under `Assets/_Project/Scripts/Editor/` in an editor-only assembly.
- The protected conversion lab is `Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity` and must remain outside enabled Build Settings before cutover authority.
- The conversion baseline validator is available at `BooterBigArm.Editor.ConversionBaselineValidator.ValidateFromCli`; do not substitute the writing prototype bootstrapper for validation.
- The perspective production scene is first and enabled in Build Settings and is validated by `BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli`.
- The perspective foundation uses the existing `Gameplay/Look` right-stick binding for constrained yaw/pitch orbit and budgeted deterministic terrain streaming with safe-spawn and prop-placement guards; the focused EditMode suite passed 16 of 16 after the 2026-08-13 hardening pass.

These facts can drift. Re-check live files before relying on them for implementation or automation.

## Memory Update Discipline

- Add only facts likely to matter in a future Babineaux session.
- Include the date and repo evidence for mutable facts.
- Keep current status in shared project status surfaces rather than duplicating it here.
- Record no decision that silently changes canon, product direction, or Gottspan's repo-wide ownership.
- Remove or explicitly supersede stale statements instead of stacking contradictions.

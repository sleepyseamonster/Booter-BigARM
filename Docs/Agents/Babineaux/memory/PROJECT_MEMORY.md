# Babineaux Project Memory

This file holds durable facts that help Babineaux bridge Codex work into Unity. It is not a live status report and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM, a Unity 6 project whose current implementation is 2D top-down and whose accepted planning direction is top-down, isometric-style 2.5D using 3D runtime assets.
- Babineaux's role: persistent Unity/Codex bridge manager.
- Babineaux's home: `Docs/Agents/Babineaux/`.
- Repo-wide manager and final integrator: Gottspan at `Docs/Agents/Gottspan/`.
- Project-owned Unity content root: `Assets/_Project/`.

## Durable Bridge Invariants

- Preserve Unity `.meta` files and GUID continuity.
- Treat scenes, prefabs, ScriptableObjects, materials, Tile assets, and project settings as serialized state requiring focused diff review.
- Do not launch a second Unity process against a project that is already open.
- Structural checks, compilation, focused tests, interactive editor checks, and builds prove different things.
- Keep `Sand Patch Grid` and `Ground Grid` disabled in `PrototypeScene` unless the user explicitly requests otherwise.
- Existing dirty files are user-owned unless the current task explicitly brings them into scope.
- Route repo-wide ownership and integration conflicts to Gottspan.
- Route all conversion work through Gottspan's canonical plan at `Docs/3D_CONVERSION_AUDIT_AND_CHECKLIST.md`; preserve the current 2D baseline until an approved conversion brief changes a specific owned surface.

## Verified Starting Baseline

Verified 2026-08-12 from root `AGENTS.md`, `ProjectSettings/ProjectVersion.txt`, the runtime/editor asmdefs, and the URP settings assets:

- Unity editor version: `6000.4.0f1`.
- Render pipeline: URP with a 2D renderer.
- Primary prototype scene: `Assets/_Project/Scenes/PrototypeScene.unity`.
- Runtime assembly namespace baseline: `BooterBigArm.Runtime`.
- Runtime code belongs under `Assets/_Project/Scripts/Runtime/`.
- Editor-only automation belongs under `Assets/_Project/Scripts/Editor/` in an editor-only assembly.

These facts can drift. Re-check live files before relying on them for implementation or automation.

## Memory Update Discipline

- Add only facts likely to matter in a future Babineaux session.
- Include the date and repo evidence for mutable facts.
- Keep current status in shared project status surfaces rather than duplicating it here.
- Record no decision that silently changes canon, product direction, or Gottspan's repo-wide ownership.
- Remove or explicitly supersede stale statements instead of stacking contradictions.

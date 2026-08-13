# Gottspan Project Memory

This file retains durable repo-management facts. It is not a live status dashboard and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM, a Unity 6 survival crafting game whose protected working implementation is 2D top-down and whose accepted new-work direction is a perspective, elevated top-down game using a fully 3D runtime world and assets.
- Active conversion foundation: `Docs/TOP_DOWN_3D_FOUNDATION_PLAN.md`, owned by Gottspan under the user's creative and product authority. The older isometric conversion plan and lab remain historical comparison evidence.
- Canonical world reference: `Docs/WORLD_BASIS.md`.
- Canonical roadmap: `Docs/ROADMAP.md`.
- Project-owned Unity content root: `Assets/_Project/`.
- Runtime assembly: `BooterBigArm.Runtime` under `Assets/_Project/Scripts/Runtime/`.
- Editor assembly: `BooterBigArm.Editor` under `Assets/_Project/Scripts/Editor/`.
- Repo and Unity project manager: Gottspan, rooted at `Docs/Agents/Gottspan/`.

## Durable Invariants

- Preserve Unity `.meta` files and GUID continuity.
- Keep `Sand Patch Grid` and `Ground Grid` disabled in `PrototypeScene` unless explicitly requested.
- Treat architecture standards as provisional baselines that may evolve with the design.
- Keep design canon, implementation fact, roadmap priority, and validation evidence distinct.
- All sub-agents share one worktree; overlapping edits are unsafe.
- Existing dirty files are user-owned until explicitly brought into scope.
- Preserve the current 2D implementation and the isometric lab as comparison baselines until the user accepts a perspective cutover candidate; foundation implementation does not authorize cutover or legacy cleanup.
- Hands-on smoke testing is user-owned. Do not create or run smoke tests unless the user explicitly requests them.

## Verified Manager Baseline

Verified 2026-08-13 from live repo files and Unity 6000.4.0f1 validation:

- Unity editor version is `6000.4.0f1`.
- URP is the rendering pipeline and the project uses the 2D renderer assets under `Assets/_Project/Settings/Rendering/URP/`.
- `PrototypeScene.unity` and `SampleScene.unity` are enabled in Build Settings, with `PrototypeScene` first.
- CLI player-build automation exists at `BooterBigArm.Editor.BuildAutomation.BuildFromCli`.
- Prototype scene build/repair tooling exists in `PrototypeSceneBootstrapper`; those operations write project content and are not validation-only commands.
- The protected conversion lane has a non-mutating validator at `Assets/_Project/Scripts/Editor/Validation/ConversionBaselineValidator.cs` and focused EditMode tests under `Assets/_Project/Tests/Editor/`.
- The parallel conversion lab is `Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity`; it explicitly selects renderer index 1 and remains outside enabled Build Settings.
- `Renderer2D.asset` remains the default renderer at index 0; `IsometricRenderer.asset` is the non-default conversion renderer at index 1.
- The separate perspective scene is `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`; it uses renderer index 1 and remains absent from Build Settings.
- Perspective runtime code is isolated in `BooterBigArm.TopDown3D.Runtime` under `Assets/_Project/Scripts/Runtime/TopDown3D/`.
- The generated foundation includes centralized input, a 3D Rigidbody motor, constrained right-stick perspective camera orbit, deterministic budget-streamed mesh terrain, walkable safe-spawn selection, collision-aware props, and compact simple-follow BigARM behavior.
- `TopDown3DPrototypeValidator` passes and the focused EditMode suite reports 16 of 16 passing after the 2026-08-13 traversal-hardening pass.

## Known Management Gaps

- Right-stick camera feel, physical-gamepad response, extended chunk traversal, player grounding across terrain, and BigARM behavior acceptance remain with the user.
- Survival-loop parity, harvesting/items, save migration, final companion mechanics, production assets, and biome/landmark generation remain deferred.
- Player-build proof remains intentionally unavailable because the perspective prototype is excluded from Build Settings until a later gate.

These are tracked constraints, not automatic permission to build tooling. Address them only when they are required by an approved task or roadmap slice.

## Update Discipline

- Add only facts likely to help future sessions.
- Cite the repo path or command that verified a mutable fact and date it.
- Replace or explicitly supersede stale statements.
- Never store credentials, private user data, full transcripts, or speculative canon here.

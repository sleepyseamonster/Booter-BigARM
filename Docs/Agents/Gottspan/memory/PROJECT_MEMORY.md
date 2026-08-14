# Gottspan Project Memory

This file retains durable repo-management facts. It is not a live status dashboard and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM, a Unity 6 survival crafting game whose primary production direction is perspective, elevated top-down, and fully 3D. The former 2D implementation is isolated legacy reference content.
- Active conversion foundation: `Docs/TOP_DOWN_3D_FOUNDATION_PLAN.md`, owned by Gottspan under the user's creative and product authority. The older isometric conversion plan and lab remain historical comparison evidence.
- Canonical world reference: `Docs/WORLD_BASIS.md`.
- Canonical roadmap: `Docs/ROADMAP.md`.
- Project-owned Unity content root: `Assets/_Project/`.
- Production runtime assembly: `BooterBigArm.TopDown3D.Runtime` under `Assets/_Project/Scripts/Runtime/TopDown3D/`; preserved legacy runtime assembly: `BooterBigArm.Runtime` under `Assets/_Project/Legacy2D/Scripts/Runtime/`.
- Editor assembly: `BooterBigArm.Editor` under `Assets/_Project/Scripts/Editor/`.
- Repo and Unity project manager: Gottspan, rooted at `Docs/Agents/Gottspan/`.

## Durable Invariants

- Preserve Unity `.meta` files and GUID continuity.
- Keep `Sand Patch Grid` and `Ground Grid` disabled in the legacy `PrototypeScene` unless explicitly requested.
- Treat architecture standards as provisional baselines that may evolve with the design.
- Keep design canon, implementation fact, roadmap priority, and validation evidence distinct.
- All sub-agents share one worktree; overlapping edits are unsafe.
- Existing dirty files are user-owned until explicitly brought into scope.
- Preserve the isolated 2D implementation and isometric lab as reference baselines. TopDown3D has accepted production authority, but that does not authorize deleting legacy content.
- BigARM is a companion and synergistic part of Booter's mechanics, not a mobile base. He may act separately but always keeps a true world position and must physically traverse to regroup; no distance, recall, unloaded-terrain, or recovery path may snap him to Booter. See `Docs/BIGARM_COMPANION_STANDARD.md`.
- Hands-on smoke testing is user-owned. Do not create or run smoke tests unless the user explicitly requests them.

## Verified Manager Baseline

Verified 2026-08-13 from live repo files and Unity 6000.4.0f1 validation:

- Unity editor version is `6000.4.0f1`.
- URP is the rendering pipeline; the 3D renderer at index 1 is the project default and the isolated legacy 2D renderer remains at index 0.
- `TopDown3DPrototype.unity` is first and enabled in Build Settings. The legacy `PrototypeScene.unity` and `SampleScene.unity` remain registered but disabled.
- CLI player-build automation exists at `BooterBigArm.Editor.BuildAutomation.BuildFromCli`.
- Prototype scene build/repair tooling exists in `PrototypeSceneBootstrapper`; those operations write project content and are not validation-only commands.
- The protected conversion lane has a non-mutating validator at `Assets/_Project/Scripts/Editor/Validation/ConversionBaselineValidator.cs` and focused EditMode tests under `Assets/_Project/Tests/Editor/`.
- The parallel conversion lab is `Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity`; it explicitly selects renderer index 1 and remains outside enabled Build Settings.
- `Renderer2D.asset` remains available at index 0 for explicitly configured legacy cameras; `IsometricRenderer.asset` is the production default at index 1.
- The perspective production scene is `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity`; it uses renderer index 1 and is the enabled build entry point.
- Perspective runtime code is isolated in `BooterBigArm.TopDown3D.Runtime` under `Assets/_Project/Scripts/Runtime/TopDown3D/`.
- The generated foundation includes centralized input, a 3D Rigidbody motor, constrained right-stick perspective camera orbit, deterministic budget-streamed mesh terrain, walkable safe-spawn selection, collision-aware props, compact simple-follow BigARM behavior, and a gameplay-readable layered dust-atmosphere controller.
- `TopDown3DDustAtmosphere` is the perspective lane's fog, close-haze, and atmosphere post-processing owner. It supplies seeded regional variation plus smooth `TopDown3DDustZone` overrides; `PerpetualTwilightSun` remains the light/ambient/sky owner and supplies twilight brightness without writing competing fog state.
- `TopDown3DPrototypeValidator` passes and the focused EditMode suite reports 16 of 16 passing after the 2026-08-13 traversal-hardening pass.

## Known Management Gaps

- Right-stick camera feel, physical-gamepad response, extended chunk traversal, player grounding across terrain, BigARM behavior acceptance, and final dust/haze visual tuning remain with the user.
- Survival-loop parity, harvesting/items, save migration, final companion mechanics, production assets, and biome/landmark generation remain deferred.
- Player-build proof remains unavailable until a dedicated build-validation task is authorized and completed.

These are tracked constraints, not automatic permission to build tooling. Address them only when they are required by an approved task or roadmap slice.

## Update Discipline

- Add only facts likely to help future sessions.
- Cite the repo path or command that verified a mutable fact and date it.
- Replace or explicitly supersede stale statements.
- Never store credentials, private user data, full transcripts, or speculative canon here.

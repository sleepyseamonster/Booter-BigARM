# Gottspan Project Memory

This file retains durable repo-management facts. It is not a live status dashboard and never replaces inspection.

## Durable Identity

- Project: Booter & BigARM, a Unity 6 2D top-down survival crafting game.
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

## Verified Manager Baseline

Verified 2026-08-12 from live repo files:

- Unity editor version is `6000.4.0f1`.
- URP is the rendering pipeline and the project uses the 2D renderer assets under `Assets/_Project/Settings/Rendering/URP/`.
- `PrototypeScene.unity` and `SampleScene.unity` are enabled in Build Settings, with `PrototypeScene` first.
- CLI player-build automation exists at `BooterBigArm.Editor.BuildAutomation.BuildFromCli`.
- Prototype scene build/repair tooling exists in `PrototypeSceneBootstrapper`; those operations write project content and are not validation-only commands.
- The Unity Test Framework package is installed, but the project has no committed test source files or test asmdefs yet.

## Known Management Gaps

- There is no dedicated non-mutating Unity validation entry point.
- There are no committed runtime/editor test assemblies yet.
- Interactive scene feel and player-loop quality still require Unity GUI proof.

These are tracked constraints, not automatic permission to build tooling. Address them only when they are required by an approved task or roadmap slice.

## Update Discipline

- Add only facts likely to help future sessions.
- Cite the repo path or command that verified a mutable fact and date it.
- Replace or explicitly supersede stale statements.
- Never store credentials, private user data, full transcripts, or speculative canon here.

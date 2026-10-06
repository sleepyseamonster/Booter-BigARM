# Babineaux Tool Inventory

This directory holds Babineaux-owned, agent-local helpers and the inventory for Unity-facing automation created through Babineaux.

## Placement Rules

- Put read-only repository inspection helpers, report generators, and agent workflow utilities in this directory.
- Put reusable Unity Editor automation under `Assets/_Project/Scripts/Editor/` so Unity imports it in the correct editor-only assembly.
- Put runtime gameplay code under `Assets/_Project/Scripts/Runtime/`; it is product code, not a Babineaux-local tool.
- Document every Unity-facing tool here with its canonical path, entry point, whether it mutates project content, prerequisites, and validation evidence.
- Do not store binaries, generated logs, credentials, machine-specific caches, or copies of canonical project files here.

## Admission Checklist

Before adding a helper or automation entry point, confirm that it:

1. Solves a repeated or task-required workflow rather than adding speculative infrastructure.
2. Has one canonical owner and does not duplicate existing automation.
3. Makes side effects explicit, especially imports, saves, scene repair, prefab writes, settings changes, and builds.
4. Fails clearly and preserves project state when interrupted.
5. Has a documented invocation and a focused proof path.

## Current Inventory

### Editor playability audit

- **Report:** [Editor playability audit, 2026-10-06](../../../Evidence/EDITOR_PLAYABILITY_AUDIT_2026-10-06.md).
- **Structural entry point:** `Assets/_Project/Scripts/Editor/Validation/RepositoryPlayabilityAudit.cs`, `BooterBigArm.Editor.RepositoryPlayabilityAudit.ValidateBothFromCli`. It opens both scene assets for inspection without saving, invokes the existing validators, performs terrain readback, and warns about missing Death Valley ground markers.
- **Terrain readback:** `Assets/_Project/Scripts/Editor/Validation/BadwaterTerrainReadbackAudit.cs`. Only the read-only methods from the retained isolated terrain builder are mirrored here; its optional `-badwaterReport` writes a JSON report to the explicitly selected output path.
- **Runtime checks:** `Assets/_Project/Tests/Editor/ScenePlayabilityAuditTests.cs`, filter `BooterBigArm.Tests.ScenePlayabilityAuditTests`. Marked `Explicit`; run only for an authorized playability check. Tests temporarily change in-memory background/input routing settings, drive simulated devices, and restore them. A diagnostic case temporarily adds missing terrain markers in Play mode; no scene is saved.
- **Progress logging:** `Assets/_Project/Tests/Editor/AuditTestCallbacks.cs` registers test callbacks only with `-repositoryAuditProgress`.
- **Proof:** Both existing structural scene validators and terrain readback pass. Original game-world runtime controls pass. Original Death Valley grounding fails; the in-memory marker diagnostic passes. See the report for the exact test boundaries and incomplete broad-suite result.
- **Prerequisites:** Pinned Unity editor, restored packages, and exclusive project ownership; preserve foreground focus. No standalone executable or new package is required.

### `launch-unity.sh`

- **Purpose:** Open this project in its pinned Unity editor without activating it, avoid a duplicate target-project session, and wait for the real editor window while preserving the user's foreground application.
- **Path:** `Docs/Agents/Babineaux/tools/launch-unity.sh`
- **Invocation:** `Docs/Agents/Babineaux/tools/launch-unity.sh`
- **Read-only check:** `Docs/Agents/Babineaux/tools/launch-unity.sh --status`
- **Explicit foreground mode:** `Docs/Agents/Babineaux/tools/launch-unity.sh --foreground`. Agents may use this only when the user explicitly requests visible interactive Unity work in the current task. It can bring Unity forward and restore the primary `TopDown3DPrototype` scene from `Untitled`.
- **Side effects:** Starting Unity can import project assets and update ignored editor state. Foreground mode may change the active editor scene but does not save it.
- **Prerequisites:** The project-pinned Unity version must be installed. macOS Accessibility permission is required for window inspection and foreground mode.
- **Safety behavior:** Defaults to background launch, reuses an already-open target project without raising it, refuses a windowless stale Unity process, and does not terminate any Unity process automatically.

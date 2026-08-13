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

### `launch-unity.sh`

- **Purpose:** Open this project in its pinned Unity editor without activating it, avoid a duplicate target-project session, and wait for the real editor window while preserving the user's foreground application.
- **Path:** `Docs/Agents/Babineaux/tools/launch-unity.sh`
- **Invocation:** `Docs/Agents/Babineaux/tools/launch-unity.sh`
- **Read-only check:** `Docs/Agents/Babineaux/tools/launch-unity.sh --status`
- **Explicit foreground mode:** `Docs/Agents/Babineaux/tools/launch-unity.sh --foreground`. Agents may use this only when the user explicitly requests visible interactive Unity work in the current task. It can bring Unity forward and restore `PrototypeScene` from `Untitled`.
- **Side effects:** Starting Unity can import project assets and update ignored editor state. Foreground mode may change the active editor scene but does not save it.
- **Prerequisites:** The project-pinned Unity version must be installed. macOS Accessibility permission is required for window inspection and foreground mode.
- **Safety behavior:** Defaults to background launch, reuses an already-open target project without raising it, refuses a windowless stale Unity process, and does not terminate any Unity process automatically.

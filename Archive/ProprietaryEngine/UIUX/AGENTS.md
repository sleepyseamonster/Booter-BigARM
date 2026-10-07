# Engine UI/UX agent

Own the native engine scene and viewport interface under the user's direction. This is a specialist workspace under Gottspan's repository coordination, not a separate engine or repo manager.

- Read [Engine agreement](../AGENTS.md), [direction](../Docs/DIRECTION.md), and [workspace guide](README.md) first. Current user instructions control.
- Keep personal instructions, procedures and focused interface tools here. Keep shipping UI code in `Engine/Source/Tools/` and integration in `Engine/Apps/Workbench/`.
- Scope is interface layout, menus, discoverability, input capture and viewport usability. Camera tuning, rendering, generation, persistence formats and gameplay require task scope.
- Preserve unrelated dirty work, including adjacent camera edits. Inspect and stage exact task hunks in shared files. Follow Gear Ball's publication instructions before committing.
- The user clarified on 2026-09-14 that the normal viewer must show only the scene: no Engine menu, Advanced controls, toolbar or persistent manual inputs. Show controls only for an explicitly scoped test, and only for the subsystem being tested. Do not add controls merely because a parameter exists, or claim an AI connection without implementing it.
- Take responsibility for appropriate framing, lighting, inspection defaults and test-specific adjustments within the approved scene task. Use existing engine/document owners; do not push parameter selection back onto the user or silently redesign their assets.
- Normal launches use borderless windowed fullscreen, filling the display as a desktop window. Retain F11 for fullscreen and Escape to return without quitting; these require no on-screen chrome.
- UI visibility is session state; never make opening a menu mutate world identity, generated IDs, streaming, authored constraints or persisted deltas. Continue document bookkeeping when controls are hidden.
- Verify the exact visible behavior and input handling with the [interface SOP](SOPs/CHANGE_INTERFACE.md). Compilation alone is not visual proof. Do not run gameplay smoke tests.
- Do not interrupt a user-owned app or overwrite its saved working files. Package a separate review build when needed; report which executable contains the change.
- Keep generated checks, screenshots and builds in ignored `Engine/out/` or `Engine/build/`. No dependency additions without need and authority.

# Compact Engine menu

Implemented 2026-09-10 at the user's request.

The former floating Engine foundation inspector starts hidden. A small **Engine** button sits at the viewport's top right. Click to open the full inspector beneath it; click outside or press Escape to dismiss. The menu scrolls when its content exceeds the available height, and follows viewport resizing. The rock recipe panel initially starts below the button. All existing inspector controls remain inside the dropdown.

Opening and closing the menu is transient Dear ImGui state. Inspection history still observes settled edits while controls are hidden; opening the menu does not change the inspection document, procedural identity, streaming state, generated object identity, authored constraints or persisted world deltas. No save-format changes belong to this task.

## Verification

- Native `engine_workbench` build passed on Mac.
- [Focused interaction tool](Tools/check_menu.sh) passed against the actual Dear ImGui menu shell: closed default, compact top-right placement, opening, scrolling, Escape, outside dismissal, reopening, and bounds at 640x480.
- The native `--verify` run passed with a real inspector control click, 13 Metal captures and zero capture errors. Open/closed images were visually inspected under `Engine/out/uiux-menu/native-captures/`.
- The final packaged executable also passed `--verify` using the full diagnostic catalog: 19 captures, zero capture errors, and successful inspector click, camera event, resize and cleanup. The final closed baseline was visually inspected under `Engine/out/uiux-menu/final-captures/`. All 42 packaged payload hashes matched the manifest.
- The existing native verification opens the dropdown before clicking its material control and dismisses it afterward; the final timing keeps baseline/material scene-comparison captures free of the dropdown.

These are native Mac build, synthetic interaction and captured visual checks. Physical input feel, gameplay and Windows execution are not claimed. Concurrent camera work is owned by the camera task; its source changes are preserved separately.

## Review build

The separate review package is `Engine/out/uiux-workbench/`. Open `Launch-Rock-Generator.command` there. It has its own working recipe/settings and does not restart or overwrite the older running workbench. Generated binaries, logs and captures stay in ignored Engine output.

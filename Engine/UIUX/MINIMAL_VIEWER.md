# Scene-only viewer

2026-09-14. Supersedes the earlier minimal-menu design. The user does not want a menu, Advanced controls or persistent manual inputs. Controls are appropriate only when required by a specific test.

Normal launches show only the scene in a maximized window. Existing inspection documents supply framing and lighting. Orbit/pan/zoom remain available without overlays. F11 toggles fullscreen and Escape leaves fullscreen without quitting. The UI agent takes responsibility for task-appropriate scene adjustments through the existing engine/document owners; the user need not choose a permanent set of sliders.

There is no normal-viewer menu to discover or configure. Explicit testing sessions use `--test-controls rock`, `engine`, `animation`, or `terrain`. Only the named subsystem's controls are submitted. Rock, animation and terrain modes require their corresponding asset arguments. Controls do not persist into the next ordinary launch. For example, a rock-generator test may expose recipe edits, Apply/Undo and export; it does not expose unrelated animation or terrain diagnostics.

No generator, world identity, generated object identity, authored constraints, streaming lifetime or persisted delta format changes are included. Inspection history continues to observe settled edits without rendering UI. No AI service or transport was added by this presentation change.

## Open

Use [the scene-only launcher](../out/scene-viewer/Launch-Rock-Generator.command). It contains the latest Golden Rock assets and separate working files. Earlier packages and user data remain intact. The previous `ai-scene-viewer` package is superseded by this one.

## Verification

Native Mac build passed. The existing `BoundedJobs.h` indentation warning is outside this UI change. The [Metal UI check](../out/scene-only-review/native/result.json) captures the scene at normal and small window sizes, explicitly asserts zero UI vertices in both, and enables only rock test controls for the edit/apply/undo portion. Five captures, zero GPU errors and the three recipe assertions passed without advancing simulation. The scene-only captures were visually inspected.

Fullscreen transitions and Windows execution remain unverified. Generated logs, captures and packages live in ignored Engine output. This is an interface and native rendering check, not a gameplay smoke test.

# Minimal scene viewer

2026-09-14. The user selected AI-controlled operation with very few manual inputs.

The scene fills the window, which launches maximized. Only the small top-right Engine button is visible by default. Its compact dropdown contains four controls: Frame scene, Exposure, Fullscreen, and Advanced controls. Frame scene uses the existing rock/formation bounds when a rock is loaded; otherwise it recenters the inspection view. It is disabled in character mode. F11 toggles fullscreen; Escape dismisses an active menu or leaves fullscreen. Escape no longer quits the workbench.

Advanced controls reveals the existing engine inspector, rock/formation authoring panel, model animation panel and terrain panel where applicable. Turning it off hides them together. Hidden panels retain their state. Inspection history still observes settled edits while hidden. No generator, world identity, stable object identity, authored constraint, streaming lifetime or persisted delta format changes are included. Viewer visibility and fullscreen are transient, not scene data.

This is the interface foundation for AI-controlled work. It adds no AI service, command transport or false connection indicator. Existing documents and programmatic runtime paths remain available; a complete live AI command interface is separate work.

## Open

Use [the current viewer launcher](../out/ai-scene-viewer/Launch-Rock-Generator.command). It contains the latest Golden Rock library and its own working settings/recipe. Existing packages and saved user data remain intact. Manual authoring and preset switching remain under Engine → Advanced controls.

## Evidence

- Native Mac workbench build passed. An existing `BoundedJobs.h` indentation warning remains outside this UI change.
- [Real ImGui interaction checks](Tools/check_menu.sh) pass: closed default, compact/advanced menu dimensions, viewport bounds, opening, Escape/outside dismissal, scrolling, frame/fullscreen requests, and advanced toggle on/off.
- [Native Metal UI receipt](../out/uiux-viewer-review/native/result.json): five captures, zero GPU errors, actual draft/apply/undo checks pass, simulation did not advance. The closed scene, compact dropdown, small-window scene and advanced panel were visually inspected.
- [General fixture receipt](../out/uiux-viewer-review/fixture/verification.json): inspector click, camera event, resizing, resource rebuild/cleanup and 13 captures passed.
- Generated logs, captures and package receipts stay under ignored `Engine/out/`. The first capture attempt used a model path absent from the Golden Rock package and stopped; the corrected run uses the actual package assets.

Fullscreen's SDL integration compiled and its UI request was exercised in ImGui; OS fullscreen transitions, physical input feel and Windows execution were not visually verified. The native captures use deterministic non-focusable windows rather than a gameplay session.

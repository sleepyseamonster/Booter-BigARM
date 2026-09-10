# Scene camera controls

Open `Engine/out/rock-showcase-camera/Launch-Rock-Generator.command` for the updated native rock workbench. Begin camera drags in the viewport, outside interface panels.

| Input | Scene action |
| --- | --- |
| Alt (Windows) / Option (Mac) + left-drag | Orbit around the current view pivot |
| Space + left-drag | Pan: slide the view sideways/up/down |
| Middle-drag | Pan |
| Right-drag | Orbit, retained shortcut |
| Mouse wheel | Zoom |
| F / Recenter view | Clear pan and return the pivot to the subject |

Pan speed follows viewing distance and field of view. Eye and pivot translate together, so pan does not rotate the camera or move the rock. Subsequent orbit rotates around the translated pivot. Inspection settings retain the offset; old inspection documents default to zero. Reset view clears the offset along with the other camera settings. A completed drag produces one inspection-history entry.

Drags that start on interface controls remain owned by the interface. Scene drags continue when crossing a panel, and release or focus loss ends the gesture. Idle keyboard-navigation focus cannot consume the first viewport drag; active text editing still owns keyboard input. Character mode retains its existing Space jump/right-drag behavior.

## Scope and acceptance

Done: the requested scene gestures work through the native input path, persist as inspection state, and ship in a usable local package. World seed, generated-object IDs, authored constraints, chunk reloads and runtime deltas are unaffected: this is editor view state, not world/object movement. No origin shifting, camera fly mode, dependency change or gameplay expansion is included.

## Verification

- [Native state cases](../Evidence/SCENE-camera-state/result.json): orbit/pan math, modifier/button routing, UI drag ownership, focus loss and camera bounds.
- [Inspection cases](../Evidence/SCENE-camera-documents/result.json): saved offset round-trip and old-document compatibility, alongside existing document/edit/export assertions.
- [Native Mac Metal pass](../Evidence/SCENE-camera-final/captures/camera.json): Alt-left orbit, Space-left pan, middle pan and F recenter through ImGui input and SDL motion, five captures, zero GPU errors. Simulation remained paused. Orbit and pan captures were visually inspected.
- Workbench and player build succeeded. [Package inventory](../Evidence/SCENE-camera-final/package.json) verifies all 42 payload hashes. The package also contains the concurrent top-right Engine-menu work, which is owned by the separate UI task.

An initial native pass exposed idle ImGui keyboard-navigation focus blocking the first Alt drag. The input gate was corrected and the final pass above succeeded. Superseded diagnostic captures are kept only in ignored local cache. This establishes native Mac technical behavior; physical Windows input and personal camera feel are not claimed.

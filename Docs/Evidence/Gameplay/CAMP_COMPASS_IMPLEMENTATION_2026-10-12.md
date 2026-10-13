# Simple camp compass implementation

2026-10-12. Implements the user's approved [compass proposal](../../Design/Gameplay/CAMP_COMPASS_BRAINSTORM.md) in the active Greater Wasteland HUD.

## Result

- A thin ring surrounds the existing algae consumption controls. A dark sphere represents north / +Z / darkward; a pale sun with four rays represents south / -Z / lightward.
- Booter's physical facing rotates the symbols. Camera orbit does not rotate the instrument. Its top means ahead of Booter.
- An outlined amber triangle points toward the established Starting Camp. Within 2 horizontal metres it becomes a stable hollow arrival circle; the arrow returns beyond 3 metres.
- Camp-relative coordinates derive from the actual established site's X/Z position. They are available from the presenter but are not permanently displayed. Unity world coordinates are unchanged.
- Consumption icons remain upright. One layout envelope supplies the safe-area inset, canister spacing, and dust-caption position. At the reference 168-unit pad size the ring radius is 116, its outer envelope radius is 130 including sun rays, the canister starts at 236, and the dust label starts at Y=-74. Removing/disabling the compass restores legacy spacing.

## Source and lifecycle

Runtime additions are `TopDown3DCompassNavigation`, `TopDown3DCompassLayout`, `TopDown3DCompassGraphic`, and `TopDown3DCompassHud`, under `BooterBigArm.TopDown3D.Runtime`. The graphic requires a CanvasRenderer and does not intercept pointer input.

The existing HUD bootstrap installs the compass only in Greater Wasteland. The waypoint controller supplies an explicit binding after camp configuration. A bounded half-second source lookup handles unresolved initialization or destroyed sources; there are no per-frame scene searches. The presenter checks the existing world/site IDs, discovery state, and established site's stable ID. Unavailable destinations hide the arrow instead of falling back to world zero. A temporary invalid facing preserves the last valid heading. A replacement player clears stale heading/arrival state.

No scenes, prefabs, package versions, project settings, terrain, travel rules, inventory transactions, or save formats were edited. Terrain remains fixed and always loaded. No generated objects, generation identity, or persisted runtime deltas are introduced. The portable recovery kit does not change the destination.

## Verification

An isolated existing validation checkout at `D:/BooterBigArmValidation/StartingCamp20261011-01` was refreshed from the current tracked source snapshot and overlaid with the exact task-owned files. The production Editor remained open and was not focused, interrupted, or used for scene-changing tests. The isolated candidate omits native production terrain; this proof does not validate terrain or production-scene loading.

- Unity 6000.4.0f1 import/compilation succeeded in the candidate.
- **96/96 focused EditMode cases passed** in `compass-sealed-tests.xml`, covering the compass, existing action D-pad, algae controller/state/input, and waypoint route/camp integration.
- New compass cases cover facing/bearing, angle wrap, horizontal origin offsets, invalid data, opposite pole positions, arrival hysteresis, passive/idempotent installation, explicit lifecycle messages, camera independence, replacement binding, reference-scene exclusion, and actual algae/dust layout restoration.
- Geometry checks require the sun's inward rays to clear a coincident arrow/arrival cue. Static rendering caught the initially missing CanvasRenderer; the corrected graphic has a regression assertion.
- Static UI captures use `BooterBigArm.Editor.TopDown3DCompassPreview.CaptureFromCli`, guarded to isolated validation paths. They render north-facing, east-facing, arrival, and coincident-bearing states at 1920×1080, 1280×720, 1024×768, and 960×540. Renderer features are temporarily disabled and restored for camera-space UI capture; production remains overlay UI. Captures do not run actor simulation or save scenes.
- Preview input sources are deliberately inactive/unbound, so existing algae prompts show “No kit.” That is a preview-source limitation, not a change to gameplay availability.
- Task-owned source hashes, `.meta` GUID uniqueness, documentation links, and staged whitespace are checked before commit.

Candidate logs and full/cropped captures remain under its `Logs/CompassPreview` and `compass-*.log` / `compass-*.xml` paths. The production CLI's read-only status reported no compilation failure; retained errors were older Pipeline request timeouts, not compass errors. No console was cleared.

The first test attempt passed 94/95; the sole failure assumed runtime MonoBehaviour enable/disable messages would automatically run during EditMode. The harness now invokes those messages explicitly. Subsequent runs include the algae integration case and all pass.

## Acceptance limits

This is deliberately simple procedural UI geometry, with immediate updates and no new art dependency. Hands-on readability, turning feel, and real-terrain contrast remain user-owned acceptance. No gameplay smoke test, PlayMode run, player build, persistence test, push, or release was performed.

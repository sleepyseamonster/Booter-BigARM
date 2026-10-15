# Two camera option implementation evidence

October 15, 2026. Implements the [camera contract](../../../../Engineering/CAMERA_OPTIONS.md) following the [audit and research](../../../../Design/Gameplay/CAMERA_OPTIONS_AUDIT_AND_RESEARCH_2026-10-15.md) and the subsequent chat plan audit.

## Delivered behavior

Camp Loop's next normal menu installation exposes Settings → Camera. Elevated retains the authored 22 m view. Third person uses a 5 m, root-relative behind-character rig with free orbit, optional delayed recentering, adjustable X/Y sensitivity, invert Y and vertical FOV. The selected mode is a saved local preference; the effective mode can temporarily remain Elevated when a requested view is obstructed or camp placement owns framing.

The existing camera and AudioListener remain the only rendering/audio output. The elevated component stays enabled for riding availability and shared framing. Cinemachine 3.1.5 evaluates a private third-person pose; the coordinator applies it only after the shared project clearance solver accepts it. No Brain competes for output, and no tag/layer changes are used to approximate actor or mount exclusions. BadwaterCameraRange remains the final range owner.

Installation is explicitly enabled only for CampLoopTesting by the new profile's scene allowlist. GreaterWasteland and GameplaySetup are not enabled. No scene is saved, replaced or rebuilt by this installation; the author-owned Camp scene and its uncommitted content remain outside this commit. Existing instantiated menus are not rebuilt during the author's active session.

## Executed proof

- [Final result index](focused-tests.json): 80 passing checks, zero unresolved failures. Coverage includes 18 new camera contracts, two new authored-menu contracts, two new synthetic runtime-camera fixtures, ten existing clearance checks, five fixed-terrain coverage checks, 25 menu/settings checks, 14 riding checks, three movement/orbit/pan checks and one runtime menu/clock fixture.
- [Combined batch run](combined-tests.xml): 78 checks passed; two new runtime-camera fixtures initially failed setup because their synthetic router was enabled before receiving an InputActionAsset. [The corrected fixture rerun](runtime-fixture-rerun.xml) passes both checks with a real cloned, device-isolated input asset. Assertions were retained. The result index replaces only these two earlier setup failures with their final passing results.
- The runtime-camera fixtures verify real LateUpdate and disable callbacks, third-person acquisition, restoration to Elevated, root following, and unchanged target position in an empty synthetic scene. They do not load production or perform gameplay traversal.
- The runtime menu fixture verifies held-submit fencing, fresh submit/back, and continued world clock/physics in its disposable fixture. Its batch XML remains valid through the Play Mode domain reload that interrupts a synchronous CLI HTTP response.
- [Prefab record preservation](prefab-record-proof.json): all 725 original records remain, 128 records are added, and only two original RectTransform records change. Original records retain their order in the sealed file; the appended new records have equivalent Unity YAML with trailing whitespace removed. The original prefab metadata GUID remains unchanged.
- Unity compilation and focused execution use the pinned 6000.4.0f1 Editor in `D:/BooterBigArmValidation/CameraOptions20261015-01`. The marker `.camera-options-validation` bounds authoring and scene-opening validation. The sealed prefab is reimported and the structural scene/UI/profile validator runs against the final asset.

## Static comparison

[Elevated framing](elevated.png) and [third-person framing](third-person.png) are 960 × 540 offscreen Edit Mode renders from the isolated graphics-enabled Editor. Both use the same saved Camp location. The views show the Edit Mode capsule proxy, not animated Booter or runtime equipment. Existing blur and optical settings are preserved. These captures establish the framing difference only.

## Remaining acceptance and recovery

Camera feel, physical controller/mouse response, mounted traversal, runtime equipment visibility, distant terrain cost and target-hardware performance remain author-owned acceptance. Static captures and synthetic fixture timings are not a Player frame-rate benchmark. No gameplay smoke traversal, Player build, production promotion, package upgrade, push or release was performed.

To withdraw the study, remove CampLoopTesting from the profile allowlist and start a new normal session. To return to the elevated view during the study, select Elevated and Apply. Keep the shared runtime camera component enabled; disabling it still means camera availability is lost to riding. Removing only the coordinator restores the elevated component's output path, with the existing last-safe-position handling when a preferred elevated shot is obstructed.

The [source manifest](source-manifest.json) identifies the exact implementation files and candidate hashes. Original audit snapshots in the parent folder remain dated audit evidence and are not overwritten with implementation state.

# Camera options audit and third person camera research

October 15, 2026. Scope: retain the elevated camera and evaluate a second, conventional behind-character view. This is an audit and proposed implementation direction; no gameplay camera, scene, package, input binding or project setting changed in this task.

Implementation continuation: the [current camera contract](../../Engineering/CAMERA_OPTIONS.md) records the approved study's refined ownership and installation design, with [executed evidence](../../Evidence/Gameplay/CameraOptions20261015/Implementation/README.md). The research and snapshots below retain their audit-time scope.

The recommended study uses **two camera rigs sharing the existing Main Camera**. Keep the current elevated rig, and evaluate the installed Cinemachine Third Person Follow component for the new close view. A small mode coordinator must give exactly one rig control of the output. A separate rendering camera is possible, but would require more reference, effects and audio integration without providing a necessary benefit for these two selectable views.

## Current implementation

The [saved source snapshot](../../Evidence/Gameplay/CameraOptions20261015/source-snapshot.json) records working-tree hashes and settings. The [read-only Editor snapshot](../../Evidence/Gameplay/CameraOptions20261015/live-camera.json) records the open Camp Loop scene in Edit Mode, clean and not compiling. It does not establish rendered appearance or controller feel. Other uncommitted work was present and remains separately owned.

| Surface | Audited state | Consequence |
| --- | --- | --- |
| Production | GreaterWasteland is the only enabled build scene | Production adoption must be explicit after the bounded study |
| Current scenes | GreaterWasteland, CampLoopTesting and GameplaySetup each serialize one Camera, one AudioListener and one custom camera rig | Preserve these output identities and references |
| Camera framing | Distance 22 m, target offset 1.15 m, pitch 50 degrees, yaw 40 degrees, vertical FOV 48 degrees | The recent closer framing is present in all three scenes and in the live Camp Loop rig |
| Orbit limits | Pitch 26 to 65 degrees; yaw wraps; stick rates 120 degrees/s yaw and 70 degrees/s pitch | This is an elevated orbit rig, with no automatic behind-character alignment |
| Follow | LateUpdate, SmoothDamp time 0.14 s | Position damping is shared across axes; rotation is directly input-driven |
| Scouting pan | Left trigger plus right stick; maximum 12 m; outward 12.6 m/s, return 37.8 m/s | This translated framing target needs a mode-specific policy |
| Lens | Custom rig reapplies FOV and 0.1 m near plane; BadwaterCameraRange owns far range, up to 8000 m | Another rig must not compete with those writes |
| Installed stack | Unity 6000.4.0f1, URP 17.4.0, Input System 1.19.0, Cinemachine 3.1.5 in manifest and materialized package | No package upgrade is needed for the study |
| Assembly | Production runtime asmdef does not reference Unity.Cinemachine | A production Cinemachine adapter would require an intentional assembly reference |
| Defaults | Newly added TopDown3DCameraRig components still default to 25 m; existing clearance tests exercise that default | The 22 m scene tuning is not a global component-default change |

The [camera rig](../../../Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DCameraRig.cs) combines target tracking, orbit input, scouting pan, riding framing, lens application and obstruction resolution. It requires a Camera on its own object. Disabling or replacing it therefore affects more than follow distance.

### Clearance strengths and gaps

Current code uses a 0.35 m sphere cast and endpoint overlap check, ignores triggers and Booter's hierarchy, and excludes the ridden mount while riding framing returns. Saturated nonallocating queries fall back to complete queries. An obstructed origin receives a bounded search along the boom. Snaps and normal follow updates share clearance resolution. A fully enclosed boom reports failure and can retain a previously safe position after checking it again.

The October 14 [lighting and camera audit](CAMP_LOOP_LIGHTING_CAMERA_AUDIT_2026-10-14.md) predates these repairs. Its claims about the minimum-distance clamp overriding nearby obstructions, missing snap clearance and missing saturation handling are historical. The serialized minimumDistance field is now only validated; the actual resolver permits a shorter safe distance. Do not copy the old findings into a new implementation brief.

Remaining design gaps are immediate obstruction compression and release, no alternative lateral/vertical shot search when the whole boom is blocked, and no swept transition-path guarantee between individually clear poses. Scouting clearance follows the panned framing target, which does not guarantee Booter's visibility. Close third-person framing also needs explicit near-plane clearance and a policy when Booter's mesh fills the view. These are source findings, not observed visual failures.

## Gameplay and rendering connections

| Owner | Existing connection | Requirement for two options |
| --- | --- | --- |
| Player motor and laboratory locomotion | Serialized cameraBasis; planar projection of camera forward/right; interpolated Rigidbody; motor turns toward locomotion | Preserve the movement basis and avoid camera/body yaw feedback |
| Legger follower | Camera basis supplies an initial fallback trailing direction | Keep the same output transform |
| Riding | Controller directly stores TopDown3DCameraRig and calls SetRidingFrame/ReleaseRidingFrame | Route framing through the active mode; preserve mount exclusion and dismount handoff |
| Interaction | Queries physical range, Booter's facing and actor-to-target line of sight | Looking at an object does not currently aim or select it; retain that rule in the first study |
| Camp placement | Uses Camera.main; input router has separate Camp modes | Preserve the output camera and define whether placement temporarily requests elevated framing |
| Compass | Resolves this scene's active tagged MainCamera | One stable output avoids rebinding ambiguity |
| Dust, sky and modular optics | Explicit Camera references and camera-local bindings; optics lookup rejects multiple configured owners in one scene | Keep the existing bindings; duplicating the camera with its effects can invalidate selection |
| Terrain rendering | Uses actual active-camera frusta, height bounds and shadow contributors | Low-angle views can increase visible terrain and rendering cost; terrain and collision remain resident |
| UI and controls | Router owns modal suppression, cursor capture and neutral-input fences | Feed both camera modes through the router; do not add raw device polling in the second rig |

The Look action includes pointer delta, but the router separates gamepad look from direct mouse delta. Maintain the existing distinction: mouse displacement is degrees per pixel; stick deflection is an angular rate. The current camera uses scaled delta time for stick orbit, pan, riding blending and follow damping. Camp acceleration and pause therefore need an explicit timing contract.

There is no camera-view action in the inspected input asset. Right-stick press is assigned to PingBigArmDanger, left trigger to scouting pan, and shoulders to existing gameplay or modal commands. An action name in the asset does not establish that its feature is currently active. Use a Camera View menu choice for the first study; choose a rebindable shortcut only after checking active-action conflicts and the controls settings registry.

## Research findings

Unity's [Third Person Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html) supplies shoulder and vertical-arm geometry, distance, per-axis damping and built-in collision correction with separate entry and return damping. It follows the tracking target's orientation. Unity describes an independently rotating invisible target as the way to orbit without rotating the character. For this project, use a stable pivot following Booter's interpolated root, separate from animation bones and visual leaning. Shooting-oriented Third Person Aim is unnecessary for the requested exploration view.

[Orbital Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html) is a useful comparison when composition across high and low orbits matters more than shoulder framing. It offers spherical or three-ring orbits and recenter wait/time controls. Third Person Follow is the first candidate because the requested second view primarily follows from behind. Neither component makes the movement and recentering decisions for the game.

Collision and target visibility are related but separate. [Decollider](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDecollider.html) resolves camera overlap and can lift the camera above terrain; it does not preserve target line of sight. [Deoccluder](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html) attempts to preserve that line of sight and requires collider geometry. Start with Third Person Follow's own collision handling and measure it. Add extensions only for demonstrated gaps, with an explicit order, rather than stacking correction systems by default. Test undersides and overhangs before accepting upward terrain recovery.

The [Cinemachine Brain](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineBrain.html) controls the real Unity Camera; Unity recommends Smart Update for target evaluation and Late Update for blends. Evaluate those settings against Booter's interpolated physics root and the chosen pivot update phase. Its ignore-time-scale setting can keep damping responsive independently of world speed. The [Input Axis Controller](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineInputAxisController.html) documents separate treatment of mouse delta through Cancel Delta Time. A project adapter should consume router-approved input rather than auto-enable actions or bypass modal fences.

[Cinemachine transitions](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/concept-camera-control-transitions.html) animate one output camera between virtual camera states. A Brain does not automatically blend from this project's unrelated custom rig. The first mixed implementation should use an explicit, collision-checked cut with matched yaw. Smooth transitions require an adapter for the elevated state or a common pose/blend owner, plus clearance checks throughout the transition.

Mark Haigh-Hutchinson's [GDC camera design presentation](https://media.gdcvault.com/gdc05/slides/GD_Haigh-Hutchinson_FundamentalsReal-TimeCameraDesign.pdf), especially slides 22–27, connects camera design to player controls: reduce unnecessary motion, preserve player intent, and avoid abrupt changes to the control reference frame. Applied here, delayed recentering should yield immediately to manual look and should not continuously steer camera-relative movement during strafing or reversal. Hold the existing movement reference until neutral when switching modes while the player is moving; then adopt the new view basis.

Microsoft's [Xbox Accessibility Guideline 117](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117) recommends adjustable FOV and camera sensitivity, control over automatic camera movement, and disabling or avoiding shake, bob and blur. Proposed defaults are a level horizon, no added gait bob, no automatic sprint FOV pulse, and optional recentering. These settings belong to local preferences rather than world saves.

The online Cinemachine 3.1 manual currently identifies itself as 3.1.7. The matching topics were also checked in the installed 3.1.5 package's Documentation~ directory. Keep 3.1.5 pinned; any implementation must compile and qualify against that installed API rather than assume newer documentation is an upgrade instruction.

## Proposed two option design

| Option | Player experience | Initial treatment |
| --- | --- | --- |
| Elevated | Existing broad exploration view with manual orbit and scouting pan | Retain the current 22 m settings and remembered orbit |
| Third person | Close view behind Booter with free orbit and optional delayed return behind forward travel | Independent pivot, broader lens, lower pitch and built-in collision |

One coordinator selects a mode and one writer at a time. In the initial mixed study, the custom rig owns transform/lens in Elevated; the Cinemachine Brain owns them in Third person. Disable the losing writer before enabling the winner, and initialize the incoming state from the current view. Guard all pose/lens entry points, including configuration, snaps, Awake and OnValidate; disabling LateUpdate alone does not prevent competing writes. BadwaterCameraRange remains the final far-range authority, applied after the active lens owner through a verified update order or explicit final application. Validate near plane, lens and post-processing ownership explicitly. Leaving a disabled custom rig in the hierarchy is insufficient riding integration: the rider's requests must reach the active framing owner.

Automatic recentering should use a filtered travel heading only during deliberate forward travel after a manual-look idle delay. Suppress it during lateral/backward input, brief pivots, low-speed idle, manual orbit and modal UI. Offer explicit recenter as a separate command when bindings are designed. This proposed policy prevents the camera chasing Booter's moment-to-moment facing while the motor simultaneously turns relative to the camera. It keeps the existing turn-to-travel locomotion model; aiming, strafing animation and combat lock-on are separate design work.

### Initial study values

These are project-specific comparison values, not universal best settings or accepted production tuning. Measure Booter's rendered size and equipped silhouette before final placement.

| Parameter | Initial comparison |
| --- | --- |
| Pivot height | 1.5–1.8 m above root; begin at 1.6 m |
| Boom distance | 4.5–6 m; begin at 5 m |
| Shoulder offset | Start centred; compare 0.35–0.5 m to one side if Booter blocks forward visibility |
| Downward pitch | Start 12 degrees; compare allowed range from 35 degrees upward to 65 degrees downward |
| Vertical FOV | Start 60 degrees; compare 55–65 degrees; label vertical units in settings |
| Orbit | Begin with existing stick rate and mouse sensitivity; expose separate X/Y sensitivity and invert Y |
| Position damping | Compare 0.1–0.2 s horizontal, 0.2–0.35 s vertical; avoid following head motion |
| Recenter | Compare 1.5 s idle delay and about 1 s return; optional and interruptible |
| Collision | Compare 0.25–0.35 m radius against the near-plane footprint; fast safe compression, 0.2–0.4 s recovery |
| Scouting pan | Keep in Elevated; suppress translated pan in Third person for the first study, without silently assigning left trigger another action |

A near-level lens exposes more horizon and potentially the sun, unlike the current 26-degree minimum pitch with a 48-degree vertical FOV. The bounded Camp Loop terrain can expose its outer edges sooner. Close views also expose model clipping, carried equipment, foliage, shadow transitions and optical glare more directly. Compare those costs and visuals before changing far distance, atmosphere or shared renderer settings.

## Implementation and qualification sequence

1. Create an isolated candidate from a documented current snapshot, preserving the author's open scene and existing dirty work. Retain an elevated baseline and add a Third Person Follow study using the existing 3.1.5 package. Do not enter the author's Play Mode or move app focus.
2. Establish the coordinator, stable root pivot, router input adapter and explicit writer handoff. Keep one output Camera and AudioListener. Use menu selection and safe cuts initially; preserve each mode's orbit state and prevent held inputs from leaking through the switch.
3. Route riding and camp framing through a shared request contract. Preserve mount exclusion, dismount/reset behavior, cursor policy, neutral-input fences, compass, optics and far-range preferences. Decide camp-placement framing before enabling the mode there.
4. Qualify the close collision path against existing clearance regressions. Test dense colliders, origin overlap, fully enclosed spaces, target/mount exclusion, terrain boundaries, near-plane corners, orbit sweeps and resets. Define a safe fallback when no valid view exists. Model fading, if needed, should affect presentation only and be a separately measured addition.
5. Add smooth transitions only after selecting a single transition owner. Adapt the elevated solver into the common state pipeline or use a project blend stage; never let two transform writers interpolate concurrently. Test the full path, including obstruction during a switch and interrupted/repeated switches. Check optical visibility history on cuts rather than assume a persistent Camera resets every temporal effect.
6. Present both modes for user-owned hands-on comparison. Promote accepted settings to GreaterWasteland and GameplaySetup through a bounded authoring change, preserving scene GUIDs and current gameplay references.

Focused automated coverage should include camera ownership and round-trip switching, input/modal and timing policy, travel-heading recenter suppression, riding lifecycle and clearance, movement basis across cuts, output reference continuity, and actual-view terrain coverage. Existing relevant fixtures include TopDown3DCameraClearanceTests, FixedTerrainCameraCoverageTests, riding ownership/lifecycle/input fixtures and input/interaction contracts. Source inspection of those tests is not a fresh passing test result.

Hands-on comparison should cover walking, sprinting, sudden turns, reversal while orbiting, uneven ground, cliffs, large rocks, camp stations, carried equipment, mounting/dismounting, inventory/radial/menu transitions and camp speed changes. Compare gamepad and mouse at several frame rates and common aspect ratios. Measure frame time and terrain-render counts from the same locations in each mode. The objective is readable navigation and comfortable control with both options available.

## World identity and persistence boundaries

Camera mode changes presentation and the movement reference frame, not world identity. Greater Wasteland's fixed terrain and collision remain always loaded. Existing visual terrain residency may respond to the new frustum, but no terrain generation or streaming is introduced. No generated-object identity or authored placement changes are required. A mode preference can be stored separately from world saves; runtime world deltas, inventory and terrain state must remain untouched. Runtime camera smoothing and temporary orbit state do not require saved-world integration in this study.

The next decision is approval of this bounded two-option camera study. Exact distance, shoulder bias, recenter feel, switching control and any later smoothing remain creative choices to settle through that comparison.

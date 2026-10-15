# Camera options

Camp Loop supports Elevated and Third person through **Settings → Camera** and two separately editable hierarchy rigs. Apply saves the preference; the active view changes after returning to gameplay when the destination is clear. Back retains camera edits in the Settings draft, and leaving Settings protects unapplied changes with the existing discard confirmation. The authored controls include third-person auto centring, invert Y, separate horizontal/vertical sensitivity and vertical field of view.

## Hierarchy authoring

In CampLoopTesting, expand **Camera → Camera Options**. Its **Elevated Camera Rig** child owns the elevated framing values; **Third Person Camera Rig** owns the third-person profile link and starting FOV. Select either child to edit its Inspector. The third-person Inspector exposes the linked profile inline; those profile edits affect every consumer of that asset. Framing follows Booter and uses these settings; moving the settings objects' transforms does not position the camera.

On Camera Options, **Starting View** selects the view requested for each Play session. The **Use Elevated at Runtime** and **Use Third Person at Runtime** buttons set that same choice from either rig's Inspector. Third Person is currently selected. Keep both objects and their components enabled; use the selector rather than GameObject activation checkboxes. **Preview** applies that rig's framing to the shared output in Edit Mode without changing Starting View, and supports Undo.

During Play mode the buttons become **Switch to …** and request a temporary session change. The selector reports requested and active views separately, so blocked destinations or camp-placement overrides remain visible. The existing runtime menu can still change the session preference. Starting View and the third-person starting FOV take precedence over local saved camera values on the first preference load in each session; subsequent menu Apply works normally. Runtime Inspector changes are temporary. Save the scene after changing authoring settings to retain them.

The visible rigs are settings/pose rigs, with one existing **Main Camera** for rendering and audio. Runtime third-person **Follow Pivot** and **Pose Solver** objects appear under its rig. They are transient and manually evaluated; do not enable the solver's CinemachineCamera component. The legacy elevated serialized values on Main Camera remain compatibility fallbacks for scenes without these authored rigs. Its Inspector routes framing work to the child rigs. Settings copy into the shared follow service only when authored values change, preserving player orbit between edits.

The player preference and effective framing are distinct. An obstructed requested view retains the current camera and retries during gameplay; the Camera panel explains the pending request. Camp placement temporarily requests Elevated and restores the saved preference on return to gameplay. Other modal input modes freeze automatic third-person motion and restart its recenter delay. Elevated retains its existing behavior.

## Installation and scope

The existing menu scene installer loads `ThirdPersonCameraProfile` from Resources and checks its explicit `enabledScenePaths` before enabling options on the existing camera rig. The profile lives under `Assets/_Project/Settings/Player/Resources/`. Its initial allowlist contains only `Assets/_Project/Scenes/Reference/CampLoopTesting.unity`. Camp Loop also has an authored selector that enables its camera options at startup. GreaterWasteland and GameplaySetup retain their existing camera behavior. Production adoption requires the author's comparison and acceptance; no terrain or build-setting migration is performed by this installation.

The shared SystemMenu prefab includes an authored Camera panel. Its entry is hidden when the scene has no configured camera-options owner. Controls are not constructed dynamically during gameplay, and no existing input binding is reassigned. The feature becomes available on the next normal menu installation, normally the next Play session; existing instantiated menus are not forcibly rebuilt.

## Camera ownership and update order

`TopDown3DCameraRig` remains enabled as the canonical follow-target and riding-framing facade. Its existing lens, orbit, snap and follow paths yield output writes while `TopDown3DCameraOptions` owns Third person. This preserves the riding controller's availability checks without changing riding leases or physics.

The coordinator runs at execution order 1000, after normal motor/presentation callbacks. It consumes router-approved look input, advances shared follow/riding framing once, evaluates its disabled CinemachineCamera, resolves clearance, then commits the verified output pose and lens. With authored rigs, its transient solver is visible under Third Person Camera Rig; legacy fixtures retain a hidden solver. It explicitly reapplies BadwaterCameraRange after the third-person lens. The existing terrain-render controller runs at order 2000 and also prepares coverage before rendering.

There is one Unity Camera, one AudioListener and the original camera-local sky/optics bindings. Cinemachine 3.1.5 calculates the third-person follow pose through its manually evaluated component pipeline. There is deliberately no Cinemachine Brain and no second rendering camera. This refines the audit's mixed Brain/custom-writer proposal: evaluating the private state first allows rejection before changing the real camera.

Mode switches are cuts. A switch to Third person begins behind Booter on first entry and remembers its orbit thereafter. A switch to Elevated restores the existing elevated yaw/pitch and resets scouting pan. Both destination poses pass clearance before ownership changes. A rejected destination does not change the output pose, lens or owner. Later smooth blending requires its own path-clearance implementation and qualification.

## Following and clearance

The profile starts with a 5 m boom, 1.6 m root-relative pivot, 12 degree downward pitch and per-axis position damping. Its pitch limits are 35 degrees upward through 65 degrees downward. It follows the interpolated actor root and existing riding frame, without reading animation-bone motion or moving the actor. Orbit and damping use unscaled time capped at 0.1 seconds per camera tick. Mouse delta retains displacement units; stick input retains angular-rate units.

Optional recentering starts after 1.5 seconds without manual look. It follows a filtered actual travel heading at a bounded angular rate during deliberate forward input. Manual look, sideways/backward input, low-speed idle, mounted framing and modal UI suppress automatic recentering. Third person adds no scouting pan, shake, gait bob or sprint FOV pulse.

Cinemachine's built-in collision correction is disabled. Both views use the project clearance solver, preserving Booter/mount hierarchy exclusions, ignored triggers, saturated-query fallback, obstructed-origin recovery and enclosed-boom failure reporting. Third-person clearance encloses the near-plane corners for the selected FOV/aspect ratio. Compression is immediate; recovery expands gradually and is rechecked before applying the pose. When no valid third-person pose exists during following, the coordinator reports unresolved clearance and retains the previous output rather than accepting an intersecting candidate. A moving obstacle can still invalidate a retained position; this is an explicit unresolved state, not a guarantee of an always-available shot.

## Preferences and world state

Camera preference fields extend the existing version-1 local menu settings. Missing or invalid camera values normalize to supported defaults; unknown newer file versions remain protected by the existing service. Failed saves leave accepted preferences and camera requests unchanged. The selected mode, recenter option, inversion, sensitivity and FOV do not enter saved-world records.

Terrain and collision remain fixed and always loaded. The new view may change visual terrain residency through the existing frustum calculations. No seed, generation version, generated-object identity, authored placement, inventory record, runtime world delta or terrain-streaming contract changes.

## Validation and authoring

Use the marked isolated candidate for authoring and validation. `CameraOptionsStudyAuthoring.AuthorFromCli` adds the profile and bounded menu-prefab controls without writing a scene. `ValidateFromCli` opens Camp Loop in that candidate and checks the output camera/listener, allowed scene and authored UI. `CaptureFromCli` makes static Edit Mode comparisons in a graphics-enabled candidate. These commands reject an unmarked project; scene opening is a side effect, so do not run them against the author's Editor.

Focused coverage includes ownership, rejected switches, shared clearance, riding availability/exclusions, modal behavior, placement restoration, old settings, persistence, draft/Apply/discard behavior, profile scope and runtime callbacks. The [implementation evidence](../Evidence/Gameplay/CameraOptions20261015/Implementation/README.md) distinguishes executed automated tests, static framing captures and remaining hands-on work.

Static Edit Mode captures show the editor proxy, not the runtime animated Booter. They establish framing only. Gamepad/mouse feel, mounted traversal, carried-equipment visibility, long-distance rendering cost and target-hardware performance require author comparison. No gameplay smoke traversal, Player build or production promotion is implied.

# Unity Automation

The Windows checkout uses the Unity CLI and its pinned `com.unity.pipeline` package for a local connection to the Editor. Existing editor-side validators and direct executable batchmode workflows remain available. No separate MCP server is required for Codex to invoke the CLI through PowerShell.

## Windows CLI connection

### Camp Loop dodge preparation and fitting

`BooterBigArm.Editor.AnimationPlaygroundDodgeAuthoring.Prepare` creates the private grounded roll derivative/profile and binds it only in the marked background animation-playground candidate. `AnimationPlaygroundDodgeFitting.Fit(worldPerUnit)` then fits the actual equipped source and creates a separate body projection; use the measured RootT.y response returned by preparation. Both tools refuse existing outputs and require batchmode Edit Mode. Preserve originals, metadata and failed candidate outputs.

The [dodge implementation receipt](../Evidence/Gameplay/DodgeImplementation20261016/README.md) records the guarded private installation in the already open main CampLoopTesting slice, East/View/F9 control changes, 185 focused checks, exact preservation seals and recovery. Source/pose/command validation does not replace author-owned Play Mode traversal testing. Never run these candidate authoring tools in the author's Editor or save/reload its dirty scene.

### Camp Loop five resource sources

`BooterBigArm.Editor.CampResourceStudyAuthoring.BuildFromCli` authors the five-source study only in a background candidate marked `.camp-resources-validation`. `ValidateFromCli` opens and validates CampLoopTesting; use the isolated candidate while the author works. `CaptureFromCli` captures the five blockouts offscreen without entering Play Mode. `FitGroundFromCli`, `RefineBlockoutFromCli` and `SealFromCli` are candidate-only authoring operations, not read-only validators.

`InstallInOpenAuthorScene` adds the verified linked prefab with Undo into an already open CampLoopTesting scene. It neither opens nor saves a scene, rejects Play Mode/compilation, validates existing content and removes only its own new object if validation fails. The equivalent menu is **Booter & BigARM → Camp Loop → Install Five Resource Sources**. Never dismiss an external-change dialog by discarding unsaved author work. See the [implementation receipt](../Evidence/Gameplay/CampResources20261014/README.md) for reset behavior, placement, tests and live synchronization status.

### Camp Loop salvage study

`BooterBigArm.Editor.CampLoopSalvageAuthoring.InstallFromCli` authors one dead ghost-machine blockout in the existing marked CampLoopTesting scene, creates private trial items/materials/icons, and assigns scene-local inventory catalog overrides. It requires background batchmode Edit Mode. An already installed study is validated without rebuilding or replacing hand edits. Do not invoke it in an active author-owned Editor session.

`BooterBigArm.Editor.CampLoopSalvageAuthoring.ValidateFromCli` opens and validates that scene in a background candidate, retaining the camp-loop native terrain, actor, rock, Ironstone and cache checks. Opening is a scene-management side effect; use an isolated candidate for tests or captures while the author works. Focused contracts are `CampLoopSalvageTests`; shared gather regressions are `TopDown3DInteractionTests`, `TopDown3DInventoryLoopTests` and `TopDown3DInventoryTests`. The study follows the animation playground's session-only checkpoint policy and existing Reset control.

The CLI is available as `unity` on PATH, with a fallback at `%LOCALAPPDATA%\Unity\bin\unity.exe`. The project pins Pipeline `0.8.0-exp.1`; setup uses CLI `1.0.0-beta.12`. Do not upgrade either automatically. See the [official Pipeline setup](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package).

Always derive the target path from this checkout and pass it explicitly. Other Editor instances can belong to isolated validation copies and must not be selected accidentally:

```powershell
$cli = Join-Path $env:LOCALAPPDATA 'Unity\bin\unity.exe'
$project = (Get-Location).Path
& $cli editors running --json
& $cli status --project-path $project --json
& $cli command --project-path $project --query eval --detail full --json
& $cli command eval 'return UnityEngine.Application.dataPath;' --project-path $project --json
& $cli command console_status --project-path $project --json
& $cli command run_tests --mode editor --filter TopDown3DVolumetricDustTests --filter_type testName --project-path $project --json
```

Pipeline connects automatically after package import and domain reload while the Editor is running. `status` verifies availability; command discovery provides the schema for the installed package. Discover a command before invoking it. `eval` executes C# with full Editor authority: use read-only expressions for inspection and review mutating operations against the task scope. Do not save scenes, enter Play Mode, or run broad repair/build commands merely to verify the connection.

The CLI's `--timeout` uses **seconds** and controls the request. It does not set `eval`'s separate millisecond dispatcher budget. Pass that budget as the second positional argument, for example `unity command eval 'return UnityEditor.EditorApplication.isCompiling;' 25000 --timeout 30 --project-path $project --json`. Confirm that the response's parameters include `timeout: "25000"`. `run_script` uses its named `--timeout_ms` argument instead. After refreshing sources, wait for compilation and confirm the intended loaded runtime/test revision before measuring; matching files on disk alone does not prove which assembly executed.

Reuse an existing target-project Editor. When no Editor owns the target project, the background batchmode launch in [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md) can be used without `-quit` to maintain a CLI session; keep `-batchmode -nographics`, `-WindowStyle Hidden`, and an ignored log. Check process ownership and the lock first. Such a session holds the project lock until closed; do not launch a second Editor against it. Close only a task-owned session, after checking for unsaved work, before a normal interactive launch. Never use `unity open` as an unverified background-safe launcher or follow a CLI suggestion to focus Unity.

CLI control is local to this machine and requires a running Editor. It does not resolve licensing failures or establish visual/gameplay acceptance. Existing `-executeMethod` methods remain the canonical validators; live invocation must inspect their implementation for side effects before use.

Connection, compilation, and focused test proof from the initial setup are recorded in the [2026-10-08 receipt](../Evidence/Unity/CLI_CONNECTION_2026-10-08.md). Select focused tests relevant to each subsequent task; the example above is the setup check, not a required suite for every change.

## Platform Routing

### Camp Loop slow standing strafe review

`Tools/Diagnostics/CampStrafeSourceAudit.cs` derives two complete walk cycles and exports source sole tracks only in the marked background candidate. `calibrate_standing_strafe.py` screens lateral sources and creates private calibration proposals without changing original animation sources. `CampStandingStrafePreparation.Prepare` creates the separate private review; `SelectReview` binds it only in that candidate. These methods refuse to replace existing outputs.

`CampStandingStrafeQualification.RunFinal`, `RunFinalOne` and `RunFinalTwo` measure fixed sole windows through the actual single-output driver at four/one/two bone influences, with a virtual lateral trajectory and no physics stepping or Play Mode. `CampStandingStrafeInstallation.Install(planPath)` imports only six sealed private files and changes the two standing-strafe binding fields in a stable, clean, already open author CampLoopTesting session. It checks exact source/protected hashes and GUID ownership; it never saves/reloads a scene or enters Play Mode. Failed installation restores previous binding bytes and retains new private pairs unbound for recovery. Invoke these tools through guarded `run_script`, after discovering its current schema; targeted task-code import and compilation must precede author installation.

The [implementation receipt](../Evidence/Gameplay/LocomotionImplementation20261014/StandingStrafeReview20261015/README.md) records the 0.5 m/s AP02 envelope, LT/look-ahead control, 157 focused checks, exact recovery and unavailable diagonal/backward coverage. Author Play Mode feel testing remains user-owned.

### Camp Loop gait pace tuning

`Tools/Diagnostics/CampGaitPaceTuning.cs` prepares and measures the authored sprint only in the marked background candidate. `InstallAuthor` imports the sealed private pairs into the already open CampLoopTesting scene in stable Edit Mode, validates exact previous/current hashes and leaves the scene unchanged. `TryJogPace` explicitly configures a temporary 2.5m/s jog during author-owned Play Mode on AP02; it preserves sprint, uses a transient policy and cleans up on Play exit or assembly reload. `PrepareFasterJog` and `InstallFasterJog` handle the later saved 3.0m/s comparison through separately sealed previous/resulting pairs; installation requires stable author Edit Mode and verifies the accepted sprint is unchanged. None of these commands enters Play Mode or drives input. See the [pace-tuning receipt](../Evidence/Gameplay/LocomotionImplementation20261014/GaitPaceTuning20261015/README.md) for recovery, verification and remaining qualification limits.

### Camp Loop forward gait review

`Tools/Diagnostics/CampForwardReviewInstallation.cs` prepares a single session-record patch in the marked offline candidate. `InstallAuthor` applies the measured patch only when the clean CampLoopTesting scene is already the sole open scene, compilation is complete, the paired runtime API is loaded and the saved bytes match the plan. It preserves the other records instead of saving the whole author scene, verifies reload/readiness, and leaves qualification flags false. The [review receipt](../Evidence/Gameplay/LocomotionImplementation20261014/ForwardReview/README.md) includes the plan and handoff. This is an experimental flat-deck review, not full terrain qualification; agents must not enter Play Mode to accept it.

`Apply-CampLocomotionHydration.ps1` accepts an optional validated `-RecoveryName` for a fresh immutable recovery directory. It still requires the task-owned candidate Editor to be stopped and verifies source/target hashes before any transfer. Never reuse or overwrite an earlier recovery directory.

For the restored Windows checkout, start with [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md) and its PowerShell preflight. The macOS paths below describe the earlier workstation and must not be used as Windows executable or project paths.

## Previous Workstation Editor

- Unity version: `6000.4.0f1`
- Editor binary: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity`

## Recommended Workflows

### Open The Project In The Editor

Babineaux's guarded launcher reads the pinned version from the project, avoids a duplicate target-project session, waits for the actual editor window, and preserves the user's current foreground application:

```bash
Docs/Agents/Babineaux/tools/launch-unity.sh
```

Only when the user explicitly requests visible interactive Unity work in the current task, foreground mode may activate Unity and restore `TopDown3DPrototype` when the editor lands on `Untitled`:

```bash
Docs/Agents/Babineaux/tools/launch-unity.sh --foreground
```

Use the direct editor command only when diagnosing the launcher itself. A direct application launch may activate Unity, so agents must not use it unless the current task explicitly authorizes visible foreground Unity work:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM"
```

### Headless Import Or Validation

Use batch mode when you want Unity to reimport assets, refresh the project, or run editor automation without the GUI.

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM" \
  -batchmode -nographics -quit
```

### Build Via `-executeMethod`

The repo should expose static editor methods under an Editor-only assembly so Unity can invoke them from the command line.

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM" \
  -batchmode -nographics -quit \
  -executeMethod BooterBigArm.Editor.BuildAutomation.BuildFromCli \
  -buildTarget StandaloneOSX \
  -buildOutput "/Users/worldbuilder/Desktop/Booter & BigARM/Builds/StandaloneOSX/BooterBigArm.app"
```

`-buildTarget` is optional. If omitted, the build script uses the current active build target in the editor.
When it is present, the build script expects the active build target to already match the requested target.

Editor Play Mode and Development Players use the full world content and production rendering settings by default while logging performance telemetry. The reduced diagnostic profile is opt-in through `Booter & BigARM/World Creator/Reduced Stress Play Mode` in the Editor or `-topDown3DStressProfile` when launching a Development Player. The reduced profile disables runtime decoration and is not suitable for visual or gameplay acceptance.

## Greater Wasteland routing — 2026-10-06

The editor-only [terrain workspace](GREATER_WASTELAND_TERRAIN_WORKSPACE.md) groups Greater Wasteland into geographic sections and 1,024 m sectors. `GreaterWastelandTerrainWorkspace.OrganizeCurrentScene()` is a mutating, Undo-supported hierarchy command that does not save automatically. `AroundBooter()`, `AroundSelection()`, `ShowAll()` and `StopLocalView()` control local Scene view visibility without changing active states, physics or Player visibility. The menu is `Booter & BigARM/Greater Wasteland Terrain`; focused EditMode coverage is `GreaterWastelandTerrainWorkspaceTests`.

`GreaterWastelandPlayerPlacement` provides Edit Mode selection, framing, hierarchy marking and Undo-supported ground placement for the existing player. The menu is `Booter & BigARM/Player Placement`; equivalent controls appear in Booter's player motor Inspector. Placement changes only the player Transform and does not save automatically. `MarkHierarchy()` moves the player root to sibling index zero and sets a scene label icon. `FindPlayer(Scene)` and `TryGroundPoint(Scene, Vector3, out Vector3)` inspect the scene; focused collision/holes tests are `GreaterWastelandPlayerPlacementTests`.

`GreaterWastelandTerrainOverview` draws a transient lightweight surface and footprint outline for hidden terrain in Edit Mode. It is enabled by default; toggle **Show Lightweight Overview** or use **Frame Entire Landscape** under the terrain menu. It adds no scene objects or runtime behavior. `RebuildIfNeeded()` creates only cached in-memory geometry, and `FootprintEdges()` is a pure occupied-cell boundary query. Focused boundary tests are `GreaterWastelandTerrainOverviewTests`. When the scene has unsaved edits, invoke these two pure test methods directly through `eval` rather than the full TestRunner scene-management flow, which may open a Save/Don't Save/Cancel modal. Never discard or save unsaved scene work to clear that dialog by assumption.

`Assets/_Project/Scenes/Production/GreaterWasteland.unity` is the primary scene and only enabled build scene. The existing `BadwaterPlayableSceneBuilder.ValidateFromCli` validates its fixed terrain/play setup and rejects the procedural world generator and generated-world save service. `ConversionBaselineValidator.ValidateFromCli` validates the new build routing. Badwater-named repair, readback and Development-build entry points now target Greater Wasteland; their names remain stable for compatibility. The general build command uses enabled Build Settings and therefore targets Greater Wasteland.

`TopDown3DPrototypeBuilder` and `TopDown3DPrototypeValidator` retain the disabled generated-world reference. They must not install its generator into Greater Wasteland. Old production wording below describes the prior foundation. The isolated Blender terrain authoring builder retains its historical output path and is not a Greater Wasteland rebuild command.

## Current State

- Player-build automation exists at `BooterBigArm.Editor.BuildAutomation.BuildFromCli`.
- `PrototypeSceneBootstrapper` exposes legacy 2D scene build and repair commands under `Assets/_Project/Legacy2D/`. These commands write scene/project content and must not be used as non-mutating validation.
- The protected-baseline validator at `BooterBigArm.Editor.ConversionBaselineValidator.ValidateFromCli` now verifies the TopDown3D production cutover and the preserved legacy boundary.
- The perspective foundation builder is `BooterBigArm.Editor.TopDown3DPrototypeBuilder.BuildFromCli`. It refuses to overwrite an existing generated scene. `RebuildFromCli` intentionally replaces only `Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity` after protected-baseline validation.
- The perspective foundation validator is `BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli`. It verifies protected assets, disabled prototype registration in Build Settings, perspective camera/renderer topology, scene component ownership, missing scripts, and compact BigARM scale.
- The GUI menu `Booter & BigARM/Top Down 3D` provides guarded Build, Open, and Validate commands.
- The Unity Test Framework package is installed, and focused non-smoke EditMode tests exist in `BooterBigArm.Editor.Tests`. Use the Unity menu command `Booter & BigARM/Validation/Run Conversion EditMode Tests` while the GUI owns the project.
- VS Code attach/debugging is already configured in [`.vscode/launch.json`](../../.vscode/launch.json).

### Perspective Foundation Commands

Build once when the generated scene does not exist:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM" \
  -batchmode -nographics -quit \
  -executeMethod BooterBigArm.Editor.TopDown3DPrototypeBuilder.BuildFromCli
```

Validate without changing project content:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM" \
  -batchmode -nographics -quit \
  -executeMethod BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli
```

Run the focused EditMode suite:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -projectPath "/Users/worldbuilder/Desktop/Booter & BigARM" \
  -batchmode -nographics -runTests -testPlatform EditMode \
  -assemblyNames BooterBigArm.Editor.Tests \
  -testResults "/tmp/booter-topdown3d-editmode.xml"
```

## Safety Gate

- Do not start batchmode against this project while the Unity GUI has it open.
- Do not activate, focus, raise, or send keystrokes to Unity during normal repo work, automation, validation, or background launch. Preserve the user's foreground application.
- Keep scene opening, repairs, validation, and appearance checks in the background. Foreground mode is allowed only when the user explicitly asks to bring that window forward. Do not require the user to focus Unity for ordinary agent progress.
- Batchmode may import or serialize assets even when used for compilation; inspect Git state before and after it runs.
- Scene build and repair entry points are mutating tools. Run them only when their output is the requested change and the affected scene/assets are owned by the task.
- Do not create or run gameplay smoke tests unless the user explicitly requests them; hands-on acceptance is user-owned for this project.

## Radial menu production validation

`BooterBigArm.Editor.TopDown3DRadialProductionValidator.ValidateFromCli` validates Greater Wasteland's authored terrain/routing and installs its runtime UI in memory to inspect radial, canister and EventSystem ownership. It does not save scenes. Use `-batchmode -nographics -quit -executeMethod BooterBigArm.Editor.TopDown3DRadialProductionValidator.ValidateFromCli` with the pinned Windows editor and an unowned project or isolated candidate copy; never launch against a checkout already open in Unity.

Focused non-smoke EditMode filters are `BooterBigArm.Tests.TopDown3DRadialTests`, `BooterBigArm.Tests.TopDown3DInventoryUiTests`, and `BooterBigArm.Tests.TopDown3DMicroDustHarvesterTests`. The [integration receipt](../Evidence/UI/RADIAL_MENU_IMPLEMENTATION_2026-10-06.md) records controls, scope and proof boundaries.

## Camp prototype validation

### Nine-tile camp-loop testing scene — 2026-10-13

`Assets/_Project/Scenes/Reference/CampLoopTesting.unity` is the bounded copy of Greater Wasteland's starting-camp playtesting slice. Open this scene and use the existing Play Mode controls. It contains nine native 256 m terrain tiles, Booter, Legger, the seven authored camp rocks, twelve ironstone deposits and the resource cache, plus the copied camera, HUD, atmosphere and lighting. Starting Camp, compass, inventory, radial and system menus install through the existing runtime UI path.

`BooterBigArm.Editor.CampLoopTestingSceneBuilder.ValidateFromCli` opens and validates the testing scene without saving it. `CampLoopTestingSceneTests` checks its saved terrain/content, camp/UI installation and idempotency, and isolated checkpoint path in EditMode. The testing scene is outside enabled Build Settings; Greater Wasteland remains production.

`BuildFromCli` is a one-time mutating extraction command that refuses to replace an existing testing scene. `CompleteFromCli` is a mutating background-only completion command: it installs missing linked ironstone/cache prefabs into the extracted scene, validates, and saves that scene. Neither writes the production scene or TerrainData. TerrainData and authored content assets remain shared; sculpting those shared terrain assets affects both scenes. The testing checkpoint is separate at `Application.persistentDataPath/CampLoopTesting/inventory-v1.json`.

See the [camp-loop testing receipt](../Evidence/Gameplay/CAMP_LOOP_TESTING_SCENE_2026-10-13.md) for the retained grid and verification boundary.

`BooterBigArm.Editor.TopDown3DCampProductionValidator.ValidateAllFromCli` runs the existing production radial/menu validators and the camp wiring check without saving scenes. Run it with the pinned Editor in an unowned isolated candidate, never against a checkout already open in Unity. The terrain-seam audit in the existing validators can take several minutes.

Focused fixtures are `TopDown3DCampTests`, `TopDown3DCampInputTests`, `TopDown3DCampIntegrationTests`, and `TopDown3DSleepTests`. The integration fixture opens disposable/reference scenes, so it must run in an isolated candidate. Regress affected survival, radial, menu, inventory UI, harvester, locomotion, and traversal fixtures when their shared owners change.

`BooterBigArm.Editor.TopDown3DCampCapture.CaptureAssetsFromCli` renders art views in an isolated candidate. `TopDown3DCampPerformanceCapture.RunFromCli` performs a bounded virtual-input performance replay there, using 10-second warmup and 30-second measurement windows at 1x and 4x. It never focuses a real window and must not be run in the user's open checkout. Do not issue diagnostic eval commands during measurement; they can stall the Editor and contaminate results. This is automated simulation/performance evidence, not hands-on gameplay smoke testing. See [the implementation receipt](../Evidence/Gameplay/CAMP_SLEEP_IMPLEMENTATION_2026-10-10.md).

## Greater Wasteland ironstone authoring and validation

`BooterBigArm.Editor.GreaterWastelandIronstoneAuthoring.BuildForLoadedScene()` creates the bounded resource-area prefab from the saved production scene's native collision surface. It requires Greater Wasteland already loaded in clean Edit Mode and refuses to overwrite an existing area prefab. It creates prefab assets through a temporary additive authoring scene, then closes that scene; it never saves production or writes TerrainData. The menu is **Booter & BigARM / Greater Wasteland Ironstone / Install Bounded Harvest Area**. New authoring versions require explicit placement/identity handling rather than regeneration of accepted IDs.

`GreaterWastelandIronstoneAuthoring.ValidateLoadedScene()` validates the prefab's twelve IDs, one state owner, definition links, persistent colliders, LODs, native grounding and existing player wiring. It also rejects generated terrain/save services in production. Invoke through the existing background CLI session. The runtime `GreaterWastelandIronstoneInstaller` installs the Resources prefab once when the production scene loads; the saved terrain scene has no added resource hierarchy.

Focused fixtures are `GreaterWastelandIronstoneTests`, `TopDown3DInteractionTests`, `TopDown3DResourceNodeTests`, `TopDown3DInventoryTests`, `TopDown3DPackingTests`, `TopDown3DInventoryUiTests`, `TopDown3DPlayerLocomotionTests`, `TopDown3DSmartTraversalTests`, and `FixedTerrainDressingTests`. Run them in a task-owned isolated candidate: serialization/installer checks create and delete temporary test scenes. The [implementation receipt](../Evidence/Gameplay/GREATER_WASTELAND_IRONSTONE_IMPLEMENTATION_2026-10-11.md) records proof and the application-restart persistence boundary.

## Starting Camp checks

`BooterBigArm.Editor.TopDown3DWaypointCampValidator.ValidateLoadedProduction()` inspects the already loaded Greater Wasteland scene without opening another scene, installing objects, entering Play Mode, or saving. It checks the authored site, 72 bounded shared-route samples, arrival clearance, menu entry, and prohibited generated services. Call it through read-only CLI eval in the explicitly selected production checkout.

`TopDown3DWaypointRouteTests` and `TopDown3DWaypointCampIntegrationTests` are focused EditMode fixtures. The integration fixture changes disposable/reference scenes and must run in an isolated candidate. Regress existing camp, sleep, menu, survival, inventory UI, locomotion, and traversal tests when their shared seams change. See the [implementation evidence](../Evidence/Gameplay/STARTING_CAMP_IMPLEMENTATION_2026-10-11.md).

`TopDown3DWaypointCampAuthoring.AuthorFromCli` writes the waypoint definition and system-menu prefab; it never writes a scene and must not be described as validation. `TopDown3DWaypointCampPreview.CaptureFromCli` produces static layout/art PNGs only in a guarded isolated candidate, without Play Mode or gameplay simulation. Neither command focuses an app window.

## Minimal inventory loop and authored checkpoint

`GreaterWastelandInventoryAuthoring.BuildCache()` samples the loaded clean production terrain and authors only the finite resource-cache prefab. It uses a temporary authoring scene and leaves Greater Wasteland untouched. `ValidateCache()` inspects the existing asset. Neither opens Play Mode or focuses a window.

`InventoryHaulingStudyAuthoring.BuildFromCli` is a one-time isolated batch authoring command. It creates `Scenes/Reference/InventoryHaulingStudy.unity`, an independent packing asset and small authored ground asset; it refuses to replace an existing study. `Validate(scene)` checks the twelve deposits, low study capacity and absence of generated/save services. The study remains outside enabled build scenes.

Focused inventory proof is `TopDown3DInventoryLoopTests`, `TopDown3DInventoryLoopUiTests`, `GreaterWastelandInventoryPersistenceTests` and `InventoryHaulingStudyTests`, with directly affected existing inventory, Ironstone, dust, input and camp fixtures. These are EditMode state/structure checks, not gameplay smoke tests. Run against a fresh or task-owned isolated candidate; keep file-write tests away from the real persistent-data profile. See [the implementation receipt](../Evidence/Gameplay/INVENTORY_LOOP_IMPLEMENTATION_2026-10-11.md) for results and user-owned feel acceptance.

## General notes

### Controls settings validation

`TopDown3DMenuPrefabAuthoring.AddControlsOwnedFromCli` is a mutating, bounded upgrade of the task-owned `SystemMenu.prefab`. It preserves its GUID and existing file IDs, adds the Controls panel/settings entry and keeps the existing camp link. Never use the full prefab rewrite to replace unrelated authored additions.

`TopDown3DMenuProductionValidator.ValidateControlsPrefabFromCli` validates that prefab's Controls hierarchy, existing camp entry, single EventSystem ownership and shipped input defaults without opening production. `ValidateAndCaptureControlsFromCli` also creates a disposable scene and six normal/large text captures at 1920×1080, 1280×720 and 1024×768 in the candidate's `Logs/ControlsCaptures`. Run captures only in an isolated background batchmode candidate with graphics available; its temporary renderer/pipeline instances are restored and never saved as assets.

Focused fixtures are `TopDown3DControlPreferencesTests`, `TopDown3DMenuTests` and `TopDown3DInventoryLoopUiTests`, with affected existing input, interaction, radial, camp, locomotion, traversal and resource fixtures. File-write tests use their own temporary directories. The [controls receipt](../Evidence/Gameplay/CONTROLS_SETTINGS_IMPLEMENTATION_2026-10-12.md) records current proof and remaining user-owned acceptance.

- Keep build output outside `Assets/`.
- Keep Unity source assets and metadata under version control.
- If the build pipeline grows, add more static entry points rather than embedding shell logic in ad hoc scripts.

## Camp Loop combined motion review

The [combined motion receipt](../Evidence/Gameplay/LocomotionImplementation20261014/CampMotionContinuation20261015/README.md) records the selected slow lateral, adapted pivot, mild-slope and expanded-speed airborne experiment. The accepted forward sources and qualification flags remain separate. Reuse AP02's authored mild ramp/turn circle and AP04's jump fixtures; no scene rewrite is needed.

`CampMotionContinuationPreparation.Create` consumes the sealed native turn/root inputs only in the marked batchmode candidate. `SelectReview` changes that candidate's two continuation binding fields; `Inspect` emits source/dependency and loaded-assembly hashes to a fresh receipt. `CampMotionContinuationCodeImport.Import` accepts a sealed plan and imports only its task scripts into the already-open author Editor. `CampMotionContinuationInstallation.Install` validates source parity/protected hashes, adds four new private payload/metadata files and selects the paired profile. Its destination and receipt guards are one-shot. It preserves the author scene's dirty state and never saves/reloads it, changes focus or enters Play Mode.

Run the focused `AnimationPlaygroundContinuationTests`, expanded `AnimationPlaygroundAirborneTests` and the combined case in `AnimationPlaygroundPlayerIntegrationTests` in the isolated candidate. The receipt lists directly affected regression fixtures. Async `test_status` results must match the requested fixture; parameterized fixture names can use parentheses before the method separator. Keep earlier unselected/intermediate results distinct from the final selected-profile series. Hands-on collision and appearance testing remains author-owned.

## Camp Loop directional and native-ground continuation

The [directional receipt](../Evidence/Gameplay/LocomotionImplementation20261014/CampDirectionalLocomotion20261015/README.md) supersedes the preceding slow lateral/mild-plane selection. `CampDirectionalSourcePreparation` and `CampDirectionalProfilePreparation` prepare complete source imports, loop derivatives and paired schema-2 profiles only in the marked candidate. Original baked root orientation is essential to the selected lateral run. Never replace original/private sources or migrate schema-1 profiles in place.

`CampDirectionalTerrainAudit.InspectNative` reads the already-loaded nine native terrains and writes a fresh collision-geometry receipt. It does not move a player or simulate physics. `InspectCandidateNative` opens and closes only a disposable scene copy in the marked batchmode candidate to compare terrain transforms. `CampDirectionalContactQualification.Inspect` measures fixed sole identities in the actual graph; its diagnostic observations are explicitly not formal contact qualification. `verify_camp_directional_review.py` binds the focused EditMode series to exact author/candidate code and selected input hashes and rejects failed, skipped, inconclusive or mismatched fixtures.

After verification, reuse the guarded task-only code import. `CampDirectionalInstallation.Install` checks protected dependencies, adds the explicitly listed new private files and changes only the two existing directional/continuation selections. It preserves dirty scene state, roots and scene disk bytes. `Inspect` independently reads back the selected family and compilation state. These commands never save/reload the author's scene, change focus, enter Play Mode or send input. Hands-on Camp Loop testing remains author-owned.

## Camp Loop moving-turn arcs

The [moving-turn receipt](../Evidence/Gameplay/MovingTurnArcs20261016/README.md) records the selected Mixamo walking/running/sprint sources, intentional mirrored directions and five-band upper-body composition. Accepted forward clips, root/hips/legs, movement speeds, cadence and native-ground output remain authoritative. The runtime uses phase-aligned upper-bone tapes; the derivative source clips are authoring context and must not be inserted as full-body gait mixer inputs.

`CampMovingTurnPreparation`, `calibrate_moving_turns.py`, `CampMovingTurnAuthoring`, `CampMovingTurnComposition` and `CampMovingTurnPoseAuthoring` operate only in a marked offline candidate and preserve completed outputs. `Run-CampMovingTurnChecks.ps1` runs the affected focused EditMode suites there. `CampMovingTurnContactQualification.Run` compares held poses on identical virtual curved trajectories without physics simulation. `CampMovingTurnReadback.ValidateCandidateSession` prepares only a held candidate main-scene session, while `Snapshot` is read-only.

`CampMovingTurnInstallation.Code` imports only sealed task scripts into the existing author Editor. Wait for successful compilation before `Assets`, which checks protected hashes/GUID ownership, imports the sealed private pairs and changes only the moving-turn binding fields. Both require stable author Edit Mode and the existing Camp Loop scene. They never save/reload it, change focus or enter Play Mode. Pass absolute tool source paths to CLI `run_script` when targeting the candidate; relative paths resolve inside that candidate and may select retained tool versions. Hands-on curve and action-transition feel testing remains author-owned.

## Camp Loop platform, ramp and stair recovery

The [surface recovery receipt](../Evidence/Gameplay/CampSurfaceRecovery20261016/README.md) and [audited plan](../Design/Gameplay/CAMP_LOOP_PLATFORM_RAMP_STAIR_REPAIR_PLAN_2026-10-16.md) cover scene-local runtime ground registration, physical support independent of static pose qualification, robust true-face probes, station-tread observation continuity and bounded step-up/down movement in the existing motor. Accepted gait/animation assets and scene geometry remain unchanged.

`Run-CampSurfaceRepairChecks.ps1` executes the twenty affected focused EditMode filters in the marked candidate. `CampSurfaceRepairInstallation.Import` checks the final sealed code, original recovery bytes, protected inputs and new GUID ownership before importing only task code into the existing author Camp Loop Editor. It requires stable Edit Mode, preserves scene roots/dirty state, imports assets instead of metadata paths, and requests compilation. Verify completion and use `CampMovingTurnReadback.Snapshot` for independent binding/gait/scene/terrain readback. Neither tool saves/reloads the author scene, changes focus or enters Play Mode. Hands-on ramp, stair and landing acceptance remains author-owned.

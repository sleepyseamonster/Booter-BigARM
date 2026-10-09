# Unity Automation

The Windows checkout uses the Unity CLI and its pinned `com.unity.pipeline` package for a local connection to the Editor. Existing editor-side validators and direct executable batchmode workflows remain available. No separate MCP server is required for Codex to invoke the CLI through PowerShell.

## Windows CLI connection

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

Reuse an existing target-project Editor. When no Editor owns the target project, the background batchmode launch in [LOCAL_WORKSPACE.md](../LOCAL_WORKSPACE.md) can be used without `-quit` to maintain a CLI session; keep `-batchmode -nographics`, `-WindowStyle Hidden`, and an ignored log. Check process ownership and the lock first. Such a session holds the project lock until closed; do not launch a second Editor against it. Close only a task-owned session, after checking for unsaved work, before a normal interactive launch. Never use `unity open` as an unverified background-safe launcher or follow a CLI suggestion to focus Unity.

CLI control is local to this machine and requires a running Editor. It does not resolve licensing failures or establish visual/gameplay acceptance. Existing `-executeMethod` methods remain the canonical validators; live invocation must inspect their implementation for side effects before use.

Connection, compilation, and focused test proof from the initial setup are recorded in the [2026-10-08 receipt](../Evidence/Unity/CLI_CONNECTION_2026-10-08.md). Select focused tests relevant to each subsequent task; the example above is the setup check, not a required suite for every change.

## Platform Routing

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

## Notes

- Keep build output outside `Assets/`.
- Keep Unity source assets and metadata under version control.
- If the build pipeline grows, add more static entry points rather than embedding shell logic in ad hoc scripts.

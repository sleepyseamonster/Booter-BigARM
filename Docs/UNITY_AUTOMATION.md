# Unity Automation

This project currently has no MCP bridge exposed in the workspace. The practical control path is the local Unity editor executable plus editor-side automation scripts.

## Installed Editor

- Unity version: `6000.4.0f1`
- Editor binary: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity`

## Recommended Workflows

### Open The Project In The Editor

Babineaux's guarded launcher reads the pinned version from the project, avoids a duplicate target-project session, waits for the actual editor window, and preserves the user's current foreground application:

```bash
Docs/Agents/Babineaux/tools/launch-unity.sh
```

Only when the user explicitly requests visible interactive Unity work in the current task, foreground mode may activate Unity and restore `PrototypeScene` when the editor lands on `Untitled`:

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

## Current State

- Player-build automation exists at `BooterBigArm.Editor.BuildAutomation.BuildFromCli`.
- `PrototypeSceneBootstrapper` exposes prototype scene build and repair commands. These commands write scene/project content and must not be used as non-mutating validation.
- The protected conversion lane has a non-mutating validator at `BooterBigArm.Editor.ConversionBaselineValidator.ValidateFromCli` and a matching Unity menu command.
- The perspective foundation builder is `BooterBigArm.Editor.TopDown3DPrototypeBuilder.BuildFromCli`. It refuses to overwrite an existing generated scene. `RebuildFromCli` intentionally replaces only `Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity` after protected-baseline validation.
- The perspective foundation validator is `BooterBigArm.Editor.TopDown3DPrototypeValidator.ValidateFromCli`. It verifies protected assets, Build Settings exclusion, perspective camera/renderer topology, scene component ownership, missing scripts, and compact BigARM scale.
- The GUI menu `Booter & BigARM/Top Down 3D` provides guarded Build, Open, and Validate commands.
- The Unity Test Framework package is installed, and focused non-smoke EditMode tests exist in `BooterBigArm.Editor.Tests`. Use the Unity menu command `Booter & BigARM/Validation/Run Conversion EditMode Tests` while the GUI owns the project.
- VS Code attach/debugging is already configured in [`.vscode/launch.json`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/.vscode/launch.json).

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
- Foreground mode is allowed only for an explicit current request for visible interactive Unity work. Do not require the user to focus Unity for ordinary agent progress.
- Batchmode may import or serialize assets even when used for compilation; inspect Git state before and after it runs.
- Scene build and repair entry points are mutating tools. Run them only when their output is the requested change and the affected scene/assets are owned by the task.
- Do not create or run gameplay smoke tests unless the user explicitly requests them; hands-on acceptance is user-owned for this project.

## Notes

- Keep build output outside `Assets/`.
- Keep Unity source assets and metadata under version control.
- If the build pipeline grows, add more static entry points rather than embedding shell logic in ad hoc scripts.

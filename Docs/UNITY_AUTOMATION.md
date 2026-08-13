# Unity Automation

This project currently has no MCP bridge exposed in the workspace. The practical control path is the local Unity editor executable plus editor-side automation scripts.

## Installed Editor

- Unity version: `6000.4.0f1`
- Editor binary: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity`

## Recommended Workflows

### Open The Project In The Editor

Babineaux's guarded interactive launcher reads the pinned version from the project, avoids a duplicate target-project session, waits for the actual editor window, restores `PrototypeScene` when Unity lands on `Untitled`, and focuses Unity:

```bash
Docs/Agents/Babineaux/tools/launch-unity.sh
```

Use the direct editor command when diagnosing the launcher itself:

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
- There is not yet a dedicated non-mutating project-validation entry point.
- The Unity Test Framework package is installed, but the repo does not yet contain committed test source files or test assemblies.
- VS Code attach/debugging is already configured in [`.vscode/launch.json`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/.vscode/launch.json).

## Safety Gate

- Do not start batchmode against this project while the Unity GUI has it open.
- Batchmode may import or serialize assets even when used for compilation; inspect Git state before and after it runs.
- Scene build and repair entry points are mutating tools. Run them only when their output is the requested change and the affected scene/assets are owned by the task.

## Notes

- Keep build output outside `Assets/`.
- Keep Unity source assets and metadata under version control.
- If the build pipeline grows, add more static entry points rather than embedding shell logic in ad hoc scripts.

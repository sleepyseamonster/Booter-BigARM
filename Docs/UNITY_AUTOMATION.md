# Unity Automation

This project currently has no MCP bridge exposed in the workspace. The practical control path is the local Unity editor executable plus editor-side automation scripts.

## Installed Editor

- Unity version: `6000.4.0f1`
- Editor binary: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity`

## Recommended Workflows

### Open The Project In The Editor

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

- There is no pre-existing custom editor automation in the repo.
- VS Code attach/debugging is already configured in [`.vscode/launch.json`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/.vscode/launch.json).
- The first automation entry point should stay small and predictable: build the active target from enabled scenes.

## Notes

- Keep build output outside `Assets/`.
- Keep Unity source assets and metadata under version control.
- If the build pipeline grows, add more static entry points rather than embedding shell logic in ad hoc scripts.

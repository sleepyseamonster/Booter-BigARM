# Agent And Unity Practices

This is the working synthesis for how Codex should operate in this Unity repo and how Unity content should be structured.

## Principles

### 1. Keep tasks small and verifiable

- Prefer well-scoped tasks that can be completed and checked in one pass.
- For larger changes, start with a short implementation plan before editing.
- Verify changes with the smallest reliable check path instead of broad guesswork.

Source:
- [How OpenAI uses Codex](https://openai.com/business/guides-and-resources/how-openai-uses-codex/)
- [Introducing Codex](https://openai.com/index/introducing-codex/)

### 2. Use repo instructions as the agent contract

- Keep repo guidance in `AGENTS.md` and related docs.
- Make the docs explicit about command-line entry points, folder conventions, and what not to touch.
- Treat those docs as the local source of truth for future Codex runs.

Source:
- [Introducing Codex](https://openai.com/index/introducing-codex/)
- [`AGENTS.md`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/AGENTS.md)

### 3. Separate editor code from runtime code

- Put editor-only scripts in `Assets/_Project/Scripts/Editor/`.
- Keep runtime gameplay scripts separate from editor tooling.
- Use `.asmdef` boundaries so Unity compiles only what changed and so editor/runtime dependencies stay explicit.

Source:
- [Assembly Definitions](https://docs.unity3d.com/es/2021.1/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- [Assembly definition and packages](https://docs.unity3d.com/ru/2020.2/Manual/cus-asmdef.html)
- [`Assets/_Project/Scripts/Editor/BooterBigArm.Editor.asmdef`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/_Project/Scripts/Editor/BooterBigArm.Editor.asmdef)

### 4. Protect Unity assets and serialization

- Treat `.meta` files as required source files, not noise.
- Rename or move assets in Unity, not by hand in the filesystem, unless you also manage the paired `.meta` file.
- Keep scenes, prefabs, and settings under version control as text serialized assets.

Source:
- [Asset workflow](https://docs.unity3d.com/es/2021.1/Manual/AssetWorkflow.html)
- [Format of Text Serialized files](https://docs.unity3d.com/ru/2019.4/Manual/FormatDescription.html)

### 5. Standardize the 2D URP baseline

- Keep URP + 2D Renderer as the rendering baseline.
- Keep the current renderer assets and project settings as the default project foundation.
- Build scene templates and gameplay scenes on top of that baseline rather than replacing it ad hoc.

Source:
- [Set up the 2D Renderer asset in URP](https://docs.unity3d.com/kr/6000.0/Manual/urp/Setup.html)
- [`Assets/Settings/UniversalRP.asset`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/Settings/UniversalRP.asset)
- [`Assets/Settings/Renderer2D.asset`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Assets/Settings/Renderer2D.asset)

### 6. Use command line for repeatable automation

- Use `-batchmode`, `-quit`, `-projectPath`, and `-logFile` for headless work.
- Use `-executeMethod` for build and validation entry points.
- Do not switch build targets repeatedly in one batch invocation.

Source:
- [Build a player from the command line](https://docs.unity3d.com/ja/current/Manual/build-command-line.html)
- [Command line arguments](https://docs.unity3d.com/es/2017.4/Manual/CommandLineArguments.html)
- [`Docs/UNITY_AUTOMATION.md`](/Users/worldbuilder/Desktop/Booter%20&%20BigARM/Docs/UNITY_AUTOMATION.md)

## Repo-Ready Recommendations

- Keep gameplay source under `Assets/_Project/Scripts/Runtime/`.
- Keep editor automation under `Assets/_Project/Scripts/Editor/`.
- Add additional `.asmdef` files when runtime code grows beyond a trivial prototype.
- Update `Docs/UNITY_AUTOMATION.md` whenever a command-line entry point changes.
- Update `Docs/WORLD_BASIS.md` when lore or game rules change.
- Update `Docs/PROJECT_BASELINE.md` when engine version, core scenes, or core settings change.

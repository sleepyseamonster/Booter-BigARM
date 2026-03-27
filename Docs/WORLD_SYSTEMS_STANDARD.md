# World Systems Standard

This is the baseline for deterministic procedural generation, chunk streaming, and save/load architecture in Booter & BigARM.

## Core Model

- Treat world generation data, runtime world state, and save-file data as separate layers.
- Keep immutable authoring/config data in `ScriptableObject` assets.
- Keep mutable gameplay state in serializable plain C# data, not in scene objects or config assets.
- Make every generated result traceable to a seed, a generation version, and a chunk coordinate or other deterministic key.

Unity docs that inform this baseline:
- [Random.InitState](https://docs.unity3d.com/kr/2020.3/ScriptReference/Random.InitState.html)
- [Random.state](https://docs.unity3d.com/ja/6000.0/ScriptReference/Random-state.html)
- [ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)
- [Script serialization](https://docs.unity3d.com/ja/2023.2/Manual/script-Serialization.html)
- [JSON serialization](https://docs.unity3d.com/ja/current/Manual/json-serialization.html)

## Deterministic Generation

- Use a world seed plus chunk coordinates as the stable input for generation.
- Do not rely on `UnityEngine.Random` as shared global state for generation code that must stay reproducible across systems.
- Prefer an explicit PRNG instance or a scoped random state per generation pass.
- Keep generation steps deterministic by sorting inputs before emitting content and by avoiding time-based or machine-specific values.

Unity notes that `Random.InitState` seeds the pseudo-random sequence, `Random.state` can be saved and restored, and editor scripts should avoid non-deterministic patterns such as time-based values or unsorted file scans.

Relevant sources:
- [Random.InitState](https://docs.unity3d.com/kr/2020.3/ScriptReference/Random.InitState.html)
- [Random.state](https://docs.unity3d.com/ja/6000.0/ScriptReference/Random-state.html)
- [Editor script determinism](https://docs.unity3d.com/kr/6000.0/Manual/build-deterministic-editor-scripts.html)

## Chunking And Streaming

- Represent the world as chunks or regions that can be generated, loaded, and unloaded independently.
- Keep chunk identity stable so the same chunk can be reconstructed later from the same seed and coordinate.
- Use additive scene loading only when authored scene content is the right tool; do not use scenes as the primary save-file format.
- Prefer asynchronous loading for runtime content that can be streamed in or out.
- Keep authored content and generated data separate so world generation can evolve without rewriting save files.

Relevant Unity sources:
- [SceneManager.LoadSceneAsync](https://docs.unity3d.com/jp/current/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)
- [SceneManager](https://docs.unity3d.com/ja/6000.0/ScriptReference/SceneManagement.SceneManager.html)
- [Addressables](https://docs.unity3d.com/es/2020.2/Manual/com.unity.addressables.html)
- [Application.streamingAssetsPath](https://docs.unity3d.com/cn/2022.3/ScriptReference/Application-streamingAssetsPath.html)

## Save/Load

- Store save data under `Application.persistentDataPath`.
- Use structured JSON for player-facing save data and system state.
- Keep save DTOs `[Serializable]`, flat enough for Unity serialization, and versioned.
- Use `JsonUtility` for simple structured saves; use `FromJsonOverwrite` when patching existing objects.
- Do not put mutable save state inside `ScriptableObject` assets.
- Keep runtime state versioned so future migrations can translate old saves forward.

Relevant Unity sources:
- [Application.persistentDataPath](https://docs.unity3d.com/es/530/ScriptReference/Application-persistentDataPath.html)
- [JSON serialization](https://docs.unity3d.com/ja/current/Manual/json-serialization.html)
- [Script serialization](https://docs.unity3d.com/ja/2023.2/Manual/script-Serialization.html)

## Data Boundaries

- Use `ScriptableObject` for static definitions, tuning data, and templates.
- Use serializable DTOs for save files and generated state snapshots.
- Use runtime systems to transform config plus seed into live world state.
- Keep serialization-compatible field layouts stable once save data is public.
- Avoid adding derived or cached data directly to save files unless it is required for migration or performance.

## Evolving Architecture

- Add a `version` field to world, chunk, and save payloads from the start.
- Separate generation algorithms from world-state storage so you can replace one without rewriting the other.
- Keep chunk generation and save migration behind small interfaces so future systems can swap implementations.
- Prefer data-driven world rules over hard-coded generation logic where the content is likely to grow.

## Practical Rule Set

1. World generation must be reproducible from stable inputs.
2. Save data must live outside the project content tree in `persistentDataPath`.
3. `ScriptableObject` assets define config, not mutable save state.
4. JSON DTOs should be versioned and kept Unity-serializable.
5. Streaming content should be asynchronous and chunk-based.
6. Generation and save formats must be designed to evolve without breaking old worlds.

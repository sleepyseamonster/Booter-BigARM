# Gameplay Architecture Baselines

This document captures the current baseline for the game's foundational systems. It is meant to evolve, but it should stay opinionated enough to prevent churn.

## Core Direction

- Gamepad is the primary interaction path.
- Keyboard and mouse remain fully supported and should not be treated as a second-class input path.
- Gameplay systems should be data-driven and able to evolve without rewriting the whole stack.
- Use authored data for tuning and runtime state for progression.

## Input Architecture

- Use the Unity Input System as the base input layer.
- Organize actions into clear action maps such as `Gameplay`, `UI`, and `System`.
- Define control schemes at the `InputActionAsset` level for `Gamepad` and `Keyboard&Mouse`.
- Make gamepad the primary binding group and keep keyboard/mouse as a first-class fallback.
- Use rebinding support for player customization rather than hardcoding controls.
- Keep gameplay code behind a thin input-facing abstraction so UI, gameplay, and accessibility can evolve independently.

Unity docs that support this baseline:
- [Input System](https://docs.unity3d.com/kr/6000.0/Manual/com.unity.inputsystem.html)
- [Input Control Scheme](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.InputControlScheme.html)
- [PlayerInput](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.PlayerInput.html)
- [Input System components](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/manual/Components.html)
- [Rebinding operation](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html)

Practical rule:
- Use `PlayerInput` when its device pairing and action bookkeeping help.
- Avoid coupling core gameplay logic to `SendMessage` style notification flow.
- Keep action maps and bindings stored in the project-owned input asset.

## Movement And Camera

- Treat the player as a physics-backed top-down actor, not a free-floating transform.
- Prefer `Rigidbody2D`-based movement for collision-aware traversal.
- Gather input outside the physics step and apply movement inside the physics step.
- Enable interpolation on the player body when the camera follows it, so motion looks smooth.
- Keep the camera isolated from gameplay logic. The camera should follow a target and provide readability, not own movement rules.
- Keep the camera orthographic unless a later mechanic explicitly requires a different projection.

Unity docs that support this baseline:
- [Rigidbody2D](https://docs.unity3d.com/kr/2022.3/ScriptReference/Rigidbody2D.html)
- [Rigidbody2D interpolation](https://docs.unity3d.com/kr/530/ScriptReference/Rigidbody2D-interpolation.html)
- [Apply interpolation to a Rigidbody](https://docs.unity3d.com/cn/2022.3/Manual/rigidbody-interpolation.html)
- [Rigidbody.velocity](https://docs.unity3d.com/kr/2022.1/ScriptReference/Rigidbody-velocity.html)
- [Rigidbody2D.MovePosition](https://docs.unity3d.com/kr/2022.2/ScriptReference/Rigidbody2D.MovePosition.html)
- [Cinemachine overview](https://docs.unity3d.com/es/2021.1/Manual/com.unity.cinemachine.html)
- [Cinemachine Framing Transposer](https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/CinemachineBodyFramingTransposer.html)

Practical rule:
- If a future camera package is added, it should swap into the follow layer without forcing gameplay rewrites.
- Movement feel can evolve from "simple and readable" to "weighted and tactical" without changing the ownership model.

## Procedural Generation

- Generate from a seed first, then layer runtime deltas on top.
- Treat chunk coordinates and seed derivation as the core of infinite world generation.
- Keep generation rules in authored data assets when possible, not buried in code.
- Generate deterministic output from the same seed and the same inputs.
- Separate "what the world can generate" from "what the player has changed."
- Persist only the runtime changes that matter, not the entire generated world, unless a later mechanic requires it.

Unity docs that support this baseline:
- [Random.InitState](https://docs.unity3d.com/kr/2020.3/ScriptReference/Random.InitState.html)
- [ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)
- [JSON Serialization](https://docs.unity3d.com/es/2021.1/Manual/JSONSerialization.html)

Practical rule:
- Keep the generation contract explicit: seed in, chunk identity in, deterministic output out.
- Use data assets for weighted tables, biome settings, spawn tuning, and authored generation rules.
- Use runtime world state only for player changes, harvested resources, destroyed structures, and other persistent deltas.

## Save And Load

- Use structured save data, not ad hoc scene state.
- Store writable save files in `Application.persistentDataPath`.
- Use JSON for the save payload when a simple structured format is enough.
- Use `ScriptableObject` for static authored configuration, not live save state.
- Use `PlayerPrefs` only for small preferences such as input, audio, or display settings.
- Version every save file so future data migrations are possible.

Unity docs that support this baseline:
- [JSON Serialization](https://docs.unity3d.com/es/2021.1/Manual/JSONSerialization.html)
- [ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)
- [PlayerPrefs.SetString](https://docs.unity3d.com/ja/2023.2/ScriptReference/PlayerPrefs.SetString.html)
- [Application.persistentDataPath](https://docs.unity3d.com/cn/2019.4/ScriptReference/Application-persistentDataPath.html)

Practical rule:
- Save data should describe the player and world deltas, not reserialize the entire authored project.
- Keep save/load logic isolated so it can evolve independently from gameplay systems.

## Evolution Rule

These systems are baselines, not fixed endpoints.

- Prefer small additive changes over rewrites.
- Keep new mechanics data-driven whenever possible.
- Update the architecture docs when a system's ownership model changes.
- If a mechanic forces a major departure from these baselines, write down why before changing code.


# Input Architecture Standard

This is the project baseline for a gamepad-optimized top-down game that still supports keyboard and mouse.

## Baseline

- Use the Unity Input System package as the primary input layer.
- Keep the input asset at `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`.
- Treat gamepad as the primary feel target and keyboard/mouse as first-class fallback support.

Official Input System docs:
- [Input System manual](https://docs.unity3d.com/kr/6000.0/Manual/com.unity.inputsystem.html)
- [InputActionMap API](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.InputActionMap.html)

## Action Maps

- Use separate maps for `Gameplay`, `UI`, and `System` or `Debug` if needed.
- Keep gameplay and UI input separated so menu behavior does not leak into moment-to-moment control.
- Use `PlayerInput` or a small input router to switch the active map on state changes.

Unity's `PlayerInput` docs note that it can pair devices automatically, choose control schemes, and switch the active action map. See:
- [PlayerInput API](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.PlayerInput.html)
- [Player notifications](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.PlayerNotifications.html)

## Control Schemes

- Define at least `Gamepad` and `KeyboardMouse`.
- Make `Gamepad` the default when a compatible controller is present.
- Keep scheme-specific bindings explicit so rebinding and device pairing stay predictable.

`PlayerInput` and control schemes can automatically pair devices and switch schemes when an appropriate device is used. That is useful for a gamepad-first game, but the code should still keep the current scheme explicit in gameplay state. See:
- [PlayerInput API](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/api/UnityEngine.InputSystem.PlayerInput.html)

## Rebinding

- Support interactive rebinding for player-facing options.
- Save overrides to JSON and reload them at startup.
- Restrict rebinding by control scheme so gamepad bindings do not get mixed with keyboard/mouse bindings.

Unity documents `PerformInteractiveRebinding`, `SaveBindingOverridesAsJson`, and `LoadBindingOverridesFromJson` for this workflow. See:
- [Input binding manual](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/manual/ActionBindings.html)

## Gamepad Feel

- Use a `Vector2` move action with a stick binding and a 2D vector keyboard composite.
- Apply a stick deadzone processor to analog movement and aim bindings.
- Tune deadzone values intentionally instead of relying on device defaults.

Unity's processor docs describe `StickDeadzone` and `AxisDeadzone` processors for exactly this purpose. See:
- [Processors manual](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/manual/Processors.html)

## UI Separation

- Keep UI navigation in its own action map.
- Drive the EventSystem with `InputSystemUIInputModule` rather than mixing gameplay and UI bindings.
- Prefer controller-friendly menu navigation from the start so the gamepad path never becomes an afterthought.

Unity's UI support docs cover `InputSystemUIInputModule` and the UI event-system integration. See:
- [Input System UI support](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/manual/UISupport.html)
- [UI event system support](https://docs.unity3d.com/kr/2022.3/Manual/UIE-Runtime-Event-System.html)

## Evolving Architecture

- Treat raw input as an adapter layer, not gameplay logic.
- Keep gameplay systems dependent on higher-level commands or state, not scattered `InputAction` calls.
- Keep action names stable and evolve bindings and control schemes over time.
- Let the input layer own device pairing, rebinding, and map switching.

## Practical Rule Set

1. Gamepad first, keyboard/mouse fully supported.
2. Separate gameplay and UI action maps.
3. Use control schemes and `PlayerInput` for device pairing.
4. Support rebinding with JSON save/load.
5. Use deadzone processors for analog feel.
6. Keep input as an adapter, not game logic.


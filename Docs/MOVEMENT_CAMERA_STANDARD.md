# Movement And Camera Standard

This is the baseline for a gamepad-optimized top-down 2D character and camera setup. It keeps movement responsive, readable, and flexible enough for future mechanics like dashes, knockback, mounts, vehicles, or status effects.

## Core Movement Rule

- Use a `Rigidbody2D`-backed movement motor as the authoritative body for any object that collides or interacts with the world.
- Do not move colliders directly by changing `Transform.position` every frame.
- Keep the movement decision layer separate from the physics application layer.

Unity's Rigidbody2D docs are explicit that moving collider-bearing bodies by Transform causes problems, and that Rigidbody2D movement should be used instead. See:
- [Rigidbody 2D](https://docs.unity3d.com/ru/2019.4/Manual/class-Rigidbody2D.html)
- [Rigidbody2D.MovePosition](https://docs.unity3d.com/kr/2022.2/ScriptReference/Rigidbody2D.MovePosition.html)
- [Rigidbody2D.MovePosition API](https://docs.unity3d.com/es/530/ScriptReference/Rigidbody2D.MovePosition.html)

## Movement Design Rule

- Treat input as a desired movement vector, not as immediate world motion.
- Apply movement through the physics step, not through ad hoc transform updates.
- Keep the movement motor swappable so future mechanics can override speed, turn behavior, or control ownership without rewriting the whole controller.
- If camera follow jitter appears on physics-driven actors, enable `Rigidbody2D` interpolation rather than coupling the camera to raw transform motion.

## Baseline Player Feel

- Gamepad input should drive an analog movement vector.
- Keyboard input should map cleanly to the same movement model.
- The movement layer should support acceleration, slowdown, stun, dash, and other modifiers as data-driven changes.
- Keep movement code deterministic enough that future systems can reason about it.

## Camera Rule

- Default the top-down camera to orthographic.
- Keep the camera follow and framing logic separate from the player movement code.
- Use camera data and camera state changes for tuning, not hard-coded movement coupling.

Unity's Cinemachine docs state that the 2D setup works with an orthographic camera and that the virtual camera drives the Unity camera. See:
- [Cinemachine 2D graphics](https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/Cinemachine2D.html)
- [Cinemachine Virtual Camera properties](https://docs.unity3d.com/ja/Packages/com.unity.cinemachine%402.6/manual/CinemachineVirtualCamera.html)

## Readability Rule

- Use Sorting Layers and Order in Layer as the primary way to express depth and overlap.
- Use Sorting Group for multi-sprite characters, equipment rigs, and any prefab with more than one renderer that should stay visually together.
- Keep camera logic simple; do not rely on camera distance alone to solve 2D readability.

Unity's sorting docs are clear that Sorting Layer and Order in Layer are the primary 2D sorting controls, and that Sorting Group keeps grouped renderers together. See:
- [Sorting Group](https://docs.unity3d.com/es/2021.1/Manual/class-SortingGroup.html)
- [2D Sorting](https://docs.unity3d.com/ru/2021.1/Manual/2DSorting.html)

## Flexibility Rule

- Put camera tuning values in data, not hard-coded constants.
- Keep the camera able to change mode later, such as exploration, combat, interior, or event focus.
- Keep the follow rig able to expand from one target to a target group later, instead of hardwiring all framing to a single actor.
- Keep movement and camera baselines permissive enough to support future traversal mechanics.
- Keep the player motor interface narrow so future traversal types can swap body behavior, camera framing, or input response without reworking the full controller.

## Practical Rule Set

1. Rigidbody2D is the authoritative movement body.
2. Transform motion is not the primary gameplay movement path.
3. Camera is orthographic by default.
4. Cinemachine or an equivalent camera rig handles follow and framing.
5. Sorting layers and sorting groups enforce readability.
